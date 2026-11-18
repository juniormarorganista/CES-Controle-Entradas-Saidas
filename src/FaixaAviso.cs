using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ces
{
    /// <summary>
    /// Faixa que aparece no alto da tela, some sozinha e nao rouba o foco de
    /// quem estiver trabalhando. Desvia da janela principal quando as duas
    /// ocupariam o mesmo espaco.
    /// </summary>
    internal sealed class FaixaAviso : Form
    {
        private const int DuracaoPadrao = 9000;
        private const int Passo = 60;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr janela, IntPtr ordem,
            int x, int y, int largura, int altura, uint opcoes);

        private static readonly IntPtr AcimaDeTudo = new IntPtr(-1);
        private const uint SemMover = 0x0002;
        private const uint SemRedimensionar = 0x0001;
        private const uint SemAtivar = 0x0010;
        private const uint Exibir = 0x0040;

        private static FaixaAviso _ativa;

        private readonly Timer _relogio;
        private readonly string _titulo;
        private readonly string _mensagem;
        private readonly Color _acento;
        private readonly int _duracao;

        private int _decorrido;

        private FaixaAviso(string titulo, string mensagem, Color acento,
            int duracao, Rectangle evitar)
        {
            _titulo = titulo;
            _mensagem = mensagem;
            _acento = acento;
            _duracao = duracao;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Tema.Principal;
            Size = new Size(Escala.P(378), Escala.P(84));
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Location = Posicionar(evitar);

            _relogio = new Timer();
            _relogio.Interval = Passo;
            _relogio.Tick += AoPassar;
            _relogio.Start();
        }

        /// <summary>Impede que a faixa tome o foco da janela ativa.</summary>
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        /// <summary>
        /// Alto da tela por padrao. Se a janela principal estiver ali, a
        /// faixa desce para logo abaixo dela; nao cabendo, sobe para cima.
        /// </summary>
        private Point Posicionar(Rectangle evitar)
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            int x = area.Left + ((area.Width - Width) / 2);
            int y = area.Top + Escala.P(28);

            if (!evitar.IsEmpty && evitar.IntersectsWith(new Rectangle(x, y, Width, Height)))
            {
                y = evitar.Bottom + Escala.P(12);

                if (y + Height > area.Bottom)
                {
                    y = Math.Max(area.Top, evitar.Top - Height - Escala.P(12));
                }
            }

            return new Point(x, y);
        }

        public static void Mostrar(string titulo, string mensagem, Color acento,
            Rectangle evitar, Sinal sinal)
        {
            Mostrar(titulo, mensagem, acento, DuracaoPadrao, evitar, sinal);
        }

        public static void Mostrar(string titulo, string mensagem, Color acento,
            int duracao, Rectangle evitar, Sinal sinal)
        {
            // Fechar a faixa anterior ja interrompe o som dela.
            if (_ativa != null && !_ativa.IsDisposed)
            {
                _ativa.Close();
            }

            _ativa = new FaixaAviso(titulo, mensagem, acento, duracao, evitar);
            _ativa.Show();

            // Garante que a faixa fique acima da janela principal, que
            // tambem esta marcada para ficar sempre visivel.
            SetWindowPos(_ativa.Handle, AcimaDeTudo, 0, 0, 0, 0,
                SemMover | SemRedimensionar | SemAtivar | Exibir);

            // O toque acompanha a faixa: comeca com ela e para quando ela sai,
            // seja pelo tempo esgotado ou por um clique.
            Alarme.Repetir(sinal);
        }

        public static void Encerrar()
        {
            if (_ativa != null && !_ativa.IsDisposed)
            {
                _ativa.Close();
            }

            _ativa = null;
        }

        private void AoPassar(object remetente, EventArgs e)
        {
            _decorrido += Passo;

            if (_decorrido >= _duracao)
            {
                Close();
                return;
            }

            // So a barra de progresso precisa ser repintada.
            Invalidate(new Rectangle(0, Height - Escala.P(3), Width, Escala.P(3)));
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _relogio.Stop();
            _relogio.Dispose();
            Alarme.Parar();

            if (ReferenceEquals(_ativa, this))
            {
                _ativa = null;
            }

            base.OnFormClosed(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush b = new SolidBrush(_acento))
            {
                g.FillRectangle(b, 0, 0, Escala.P(5), Height);
            }

            using (SolidBrush b = new SolidBrush(Tema.TextoClaro))
            {
                g.DrawString(_titulo, Tema.FaixaTitulo, b, Escala.P(20), Escala.P(15));
            }

            using (SolidBrush b = new SolidBrush(Color.FromArgb(0xC5, 0xD6, 0xD9)))
            {
                g.DrawString(_mensagem, Tema.FaixaTexto, b, Escala.P(21), Escala.P(42));
            }

            // Progresso do tempo restante.
            int espessura = Escala.P(3);
            int largura = Width - (int)((long)Width * _decorrido / _duracao);
            using (SolidBrush b = new SolidBrush(_acento))
            {
                g.FillRectangle(b, 0, Height - espessura, largura, espessura);
            }

            using (Pen p = new Pen(Color.FromArgb(0x0E, 0x4C, 0x58)))
            {
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}
