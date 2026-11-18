# Gera build\ces.ico a partir de desenho vetorial, nas resolucoes 16, 32 e 48.
# Rodar apenas quando o desenho do icone mudar; o .ico fica versionado.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$destino = Join-Path $PSScriptRoot 'ces.ico'
$tamanhos = @(16, 32, 48)

$verde  = [System.Drawing.Color]::FromArgb(255, 0x00, 0x36, 0x41)
$turq   = [System.Drawing.Color]::FromArgb(255, 0x00, 0xAE, 0x9D)
$branco = [System.Drawing.Color]::FromArgb(255, 0xFF, 0xFF, 0xFF)

function New-Desenho([int]$t) {
    $bmp = New-Object System.Drawing.Bitmap($t, $t, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Disco de fundo
    $fundo = New-Object System.Drawing.SolidBrush($verde)
    $g.FillEllipse($fundo, 0.0, 0.0, [float]($t - 1), [float]($t - 1))

    # Aro turquesa
    $esp = [Math]::Max(1.0, $t * 0.085)
    $inset = $t * 0.13
    $caneta = New-Object System.Drawing.Pen($turq, [float]$esp)
    $g.DrawEllipse($caneta, [float]$inset, [float]$inset, [float]($t - 1 - 2 * $inset), [float]($t - 1 - 2 * $inset))

    # Ponteiros
    $cx = ($t - 1) / 2.0
    $cy = ($t - 1) / 2.0
    $ponteiro = New-Object System.Drawing.Pen($branco, [float][Math]::Max(1.0, $t * 0.075))
    $ponteiro.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $ponteiro.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    # Minutos para cima, horas para a direita
    $g.DrawLine($ponteiro, [float]$cx, [float]$cy, [float]$cx, [float]($cy - $t * 0.26))
    $g.DrawLine($ponteiro, [float]$cx, [float]$cy, [float]($cx + $t * 0.19), [float]$cy)

    $miolo = New-Object System.Drawing.SolidBrush($turq)
    $rm = [Math]::Max(0.8, $t * 0.055)
    $g.FillEllipse($miolo, [float]($cx - $rm), [float]($cy - $rm), [float](2 * $rm), [float](2 * $rm))

    $fundo.Dispose(); $caneta.Dispose(); $ponteiro.Dispose(); $miolo.Dispose(); $g.Dispose()
    return $bmp
}

$blocos = @()
foreach ($t in $tamanhos) {
    $bmp = New-Desenho $t
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $blocos += , $ms.ToArray()
    $ms.Dispose(); $bmp.Dispose()
}

$fs = [System.IO.File]::Create($destino)
$bw = New-Object System.IO.BinaryWriter($fs)

$bw.Write([UInt16]0)                    # reservado
$bw.Write([UInt16]1)                    # tipo: icone
$bw.Write([UInt16]$tamanhos.Count)

$deslocamento = 6 + (16 * $tamanhos.Count)
for ($i = 0; $i -lt $tamanhos.Count; $i++) {
    $bw.Write([Byte]$tamanhos[$i])
    $bw.Write([Byte]$tamanhos[$i])
    $bw.Write([Byte]0)                  # cores da paleta
    $bw.Write([Byte]0)                  # reservado
    $bw.Write([UInt16]1)                # planos
    $bw.Write([UInt16]32)               # bits por pixel
    $bw.Write([UInt32]$blocos[$i].Length)
    $bw.Write([UInt32]$deslocamento)
    $deslocamento += $blocos[$i].Length
}

foreach ($b in $blocos) { $bw.Write($b) }

$bw.Close(); $fs.Close()

$info = Get-Item $destino
Write-Output ("ces.ico gerado: " + $info.Length + " bytes")
