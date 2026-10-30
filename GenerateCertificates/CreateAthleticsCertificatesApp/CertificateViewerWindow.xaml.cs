using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AthleticsCertificateMaker.GenerateCertificateApp;

public partial class CertificateViewerWindow : Window
{
    private readonly Func<string?>? _regenerateCertificate;

    public CertificateViewerWindow(string certificatePath, Func<string?>? regenerateCertificate = null)
    {
        InitializeComponent();
        _regenerateCertificate = regenerateCertificate;
        LoadCertificate(certificatePath);
    }

    private void LoadCertificate(string certificatePath)
    {
        if (string.IsNullOrWhiteSpace(certificatePath) || !File.Exists(certificatePath))
        {
            throw new FileNotFoundException("Certificate image not found.", certificatePath);
        }

        FileNameText.Text = Path.GetFileName(certificatePath);

        var bytes = File.ReadAllBytes(certificatePath);
        using var ms = new MemoryStream(bytes);

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.None;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();

        CertificateImage.Source = image;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void NudgeButton_Click(object sender, RoutedEventArgs e)
    {
        var main = Application.Current?.MainWindow as MainWindow;
        if (main == null)
        {
            return;
        }

        if (!int.TryParse((sender as FrameworkElement)?.Tag?.ToString(), out var delta))
        {
            return;
        }

        var selectedTarget = NudgeTargetCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
        var target = selectedTarget?.Tag?.ToString() ?? "Name.ValueY";

        var before = main.GetStandardFieldY(target);
        main.LogFromUi($"{target} before nudge: {(before.HasValue ? before.Value.ToString() : "null")}");

        var nudged = main.NudgeStandardFieldY(target, delta);
        if (!nudged)
        {
            return;
        }

        var after = main.GetStandardFieldY(target);
        main.LogFromUi($"{target} after nudge: {(after.HasValue ? after.Value.ToString() : "null")}");

        if (_regenerateCertificate == null)
        {
            return;
        }

        var updatedPath = _regenerateCertificate();
        main.LogFromUi($"Regenerated certificate path: {updatedPath ?? "(null)"}");

        if (!string.IsNullOrWhiteSpace(updatedPath) && File.Exists(updatedPath))
        {
            LoadCertificate(updatedPath);
        }
        else
        {
            main.LogFromUi("Regenerate callback did not return a valid file path.");
        }
    }
}
