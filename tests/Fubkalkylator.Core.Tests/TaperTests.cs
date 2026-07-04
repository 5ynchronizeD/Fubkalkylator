using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class TaperTests
{
    [Fact]
    public void Butt_is_thicker_than_top()
    {
        double butt = Taper.ButtDiameterCm(25, 1.0, 4);   // 25 + 1·4
        Assert.Equal(29, butt, 9);
        Assert.True(butt > 25);
    }

    [Fact]
    public void Top_and_butt_are_inverses()
    {
        double butt = Taper.ButtDiameterCm(25, 1.2, 5);
        Assert.Equal(25, Taper.TopDiameterCm(butt, 1.2, 5), 9);
    }

    [Fact]
    public void Mid_is_between_top_and_butt()
    {
        double top = 25, taper = 1.0, len = 4;
        double mid = Taper.MidDiameterCm(top, taper, len);
        Assert.Equal(27, mid, 9);
        Assert.InRange(mid, top, Taper.ButtDiameterCm(top, taper, len));
    }

    [Fact]
    public void Negative_length_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Taper.ButtDiameterCm(25, 1, -1));
    }
}
