$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'assets'
New-Item -ItemType Directory -Force $dir | Out-Null
function Draw-Shield($g,[float]$x,[float]$y,[float]$scale){
 $points=[Drawing.PointF[]]@([Drawing.PointF]::new($x,$y-30*$scale),[Drawing.PointF]::new($x+25*$scale,$y-20*$scale),[Drawing.PointF]::new($x+21*$scale,$y+12*$scale),[Drawing.PointF]::new($x,$y+32*$scale),[Drawing.PointF]::new($x-21*$scale,$y+12*$scale),[Drawing.PointF]::new($x-25*$scale,$y-20*$scale))
 $pen=[Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#4AD7BD'),3*$scale)
 $g.DrawPolygon($pen,$points)
 $g.DrawLines($pen,[Drawing.PointF[]]@([Drawing.PointF]::new($x-10*$scale,$y),[Drawing.PointF]::new($x-2*$scale,$y+8*$scale),[Drawing.PointF]::new($x+12*$scale,$y-9*$scale)))
 $pen.Dispose()
}
$bitmap=[Drawing.Bitmap]::new(164,314)
$g=[Drawing.Graphics]::FromImage($bitmap)
$g.SmoothingMode='AntiAlias'
$g.Clear([Drawing.ColorTranslator]::FromHtml('#0C121E'))
Draw-Shield $g 82 90 1.4
$font=[Drawing.Font]::new('Segoe UI',16,[Drawing.FontStyle]::Bold)
$small=[Drawing.Font]::new('Segoe UI',8)
$brush=[Drawing.SolidBrush]::new([Drawing.Color]::White)
$muted=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#90A2B9'))
$g.DrawString("MultiVPN`nConnect",$font,$brush,17,155)
$g.DrawString("Your network.`nYour connection.",$small,$muted,19,226)
$bitmap.Save((Join-Path $dir 'welcome.bmp'),[Drawing.Imaging.ImageFormat]::Bmp)
$g.Dispose();$bitmap.Dispose()
$bitmap=[Drawing.Bitmap]::new(150,57);$g=[Drawing.Graphics]::FromImage($bitmap);$g.Clear([Drawing.Color]::White);$g.SmoothingMode='AntiAlias';Draw-Shield $g 116 28 0.6;$bitmap.Save((Join-Path $dir 'header.bmp'),[Drawing.Imaging.ImageFormat]::Bmp);$g.Dispose();$bitmap.Dispose()
# Supply native images for small taskbar icons, high DPI displays, and Explorer.
$frames=@()
foreach($size in @(16,20,24,32,40,48,64,128,256)) {
 $bitmap=[Drawing.Bitmap]::new($size,$size)
 $g=[Drawing.Graphics]::FromImage($bitmap)
 $g.SmoothingMode='AntiAlias'
 $g.Clear([Drawing.ColorTranslator]::FromHtml('#0C121E'))
 Draw-Shield $g ($size/2) ($size*0.48) ($size/80)
 $memory=[IO.MemoryStream]::new()
 $bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
 $frames+=,@{Size=$size;Bytes=$memory.ToArray()}
 $memory.Dispose();$g.Dispose();$bitmap.Dispose()
}
$stream=[IO.File]::Create((Join-Path $dir 'app.ico'))
$writer=[IO.BinaryWriter]::new($stream)
$writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$frames.Count)
$offset=6+16*$frames.Count
foreach($frame in $frames){$dimension=if($frame.Size -eq 256){0}else{$frame.Size};$writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frame.Bytes.Length);$writer.Write([uint32]$offset);$offset+=$frame.Bytes.Length}
foreach($frame in $frames){$writer.Write([byte[]]$frame.Bytes)}
$writer.Dispose();$font.Dispose();$small.Dispose();$brush.Dispose();$muted.Dispose()
