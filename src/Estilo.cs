using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Ces
{
    /// <summary>
    /// Aparencia ajustavel sem recompilar: cores e textos do cabecalho ficam
    /// em %APPDATA%\CES\<versao>\estilo.ini.
    ///
    /// O arquivo e criado com os valores padrao na primeira execucao, ja
    /// comentado, para servir de referencia de quais chaves existem. Chave
    /// ausente ou mal preenchida usa o padrao, entao um arquivo estragado
    /// nao impede o programa de abrir.
    /// </summary>
    internal static class Estilo
    {
        private const string NomeArquivo = "estilo.ini";

        public static string Assinatura = "Controle de Entradas e Saídas";

        // Texto de exemplo, feito para ser trocado: quem usa escreve ali o
        // nome da propria area, equipe ou empresa.
        public static string Origem = "Nome da sua área ou equipe";

        /// <summary>
        /// Cada versao guarda o seu arquivo em uma pasta propria, para uma
        /// versao nova nunca comecar herdando ajuste feito em outra.
        /// </summary>
        private static string Pasta
        {
            get { return Versao.Pasta; }
        }

        private static string Caminho
        {
            get { return Path.Combine(Pasta, NomeArquivo); }
        }

        public static void Carregar()
        {
            try
            {
                if (!File.Exists(Caminho))
                {
                    Criar();
                    return;
                }

                Aplicar(Ini.Ler(File.ReadAllLines(Caminho, Encoding.UTF8)));
            }
            catch (IOException)
            {
                // Arquivo indisponivel: segue com a aparencia padrao.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void Aplicar(Dictionary<string, string> dados)
        {
            Tema.Principal = Ini.Cor(dados, "principal", Tema.Principal);
            Tema.Realce = Ini.Cor(dados, "realce", Tema.Realce);
            Tema.Fundo = Ini.Cor(dados, "fundo", Tema.Fundo);
            Tema.Cartao = Ini.Cor(dados, "cartao", Tema.Cartao);
            Tema.Borda = Ini.Cor(dados, "borda", Tema.Borda);
            Tema.TextoForte = Ini.Cor(dados, "textoForte", Tema.TextoForte);
            Tema.TextoSuave = Ini.Cor(dados, "textoSuave", Tema.TextoSuave);
            Tema.TextoClaro = Ini.Cor(dados, "textoClaro", Tema.TextoClaro);
            Tema.Atencao = Ini.Cor(dados, "atencao", Tema.Atencao);
            Tema.Critico = Ini.Cor(dados, "critico", Tema.Critico);

            Assinatura = Ini.Texto(dados, "assinatura", Assinatura);
            Origem = Ini.Texto(dados, "origem", Origem);
        }

        /// <summary>Escreve o arquivo de referencia com os valores atuais.</summary>
        private static void Criar()
        {
            Directory.CreateDirectory(Pasta);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("; CES - aparência");
            sb.AppendLine("; versão " + Versao.Numero);
            sb.AppendLine(";");
            sb.AppendLine("; Edite, salve e abra o programa de novo. Não precisa recompilar.");
            sb.AppendLine("; O arquivo é lido só na abertura: feche e abra o CES para ver a mudança.");
            sb.AppendLine("; Apague uma linha para voltar ao padrão dela. Apague o arquivo");
            sb.AppendLine("; inteiro para voltar tudo ao padrão: ele é recriado na abertura.");
            sb.AppendLine();
            sb.AppendLine("; --- Textos do topo da janela ---");
            sb.AppendLine(";");
            sb.AppendLine("; assinatura: primeira linha, ao lado da sigla CES. Cabem umas 38 letras.");
            sb.AppendLine("; origem: segunda linha, menor. Troque pelo nome da sua área, equipe");
            sb.AppendLine(";         ou empresa. A linha toda cabe na largura da janela.");
            sb.AppendLine("assinatura=" + Assinatura);
            sb.AppendLine("origem=" + Origem);
            sb.AppendLine();
            sb.AppendLine("; --- Cores, no formato #RRGGBB ---");
            sb.AppendLine();
            sb.AppendLine("; Cor principal: barra de cima, borda da janela e horário da saída");
            sb.AppendLine("principal=" + Html(Tema.Principal));
            sb.AppendLine();
            sb.AppendLine("; Cor de realce: risco lateral, campo em foco e caixa marcada");
            sb.AppendLine("realce=" + Html(Tema.Realce));
            sb.AppendLine();
            sb.AppendLine("; Fundo da janela e fundo dos campos");
            sb.AppendLine("fundo=" + Html(Tema.Fundo));
            sb.AppendLine("cartao=" + Html(Tema.Cartao));
            sb.AppendLine();
            sb.AppendLine("; Contorno dos campos");
            sb.AppendLine("borda=" + Html(Tema.Borda));
            sb.AppendLine();
            sb.AppendLine("; Texto dos horários, dos rótulos e sobre fundo escuro");
            sb.AppendLine("textoForte=" + Html(Tema.TextoForte));
            sb.AppendLine("textoSuave=" + Html(Tema.TextoSuave));
            sb.AppendLine("textoClaro=" + Html(Tema.TextoClaro));
            sb.AppendLine();
            sb.AppendLine("; Contagem nos últimos 15 minutos, e jornada ultrapassada");
            sb.AppendLine("atencao=" + Html(Tema.Atencao));
            sb.AppendLine("critico=" + Html(Tema.Critico));

            File.WriteAllText(Caminho, sb.ToString(), Encoding.UTF8);
        }

        private static string Html(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
    }
}
