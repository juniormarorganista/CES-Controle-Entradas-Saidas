using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Ces
{
    internal static class Program
    {
        private const string NomeTrava = "Local\\CES.CalculaEntradaSaida";
        private const string TituloJanela = "CES - Controle de Entradas e Saídas";

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string classe, string titulo);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr janela, int comando);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr janela);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private const int SwRestore = 9;

        private static bool _encerrandoPorErro;

        [STAThread]
        private static void Main()
        {
            bool primeira;
            using (Mutex trava = new Mutex(true, NomeTrava, out primeira))
            {
                if (!primeira)
                {
                    TrazerParaFrente();
                    return;
                }

                // Precisa vir antes de qualquer janela para o Windows nao
                // ampliar a interface por conta propria.
                try
                {
                    SetProcessDPIAware();
                }
                catch (EntryPointNotFoundException)
                {
                }

                Escala.Inicializar();
                Estilo.Carregar();

                // Sem isto, qualquer erro inesperado no relogio de um segundo
                // apareceria como caixa de erro crua do .NET.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += AoFalhar;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new JanelaPrincipal());

                GC.KeepAlive(trava);
            }
        }

        /// <summary>
        /// Erro inesperado: avisa em portugues e encerra, em vez de deixar o
        /// programa seguir num estado que ninguem sabe qual e.
        ///
        /// Encerrar tambem evita a repeticao: o relogio bate a cada segundo,
        /// e um erro que se repita ali encheria a tela de caixas de aviso.
        /// So o primeiro passa, e o Application.Exit grava as preferencias
        /// pelo caminho normal de fechamento.
        ///
        /// Nada e gravado em disco alem dos dois arquivos de sempre: registro
        /// de erro em arquivo abriria um terceiro caminho de escrita, o que
        /// mudaria o que o SEGURANCA.md descreve.
        /// </summary>
        private static void AoFalhar(object remetente, ThreadExceptionEventArgs e)
        {
            if (_encerrandoPorErro)
            {
                return;
            }

            _encerrandoPorErro = true;

            MessageBox.Show(
                "O CES encontrou um erro inesperado e vai fechar.\r\n\r\n"
                    + (e.Exception == null ? "Erro desconhecido." : e.Exception.Message)
                    + "\r\n\r\nBasta abrir o programa de novo. Se acontecer sempre,"
                    + " relate o problema na página do projeto no GitHub,"
                    + " CES-Controle-Entradas-Saidas.",
                TituloJanela,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            Application.Exit();
        }

        /// <summary>
        /// Ja existe uma instancia aberta: restaura a janela existente em vez
        /// de abrir outra.
        /// </summary>
        private static void TrazerParaFrente()
        {
            IntPtr janela = FindWindow(null, TituloJanela);
            if (janela == IntPtr.Zero)
            {
                return;
            }

            ShowWindow(janela, SwRestore);
            SetForegroundWindow(janela);
        }
    }
}
