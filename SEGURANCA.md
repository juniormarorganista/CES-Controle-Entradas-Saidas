# Análise de segurança do CES

Documento para conferência por equipe de tecnologia ou segurança da informação antes de o
programa ser distribuído. Nada aqui pede confiança: cada afirmação vem acompanhada de como
verificá-la de forma independente.

## Arquivo analisado

| | |
|---|---|
| Nome | `CES.exe` |
| Versão | 1.5.0.0 |
| Tamanho | 46.592 bytes |
| SHA-256 | `4FB54B48EE27A6304B5C7E3887E9BF42CBCD1FCD0B866C87C1F2F393F8E08FA1` |
| Plataforma | .NET Framework 4.8, código gerenciado, sem ofuscação |
| Assinatura digital | não possui |

Confira o hash antes de qualquer coisa:

```powershell
Get-FileHash CES.exe -Algorithm SHA256
```

Se não bater com o valor acima, o arquivo não é este e a análise não se aplica.

Uma observação sobre isso: o compilador do .NET Framework grava um identificador novo de módulo
a cada compilação, então recompilar o mesmo código-fonte produz um arquivo com soma de
verificação diferente. A soma acima identifica o arquivo entregue, não o código-fonte. Quem
compilar a partir do repositório vai obter um valor diferente, e isso é esperado. O script
`build\gerar-tudo.ps1` reescreve os três campos da tabela acima a cada geração, para o documento
nunca descrever um arquivo que não é o distribuído.

## Como confirmar que o executável veio deste código-fonte

O hash acima identifica o arquivo, mas não diz de onde ele veio. Auditar o código-fonte não
significa nada se o executável entregue tiver sido compilado a partir de outra coisa, e é uma
lacuna que a soma de verificação sozinha não fecha, justamente por mudar a cada compilação.

O que fecha é o código intermediário. O script abaixo recompila o `src/` em pasta temporária,
com as mesmas opções de compilação usadas na geração, e compara o corpo de cada método dos dois
arquivos:

```powershell
powershell -ExecutionPolicy Bypass -File testes\reproduzir.ps1
```

Para conferir um executável recebido de outra pessoa, aponte para ele:

```powershell
powershell -ExecutionPolicy Bypass -File testes\reproduzir.ps1 -Exe caminho\CES.exe
```

Corpo idêntico em todos os métodos significa mesmo código executável, independentemente do
identificador de módulo. Termina com código de saída diferente de zero se algum método divergir,
e nesse caso lista quais. É esta verificação, e não o hash, que liga o binário distribuído ao
código publicado aqui.

## Como reproduzir a análise

Um executável .NET carrega, nos próprios metadados, a lista completa de tudo o que ele é capaz
de chamar. Não é amostragem nem heurística: se um método não está nessa lista, o programa não
tem como executá-lo. O script abaixo extrai essa lista:

```powershell
powershell -ExecutionPolicy Bypass -File testes\auditar.ps1
```

Ele imprime os assemblies referenciados, os 262 membros externos que o programa pode chamar, as
chamadas a código nativo, os recursos embutidos, e no fim procura por capacidades sensíveis.
Termina com código de saída diferente de zero se encontrar alguma.

O código-fonte completo, cerca de 2.400 linhas de C#, está em `src/` e compila com o compilador
que já acompanha o Windows.

## O que o programa não é capaz de fazer

Nenhum dos itens abaixo aparece nos metadados do executável, o que significa ausência de
capacidade e não ausência de uso.

| Capacidade | Situação |
|---|---|
| Acesso a rede (HTTP, sockets, DNS) | nenhuma referência a `System.Net` |
| Leitura ou escrita no registro | nenhuma referência a `Microsoft.Win32.Registry` |
| Iniciar outros programas | nenhuma referência a `System.Diagnostics.Process`, `CreateProcess` ou `WinExec` |
| Carregar código externo | nenhuma referência a `Assembly.Load` |
| Captura de teclado | nenhuma referência a `SetWindowsHookEx` ou `GetAsyncKeyState` |
| Captura de tela | nenhuma referência a `BitBlt` |
| Área de transferência | nenhuma referência a `Clipboard` |
| Criptografia | nenhuma referência a `System.Security.Cryptography` |
| Percorrer pastas ou unidades | nenhuma referência a `Directory.GetFiles` ou `DriveInfo` |
| Download de arquivos | nenhuma referência a `URLDownloadToFile` |

