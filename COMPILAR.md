# Como gerar o executável

Guia para quando você mexer no código e precisar produzir um `CES.exe` novo.

## O que precisa estar instalado

Nada. O compilador de C# usado aqui faz parte do próprio Windows, em

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

Não é preciso Visual Studio, nem SDK do .NET, nem NuGet, nem conexão com a internet. Foi uma
decisão de projeto: qualquer máquina Windows atual compila este repositório sem preparo.

Por isso o projeto não tem arquivo `.csproj` e **não** se compila com `dotnet build`. Os scripts
chamam o compilador direto.

## O comando

Depois de salvar suas alterações, abra o PowerShell na pasta do repositório e rode:

```powershell
powershell -ExecutionPolicy Bypass -File build\gerar-tudo.ps1
```

É só isso. O script faz cinco coisas em sequência e para na primeira que der errado:

1. roda as verificações das regras de cálculo, da máscara e da configuração;
2. compila o executável;
3. confere se o tamanho ficou dentro do limite de 114.688 bytes;
4. audita o binário procurando capacidade sensível, e falha se achar alguma;
5. monta o `dist\CES-x.y.z.zip` com o programa e as instruções, e atualiza o `SEGURANCA.md`
   com a versão, o tamanho e a soma de verificação do arquivo recém-gerado.

No fim ele imprime o caminho do executável, o do pacote e o SHA-256. O pacote é o que vai para
quem for usar.

O `-ExecutionPolicy Bypass` está aí porque o Windows bloqueia scripts por padrão. Ele vale só
para aquela execução e não altera configuração nenhuma da máquina.

## Onde mexer para mudar cada coisa

Antes de recompilar: **cores e os textos do cabeçalho não precisam de compilação**. Ficam em
`%APPDATA%\CES\1.5.0\estilo.ini`, criado sozinho na primeira execução, numa pasta por versão. Veja
[PERSONALIZAR.md](PERSONALIZAR.md), que trata de aparência em detalhe.

O que está abaixo é o que só muda no código. Procure pelo nome indicado dentro do arquivo; são
todos nomes únicos no projeto.

| Quero mudar | Arquivo | Procure por |
|---|---|---|
| Cores padrão | `src/Tema.cs` | `Principal`, `Realce`, `Fundo` |
| Tons fixos do topo e das faixas | `src/JanelaPrincipal.cs`, `src/FaixaAviso.cs` | `AoPintarCabecalho`, `CriarBotao`, `OnPaint` |
| Tamanho das letras | `src/Tema.cs` | `Destaque`, `Titulo`, `Corpo` |
| Horários que aparecem por padrão | `src/Preferencias.cs` | `Entrada`, `SaidaAlmoco`, `VoltaAlmoco` |
| Jornada padrão | `src/Preferencias.cs` | `Jornada` |
| Intervalo mínimo de almoço | `src/Preferencias.cs` | `IntervaloMinimo` |
| Os marcos de 15, 10 e 5 minutos | `src/JanelaPrincipal.cs` | `faltam <= 15` |
| Quanto tempo o alarme toca | `src/FaixaAviso.cs` | `DuracaoPadrao` |
| Pausa entre os toques | `src/Alarme.cs` | `IntervaloAviso`, `IntervaloAlarme` |
| Som de cada sinal | `src/Alarme.cs` | `SystemSounds` |
| Tamanho e posição das coisas na janela | `src/JanelaPrincipal.cs` | `Largura`, `Altura` |
| Limites do redimensionamento | `src/JanelaPrincipal.cs` | `ZoomMinimo`, `ZoomMaximo` |
| Corpo de cada fonte | `src/Tema.cs` | `Aplicar` |
| Texto do topo, padrão de fábrica (hoje "Nome da sua área ou equipe") | `src/Estilo.cs` | `Assinatura`, `Origem` |
| Textos da janela | `src/JanelaPrincipal.cs` | `CriarCampos`, `CriarRodape` |
| Textos dos avisos | `src/JanelaPrincipal.cs` | `FaixaAviso.Mostrar` |
| Tolerância do alarme atrasado | `src/JanelaPrincipal.cs` | `TimeSpan.FromMinutes(2)` |
| A fórmula do cálculo | `src/Calculo.cs` | `Avaliar` |
| A regra da digitação sem os dois pontos | `src/Mascara.cs` | `Dividir` |
| O desenho do ícone | `build/gerar-icone.ps1` | `New-Desenho` |
| O número da versão | `src/AssemblyInfo.cs` | `AssemblyVersion` |
| As instruções que vão junto do programa | `docs/COMO-USAR.txt`, `docs/COMO-AJUSTAR.txt` | |

