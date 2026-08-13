using System.Drawing.Imaging;

namespace LookUp;

/// <summary>
/// Grabs the whole virtual desktop (all monitors) into a single bitmap and crops it.
/// The process runs System-DPI-aware, so <see cref="SystemInformation.VirtualScreen"/>
/// and the overlay window share one coordinate space — selection maps 1:1 to pixels.
/// </summary>
internal static class ScreenCapture
{
    public static Bitmap CaptureVirtualScreen()
    {
        var bounds = SystemInformation.VirtualScreen;
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    public static Bitmap Crop(Bitmap source, Rectangle region)
    {
        region.Intersect(new Rectangle(0, 0, source.Width, source.Height));
        if (region.Width <= 0 || region.Height <= 0)
            region = new Rectangle(0, 0, 1, 1);

        var cropped = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(cropped);
        g.DrawImage(source,
            new Rectangle(0, 0, region.Width, region.Height),
            region,
            GraphicsUnit.Pixel);
        return cropped;
    }
}
