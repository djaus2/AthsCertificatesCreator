using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AthleticsCertificateMaker.GenerateCertificateApp.Models;
using AthleticsCertificateMaker.GenerateCertificateApp.Services;

namespace AthleticsCertificateMaker.GenerateCertificateApp;

public partial class SettingsEditorWindow : Window
{
    private string? _currentPath;
    private CertificateConfig _model = new();
    private string? _resultsPath;
    private string _resultsDelimiter = ",";
    private readonly string? _explicitResultsPath;

    public string? SavedPath { get; private set; }

    public SettingsEditorWindow(string? initialPath = null, string? resultsPath = null)
    {
        InitializeComponent();
        _explicitResultsPath = resultsPath;

        if (!string.IsNullOrWhiteSpace(initialPath) && File.Exists(initialPath))
        {
            LoadFromPath(initialPath);
        }
        else
        {
            NewModel();
        }
    }

    private void NewModel()
    {
        _model = new CertificateConfig
        {
            CertificateTemplateFile = "CertificateTemplate.png",
            ParticipantCsvFile = "results.csv",
            ParticipantCsvDelimiter = "Auto",
            SendUsingAccount = "onehour@somewhere.com.au",
            DefaultTextColor = "Red",
            DefaultLabelColor = "LightGreen",
            DefaultValueColor = "Yellow",
            CertificateSmoothingMode = "HighQuality",
            CertificateTextRenderingHint = "AntiAliasGridFit",
            ShowLocation = false,
            ShowHost = true,
            CertificateText = new List<CertificateTextItem>
            {
                new() { Name = "Title", Text = "1 HOUR TRACK RUN", Color = "Red", Font = "Times New Roman", Size = 85, Style = "Bold", Y = 70 },
                new() { Name = "Location", Text = "Somewhere Athletics Centre", Color = "White", Font = "Arial Narrow", Size = 38, Style = "Regular", Y = 320 }
            },
            CertificateFields = new CertificateFieldsConfig
            {
                Standard = new List<CertificateFieldItem>
                {
                    new() { Name = "Name", Label = "Name:", LabelFont = "Arial", LabelSize = 70, LabelStyle = "Bold", LabelColor = "LightGreen", LabelY = 800, ValueFont = "Arial", ValueSize = 90, ValueStyle = "Regular", ValueColor = "Yellow", ValueY = 900 }
                },
                Time = new List<CertificateFieldItem>
                {
                    new() { Name = "Name", Label = "Name", LabelFont = "Arial", LabelSize = 70, LabelStyle = "Bold", LabelColor = "LightGreen", LabelY = 800, ValueFont = "Arial", ValueSize = 90, ValueStyle = "Regular", ValueColor = "Yellow", ValueY = 900 }
                }
            }
        };

        BindModelToUi();
        _currentPath = null;
        LblPath.Text = "(new)";

        _resultsPath = null;
        GridResults.ItemsSource = null;
        LblResultsPath.Text = "(none)";
    }

