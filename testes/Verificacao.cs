using System;
using System.Collections.Generic;
using System.Drawing;

namespace Ces
{
    /// <summary>
    /// Verificacao das regras de calculo, da mascara de digitacao, dos marcos
    /// de aviso e da leitura dos arquivos de configuracao.
    /// Compilada e executada por testes\verificar.ps1.
    /// </summary>
    internal static class Verificacao
    {
        private static int _passaram;
        private static int _falharam;

        private static readonly TimeSpan UmaHora = TimeSpan.FromHours(1);

        /// <summary>
        /// Dia fixo para os casos que dependem de data. Nada aqui consulta o
        /// relogio do computador, para a verificacao dar o mesmo resultado
        /// em qualquer dia.
        /// </summary>
        private static readonly DateTime Hoje = new DateTime(2026, 8, 13);

        private static int Main()
        {
            Console.WriteLine("CES - verificacao das regras");
            Console.WriteLine(new string('-', 58));

            CalculoBasico();
            CalculoLimites();
            CalculoSituacoes();
            MascaraMontagem();
            MascaraRestricoes();
            MascaraNormalizacao();
            MascaraDoisPontos();
            MascaraCobertura();
            LeituraTexto();
            MarcosSaida();
            MarcosContagem();
            MarcosAlmoco();
            MarcosReinicio();
            PreferenciasLeitura();
            IniLeitura();
            IniCores();
            VersaoIsolamento();

            Console.WriteLine(new string('-', 58));
            Console.WriteLine("Passaram: " + _passaram + "   Falharam: " + _falharam);

            return _falharam == 0 ? 0 : 1;
        }

        // ---------------------------------------------------------------- //
        // Calculo                                                          //
        // ---------------------------------------------------------------- //

        private static void CalculoBasico()
        {
            Secao("Calculo da saida prevista");

            // Jornada de 8h com intervalo de 1h.
            SaidaIgual("08:00", "12:00", "13:00", "08:00", "17:00");

            // Sem intervalo: equivale a entrada + jornada.
            SaidaIgual("08:00", "12:00", "12:00", "08:00", "16:00");

            // Jornada de 6h, tipica de escriturario.
            SaidaIgual("08:00", "11:00", "12:00", "06:00", "15:00");

            // Intervalo de 2h empurra a saida na mesma medida.
            SaidaIgual("08:00", "12:00", "14:00", "08:00", "18:00");

            // Minutos quebrados: manha 2h45, restam 5h15.
            SaidaIgual("09:30", "12:15", "13:20", "08:00", "18:35");

            // Jornada de 6h20.
            SaidaIgual("07:40", "11:00", "12:00", "06:20", "15:00");

            // Entrada antes das 7h.
            SaidaIgual("06:45", "11:15", "12:30", "08:00", "16:00");

            // Virada de hora no minuto.
            SaidaIgual("08:50", "12:00", "13:00", "08:00", "17:50");
        }

        private static void CalculoLimites()
        {
            Secao("Limites e inconsistencias");

            SituacaoIgual("12:00", "08:00", "13:00", "08:00", Situacao.Invalido,
                "saida para o almoco antes da entrada");

            SituacaoIgual("08:00", "13:00", "12:00", "08:00", Situacao.Invalido,
                "volta do almoco antes da saida");

            SituacaoIgual("20:00", "21:00", "22:00", "08:00", Situacao.Invalido,
                "saida cairia no dia seguinte");

            SituacaoIgual("08:00", "12:00", "13:00", "00:00", Situacao.Invalido,
                "jornada zerada");

            // Manha ja cobre a jornada inteira: saida correta era entrada + jornada.
            Resultado r = Avaliar("06:00", "15:00", "16:00", "08:00", "16:30");
            Conferir("jornada cumprida antes do almoco",
                Calculo.Hhmm(r.Saida), "14:00");
            Conferir("aviso de jornada cumprida antes do almoco",
                r.Aviso.Length > 0, true);

            // Intervalo menor que o minimo gera aviso, mas o calculo continua.
            r = Avaliar("08:00", "12:00", "12:30", "08:00", "10:00");
            Conferir("saida com intervalo de 30min",
                Calculo.Hhmm(r.Saida), "16:30");
            Conferir("aviso de intervalo curto", r.Aviso.Length > 0, true);

            // Faltando qualquer campo o resultado fica incompleto.
            Resultado incompleto = Calculo.Avaliar(
                null, new TimeSpan(12, 0, 0), new TimeSpan(13, 0, 0),
                new TimeSpan(8, 0, 0), new TimeSpan(9, 0, 0), UmaHora);
            Conferir("entrada ausente", incompleto.Situacao, Situacao.Incompleto);
        }

