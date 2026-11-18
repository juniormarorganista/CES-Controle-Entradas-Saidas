# Histórico de versões

## 1.5.0

Primeira versão publicada em repositório aberto.

O texto padrão da segunda linha do topo passou a ser **"Nome da sua área ou equipe"**, um
exemplo feito para ser trocado. A documentação ganhou, no README, uma seção dizendo o que cada
pessoa pode personalizar e onde, e o `PERSONALIZAR.md` abre com um resumo dos três ajustes que
uma equipe costuma fazer primeiro.

As duas primeiras cores do `estilo.ini` mudaram de nome, de `verdeEscuro` e `turquesa` para
`principal` e `realce`. Os nomes antigos descreviam a cor, e deixavam de fazer sentido assim que
alguém trocava a paleta. Os valores padrão continuam os mesmos. Um `estilo.ini` copiado de
versão anterior precisa ter as duas linhas renomeadas; sem isso elas são ignoradas e as duas
cores voltam ao padrão.

Os comentários que o programa escreve dentro do `estilo.ini` passaram a ter acentuação e a
explicar cada um dos dois textos do topo. Era a pendência registrada para a próxima geração de
binário.

A mensagem de erro inesperado aponta a página do projeto no GitHub, e as instruções que vão no
pacote dizem onde relatar problema.

O botão de minimizar passou a usar o sinal de menos no lugar da meia-risca; o desenho na tela é
o mesmo.

## 1.4.0

O pacote de entrega passou a levar um segundo arquivo de instruções, `COMO-AJUSTAR.txt`,
escrito para quem tem apenas o executável: como trocar as cores, os textos do topo e o intervalo
mínimo, passo a passo, sem acesso ao código. O executável não mudou, então a versão continua a
mesma e a soma de verificação do `SEGURANCA.md` segue valendo.

O produto passou a se chamar **Controle de Entradas e Saídas**. A sigla CES continua servindo,
agora como Controle de Entradas e Saídas. O nome novo aparece na barra de tarefas, nas
propriedades do arquivo, na confirmação de fechamento e no cabeçalho da janela.

As configurações passaram a ficar em uma pasta com o número da versão:

```
%APPDATA%\CES\1.4.0\preferencias.ini
%APPDATA%\CES\1.4.0\estilo.ini
```

Cada versão lê e escreve na própria pasta. Uma versão nova nunca começa herdando ajuste feito em
outra, e voltar para uma anterior encontra a pasta dela intacta.

Isso era necessário para o próprio nome mudar de fato: quem já tinha `estilo.ini` continuaria
vendo o nome antigo, porque o arquivo vence o padrão do código. Vale para qualquer mudança de
aparência ou de texto padrão daqui em diante.

Os arquivos soltos das versões anteriores continuam onde estavam, sem uso. Podem ser apagados à
mão.

O efeito colateral é que atualizar o programa pede refazer o que estava personalizado, ou copiar
os arquivos da pasta anterior antes de abrir.

## 1.3.0

O alarme do almoço e do fim da jornada passou a sair pelo esquema de sons do Windows. Antes era
um tom gerado direto, pelo caminho do bipe de sistema, que na maioria das máquinas não obedece
ao volume: os quatorze segundos da faixa de saída não tinham como ser abaixados nem
silenciados. Agora acompanham o volume como qualquer outro programa, e o som em si pode ser
trocado no Painel de Controle.

A contagem dos minutos continua com uma batida e o alarme passou a ter duas, para os dois
seguirem distinguíveis sem olhar para a tela. O ritmo mudou um pouco: o som do sistema é
assíncrono, então a pausa entre as batidas é o que dá o compasso, e o ciclo ficou mais espaçado
que o anterior.

Nada mais mudou de comportamento. O que segue é por dentro.

A decisão de quando cada aviso dispara saiu da janela e virou `src/Marcos.cs`. Eram métodos
privados do formulário, embaralhados com a caixa de seleção, os campos e a faixa; é o código
com mais estado do projeto e era o único que não tinha como ser verificado. A janela ficou com
o texto, a cor e o som de cada aviso. Mesmo princípio que `Calculo` já seguia.

A leitura dos dois arquivos `.ini` virou `src/Ini.cs`, uma cópia só em vez de duas. As duas
tinham divergido: a de estilo pulava linhas de comentário e a de preferências não, o que só não
dava problema porque o comentário dela não tem sinal de igual.

A verificação foi de 85 para 189 casos, cobrindo os marcos de aviso e a leitura dos dois
arquivos de configuração, que antes não tinham nenhum. Nenhum caso lê o relógio nem o disco.

Erro inesperado agora aparece como aviso em português e encerra o programa, em vez de caixa de
erro crua do .NET. Nada é gravado em disco por causa disso: registro de erro em arquivo abriria
um terceiro caminho de escrita.

Documentação: a caixa de seleção era descrita como desligando os três avisos, com o alarme da
saída como exceção; os dois do almoço também são exceção, e isso não estava escrito. A
tolerância de dois minutos era descrita só para quem abre o programa tarde, quando explica
igualmente a máquina que estava suspensa na hora da saída.

Novo `testes/reproduzir.ps1`, que recompila o código-fonte e compara o código intermediário
método a método com um executável entregue. É o passo que faltava para demonstrar que o
binário distribuído veio deste repositório, coisa que a soma de verificação sozinha não mostra.

## 1.2.2

O limite mínimo de redimensionamento subiu de 50% para 60% do tamanho padrão, onde os rótulos
das colunas ainda cabem inteiros. Em 50% eles apareciam como "SAÍDA" e "VOLTA", sem o "ALM.",
porque a fonte deixa de encolher proporcionalmente em corpos muito pequenos.

