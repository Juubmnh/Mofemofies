using Fractions;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Mofemofies;

public enum MofiMode
{
    Preview,
    Export
}

public sealed class MofiMovie : IDisposable
{
    private Process? _ffmpeg;
    private uint _framebuffer, _texture;
    private GRBackendRenderTarget? _renderTarget;
    private SKSurface? _surface;
    private Fraction _viewportScale;
    private Vector2D<int> _viewportOffset;

    public MofiMode Mode { get; set; } = MofiMode.Preview;
    public string FFmpegPath { get; set; } = "ffmpeg";
    public string? FFmpegArguments { get; set; }
    public string OutputPath { get; set; } = "output.mp4";
    public Vector2D<int> CanvasSize { get; set; } = new(1920, 1080);
    public long FPS { get; set; } = 60;
    public double LastFrameMilliseconds { get; private set; }

    public IWindow? PreviewWindow { get; private set; }
    public Vector2D<int> PreviewSize { get; set; } = new(1280, 960);
    public GL? SilkGL { get; private set; }
    public Vector4D<int>? BitDepth { get; set; }
    public int? StencilBits { get; set; }
    public int? SampleCount { get; set; }
    public SKColorType ColorType { get; set; } = SKColorType.Rgba8888;
    public SKColorSpace? ColorSpace { get; set; }
    public PixelFormat PixelFormat { get; set; } = PixelFormat.Rgba;
    public PixelType PixelType { get; set; } = PixelType.UnsignedByte;
    public int PixelSize { get; set; } = 4;
    public GLEnum TextureFilterParameter { get; set; } = GLEnum.Linear;
    public GRGlInterface? SKGRGL { get; private set; }
    public GRContext? SKGRContext { get; private set; }
    public SKSurfaceProperties? SurfaceProperties { get; set; }
    public IInputContext? InputContext { get; private set; }

    private MofiScene? _currentScene;
    private readonly ObstructiveTimer _obstructiveTimer = new();

    public long CurrentFrameIndex { get; private set; }
    public long TargetFrameIndex { get; private set; }
    public List<MofiSceneFactory> SceneFactories { get; } = [];
    public int CurrentSceneIndex { get; private set; }
    public bool HasAvailableScene => 0 <= CurrentSceneIndex && CurrentSceneIndex < SceneFactories.Count;
    public bool IsRestarting { get; private set; }
    public bool IsFrozen { get; private set; }
    public bool IsSkipping { get; private set; }

    public void Dispose()
    {
        _surface?.Dispose();
        _renderTarget?.Dispose();
        SKGRContext?.Dispose();
        SKGRGL?.Dispose();
        SilkGL?.DeleteTexture(_texture);
        SilkGL?.DeleteFramebuffer(_framebuffer);
        SilkGL?.Dispose();
        InputContext?.Dispose();
        PreviewWindow?.Dispose();
        _ffmpeg?.Dispose();
    }

    [MemberNotNull(nameof(PreviewWindow))]
    public void Run()
    {
        var windowOptions = WindowOptions.Default with
        {
            Size = PreviewSize,
            Title = "Mofemofies Preview",
            WindowBorder = WindowBorder.Fixed,
            PreferredBitDepth = BitDepth,
            PreferredStencilBufferBits = StencilBits,
            Samples = SampleCount,
            VSync = false,
            IsVisible = Mode == MofiMode.Preview
        };

        PreviewWindow = Window.Create(windowOptions);
        PreviewWindow.Initialize();

        CreateOpenGL();

        if (Mode == MofiMode.Preview)
        {
            CreateInput();
            PreviewRenderLoop();
        }
        else
        {
            StartFFmpeg();
            ExportRenderLoop();
        }
    }

