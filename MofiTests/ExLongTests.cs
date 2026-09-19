using Mofemofies.Data;

namespace MofemofiesTests;

public class ExLongTests
{
    [Fact]
    public void Compare_ExLong_Succeed()
    {
        Assert.Equal(1L, new ExLong(1));
        Assert.Equal(1, new ExLong(1));

        Assert.NotEqual(1, ExLong.PositiveInfinity);
        Assert.NotEqual(ExLong.NegativeInfinity, ExLong.PositiveInfinity);

        var posInfty = ExLong.PositiveInfinity;
        _ = Assert.ThrowsAny<Exception>(() => posInfty == ExLong.PositiveInfinity);

        Assert.True(new ExLong(1L) < new ExLong(2L));
        Assert.True(new ExLong(1) < new ExLong(2L));
        Assert.True(new ExLong(-2L) < new ExLong(-1));
        Assert.True(new ExLong(-2L) < new ExLong(-1L));

        Assert.True(new ExLong() < ExLong.PositiveInfinity);
        Assert.True(ExLong.NegativeInfinity < new ExLong());
        Assert.True(ExLong.NegativeInfinity < ExLong.PositiveInfinity);

        Assert.True(new ExLong(long.MaxValue) < ExLong.PositiveInfinity);
        Assert.True(ExLong.NegativeInfinity < new ExLong(long.MinValue));
    }

    [Fact]
    public void Cast_ExLong_Succeed()
    {
        _ = (long)new ExLong(1);
        _ = new ExLong(1);

        _ = Assert.ThrowsAny<Exception>(() => (long)ExLong.PositiveInfinity);
    }
}
