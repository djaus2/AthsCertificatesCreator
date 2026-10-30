using System.Reflection;
using System.Windows;

namespace AthleticsCertificateMaker.GenerateCertificateApp;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {Assembly.GetExecutingAssembly().GetName().Version}";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
