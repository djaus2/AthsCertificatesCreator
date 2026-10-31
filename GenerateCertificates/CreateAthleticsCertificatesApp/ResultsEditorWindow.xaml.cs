using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AthleticsCertificateMaker.GenerateCertificateApp.Models;
using AthleticsCertificateMaker.GenerateCertificateApp.Services;

namespace AthleticsCertificateMaker.GenerateCertificateApp;

public partial class ResultsEditorWindow : Window
{
    private string? _resultsPath;
    private string _resultsDelimiter = ",";
    private readonly string _delimiterMode;

    public ResultsEditorWindow(string? resultsPath = null, string? delimiterMode = null)
    {
        InitializeComponent();
        _delimiterMode = string.IsNullOrWhiteSpace(delimiterMode) ? "Auto" : delimiterMode;

        if (!string.IsNullOrWhiteSpace(resultsPath) && File.Exists(resultsPath))
        {
            LoadResults(resultsPath);
        }
        else
        {
            LblResultsPath.Text = "(no results file)";
        }
    }

    private void LoadResults(string path)
    {
        try
        {
            _resultsDelimiter = ParticipantCsvLoader.ResolveDelimiter(path, _delimiterMode);
            GridResults.ItemsSource = new ObservableCollection<ResultRow>(
                ParticipantCsvLoader.LoadRawRows(path, _delimiterMode));
            _resultsPath = path;
            LblResultsPath.Text = path;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Load results failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnLoadResults_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Results CSV/TSV",
            Filter = "CSV/TSV Files (*.csv;*.tsv;*.txt)|*.csv;*.tsv;*.txt|All files (*.*)|*.*",
            FileName = Path.GetFileName(_resultsPath) ?? string.Empty
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadResults(dialog.FileName);
        }
    }

    private void GridResults_BeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.Item is not ResultRow row || row.No?.TrimStart().StartsWith("$") != true)
        {
            return;
        }

        if (e.Column is DataGridBoundColumn column
            && column.Binding is Binding binding
            && binding.Path.Path != nameof(ResultRow.No))
        {
            e.Cancel = true;
        }
    }

    private void GridResults_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit
            || e.Row.Item is not ResultRow row
            || e.Column is not DataGridBoundColumn column
            || column.Binding is not Binding binding
            || binding.Path.Path != nameof(ResultRow.No)
            || e.EditingElement is not TextBox textBox)
        {
            return;
        }

        var newNo = textBox.Text.Trim();
        var wasHeading = row.No?.TrimStart().StartsWith('$') == true;

        if (wasHeading && !newNo.StartsWith('$'))
        {
            MessageBox.Show(this, "A heading line must start with '$'.",
                "Invalid heading", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Cancel = true;
            return;
        }

        var hasOtherValues = !string.IsNullOrWhiteSpace(row.Name)
            || !string.IsNullOrWhiteSpace(row.Email)
            || !string.IsNullOrWhiteSpace(row.Distance)
            || !string.IsNullOrWhiteSpace(row.Time);

        if (newNo.StartsWith('$') && hasOtherValues)
        {
            var confirm = MessageBox.Show(this,
                "A heading line cannot have values in other columns. Clear them?",
                "Clear other columns", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            row.Name = row.Email = row.Distance = row.Time = string.Empty;
            Dispatcher.BeginInvoke(new Action(() => GridResults.Items.Refresh()));
        }
    }

    private void BtnClearResults_Click(object sender, RoutedEventArgs e)
    {
        if (GridResults.ItemsSource is null)
        {
            return;
        }

        var confirm = MessageBox.Show(this, "Clear all result rows?",
            "Clear results", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm == MessageBoxResult.Yes)
        {
            GridResults.ItemsSource = new ObservableCollection<ResultRow>();
        }
    }

    private void BtnSaveResults_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_resultsPath) || GridResults.ItemsSource is null)
        {
            MessageBox.Show(this, "No results file loaded.", "Save results",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var rows = new List<ResultRow>();
            foreach (var item in GridResults.Items)
            {
                if (item is ResultRow row)
                {
                    rows.Add(row);
                }
            }

            ParticipantCsvLoader.SaveRows(_resultsPath, rows, _resultsDelimiter);
            MessageBox.Show(this, $"Results saved to {_resultsPath}", "Save results",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save results failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static ResultRow? ContextResultRow(object sender)
        => ((sender as MenuItem)?.Parent as ContextMenu)?.PlacementTarget is DataGridRow row
            ? row.DataContext as ResultRow
            : null;

    private void ResultsClearLine_Click(object sender, RoutedEventArgs e)
    {
        if (ContextResultRow(sender) is not { } row)
        {
            return;
        }

        row.No = row.Name = row.Email = row.Distance = row.Time = string.Empty;
        GridResults.Items.Refresh();
    }

    private void ResultsDeleteLine_Click(object sender, RoutedEventArgs e)
    {
        if (ContextResultRow(sender) is not { } row
            || GridResults.ItemsSource is not ObservableCollection<ResultRow> items)
        {
            return;
        }

        items.Remove(row);
    }

    private void ResultsInsertLine_Click(object sender, RoutedEventArgs e)
    {
        if (ContextResultRow(sender) is not { } row
            || GridResults.ItemsSource is not ObservableCollection<ResultRow> items)
        {
            return;
        }

        items.Insert(items.IndexOf(row), new ResultRow());
    }
}
