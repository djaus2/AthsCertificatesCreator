using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using AthleticsCertificateMaker.EditorUI.Models;

namespace AthleticsCertificateMaker.EditorUI
{
    public partial class MainWindow : Window
    {
        private string? _currentPath;
        private CertificateModel _model = new CertificateModel();

        public MainWindow()
        {
            InitializeComponent();
            NewModel();
        }

        private void NewModel()
        {
            _model = new CertificateModel
            {
                CertificateTemplateFile = "DoncasterCertificateTemplate2.png",
                ParticipantCsvFile = "results.csv",
                ParticipantCsvDelimiter = "Auto",
                SendUsingAccount = "djaus@sportronics.com.au",
                DefaultTextColor = "Red",
                DefaultLabelColor = "LightGreen",
                DefaultValueColor = "Yellow",
                DefaultLabelTextSize = 70,
                DefaultDataTextSize = 90,
                CertificateSmoothingMode = "HighQuality",
                CertificateTextRenderingHint = "AntiAliasGridFit",
                ShowLocation = false,
                ShowHost = true,
                CertificateText = new System.Collections.Generic.List<TextItem>
                {
                    new TextItem { Name = "Title", Text = "1 HOUR TRACK RUN", Color = "Red", Font = "Times New Roman", Size = 85, Style = "Bold", Y = 70 },
                    new TextItem { Name = "Location", Text = "Doncaster Athletics Centre", Color = "White", Font = "Arial Narrow", Size = 38, Style = "Regular", Y = 320 }
                },
                CertificateFields = new CertificateFields
                {
                    Standard = new System.Collections.Generic.List<FieldItem>
                    {
                        new FieldItem { Name = "Name", Label = "Name:", LabelFont = "Arial", LabelSize = 70, LabelStyle = "Bold", LabelColor = "LightGreen", LabelY = 800, ValueFont = "Arial", ValueSize = 90, ValueStyle = "Regular", ValueColor = "Yellow", ValueY = 900 }
                    },
                    Time = new System.Collections.Generic.List<FieldItem>
                    {
                        new FieldItem { Name = "Name", Label = "Name", LabelFont = "Arial", LabelSize = 70, LabelStyle = "Bold", LabelColor = "LightGreen", LabelY = 800, ValueFont = "Arial", ValueSize = 90, ValueStyle = "Regular", ValueColor = "Yellow", ValueY = 900 }
                    }
                }
            };

            BindModelToUi();
            _currentPath = null;
            LblPath.Text = "(new)";
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
            ChkShowLocation.IsChecked = _model.ShowLocation ?? false;
            ChkShowHost.IsChecked = _model.ShowHost ?? false;

            GridText.ItemsSource = new ObservableCollection<TextItem>(_model.CertificateText ?? new System.Collections.Generic.List<TextItem>());
            GridStandard.ItemsSource = new ObservableCollection<FieldItem>(_model.CertificateFields?.Standard ?? new System.Collections.Generic.List<FieldItem>());
            GridTime.ItemsSource = new ObservableCollection<FieldItem>(_model.CertificateFields?.Time ?? new System.Collections.Generic.List<FieldItem>());
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
                try
                {
                    var txt = File.ReadAllText(dlg.FileName);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var model = JsonSerializer.Deserialize<CertificateModel>(txt, options);
                    if (model != null)
                    {
                        _model = model;
                        BindModelToUi();
                        _currentPath = dlg.FileName;
                        LblPath.Text = _currentPath;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                // update model from UI
                _model.CertificateTemplateFile = TxtTemplateFile.Text;
                _model.ParticipantCsvFile = TxtCsvFile.Text;
                _model.ParticipantCsvDelimiter = TxtDelimiter.Text;
                _model.SendUsingAccount = TxtSendUsing.Text;
                _model.DefaultTextColor = TxtDefaultTextColor.Text;
                _model.DefaultLabelColor = TxtDefaultLabelColor.Text;
                _model.DefaultValueColor = TxtDefaultValueColor.Text;
                _model.ShowLocation = ChkShowLocation.IsChecked;
                _model.ShowHost = ChkShowHost.IsChecked;

                // collect grids
                _model.CertificateText = new System.Collections.Generic.List<TextItem>();
                foreach (var it in GridText.Items)
                {
                    if (it is TextItem ti) _model.CertificateText.Add(ti);
                }

                _model.CertificateFields = new CertificateFields();
                _model.CertificateFields.Standard = new System.Collections.Generic.List<FieldItem>();
                foreach (var it in GridStandard.Items)
                {
                    if (it is FieldItem fi) _model.CertificateFields.Standard.Add(fi);
                }
                _model.CertificateFields.Time = new System.Collections.Generic.List<FieldItem>();
                foreach (var it in GridTime.Items)
                {
                    if (it is FieldItem fi) _model.CertificateFields.Time.Add(fi);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var txt = JsonSerializer.Serialize(_model, options);
                File.WriteAllText(path, txt);
                _currentPath = path;
                LblPath.Text = path;
                MessageBox.Show(this, "Saved", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
