using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ces
{
    /// <summary>
    /// Caixa de horario que dispensa a digitacao dos dois pontos. A regra de
    /// preenchimento fica em <see cref="Mascara"/>.
    /// </summary>
    internal sealed class CampoHora : TextBox
    {
        private string _digitos = string.Empty;
        private bool _interno;

        public event EventHandler ValorAlterado;

        public CampoHora()
        {
            BorderStyle = BorderStyle.None;
            TextAlign = HorizontalAlignment.Center;
            Font = Tema.Campo;
            ForeColor = Tema.TextoForte;
            BackColor = Tema.Cartao;
            MaxLength = 5;
            ShortcutsEnabled = false;
        }

        /// <summary>Horario digitado, ou nulo enquanto estiver incompleto.</summary>
        public TimeSpan? Valor
        {
            get
            {
                string hh, mm;
                Mascara.Dividir(_digitos, out hh, out mm);

                if (hh.Length < 2 || mm.Length < 2)
                {
                    return null;
                }

                return Calculo.Ler(hh + ":" + mm);
            }
        }

        public void DefinirValor(TimeSpan valor)
        {
            int horas = (int)valor.TotalHours;
            if (horas < 0 || horas > 23)
            {
                return;
            }

            _digitos = horas.ToString("00") + valor.Minutes.ToString("00");
            Redesenhar();
        }

        /// <summary>Completa o horario com zeros a direita.</summary>
        public void Normalizar()
        {
            string canonico = Mascara.Normalizar(_digitos);
            if (canonico == _digitos)
            {
                return;
            }

            _digitos = canonico;
            Redesenhar();
        }

        private void Redesenhar()
        {
            _interno = true;
            Text = Mascara.Montar(_digitos);
            SelectionStart = TextLength;
            SelectionLength = 0;
            _interno = false;

            if (ValorAlterado != null)
            {
                ValorAlterado(this, EventArgs.Empty);
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            e.Handled = true;

            if (e.KeyChar == (char)Keys.Back)
            {
                if (_digitos.Length > 0)
                {
                    _digitos = _digitos.Substring(0, _digitos.Length - 1);
                    Redesenhar();
                }

                return;
            }

            // Os dois pontos sao dispensaveis, mas quem digita fixa a hora.
            if (e.KeyChar == ':')
            {
                string fechado = Mascara.FecharHora(_digitos);
                if (fechado != _digitos)
                {
                    _digitos = fechado;
                    Redesenhar();
                }

                return;
            }

            if (e.KeyChar < '0' || e.KeyChar > '9')
            {
                return;
            }

            // Com o texto todo selecionado, a digitacao recomeca o campo.
            string inicio = SelectionLength == TextLength && TextLength > 0
                ? string.Empty
                : _digitos;

            string candidato = inicio + e.KeyChar;
            if (!Mascara.Cabe(candidato))
            {
                return;
            }

            _digitos = candidato;
            Redesenhar();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Normalizar();
                Parent.SelectNextControl(this, true, true, true, true);
                return;
            }

            if (e.KeyCode == Keys.Delete)
            {
                e.SuppressKeyPress = true;
                _digitos = string.Empty;
                Redesenhar();
                return;
            }

            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                Ajustar(e.KeyCode == Keys.Up ? 1 : -1);
                return;
            }

            base.OnKeyDown(e);
        }

        /// <summary>Soma ou subtrai um minuto, com volta no fim do dia.</summary>
        private void Ajustar(int minutos)
        {
            TimeSpan? atual = Valor;
            if (!atual.HasValue)
            {
                return;
            }

            int total = (int)atual.Value.TotalMinutes + minutos;
            total = ((total % 1440) + 1440) % 1440;
            DefinirValor(TimeSpan.FromMinutes(total));
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            if (_interno)
            {
                return;
            }

            // Conteudo colado ou atribuido de fora.
            _digitos = Mascara.Acumular(Text);
            Redesenhar();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            BeginInvoke(new MethodInvoker(SelectAll));
        }

        protected override void OnLeave(EventArgs e)
        {
            Normalizar();
            base.OnLeave(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (SelectionLength == 0)
            {
                SelectionStart = TextLength;
            }
        }
    }
}
