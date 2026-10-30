using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Text.Json;
using Microsoft.Win32;
using System.Linq;
using AthleticsCertificateMaker.GenerateCertificateApp.Models;
using AthleticsCertificateMaker.GenerateCertificateApp.Services;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;

namespace AthleticsCertificateMaker.GenerateCertificateApp;

public partial class MainWindow : Window
{
    private enum CertificateRunMode
    {
        BuildOnly,
        BuildAndSave,
        BuildAndSend
    }

    private readonly AppRuntimeSettingsService _appSettingsService = new();

    private string _settingsPath = string.Empty;
    private string _resultsPath = string.Empty;
    private string _templatePath = string.Empty;
    private CertificateConfig? _config;


    public MainWindow()
    {
        InitializeComponent();
        InitializeDefaults();

        UpdateSettingsDependentMenusState();
    }

    private void InitializeDefaults()
    {
        var appSettings = _appSettingsService.Load();

        var settingsPath = appSettings.SettingsJsonPath;
        if (string.IsNullOrWhiteSpace(settingsPath) || !File.Exists(settingsPath))
        {
            settingsPath = Path.Combine(Environment.CurrentDirectory, "CertificateText.json");
        }

        if (File.Exists(settingsPath))
        {
            LoadSettings(settingsPath);
            if (!string.IsNullOrWhiteSpace(appSettings.ResultsPath))
            {
                _resultsPath = appSettings.ResultsPath;
            }

            if (!string.IsNullOrWhiteSpace(appSettings.TemplatePath))
            {
                _templatePath = appSettings.TemplatePath;
            }

            UpdatePathTextBoxes();
        }
        else
        {
            Log("Select File > Get Settings from JSON to choose CertificateText.json.");
        }
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        => SelectSettingsFile();

    private void OpenResultsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Results CSV/TSV",
            Filter = "CSV/TSV Files (*.csv;*.tsv;*.txt)|*.csv;*.tsv;*.txt|All files (*.*)|*.*",
            FileName = Path.GetFileName(_resultsPath)
        };

