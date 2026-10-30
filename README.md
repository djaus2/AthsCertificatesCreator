# AthsCertificatesGenerator

Tools for editing athletics certificate configuration (JSON) and generating certificates.
Built upon [AthsCertificateEditors](https://github.com/djaus2/AthsCertificateEditors), which
supports [AthleticsCertificateMaker](https://github.com/djaus2/AthleticsCertificateMaker).

## Included apps

- **FormEditCert** (`EditJson/FormEdit`)
  - A form-based WPF editor that binds to a `CertificateModel`. Provides UI grids to edit
    `CertificateText` and `CertificateFields`, and common metadata (template file, CSV file,
    colors, flags). Produces the JSON consumed by the generator.
  - Run: `dotnet run --project EditJson/FormEdit/FormEditCert.csproj`

- **GenerateCertificateAppLatest** (`GenerateCertificates/GenerateCertificateAppLatest`)
  - WPF app that loads a certificate JSON config, a template PNG, and a results CSV, then
    generates certificates (view existing, select from results, generate and view). Can
    send via Classic Outlook (Save option available until ready to send).
  - Run: `dotnet run --project GenerateCertificates/GenerateCertificateAppLatest/GenerateCertificateAppLatest.csproj`
  - Note: in dev mode you may need to turn off **Smart App Control** in Windows Security.

## Requirements

- .NET 10 SDK
- Windows (WPF)
- Visual Studio 2026 or a compatible IDE (solution: `AthsCertificatesGenerator.slnx`)

## Getting started

1. Clone: `git clone https://github.com/djaus2/AthsCertificatesCreator.git`
2. Open `AthsCertificatesGenerator.slnx` in Visual Studio, or `dotnet build`
3. Use **FormEditCert** to create/edit the certificate JSON (e.g. `CertificateText.json`)
4. Run **GenerateCertificateAppLatest**, load the JSON, adjust settings, and generate
   certificates into `Output/`.

## License

See the LICENSE file.
