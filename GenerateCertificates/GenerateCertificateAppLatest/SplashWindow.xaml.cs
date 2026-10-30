using System.Reflection;
using System.Windows;

namespace AthleticsCertificateMaker.GenerateCertificateApp;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {Assembly.GetExecutingAssembly().GetName().Version}";
    }
}
