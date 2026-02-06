
Add-Type -AssemblyName System.Drawing

function Create-Icon($fileName, $size, $fontSize) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    
    $greenBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Green)
    $g.FillEllipse($greenBrush, 0, 0, $size, $size)
    
    $font = New-Object System.Drawing.Font("Arial", $fontSize, [System.Drawing.FontStyle]::Bold)
    $yellowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Yellow)
    
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    
    $rect = New-Object System.Drawing.RectangleF 0, 0, $size, $size
    $g.DrawString("MB", $font, $yellowBrush, $rect, $format)
    
    $path = Join-Path (Join-Path $PSScriptRoot "..\AppPackage") $fileName
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "Created $path"
    
    $g.Dispose()
    $bmp.Dispose()
}

Create-Icon "color.png" 192 80
Create-Icon "outline.png" 32 12
