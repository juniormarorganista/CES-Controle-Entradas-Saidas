# Compila e executa a verificacao das regras de calculo e da mascara.
#
# Uso:  powershell -ExecutionPolicy Bypass -File testes\verificar.ps1

$ErrorActionPreference = 'Stop'

$raiz = Split-Path $PSScriptRoot -Parent
$temporario = Join-Path $env:TEMP 'ces-verificacao.exe'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) {
    throw 'Compilador C# do .NET Framework 4.x nao encontrado neste computador.'
}

# Apenas os arquivos sem dependencia de interface. Todos compilam fora da
# janela, que e o que permite verifica-los aqui.
$fontes = @(
    (Join-Path $raiz 'src\Calculo.cs')
    (Join-Path $raiz 'src\Mascara.cs')
    (Join-Path $raiz 'src\Marcos.cs')
    (Join-Path $raiz 'src\Ini.cs')
    (Join-Path $raiz 'src\Versao.cs')
    (Join-Path $raiz 'src\Preferencias.cs')
    (Join-Path $PSScriptRoot 'Verificacao.cs')
)

& $csc '/nologo' '/target:exe' '/optimize+' '/codepage:65001' "/out:$temporario" `
    '/reference:System.dll' '/reference:System.Drawing.dll' $fontes
if ($LASTEXITCODE -ne 0) { throw "Falha ao compilar a verificacao (codigo $LASTEXITCODE)." }

& $temporario
$codigo = $LASTEXITCODE

Remove-Item $temporario -Force -ErrorAction SilentlyContinue

if ($codigo -ne 0) { throw 'Ha verificacoes falhando.' }
