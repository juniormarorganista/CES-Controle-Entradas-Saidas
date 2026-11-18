using System;

namespace Ces
{
    internal enum Situacao
    {
        /// <summary>Falta preencher algum horario.</summary>
        Incompleto,

        /// <summary>Sequencia de horarios impossivel.</summary>
        Invalido,

        /// <summary>Jornada em curso, saida ainda no futuro.</summary>
        EmAndamento,

        /// <summary>Horario de saida atingido ou ultrapassado.</summary>
        Encerrado
    }

    internal sealed class Resultado
    {
        public Situacao Situacao;

        /// <summary>Horario em que a jornada se completa.</summary>
        public TimeSpan Saida;

        /// <summary>Periodo trabalhado antes do almoco.</summary>
        public TimeSpan Manha;

        /// <summary>Duracao do intervalo de almoco.</summary>
        public TimeSpan Intervalo;

        /// <summary>Tempo que ainda falta para a saida.</summary>
        public TimeSpan Falta;

        /// <summary>Tempo trabalhado alem da jornada.</summary>
        public TimeSpan Excedente;

        /// <summary>Inconsistencia detectada, vazio quando nao ha.</summary>
        public string Aviso;

        public Resultado()
        {
            Aviso = string.Empty;
        }
    }

    /// <summary>
    /// Regra de calculo da jornada. Sem dependencia de interface para
    /// permitir verificacao isolada.
    /// </summary>
    internal static class Calculo
    {
        private static readonly TimeSpan UmDia = TimeSpan.FromHours(24);

        /// <summary>
        /// Apura a situacao do dia.
        ///
        /// A saida prevista sai de:
        ///     saida = volta do almoco + (jornada - periodo da manha)
        /// onde periodo da manha = saida para o almoco - entrada.
        ///
        /// Quando nao ha intervalo (saida e volta no mesmo horario) a
        /// expressao se reduz a entrada + jornada.
        /// </summary>
        public static Resultado Avaliar(
            TimeSpan? entrada,
            TimeSpan? saidaAlmoco,
            TimeSpan? voltaAlmoco,
            TimeSpan? jornada,
            TimeSpan agora,
            TimeSpan intervaloMinimo)
        {
            Resultado r = new Resultado();

            if (!entrada.HasValue || !saidaAlmoco.HasValue
                || !voltaAlmoco.HasValue || !jornada.HasValue)
            {
                r.Situacao = Situacao.Incompleto;
                return r;
            }

            TimeSpan e = entrada.Value;
            TimeSpan sa = saidaAlmoco.Value;
            TimeSpan va = voltaAlmoco.Value;
            TimeSpan j = jornada.Value;

            if (j <= TimeSpan.Zero)
            {
                r.Situacao = Situacao.Invalido;
                r.Aviso = "A jornada precisa ser maior que zero.";
                return r;
            }

            if (j >= UmDia)
            {
                r.Situacao = Situacao.Invalido;
                r.Aviso = "A jornada precisa ser menor que 24 horas.";
                return r;
            }

            if (sa < e)
            {
                r.Situacao = Situacao.Invalido;
                r.Aviso = "A saída para o almoço está antes da entrada.";
                return r;
            }

            if (va < sa)
            {
                r.Situacao = Situacao.Invalido;
                r.Aviso = "A volta do almoço está antes da saída para o almoço.";
                return r;
            }

            r.Manha = sa - e;
            r.Intervalo = va - sa;

            if (r.Manha >= j)
            {
                // Jornada ja cumprida antes do intervalo: a saida correta
                // seria entrada + jornada.
                r.Saida = e + j;
                r.Aviso = "A jornada se completou antes do almoço.";
            }
            else
            {
                r.Saida = va + (j - r.Manha);
            }

            if (r.Saida >= UmDia)
            {
                r.Situacao = Situacao.Invalido;
                r.Aviso = "Com esses horários a saída cairia no dia seguinte.";
                return r;
            }

            if (agora >= r.Saida)
            {
                r.Situacao = Situacao.Encerrado;
                r.Excedente = agora - r.Saida;
            }
            else
            {
                r.Situacao = Situacao.EmAndamento;
                r.Falta = r.Saida - agora;
            }

            if (r.Aviso.Length == 0 && r.Intervalo < intervaloMinimo)
            {
                r.Aviso = "Intervalo de almoço abaixo de " + Hhmm(intervaloMinimo) + ".";
            }

            return r;
        }

        /// <summary>Formata uma duracao ou horario como HH:mm.</summary>
        public static string Hhmm(TimeSpan t)
        {
            bool negativo = t < TimeSpan.Zero;
            if (negativo)
            {
                t = t.Negate();
            }

            int horas = (int)t.TotalHours;
            string texto = horas.ToString("00") + ":" + t.Minutes.ToString("00");
            return negativo ? "-" + texto : texto;
        }

        /// <summary>
        /// Formata uma duracao em texto corrido, para a contagem regressiva.
        /// </summary>
        public static string PorExtenso(TimeSpan t)
        {
            if (t < TimeSpan.Zero)
            {
                t = t.Negate();
            }

            int horas = (int)t.TotalHours;
            int minutos = t.Minutes;

            if (horas > 0 && minutos > 0)
            {
                return horas + "h " + minutos + "min";
            }

            if (horas > 0)
            {
                return horas + "h";
            }

            if (minutos > 0)
            {
                return minutos + "min";
            }

            return "< 1min";
        }

        /// <summary>
        /// Converte texto no formato HH:mm em duracao. Devolve nulo quando
        /// o texto nao representa um horario valido.
        /// </summary>
        public static TimeSpan? Ler(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return null;
            }

            string[] partes = texto.Split(':');
            if (partes.Length != 2)
            {
                return null;
            }

            int h, m;
            if (!int.TryParse(partes[0], out h) || !int.TryParse(partes[1], out m))
            {
                return null;
            }

            if (h < 0 || h > 23 || m < 0 || m > 59)
            {
                return null;
            }

            return new TimeSpan(h, m, 0);
        }
    }
}
