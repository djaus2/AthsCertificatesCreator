using System.Collections.Generic;

namespace AthleticsCertificateMaker.EditorUI.Models
{
    public class TextItem
    {
        public string? Name { get; set; }
        public string? Text { get; set; }
        public string? Color { get; set; }
        public string? Font { get; set; }
        public int? Size { get; set; }
        public string? Style { get; set; }
        public int? Y { get; set; }
    }

    public class FieldItem
    {
        public string? Name { get; set; }
        public string? Label { get; set; }
        public string? Suffix { get; set; }
        public string? LabelFont { get; set; }
        public int? LabelSize { get; set; }
        public string? LabelStyle { get; set; }
        public string? LabelColor { get; set; }
        public int? LabelY { get; set; }
        public string? ValueFont { get; set; }
        public int? ValueSize { get; set; }
        public string? ValueStyle { get; set; }
        public string? ValueColor { get; set; }
        public int? ValueY { get; set; }
    }

    public class CertificateFields
    {
        public List<FieldItem>? Standard { get; set; }
        public List<FieldItem>? Time { get; set; }
    }

    public class CertificateModel
    {
        public string? CertificateTemplateFile { get; set; }
        public string? ParticipantCsvFile { get; set; }
        public string? ParticipantCsvDelimiter { get; set; }
        public string? SendUsingAccount { get; set; }
        public string? DefaultTextColor { get; set; }
        public string? DefaultLabelColor { get; set; }
        public string? DefaultValueColor { get; set; }
        public int? DefaultLabelTextSize { get; set; }
        public int? DefaultDataTextSize { get; set; }
        public string? CertificateSmoothingMode { get; set; }
        public string? CertificateTextRenderingHint { get; set; }
        public bool? ShowLocation { get; set; }
        public bool? ShowHost { get; set; }

        public List<TextItem>? CertificateText { get; set; }
        public CertificateFields? CertificateFields { get; set; }
    }
}
