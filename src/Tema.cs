using System.Drawing;

namespace Ces
{
    /// <summary>
    /// Paleta e fontes usadas na interface. Ponto unico de ajuste visual.
    ///
    /// As fontes sao refeitas sempre que o usuario muda o tamanho da janela,
    /// para o texto acompanhar a mesma proporcao do layout.
    /// </summary>
    internal static class Tema
    {
        // Valores padrao. Estilo.Carregar pode substituir qualquer um deles
        // pelo que estiver em %APPDATA%\CES\<versao>\estilo.ini.

        // Cores da marca: Principal pinta as superficies escuras (barra de
        // cima, borda, horario da saida) e Realce os detalhes que chamam o
        // olho (risco lateral, campo em foco, caixa marcada). Os nomes
        // descrevem o papel, nao a cor, para continuarem certos em qualquer
        // paleta.
        public static Color Principal = Color.FromArgb(0x00, 0x36, 0x41);
        public static Color Realce = Color.FromArgb(0x00, 0xAE, 0x9D);

        // Superficies
        public static Color Fundo = Color.FromArgb(0xF2, 0xF5, 0xF5);
        public static Color Cartao = Color.FromArgb(0xFF, 0xFF, 0xFF);
        public static Color Borda = Color.FromArgb(0xD2, 0xDD, 0xDD);

        // Texto
        public static Color TextoForte = Color.FromArgb(0x00, 0x36, 0x41);
        public static Color TextoSuave = Color.FromArgb(0x5C, 0x71, 0x76);
        public static Color TextoClaro = Color.FromArgb(0xFF, 0xFF, 0xFF);

        // Estados
        public static Color Atencao = Color.FromArgb(0xE0, 0x8A, 0x11);
        public static Color Critico = Color.FromArgb(0xC0, 0x2E, 0x2E);

        public static Font Titulo { get; private set; }
        public static Font Rotulo { get; private set; }
        public static Font Campo { get; private set; }
        public static Font Destaque { get; private set; }
        public static Font Corpo { get; private set; }
        public static Font Miudo { get; private set; }
        public static Font Simbolo { get; private set; }
        public static Font FaixaTitulo { get; private set; }
        public static Font FaixaTexto { get; private set; }

        /// <summary>
        /// Fontes da geracao anterior. Sao descartadas apenas na troca
        /// seguinte, quando ja nao ha controle usando nenhuma delas.
        /// </summary>
        private static Font[] _anteriores;

        static Tema()
        {
            Aplicar(1f);
        }

        public static void Aplicar(float zoom)
        {
            if (_anteriores != null)
            {
                foreach (Font f in _anteriores)
                {
                    if (f != null)
                    {
                        f.Dispose();
                    }
                }
            }

            _anteriores = new Font[]
            {
                Titulo, Rotulo, Campo, Destaque, Corpo, Miudo,
                Simbolo, FaixaTitulo, FaixaTexto
            };

            Titulo = new Font("Segoe UI", 9F * zoom, FontStyle.Bold);
            Rotulo = new Font("Segoe UI", 7.5F * zoom, FontStyle.Bold);
            Campo = new Font("Segoe UI", 12F * zoom, FontStyle.Regular);
            Destaque = new Font("Segoe UI", 16F * zoom, FontStyle.Bold);
            Corpo = new Font("Segoe UI", 8.25F * zoom, FontStyle.Regular);
            Miudo = new Font("Segoe UI", 7.5F * zoom, FontStyle.Regular);
            Simbolo = new Font("Segoe UI", 9F * zoom, FontStyle.Regular);
            FaixaTitulo = new Font("Segoe UI", 11F * zoom, FontStyle.Bold);
            FaixaTexto = new Font("Segoe UI", 9F * zoom, FontStyle.Regular);
        }
    }
}