        private static void CalculoSituacoes()
        {
            Secao("Andamento da jornada");

            Resultado r = Avaliar("08:00", "12:00", "13:00", "08:00", "14:00");
            Conferir("as 14h a jornada esta em andamento",
                r.Situacao, Situacao.EmAndamento);
            Conferir("faltam 3h para a saida",
                Calculo.Hhmm(r.Falta), "03:00");

            r = Avaliar("08:00", "12:00", "13:00", "08:00", "16:45");
            Conferir("faltam 15min", Calculo.Hhmm(r.Falta), "00:15");

            r = Avaliar("08:00", "12:00", "13:00", "08:00", "17:00");
            Conferir("as 17h a jornada esta encerrada",
                r.Situacao, Situacao.Encerrado);
            Conferir("excedente zerado no instante da saida",
                Calculo.Hhmm(r.Excedente), "00:00");

            r = Avaliar("08:00", "12:00", "13:00", "08:00", "17:40");
            Conferir("excedente de 40min", Calculo.Hhmm(r.Excedente), "00:40");

            r = Avaliar("08:00", "12:00", "13:00", "08:00", "09:00");
            Conferir("manha trabalhada", Calculo.Hhmm(r.Manha), "04:00");
            Conferir("intervalo apurado", Calculo.Hhmm(r.Intervalo), "01:00");

            Conferir("duracao por extenso com hora e minuto",
                Calculo.PorExtenso(new TimeSpan(3, 12, 0)), "3h 12min");
            Conferir("duracao por extenso so com minutos",
                Calculo.PorExtenso(new TimeSpan(0, 25, 0)), "25min");
            Conferir("duracao por extenso so com horas",
                Calculo.PorExtenso(new TimeSpan(2, 0, 0)), "2h");
            Conferir("duracao por extenso abaixo de um minuto",
                Calculo.PorExtenso(new TimeSpan(0, 0, 30)), "< 1min");
        }

        // ---------------------------------------------------------------- //
        // Mascara                                                          //
        // ---------------------------------------------------------------- //

        private static void MascaraMontagem()
        {
            Secao("Mascara: separador automatico");

            Conferir("campo vazio", Mascara.Montar(""), "");
            Conferir("digitou 1", Mascara.Montar("1"), "1");
            Conferir("digitou 2", Mascara.Montar("2"), "2");

            // Algarismo de 3 a 9 so pode ser hora de um digito.
            Conferir("digitou 8", Mascara.Montar("8"), "08:");
            Conferir("digitou 85", Mascara.Montar("85"), "08:5");
            Conferir("digitou 853", Mascara.Montar("853"), "08:53");
            Conferir("digitou 9", Mascara.Montar("9"), "09:");
            Conferir("digitou 30", Mascara.Montar("30"), "03:0");

            // Algarismo de 0 a 2 aguarda o segundo.
            Conferir("digitou 12", Mascara.Montar("12"), "12:");
            Conferir("digitou 1230", Mascara.Montar("1230"), "12:30");
            Conferir("digitou 0853", Mascara.Montar("0853"), "08:53");
            Conferir("digitou 2359", Mascara.Montar("2359"), "23:59");
            Conferir("digitou 0000", Mascara.Montar("0000"), "00:00");

            // 25 nao e hora valida: o 5 vira minuto.
            Conferir("digitou 25", Mascara.Montar("25"), "02:5");
            Conferir("digitou 253", Mascara.Montar("253"), "02:53");
        }

        private static void MascaraRestricoes()
        {
            Secao("Mascara: recusa de sequencias impossiveis");

            Conferir("minuto nao comeca com 6", Mascara.Cabe("086"), false);
            Conferir("minuto nao comeca com 9", Mascara.Cabe("089"), false);
            Conferir("29 nao e hora nem minuto valido", Mascara.Cabe("29"), false);
            Conferir("quinto algarismo nao cabe", Mascara.Cabe("08533"), false);
            Conferir("0853 cabe", Mascara.Cabe("0853"), true);
            Conferir("2359 cabe", Mascara.Cabe("2359"), true);
            Conferir("853 cabe", Mascara.Cabe("853"), true);
        }

