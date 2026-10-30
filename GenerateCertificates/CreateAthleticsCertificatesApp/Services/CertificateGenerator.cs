using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using AthleticsCertificateMaker.GenerateCertificateApp.Models;

namespace AthleticsCertificateMaker.GenerateCertificateApp.Services;

public static class CertificateGenerator
{
    public static GenerationResult Generate(
        string rootFolder,
        CertificateConfig config,
        IReadOnlyCollection<ParticipantRecord> participants,
        Action<string> log,
        string? templateOverridePath = null)
    {
        var templatePath = !string.IsNullOrWhiteSpace(templateOverridePath)
            ? templateOverridePath
            : MakePath(rootFolder, config.CertificateTemplateFile);
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException("Certificate template image not found.", templatePath);
        }

        var pngFolder = Path.Combine(rootFolder, "Output", "PNG");
        var resultsFolder = Path.Combine(rootFolder, "Results");
        Directory.CreateDirectory(pngFolder);
        Directory.CreateDirectory(resultsFolder);

        var officialResults = participants
            .Select(p => new[] { p.No, p.Heading, p.Name, p.Distance, p.Time })
            .ToList();

        var resultsCsvPath = Path.Combine(resultsFolder, "OfficialResults.csv");
        WriteOfficialResultsCsv(resultsCsvPath, officialResults);
        log($"Wrote results CSV: {resultsCsvPath}");

        var generated = 0;
        var generatedCertificates = new List<GeneratedCertificate>();
        var certificateHost = config.CertificateText
            .FirstOrDefault(i => string.Equals(i.Name, "Host", StringComparison.OrdinalIgnoreCase))?.Text ?? "Certificate";
        var certificateTitle = config.CertificateText
            .FirstOrDefault(i => string.Equals(i.Name, "Title", StringComparison.OrdinalIgnoreCase))?.Text ?? "Event";

        foreach (var participant in participants)
        {
            if (!string.IsNullOrWhiteSpace(participant.Time))
            {
                log($"Skipping timed result: {participant.Name} ({participant.Time})");
                continue;
            }

            if (string.Equals(participant.Distance?.Trim(), "DNS", StringComparison.OrdinalIgnoreCase))
            {
                log($"Skipping DNS certificate: {participant.Name}");
                continue;
            }

            var safeName = SanitizeFileName(participant.Name ?? string.Empty);
            var no = int.TryParse(participant.No, out var parsedNo) ? parsedNo.ToString("D3") : "000";
            var outputFile = Path.Combine(pngFolder, $"{no}-{safeName}.png");

            GenerateCertificateImage(templatePath, outputFile, config, participant);
            generated++;
            generatedCertificates.Add(new GeneratedCertificate
            {
                ParticipantName = participant.Name ?? string.Empty,
                Email = participant.Email ?? string.Empty,
                Distance = participant.Distance ?? string.Empty,
                Heading = participant.Heading ?? string.Empty,
                CertificateFilePath = outputFile,
                CertificateHost = certificateHost,
                EmailTitle = string.IsNullOrWhiteSpace(participant.Heading) ? certificateTitle : (participant.Heading ?? string.Empty)
            });
            log($"Created certificate: {outputFile}");
        }

