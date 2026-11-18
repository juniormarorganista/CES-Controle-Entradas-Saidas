using System;

namespace Ces
{
    /// <summary>Aviso que deve aparecer neste instante.</summary>
    internal enum Disparo
    {
        Nenhum,

        /// <summary>Chegou a hora marcada para sair para o almoco.</summary>
        AlmocoSair,

        /// <summary>Chegou a hora marcada para voltar do almoco.</summary>
        AlmocoVoltar,

        /// <summary>A jornada se completou: dali em diante e hora extra.</summary>
        Saida,

        /// <summary>Faltam 15, 10 ou 5 minutos para a saida.</summary>
        Contagem
    }

    internal sealed class Aviso
    {
        public Disparo Disparo;

        /// <summary>Horario a que o aviso se refere.</summary>
        public TimeSpan Horario;

        /// <summary>Minutos que ainda faltam. So vale em <see cref="Disparo.Contagem"/>.</summary>
        public int Faltam;
    }

    /// <summary>
    /// Decide qual aviso disparar a cada segundo, uma vez por marco.
    ///
    /// Sem dependencia de interface para permitir verificacao isolada: recebe
    /// o resultado do calculo e a hora, guarda o que ja foi avisado e devolve
    /// o que mostrar. Quem desenha a faixa e toca o som e a janela.
    /// </summary>
    internal sealed class Marcos
    {
        /// <summary>
        /// Tolerancia para o disparo. Passado esse tempo o marco e
        /// considerado perdido, para abrir o programa muito depois do horario
        /// nao provocar alarme atrasado.
        ///
        /// Vale tambem para o programa ja aberto: maquina suspensa que so
        /// volta depois dessa janela perde o toque, e o horario ultrapassado
        /// aparece apenas na tela, em vermelho.
        /// </summary>
        public static readonly TimeSpan Janela = TimeSpan.FromMinutes(2);

        private bool _marco15, _marco10, _marco5, _marcoSaida;
        private bool _marcoAlmocoSair, _marcoAlmocoVoltar;
        private TimeSpan _saidaMonitorada = TimeSpan.MinValue;
        private TimeSpan _saidaAlmocoMonitorada = TimeSpan.MinValue;
        private TimeSpan _voltaAlmocoMonitorada = TimeSpan.MinValue;
        private DateTime _diaMonitorado = DateTime.MinValue;

        /// <summary>
        /// Apura o instante atual. Quando mais de um aviso cai no mesmo
        /// segundo, todos ficam registrados como avisados e vale o de maior
        /// peso: o fim da jornada cobre a contagem, e qualquer um dos dois
        /// cobre o almoco.
        /// </summary>
        public Aviso Avaliar(Resultado r, TimeSpan agora, DateTime hoje,
            TimeSpan? saidaAlmoco, TimeSpan? voltaAlmoco, bool exibirAvisos)
        {
            if (_diaMonitorado != hoje)
            {
                _diaMonitorado = hoje;
                _marco15 = _marco10 = _marco5 = _marcoSaida = false;
                _marcoAlmocoSair = _marcoAlmocoVoltar = false;
                _saidaMonitorada = TimeSpan.MinValue;
                _saidaAlmocoMonitorada = TimeSpan.MinValue;
                _voltaAlmocoMonitorada = TimeSpan.MinValue;
            }

            // O almoco vem primeiro e independe da situacao do calculo: os
            // horarios do intervalo podem estar preenchidos com a entrada
            // ainda em branco. A caixa de avisos tambem nao o desliga.
            Aviso aviso = Almoco(agora, saidaAlmoco, voltaAlmoco);

            if (r.Situacao != Situacao.EmAndamento && r.Situacao != Situacao.Encerrado)
            {
                return aviso;
            }

            if (r.Saida != _saidaMonitorada)
            {
                _saidaMonitorada = r.Saida;
                _marco15 = _marco10 = _marco5 = _marcoSaida = false;
            }

            if (!_marcoSaida && agora >= r.Saida && agora < r.Saida + Janela)
            {
                // Este e a razao de o programa existir, entao dispara mesmo
                // com os avisos desligados.
                _marcoSaida = true;
                _marco15 = _marco10 = _marco5 = true;
                return Novo(Disparo.Saida, r.Saida);
            }

            if (!exibirAvisos || r.Situacao != Situacao.EmAndamento)
            {
                return aviso;
            }

            int faltam = (int)Math.Ceiling(r.Falta.TotalMinutes);
            bool avisar = false;

            // Do maior marco para o menor: quando varios ja passaram, todos
            // ficam registrados e um unico aviso aparece.
            if (!_marco15 && faltam <= 15)
            {
                _marco15 = true;
                avisar = true;
            }

            if (!_marco10 && faltam <= 10)
            {
                _marco10 = true;
                avisar = true;
            }

            if (!_marco5 && faltam <= 5)
            {
                _marco5 = true;
                avisar = true;
            }

            if (!avisar)
            {
                return aviso;
            }

            // O texto usa o tempo real que resta, nao o marco, para o caso de
            // o programa ser aberto ja perto da saida.
            Aviso contagem = Novo(Disparo.Contagem, r.Saida);
            contagem.Faltam = faltam;
            return contagem;
        }

        private Aviso Almoco(TimeSpan agora, TimeSpan? saida, TimeSpan? volta)
        {
            Aviso aviso = new Aviso();

            if (saida.HasValue)
            {
                if (saida.Value != _saidaAlmocoMonitorada)
                {
                    _saidaAlmocoMonitorada = saida.Value;
                    _marcoAlmocoSair = false;
                }

                if (!_marcoAlmocoSair && agora >= saida.Value
                    && agora < saida.Value + Janela)
                {
                    _marcoAlmocoSair = true;
                    aviso = Novo(Disparo.AlmocoSair, saida.Value);
                }
            }

            if (!volta.HasValue)
            {
                return aviso;
            }

            if (volta.Value != _voltaAlmocoMonitorada)
            {
                _voltaAlmocoMonitorada = volta.Value;
                _marcoAlmocoVoltar = false;
            }

            if (!_marcoAlmocoVoltar && agora >= volta.Value
                && agora < volta.Value + Janela)
            {
                _marcoAlmocoVoltar = true;
                aviso = Novo(Disparo.AlmocoVoltar, volta.Value);
            }

            return aviso;
        }

        private static Aviso Novo(Disparo disparo, TimeSpan horario)
        {
            Aviso aviso = new Aviso();
            aviso.Disparo = disparo;
            aviso.Horario = horario;
            return aviso;
        }
    }
}