        private static void MascaraNormalizacao()
        {
            Secao("Mascara: completar com zeros");

            Conferir("vazio continua vazio", Mascara.Normalizar(""), "");
            Conferir("8 vira 08:00", Mascara.Normalizar("8"), "0800");
            Conferir("1 vira 01:00", Mascara.Normalizar("1"), "0100");
            Conferir("85 vira 08:50", Mascara.Normalizar("85"), "0850");
            Conferir("853 vira 08:53", Mascara.Normalizar("853"), "0853");
            Conferir("12 vira 12:00", Mascara.Normalizar("12"), "1200");
            Conferir("forma canonica nao muda", Mascara.Normalizar("0853"), "0853");

            Secao("Mascara: texto colado");

            Conferir("colou 08:53", Mascara.Acumular("08:53"), "0853");
            Conferir("colou 8h53", Mascara.Acumular("8h53"), "853");
            Conferir("colou texto sem numero", Mascara.Acumular("abc"), "");
            Conferir("colou sequencia longa", Mascara.Acumular("085399"), "0853");
            Conferir("colou nulo", Mascara.Acumular(null), "");
        }

        private static void MascaraDoisPontos()
        {
            Secao("Mascara: dois pontos como desempate");

            // Horas de 00 a 02 sao ambiguas: apos o "1" nao da para saber se
            // o horario e 01:xx ou 1x:xx. Digitar o separador resolve.
            Conferir("digitou 1:", Digitar("1:"), "01");
            Conferir("digitou 1:23", Montado(Digitar("1:23")), "01:23");
            Conferir("digitou 0:45", Montado(Digitar("0:45")), "00:45");
            Conferir("digitou 2:07", Montado(Digitar("2:07")), "02:07");

            // Sem o separador o mesmo teclado produz o horario cheio.
            Conferir("digitou 123 sem separador", Montado(Digitar("123")), "12:30");

            // Para horas de 03 a 09 o separador ja e automatico e digita-lo
            // nao muda nada.
            Conferir("digitou 8:53", Montado(Digitar("8:53")), "08:53");
            Conferir("digitou 853", Montado(Digitar("853")), "08:53");

            // Separador fora de hora e ignorado.
            Conferir("separador no campo vazio", Digitar(":"), "");
            Conferir("separador repetido", Montado(Digitar("8::53")), "08:53");
        }

        private static void MascaraCobertura()
        {
            Secao("Mascara: cobertura dos 1440 horarios do dia");

            int falhasCompletas = 0;
            int falhasAbreviadas = 0;
            int falhasComSeparador = 0;
            int falhasEstaveis = 0;

            for (int minuto = 0; minuto < 1440; minuto++)
            {
                int h = minuto / 60;
                int m = minuto % 60;
                string esperado = h.ToString("00") + ":" + m.ToString("00");
                string hh = h.ToString("00");
                string mm = m.ToString("00");

                // Caminho 1: os quatro algarismos, sempre valido.
                if (Montado(Digitar(hh + mm)) != esperado)
                {
                    falhasCompletas++;
                }

                // Caminho 2: hora de 3 a 9 com um algarismo so.
                if (h >= 3 && h <= 9
                    && Montado(Digitar(h.ToString() + mm)) != esperado)
                {
                    falhasAbreviadas++;
                }

                // Caminho 3: hora abreviada com separador, valido de 0 a 9.
                if (h < 10
                    && Montado(Digitar(h.ToString() + ":" + mm)) != esperado)
                {
                    falhasComSeparador++;
                }

                // Normalizar duas vezes nao pode mudar o resultado.
                string uma = Mascara.Normalizar(hh + mm);
                if (Mascara.Normalizar(uma) != uma)
                {
                    falhasEstaveis++;
                }
            }

            Conferir("1440 horarios com os quatro algarismos", falhasCompletas, 0);
            Conferir("420 horarios abreviados entre 03h e 09h", falhasAbreviadas, 0);
            Conferir("600 horarios abreviados com separador", falhasComSeparador, 0);
            Conferir("normalizacao estavel", falhasEstaveis, 0);
        }

        /// <summary>
        /// Reproduz a digitacao tecla a tecla exatamente como o campo faz.
        /// </summary>
        private static string Digitar(string teclas)
        {
            string d = string.Empty;

            foreach (char c in teclas)
            {
                if (c == ':')
                {
                    d = Mascara.FecharHora(d);
                    continue;
                }

                if (c < '0' || c > '9')
                {
                    continue;
                }

                string candidato = d + c;
                if (Mascara.Cabe(candidato))
                {
                    d = candidato;
                }
            }

            return d;
        }

        /// <summary>Aplica o fechamento do campo e devolve o texto final.</summary>
        private static string Montado(string digitos)
        {
            return Mascara.Montar(Mascara.Normalizar(digitos));
        }

