Add-Type -AssemblyName System.Drawing
$bitmap = New-Object System.Drawing.Bitmap 64,64
$g = [System.Drawing.Graphics]::FromImage($bitmap)
$g.SmoothingMode = 'AntiAlias'
$g.Clear([System.Drawing.Color]::Transparent)
function Polygon($color, $coords) {
    $points = [System.Drawing.Point[]]@($coords | ForEach-Object { [System.Drawing.Point]::new($_[0],$_[1]) })
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($color))
    $g.FillPolygon($brush,$points)
    $brush.Dispose()
}
Polygon '#B97935' @(@(5,39),@(57,39),@(57,47),@(10,57),@(5,52))
Polygon '#F6CA72' @(@(5,38),@(47,18),@(59,43),@(11,53))
Polygon '#F05234' @(@(5,34),@(48,15),@(59,37),@(12,48))
Polygon '#70A54B' @(@(4,29),@(47,10),@(61,32),@(53,39),@(43,36),@(33,44),@(24,40),@(12,46))
Polygon '#E7A743' @(@(4,25),@(46,5),@(61,28),@(61,33),@(13,44),@(4,35))
Polygon '#FFE0A0' @(@(5,25),@(45,7),@(58,28),@(13,38))
$g.Dispose()
$bitmap.Save((Join-Path $PSScriptRoot 'sandwich.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$stream = [IO.MemoryStream]::new()
$bitmap.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
$bytes=$stream.ToArray()
$file=[IO.File]::Create((Join-Path $PSScriptRoot 'sandwich.ico'))
$writer=[IO.BinaryWriter]::new($file)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
$writer.Write([byte]64); $writer.Write([byte]64); $writer.Write([byte]0); $writer.Write([byte]0)
$writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$bytes.Length); $writer.Write([uint32]22)
$writer.Write($bytes); $writer.Dispose(); $stream.Dispose(); $bitmap.Dispose()
