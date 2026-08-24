using Xunit;
using static RegionToShare.NativeMethods;

namespace RegionToShare.Tests;

public class SerializeDeserializeTests
{
    [Fact]
    public void Serialize_ProducesTabSeparatedEdges()
    {
        var rect = new RECT { Left = 10, Top = 20, Right = 310, Bottom = 420 };

        var result = rect.Serialize();

        Assert.Equal("10\t20\t310\t420", result);
    }

    [Fact]
    public void DeserializeFrom_RoundTripsASerializedRect()
    {
        var original = new RECT { Left = 10, Top = 20, Right = 310, Bottom = 420 };
        var serialized = original.Serialize();

        var target = new RECT();
        var success = target.DeserializeFrom(serialized);

        Assert.True(success);
        Assert.Equal(original.Left, target.Left);
        Assert.Equal(original.Top, target.Top);
        Assert.Equal(original.Right, target.Right);
        Assert.Equal(original.Bottom, target.Bottom);
    }

    [Fact]
    public void DeserializeFrom_ClampsWidthToMinimum200()
    {
        // Right - Left would be 50, below the 200 minimum.
        var target = new RECT();

        var success = target.DeserializeFrom("0\t0\t50\t500");

        Assert.True(success);
        Assert.Equal(200, target.Right);
    }

    [Fact]
    public void DeserializeFrom_ClampsHeightToMinimum200()
    {
        // Bottom - Top would be 50, below the 200 minimum.
        var target = new RECT();

        var success = target.DeserializeFrom("0\t0\t500\t50");

        Assert.True(success);
        Assert.Equal(200, target.Bottom);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-rect")]
    [InlineData("1\t2\t3")]
    [InlineData("1\t2\t3\t4\t5")]
    [InlineData("1\ta\t3\t4")]
    public void DeserializeFrom_MalformedInput_ReturnsFalseAndLeavesRectUntouched(string malformedValue)
    {
        var target = new RECT { Left = 1, Top = 2, Right = 3, Bottom = 4 };

        var success = target.DeserializeFrom(malformedValue);

        Assert.False(success);
        Assert.Equal(1, target.Left);
        Assert.Equal(2, target.Top);
        Assert.Equal(3, target.Right);
        Assert.Equal(4, target.Bottom);
    }
}