        if (dialog.ShowDialog(this) == true)
        {
            _resultsPath = dialog.FileName;
            UpdatePathTextBoxes();
            PersistAppSettings();
            Log($"Results file selected: {_resultsPath}");
        }
    }

    private void SelectTemplateMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select certificate template image",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
            FileName = Path.GetFileName(_templatePath)
        };

        if (dialog.ShowDialog(this) == true)
        {
            _templatePath = dialog.FileName;
            UpdatePathTextBoxes();
            PersistAppSettings();
            Log($"Template selected: {_templatePath}");
        }
    }

    private void SaveSettingsToJsonMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_config is null || string.IsNullOrWhiteSpace(_settingsPath))
            {
                throw new InvalidOperationException("Settings are not loaded.");
            }

            _config.CertificateTemplateFile = ToRelativeOrAbsolute(_settingsPath, _templatePath);
            _config.ParticipantCsvFile = ToRelativeOrAbsolute(_settingsPath, _resultsPath);

            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_settingsPath, json);
            Log($"Settings saved to JSON: {_settingsPath}");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Save settings failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearAppSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var confirmation = MessageBox.Show(
            this,
            "Clear saved app settings (last used JSON, results and template paths)?",
            "Clear App Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        _appSettingsService.Save(new AppRuntimeSettings());

        _settingsPath = string.Empty;
        _resultsPath = string.Empty;
        _templatePath = string.Empty;
        _config = null;

        SettingsPathTextBox.Text = string.Empty;
        ResultsPathTextBox.Text = string.Empty;
        TemplatePathTextBox.Text = string.Empty;
        CertificatesHeader.IsEnabled = false;

        SummaryTextBlock.Text = "App settings cleared";
        Log("App runtime settings cleared.");
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        => Close();

    private void BuildCertificatesMenuItem_Click(object sender, RoutedEventArgs e)
        => RunCertificateBuild(CertificateRunMode.BuildOnly);

    private void BuildAndSaveCertificatesMenuItem_Click(object sender, RoutedEventArgs e)
        => RunCertificateBuild(CertificateRunMode.BuildAndSave);

    private void BuildAndSendCertificatesMenuItem_Click(object sender, RoutedEventArgs e)
        => RunCertificateBuild(CertificateRunMode.BuildAndSend);

    private void SendDraftsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_config is null)
            {
                throw new InvalidOperationException("Settings are not loaded. Use File > Get Settings from JSON.");
            }

            var confirm = MessageBox.Show(
                this,
                "Send all certificate drafts created by this app?",
                "Confirm send drafts",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var count = OutlookEmailService.SendMarkedDrafts(_config.SendUsingAccount, Log);
            SummaryTextBlock.Text = $"Drafts sent: {count}";
            Log($"Drafts sent: {count}");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Send Drafts failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearDraftsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var confirm = MessageBox.Show(
                this,
                "Delete all certificate drafts created by this app?",
                "Confirm clear drafts",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var count = OutlookEmailService.ClearMarkedDrafts(Log);
            SummaryTextBlock.Text = $"Drafts cleared: {count}";
            Log($"Drafts cleared: {count}");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Clear Drafts failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyLogMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(LogTextBox.Text ?? string.Empty);
        SummaryTextBlock.Text = "Log copied";
    }

    private void ClearLogMenuItem_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
        SummaryTextBlock.Text = "Log cleared";
    }

    private void BrowseGeneratedCertificatesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var outputFolder = Path.Combine(GetRootFolder(), "Output", "PNG");
            if (!Directory.Exists(outputFolder))
            {
                throw new DirectoryNotFoundException($"Generated certificates folder not found: {outputFolder}");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = outputFolder,
                UseShellExecute = true
            });

            Log($"Opened generated certificates folder: {outputFolder}");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Browse generated certificates failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CleanMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var confirmation = MessageBox.Show(
                this,
                "Remove generated output files?",
                "Clear generated files",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            var rootFolder = GetRootFolder();
            Log($"Cleaning generated files under: {rootFolder}");

            RunClean(rootFolder);

            Log("Clear completed.");
            SummaryTextBlock.Text = "Cleared";
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Clear failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
        => RunCertificateBuild(CertificateRunMode.BuildOnly);

    private void RunCertificateBuild(CertificateRunMode mode)
        => RunCertificateBuild(mode, _resultsPath);

    private void RunCertificateBuild(CertificateRunMode mode, string resultsPath)
    {
        try
        {
            if (!EnsureSettingsLoadedInMemory())
            {
                throw new InvalidOperationException("Settings are not loaded. Use File > Get Settings from JSON.");
            }

            if (string.IsNullOrWhiteSpace(resultsPath) || !File.Exists(resultsPath))
            {
                throw new FileNotFoundException("Results file not found. Use File > Select Results...", resultsPath);
            }

            if (string.IsNullOrWhiteSpace(_templatePath) || !File.Exists(_templatePath))
            {
                throw new FileNotFoundException("Template image file not found.", _templatePath);
            }

            SummaryTextBlock.Text = mode switch
            {
                CertificateRunMode.BuildAndSave => "Generating and saving drafts...",
                CertificateRunMode.BuildAndSend => "Generating and sending...",
                _ => "Generating..."
            };
            Log("Loading participants...");

            var participants = ParticipantCsvLoader.Load(resultsPath, _config.ParticipantCsvDelimiter);
            Log($"Participants loaded: {participants.Count}");

            var rootFolder = Path.GetDirectoryName(_settingsPath) ?? Environment.CurrentDirectory;
            var result = CertificateGenerator.Generate(rootFolder, _config, participants, Log, _templatePath);

            if (mode == CertificateRunMode.BuildAndSave || mode == CertificateRunMode.BuildAndSend)
            {
                if (mode == CertificateRunMode.BuildAndSend)
                {
                    var confirmSend = MessageBox.Show(
                        this,
                        "Build and Send will immediately send emails via Outlook. Continue?",
                        "Confirm send",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (confirmSend != MessageBoxResult.Yes)
                    {
                        SummaryTextBlock.Text = "Send cancelled";
                        Log("Send cancelled by user.");
                        return;
                    }
                }

                var deliveryMode = mode == CertificateRunMode.BuildAndSend
                    ? EmailDeliveryMode.Send
                    : EmailDeliveryMode.Save;

                var emailCount = OutlookEmailService.ProcessCertificates(
                    result.GeneratedCertificates,
                    result.ResultsCsvPath,
                    _config.SendUsingAccount,
                    deliveryMode,
                    Log);

                Log(mode == CertificateRunMode.BuildAndSend
                    ? $"Emails sent: {emailCount}"
                    : $"Drafts saved: {emailCount}");
            }

            SummaryTextBlock.Text = $"Done. Certificates: {result.CertificatesGenerated}";
            Log($"Completed. Output folder: {result.OutputFolder}");
            Log($"Official results CSV: {result.ResultsCsvPath}");
        }
        catch (Exception ex)
        {
            SummaryTextBlock.Text = "Failed";
            Log($"ERROR: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Generation failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            //GenerateButton.IsEnabled = true;
        }
    }

    private void SelectSettingsFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Certificate Settings JSON",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = Path.GetFileName(_settingsPath)
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadSettings(dialog.FileName);
            PersistAppSettings();
        }
    }

    private void LoadSettings(string settingsPath)
    {
        _config = CertificateConfigLoader.Load(settingsPath);
        _settingsPath = settingsPath;

        _templatePath = ResolvePath(_settingsPath, _config.CertificateTemplateFile);

        var configuredResultsPath = ResolvePath(_settingsPath, _config.ParticipantCsvFile);
        if (File.Exists(configuredResultsPath))
        {
            _resultsPath = configuredResultsPath;
        }
        else if (string.IsNullOrWhiteSpace(_resultsPath))
        {
            _resultsPath = configuredResultsPath;
        }

        UpdatePathTextBoxes();

        Log($"Settings loaded: {_settingsPath}");
        Log($"Configured ParticipantCsvFile: {_config.ParticipantCsvFile}");
        Log($"Configured template: {_config.CertificateTemplateFile}");

        UpdateSettingsDependentMenusState();
    }

    private void UpdateSettingsDependentMenusState()
    {
        var settingsLoaded = _config != null;

        if (CertificatesHeader != null)
        {
            CertificatesHeader.IsEnabled = settingsLoaded;
        }

        if (EditMenu != null)
        {
            EditMenu.IsEnabled = settingsLoaded;
        }
    }

    private void UpdatePathTextBoxes()
    {
        SettingsPathTextBox.Text = _settingsPath;
        ResultsPathTextBox.Text = _resultsPath;
        TemplatePathTextBox.Text = _templatePath;
        CertificatesHeader.IsEnabled = true;
    }

    private void PersistAppSettings()
    {
        _appSettingsService.Save(new AppRuntimeSettings
        {
            SettingsJsonPath = _settingsPath,
            ResultsPath = _resultsPath,
            TemplatePath = _templatePath
        });
    }

    private static string ResolvePath(string settingsPath, string filePath)
    {
        if (Path.IsPathRooted(filePath))
        {
            return filePath;
        }

        var root = Path.GetDirectoryName(settingsPath) ?? Environment.CurrentDirectory;
        return Path.Combine(root, filePath);
    }

    private string GetRootFolder()
    {
        if (!string.IsNullOrWhiteSpace(_settingsPath))
        {
            return Path.GetDirectoryName(_settingsPath) ?? Environment.CurrentDirectory;
        }

        return Environment.CurrentDirectory;
    }

    private static string ToRelativeOrAbsolute(string settingsPath, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return string.Empty;
        }

        var settingsDir = Path.GetDirectoryName(settingsPath) ?? Environment.CurrentDirectory;
        var settingsDirFull = Path.GetFullPath(settingsDir)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var candidateFull = Path.GetFullPath(fullPath);

        if (candidateFull.StartsWith(settingsDirFull, StringComparison.InvariantCultureIgnoreCase))
        {
            return Path.GetRelativePath(settingsDir, candidateFull);
        }

        return candidateFull;
    }

    private void RunClean(string repoRoot)
    {
        var clearFolders = new[] { "Output", "Results", "logs" };
        var deletePaths = new[] { "," };
        var keepNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "004-Fred Nurk.png"
        };

        foreach (var folder in clearFolders)
        {
            var folderPath = Path.Combine(repoRoot, folder);
            if (!Directory.Exists(folderPath))
            {
                continue;
            }

            foreach (var filePath in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(filePath);
                if (name is not null && keepNames.Contains(name))
                {
                    continue;
                }

                RemoveRepoItem(filePath, repoRoot, recurse: false);
            }
        }

        foreach (var relPath in deletePaths)
        {
            var fullPath = Path.Combine(repoRoot, relPath);
            if (File.Exists(fullPath))
            {
                RemoveRepoItem(fullPath, repoRoot, recurse: false);
            }
            else if (Directory.Exists(fullPath))
            {
                RemoveRepoItem(fullPath, repoRoot, recurse: true);
            }
        }

        foreach (var rootFile in Directory.GetFiles(repoRoot, "~$*.xlsx", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(rootFile);
            if (name is not null && keepNames.Contains(name))
            {
                continue;
            }

            RemoveRepoItem(rootFile, repoRoot, recurse: false);
        }
    }

    private void RemoveRepoItem(string fullPath, string repoRoot, bool recurse)
    {
        var fullRepoPath = Path.GetFullPath(repoRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullItemPath = Path.GetFullPath(fullPath);

        if (!fullItemPath.StartsWith(fullRepoPath, StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        try
        {
            if (Directory.Exists(fullItemPath))
            {
                Directory.Delete(fullItemPath, recurse);
                Log($"Removed: {fullItemPath}");
            }
            else if (File.Exists(fullItemPath))
            {
                File.Delete(fullItemPath);
                Log($"Removed: {fullItemPath}");
            }
        }
        catch (Exception ex)
        {
            Log($"ERROR removing '{fullItemPath}': {ex.Message}");
        }
    }

    private void Log(string message)
    {
        LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };

        aboutWindow.ShowDialog();
    }

    private void SelectGeneratedCertificate_Click(object sender, RoutedEventArgs e)
    {
        var preferredPngDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "source", "repos", "AthsCertificateEditors", "Output", "PNG");

        var generatedDir = Path.Combine(AppContext.BaseDirectory, "GeneratedCertificates");
        var generatedPngDir = Path.Combine(generatedDir, "PNG");

        try
        {
            var defaultDir = Directory.Exists(preferredPngDir)
                ? preferredPngDir
                : (Directory.Exists(generatedPngDir) ? generatedPngDir : generatedDir);

            var dialog = new OpenFileDialog
            {
                Title = "Select Generated Certificate",
                InitialDirectory = Directory.Exists(defaultDir) ? defaultDir : AppContext.BaseDirectory,
                Filter = "Certificate files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var viewer = new CertificateViewerWindow(dialog.FileName)
            {
                Owner = this
            };
            viewer.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open certificate preview. {ex.Message}", "Select Certificate", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditMenu_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        UpdateSelectResultMenuState();
    }

    private void UpdateSelectResultMenuState()
    {
        if (SelectResultMenuItem == null)
        {
            return;
        }

        var configuredPath = GetConfiguredResultsFilePath();
        SelectResultMenuItem.IsEnabled = !string.IsNullOrWhiteSpace(configuredPath);
    }

    private string? GetConfiguredResultsFilePath()
    {
        return ResultsPathTextBox?.Text?.Trim();
    }

    private const string DefaultResultHeading = "Default";

    private sealed class ResultSection
    {
        public string Heading { get; init; } = DefaultResultHeading;
        public List<string> Rows { get; } = new();
    }

    private void SelectResult_Click(object sender, RoutedEventArgs e)
    {
        var resultsFilePath = GetConfiguredResultsFilePath();

        if (string.IsNullOrWhiteSpace(resultsFilePath))
        {
            MessageBox.Show("Results CSV/TSV path is blank.", "Select Result", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateSelectResultMenuState();
            return;
        }

        if (!File.Exists(resultsFilePath))
        {
            MessageBox.Show("Configured Results CSV/TSV file was not found.", "Select Result", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var sections = LoadResultSections(resultsFilePath);
            if (sections.Count == 0)
            {
                MessageBox.Show("No selectable results were found in that file.", "Select Result", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ResultSection selectedSection;

            if (sections.Count == 1)
            {
                selectedSection = sections[0];
            }
            else
            {
                var headingPicker = new HeadingSelectionWindow(sections.Select(s => s.Heading))
                {
                    Owner = this
                };

                if (headingPicker.ShowDialog() != true || string.IsNullOrWhiteSpace(headingPicker.SelectedHeading))
                {
                    return;
                }

                selectedSection = sections.First(s => string.Equals(s.Heading, headingPicker.SelectedHeading, StringComparison.Ordinal));
            }

            var resultPicker = new ResultSelectionWindow(selectedSection.Rows, selectedSection.Heading)
            {
                Owner = this
            };

            if (resultPicker.ShowDialog() != true || string.IsNullOrWhiteSpace(resultPicker.SelectedResult))
            {
                return;
            }

            var generatedAfter = DateTime.UtcNow;
            var tempSingleResultsPath = CreateSingleResultTempFile(resultsFilePath, resultPicker.SelectedResult, selectedSection.Heading);
            try
            {
                RunCertificateBuild(CertificateRunMode.BuildOnly, tempSingleResultsPath);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempSingleResultsPath))
                    {
                        File.Delete(tempSingleResultsPath);
                    }
                }
                catch
                {
                }
            }

            var rootFolder = Path.GetDirectoryName(_settingsPath) ?? Environment.CurrentDirectory;
            var pngDir = Path.Combine(rootFolder, "Output", "PNG");
            var generatedPath = FindNewestGeneratedCertificate(pngDir, generatedAfter);

            if (string.IsNullOrWhiteSpace(generatedPath))
            {
                MessageBox.Show("Certificate generation completed, but no new output image was found in Output\\PNG.", "Select Result", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectedResult = resultPicker.SelectedResult;
            var selectedHeading = selectedSection.Heading;

            var viewer = new CertificateViewerWindow(
                generatedPath,
                () =>
                {
                    var generatedAfterNudge = DateTime.UtcNow;
                    var tempSingleResultsPathNudge = CreateSingleResultTempFile(resultsFilePath, selectedResult, selectedHeading);
                    try
                    {
                        RunCertificateBuild(CertificateRunMode.BuildOnly, tempSingleResultsPathNudge);
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(tempSingleResultsPathNudge))
                            {
                                File.Delete(tempSingleResultsPathNudge);
                            }
                        }
                        catch
                        {
                        }
                    }

                    var rootFolderNudge = Path.GetDirectoryName(_settingsPath) ?? Environment.CurrentDirectory;
                    var pngDirNudge = Path.Combine(rootFolderNudge, "Output", "PNG");
                    return FindNewestGeneratedCertificate(pngDirNudge, generatedAfterNudge);
                })
            {
                Owner = this
            };
            viewer.ShowDialog();
        }
        catch (Exception ex)
        {
            Log($"Select Result ERROR: {ex}");
            MessageBox.Show($"Could not select/generate certificate. {ex.Message}", "Select Result", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static List<ResultSection> LoadResultSections(string resultsFilePath)
    {
        var lines = File.ReadAllLines(resultsFilePath)
            .Select(l => l?.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Cast<string>()
            .ToList();

        if (lines.Count == 0)
        {
            return new List<ResultSection>();
        }

        var header = lines.FirstOrDefault(IsLikelyHeader);
        var sections = new List<ResultSection>();
        var currentHeading = DefaultResultHeading;

        foreach (var line in lines)
        {
            if (header != null && string.Equals(line, header, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsLikelyResultRow(line))
            {
                var section = sections.FirstOrDefault(s => string.Equals(s.Heading, currentHeading, StringComparison.Ordinal));
                if (section == null)
                {
                    section = new ResultSection { Heading = currentHeading };
                    sections.Add(section);
                }

                section.Rows.Add(line);
                continue;
            }

            // Non-result line becomes current heading for subsequent result rows.
            currentHeading = line;
        }

        return sections.Where(s => s.Rows.Count > 0).ToList();
    }

    private static string CreateSingleResultTempFile(string fullResultsPath, string selectedRow, string selectedHeading)
    {
        var lines = File.ReadAllLines(fullResultsPath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList();

        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Results file is empty.");
        }

        var header = lines.FirstOrDefault(IsLikelyHeader) ?? lines[0];
        var extension = Path.GetExtension(fullResultsPath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".csv";
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"single_result_{Guid.NewGuid():N}{extension}");

        var outputLines = new List<string> { header };

        if (!string.IsNullOrWhiteSpace(selectedHeading) && !string.Equals(selectedHeading, DefaultResultHeading, StringComparison.Ordinal))
        {
            outputLines.Add(selectedHeading);
        }

        outputLines.Add(selectedRow);
        File.WriteAllLines(tempPath, outputLines);
        return tempPath;
    }

    private static string? FindNewestGeneratedCertificate(string pngFolder, DateTime generatedAfterUtc)
    {
        if (!Directory.Exists(pngFolder))
        {
            return null;
        }

        var extensions = new[] { ".png", ".jpg", ".jpeg", ".bmp" };

        return Directory
            .EnumerateFiles(pngFolder, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => File.GetLastWriteTimeUtc(f) >= generatedAfterUtc)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static bool IsLikelyHeader(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var hasDelimiter = line.Contains(",") || line.Contains("\t");
        var hasKnownHeaderWord =
            line.Contains("name", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("id", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("time", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("position", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("email", StringComparison.OrdinalIgnoreCase);

        return hasDelimiter && hasKnownHeaderWord;
    }

    private static bool IsLikelyResultRow(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (IsLikelyHeader(line))
        {
            return false;
        }

        return line.Contains(",") || line.Contains("\t");
    }

    private bool EnsureSettingsLoadedInMemory()
    {
        if (_config != null)
        {
            return true;
        }

        var settingsPath = _settingsPath;

        // Fallback to UI textbox if available.
        if (string.IsNullOrWhiteSpace(settingsPath) && SettingsPathTextBox != null)
        {
            settingsPath = SettingsPathTextBox.Text?.Trim();
        }

        if (string.IsNullOrWhiteSpace(settingsPath) || !File.Exists(settingsPath))
        {
            return false;
        }

        LoadSettings(settingsPath);
        return _config != null;
    }

    public bool NudgeStandardFieldValueY(string fieldName, int delta)
    {
        if (_config == null)
        {
            Log("Nudge skipped: settings not loaded in memory.");
            return false;
        }

        static string Nk(string s)
            => new string((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        var configType = _config.GetType();
        var certificateFields = configType
            .GetProperty("CertificateFields", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(_config);

        if (certificateFields == null)
        {
            Log("Nudge failed: CertificateFields not found in _config.");
            return false;
        }

        object? standardSection = null;

        if (certificateFields is System.Collections.IDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                if (key == null)
                {
                    continue;
                }

                if (Nk(key.ToString() ?? string.Empty) == "standard")
                {
                    standardSection = dict[key];
                    break;
                }
            }
        }

        standardSection ??= certificateFields.GetType()
            .GetProperty("Standard", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(certificateFields);

        if (standardSection is not System.Collections.IEnumerable rows)
        {
            Log("Nudge failed: CertificateFields.Standard section not found.");
            return false;
        }

        foreach (var row in rows)
        {
            if (row == null)
            {
                continue;
            }

            var rowType = row.GetType();
            var nameProp = rowType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var valueYProp = rowType.GetProperty("ValueY", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (nameProp == null || valueYProp == null || !valueYProp.CanRead || !valueYProp.CanWrite)
            {
                continue;
            }

            var rowName = nameProp.GetValue(row)?.ToString() ?? string.Empty;
            if (!string.Equals(rowName, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var current = Convert.ToDouble(valueYProp.GetValue(row) ?? 0d, System.Globalization.CultureInfo.InvariantCulture);
            var updated = current + delta;

            var targetType = Nullable.GetUnderlyingType(valueYProp.PropertyType) ?? valueYProp.PropertyType;
            object converted = targetType == typeof(int)
                ? Convert.ToInt32(Math.Round(updated), System.Globalization.CultureInfo.InvariantCulture)
                : targetType == typeof(float)
                    ? Convert.ToSingle(updated, System.Globalization.CultureInfo.InvariantCulture)
                    : targetType == typeof(decimal)
                        ? Convert.ToDecimal(updated, System.Globalization.CultureInfo.InvariantCulture)
                        : Convert.ToDouble(updated, System.Globalization.CultureInfo.InvariantCulture);

            valueYProp.SetValue(row, converted);
            Log($"Nudged {fieldName}.ValueY: {current} -> {updated}");
            return true;
        }

        Log($"Nudge failed: field '{fieldName}' not found in CertificateFields.Standard.");
        return false;
    }

    public double? NudgeStandardFieldValueY(string fieldName)
    {
        if (_config == null)
        {
            return null;
        }

        static string Nk(string s)
            => new string((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        var certificateFields = _config.GetType()
            .GetProperty("CertificateFields", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(_config);

        if (certificateFields == null)
        {
            return null;
        }

        object? standardSection = null;

        if (certificateFields is System.Collections.IDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                if (key == null)
                {
                    continue;
                }

                if (Nk(key.ToString() ?? string.Empty) == "standard")
                {
                    standardSection = dict[key];
                    break;
                }
            }
        }

        standardSection ??= certificateFields.GetType()
            .GetProperty("Standard", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(certificateFields);

        if (standardSection is not System.Collections.IEnumerable rows)
        {
            return null;
        }

        foreach (var row in rows)
        {
            if (row == null)
            {
                continue;
            }

            var rowType = row.GetType();
            var nameProp = rowType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var valueYProp = rowType.GetProperty("ValueY", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (nameProp == null || valueYProp == null || !valueYProp.CanRead)
            {
                continue;
            }

            var rowName = nameProp.GetValue(row)?.ToString() ?? string.Empty;
            if (!string.Equals(rowName, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return Convert.ToDouble(valueYProp.GetValue(row) ?? 0d, System.Globalization.CultureInfo.InvariantCulture);
        }

        return null;
    }

    public void LogFromUi(string message)
    {
        Log(message);
    }

    public double? GetStandardFieldY(string targetKey)
    {
        if (_config == null)
        {
            return null;
        }

        ParseTargetKey(targetKey, out var fieldName, out var yPropertyName);

        var row = FindStandardRow(fieldName);
        if (row == null)
        {
            return null;
        }

        var yProp = row.GetType().GetProperty(yPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (yProp == null || !yProp.CanRead)
        {
            return null;
        }

        return Convert.ToDouble(yProp.GetValue(row) ?? 0d, System.Globalization.CultureInfo.InvariantCulture);
    }

    public bool NudgeStandardFieldY(string targetKey, int delta)
    {
        if (_config == null)
        {
            Log("Nudge skipped: settings not loaded in memory.");
            return false;
        }

        ParseTargetKey(targetKey, out var fieldName, out var yPropertyName);

        var row = FindStandardRow(fieldName);
        if (row == null)
        {
            Log($"Nudge failed: field '{fieldName}' not found in CertificateFields.Standard.");
            return false;
        }

        var yProp = row.GetType().GetProperty(yPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (yProp == null || !yProp.CanRead || !yProp.CanWrite)
        {
            Log($"Nudge failed: property '{yPropertyName}' not found/writable for '{fieldName}'.");
            return false;
        }

        var current = Convert.ToDouble(yProp.GetValue(row) ?? 0d, System.Globalization.CultureInfo.InvariantCulture);
        var updated = current + delta;

        var targetType = Nullable.GetUnderlyingType(yProp.PropertyType) ?? yProp.PropertyType;
        object converted = targetType == typeof(int)
            ? Convert.ToInt32(Math.Round(updated), System.Globalization.CultureInfo.InvariantCulture)
            : targetType == typeof(float)
                ? Convert.ToSingle(updated, System.Globalization.CultureInfo.InvariantCulture)
                : targetType == typeof(decimal)
                    ? Convert.ToDecimal(updated, System.Globalization.CultureInfo.InvariantCulture)
                    : Convert.ToDouble(updated, System.Globalization.CultureInfo.InvariantCulture);

        yProp.SetValue(row, converted);
        Log($"Nudged {fieldName}.{yPropertyName}: {current} -> {updated}");
        return true;
    }

    private object? FindStandardRow(string fieldName)
    {
        static string Nk(string s) => new string((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        var certificateFields = _config?.GetType()
            .GetProperty("CertificateFields", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(_config!);

        if (certificateFields == null)
        {
            return null;
        }

        object? standardSection = null;
        if (certificateFields is System.Collections.IDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                if (key != null && Nk(key.ToString() ?? string.Empty) == "standard")
                {
                    standardSection = dict[key];
                    break;
                }
            }
        }

        standardSection ??= certificateFields.GetType()
            .GetProperty("Standard", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(certificateFields);

        if (standardSection is not System.Collections.IEnumerable rows)
        {
            return null;
        }

        foreach (var row in rows)
        {
            if (row == null) continue;
            var nameProp = row.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var rowName = nameProp?.GetValue(row)?.ToString() ?? string.Empty;
            if (string.Equals(rowName, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                return row;
            }
        }

        return null;
    }

    private static void ParseTargetKey(string targetKey, out string fieldName, out string yPropertyName)
    {
        var parts = (targetKey ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        fieldName = parts.Length > 0 ? parts[0] : "Name";
        yPropertyName = parts.Length > 1 ? parts[1] : "ValueY";
    }
}
