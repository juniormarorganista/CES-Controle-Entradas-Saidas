# Toca os dois sinais sonoros do programa, para conferir de ouvido sem
# precisar esperar o horario chegar. Serve tambem para diagnosticar maquina
# de colega em que o alarme nao esteja saindo.
#
# Uso:  powershell -ExecutionPolicy Bypass -File testes\ouvir-alarmes.ps1

$ErrorActionPreference = 'Stop'

$raiz = Split-Path $PSScriptRoot -Parent
$temporario = Join-Path $env:TEMP 'ces-som.exe'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) {
    throw 'Compilador C# do .NET Framework 4.x nao encontrado neste computador.'
}

$programa = @'
using System;
using System.Diagnostics;
using System.Threading;

namespace Ces
{
    internal static class Som
    {
        private static void Main()
        {
            // Na aplicacao o toque dura o tempo em que a faixa fica na tela.
            // Aqui a duracao e simulada pelas esperas abaixo.
            Console.WriteLine("Aviso dos 15, 10 e 5 minutos, por 9 segundos.");
            Stopwatch t = Stopwatch.StartNew();
            Alarme.Repetir(Sinal.Aviso);
            Console.WriteLine("  chamada retornou em " + t.ElapsedMilliseconds + " ms");
            Thread.Sleep(9000);
            Alarme.Parar();
            Console.WriteLine("  parado apos " + t.ElapsedMilliseconds + " ms");
            Thread.Sleep(1500);

            Console.WriteLine();
            Console.WriteLine("Alarme de almoco e de saida, por 9 segundos.");
            t.Restart();
            Alarme.Repetir(Sinal.Alarme);
            Console.WriteLine("  chamada retornou em " + t.ElapsedMilliseconds + " ms");
            Thread.Sleep(9000);
            Alarme.Parar();
            Console.WriteLine("  parado apos " + t.ElapsedMilliseconds + " ms");
            Thread.Sleep(1500);

            Console.WriteLine();
            Console.WriteLine("Se nao saiu som nenhum, verifique o volume do sistema");
            Console.WriteLine("e o esquema de sons do Windows.");
        }
    }
}
'@

$fonte = Join-Path $env:TEMP 'ces-som.cs'
[System.IO.File]::WriteAllText($fonte, $programa, (New-Object System.Text.UTF8Encoding($false)))

& $csc '/nologo' '/target:exe' '/optimize+' '/codepage:65001' "/out:$temporario" `
    (Join-Path $raiz 'src\Alarme.cs') $fonte
if ($LASTEXITCODE -ne 0) { throw "Falha ao compilar (codigo $LASTEXITCODE)." }

& $temporario

Remove-Item $temporario, $fonte -Force -ErrorAction SilentlyContinue
