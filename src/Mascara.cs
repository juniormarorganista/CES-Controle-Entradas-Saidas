namespace Ces
{
    /// <summary>
    /// Regra de preenchimento automatico do horario, isolada da interface
    /// para poder ser verificada sozinha.
    ///
    /// O usuario digita apenas algarismos. O separador entra sozinho assim
    /// que a hora fica definida:
    ///
    ///   - primeiro algarismo de 3 a 9: so pode ser hora de um digito,
    ///     entao "853" resulta em 08:53;
    ///   - primeiro algarismo de 0 a 2: aguarda o segundo para decidir,
    ///     entao "1230" resulta em 12:30 e "2530" resulta em 02:53
    ///     (25 nao e hora valida, o 5 passa a ser minuto).
    /// </summary>
    internal static class Mascara
    {
        /// <summary>Separa a sequencia digitada em hora e minuto.</summary>
        public static void Dividir(string d, out string hh, out string mm)
        {
            hh = string.Empty;
            mm = string.Empty;

            if (string.IsNullOrEmpty(d))
            {
                return;
            }

            if (d[0] >= '3')
            {
                hh = "0" + d[0];
                mm = d.Substring(1);
                return;
            }

            if (d.Length == 1)
            {
                hh = d;
                return;
            }

            int valor = ((d[0] - '0') * 10) + (d[1] - '0');
            if (valor <= 23)
            {
                hh = d.Substring(0, 2);
                mm = d.Substring(2);
            }
            else
            {
                hh = "0" + d[0];
                mm = d.Substring(1);
            }
        }

        /// <summary>Monta o texto exibido a partir da sequencia digitada.</summary>
        public static string Montar(string d)
        {
            if (string.IsNullOrEmpty(d))
            {
                return string.Empty;
            }

            string hh, mm;
            Dividir(d, out hh, out mm);

            // Hora ainda indefinida: mostra o algarismo sem separador.
            return hh.Length == 1 ? hh : hh + ":" + mm;
        }

        /// <summary>Diz se a sequencia continua sendo um horario possivel.</summary>
        public static bool Cabe(string candidato)
        {
            string hh, mm;
            Dividir(candidato, out hh, out mm);

            if (mm.Length > 2)
            {
                return false;
            }

            // Minuto nunca comeca com algarismo maior que 5.
            return mm.Length == 0 || mm[0] <= '5';
        }

        /// <summary>
        /// Encerra a hora quando o usuario digita os dois pontos.
        ///
        /// Serve para desfazer a ambiguidade das horas 00 a 02: apos o
        /// algarismo 1 nao ha como saber se o horario e 01:xx ou 1x:xx.
        /// Digitar "1:" fixa a hora em 01. Para as horas de 03 a 09 o
        /// separador ja entra sozinho e este metodo nao altera nada.
        /// </summary>
        public static string FecharHora(string d)
        {
            if (d != null && d.Length == 1 && d[0] <= '2')
            {
                return "0" + d;
            }

            return d;
        }

        /// <summary>
        /// Completa a sequencia com zeros ate a forma canonica de quatro
        /// algarismos. Sequencia vazia continua vazia.
        /// </summary>
        public static string Normalizar(string d)
        {
            if (string.IsNullOrEmpty(d))
            {
                return string.Empty;
            }

            string hh, mm;
            Dividir(d, out hh, out mm);

            if (hh.Length == 1)
            {
                hh = "0" + hh;
            }

            while (mm.Length < 2)
            {
                mm += "0";
            }

            return hh + mm;
        }

        /// <summary>
        /// Reduz um texto qualquer a maior sequencia de algarismos que ainda
        /// forma um horario. Usado quando o campo recebe conteudo colado.
        /// </summary>
        public static string Acumular(string texto)
        {
            string resultado = string.Empty;
            if (texto == null)
            {
                return resultado;
            }

            foreach (char c in texto)
            {
                if (c < '0' || c > '9')
                {
                    continue;
                }

                string candidato = resultado + c;
                if (!Cabe(candidato))
                {
                    break;
                }

                resultado = candidato;
            }

            return resultado;
        }
    }
}