    [MemberNotNull(nameof(SilkGL), nameof(SKGRGL), nameof(SKGRContext), nameof(_renderTarget), nameof(_surface))]
    private void CreateOpenGL()
    {
        SilkGL = PreviewWindow.CreateOpenGL();
        _framebuffer = SilkGL.GenFramebuffer();
        _texture = SilkGL.GenTexture();
        SilkGL.BindTexture(TextureTarget.Texture2D, _texture);

        SilkGL.TexImage2D(TextureTarget.Texture2D, level: 0, (int)ColorType.ToGlSizedFormat(),
                (uint)CanvasSize.X, (uint)CanvasSize.Y, border: 0,
                PixelFormat, PixelType, ReadOnlySpan<byte>.Empty);
        SilkGL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureFilterParameter);
        SilkGL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureFilterParameter);
        SilkGL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        SilkGL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _texture, level: 0);
        if (SilkGL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            throw new Exception("Creating framebuffer failed.");
        }

        SKGRGL = GRGlInterface.Create();
        SKGRContext = GRContext.CreateGl(SKGRGL);

        SilkGL.GetInteger(GLEnum.StencilBufferBit, out var stencilBits);
        GRGlFramebufferInfo framebufferInfo = new(_framebuffer, ColorType.ToGlSizedFormat());
        _renderTarget = new(CanvasSize.X, CanvasSize.Y,
            SampleCount ?? 0, stencilBits, framebufferInfo);

        _surface = SKSurface.Create(SKGRContext, _renderTarget, GRSurfaceOrigin.BottomLeft, ColorType,
            ColorSpace, SurfaceProperties);

        if (Mode == MofiMode.Preview)
        {
            Fraction scaleX = new(PreviewWindow!.FramebufferSize.X, CanvasSize.X),
                scaleY = new(PreviewWindow.FramebufferSize.Y, CanvasSize.Y);
            _viewportScale = scaleX < scaleY ? scaleX : scaleY;
            _viewportOffset = new(((PreviewWindow.FramebufferSize.X - CanvasSize.X * _viewportScale) / 2).ToInt32(),
                ((PreviewWindow.FramebufferSize.Y - CanvasSize.Y * _viewportScale) / 2).ToInt32());
        }
    }

    [MemberNotNull(nameof(InputContext))]
    private void CreateInput()
    {
        InputContext = PreviewWindow!.CreateInput();
        foreach (var keyboard in InputContext.Keyboards)
        {
            keyboard.KeyDown += KeyDown;
            keyboard.KeyUp += KeyUp;
        }
    }

    [MemberNotNull(nameof(_ffmpeg))]
    private void StartFFmpeg()
    {
        FFmpegArguments ??= $"-y -f rawvideo -vcodec rawvideo -s {CanvasSize.X}x{CanvasSize.Y}" +
                $" -pix_fmt rgba -r {FPS} -i pipe:0 -vf vflip -c:v libx264 -pix_fmt yuv420p \"{OutputPath}\"";
        ProcessStartInfo ffmpegProcessInfo = new()
        {
            FileName = FFmpegPath,
            Arguments = FFmpegArguments,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            UseShellExecute = false
        };

        _ffmpeg = Process.Start(ffmpegProcessInfo) ?? throw new NullReferenceException("Starting FFmpeg process failed.");
    }

    private void PreviewRenderLoop()
    {
        _obstructiveTimer.FPS = FPS;
        Stopwatch timer = new();

        IsRestarting = true;
        while (!PreviewWindow!.IsClosing)
        {
            timer.Restart();
            PreviewWindow.DoEvents();

            if (IsRestarting)
            {
                TargetFrameIndex = 0;
                CurrentFrameIndex = 0;
                CurrentSceneIndex = -1;

                if (SceneFactories.Count > 0)
                {
                    _ = SwitchScene(0);
                }

                IsRestarting = false;
            }

            if (HasAvailableScene)
            {
                if (!Update(CurrentFrameIndex))
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

            SilkGL!.Viewport(0, 0, (uint)CanvasSize.X, (uint)CanvasSize.Y);
            SilkGL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
            SilkGL.Clear((uint)GLEnum.ColorBufferBit);

            SKGRContext!.ResetContext();
            Render();
            SKGRContext.Flush();

            SilkGL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebuffer);
            SilkGL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            SilkGL.BlitFramebuffer(0, 0, CanvasSize.X, CanvasSize.Y,
                _viewportOffset.X, _viewportOffset.Y,
                _viewportOffset.X + (int)(CanvasSize.X * _viewportScale),
                _viewportOffset.Y + (int)(CanvasSize.Y * _viewportScale),
                ClearBufferMask.ColorBufferBit, TextureFilterParameter);

            PreviewWindow.SwapBuffers();
            timer.Stop();
            LastFrameMilliseconds = timer.Elapsed.TotalMilliseconds;

            if (CurrentFrameIndex == TargetFrameIndex)
            {
                if (IsSkipping)
                {
                    _obstructiveTimer.Restart();
                    IsSkipping = false;
                }

                _obstructiveTimer.Wait();

                TargetFrameIndex++;
            }

            CurrentFrameIndex++;
            _obstructiveTimer.Proceed();

            if (IsFrozen)
            {
                _obstructiveTimer.Stop();

                while (IsFrozen && !PreviewWindow.IsClosing)
                {
                    PreviewWindow.DoEvents();
                    Thread.Sleep(10);
                }

                _obstructiveTimer.Restart();
            }
        }
    }

    private void ExportRenderLoop()
    {
        if (SceneFactories.Count == 0)
        {
            return;
        }

        CurrentSceneIndex = -1;
        _ = SwitchScene(0);

        var framePixels = new byte[CanvasSize.X * CanvasSize.Y * PixelSize];
        for (CurrentFrameIndex = 0; HasAvailableScene; CurrentFrameIndex++)
        {
            if (!Update(CurrentFrameIndex))
            {
                break;
            }

            SilkGL!.Viewport(0, 0, (uint)CanvasSize.X, (uint)CanvasSize.Y);
            SilkGL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
            SilkGL.Clear((uint)GLEnum.ColorBufferBit);

            SKGRContext!.ResetContext();
            Render();
            SKGRContext.Flush();

            SilkGL.PixelStore(PixelStoreParameter.PackAlignment, 1);
            SilkGL.ReadPixels(0, 0, (uint)CanvasSize.X, (uint)CanvasSize.Y,
                     PixelFormat, PixelType, framePixels);

            _ffmpeg!.StandardInput.BaseStream.Write(framePixels, offset: 0, framePixels.Length);
        }

        _ffmpeg!.StandardInput.BaseStream.Flush();
        _ffmpeg.StandardInput.BaseStream.Close();
        _ffmpeg.WaitForExit();
    }

    private bool Update(long frameIndex)
    {
        while (true)
        {
            ((IMofiDisplay)_currentScene!).Update(new(), frameIndex);

            if (_currentScene.IsFinished)
            {
                if (!SwitchScene(frameIndex))
                {
                    return false;
                }
            }
            else
            {
                break;
            }
        }

        return true;
    }

    [MemberNotNullWhen(true, nameof(_currentScene))]
    private bool SwitchScene(long frameIndex)
    {
        _currentScene?.Dispose();

        CurrentSceneIndex++;
        if (!HasAvailableScene)
        {
            return false;
        }

        _currentScene = SceneFactories[CurrentSceneIndex].Create();
        ((IMofiDisplay)_currentScene).Load(frameIndex);

        if (Mode == MofiMode.Preview)
        {
            _obstructiveTimer.Restart();
        }

        return true;
    }

    private void Render() => ((IMofiDisplay)_currentScene!).Render(_surface!.Canvas);

    private void KeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        switch (key)
        {
            case Key.Escape:
                PreviewWindow!.Close();
                break;
            case Key.R:
                IsRestarting = true;
                break;
            case Key.Space:
                IsFrozen = !IsFrozen;
                break;
            case Key.Right:
                _obstructiveTimer.Stop();
                IsSkipping = true;
                TargetFrameIndex += 60;
                break;
            case Key.ControlLeft:
                _obstructiveTimer.FPS *= 2;
                _obstructiveTimer.Restart();
                break;
            default:
                break;
        }
    }

    private void KeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        switch (key)
        {
            case Key.ControlLeft:
                _obstructiveTimer.FPS = FPS;
                _obstructiveTimer.Restart();
                break;
            default:
                break;
        }
    }
}
