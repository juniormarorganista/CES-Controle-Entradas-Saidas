using System;
using System.Reflection;

namespace Ces
{
    /// <summary>
    /// Versao do executavel em uso.
    ///
    /// Serve para separar as configuracoes de cada versao: cada uma escreve
    /// na propria pasta, entao uma versao nova nunca comeca herdando ajuste
    /// feito em outra, e voltar para uma anterior encontra a pasta dela
    /// intacta.
    /// </summary>
    internal static class Versao
    {
        private static string _numero;

        /// <summary>Tres partes, no formato 1.4.0.</summary>
        public static string Numero
        {
            get
            {
                if (_numero == null)
                {
                    Version v = Assembly.GetExecutingAssembly().GetName().Version;
                    _numero = v.Major + "." + v.Minor + "." + v.Build;
                }

                return _numero;
            }
        }

        /// <summary>
        /// Pasta de configuracao desta versao, dentro do perfil do usuario.
        /// </summary>
        public static string Pasta
        {
            get
            {
                return System.IO.Path.Combine(
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "CES"),
                    Numero);
            }
        }
    }
}
