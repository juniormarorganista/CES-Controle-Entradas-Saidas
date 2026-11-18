using System;
using System.Media;
using System.Threading;

namespace Ces
{
    internal enum Sinal
    {
        Nenhum,

        /// <summary>Uma batida, usada nas contagens de minutos.</summary>
        Aviso,

        /// <summary>Duas batidas, usadas no almoco e no fim da jornada.</summary>
        Alarme
    }

    /// <summary>
    /// Sinais sonoros. Usa apenas o esquema de sons do proprio Windows, para
    /// nao carregar arquivos de audio no executavel e para o volume ficar sob
    /// controle de quem ouve: som de esquema passa pelo mixer do Windows e
    /// pode ser abaixado ou silenciado como qualquer outro aplicativo.
    ///
    /// O toque se repete ate alguem mandar parar, o que amarra a duracao do
    /// som ao tempo em que a faixa de aviso fica na tela.
    /// </summary>
    internal static class Alarme
    {
        /// <summary>Pausa entre as repeticoes do som curto.</summary>
        private const int IntervaloAviso = 900;

        /// <summary>Pausa entre as repeticoes do alarme.</summary>
        private const int IntervaloAlarme = 700;

        /// <summary>Pausa entre as duas batidas de cada alarme.</summary>
        private const int IntervaloBatida = 450;

        /// <summary>Granularidade da espera, para parar sem demora.</summary>
        private const int Fatia = 60;

        /// <summary>
        /// Cada pedido de toque recebe um numero. O laco so continua enquanto
        /// o numero dele ainda for o mais recente, entao pedir um toque novo
        /// ou mandar parar encerra o anterior.
        /// </summary>
        private static int _geracao;

        public static void Repetir(Sinal sinal)
        {
            if (sinal == Sinal.Nenhum)
            {
                Parar();
                return;
            }

            int minha = Interlocked.Increment(ref _geracao);

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    while (Ativo(minha))
                    {
                        if (sinal == Sinal.Aviso)
                        {
                            SystemSounds.Exclamation.Play();
                        }
                        else
                        {
                            Sequencia(minha);
                        }

                        int pausa = sinal == Sinal.Aviso ? IntervaloAviso : IntervaloAlarme;
                        if (!Esperar(minha, pausa))
                        {
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    // Maquina sem dispositivo de audio: segue sem som.
                }
            });
        }

        public static void Parar()
        {
            Interlocked.Increment(ref _geracao);
        }

        /// <summary>
        /// Duas batidas seguidas, para o alarme do almoco e da saida nao se
        /// confundir com o aviso de contagem, que e uma batida so.
        ///
        /// O som de esquema e assincrono: chamado de novo antes de terminar,
        /// interrompe o anterior em vez de somar. A pausa entre as batidas
        /// existe por isso, e e o que da o ritmo de duas notas.
        /// </summary>
        private static void Sequencia(int minha)
        {
            SystemSounds.Hand.Play();

            if (!Esperar(minha, IntervaloBatida))
            {
                return;
            }

            SystemSounds.Hand.Play();
        }

        private static bool Ativo(int minha)
        {
            return Thread.VolatileRead(ref _geracao) == minha;
        }

        private static bool Esperar(int minha, int total)
        {
            for (int decorrido = 0; decorrido < total; decorrido += Fatia)
            {
                if (!Ativo(minha))
                {
                    return false;
                }

                Thread.Sleep(Math.Min(Fatia, total - decorrido));
            }

            return Ativo(minha);
        }
    }
}