        private static void LeituraTexto()
        {
            Secao("Leitura de texto");

            Conferir("le 08:53", Calculo.Ler("08:53"), new TimeSpan(8, 53, 0));
            Conferir("le 00:00", Calculo.Ler("00:00"), new TimeSpan(0, 0, 0));
            Conferir("le 23:59", Calculo.Ler("23:59"), new TimeSpan(23, 59, 0));
            Conferir("recusa 24:00", Calculo.Ler("24:00"), null);
            Conferir("recusa 08:60", Calculo.Ler("08:60"), null);
            Conferir("recusa texto solto", Calculo.Ler("abc"), null);
            Conferir("recusa vazio", Calculo.Ler(""), null);
            Conferir("formata 08:05", Calculo.Hhmm(new TimeSpan(8, 5, 0)), "08:05");
            Conferir("formata duracao longa",
                Calculo.Hhmm(new TimeSpan(26, 5, 0)), "26:05");
        }

        // ---------------------------------------------------------------- //
        // Marcos de aviso                                                  //
        // ---------------------------------------------------------------- //

        private static void MarcosSaida()
        {
            Secao("Marcos: alarme do fim da jornada");

            // Os casos abaixo usam os avisos desligados, para que so o fim da
            // jornada possa disparar e o comportamento fique isolado.
            Marcos m = new Marcos();
            Conferir("nada antes da saida",
                Tique(m, Hora(16, 59), false).Disparo, Disparo.Nenhum);

            Aviso a = Tique(m, Hora(17, 0), false);
            Conferir("dispara as 17:00", a.Disparo, Disparo.Saida);
            Conferir("aviso traz o horario da saida", Calculo.Hhmm(a.Horario), "17:00");

            Conferir("nao repete no segundo seguinte",
                Tique(m, Hora(17, 0, 1), false).Disparo, Disparo.Nenhum);
            Conferir("nao repete um minuto depois",
                Tique(m, Hora(17, 1), false).Disparo, Disparo.Nenhum);

            // A caixa de selecao nao desliga este aviso: e a razao de o
            // programa existir.
            Conferir("dispara com os avisos desligados",
                Tique(new Marcos(), Hora(17, 0), false).Disparo, Disparo.Saida);
            Conferir("dispara com os avisos ligados",
                Tique(new Marcos(), Hora(17, 0), true).Disparo, Disparo.Saida);

            // Janela de tolerancia. Vale para o programa aberto tarde e vale
            // para a maquina que dormiu e acordou depois do horario.
            Conferir("acordou 1min59s depois ainda avisa",
                Tique(new Marcos(), Hora(17, 1, 59), false).Disparo, Disparo.Saida);
            Conferir("acordou 2min depois nao avisa",
                Tique(new Marcos(), Hora(17, 2), false).Disparo, Disparo.Nenhum);
            Conferir("acordou muito depois nao avisa",
                Tique(new Marcos(), Hora(18, 30), false).Disparo, Disparo.Nenhum);
        }

        private static void MarcosContagem()
        {
            Secao("Marcos: contagem de 15, 10 e 5 minutos");

            Marcos m = new Marcos();
            Conferir("nada faltando 16 minutos",
                Tique(m, Hora(16, 44), true).Disparo, Disparo.Nenhum);

            Aviso a = Tique(m, Hora(16, 45), true);
            Conferir("dispara aos 15 minutos", a.Disparo, Disparo.Contagem);
            Conferir("informa 15 minutos", a.Faltam, 15);

            Conferir("nao repete o marco de 15",
                Tique(m, Hora(16, 46), true).Disparo, Disparo.Nenhum);

            a = Tique(m, Hora(16, 50), true);
            Conferir("dispara aos 10 minutos", a.Disparo, Disparo.Contagem);
            Conferir("informa 10 minutos", a.Faltam, 10);

            a = Tique(m, Hora(16, 55), true);
            Conferir("dispara aos 5 minutos", a.Disparo, Disparo.Contagem);
            Conferir("informa 5 minutos", a.Faltam, 5);

            Conferir("nao repete depois do ultimo marco",
                Tique(m, Hora(16, 57), true).Disparo, Disparo.Nenhum);

            // Estes tres, sim, a caixa de selecao desliga.
            Conferir("nao dispara com os avisos desligados",
                Tique(new Marcos(), Hora(16, 45), false).Disparo, Disparo.Nenhum);

            // Aberto ja perto da saida: os marcos vencidos ficam registrados
            // e sai um aviso so, com o tempo real que resta.
            a = Tique(new Marcos(), Hora(16, 58), true);
            Conferir("aberto faltando 2min avisa uma vez", a.Disparo, Disparo.Contagem);
            Conferir("aviso traz o tempo real, nao o marco", a.Faltam, 2);

            Conferir("faltando 30s arredonda para 1min",
                Tique(new Marcos(), Hora(16, 59, 30), true).Faltam, 1);

            // O fim da jornada cobre qualquer contagem pendente.
            Marcos n = new Marcos();
            Conferir("saida cobre a contagem",
                Tique(n, Hora(17, 0), true).Disparo, Disparo.Saida);
            Conferir("contagem nao dispara depois da saida",
                Tique(n, Hora(17, 0, 30), true).Disparo, Disparo.Nenhum);

            // E cobre os tres marcos, nao so o primeiro. Sem isso, relogio
            // do sistema ajustado para tras depois do alarme faria a jornada
            // voltar a estar em andamento e as contagens tocarem de novo.
            Conferir("relogio para tras nao ressuscita o marco de 10",
                Tique(n, Hora(16, 50), true).Disparo, Disparo.Nenhum);
            Conferir("relogio para tras nao ressuscita o marco de 5",
                Tique(n, Hora(16, 56), true).Disparo, Disparo.Nenhum);
        }

