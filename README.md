# Athletics Certificate Creator

> This version of the certificate generators is the whole box and dice. Can edit the
> settings JSON file, edit results and generate certificates. Combines the previous
> Generator app with the Forms version of the JSON editors.

Built upon [AthsCertificateEditors](https://github.com/djaus2/AthsCertificateEditors), which
supports [AthleticsCertificateMaker](https://github.com/djaus2/AthleticsCertificateMaker).

## Included apps

- **CreateAthleticsCertificatesApp** (`GenerateCertificates/CreateAthleticsCertificatesApp`)
  - WPF app that loads a certificate JSON config, a template PNG, and a results CSV, then
    generates certificates (view existing, select from results, generate and view). Can
    send via Classic Outlook (Save option available until ready to send).
  - Includes a built-in form-based editor for the certificate JSON config
    (**File → Settings → Edit in Form Editor...**). Provides UI grids to edit
    `CertificateText` and `CertificateFields`, plus common metadata (template file, CSV
    file, colors, flags). Saving there reloads the settings into the app.
  - **Edit → Results...** opens a separate editor for the results file (tab- or
    comma-delimited), including `$` heading lines.
  - Run: `dotnet run --project GenerateCertificates/CreateAthleticsCertificatesApp/CreateAthleticsCertificatesApp.csproj`
  - Note: in dev mode you may need to turn off **Smart App Control** in Windows Security.

## Requirements

- .NET 10 SDK
- Windows (WPF)
- Visual Studio 2026 or a compatible IDE (solution: `AthsCertificateCreator.slnx`)

## Getting started

1. Clone: `git clone https://github.com/djaus2/AthsCertificatesCreator.git`
2. Open `AthsCertificateCreator.slnx` in Visual Studio, or `dotnet build`
3. Run **CreateAthleticsCertificatesApp**, load the JSON (e.g. `CertificateText.json`),
   adjust settings, and generate certificates into `Output/`.
4. To create/edit the JSON config, use **File → Settings → Edit in Form Editor...** in the app.

## License

See the LICENSE file.
