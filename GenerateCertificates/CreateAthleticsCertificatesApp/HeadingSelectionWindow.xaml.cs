using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AthleticsCertificateMaker.GenerateCertificateApp;

public partial class HeadingSelectionWindow : Window
{
    public string? SelectedHeading { get; private set; }

    public HeadingSelectionWindow(IEnumerable<string> headings)
    {
        InitializeComponent();
        HeadingsList.ItemsSource = headings.ToList();
    }

    private void HeadingsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HeadingsList.SelectedItem is not string selected || string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        SelectedHeading = selected;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}