        private static void MarcosAlmoco()
        {
            Secao("Marcos: alarmes do almoco");

            Marcos m = new Marcos();
            Aviso a = Tique(m, Hora(12, 0), true);
            Conferir("dispara a saida para o almoco", a.Disparo, Disparo.AlmocoSair);
            Conferir("aviso traz a hora de sair", Calculo.Hhmm(a.Horario), "12:00");

            Conferir("nao repete a saida para o almoco",
                Tique(m, Hora(12, 1), true).Disparo, Disparo.Nenhum);

            a = Tique(m, Hora(13, 0), true);
            Conferir("dispara a volta do almoco", a.Disparo, Disparo.AlmocoVoltar);
            Conferir("aviso traz a hora de voltar", Calculo.Hhmm(a.Horario), "13:00");

            Conferir("nao repete a volta do almoco",
                Tique(m, Hora(13, 1), true).Disparo, Disparo.Nenhum);

            // A caixa de selecao cobre apenas a contagem dos 15, 10 e 5
            // minutos: os alarmes do almoco continuam tocando.
            Conferir("almoco dispara com os avisos desligados",
                Tique(new Marcos(), Hora(12, 0), false).Disparo, Disparo.AlmocoSair);

            // Mesma janela de tolerancia do fim da jornada.
            Conferir("almoco ainda avisa 1min59s depois",
                Tique(new Marcos(), Hora(12, 1, 59), true).Disparo, Disparo.AlmocoSair);
            Conferir("almoco nao avisa 2min depois",
                Tique(new Marcos(), Hora(12, 2), true).Disparo, Disparo.Nenhum);

            // Os alarmes do almoco nao dependem de o calculo fechar: os
            // horarios do intervalo podem estar preenchidos com a entrada
            // ainda em branco.
            Aviso incompleto = TiqueEm(new Marcos(), "", "12:00", "13:00", "08:00",
                Hora(12, 0), true, Hoje);
            Conferir("almoco avisa com a entrada em branco",
                incompleto.Disparo, Disparo.AlmocoSair);
        }

        private static void MarcosReinicio()
        {
            Secao("Marcos: reinicio por troca de horario e por virada do dia");

            // Mudar a jornada muda a saida, e os marcos da saida nova sao
            // refeitos.
            Marcos m = new Marcos();
            Conferir("avisou as 17:00",
                Tique(m, Hora(17, 0), false).Disparo, Disparo.Saida);
            Conferir("nao repete na mesma saida",
                Tique(m, Hora(17, 0, 30), false).Disparo, Disparo.Nenhum);

            Aviso a = TiqueEm(m, "08:00", "12:00", "13:00", "09:00",
                Hora(18, 0), false, Hoje);
            Conferir("saida nova avisa de novo", a.Disparo, Disparo.Saida);
            Conferir("aviso traz a saida nova", Calculo.Hhmm(a.Horario), "18:00");

            // Mudar o horario do almoco refaz o marco dele.
            Marcos n = new Marcos();
            Conferir("avisou o almoco as 12:00",
                Tique(n, Hora(12, 0), true).Disparo, Disparo.AlmocoSair);
            a = TiqueEm(n, "08:00", "12:30", "13:30", "08:00",
                Hora(12, 30), true, Hoje);
            Conferir("almoco novo avisa de novo", a.Disparo, Disparo.AlmocoSair);

            // Programa aberto na virada do dia: tudo recomeca.
            Marcos o = new Marcos();
            Conferir("avisou hoje",
                Tique(o, Hora(17, 0), false).Disparo, Disparo.Saida);
            Aviso amanha = TiqueEm(o, "08:00", "12:00", "13:00", "08:00",
                Hora(17, 0), false, Hoje.AddDays(1));
            Conferir("avisa de novo amanha", amanha.Disparo, Disparo.Saida);
        }

