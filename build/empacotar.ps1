# Monta o pacote de entrega: executavel mais instrucoes, em um unico zip.
#
# Uso:  powershell -ExecutionPolicy Bypass -File build\empacotar.ps1

$ErrorActionPreference = 'Stop'

$raiz = Split-Path $PSScriptRoot -Parent
$exe = Join-Path $raiz 'dist\CES.exe'
$instrucoes = @(
    (Join-Path $raiz 'docs\COMO-USAR.txt')
    (Join-Path $raiz 'docs\COMO-AJUSTAR.txt')
)

if (-not (Test-Path $exe)) {
    & (Join-Path $PSScriptRoot 'compilar.ps1')
}

foreach ($doc in $instrucoes) {
    if (-not (Test-Path $doc)) {
        throw "Arquivo nao encontrado: $doc"
    }
}

$versao = (Get-Item $exe).VersionInfo.FileVersion
$versao = ($versao -split '\.')[0..2] -join '.'

$pacote = Join-Path $raiz "dist\CES-$versao.zip"
$temporario = Join-Path $env:TEMP "ces-pacote-$versao"

if (Test-Path $temporario) { Remove-Item $temporario -Recurse -Force }
New-Item -ItemType Directory -Path $temporario | Out-Null

Copy-Item $exe $temporario
foreach ($doc in $instrucoes) { Copy-Item $doc $temporario }

if (Test-Path $pacote) { Remove-Item $pacote -Force }
Compress-Archive -Path (Join-Path $temporario '*') -DestinationPath $pacote -CompressionLevel Optimal

Remove-Item $temporario -Recurse -Force

$info = Get-Item $pacote
Write-Output ''
Write-Output ("Pacote : " + $pacote)
Write-Output ("Tamanho: " + $info.Length + " bytes (" + [math]::Round($info.Length / 1KB, 1) + " KB)")
Write-Output ''
Write-Output 'Conteudo:'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($pacote)
foreach ($item in $zip.Entries) {
    Write-Output ("  " + $item.Name + "  (" + $item.Length + " bytes)")
}
$zip.Dispose()
