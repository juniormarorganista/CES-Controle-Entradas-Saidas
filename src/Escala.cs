using System;
using System.Drawing;

namespace Ces
{
    /// <summary>
    /// Converte as medidas do desenho, feitas em 96 pontos por polegada,
    /// para os pixels reais da tela.
    ///
    /// Dois fatores entram na conta. O primeiro vem da tela: o programa se
    /// declara ciente de DPI, entao o Windows nao amplia a janela por conta
    /// propria e a conversao fica por nossa conta. O segundo vem do usuario,
    /// que pode aumentar ou diminuir a janela arrastando o canto.
    /// </summary>
    internal static class Escala
    {
        private static float _tela = 1f;
        private static float _zoom = 1f;

        public static void Inicializar()
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    _tela = g.DpiX / 96f;
                }
            }
            catch (Exception)
            {
                _tela = 1f;
            }

            if (_tela < 1f)
            {
                _tela = 1f;
            }
        }

        /// <summary>Fator escolhido pelo usuario. Um significa tamanho padrao.</summary>
        public static float Zoom
        {
            get { return _zoom; }
            set { _zoom = value; }
        }

        /// <summary>Converte uma medida do desenho em pixels de tela.</summary>
        public static int P(int medida)
        {
            return (int)Math.Round(medida * _tela * _zoom);
        }

        /// <summary>
        /// Converte considerando apenas a tela, sem o zoom. Serve de
        /// referencia para saber a que zoom corresponde um tamanho de janela.
        /// </summary>
        public static int Padrao(int medida)
        {
            return (int)Math.Round(medida * _tela);
        }
    }
}