O som e a faixa de aviso têm a mesma duração: o toque começa quando a faixa aparece e para
quando ela sai, seja pelo tempo esgotado ou por um clique. Por isso o tempo do alarme se ajusta
em `DuracaoPadrao`, no `FaixaAviso.cs`, e não no `Alarme.cs`.

### Como o tamanho funciona

Os números em `JanelaPrincipal.cs` não são pixels. São unidades de um desenho pensado para tela
de 96 pontos por polegada, que passam pela função `P()` antes de virar pixel. `P()` multiplica
por dois fatores, ambos em `src/Escala.cs`:

- a escala da tela do usuário, que resolve monitores em 125% ou 150%;
- o zoom que o usuário escolhe arrastando o canto inferior direito da janela.

Todo o layout é recalculado em `AplicarLayout`, que é o único lugar do código que define posição
e tamanho de controle. Quem muda o zoom chama esse método de novo, e nada mais precisa saber
que o tamanho mudou.

As fontes seguem a mesma ideia em `Tema.Aplicar`: o corpo declarado ali é multiplicado pelo
zoom, e as fontes antigas são descartadas uma geração depois, quando já não há controle usando
nenhuma delas.

Se for mexer em posição, mexa no número, não na chamada de `P()`. Somar pixel direto faz aquele
pedaço deixar de acompanhar o resto quando a janela muda de tamanho.

A caixa de marcação dos avisos é desenhada pelo programa, em `src/CaixaOpcao.cs`, e não pela do
Windows. O motivo é esse mesmo: a do sistema tem tamanho fixo e não acompanharia o zoom.