        // ---------------------------------------------------------------- //
        // Arquivos de configuracao                                         //
        // ---------------------------------------------------------------- //

        private static void PreferenciasLeitura()
        {
            Secao("Preferencias: leitura do arquivo");

            // Arquivo ausente ou sem nenhuma chave: tudo no padrao.
            Preferencias p = Preferencias.Interpretar(new string[0], Hoje);
            Conferir("entrada padrao", Calculo.Hhmm(p.Entrada), "08:00");
            Conferir("saida do almoco padrao", Calculo.Hhmm(p.SaidaAlmoco), "12:00");
            Conferir("volta do almoco padrao", Calculo.Hhmm(p.VoltaAlmoco), "13:00");
            Conferir("jornada padrao", Calculo.Hhmm(p.Jornada), "08:00");
            Conferir("intervalo minimo padrao", Calculo.Hhmm(p.IntervaloMinimo), "01:00");
            Conferir("avisos ligados por padrao", p.ExibirAvisos, true);
            Conferir("janela sem posicao guardada", p.JanelaX, -1);
            Conferir("zoom padrao", p.Zoom, 1f);

            string[] arquivo =
            {
                "; CES - Controle de Entradas e Saidas",
                "versao=9.9.9",
                "data=2026-08-13",
                "entrada=07:30",
                "saidaAlmoco=11:45",
                "voltaAlmoco=12:45",
                "jornada=06:00",
                "intervaloMinimo=00:30",
                "avisos=0",
                "janelaX=120",
                "janelaY=40",
                "zoom=1.25"
            };

            // Arquivo do mesmo dia: os horarios do dia voltam.
            p = Preferencias.Interpretar(arquivo, Hoje);
            Conferir("entrada do dia", Calculo.Hhmm(p.Entrada), "07:30");
            Conferir("saida do almoco do dia", Calculo.Hhmm(p.SaidaAlmoco), "11:45");
            Conferir("volta do almoco do dia", Calculo.Hhmm(p.VoltaAlmoco), "12:45");
            Conferir("jornada guardada", Calculo.Hhmm(p.Jornada), "06:00");
            Conferir("intervalo minimo guardado", Calculo.Hhmm(p.IntervaloMinimo), "00:30");
            Conferir("avisos desligados", p.ExibirAvisos, false);
            Conferir("posicao da janela em x", p.JanelaX, 120);
            Conferir("posicao da janela em y", p.JanelaY, 40);
            Conferir("zoom guardado", p.Zoom, 1.25f);

            // Arquivo de outro dia: os horarios voltam ao padrao e o resto
            // continua valendo.
            p = Preferencias.Interpretar(arquivo, Hoje.AddDays(1));
            Conferir("entrada de ontem nao volta", Calculo.Hhmm(p.Entrada), "08:00");
            Conferir("saida do almoco de ontem nao volta",
                Calculo.Hhmm(p.SaidaAlmoco), "12:00");
            Conferir("volta do almoco de ontem nao volta",
                Calculo.Hhmm(p.VoltaAlmoco), "13:00");
            Conferir("jornada vale para sempre", Calculo.Hhmm(p.Jornada), "06:00");
            Conferir("avisos valem para sempre", p.ExibirAvisos, false);
            Conferir("zoom vale para sempre", p.Zoom, 1.25f);

            // Valor mal preenchido cai no padrao daquela chave, sem impedir a
            // leitura do resto do arquivo.
            p = Preferencias.Interpretar(new[]
            {
                "data=2026-08-13",
                "entrada=lixo",
                "jornada=25:00",
                "zoom=muito",
                "janelaX=abc",
                "saidaAlmoco=11:00"
            }, Hoje);
            Conferir("entrada invalida cai no padrao", Calculo.Hhmm(p.Entrada), "08:00");
            Conferir("hora acima de 23 cai no padrao", Calculo.Hhmm(p.Jornada), "08:00");
            Conferir("zoom invalido cai no padrao", p.Zoom, 1f);
            Conferir("janelaX invalido cai no padrao", p.JanelaX, -1);
            Conferir("chave boa no mesmo arquivo continua valendo",
                Calculo.Hhmm(p.SaidaAlmoco), "11:00");

            // Data ilegivel equivale a arquivo de outro dia.
            p = Preferencias.Interpretar(new[] { "data=13/08/2026", "entrada=07:00" }, Hoje);
            Conferir("data em outro formato nao restaura horarios",
                Calculo.Hhmm(p.Entrada), "08:00");

            // Zoom e gravado com ponto decimal invariante: virgula nao vale.
            p = Preferencias.Interpretar(new[] { "zoom=1,5" }, Hoje);
            Conferir("zoom com virgula cai no padrao", p.Zoom, 1f);
        }

