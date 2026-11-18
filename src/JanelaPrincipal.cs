using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ces
{
    internal sealed class JanelaPrincipal : Form
    {
        // Medidas do desenho, em unidades de 96 pontos por polegada. Passam
        // por P() antes de virar pixel, que aplica a tela e o zoom.
        private const int Largura = 332;
        private const int Altura = 206;
        private const int AlturaCabecalho = 46;
        private const int Margem = 12;
        private const int LarguraColuna = 71;
        private const int Vao = 8;
        private const int AlturaCartao = 28;
        private const int TopoRotulos = 56;
        private const int TopoCampos = 70;
        private const int TopoResultado = 108;
        private const int AlturaResultado = 44;
        private const int TopoRodape = 156;
        private const int TopoOpcao = 174;
        private const int LadoCaixa = 16;
        private const int LarguraBotoes = 58;
        private const int LarguraBotao = 29;
        private const int LadoAlca = 16;

        private const float ZoomMinimo = 0.6F;
        private const float ZoomMaximo = 2.0F;

        // Os textos do cabecalho vem de Estilo, ajustaveis sem recompilar.

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr janela, int mensagem, IntPtr wParam, IntPtr lParam);

        private const int WmNcLButtonDown = 0xA1;
        private const int HtCaption = 0x2;

        private readonly Preferencias _prefs;
        private readonly Timer _relogio = new Timer();

        private Panel _barra;
        private Label _botaoMinimizar;
        private Label _botaoFechar;
        private Label[] _rotulos;
        private Panel[] _cartoes;
        private CampoHora[] _campos;
        private CampoHora _entrada;
        private CampoHora _saidaAlmoco;
        private CampoHora _voltaAlmoco;
        private CampoHora _jornada;
        private Panel _resultado;
        private Label _tituloResultado;
        private Label _saidaPrevista;
        private Label _contagem;
        private Label _rodape;
        private CaixaOpcao _opcao;
        private Label _rotuloOpcao;

        // Controle de disparo unico por marco, verificavel em isolamento.
        private readonly Marcos _marcos = new Marcos();

        private bool _encerrando;
        private bool _redimensionando;
        private Point _origemArraste;
        private int _larguraOrigem;

        private static int P(int medida)
        {
            return Escala.P(medida);
        }

        public JanelaPrincipal()
        {
            _prefs = Preferencias.Carregar();
            Escala.Zoom = Limitar(_prefs.Zoom);

            CriarJanela();
            CriarCabecalho();
            CriarCampos();
            CriarResultado();
            CriarRodape();
            AplicarLayout(true);
            Posicionar();

            _entrada.DefinirValor(_prefs.Entrada);
            _saidaAlmoco.DefinirValor(_prefs.SaidaAlmoco);
            _voltaAlmoco.DefinirValor(_prefs.VoltaAlmoco);
            _jornada.DefinirValor(_prefs.Jornada);
            _opcao.Marcada = _prefs.ExibirAvisos;

            _relogio.Interval = 1000;
            _relogio.Tick += AoPassarSegundo;
            _relogio.Start();


            Recalcular();
        }

        private static float Limitar(float zoom)
        {
            if (zoom < ZoomMinimo)
            {
                return ZoomMinimo;
            }

            return zoom > ZoomMaximo ? ZoomMaximo : zoom;
        }

        // ---------------------------------------------------------------- //
        // Montagem                                                         //
        // ---------------------------------------------------------------- //

        private void CriarJanela()
        {
            Text = "CES - Controle de Entradas e Saídas";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Tema.Fundo;
            TopMost = true;
            ShowInTaskbar = true;
            MaximizeBox = false;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual;
        }

        private void Posicionar()
        {
            if (_prefs.JanelaX >= 0 && _prefs.JanelaY >= 0
                && Visivel(_prefs.JanelaX, _prefs.JanelaY))
            {
                Location = new Point(_prefs.JanelaX, _prefs.JanelaY);
                return;
            }

            // Padrao: alto da tela, centralizado na horizontal.
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(
                area.Left + ((area.Width - ClientSize.Width) / 2),
                area.Top + P(12));
        }

        private bool Visivel(int x, int y)
        {
            Rectangle ponto = new Rectangle(x, y, ClientSize.Width, ClientSize.Height);
            foreach (Screen tela in Screen.AllScreens)
            {
                if (tela.WorkingArea.IntersectsWith(ponto))
                {
                    return true;
                }
            }

            return false;
        }

        private void CriarCabecalho()
        {
            _barra = new Panel();
            _barra.BackColor = Tema.Principal;
            _barra.MouseDown += AoArrastar;
            _barra.Paint += AoPintarCabecalho;
            Controls.Add(_barra);

            _botaoMinimizar = CriarBotao("−");
            _botaoMinimizar.Click += delegate { WindowState = FormWindowState.Minimized; };
            _barra.Controls.Add(_botaoMinimizar);

            _botaoFechar = CriarBotao("✕");
            _botaoFechar.Click += delegate { Close(); };
            _barra.Controls.Add(_botaoFechar);
        }

        private static Label CriarBotao(string simbolo)
        {
            Label b = new Label();
            b.Text = simbolo;
            b.TextAlign = ContentAlignment.MiddleCenter;
            b.ForeColor = Color.FromArgb(0xB8, 0xCD, 0xD1);
            b.BackColor = Color.Transparent;
            b.Cursor = Cursors.Hand;

            b.MouseEnter += delegate
            {
                b.BackColor = Color.FromArgb(0x0E, 0x4C, 0x58);
                b.ForeColor = Tema.TextoClaro;
            };
            b.MouseLeave += delegate
            {
                b.BackColor = Color.Transparent;
                b.ForeColor = Color.FromArgb(0xB8, 0xCD, 0xD1);
            };

            return b;
        }

        private void AoPintarCabecalho(object remetente, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush b = new SolidBrush(Tema.Realce))
            {
                g.FillRectangle(b, 0, 0, P(4), _barra.Height);
            }

            float x = P(13);
            using (SolidBrush b = new SolidBrush(Tema.TextoClaro))
            {
                g.DrawString("CES", Tema.Titulo, b, x, P(6));
            }

            x += g.MeasureString("CES", Tema.Titulo).Width + P(2);

            using (SolidBrush b = new SolidBrush(Color.FromArgb(0x8F, 0xB2, 0xB8)))
            {
                g.DrawString(Estilo.Assinatura, Tema.Corpo, b, x, P(8));
            }

            using (SolidBrush b = new SolidBrush(Color.FromArgb(0x6F, 0x95, 0x9C)))
            {
                g.DrawString(Estilo.Origem, Tema.Miudo, b, P(13), P(26));
            }
        }

        private void AoArrastar(object remetente, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, WmNcLButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
        }

        private void CriarCampos()
        {
            string[] textos = { "ENTRADA", "SAÍDA ALM.", "VOLTA ALM.", "JORNADA" };
            _rotulos = new Label[4];
            _cartoes = new Panel[4];
            _campos = new CampoHora[4];

            for (int i = 0; i < 4; i++)
            {
                Label rotulo = new Label();
                rotulo.Text = textos[i];
                rotulo.ForeColor = Tema.TextoSuave;
                rotulo.BackColor = Color.Transparent;
                rotulo.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(rotulo);
                _rotulos[i] = rotulo;

                Panel cartao = new Panel();
                cartao.BackColor = Tema.Cartao;
                Controls.Add(cartao);
                _cartoes[i] = cartao;

                CampoHora campo = new CampoHora();
                cartao.Controls.Add(campo);
                campo.ValorAlterado += delegate { Recalcular(); };
                campo.Enter += delegate { cartao.Invalidate(); };
                campo.Leave += delegate
                {
                    cartao.Invalidate();
                    GravarPreferencias();
                };
                _campos[i] = campo;

                CampoHora referencia = campo;
                cartao.Paint += delegate(object remetente, PaintEventArgs e)
                {
                    bool ativo = referencia.Focused;
                    using (Pen p = new Pen(ativo ? Tema.Realce : Tema.Borda,
                        ativo ? Math.Max(2F, Escala.Zoom * 2F) : Math.Max(1F, Escala.Zoom)))
                    {
                        e.Graphics.DrawRectangle(p, 0, 0,
                            cartao.Width - 1, cartao.Height - 1);
                    }
                };
            }

            _entrada = _campos[0];
            _saidaAlmoco = _campos[1];
            _voltaAlmoco = _campos[2];
            _jornada = _campos[3];
        }

        private void CriarResultado()
        {
            _resultado = new Panel();
            _resultado.BackColor = Tema.Cartao;
            _resultado.Paint += delegate(object remetente, PaintEventArgs e)
            {
                using (SolidBrush b = new SolidBrush(Tema.Realce))
                {
                    e.Graphics.FillRectangle(b, 0, 0, P(4), _resultado.Height);
                }

                using (Pen p = new Pen(Tema.Borda, Math.Max(1F, Escala.Zoom)))
                {
                    e.Graphics.DrawRectangle(p, 0, 0,
                        _resultado.Width - 1, _resultado.Height - 1);
                }
            };
            Controls.Add(_resultado);

            // Os tres elementos ficam lado a lado na mesma linha: rotulo,
            // horario e contagem regressiva.
            _tituloResultado = new Label();
            _tituloResultado.Text = "SAÍDA PREVISTA";
            _tituloResultado.ForeColor = Tema.TextoSuave;
            _tituloResultado.BackColor = Color.Transparent;
            _tituloResultado.TextAlign = ContentAlignment.MiddleLeft;
            _resultado.Controls.Add(_tituloResultado);

            _saidaPrevista = new Label();
            _saidaPrevista.ForeColor = Tema.Principal;
            _saidaPrevista.BackColor = Color.Transparent;
            _saidaPrevista.TextAlign = ContentAlignment.MiddleLeft;
            _resultado.Controls.Add(_saidaPrevista);

            _contagem = new Label();
            _contagem.ForeColor = Tema.TextoSuave;
            _contagem.BackColor = Color.Transparent;
            _contagem.TextAlign = ContentAlignment.MiddleRight;
            _resultado.Controls.Add(_contagem);
        }

        private void CriarRodape()
        {
            _rodape = new Label();
            _rodape.ForeColor = Tema.TextoSuave;
            _rodape.BackColor = Color.Transparent;
            Controls.Add(_rodape);

            _opcao = new CaixaOpcao();
            _opcao.Alterada += delegate
            {
                _prefs.ExibirAvisos = _opcao.Marcada;
                GravarPreferencias();
            };
            Controls.Add(_opcao);

            _rotuloOpcao = new Label();
            _rotuloOpcao.Text = "Avisar 15, 10 e 5 minutos antes da saída";
            _rotuloOpcao.ForeColor = Tema.TextoForte;
            _rotuloOpcao.BackColor = Color.Transparent;
            _rotuloOpcao.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_rotuloOpcao);
        }

        // ---------------------------------------------------------------- //
        // Layout                                                           //
        // ---------------------------------------------------------------- //

        /// <summary>
        /// Recalcula todas as medidas e fontes a partir do zoom atual. E o
        /// unico lugar que define posicao e tamanho de controle.
        /// </summary>
        private void AplicarLayout(bool ajustarJanela)
        {
            SuspendLayout();

            Tema.Aplicar(Escala.Zoom);

            if (ajustarJanela)
            {
                ClientSize = new Size(P(Largura), P(Altura));
            }

            int largura = ClientSize.Width;

            _barra.Bounds = new Rectangle(0, 0, largura, P(AlturaCabecalho));
            _botaoMinimizar.Font = Tema.Simbolo;
            _botaoMinimizar.Bounds = new Rectangle(
                largura - P(LarguraBotoes), 0, P(LarguraBotao), P(AlturaCabecalho));
            _botaoFechar.Font = Tema.Simbolo;
            _botaoFechar.Bounds = new Rectangle(
                largura - P(LarguraBotao), 0, P(LarguraBotao), P(AlturaCabecalho));

            for (int i = 0; i < 4; i++)
            {
                int x = P(Margem + (i * (LarguraColuna + Vao)));

                _rotulos[i].Font = Tema.Rotulo;
                _rotulos[i].Bounds = new Rectangle(
                    x, P(TopoRotulos), P(LarguraColuna), P(12));

                _cartoes[i].Bounds = new Rectangle(
                    x, P(TopoCampos), P(LarguraColuna), P(AlturaCartao));

                _campos[i].Font = Tema.Campo;
                _campos[i].Width = P(LarguraColuna) - P(8);
                _campos[i].Location = new Point(
                    P(4), (_cartoes[i].Height - _campos[i].Height) / 2);
            }

            _resultado.Bounds = new Rectangle(
                P(Margem), P(TopoResultado),
                largura - (2 * P(Margem)), P(AlturaResultado));

            _tituloResultado.Font = Tema.Rotulo;
            _tituloResultado.Bounds = new Rectangle(P(14), 0, P(106), P(AlturaResultado));

            _saidaPrevista.Font = Tema.Destaque;
            _saidaPrevista.Bounds = new Rectangle(P(124), 0, P(88), P(AlturaResultado));

            _contagem.Font = Tema.Corpo;
            _contagem.Bounds = new Rectangle(P(214), 0, P(84), P(AlturaResultado));

            _rodape.Font = Tema.Corpo;
            _rodape.Bounds = new Rectangle(
                P(Margem + 2), P(TopoRodape), largura - (2 * P(Margem)), P(14));

            _opcao.Bounds = new Rectangle(
                P(Margem), P(TopoOpcao), P(LadoCaixa), P(LadoCaixa));

            _rotuloOpcao.Font = Tema.Corpo;
            _rotuloOpcao.Bounds = new Rectangle(
                P(Margem + LadoCaixa + 5), P(TopoOpcao),
                largura - P(Margem + LadoCaixa + 5) - P(Margem), P(LadoCaixa));

            ResumeLayout(true);
            Invalidate(true);
        }

        // ---------------------------------------------------------------- //
        // Redimensionamento                                                //
        // ---------------------------------------------------------------- //

        private bool NaAlca(Point p)
        {
            int lado = P(LadoAlca);
            return p.X >= ClientSize.Width - lado
                && p.Y >= ClientSize.Height - lado;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && NaAlca(e.Location))
            {
                _redimensionando = true;
                _origemArraste = Cursor.Position;
                _larguraOrigem = ClientSize.Width;
                Cursor = Cursors.SizeNWSE;
                Capture = true;
                return;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_redimensionando)
            {
                AjustarZoom(_larguraOrigem + (Cursor.Position.X - _origemArraste.X));

                // Redimensionar a janela solta a captura do mouse. Sem
                // reafirmar aqui, o arraste para no meio e os eventos
                // seguintes vao para o controle que estiver sob o cursor.
                if (!Capture)
                {
                    Capture = true;
                }

                return;
            }

            Cursor = NaAlca(e.Location) ? Cursors.SizeNWSE : Cursors.Default;
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_redimensionando)
            {
                _redimensionando = false;
                Capture = false;
                Cursor = Cursors.Default;
                GravarPreferencias();
                return;
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Converte a largura arrastada em zoom e refaz o layout. A altura
        /// acompanha na mesma proporcao, entao a janela nunca se deforma.
        /// </summary>
        private void AjustarZoom(int larguraDesejada)
        {
            float alvo = Limitar(larguraDesejada / (float)Escala.Padrao(Largura));

            if (Math.Abs(alvo - Escala.Zoom) < 0.004F)
            {
                return;
            }

            Escala.Zoom = alvo;
            AplicarLayout(true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (Pen p = new Pen(Tema.Principal, Math.Max(1F, Escala.Zoom)))
            {
                e.Graphics.DrawRectangle(p, 0, 0,
                    ClientSize.Width - 1, ClientSize.Height - 1);
            }

            DesenharAlca(e.Graphics);
        }

        /// <summary>Tres riscos no canto, indicando que a janela estica.</summary>
        private void DesenharAlca(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int x = ClientSize.Width - P(3);
            int y = ClientSize.Height - P(3);

            using (Pen p = new Pen(Tema.Principal, Math.Max(1F, Escala.Zoom)))
            {
                for (int i = 0; i < 3; i++)
                {
                    int d = P(4 + (i * 4));
                    g.DrawLine(p, x - d, y, x, y - d);
                }
            }
        }

        // ---------------------------------------------------------------- //
        // Calculo e avisos                                                 //
        // ---------------------------------------------------------------- //

        private void AoPassarSegundo(object remetente, EventArgs e)
        {
            Recalcular();
        }

        /// <summary>
        /// Espaco que a faixa de aviso deve evitar. Vazio quando a janela
        /// esta minimizada.
        /// </summary>
        private Rectangle AreaOcupada
        {
            get
            {
                return WindowState == FormWindowState.Normal
                    ? Bounds
                    : Rectangle.Empty;
            }
        }

        private void Recalcular()
        {
            TimeSpan agora = DateTime.Now.TimeOfDay;

            Resultado r = Calculo.Avaliar(
                _entrada.Valor,
                _saidaAlmoco.Valor,
                _voltaAlmoco.Valor,
                _jornada.Valor,
                agora,
                _prefs.IntervaloMinimo);

            switch (r.Situacao)
            {
                case Situacao.Incompleto:
                    _saidaPrevista.Text = "--:--";
                    _saidaPrevista.ForeColor = Tema.Borda;
                    _contagem.Text = string.Empty;
                    _rodape.Text = "Preencha os quatro horários.";
                    _rodape.ForeColor = Tema.TextoSuave;
                    break;

                case Situacao.Invalido:
                    _saidaPrevista.Text = "--:--";
                    _saidaPrevista.ForeColor = Tema.Borda;
                    _contagem.Text = string.Empty;
                    _rodape.Text = r.Aviso;
                    _rodape.ForeColor = Tema.Critico;
                    break;

                case Situacao.EmAndamento:
                    _saidaPrevista.Text = Calculo.Hhmm(r.Saida);
                    _saidaPrevista.ForeColor = Tema.Principal;
                    _contagem.Text = "faltam\r\n" + Calculo.PorExtenso(r.Falta);
                    _contagem.ForeColor = r.Falta <= TimeSpan.FromMinutes(15)
                        ? Tema.Atencao
                        : Tema.TextoSuave;
                    DescreverRodape(r);
                    break;

                case Situacao.Encerrado:
                    _saidaPrevista.Text = Calculo.Hhmm(r.Saida);
                    _saidaPrevista.ForeColor = Tema.Critico;
                    _contagem.Text = "excedente\r\n+" + Calculo.PorExtenso(r.Excedente);
                    _contagem.ForeColor = Tema.Critico;
                    DescreverRodape(r);
                    break;
            }

            VerificarMarcos(r, agora);
        }

        private void DescreverRodape(Resultado r)
        {
            if (r.Aviso.Length > 0)
            {
                _rodape.Text = r.Aviso;
                _rodape.ForeColor = Tema.Atencao;
                return;
            }

            _rodape.Text = "Manhã " + Calculo.Hhmm(r.Manha)
                + "     Intervalo " + Calculo.Hhmm(r.Intervalo);
            _rodape.ForeColor = Tema.TextoSuave;
        }

        /// <summary>
        /// Quem decide o que disparar e <see cref="Marcos"/>. Aqui fica
        /// apenas o texto, a cor e o som de cada aviso.
        /// </summary>
        private void VerificarMarcos(Resultado r, TimeSpan agora)
        {
            Aviso aviso = _marcos.Avaliar(
                r,
                agora,
                DateTime.Today,
                _saidaAlmoco.Valor,
                _voltaAlmoco.Valor,
                _opcao.Marcada);

            switch (aviso.Disparo)
            {
                case Disparo.Saida:
                    FaixaAviso.Mostrar(
                        "Jornada cumprida",
                        "São " + Calculo.Hhmm(aviso.Horario) + ". A partir daqui é hora extra.",
                        Tema.Critico,
                        14000,
                        AreaOcupada,
                        Sinal.Alarme);
                    break;

                case Disparo.Contagem:
                    FaixaAviso.Mostrar(
                        aviso.Faltam == 1
                            ? "Falta 1 minuto"
                            : "Faltam " + aviso.Faltam + " minutos",
                        "Saída prevista para " + Calculo.Hhmm(aviso.Horario) + ".",
                        Tema.Realce,
                        AreaOcupada,
                        Sinal.Aviso);
                    break;

                case Disparo.AlmocoSair:
                    FaixaAviso.Mostrar(
                        "Hora do almoço",
                        "Saída marcada para " + Calculo.Hhmm(aviso.Horario) + ".",
                        Tema.Realce,
                        AreaOcupada,
                        Sinal.Alarme);
                    break;

                case Disparo.AlmocoVoltar:
                    FaixaAviso.Mostrar(
                        "Fim do intervalo",
                        "Retorno marcado para " + Calculo.Hhmm(aviso.Horario) + ".",
                        Tema.Realce,
                        AreaOcupada,
                        Sinal.Alarme);
                    break;
            }
        }

        // ---------------------------------------------------------------- //
        // Encerramento                                                     //
        // ---------------------------------------------------------------- //

        private void GravarPreferencias()
        {
            if (_encerrando)
            {
                return;
            }

            TimeSpan? v;

            v = _entrada.Valor;
            if (v.HasValue)
            {
                _prefs.Entrada = v.Value;
            }

            v = _saidaAlmoco.Valor;
            if (v.HasValue)
            {
                _prefs.SaidaAlmoco = v.Value;
            }

            v = _voltaAlmoco.Valor;
            if (v.HasValue)
            {
                _prefs.VoltaAlmoco = v.Value;
            }

            v = _jornada.Valor;
            if (v.HasValue)
            {
                _prefs.Jornada = v.Value;
            }

            _prefs.ExibirAvisos = _opcao.Marcada;
            _prefs.Zoom = Escala.Zoom;

            if (WindowState == FormWindowState.Normal)
            {
                _prefs.JanelaX = Location.X;
                _prefs.JanelaY = Location.Y;
            }

            _prefs.Salvar();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Confirma sempre que o fechamento parte do usuario. Desligar o
            // Windows ou encerrar pelo gerenciador de tarefas nao pergunta.
            bool confirmar = e.CloseReason != CloseReason.WindowsShutDown
                && e.CloseReason != CloseReason.TaskManagerClosing
                && e.CloseReason != CloseReason.ApplicationExitCall;

            if (confirmar)
            {
                DialogResult resposta = MessageBox.Show(
                    this,
                    "Deseja fechar o CES?\r\n\r\nOs avisos de fim de jornada deixarão de funcionar.",
                    "CES - Controle de Entradas e Saídas",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (resposta != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _relogio.Stop();

            foreach (CampoHora campo in _campos)
            {
                campo.Normalizar();
            }

            GravarPreferencias();
            _encerrando = true;

            FaixaAviso.Encerrar();
            base.OnFormClosing(e);
        }
    }
}
