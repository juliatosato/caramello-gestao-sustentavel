namespace Caramello.Desktop;

internal static class Program
{
    private const string MutexName = "Local\\CaramelloGestaoSustentavel-7D79527F";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var primeiraInstancia);
        if (!primeiraInstancia)
        {
            MessageBox.Show("O Caramello Gestão Sustentável já está aberto.", "Caramello", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
