using System.IO;
using System.Text.Json;
using AthleticsCertificateMaker.GenerateCertificateApp.Models;

namespace AthleticsCertificateMaker.GenerateCertificateApp.Services;

public static class CertificateConfigLoader
{
    public static CertificateConfig Load(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var config = JsonSerializer.Deserialize<CertificateConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config is null)
        {
            throw new InvalidOperationException("Failed to parse JSON settings.");
        }

        return config;
    }
}
