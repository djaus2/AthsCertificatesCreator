using System.Threading.Tasks;
using System.Windows;

namespace AthleticsCertificateMaker.GenerateCertificateApp;

public partial class App : Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        var splash = new SplashWindow();
        splash.Show();

        await Task.Delay(1400);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();

        splash.Close();
    }
}
