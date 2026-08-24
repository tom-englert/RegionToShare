using System.Windows;
using Xunit;
using static RegionToShare.NativeMethods;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Windows.Size;

namespace RegionToShare.Tests;

public class RectTests
{
    [Fact]
    public void WidthAndHeight_AreComputedFromEdges()
    {
        var rect = new RECT { Left = 10, Top = 20, Right = 110, Bottom = 220 };

        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
    }

    [Fact]
    public void TopLeftAndBottomRight_ReturnCorrespondingCorners()
    {
        var rect = new RECT { Left = 10, Top = 20, Right = 110, Bottom = 220 };

        Assert.Equal(new POINT(10, 20), rect.TopLeft);
        Assert.Equal(new POINT(110, 220), rect.BottomRight);
    }

    [Fact]
    public void AddThickness_GrowsRectOutward()
    {
        var rect = new RECT { Left = 100, Top = 100, Right = 200, Bottom = 200 };
        var border = new Thickness(1, 2, 3, 4);

        var result = rect + border;

        Assert.Equal(99, result.Left);
        Assert.Equal(98, result.Top);
        Assert.Equal(203, result.Right);
        Assert.Equal(204, result.Bottom);
    }

    [Fact]
    public void SubtractThickness_ShrinksRectInward()
    {
        var rect = new RECT { Left = 100, Top = 100, Right = 200, Bottom = 200 };
        var border = new Thickness(1, 2, 3, 4);

        var result = rect - border;

        Assert.Equal(101, result.Left);
        Assert.Equal(102, result.Top);
        Assert.Equal(197, result.Right);
        Assert.Equal(196, result.Bottom);
    }

    [Fact]
    public void AddThenSubtractSameThickness_RoundTrips()
    {
        var rect = new RECT { Left = 50, Top = 60, Right = 150, Bottom = 260 };
        var border = new Thickness(4);

        var result = (rect + border) - border;

        Assert.Equal(rect.Left, result.Left);
        Assert.Equal(rect.Top, result.Top);
        Assert.Equal(rect.Right, result.Right);
        Assert.Equal(rect.Bottom, result.Bottom);
    }

    [Fact]
    public void AddPoint_OffsetsAllEdges()
    {
        var rect = new RECT { Left = 10, Top = 10, Right = 20, Bottom = 20 };
        var offset = new POINT(5, -3);

        var result = rect + offset;

        Assert.Equal(15, result.Left);
        Assert.Equal(7, result.Top);
        Assert.Equal(25, result.Right);
        Assert.Equal(17, result.Bottom);
    }

    [Fact]
    public void SubtractPoint_OffsetsAllEdgesInReverse()
    {
        var rect = new RECT { Left = 15, Top = 7, Right = 25, Bottom = 17 };
        var offset = new POINT(5, -3);

        var result = rect - offset;

        Assert.Equal(10, result.Left);
        Assert.Equal(10, result.Top);
        Assert.Equal(20, result.Right);
        Assert.Equal(20, result.Bottom);
    }

    [Fact]
    public void ImplicitConversion_ToAndFromWpfRect_RoundTrips()
    {
        var rect = new RECT { Left = 10, Top = 20, Right = 110, Bottom = 220 };

        Rect wpfRect = rect;
        RECT roundTripped = wpfRect;

        Assert.Equal(rect.Left, roundTripped.Left);
        Assert.Equal(rect.Top, roundTripped.Top);
        Assert.Equal(rect.Right, roundTripped.Right);
        Assert.Equal(rect.Bottom, roundTripped.Bottom);
    }

    [Fact]
    public void ImplicitConversion_ToAndFromDrawingRectangle_RoundTrips()
    {
        var rect = new RECT { Left = 10, Top = 20, Right = 110, Bottom = 220 };

        Rectangle drawingRect = rect;
        RECT roundTripped = drawingRect;

        Assert.Equal(rect.Left, roundTripped.Left);
        Assert.Equal(rect.Top, roundTripped.Top);
        Assert.Equal(rect.Right, roundTripped.Right);
        Assert.Equal(rect.Bottom, roundTripped.Bottom);
    }
}

public class PointTests
{
    [Fact]
    public void Add_SumsComponents()
    {
        var result = new POINT(1, 2) + new POINT(3, 4);

        Assert.Equal(new POINT(4, 6), result);
    }

    [Fact]
    public void Subtract_DiffsComponents()
    {
        var result = new POINT(10, 10) - new POINT(3, 4);

        Assert.Equal(new POINT(7, 6), result);
    }

    [Fact]
    public void ImplicitConversion_ToAndFromWpfPoint_RoundTrips()
    {
        var point = new POINT(12, 34);

        Point wpfPoint = point;
        POINT roundTripped = wpfPoint;

        Assert.Equal(point, roundTripped);
    }

    [Fact]
    public void ImplicitConversion_FromWpfPoint_RoundsToNearestInt()
    {
        Point wpfPoint = new(1.6, 2.4);

        POINT result = wpfPoint;

        Assert.Equal(2, result.X);
        Assert.Equal(2, result.Y);
    }
}

public class SizeTests
{
    [Fact]
    public void ImplicitConversion_ToAndFromWpfSize_RoundTrips()
    {
        var size = new SIZE(800, 600);

        Size wpfSize = size;
        SIZE roundTripped = wpfSize;

        Assert.Equal(size.Width, roundTripped.Width);
        Assert.Equal(size.Height, roundTripped.Height);
    }

    [Fact]
    public void AddThickness_GrowsBothDimensions()
    {
        var size = new SIZE(100, 200);
        var thickness = new Thickness(1, 2, 3, 4);

        var result = size + thickness;

        Assert.Equal(104, result.Width);
        Assert.Equal(206, result.Height);
    }

    [Fact]
    public void SubtractThickness_ShrinksBothDimensions()
    {
        var size = new SIZE(104, 206);
        var thickness = new Thickness(1, 2, 3, 4);

        var result = size - thickness;

        Assert.Equal(100, result.Width);
        Assert.Equal(200, result.Height);
    }
}
