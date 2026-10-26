# Recreates the application icon without external artwork or conversion tools.
Add-Type -AssemblyName System.Drawing
$frames = @()
foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
    $bitmap = [Drawing.Bitmap]::new($size, $size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform(($size / 256.0), ($size / 256.0))
    $navy = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#102F50'))
    $gold = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#E3BA61'))
    $paper = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#F4F7FC'))
    $ink = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#53728E'), 10)
    $edge = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#E3BA61'), 7)
    $check = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#102F50'), 12)
    $check.StartCap = $check.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $path = [Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc(8, 8, 52, 52, 180, 90)
    $path.AddArc(196, 8, 52, 52, 270, 90)
    $path.AddArc(196, 196, 52, 52, 0, 90)
    $path.AddArc(8, 196, 52, 52, 90, 90)
    $path.CloseFigure()
    $graphics.FillPath($navy, $path)
    $graphics.DrawPath($edge, $path)
    $graphics.FillRectangle($paper, 53, 43, 132, 166)
    $graphics.FillRectangle($gold, 53, 43, 19, 166)
    $graphics.DrawLine($ink, 89, 78, 159, 78)
    $graphics.DrawLine($ink, 89, 106, 159, 106)
    $graphics.DrawLine($ink, 89, 134, 134, 134)
    $graphics.FillEllipse($gold, 134, 142, 89, 89)
    $graphics.DrawLines($check, [Drawing.PointF[]]@([Drawing.PointF]::new(155, 186), [Drawing.PointF]::new(172, 202), [Drawing.PointF]::new(204, 168)))
    $stream = [IO.MemoryStream]::new()
    $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
    $frames += ,@($size, $stream.ToArray())
    if ($size -eq 256) { $bitmap.Save((Join-Path $PSScriptRoot 'Hakedis.png'), [Drawing.Imaging.ImageFormat]::Png) }
    $stream.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    foreach ($resource in @($navy, $gold, $paper, $ink, $edge, $check, $path)) { $resource.Dispose() }
}
$output = [IO.File]::Create((Join-Path $PSScriptRoot 'Hakedis.ico'))
$writer = [IO.BinaryWriter]::new($output)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($frame in $frames) {
    $dimension = if ($frame[0] -eq 256) { 0 } else { $frame[0] }
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
    $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$frame[1].Length); $writer.Write([uint32]$offset)
    $offset += $frame[1].Length
}
foreach ($frame in $frames) { $writer.Write([byte[]]$frame[1]) }
$writer.Dispose()