Se criar um arquivo `.cs` novo, basta salvá-lo em `src\`. O script de compilação pega todos os
arquivos da pasta, não há lista para atualizar. Salve em UTF-8, que é o que o compilador espera.

## Se você mexer no cálculo, na máscara, nos avisos ou na configuração

As verificações em `testes\Verificacao.cs` vão acusar. É proposital: elas travam o valor
esperado de cada caso, então mudar a regra sem atualizar o teste faz a geração parar na primeira
etapa.

Cobrem `Calculo.cs`, `Mascara.cs`, `Marcos.cs` (quando cada aviso dispara) e `Ini.cs` com
`Preferencias.cs` (a leitura dos dois arquivos `.ini`). São os arquivos que compilam fora da
janela; quem depender de `Form` não entra, e é por isso que a decisão dos avisos foi separada
do desenho deles.

Quando a mudança for intencional, ajuste o valor esperado no teste junto com o código. Quando
não for, você acabou de descobrir um erro antes de ele chegar em alguém.

Para rodar só as verificações, sem compilar:

```powershell
powershell -ExecutionPolicy Bypass -File testes\verificar.ps1
```

## Se você mexer nos sons

Não dá para conferir som olhando. Rode:

```powershell
powershell -ExecutionPolicy Bypass -File testes\ouvir-alarmes.ps1
```

Toca os dois sinais na hora, sem esperar horário nenhum.

## Trocar o número da versão

Antes de gerar uma versão para distribuir, atualize `src\AssemblyInfo.cs`:

```csharp
[assembly: AssemblyVersion("1.5.1.0")]
[assembly: AssemblyFileVersion("1.5.1.0")]
```

As duas linhas precisam ter o mesmo valor. O nome do pacote sai da versão do executável, então
`1.5.1.0` produz `CES-1.5.1.zip` sem você precisar digitar isso em lugar nenhum, e a pasta de
configuração do usuário passa a ser `%APPDATA%\CES\1.5.1`, começando limpa.

Há outros dois lugares, e esses não são automáticos: a primeira linha de `docs\COMO-USAR.txt` e a
de `docs\COMO-AJUSTAR.txt`, que vão junto com o executável no pacote. O `SEGURANCA.md` se atualiza sozinho na geração, mas o
`COMO-USAR.txt` não, e já ficou anunciando 1.0.0 com o programa em 1.2.2 nas mãos de quem usa.

A convenção é a de sempre: o terceiro número sobe em correção, o segundo em funcionalidade nova,
o primeiro em mudança grande. Registre o que mudou no `CHANGELOG.md`.

## Quando algo dá errado

**"O CES está aberto. Feche o programa e rode de novo."**
O Windows tranca o arquivo enquanto o programa roda. Feche a janela do CES e repita.

**"Ha verificacoes falhando."**
Alguma regra de cálculo ou de digitação mudou de comportamento. A saída da primeira etapa mostra
qual caso falhou, com o valor esperado e o obtido.

**"Executavel acima do limite estabelecido."**
O binário passou de 114.688 bytes, que é o teto adotado no projeto, quatro vezes o tamanho do
programa de 2009. Hoje ele usa menos de um terço disso, então se estourar é porque entrou algo
grande, provavelmente um arquivo embutido. Vale entender antes de simplesmente aumentar o limite
em `build\compilar.ps1`.

**"A auditoria encontrou capacidade sensivel no binario."**
Você usou alguma API de rede, registro, execução de programas ou similar. Isso muda o que consta
no `SEGURANCA.md` e precisa de decisão consciente, não de contornar a checagem.

**Acentos saindo errados na janela**
O arquivo `.cs` foi salvo em outra codificação. Salve em UTF-8.

**Alterei o código e nada mudou no programa**
Você provavelmente abriu um `CES.exe` antigo, de outra pasta. Confira com:

```powershell
Get-Process CES | Select-Object Path
```

Lembrando que abrir o programa uma segunda vez não cria janela nova: ele traz de volta a que já
estava aberta, mesmo que seja de outra versão.

## Os scripts, um por um

Se preferir rodar as etapas separadas em vez do `gerar-tudo.ps1`:

| Script | Para quê |
|---|---|
| `testes\verificar.ps1` | roda as verificações de cálculo, máscara, avisos e configuração |
| `build\compilar.ps1` | gera `dist\CES.exe` e confere o tamanho |
| `testes\auditar.ps1` | lista tudo o que o binário pode chamar |
| `build\empacotar.ps1` | monta o zip com o programa e as instruções |
| `testes\reproduzir.ps1` | prova que um `CES.exe` saiu deste código-fonte |
| `testes\ouvir-alarmes.ps1` | toca os dois sinais sonoros |
| `build\gerar-icone.ps1` | redesenha `build\ces.ico` |
| `build\gerar-tudo.ps1` | verificação, compilação, auditoria e pacote, parando no primeiro erro |

O `reproduzir.ps1` fica de fora do `gerar-tudo.ps1` de propósito: recém-gerado, o executável
obviamente confere com o código, e a conferência não diria nada. Ele serve depois, para quem
recebeu o arquivo e quer confirmar de onde veio, com `-Exe caminho\CES.exe`.

O `gerar-icone.ps1` só precisa rodar se você mexer no desenho do ícone. O `.ico` fica versionado
justamente para a geração normal não depender dele.

## Depois de gerar

Registre o que mudou e guarde o ponto:

```powershell
git add -A
git commit -m "descricao curta do que mudou"
git push origin main
```

Se for uma versão para distribuir, marque e publique o pacote na página de releases:

```powershell
git tag -a v1.5.1 -m "descricao da versao"
git push origin v1.5.1
gh release create v1.5.1 dist\CES-1.5.1.zip --title "CES 1.5.1" --notes "descricao da versao"
```

O último comando usa o [GitHub CLI](https://cli.github.com/); sem ele, a release se cria pela
página do repositório, em **Releases**, anexando o mesmo zip.

E lembre que o `SEGURANCA.md` foi reescrito pela geração, com a soma de verificação do arquivo
novo. Ele precisa ir junto no commit, e é a versão dele que acompanha o executável quando a
área de tecnologia for conferir.
