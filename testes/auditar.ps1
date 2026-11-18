# Lista tudo o que o executavel e capaz de chamar: assemblies, tipos e
# membros externos, chamadas a codigo nativo e recursos embutidos.
#
# Serve para conferencia independente. Qualquer pessoa com o arquivo pode
# rodar e verificar as afirmacoes do SEGURANCA.md sem precisar confiar nelas.
#
# Uso:  powershell -ExecutionPolicy Bypass -File testes\auditar.ps1
#       powershell -ExecutionPolicy Bypass -File testes\auditar.ps1 -Exe caminho\CES.exe

param([string]$Exe)

$ErrorActionPreference = 'Stop'

if (-not $Exe) {
    $Exe = Join-Path (Split-Path $PSScriptRoot -Parent) 'dist\CES.exe'
}
if (-not (Test-Path $Exe)) {
    throw "Executavel nao encontrado: $Exe"
}

$Exe = (Resolve-Path $Exe).Path

Write-Output '=== IDENTIDADE DO ARQUIVO ==='
$fi = Get-Item $Exe
Write-Output ('Arquivo : ' + $fi.FullName)
Write-Output ('Tamanho : ' + $fi.Length + ' bytes')
Write-Output ('SHA-256 : ' + (Get-FileHash $Exe -Algorithm SHA256).Hash)
Write-Output ('Produto : ' + $fi.VersionInfo.ProductName)
Write-Output ('Versao  : ' + $fi.VersionInfo.FileVersion)
Write-Output ('Empresa : ' + $fi.VersionInfo.CompanyName)
Write-Output ''

# Contexto de reflexao pura: le os metadados sem dar ao binario nenhuma
# chance de executar. Importa porque este script existe para ser apontado
# para um arquivo em que ainda nao se confia.
[System.AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({
    param($remetente, $evento)
    return [System.Reflection.Assembly]::ReflectionOnlyLoad($evento.Name)
})

$asm = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($Exe)
$mod = $asm.GetModules()[0]

Write-Output '=== ASSEMBLIES REFERENCIADOS ==='
foreach ($r in $asm.GetReferencedAssemblies()) {
    Write-Output ('  ' + $r.Name + ' ' + $r.Version)
}
Write-Output ''

Write-Output '=== MEMBROS EXTERNOS QUE O PROGRAMA PODE CHAMAR ==='
$membros = New-Object System.Collections.Generic.SortedSet[string]
for ($i = 1; $i -lt 8192; $i++) {
    try { $m = $mod.ResolveMember(0x0A000000 -bor $i) } catch { continue }
    if ($null -eq $m) { continue }
    $dono = if ($m.DeclaringType) { $m.DeclaringType.FullName } else { '?' }
    [void]$membros.Add(($dono -replace ', mscorlib.*?\]', ']') + '::' + $m.Name)
}
foreach ($m in $membros) { Write-Output ('  ' + $m) }
Write-Output ('  total: ' + $membros.Count)
Write-Output ''

Write-Output '=== CHAMADAS A CODIGO NATIVO (DllImport) ==='
$flags = [System.Reflection.BindingFlags]'Public,NonPublic,Static,Instance,DeclaredOnly'
foreach ($t in $asm.GetTypes()) {
    foreach ($m in $t.GetMethods($flags)) {
        if ($m.Attributes -band [System.Reflection.MethodAttributes]::PinvokeImpl) {
            # Em reflexao pura o atributo nao e instanciado: le-se o dado bruto.
            $nome = '?'
            foreach ($d in [System.Reflection.CustomAttributeData]::GetCustomAttributes($m)) {
                if ($d.AttributeType.Name -eq 'DllImportAttribute') {
                    $nome = $d.ConstructorArguments[0].Value
                }
            }
            Write-Output ('  ' + $nome + ' -> ' + $m.Name)
        }
    }
}
Write-Output ''

Write-Output '=== RECURSOS EMBUTIDOS ==='
$res = $asm.GetManifestResourceNames()
if ($res.Count -eq 0) { Write-Output '  nenhum' } else { $res | ForEach-Object { Write-Output ('  ' + $_) } }
Write-Output ''

Write-Output '=== BUSCA POR CAPACIDADES SENSIVEIS ==='
$suspeitos = @(
    'System.Net',
    'Microsoft.Win32.Registry',
    'System.Diagnostics.Process',
    'System.Reflection.Assembly::Load',
    'System.Windows.Forms.Clipboard',
    'System.Security.Cryptography',
    'System.IO.Directory::GetFiles',
    'System.IO.DriveInfo',
    'SetWindowsHookEx',
    'GetAsyncKeyState',
    'BitBlt',
    'CreateProcess',
    'WinExec',
    'URLDownloadToFile'
)

$achou = $false
foreach ($s in $suspeitos) {
    $hits = @($membros | Where-Object { $_ -like ('*' + $s + '*') })
    if ($hits.Count -gt 0) {
        $achou = $true
        Write-Output ('  ENCONTRADO: ' + $s)
        $hits | ForEach-Object { Write-Output ('     ' + $_) }
    }
    else {
        Write-Output ('  ausente: ' + $s)
    }
}

Write-Output ''
if ($achou) {
    Write-Output 'Ha capacidades sensiveis referenciadas. Revisar antes de distribuir.'
    exit 1
}

Write-Output 'Nenhuma capacidade sensivel referenciada no binario.'
