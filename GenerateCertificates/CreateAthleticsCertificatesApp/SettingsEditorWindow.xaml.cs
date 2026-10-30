using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using AthleticsCertificateMaker.GenerateCertificateApp.Models;
using AthleticsCertificateMaker.GenerateCertificateApp.Services;

namespace AthleticsCertificateMaker.GenerateCertificateApp;

public partial class SettingsEditorWindow : Window
{
    private string? _currentPath;
    private CertificateConfig _model = new();

    public string? SavedPath { get; private set; }

    public SettingsEditorWindow(string? initialPath = null)
    {
        InitializeComponent();

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
    }

    private void LoadFromPath(string path)
    {
        try
        {
            _model = CertificateConfigLoader.Load(path);
            BindModelToUi();
            _currentPath = path;
            LblPath.Text = _currentPath;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Error);
            NewModel();
        }
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
            MessageBox.Show(this, "Saved", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