        private static void IniLeitura()
        {
            Secao("Ini: leitura de chave e valor");

            Dictionary<string, string> d = Ini.Ler(new[]
            {
                "; comentario com = no meio",
                "# outro comentario",
                "",
                "   ",
                "sem separador",
                "=valor sem chave",
                "  chave  =  valor com espaco  ",
                "vazia=",
                "repetida=primeira",
                "repetida=segunda"
            });

            Conferir("linhas invalidas descartadas", d.Count, 3);
            Conferir("espaco em volta da chave e do valor removido",
                d["chave"], "valor com espaco");
            Conferir("valor vazio e aceito", d["vazia"], "");
            Conferir("chave repetida vale a ultima", d["repetida"], "segunda");
            Conferir("comentario com separador nao vira chave",
                d.ContainsKey("; comentario com "), false);

            // Editor de texto que salva com marca de ordem de bytes nao pode
            // esconder a primeira chave do arquivo.
            Conferir("marca de ordem de bytes removida da chave",
                Ini.Ler(new[] { "﻿avisos=0" }).ContainsKey("avisos"), true);

            Conferir("lista nula devolve dicionario vazio", Ini.Ler(null).Count, 0);
            Conferir("linha nula no meio e ignorada",
                Ini.Ler(new string[] { "a=1", null, "b=2" }).Count, 2);

            // Valores tipados, todos com padrao para valor ausente ou torto.
            Dictionary<string, string> v = Ini.Ler(new[]
            {
                "ligado=1", "desligado=0", "torto=sim",
                "inteiro=42", "negativo=-7", "decimal=1.25", "hora=07:30"
            });

            Conferir("bool 1 e verdadeiro", Ini.Bool(v, "ligado", false), true);
            Conferir("bool 0 e falso", Ini.Bool(v, "desligado", true), false);
            Conferir("bool com outro texto e falso", Ini.Bool(v, "torto", true), false);
            Conferir("bool ausente usa o padrao", Ini.Bool(v, "naoExiste", true), true);
            Conferir("inteiro lido", Ini.Int(v, "inteiro", 0), 42);
            Conferir("inteiro negativo lido", Ini.Int(v, "negativo", 0), -7);
            Conferir("inteiro ausente usa o padrao", Ini.Int(v, "naoExiste", 9), 9);
            Conferir("decimal lido", Ini.Float(v, "decimal", 0f), 1.25f);
            Conferir("hora lida", Ini.Hora(v, "hora", TimeSpan.Zero),
                new TimeSpan(7, 30, 0));
            Conferir("hora ausente usa o padrao",
                Ini.Hora(v, "naoExiste", UmaHora), UmaHora);
            Conferir("texto ausente usa o padrao",
                Ini.Texto(v, "naoExiste", "padrao"), "padrao");
        }

        private static void IniCores()
        {
            Secao("Ini: leitura de cores do estilo.ini");

            Dictionary<string, string> d = Ini.Ler(new[]
            {
                "comSustenido=#00AE9D",
                "semSustenido=00AE9D",
                "minusculas=00ae9d",
                "branco=#FFFFFF",
                "preto=#000000",
                "curta=#ABC",
                "longa=#00AE9DFF",
                "naoHexa=#GGHHII",
                "vazia="
            });

            Color turquesa = Color.FromArgb(0x00, 0xAE, 0x9D);
            Color padrao = Color.FromArgb(0x12, 0x34, 0x56);

            Conferir("cor com sustenido", Ini.Cor(d, "comSustenido", padrao), turquesa);
            Conferir("cor sem sustenido", Ini.Cor(d, "semSustenido", padrao), turquesa);
            Conferir("cor em minusculas", Ini.Cor(d, "minusculas", padrao), turquesa);
            Conferir("branco", Ini.Cor(d, "branco", padrao),
                Color.FromArgb(0xFF, 0xFF, 0xFF));
            Conferir("preto", Ini.Cor(d, "preto", padrao),
                Color.FromArgb(0x00, 0x00, 0x00));

            // Linha errada usa o padrao dela, entao arquivo estragado nao
            // impede o programa de abrir.
            Conferir("cor curta cai no padrao", Ini.Cor(d, "curta", padrao), padrao);
            Conferir("cor longa cai no padrao", Ini.Cor(d, "longa", padrao), padrao);
            Conferir("cor nao hexadecimal cai no padrao",
                Ini.Cor(d, "naoHexa", padrao), padrao);
            Conferir("cor vazia cai no padrao", Ini.Cor(d, "vazia", padrao), padrao);
            Conferir("cor ausente cai no padrao",
                Ini.Cor(d, "naoExiste", padrao), padrao);
        }

