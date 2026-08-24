using Xunit;

namespace RegionToShare.Tests;

public class TryParseSizeTests
{
    [Fact]
    public void ValidInput_AboveMinimum_ParsesSuccessfully()
    {
        var success = MainWindow.TryParseSize("1024x782", minWidth: 300, minHeight: 200, out var size);

        Assert.True(success);
        Assert.Equal(1024, size.Width);
        Assert.Equal(782, size.Height);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1024")]
    [InlineData("1024x")]
    [InlineData("x782")]
    [InlineData("1024x782x1")]
    [InlineData("abcxdef")]
    [InlineData("1024,782")]
    public void MalformedInput_ReturnsFalse(string malformedValue)
    {
        var success = MainWindow.TryParseSize(malformedValue, minWidth: 300, minHeight: 200, out _);

        Assert.False(success);
    }

    [Fact]
    public void WidthBelowMinimum_ReturnsFalse()
    {
        var success = MainWindow.TryParseSize("100x500", minWidth: 300, minHeight: 200, out _);

        Assert.False(success);
    }

    [Fact]
    public void HeightBelowMinimum_ReturnsFalse()
    {
        var success = MainWindow.TryParseSize("500x100", minWidth: 300, minHeight: 200, out _);

        Assert.False(success);
    }

    [Fact]
    public void SizeExactlyAtMinimum_IsAccepted()
    {
        var success = MainWindow.TryParseSize("300x200", minWidth: 300, minHeight: 200, out var size);

        Assert.True(success);
        Assert.Equal(300, size.Width);
        Assert.Equal(200, size.Height);
    }
}

public class ClampFramesPerSecondTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(60)]
    public void SupportedValue_IsReturnedUnchanged(int supportedValue)
    {
        Assert.Equal(supportedValue, MainWindow.ClampFramesPerSecond(supportedValue));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(100)]
    [InlineData(-5)]
    public void UnsupportedValue_FallsBackTo15(int unsupportedValue)
    {
        Assert.Equal(15, MainWindow.ClampFramesPerSecond(unsupportedValue));
    }
}

public class IsValidThemeColorTests
{
    [Theory]
    [InlineData("SteelBlue")]
    [InlineData("Red")]
    [InlineData("#FF0000")]
    [InlineData("#FFFF0000")]
    public void RecognizedColor_IsValid(string color)
    {
        Assert.True(MainWindow.IsValidThemeColor(color));
    }

    [Theory]
    [InlineData("")]
    [InlineData("NotAColor")]
    [InlineData("12345")]
    public void UnrecognizedColor_IsInvalid(string color)
    {
        Assert.False(MainWindow.IsValidThemeColor(color));
    }

    [Fact]
    public void NullColor_DoesNotThrow_ButIsNotFlaggedInvalid()
    {
        // Documents existing behavior: ColorConverter.ConvertFromString(null) returns null
        // without throwing, so IsValidThemeColor(null) reports "valid" even though the
        // result isn't a usable color. This is pre-existing behavior from before this
        // extraction (settings.ThemeColor is a persisted string that's never actually null
        // in practice) - preserved as-is rather than changed as part of this refactor.
        Assert.True(MainWindow.IsValidThemeColor(null));
    }
}
