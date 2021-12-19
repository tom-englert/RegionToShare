using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

var sourceName = args[0];
var targetSize = int.Parse(args[1]);
var targetName = args[2];

using var sourceImage = Image.Load<Rgba32>(sourceName);
using var targetImage = new Image<Rgba32>(targetSize, targetSize);

var xOffset = (targetImage.Width - sourceImage.Width) / 2;
var yOffset = (targetImage.Height - sourceImage.Height) / 2;

for (var y = 0; y < sourceImage.Height; y++)
{
    var sourceRow = sourceImage.GetPixelRowSpan(y);
    var targetRow = targetImage.GetPixelRowSpan(y + yOffset);

    for (var x = 0; x < sourceImage.Width; x++) targetRow[x + xOffset] = sourceRow[x];
}

targetImage.SaveAsPng(targetName);