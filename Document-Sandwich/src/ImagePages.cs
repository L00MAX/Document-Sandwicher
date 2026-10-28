using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace DocumentSandwich;
public static class InputTypes
{
    public const string Filter = "Supported files|*.pdf;*.docx;*.doc;*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|PDF files|*.pdf|Word documents|*.docx;*.doc|Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff";
    public static bool IsImage(string file) => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff" }.Contains(Path.GetExtension(file).ToLowerInvariant());
    public static bool IsSupported(string file) => IsImage(file) || new[] { ".pdf", ".docx", ".doc" }.Contains(Path.GetExtension(file).ToLowerInvariant());
}
public static class ImagePages
{
    public static void Add(PdfDocument output, string file)
    {
        try
        {
            using var source = File.OpenRead(file);
            var decoder = BitmapDecoder.Create(source, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            // TIFF may contain scanned pages; animated GIFs use their first frame.
            var frames = decoder is TiffBitmapDecoder ? decoder.Frames.ToArray() : [decoder.Frames[0]];
            foreach (var frame in frames)
            {
                BitmapSource bitmap = frame;
                var orientation = 1;
                try
                {
                    if (frame.Metadata is BitmapMetadata metadata && metadata.ContainsQuery("/app1/ifd/{ushort=274}"))
                        orientation = Convert.ToInt32(metadata.GetQuery("/app1/ifd/{ushort=274}"));
                }
                catch (NotSupportedException) { }
                var transform = new TransformGroup();
                if (orientation is 2 or 4 or 5 or 7) transform.Children.Add(new ScaleTransform(-1,1));
                var angle = orientation switch { 3 or 4 => 180, 5 or 6 => 90, 7 or 8 => 270, _ => 0 };
                if (angle != 0) transform.Children.Add(new RotateTransform(angle));
                if (transform.Children.Count > 0) bitmap = new TransformedBitmap(bitmap,transform);
                // Normalize Windows-supported formats to PNG before embedding in PDFsharp.
                var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
                using var stream = new MemoryStream(); png.Save(stream); stream.Position = 0;
                using var image = XImage.FromStream(stream);
                var page = output.AddPage(); page.Size = PageSize.A4;
                if (bitmap.PixelWidth > bitmap.PixelHeight) page.Orientation = PageOrientation.Landscape;
                using var graphics = XGraphics.FromPdfPage(page);
                var width = page.Width.Point; var height = page.Height.Point;
                graphics.DrawRectangle(XBrushes.White,0,0,width,height);
                const double margin = 24;
                var scale = Math.Min((width-2*margin)/bitmap.PixelWidth,(height-2*margin)/bitmap.PixelHeight);
                var drawWidth = bitmap.PixelWidth*scale; var drawHeight = bitmap.PixelHeight*scale;
                graphics.DrawImage(image,(width-drawWidth)/2,(height-drawHeight)/2,drawWidth,drawHeight);
            }
        }
        catch (Exception ex) { throw new InvalidOperationException("Cannot read image " + Path.GetFileName(file) + ". It may be damaged or use an unsupported encoding. " + ex.Message,ex); }
    }
}
