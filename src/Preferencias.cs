using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Ces
{
    /// <summary>
    /// Guarda horarios do dia e preferencias do usuario em
    /// %APPDATA%\CES\<versao>\preferencias.ini.
    /// </summary>
    internal sealed class Preferencias
    {
        private const string NomeArquivo = "preferencias.ini";

        public TimeSpan Entrada = new TimeSpan(8, 0, 0);
        public TimeSpan SaidaAlmoco = new TimeSpan(12, 0, 0);
        public TimeSpan VoltaAlmoco = new TimeSpan(13, 0, 0);
        public TimeSpan Jornada = new TimeSpan(8, 0, 0);
        public TimeSpan IntervaloMinimo = new TimeSpan(1, 0, 0);
        public bool ExibirAvisos = true;
        public DateTime Data = DateTime.Today;
        public int JanelaX = -1;
        public int JanelaY = -1;
        public float Zoom = 1f;

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

        public static Preferencias Carregar()
        {
            try
            {
                if (!File.Exists(Caminho))
                {
                    return new Preferencias();
                }

                return Interpretar(File.ReadAllLines(Caminho, Encoding.UTF8), DateTime.Today);
            }
            catch (IOException)
            {
                // Arquivo indisponivel: segue com os valores padrao.
            }
            catch (UnauthorizedAccessException)
            {
            }

            return new Preferencias();
        }

        /// <summary>
        /// Converte as linhas do arquivo em preferencias. Separado da leitura
        /// do disco para permitir verificacao isolada, com a data de hoje
        /// entrando como parametro em vez de vir do relogio.
        /// </summary>
        public static Preferencias Interpretar(string[] linhas, DateTime hoje)
        {
            Preferencias p = new Preferencias();
            p.Data = hoje;

            Dictionary<string, string> dados = Ini.Ler(linhas);

            p.ExibirAvisos = Ini.Bool(dados, "avisos", true);
            p.Jornada = Ini.Hora(dados, "jornada", p.Jornada);
            p.IntervaloMinimo = Ini.Hora(dados, "intervaloMinimo", p.IntervaloMinimo);
            p.JanelaX = Ini.Int(dados, "janelaX", -1);
            p.JanelaY = Ini.Int(dados, "janelaY", -1);
            p.Zoom = Ini.Float(dados, "zoom", 1f);

            // Horarios do dia so voltam se o arquivo for do mesmo dia.
            DateTime data;
            if (dados.ContainsKey("data")
                && DateTime.TryParseExact(dados["data"], "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out data)
                && data.Date == hoje.Date)
            {
                p.Data = data;
                p.Entrada = Ini.Hora(dados, "entrada", p.Entrada);
                p.SaidaAlmoco = Ini.Hora(dados, "saidaAlmoco", p.SaidaAlmoco);
                p.VoltaAlmoco = Ini.Hora(dados, "voltaAlmoco", p.VoltaAlmoco);
            }

            return p;
        }

        public void Salvar()
        {
            try
            {
                Directory.CreateDirectory(Pasta);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("; CES - Controle de Entradas e Saídas");
                sb.AppendLine("versao=" + Versao.Numero);
                sb.AppendLine("data=" + DateTime.Today.ToString("yyyy-MM-dd",
                    CultureInfo.InvariantCulture));
                sb.AppendLine("entrada=" + Calculo.Hhmm(Entrada));
                sb.AppendLine("saidaAlmoco=" + Calculo.Hhmm(SaidaAlmoco));
                sb.AppendLine("voltaAlmoco=" + Calculo.Hhmm(VoltaAlmoco));
                sb.AppendLine("jornada=" + Calculo.Hhmm(Jornada));
                sb.AppendLine("intervaloMinimo=" + Calculo.Hhmm(IntervaloMinimo));
                sb.AppendLine("avisos=" + (ExibirAvisos ? "1" : "0"));
                sb.AppendLine("janelaX=" + JanelaX.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("janelaY=" + JanelaY.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("zoom=" + Zoom.ToString("0.###", CultureInfo.InvariantCulture));

                File.WriteAllText(Caminho, sb.ToString(), Encoding.UTF8);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

    }
}
