# Prova que um CES.exe foi gerado a partir do codigo-fonte deste repositorio.
#
# A soma de verificacao sozinha nao demonstra isso. O compilador do .NET grava
# um identificador de modulo novo a cada compilacao, entao dois arquivos
# gerados do mesmo codigo tem SHA-256 diferente. Conferir o hash prova que o
# arquivo e o mesmo que alguem entregou; nao prova de onde ele veio.
#
# O que prova e o codigo intermediario. Este script recompila o src\ em pasta
# temporaria, com as mesmas opcoes de build\compilar.ps1, e compara o corpo de
# cada metodo dos dois arquivos. Corpos iguais em todos os metodos significa
# mesmo codigo executavel, independentemente do identificador de modulo.
#
# Uso:  powershell -ExecutionPolicy Bypass -File testes\reproduzir.ps1
#       powershell -ExecutionPolicy Bypass -File testes\reproduzir.ps1 -Exe caminho\CES.exe

param([string]$Exe)

$ErrorActionPreference = 'Stop'

$raiz = Split-Path $PSScriptRoot -Parent

if (-not $Exe) {
    $Exe = Join-Path $raiz 'dist\CES.exe'
}
if (-not (Test-Path $Exe)) {
    throw "Executavel nao encontrado: $Exe"
}
$Exe = (Resolve-Path $Exe).Path

$temporario = Join-Path $env:TEMP ('ces-reproduzir-' + [System.Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporario | Out-Null
$referencia = Join-Path $temporario 'CES.exe'

try {
    Write-Output '=== RECOMPILANDO O CODIGO-FONTE ==='
    Write-Output ('  Fonte : ' + (Join-Path $raiz 'src'))
    Write-Output ('  Saida : ' + $referencia)

    & powershell -ExecutionPolicy Bypass -NoProfile `
        -File (Join-Path $raiz 'build\compilar.ps1') -Destino $referencia | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao recompilar (codigo $LASTEXITCODE)."
    }
    Write-Output '  Recompilado.'
    Write-Output ''

    Write-Output '=== IDENTIDADE DOS DOIS ARQUIVOS ==='
    $a = Get-Item $Exe
    $b = Get-Item $referencia
    Write-Output ('  Analisado   : ' + $a.Length + ' bytes  ' + (Get-FileHash $Exe -Algorithm SHA256).Hash)
    Write-Output ('  Recompilado : ' + $b.Length + ' bytes  ' + (Get-FileHash $referencia -Algorithm SHA256).Hash)
    Write-Output '  As somas diferem por causa do identificador de modulo. E esperado.'
    Write-Output ''

    # Os dois arquivos tem a mesma identidade de assembly, e o .NET recusa
    # carregar a segunda copia de outro caminho dentro do mesmo appdomain.
    # Por isso cada lado e lido num processo proprio.
    #
    # A leitura e em reflexao pura: nenhum dos dois executa durante a
    # comparacao, o que importa quando o arquivo analisado veio de fora.
    $extrator = Join-Path $temporario 'extrair.ps1'
    $codigo = @'
param([string]$Caminho)

$ErrorActionPreference = 'Stop'

[System.AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({
    param($remetente, $evento)
    return [System.Reflection.Assembly]::ReflectionOnlyLoad($evento.Name)
})

$asm = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($Caminho)
$flags = [System.Reflection.BindingFlags]'Public,NonPublic,Static,Instance,DeclaredOnly'
$sha = [System.Security.Cryptography.SHA256]::Create()

foreach ($t in ($asm.GetModules()[0].GetTypes() | Sort-Object FullName)) {
    $membros = @($t.GetMethods($flags)) + @($t.GetConstructors($flags))
    foreach ($m in ($membros | Sort-Object Name)) {
        $corpo = $null
        try { $corpo = $m.GetMethodBody() } catch { }
        if ($null -eq $corpo) { continue }

        $il = $corpo.GetILAsByteArray()
        if ($null -eq $il) { continue }

        Write-Output ($t.FullName + '::' + $m.Name + ' | ' + $il.Length + ' | ' `
            + [System.BitConverter]::ToString($sha.ComputeHash($il)).Replace('-', ''))
    }
}
'@
    [System.IO.File]::WriteAllText($extrator, $codigo,
        (New-Object System.Text.UTF8Encoding($false)))

    Write-Output '=== COMPARACAO DO CODIGO INTERMEDIARIO ==='

    $ilAnalisado = @(& powershell -ExecutionPolicy Bypass -NoProfile -File $extrator -Caminho $Exe)
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao ler o executavel analisado.' }

    $ilReferencia = @(& powershell -ExecutionPolicy Bypass -NoProfile -File $extrator -Caminho $referencia)
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao ler o executavel recompilado.' }

    if ($ilAnalisado.Count -eq 0 -or $ilReferencia.Count -eq 0) {
        throw 'Nenhum metodo lido. O arquivo analisado e um executavel .NET?'
    }
    Write-Output ('  Metodos no analisado   : ' + $ilAnalisado.Count)
    Write-Output ('  Metodos no recompilado : ' + $ilReferencia.Count)

    $diferenca = Compare-Object $ilAnalisado $ilReferencia
    Write-Output ''

    if ($null -eq $diferenca) {
        Write-Output 'CONFERE: todos os metodos tem o mesmo codigo intermediario.'
        Write-Output 'O executavel analisado foi gerado a partir do codigo-fonte em src\.'
        exit 0
    }

    Write-Output 'NAO CONFERE. Metodos divergentes:'
    foreach ($d in $diferenca) {
        $lado = if ($d.SideIndicator -eq '<=') { 'so no analisado  ' } else { 'so no recompilado' }
        Write-Output ('  ' + $lado + '  ' + $d.InputObject)
    }
    Write-Output ''
    Write-Output 'O executavel analisado nao corresponde ao codigo-fonte deste repositorio.'
    exit 1
}
finally {
    Remove-Item $temporario -Recurse -Force -ErrorAction SilentlyContinue
}
