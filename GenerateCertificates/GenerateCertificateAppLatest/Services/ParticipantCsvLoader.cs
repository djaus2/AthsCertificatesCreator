using System.IO;
using System.Linq;
using AthleticsCertificateMaker.GenerateCertificateApp.Models;
using Microsoft.VisualBasic.FileIO;

namespace AthleticsCertificateMaker.GenerateCertificateApp.Services;

public static class ParticipantCsvLoader
{
    public static List<ParticipantRecord> Load(string path, string delimiterMode)
    {
        var delimiter = ResolveDelimiter(path, delimiterMode);
        var rows = ReadRows(path, delimiter);

        var participants = new List<ParticipantRecord>();
        var heading = string.Empty;

        foreach (var row in rows)
        {
            var no = GetValue(row, "No");
            if (no.StartsWith('$'))
            {
                heading = no[1..].Trim();
                continue;
            }

            var name = GetValue(row, "Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            participants.Add(new ParticipantRecord
            {
                No = no,
                Heading = heading,
                Name = name,
                Email = GetValue(row, "Email"),
                Distance = GetValue(row, "Distance"),
                Time = GetValue(row, "Time")
            });
        }

        return participants;
    }

    private static List<Dictionary<string, string>> ReadRows(string path, string delimiter)
    {
        var result = new List<Dictionary<string, string>>();
        using var parser = new TextFieldParser(path);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(delimiter);
        parser.HasFieldsEnclosedInQuotes = true;

        if (parser.EndOfData)
        {
            return result;
        }

        var headers = parser.ReadFields() ?? Array.Empty<string>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields() ?? Array.Empty<string>();
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                var key = headers[i]?.Trim().Trim('"') ?? string.Empty;
                var value = i < fields.Length ? fields[i]?.Trim().Trim('"') ?? string.Empty : string.Empty;
                row[key] = value;
            }

            result.Add(row);
        }

        return result;
    }

    private static string ResolveDelimiter(string path, string mode)
    {
        var normalized = (mode ?? "Auto").Trim().ToLowerInvariant();
        return normalized switch
        {
            "comma" => ",",
            "tab" => "\t",
            "auto" => DetectDelimiter(path),
            _ => throw new InvalidOperationException($"Invalid ParticipantCsvDelimiter '{mode}'. Use Auto, Comma, or Tab.")
        };
    }

    private static string DetectDelimiter(string path)
    {
        var header = File.ReadLines(path).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header))
        {
            throw new InvalidOperationException($"Participant file is empty: {path}");
        }

        var commaCount = header.Count(c => c == ',');
        var tabCount = header.Count(c => c == '\t');
        if (commaCount == tabCount)
        {
            throw new InvalidOperationException($"Could not detect comma or tab delimiter in '{path}'.");
        }

        return commaCount > tabCount ? "," : "\t";
    }

    private static string GetValue(Dictionary<string, string> row, string name)
        => row.TryGetValue(name, out var value) ? value : string.Empty;
}