    private void LoadFromPath(string path)
    {
        try
        {
            _model = CertificateConfigLoader.Load(path);
            BindModelToUi();
            _currentPath = path;
            LblPath.Text = _currentPath;
            AutoLoadResults();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Error);
            NewModel();
        }
    }

    private void AutoLoadResults()
    {
        var path = _explicitResultsPath;
        if (string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(_model.ParticipantCsvFile) && _currentPath is not null)
        {
            path = Path.IsPathRooted(_model.ParticipantCsvFile)
                ? _model.ParticipantCsvFile
                : Path.Combine(Path.GetDirectoryName(_currentPath) ?? string.Empty, _model.ParticipantCsvFile);
        }

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            LoadResults(path);
        }
        else
        {
            _resultsPath = null;
            GridResults.ItemsSource = null;
            LblResultsPath.Text = "(no results file)";
        }
    }

    private void LoadResults(string path)
    {
        try
        {
            var mode = _model.ParticipantCsvDelimiter ?? "Auto";
            _resultsDelimiter = ParticipantCsvLoader.ResolveDelimiter(path, mode);
            GridResults.ItemsSource = new ObservableCollection<ResultRow>(
                ParticipantCsvLoader.LoadRawRows(path, mode));
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
        var dlg = new OpenFileDialog { Filter = "CSV/TSV Files (*.csv;*.tsv;*.txt)|*.csv;*.tsv;*.txt|All files (*.*)|*.*" };
        if (dlg.ShowDialog(this) == true)
        {
            LoadResults(dlg.FileName);
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
            SaveResults();
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

    private void SaveResults()
    {
        if (string.IsNullOrWhiteSpace(_resultsPath) || GridResults.ItemsSource is null)
        {
            return;
        }

        var rows = new List<ResultRow>();
        foreach (var it in GridResults.Items)
        {
            if (it is ResultRow rr) rows.Add(rr);
        }

        ParticipantCsvLoader.SaveRows(_resultsPath, rows, _resultsDelimiter);
    }

    private void BindModelToUi()
    {
        TxtTemplateFile.Text = _model.CertificateTemplateFile ?? string.Empty;
        TxtCsvFile.Text = _model.ParticipantCsvFile ?? string.Empty;
        TxtDelimiter.Text = _model.ParticipantCsvDelimiter ?? string.Empty;
        TxtSendUsing.Text = _model.SendUsingAccount ?? string.Empty;
        TxtDefaultTextColor.Text = _model.DefaultTextColor ?? string.Empty;
        TxtDefaultLabelColor.Text = _model.DefaultLabelColor ?? string.Empty;
        TxtDefaultValueColor.Text = _model.DefaultValueColor ?? string.Empty;
        ChkShowLocation.IsChecked = _model.ShowLocation;
        ChkShowHost.IsChecked = _model.ShowHost;

        GridText.ItemsSource = new ObservableCollection<CertificateTextItem>(_model.CertificateText ?? new List<CertificateTextItem>());
        GridStandard.ItemsSource = new ObservableCollection<CertificateFieldItem>(_model.CertificateFields?.Standard ?? new List<CertificateFieldItem>());
        GridTime.ItemsSource = new ObservableCollection<CertificateFieldItem>(_model.CertificateFields?.Time ?? new List<CertificateFieldItem>());
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        NewModel();
    }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" };
        if (dlg.ShowDialog(this) == true)
        {
            LoadFromPath(dlg.FileName);
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentPath))
        {
            BtnSaveAs_Click(sender, e);
            return;
        }
        SaveToPath(_currentPath);
    }

    private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*", FileName = "CertificateText.json" };
        if (dlg.ShowDialog(this) == true)
        {
            SaveToPath(dlg.FileName);
        }
    }

    private void SaveToPath(string path)
    {
        try
        {
            _model.CertificateTemplateFile = TxtTemplateFile.Text;
            _model.ParticipantCsvFile = TxtCsvFile.Text;
            _model.ParticipantCsvDelimiter = TxtDelimiter.Text;
            _model.SendUsingAccount = TxtSendUsing.Text;
            _model.DefaultTextColor = TxtDefaultTextColor.Text;
            _model.DefaultLabelColor = TxtDefaultLabelColor.Text;
            _model.DefaultValueColor = TxtDefaultValueColor.Text;
            _model.ShowLocation = ChkShowLocation.IsChecked ?? false;
            _model.ShowHost = ChkShowHost.IsChecked ?? false;

            _model.CertificateText = new List<CertificateTextItem>();
            foreach (var it in GridText.Items)
            {
                if (it is CertificateTextItem ti) _model.CertificateText.Add(ti);
            }

            _model.CertificateFields = new CertificateFieldsConfig();
            _model.CertificateFields.Standard = new List<CertificateFieldItem>();
            foreach (var it in GridStandard.Items)
            {
                if (it is CertificateFieldItem fi) _model.CertificateFields.Standard.Add(fi);
            }
            _model.CertificateFields.Time = new List<CertificateFieldItem>();
            foreach (var it in GridTime.Items)
            {
                if (it is CertificateFieldItem fi) _model.CertificateFields.Time.Add(fi);
            }

            var txt = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, txt);
            _currentPath = path;
            SavedPath = path;
            LblPath.Text = path;

            SaveResults();

            MessageBox.Show(this, "Saved", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
