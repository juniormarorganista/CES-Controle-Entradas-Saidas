using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Ces
{
    /// <summary>
    /// Caixa de marcacao desenhada pelo programa.
    ///
    /// A caixa do Windows tem tamanho fixo, definido pelo tema do sistema, e
    /// nao acompanharia o usuario aumentando a janela. Esta acompanha, e de
    /// quebra usa as cores do resto da interface.
    ///
    /// So o quadrado responde ao clique: o texto ao lado e um rotulo comum.
    /// </summary>
    internal sealed class CaixaOpcao : Control
    {
        private bool _marcada;

        public event EventHandler Alterada;

        public CaixaOpcao()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.Selectable,
                true);

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public bool Marcada
        {
            get { return _marcada; }

            set
            {
                if (_marcada == value)
                {
                    return;
                }

                _marcada = value;
                Invalidate();

                if (Alterada != null)
                {
                    Alterada(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                Focus();
                Marcada = !Marcada;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                e.SuppressKeyPress = true;
                Marcada = !Marcada;
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle quadro = new Rectangle(0, 0, Width - 1, Height - 1);

            using (SolidBrush b = new SolidBrush(_marcada ? Tema.Realce : Tema.Cartao))
            {
                g.FillRectangle(b, quadro);
            }

            Color corBorda = _marcada ? Tema.Realce : Tema.Borda;
            if (Focused)
            {
                corBorda = Tema.Principal;
            }

            using (Pen p = new Pen(corBorda, Focused ? 2F : 1F))
            {
                g.DrawRectangle(p, quadro);
            }

            if (!_marcada)
            {
                return;
            }

            using (Pen p = new Pen(Tema.TextoClaro, Math.Max(1.5F, Width / 8F)))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;

                g.DrawLines(p, new Point[]
                {
                    new Point((int)(Width * 0.24F), (int)(Height * 0.52F)),
                    new Point((int)(Width * 0.43F), (int)(Height * 0.71F)),
                    new Point((int)(Width * 0.78F), (int)(Height * 0.29F))
                });
            }
        }
    }
}