O máximo continua em 200%.

## 1.2.1

A alça de redimensionamento, no canto inferior direito, estava desenhada na cor de contorno dos
campos e quase não aparecia. Passou para o verde escuro da paleta, o mesmo da barra de cima.
Como sai do arquivo de estilo, acompanha quem trocar a paleta.

O limite mínimo de redimensionamento caiu de 80% para 50% do tamanho padrão. O máximo continua
em 200%.

Vale saber: em 50% os rótulos das colunas do meio aparecem como "SAÍDA" e "VOLTA", sem o "ALM.".
Não é corte de layout, é a fonte que deixa de encolher proporcionalmente nesses corpos muito
pequenos. O resto da tela continua inteiro e os horários continuam legíveis.

## 1.2.0

As cores e os textos do cabeçalho saíram do código e passaram para
`%APPDATA%\CES\estilo.ini`. Dá para trocar a paleta inteira e o nome da área editando um arquivo
de texto e reabrindo o programa, sem compilar nada.

O arquivo é criado sozinho na primeira execução, já com os valores atuais e um comentário
explicando cada chave. Linha errada é ignorada e usa o padrão dela, então arquivo estragado não
impede o programa de abrir. Apagar o arquivo devolve a aparência original.

Documentação nova em `PERSONALIZAR.md`, com o que cada cor pinta na tela, o limite de tamanho
dos textos do cabeçalho e a lista do que continua só no código.

## 1.1.0

A janela pode ser aumentada e diminuída arrastando o canto inferior direito, onde há três
risquinhos. As letras acompanham a mesma proporção, então quem achar a fonte pequena resolve
puxando o canto. O tamanho de hoje continua sendo o padrão, e o escolhido fica guardado.

A proporção entre largura e altura é mantida, então a janela não se deforma. O limite vai de
80% a 200% do tamanho padrão.

Por dentro, o layout deixou de ser fixo. Todas as medidas e fontes passaram a ser recalculadas
a partir de um fator de zoom, num único ponto do código, e as fontes são refeitas a cada
mudança em vez de ficarem constantes.

A caixa de marcação dos avisos passou a ser desenhada pelo programa. A do Windows tem tamanho
fixo, definido pelo tema do sistema, e não acompanharia o redimensionamento.

## 1.0.4

O toque agora acompanha a faixa de aviso. Começa quando ela aparece e só para quando ela sai,
seja pelo tempo esgotado ou por um clique. O som e a pausa entre as repetições continuam os
mesmos; o que mudou é que a quantidade de repetições deixou de ser fixa em três e passou a
depender do tempo em que a faixa fica na tela.

O cartão de resultado foi reorganizado. O horário de saída saiu de baixo do rótulo e passou a
ficar ao lado dele, antes da contagem regressiva, e a fonte caiu de 25 para 16 pontos, atendendo
a quem achou o número grande demais. A janela ficou 32 pixels mais baixa.

A contagem passou a mostrar "< 1min" no lugar de "menos de 1min", que não cabia no espaço novo.

## 1.0.3

O cabeçalho passou a identificar a área: "Calculadora de Entrada e Saída" na primeira linha e
o nome da área na segunda.

O texto completo precisaria de 387 pixels e o cabeçalho tem 227 livres antes dos botões. Numa
linha só, só caberia alargando a janela em quase metade, o que passaria do limite de tamanho
adotado no projeto. Em duas linhas a janela cresceu 12 pixels de altura e nada mais.

## 1.0.2

A caixa que liga e desliga os avisos ocupava a largura inteira da janela. Clicar em qualquer
ponto daquela linha, inclusive no texto e no espaço vazio à direita dele, alternava a opção sem
querer.

Agora a caixa tem o tamanho do próprio quadrado e o texto virou um rótulo comum, que não
responde a clique. A tecla de espaço continua alternando quando a caixa está selecionada.

## 1.0.1

O aviso sonoro dos 15, 10 e 5 minutos tocava uma vez só. Agora toca três vezes, com pausa entre
elas. A pausa é necessária porque o som do sistema é assíncrono: chamado de novo antes de
terminar, ele interrompe o anterior em vez de somar.

O alarme dos horários de almoço e de saída já repetia três vezes e continua igual. As duas
contagens passaram a usar a mesma constante.

## 1.0.0

Primeira versão do CES, substituindo um programa de 2009 que fazia o mesmo cálculo, sem legenda
nos campos e sem aviso nenhum.

O cálculo da saída é o mesmo do programa anterior. Mudou tudo em volta dele.

Adicionado:

- legenda em cada um dos quatro campos, incluindo o de jornada, que no programa antigo não
  tinha identificação e era confundido com um segundo horário de entrada;
- botões de minimizar e fechar, com confirmação antes de encerrar;
- avisos aos 15, 10 e 5 minutos antes do fim da jornada, em faixa que some sozinha e não rouba
  o foco, com caixa de seleção para desligá-los;
- alarme sonoro no horário da saída, na hora de sair para o almoço e na hora de voltar;
- contagem regressiva e indicação do tempo excedente quando a jornada é ultrapassada;
- jornada digitável, para atender contratos de 6h e de 8h;
- preenchimento automático dos dois pontos na digitação dos horários;
- ajuste de minuto pelas setas do teclado;
- validação da sequência dos horários, com aviso quando o intervalo fica abaixo do mínimo;
- memória dos horários do dia, da jornada e da posição da janela;
- instância única: abrir o programa de novo restaura a janela existente;
- suporte a telas com escala de 125% ou mais, sem o texto borrado do redimensionamento
  automático do Windows.

Mantido:

- a janela continua sempre visível sobre as outras;
- o tamanho da janela continua pequeno o suficiente para ficar aberta o dia todo.
