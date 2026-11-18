using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace Ces
{
    /// <summary>
    /// Leitura dos arquivos de configuracao, no formato "chave=valor" por
    /// linha. Serve tanto ao preferencias.ini quanto ao estilo.ini.
    ///
    /// Sem dependencia de disco para permitir verificacao isolada: recebe as
    /// linhas ja lidas e devolve os valores. Toda chave tem um padrao, e
    /// valor mal preenchido cai nele em vez de impedir o programa de abrir.
    /// </summary>
    internal static class Ini
    {
        /// <summary>
        /// Reduz as linhas a um dicionario de chave e valor. Linha vazia,
        /// comentario e linha sem separador sao ignoradas.
        /// </summary>
        public static Dictionary<string, string> Ler(IEnumerable<string> linhas)
        {
            Dictionary<string, string> dados = new Dictionary<string, string>();
            if (linhas == null)
            {
                return dados;
            }

            foreach (string linha in linhas)
            {
                if (linha == null)
                {
                    continue;
                }

                string limpa = linha.Trim();
                if (limpa.Length == 0 || limpa[0] == ';' || limpa[0] == '#')
                {
                    continue;
                }

                int corte = limpa.IndexOf('=');
                if (corte <= 0)
                {
                    continue;
                }

                // O TrimStart cobre o caso de o arquivo ter sido salvo com
                // marca de ordem de bytes por um editor de texto.
                string chave = limpa.Substring(0, corte).Trim().TrimStart('﻿');
                dados[chave] = limpa.Substring(corte + 1).Trim();
            }

            return dados;
        }

        public static string Texto(Dictionary<string, string> d, string chave, string padrao)
        {
            if (!d.ContainsKey(chave) || d[chave].Length == 0)
            {
                return padrao;
            }

            return d[chave];
        }

        public static bool Bool(Dictionary<string, string> d, string chave, bool padrao)
        {
            if (!d.ContainsKey(chave))
            {
                return padrao;
            }

            return d[chave] == "1";
        }

        public static int Int(Dictionary<string, string> d, string chave, int padrao)
        {
            int valor;
            if (d.ContainsKey(chave)
                && int.TryParse(d[chave], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out valor))
            {
                return valor;
            }

            return padrao;
        }

        public static float Float(Dictionary<string, string> d, string chave, float padrao)
        {
            float valor;
            if (d.ContainsKey(chave)
                && float.TryParse(d[chave], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out valor))
            {
                return valor;
            }

            return padrao;
        }

        public static TimeSpan Hora(Dictionary<string, string> d, string chave, TimeSpan padrao)
        {
            if (!d.ContainsKey(chave))
            {
                return padrao;
            }

            TimeSpan? valor = Calculo.Ler(d[chave]);
            return valor.HasValue ? valor.Value : padrao;
        }

        /// <summary>Le uma cor no formato #RRGGBB, com ou sem o sustenido.</summary>
        public static Color Cor(Dictionary<string, string> d, string chave, Color padrao)
        {
            if (!d.ContainsKey(chave))
            {
                return padrao;
            }

            string texto = d[chave].TrimStart('#');
            if (texto.Length != 6)
            {
                return padrao;
            }

            int valor;
            if (!int.TryParse(texto, NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out valor))
            {
                return padrao;
            }

            return Color.FromArgb((valor >> 16) & 0xFF, (valor >> 8) & 0xFF, valor & 0xFF);
        }
    }
}
