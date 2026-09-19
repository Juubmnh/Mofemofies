open System
open System.IO
open SkiaSharp
open nkast.Aether.Physics2D.Common
open nkast.Aether.Physics2D.Dynamics
open Mofemofies
open Mofemofies.Components
open Mofemofies.Data

type FilmDesc() =
    class
        inherit Scriptable("FilmDesc")

        member val Width: int = 1920 with get, set
        member val Height: int = 1080 with get, set
        member val FrameRate: float32 = 60f with get, set
        member val DurationSeconds: float32 = 10f with get, set
    end

let filmDesc = FilmDesc()

let yaml = Scriptor.Serializer.Serialize(filmDesc)
File.WriteAllText("filmDesc.yaml", yaml)

let deserializedFilmDesc = Scriptor.Deserializer.Deserialize<Scriptable>(yaml)

type GlowStyle = {
    GlowColor: SKColor
    GlowRadius: float32
    CoreColor: SKColor
    BlendAdditive: bool
}

module SkiaGlow =
    let drawGlowingCircle (canvas: SKCanvas) (center: SKPoint) (radius: float32) (style: GlowStyle) =
        use glowPaint = new SKPaint(
            IsAntialias = true,
            Color = style.GlowColor,
            Style = SKPaintStyle.Fill,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, style.GlowRadius)
        )
        
        if style.BlendAdditive then
            glowPaint.BlendMode <- SKBlendMode.Plus

        use corePaint = new SKPaint(
            IsAntialias = true,
            Color = style.CoreColor,
            Style = SKPaintStyle.Fill
        )

        canvas.DrawCircle(center, radius, glowPaint)
        canvas.DrawCircle(center, radius * 0.9f, corePaint)

    let drawGlowingPath (canvas: SKCanvas) (path: SKPath) (strokeWidth: float32) (style: GlowStyle) =
        use glowPaint = new SKPaint(
            IsAntialias = true,
            Color = style.GlowColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, style.GlowRadius)
        )

        use corePaint = new SKPaint(
            IsAntialias = true,
            Color = style.CoreColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth * 0.5f
        )

        canvas.DrawPath(path, glowPaint)
        canvas.DrawPath(path, corePaint)

[<Measure>] type px
[<Measure>] type m
[<Measure>] type s

type Ball(body: Body, radiusPx: float32<px>, color: SKColor) =
    member val body = body
    member val radiusPx = radiusPx
    member val color = color

type MyPhysicsSystem() =
    let displayToSim = 0.01f<m / px>
    let simToDisplay = 100f<px / m>

    let colors : SKColor[] = [| 
        SKColor(242uy, 66uy, 89uy)
        SKColor(38uy, 173uy, 97uy)
        SKColor(51uy, 153uy, 219uy)
        SKColor(242uy, 196uy, 15uy) 
    |]

    let mutable world : World = null
    let mutable balls: List<Ball> = []

    let ballPaint = new SKPaint(
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    )

    let textPaint = new SKPaint(
        IsAntialias = true,
        Color = SKColors.White
    )

    let textFont = new SKFont(
        Typeface = SKTypeface.FromFamilyName "Consolas",
        Size = 30f
    )

    member val BallCount: int = 0 with get, set
    member val LastFrameMilliseconds = 0.0 with get, set

    interface IDisposable with
        member this.Dispose (): unit = 
            ballPaint.Dispose()
            textPaint.Dispose()
            textFont.Dispose()

    interface IMofiDisplay with
        member this.Load (frame: int64): unit = 
            world <- World(Vector2(0f, 9.8f))
            
            let wM = 1920f<px> * displayToSim
            let hM = 1080f<px> * displayToSim

            let walls = [ 
                world.CreateEdge(Vector2(0f, 0f), Vector2(float32 wM, 0f))
                world.CreateEdge(Vector2(0f, float32 hM), Vector2(float32 wM, float32 hM))
                world.CreateEdge(Vector2(0f, 0f), Vector2(0f, float32 hM))
                world.CreateEdge(Vector2(float32 wM, 0f), Vector2(float32 wM, float32 hM)) 
            ]

            for wall in walls do
                wall.FixtureList[0].Restitution <- 0.85f
                wall.FixtureList[0].Friction <- 0f
                
            let random = Random(42)

            let createBall(i: int) =
                let radiusPx = float32(random.Next(25, 46)) * 1f<px>
                
                let radiusM = radiusPx * displayToSim
                let posX = float32(random.Next(100, 1820)) * 1f<px> * displayToSim
                let posY = float32(random.Next(100, 540)) * 1f<px> * displayToSim
                let vx = float32(random.Next(-300, 301)) * 1f<px / s> * displayToSim
                let vy = float32(random.Next(-100, 101)) * 1f<px / s> * displayToSim

                let body = world.CreateCircle(
                    float32 radiusM,
                    1f,
                    Vector2(float32 posX, float32 posY),
                    BodyType.Dynamic
                )
                
                body.LinearVelocity <- Vector2(float32 vx, float32 vy)
                body.FixtureList[0].Restitution <- 0.85f
                body.FixtureList[0].Friction <- 0f

                Ball(body, radiusPx, colors[i % colors.Length])

            balls <- [for i in 1..this.BallCount -> createBall i]

        member this.Update (sender: DisplayCallStack, frame: int64): unit = 
            if world <> null then
                world.Step(1f / 60f)

        member this.Render (canvas: SKCanvas): unit = 
            canvas.Clear(SKColor(20uy, 20uy, 31uy))

            let style = {
                GlowColor = SKColor(0uy, 230uy, 255uy, 255uy)
                GlowRadius = 15f
                CoreColor = SKColors.White
                BlendAdditive = true
            }

            for ball in balls do
                ballPaint.Color <- ball.color
                
                let px = ball.body.Position.X * 1f<m> * simToDisplay
                let py = ball.body.Position.Y * 1f<m> * simToDisplay
                
                SkiaGlow.drawGlowingCircle canvas
                    (SKPoint(float32(px), float32(py)))
                    (float32(ball.radiusPx))
                    ({ style with GlowColor = ball.color; CoreColor = ball.color})

            let text = $"LastFrameMilliseconds: {this.LastFrameMilliseconds}"
            canvas.DrawText(text, 10f, 30f, SKTextAlign.Left, textFont, textPaint)

let movie = new MofiMovie()

let smallChannel = MofiChannel(300L)

let smallEventFactory = MofiEventFactory(0, Action<MofiEvent>(fun event ->
    event.QueryComponent<MyPhysicsSystem>(fun physics ->
        physics.BallCount <- 5
        event.QueryComponent<CEveryGivenTime>(fun comp ->
            comp.Interval <- TimeSpan.FromSeconds 1.
            comp.Executor <- Action(fun () -> physics.LastFrameMilliseconds <- movie.LastFrameMilliseconds)))))
        

smallChannel.Subscribe smallEventFactory

let largeChannel = MofiChannel()

let largeEventFactory = smallEventFactory.Inherit(Action<MofiEvent>(fun event ->
    event.QueryComponent<MyPhysicsSystem>(fun comp ->
        comp.BallCount <- 15)))

largeChannel.Subscribe largeEventFactory

movie.SceneFactories.Add(MofiSceneFactory(fun scene ->
    scene.Channels.Add smallChannel))

movie.SceneFactories.Add(MofiSceneFactory(fun scene ->
    scene.Channels.Add largeChannel))

ObjectPool.ShowDebugInfo <- true

//movie.Mode <- MofiMode.Export
movie.Run()
movie.Dispose()

ObjectPool.Clear()
