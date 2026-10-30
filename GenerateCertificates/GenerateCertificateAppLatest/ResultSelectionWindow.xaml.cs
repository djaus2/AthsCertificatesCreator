using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AthleticsCertificateMaker.GenerateCertificateApp;

public partial class ResultSelectionWindow : Window
{
    public string? SelectedResult { get; private set; }

    public ResultSelectionWindow(IEnumerable<string> items, string? heading = null)
    {
        InitializeComponent();
        ResultsList.ItemsSource = items.ToList();

        if (!string.IsNullOrWhiteSpace(heading))
        {
            Title = $"Select Result - {heading}";
            PromptText.Text = heading;
        }
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not string selected || string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        SelectedResult = selected;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
