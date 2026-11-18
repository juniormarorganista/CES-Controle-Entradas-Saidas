# Executa o caminho completo, na ordem certa, parando no primeiro erro:
# verificacao das regras, compilacao, auditoria do binario e empacotamento.
#
# E o comando unico para gerar uma versao nova depois de mexer no codigo.
#
# Uso:  powershell -ExecutionPolicy Bypass -File build\gerar-tudo.ps1

$ErrorActionPreference = 'Stop'

$raiz = Split-Path $PSScriptRoot -Parent
$etapa = 0

function Etapa([string]$titulo) {
    $script:etapa++
    Write-Output ''
    Write-Output ('=' * 62)
    Write-Output ("  $script:etapa. $titulo")
    Write-Output ('=' * 62)
}

function Executar([string]$script, [string]$falha) {
    & powershell -ExecutionPolicy Bypass -NoProfile -File (Join-Path $raiz $script)
    if ($LASTEXITCODE -ne 0) {
        throw $falha
    }
}

Etapa 'Verificacao das regras de calculo e da mascara'
Executar 'testes\verificar.ps1' 'Ha verificacoes falhando. Corrija antes de gerar o executavel.'

Etapa 'Compilacao'
Executar 'build\compilar.ps1' 'A compilacao falhou.'

Etapa 'Auditoria do binario'
$auditoria = & powershell -ExecutionPolicy Bypass -NoProfile -File (Join-Path $raiz 'testes\auditar.ps1')
if ($LASTEXITCODE -ne 0) {
    $auditoria | Write-Output
    throw 'A auditoria encontrou capacidade sensivel no binario.'
}
$auditoria | Write-Output

$totalMembros = 0
$linhaTotal = $auditoria | Select-String -Pattern '^\s*total:\s*(\d+)' | Select-Object -First 1
if ($linhaTotal) { $totalMembros = [int]$linhaTotal.Matches[0].Groups[1].Value }

Etapa 'Empacotamento'
Executar 'build\empacotar.ps1' 'O empacotamento falhou.'

$exe = Join-Path $raiz 'dist\CES.exe'
$info = Get-Item $exe
$versao = $info.VersionInfo.FileVersion
$versaoCurta = ($versao -split '\.')[0..2] -join '.'
$hash = (Get-FileHash $exe -Algorithm SHA256).Hash
$tamanho = $info.Length.ToString('N0', [System.Globalization.CultureInfo]::GetCultureInfo('pt-BR'))

Etapa 'Atualizacao do documento de seguranca'

# O compilador grava um identificador novo em cada build, entao a soma de
# verificacao muda mesmo sem alteracao no codigo. Deixar o documento
# desatualizado invalidaria a conferencia feita pela area de tecnologia.
$doc = Join-Path $raiz 'SEGURANCA.md'
$texto = [System.IO.File]::ReadAllText($doc, [System.Text.Encoding]::UTF8)
$antes = $texto

$texto = [regex]::Replace($texto, '(?m)^\| Versão \| .*$', '| Versão | ' + $versao + ' |')
$texto = [regex]::Replace($texto, '(?m)^\| Tamanho \| .*$', '| Tamanho | ' + $tamanho + ' bytes |')
$texto = [regex]::Replace($texto, '(?m)^\| SHA-256 \| .*$', '| SHA-256 | `' + $hash + '` |')

# Os numeros citados no texto tambem precisam acompanhar o codigo.
if ($totalMembros -gt 0) {
    $texto = [regex]::Replace($texto, 'os \d+ membros externos', 'os ' + $totalMembros + ' membros externos')
}

$linhas = 0
Get-ChildItem (Join-Path $raiz 'src') -Filter *.cs | ForEach-Object {
    $linhas += (Get-Content $_.FullName | Measure-Object -Line).Lines
}
$arredondado = [math]::Round($linhas / 100.0) * 100
$texto = [regex]::Replace($texto, 'cerca de [\d\.]+ linhas de C#',
    'cerca de ' + $arredondado.ToString('N0', [System.Globalization.CultureInfo]::GetCultureInfo('pt-BR')) + ' linhas de C#')

if ($texto -ne $antes) {
    [System.IO.File]::WriteAllText($doc, $texto, (New-Object System.Text.UTF8Encoding($false)))
    Write-Output '  SEGURANCA.md atualizado com a versao, o tamanho e a soma de verificacao.'
}
else {
    Write-Output '  SEGURANCA.md ja estava atualizado.'
}

Write-Output ''
Write-Output ('=' * 62)
Write-Output '  Tudo pronto'
Write-Output ('=' * 62)
Write-Output ''
Write-Output ('Versao  : ' + $versaoCurta)
Write-Output ('Programa: ' + $exe)
Write-Output ('Pacote  : ' + (Join-Path $raiz "dist\CES-$versaoCurta.zip"))
Write-Output ('SHA-256 : ' + $hash)
