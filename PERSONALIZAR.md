# Como mudar a aparência do CES

Há dois caminhos. O primeiro não exige compilar nada e cobre o que muda com mais frequência:
cores e os textos do topo da janela. O segundo mexe no código e cobre o resto.

## Resumo: o que trocar primeiro

Quem adota o CES numa equipe normalmente faz três ajustes, todos no `estilo.ini`, sem compilar:

| Ajuste | Chave | Valor padrão |
|---|---|---|
| Nome da área, equipe ou empresa | `origem` | `Nome da sua área ou equipe` |
| Título ao lado da sigla CES | `assinatura` | `Controle de Entradas e Saídas` |
| Cores da identidade visual | `principal` e `realce` | `#003641` e `#00AE9D` |

O valor padrão de `origem` é um texto de exemplo: ele existe para ser trocado. Os outros dois
podem ficar como estão.

## Sem compilar: o arquivo de estilo

O programa lê a aparência de

```
%APPDATA%\CES\1.5.0\estilo.ini
```

A pasta tem o número da versão em uso: cada versão do CES lê e escreve na sua, e nenhuma
começa herdando ajuste feito em outra. Ao atualizar o programa, a personalização precisa ser
refeita ou copiada da pasta anterior.

Para chegar lá: pressione `Win + R`, digite `%APPDATA%\CES` e pressione Enter.

O arquivo é criado sozinho na primeira execução, já com todos os valores atuais e comentários
explicando cada um. Edite no Bloco de Notas, salve, feche o CES e abra de novo: o arquivo só é
lido na abertura.

Se uma linha estiver errada, o programa ignora aquela linha e usa o valor padrão dela, sem
travar. Linha com o valor vazio também volta ao padrão. Para voltar tudo ao original, apague o
arquivo: ele é recriado na próxima abertura.

### Os textos do topo

```ini
assinatura=Controle de Entradas e Saídas
origem=Nome da sua área ou equipe
```

`assinatura` é a primeira linha, ao lado da sigla CES. `origem` é a segunda, menor e mais
apagada, onde vai o nome da área, da equipe ou da empresa. Exemplos do que costuma entrar ali:

```ini
origem=Equipe de Suporte - Unidade Centro
origem=Financeiro - Contas a Pagar
origem=Escritório Silva & Associados
```

Cuidado com o comprimento. A primeira linha divide espaço com a sigla e com os botões de
minimizar e fechar: no tamanho padrão sobram cerca de 230 pixels, o que dá umas 38 letras.
A segunda linha tem a largura toda. Se o texto passar disso, ele fica por baixo dos botões.
Nesse caso, ou encurta, ou aumenta a janela arrastando o canto.

### As cores

Todas no formato `#RRGGBB`, o mesmo do HTML.

| Chave | Onde aparece |
|---|---|
| `principal` | barra de cima, borda da janela, número da saída prevista, riscos da alça de tamanho, fundo das faixas de aviso |
| `realce` | risco à esquerda da barra e do cartão, contorno do campo em foco, caixa marcada |
| `fundo` | fundo da janela, atrás dos rótulos |
| `cartao` | fundo branco dos campos e do cartão de resultado |
| `borda` | contorno dos campos em repouso |
| `textoForte` | texto digitado nos campos e o texto ao lado da caixa de marcação |
| `textoSuave` | rótulos, contagem regressiva e a linha de manhã e intervalo |
| `textoClaro` | sigla CES na barra escura, título das faixas de aviso e o sinal de marcado |
| `atencao` | contagem nos últimos 15 minutos, e avisos de inconsistência |
| `critico` | saída prevista e contagem depois de a jornada ser ultrapassada |

Repare que `principal` e `textoForte` têm o mesmo valor por padrão, mas são coisas diferentes:
o primeiro é superfície, o segundo é texto. Trocar só um dos dois é o que você quer quando a
barra muda de cor mas o texto dos horários precisa continuar escuro.

As chaves descrevem o papel de cada cor, não a cor em si. Até a versão 1.4.0 as duas primeiras
se chamavam `verdeEscuro` e `turquesa`; um `estilo.ini` dessa época precisa ter essas duas
linhas renomeadas, senão elas são ignoradas e as cores voltam ao padrão.

### Um exemplo completo

É a paleta da segunda imagem do [README](README.md):

```ini
assinatura=Controle de Entradas e Saídas
origem=Equipe de Suporte - Unidade Centro

principal=#23395B
realce=#E9B44C
fundo=#F5F6F9
cartao=#FFFFFF
borda=#D6DBE4
textoForte=#23395B
textoSuave=#5E6B7D
textoClaro=#FFFFFF
atencao=#D9480F
critico=#B3261E
```

### Escolhendo cores que funcionam

Duas regras bastam para o resultado não ficar ruim.

A primeira é contraste: `textoClaro` precisa ser bem mais claro que `principal`, senão o
cabeçalho fica ilegível; `textoForte` e `textoSuave` precisam ser bem mais escuros que `cartao`
e que `fundo`. Na dúvida, mantenha `textoClaro` branco e `cartao` branco.