        // ---------------------------------------------------------------- //
        // Apoio                                                            //
        // ---------------------------------------------------------------- //

        private static TimeSpan Hora(int h, int m)
        {
            return new TimeSpan(h, m, 0);
        }

        private static TimeSpan Hora(int h, int m, int s)
        {
            return new TimeSpan(h, m, s);
        }

        /// <summary>
        /// Um tique do relogio no dia padrao: entrada 08:00, almoco de 12:00
        /// a 13:00, jornada de 08:00, o que da saida as 17:00.
        /// </summary>
        private static Aviso Tique(Marcos m, TimeSpan agora, bool avisos)
        {
            return TiqueEm(m, "08:00", "12:00", "13:00", "08:00", agora, avisos, Hoje);
        }

        /// <summary>
        /// Um tique com os horarios escolhidos, do mesmo jeito que a janela
        /// faz: o resultado do calculo e os horarios do almoco saem dos
        /// mesmos campos.
        /// </summary>
        private static Aviso TiqueEm(Marcos m, string entrada, string saidaAlmoco,
            string voltaAlmoco, string jornada, TimeSpan agora, bool avisos, DateTime dia)
        {
            Resultado r = Calculo.Avaliar(
                Calculo.Ler(entrada),
                Calculo.Ler(saidaAlmoco),
                Calculo.Ler(voltaAlmoco),
                Calculo.Ler(jornada),
                agora,
                UmaHora);

            return m.Avaliar(r, agora, dia,
                Calculo.Ler(saidaAlmoco), Calculo.Ler(voltaAlmoco), avisos);
        }

        private static Resultado Avaliar(string entrada, string saidaAlmoco,
            string voltaAlmoco, string jornada, string agora)
        {
            return Calculo.Avaliar(
                Calculo.Ler(entrada),
                Calculo.Ler(saidaAlmoco),
                Calculo.Ler(voltaAlmoco),
                Calculo.Ler(jornada),
                Calculo.Ler(agora).Value,
                UmaHora);
        }

        private static void SaidaIgual(string entrada, string saidaAlmoco,
            string voltaAlmoco, string jornada, string esperado)
        {
            Resultado r = Avaliar(entrada, saidaAlmoco, voltaAlmoco, jornada, "00:00");
            string caso = entrada + " / " + saidaAlmoco + " / " + voltaAlmoco
                + " / jornada " + jornada;
            Conferir(caso, Calculo.Hhmm(r.Saida), esperado);
        }

        private static void SituacaoIgual(string entrada, string saidaAlmoco,
            string voltaAlmoco, string jornada, Situacao esperada, string caso)
        {
            Resultado r = Avaliar(entrada, saidaAlmoco, voltaAlmoco, jornada, "12:00");
            Conferir(caso, r.Situacao, esperada);
        }

        private static void VersaoIsolamento()
        {
            Secao("Isolamento por versao");

            string numero = Versao.Numero;
            string[] partes = numero.Split('.');
            Conferir("numero da versao tem tres partes", partes.Length, 3);

            bool soNumeros = partes.Length == 3;
            foreach (string parte in partes)
            {
                int n;
                if (parte.Length == 0 || !int.TryParse(parte, out n))
                {
                    soNumeros = false;
                }
            }

            Conferir("cada parte da versao e um numero", soNumeros, true);

            // A pasta de configuracao precisa terminar no numero da versao:
            // e o que impede uma versao de ler o ajuste feito em outra.
            char barra = System.IO.Path.DirectorySeparatorChar;
            string pasta = Versao.Pasta;

            Conferir("a pasta termina com o numero da versao",
                pasta.EndsWith(barra + numero), true);
            Conferir("a pasta fica dentro de CES",
                pasta.Contains(barra + "CES" + barra), true);
            Conferir("versoes diferentes dao pastas diferentes",
                pasta.EndsWith(barra + "0.0.0"), numero == "0.0.0");
        }

        private static void Secao(string titulo)
        {
            Console.WriteLine();
            Console.WriteLine("[" + titulo + "]");
        }

        private static void Conferir(string caso, object obtido, object esperado)
        {
            bool igual = obtido == null ? esperado == null : obtido.Equals(esperado);

            if (igual)
            {
                _passaram++;
                Console.WriteLine("  ok    " + caso);
                return;
            }

            _falharam++;
            Console.WriteLine("  FALHA " + caso
                + "  (esperado " + Texto(esperado) + ", obtido " + Texto(obtido) + ")");
        }

        private static string Texto(object valor)
        {
            return valor == null ? "nulo" : valor.ToString();
        }
    }
}