Também não há recursos embutidos no executável: nenhum arquivo, script ou dado escondido
dentro dele além do ícone da janela.

A lista de membros mostra `System.Reflection`, o que costuma chamar atenção em revisão. O uso é
restrito a três chamadas, em `src/Versao.cs`: `Assembly.GetExecutingAssembly`, `Assembly.GetName`
e `AssemblyName.Version`. Servem para o programa ler o próprio número de versão e separar as
configurações por versão. Reflexão que carrega código de fora seria `Assembly.Load`, e essa não
aparece.

## O que o programa faz com o disco

O acesso a arquivos do binário inteiro se resume a cinco chamadas:

```
System.IO.Path::Combine
System.IO.Directory::CreateDirectory
System.IO.File::Exists
System.IO.File::ReadAllLines
System.IO.File::WriteAllText
```

Os caminhos usados nelas são montados em dois lugares do código
([Preferencias.cs](src/Preferencias.cs) e [Estilo.cs](src/Estilo.cs)), ambos a partir de
`Environment.GetFolderPath(SpecialFolder.ApplicationData)`, resultando em

```
%APPDATA%\CES\1.5.0\preferencias.ini
%APPDATA%\CES\1.5.0\estilo.ini
```

A última parte do caminho é a versão do executável em uso, obtida dos metadados do proprio
assembly. Serve para cada versão manter a sua configuração separada.

O primeiro guarda os horários do dia e as preferências de janela. O segundo guarda as cores e os
textos do cabeçalho, e é criado com os valores padrão na primeira execução para o usuário poder
ajustar a aparência sem recompilar.

Não existe no binário nenhuma outra origem de caminho: o programa não lê argumentos de linha de
comando, não enumera diretórios e não recebe caminho de lugar nenhum. Os dois arquivos são texto
simples, visíveis a qualquer momento pelo Bloco de Notas.

## O que o programa faz com o sistema

Sete chamadas a código nativo, todas em `user32.dll` e todas de gerenciamento de janela:

| Função | Para quê |
|---|---|
| `SetProcessDPIAware` | evitar que o Windows amplie a janela e borre o texto |
| `SetWindowPos` | manter a faixa de aviso acima da janela principal |
| `ReleaseCapture`, `SendMessage` | arrastar a janela pela barra superior |
| `FindWindow`, `ShowWindow`, `SetForegroundWindow` | restaurar a janela existente quando o programa é aberto duas vezes |

Não há chamadas a `kernel32.dll`, `advapi32.dll`, `ws2_32.dll` nem `wininet.dll`. O programa
não altera configuração do sistema, não instala serviço, não cria tarefa agendada e não se
adiciona à inicialização. Colocar o programa na inicialização, se desejado, é feito pelo próprio
usuário criando um atalho, conforme descrito nas instruções.

## Privilégios

Roda com a conta do usuário comum, sem solicitar elevação. Não há manifesto pedindo
`requireAdministrator`. Tudo o que ele escreve está dentro do perfil do próprio usuário.

## O ponto que merece atenção

O executável não tem assinatura digital, porque certificado de assinatura de código é pago. A
consequência prática é o aviso "O Windows protegeu o computador" na primeira execução, e a
possibilidade de bloqueio por soluções de proteção de endpoint que barram binários não
assinados por política.

Isso não é falha de segurança do programa, mas é o motivo pelo qual a distribuição deve passar
pela equipe de tecnologia. Se a instituição mantiver autoridade certificadora interna, assinar o
arquivo resolve o aviso e permite liberação por política.

## Limites desta análise

A análise cobre o código gerenciado do executável nesta versão e nesta soma de verificação. Ela
não substitui a avaliação da equipe de segurança da instituição, nem autoriza a distribuição do
programa: a decisão sobre instalar software não homologado em máquinas corporativas é de quem
tem essa alçada.