A segunda é que `atencao` e `critico` precisam se destacar do resto. São as duas cores que
avisam que o horário está chegando ou já passou; se elas se parecerem com `textoSuave` ou com
`realce`, o aviso deixa de funcionar.

### O que o arquivo não cobre

Alguns tons do topo da janela e das faixas de aviso estão fixos no código, em tons
acinzentados que combinam com fundo escuro: o texto das duas linhas do topo, os botões de
minimizar e fechar e o texto menor das faixas. Com `principal` escuro, como no padrão e no
exemplo acima, eles continuam legíveis. Com `principal` claro, ficam apagados; nesse caso o
ajuste é no código (tabela abaixo, linha "Tons fixos do topo e das faixas").

## Compilando: o que está no código

Para o resto, edite o arquivo indicado e gere o executável de novo com

```powershell
powershell -ExecutionPolicy Bypass -File build\gerar-tudo.ps1
```

O passo a passo completo está em [COMPILAR.md](COMPILAR.md).

| Quero mudar | Arquivo | Procure por |
|---|---|---|
| Texto padrão do topo, o que vale sem `estilo.ini` | `src/Estilo.cs` | `Assinatura`, `Origem` |
| Cores padrão, o que vale sem `estilo.ini` | `src/Tema.cs` | `Principal`, `Realce` |
| Tons fixos do topo e das faixas | `src/JanelaPrincipal.cs`, `src/FaixaAviso.cs` | `AoPintarCabecalho`, `CriarBotao`, `OnPaint` |
| Corpo de cada fonte | `src/Tema.cs` | `Aplicar` |
| Tamanho padrão da janela | `src/JanelaPrincipal.cs` | `Largura`, `Altura` |
| Posição de cada elemento | `src/JanelaPrincipal.cs` | `TopoRotulos`, `TopoCampos`, `TopoResultado`, `TopoRodape`, `TopoOpcao` |
| Largura das colunas de horário | `src/JanelaPrincipal.cs` | `LarguraColuna`, `Vao` |
| Altura da barra de cima | `src/JanelaPrincipal.cs` | `AlturaCabecalho` |
| Limites do redimensionamento | `src/JanelaPrincipal.cs` | `ZoomMinimo`, `ZoomMaximo` |
| Nomes dos quatro campos | `src/JanelaPrincipal.cs` | `CriarCampos` |
| Texto da opção de avisos | `src/JanelaPrincipal.cs` | `_rotuloOpcao` |
| Textos dos avisos que aparecem | `src/JanelaPrincipal.cs` | `FaixaAviso.Mostrar` |
| Tamanho e tempo da faixa de aviso | `src/FaixaAviso.cs` | `DuracaoPadrao`, `Size` |
| Desenho da caixa de marcação | `src/CaixaOpcao.cs` | `OnPaint` |
| Desenho do ícone | `build/gerar-icone.ps1` | `New-Desenho` |

Mudar o texto ou as cores padrão no código vale para quem ainda não tem `estilo.ini`. Para quem
já abriu o programa, o arquivo vence o código. Por isso uma versão nova, que usa pasta nova,
é o caminho para distribuir um padrão diferente a uma equipe inteira.

### Sobre as medidas

Os números em `JanelaPrincipal.cs` não são pixels. São unidades de um desenho pensado para tela
de 96 pontos por polegada, que passam pela função `P()` antes de virar pixel. `P()` aplica dois
fatores: a escala da tela do usuário e o zoom escolhido por ele arrastando o canto da janela.

Na prática isso significa que você mexe no número e o layout continua certo em qualquer tela e
em qualquer tamanho de janela. O que não pode é somar pixel direto sem passar por `P()`, senão
aquele pedaço deixa de acompanhar o resto.

As fontes seguem a mesma lógica em `Tema.Aplicar`: o corpo declarado ali é multiplicado pelo
zoom. Mudar `16F` para `18F` no `Destaque` aumenta o horário da saída em todos os tamanhos.

## Testando o que você mudou

Depois de editar o `estilo.ini`, feche e abra o programa. Depois de editar o código, gere o
executável e abra.

Vale conferir os dois extremos de tamanho, porque cor e fonte que funcionam no tamanho padrão
podem não funcionar nos outros: arraste o canto até o mínimo, que é 60% do padrão, e até o
máximo, que é o dobro.

Se for baixar o `ZoomMinimo`, confira os rótulos das colunas. Abaixo de 60% a fonte deixa de
encolher proporcionalmente, o texto passa a ocupar mais espaço relativo do que a coluna tem, e
"SAÍDA ALM." aparece como "SAÍDA". Foi por isso que o mínimo ficou onde está. Se precisar de
uma janela menor que isso, encurte antes os rótulos em `CriarCampos`, no `JanelaPrincipal.cs`.

Para ver os avisos sem esperar o horário, coloque a jornada de forma que a saída caia uns
minutos à frente. Por exemplo, com entrada `00:00`, saída e volta do almoço iguais, a saída
prevista é igual à jornada: basta pôr na jornada um horário 14 minutos à frente do relógio para
o aviso dos 15 minutos disparar na hora.
