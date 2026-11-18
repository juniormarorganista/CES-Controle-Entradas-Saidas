# Compila o CES em um unico executavel.
#
# Requisito: .NET Framework 4.8, que ja acompanha o Windows 10 (1903+) e o
# Windows 11. O compilador usado faz parte do proprio sistema, nao e preciso
# instalar SDK nenhum.
#
# Uso:  powershell -ExecutionPolicy Bypass -File build\compilar.ps1
#       powershell -ExecutionPolicy Bypass -File build\compilar.ps1 -Destino caminho\CES.exe
#
# O parametro -Destino existe para o testes\reproduzir.ps1 poder compilar em
# pasta temporaria com exatamente estas opcoes. Manter as opcoes num lugar so
# e o que faz a comparacao de IL valer alguma coisa.

param([string]$Destino)

$ErrorActionPreference = 'Stop'

$raiz = Split-Path $PSScriptRoot -Parent
$saida = Join-Path $raiz 'dist'
$padrao = -not $Destino
$exe = if ($Destino) { $Destino } else { Join-Path $saida 'CES.exe' }
$icone = Join-Path $PSScriptRoot 'ces.ico'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) {
    throw 'Compilador C# do .NET Framework 4.x nao encontrado neste computador.'
}

if (-not (Test-Path $icone)) {
    & (Join-Path $PSScriptRoot 'gerar-icone.ps1')
}

# O Windows trava o arquivo enquanto o programa esta aberto, e o erro que
# aparece nesse caso nao diz o motivo. So vale para o destino padrao: em
# pasta temporaria nao ha arquivo travado para atrapalhar.
if ($padrao) {
    $aberto = @(Get-Process -Name 'CES' -ErrorAction SilentlyContinue)
    if ($aberto.Count -gt 0) {
        throw 'O CES esta aberto. Feche o programa e rode de novo.'
    }
}

New-Item -ItemType Directory -Force -Path (Split-Path $exe -Parent) | Out-Null
if (Test-Path $exe) { Remove-Item $exe -Force }

$fontes = Get-ChildItem (Join-Path $raiz 'src') -Filter *.cs | ForEach-Object { $_.FullName }

$argumentos = @(
    '/nologo'
    '/target:winexe'
    '/platform:anycpu'
    '/optimize+'
    '/debug-'
    '/codepage:65001'
    '/warnaserror-'
    "/out:$exe"
    "/win32icon:$icone"
    '/reference:System.dll'
    '/reference:System.Drawing.dll'
    '/reference:System.Windows.Forms.dll'
) + $fontes

& $csc $argumentos
if ($LASTEXITCODE -ne 0) { throw "Falha na compilacao (codigo $LASTEXITCODE)." }

$info = Get-Item $exe
$limite = 114688   # quatro vezes o tamanho do programa original

Write-Output ''
Write-Output ("Gerado : " + $exe)
Write-Output ("Tamanho: " + $info.Length + " bytes (" + [math]::Round($info.Length / 1KB, 1) + " KB)")
Write-Output ("Limite : " + $limite + " bytes")

if ($info.Length -gt $limite) {
    Write-Warning 'Executavel acima do limite estabelecido.'
    exit 1
}

Write-Output 'Dentro do limite.'