        return new GenerationResult
        {
            CertificatesGenerated = generated,
            ResultsCsvPath = resultsCsvPath,
            OutputFolder = pngFolder,
            GeneratedCertificates = generatedCertificates
        };
    }

    private static void GenerateCertificateImage(
        string templatePath,
        string outputFile,
        CertificateConfig config,
        ParticipantRecord participant)
    {
        using var bitmap = new Bitmap(templatePath);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = ParseSmoothingMode(config.CertificateSmoothingMode);
        graphics.TextRenderingHint = ParseTextRenderingHint(config.CertificateTextRenderingHint);

        foreach (var item in config.CertificateText)
        {
            if (string.Equals(item.Name, "Location", StringComparison.OrdinalIgnoreCase) && !config.ShowLocation)
            {
                continue;
            }

            if (string.Equals(item.Name, "Host", StringComparison.OrdinalIgnoreCase) && !config.ShowHost)
            {
                continue;
            }

            var text = item.Text ?? string.Empty;
            if (string.Equals(item.Name, "Title", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(participant.Heading))
            {
                text = participant.Heading;
            }

            using var brush = new SolidBrush(Color.FromName(item.Color ?? config.DefaultTextColor));
            using var font = new Font(
                item.Font ?? "Arial",
                item.Size <= 0 ? 20 : item.Size,
                ParseFontStyle(item.Style),
                GraphicsUnit.Pixel);

            var size = graphics.MeasureString(text, font);
            var x = (bitmap.Width - size.Width) / 2;
            graphics.DrawString(text, font, brush, x, item.Y);
        }

        foreach (var field in config.CertificateFields.Standard)
        {
            var labelColor = field.LabelColor ?? config.DefaultLabelColor;
            var valueColor = field.ValueColor ?? config.DefaultValueColor;

            var labelText = field.Label ?? string.Empty;
            var valueText = GetFieldValue(participant, field.Name) + (field.Suffix ?? string.Empty);

            using var labelBrush = new SolidBrush(Color.FromName(labelColor));
            using var valueBrush = new SolidBrush(Color.FromName(valueColor));
            using var labelFont = new Font(
                field.LabelFont ?? "Arial",
                field.LabelSize <= 0 ? 20 : field.LabelSize,
                ParseFontStyle(field.LabelStyle),
                GraphicsUnit.Pixel);
            using var valueFont = new Font(
                field.ValueFont ?? "Arial",
                field.ValueSize <= 0 ? 20 : field.ValueSize,
                ParseFontStyle(field.ValueStyle),
                GraphicsUnit.Pixel);

            var labelX = (bitmap.Width - graphics.MeasureString(labelText, labelFont).Width) / 2;
            var valueX = (bitmap.Width - graphics.MeasureString(valueText, valueFont).Width) / 2;

            graphics.DrawString(labelText, labelFont, labelBrush, labelX, field.LabelY);
            graphics.DrawString(valueText, valueFont, valueBrush, valueX, field.ValueY);
        }

        bitmap.Save(outputFile, ImageFormat.Png);
    }

    private static string GetFieldValue(ParticipantRecord participant, string? fieldName)
    {
        return fieldName?.Trim().ToLowerInvariant() switch
        {
            "no" => participant.No,
            "name" => participant.Name,
            "email" => participant.Email,
            "distance" => participant.Distance,
            "time" => participant.Time,
            "heading" => participant.Heading,
            _ => string.Empty
        };
    }

    private static string MakePath(string rootFolder, string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(rootFolder, path);

    private static FontStyle ParseFontStyle(string? style)
    {
        if (Enum.TryParse<FontStyle>(style, true, out var parsed))
        {
            return parsed;
        }

        return FontStyle.Regular;
    }

    private static SmoothingMode ParseSmoothingMode(string? mode)
    {
        if (Enum.TryParse<SmoothingMode>(mode, true, out var parsed))
        {
            return parsed;
        }

        return SmoothingMode.HighQuality;
    }

    private static TextRenderingHint ParseTextRenderingHint(string? hint)
    {
        if (Enum.TryParse<TextRenderingHint>(hint, true, out var parsed))
        {
            return parsed;
        }

        return TextRenderingHint.AntiAliasGridFit;
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Trim();
    }

    private static void WriteOfficialResultsCsv(string path, IReadOnlyCollection<string[]> rows)
    {
        using var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8);
        writer.WriteLine("No,Heading,Name,Distance,Time");
        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",", row.Select(EscapeCsv)));
        }
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return text;
    }
}

public class GenerationResult
{
    public int CertificatesGenerated { get; init; }
    public string ResultsCsvPath { get; init; } = string.Empty;
    public string OutputFolder { get; init; } = string.Empty;
    public IReadOnlyList<GeneratedCertificate> GeneratedCertificates { get; init; } = Array.Empty<GeneratedCertificate>();
}

public class GeneratedCertificate
{
    public string ParticipantName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Distance { get; init; } = string.Empty;
    public string Heading { get; init; } = string.Empty;
    public string CertificateFilePath { get; init; } = string.Empty;
    public string CertificateHost { get; init; } = string.Empty;
    public string EmailTitle { get; init; } = string.Empty;
}
