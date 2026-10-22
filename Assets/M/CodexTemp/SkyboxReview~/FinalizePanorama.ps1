$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assetPath = [IO.Path]::GetFullPath('D:/project/ProjectWait/Assets/M/Arts/Textures/Skybox_Storybook_City_1024.png')
$reviewPath = [IO.Path]::GetFullPath('D:/project/ProjectWait/Assets/M/CodexTemp/SkyboxCityReview')
$scopePath = [IO.Path]::GetFullPath('D:/project/ProjectWait/Assets/M/')
if (!$assetPath.StartsWith($scopePath, [StringComparison]::OrdinalIgnoreCase) -or !$reviewPath.StartsWith($scopePath, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'All output must remain inside Assets/M.'
}
$sourceStream = New-Object IO.MemoryStream(,[IO.File]::ReadAllBytes($assetPath))
$source = [Drawing.Bitmap]::FromStream($sourceStream)
$bitmap = $source.Clone((New-Object Drawing.Rectangle(0, 0, $source.Width, $source.Height)), [Drawing.Imaging.PixelFormat]::Format24bppRgb)
$source.Dispose()
$strip = $null
$enlarged = $null
$graphics = $null
try {
    if ($bitmap.Width -ne 1024 -or $bitmap.Height -ne 512) { throw 'Expected 1024 x 512.' }
    $edgeWidth = 16
    $beforeMaximum = 0
    $beforeSum = 0
    $afterMaximum = 0
    $changedPixels = 0
    $maximumCorrection = 0
    $rowReports = @()
    for ($row = 0; $row -lt $bitmap.Height; $row++) {
        $left = $bitmap.GetPixel(0, $row)
        $right = $bitmap.GetPixel($bitmap.Width - 1, $row)
        $leftValues = @([int]$left.R, [int]$left.G, [int]$left.B)
        $rightValues = @([int]$right.R, [int]$right.G, [int]$right.B)
        $common = @(0, 0, 0)
        for ($channel = 0; $channel -lt 3; $channel++) {
            $common[$channel] = [int][Math]::Round(($leftValues[$channel] + $rightValues[$channel]) / 2.0)
            $difference = [Math]::Abs($leftValues[$channel] - $rightValues[$channel])
            $beforeMaximum = [Math]::Max($beforeMaximum, $difference)
            $beforeSum += $difference
        }
        for ($offset = 0; $offset -lt $edgeWidth; $offset++) {
            # A cosine taper preserves the inner image and smooths the export boundary.
            $weight = 0.5 * (1 + [Math]::Cos([Math]::PI * $offset / ($edgeWidth - 1)))
            foreach ($side in @(0, 1)) {
                $column = if ($side -eq 0) { $offset } else { $bitmap.Width - 1 - $offset }
                $original = $bitmap.GetPixel($column, $row)
                $values = @([int]$original.R, [int]$original.G, [int]$original.B)
                $edge = if ($side -eq 0) { $leftValues } else { $rightValues }
                $corrected = @(0, 0, 0)
                $pixelChanged = $false
                for ($channel = 0; $channel -lt 3; $channel++) {
                    $corrected[$channel] = [int][Math]::Max(0, [Math]::Min(255, [Math]::Round($values[$channel] + $weight * ($common[$channel] - $edge[$channel]))))
                    $correction = [Math]::Abs($corrected[$channel] - $values[$channel])
                    $maximumCorrection = [Math]::Max($maximumCorrection, $correction)
                    if ($correction -ne 0) { $pixelChanged = $true }
                }
                if ($pixelChanged) { $changedPixels++ }
                $bitmap.SetPixel($column, $row, [Drawing.Color]::FromArgb($corrected[0], $corrected[1], $corrected[2]))
            }
        }
        $newLeft = $bitmap.GetPixel(0, $row)
        $newRight = $bitmap.GetPixel($bitmap.Width - 1, $row)
        $delta = @([Math]::Abs([int]$newLeft.R - [int]$newRight.R), [Math]::Abs([int]$newLeft.G - [int]$newRight.G), [Math]::Abs([int]$newLeft.B - [int]$newRight.B))
        $peak = ($delta | Measure-Object -Maximum).Maximum
        $afterMaximum = [Math]::Max($afterMaximum, $peak)
        $rowReports += [pscustomobject]@{ y = $row; left = @($newLeft.R, $newLeft.G, $newLeft.B); right = @($newRight.R, $newRight.G, $newRight.B); difference = $delta }
    }
    if ($afterMaximum -ne 0) { throw 'Edge equality check failed.' }
    $encoded = New-Object IO.MemoryStream
    try {
        $bitmap.Save($encoded, [Drawing.Imaging.ImageFormat]::Png)
        [IO.File]::WriteAllBytes($assetPath, $encoded.ToArray())
    } finally { $encoded.Dispose() }

    # Show the actual wrap: rightmost 64 pixels followed by leftmost 64 pixels.
    $strip = New-Object Drawing.Bitmap(128, 512, [Drawing.Imaging.PixelFormat]::Format24bppRgb)
    for ($row = 0; $row -lt 512; $row++) {
        for ($column = 0; $column -lt 128; $column++) {
            $sampleX = if ($column -lt 64) { 960 + $column } else { $column - 64 }
            $strip.SetPixel($column, $row, $bitmap.GetPixel($sampleX, $row))
        }
    }
    $enlarged = New-Object Drawing.Bitmap(512, 2048, [Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [Drawing.Graphics]::FromImage($enlarged)
    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.DrawImage($strip, (New-Object Drawing.Rectangle(0, 0, 512, 2048)))
    $enlarged.Save((Join-Path $reviewPath 'WrapSeam_4x.png'), [Drawing.Imaging.ImageFormat]::Png)
    $report = [pscustomobject]@{
        path = 'Assets/M/Arts/Textures/Skybox_Storybook_City_1024.png'
        width = 1024; height = 512
        method = 'Built-in image_gen artwork; deterministic export edge normalization over 16 columns on each side with a cosine taper. Center 992 columns unchanged.'
        beforeMaxChannelDifference = $beforeMaximum
        beforeMeanAbsoluteChannelDifference = $beforeSum / (512 * 3)
        afterMaxChannelDifference = $afterMaximum
        checkedRows = 512; checkedRgbChannels = 1536; exactMatchingRows = 512
        changedPixels = $changedPixels; maximumPixelChannelCorrection = $maximumCorrection
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $assetPath).Hash
        perRow = $rowReports
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $reviewPath 'SeamValidation.json')
    $report | Select-Object path,width,height,beforeMaxChannelDifference,afterMaxChannelDifference,checkedRows,checkedRgbChannels,exactMatchingRows,changedPixels,maximumPixelChannelCorrection,sha256 | ConvertTo-Json
} finally {
    if ($null -ne $graphics) { $graphics.Dispose() }
    if ($null -ne $enlarged) { $enlarged.Dispose() }
    if ($null -ne $strip) { $strip.Dispose() }
    $bitmap.Dispose()
    $sourceStream.Dispose()
}
