namespace AthleticsCertificateMaker.GenerateCertificateApp.Models;

public class CertificateTextItem
{
    public string? Name { get; set; }
    public string? Text { get; set; }
    public string? Color { get; set; }
    public string? Font { get; set; }
    public float Size { get; set; }
    public string? Style { get; set; }
    public float Y { get; set; }
}

public class CertificateFieldItem
{
    public string? Name { get; set; }
    public string? Label { get; set; }
    public string? Suffix { get; set; }
    public string? LabelFont { get; set; }
    public float LabelSize { get; set; }
    public string? LabelStyle { get; set; }
    public string? LabelColor { get; set; }
    public float LabelY { get; set; }
    public string? ValueFont { get; set; }
    public float ValueSize { get; set; }
    public string? ValueStyle { get; set; }
    public string? ValueColor { get; set; }
    public float ValueY { get; set; }
}

public class CertificateFieldsConfig
{
    public List<CertificateFieldItem> Standard { get; set; } = new();
    public List<CertificateFieldItem> Time { get; set; } = new();
}

public class CertificateConfig
{
    public string CertificateTemplateFile { get; set; } = string.Empty;
    public string ParticipantCsvFile { get; set; } = "results.csv";
    public string ParticipantCsvDelimiter { get; set; } = "Auto";
    public string SendUsingAccount { get; set; } = string.Empty;

    public string DefaultTextColor { get; set; } = "Red";
    public string DefaultLabelColor { get; set; } = "LightGreen";
    public string DefaultValueColor { get; set; } = "Yellow";

    public string CertificateSmoothingMode { get; set; } = "HighQuality";
    public string CertificateTextRenderingHint { get; set; } = "AntiAliasGridFit";

    public bool ShowLocation { get; set; }
    public bool ShowHost { get; set; }

    public List<CertificateTextItem> CertificateText { get; set; } = new();
    public CertificateFieldsConfig CertificateFields { get; set; } = new();
}
