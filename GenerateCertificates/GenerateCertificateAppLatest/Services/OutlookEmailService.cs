using System.IO;

namespace AthleticsCertificateMaker.GenerateCertificateApp.Services;

public enum EmailDeliveryMode
{
    Save,
    Send
}

public static class OutlookEmailService
{
    private const string DraftMarkerProperty = "GenerateCertificateAppDraft";

    public static int ProcessCertificates(
        IReadOnlyCollection<GeneratedCertificate> certificates,
        string resultsCsvPath,
        string sendUsingAccount,
        EmailDeliveryMode mode,
        Action<string> log)
    {
        if (certificates.Count == 0)
        {
            return 0;
        }

        var outlookType = Type.GetTypeFromProgID("Outlook.Application")
            ?? throw new InvalidOperationException("Outlook 16 COM object not available.");

        dynamic outlook = Activator.CreateInstance(outlookType)
            ?? throw new InvalidOperationException("Failed to create Outlook COM object.");

        dynamic? sendAccount = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(sendUsingAccount))
            {
                try
                {
                    sendAccount = outlook.Session.Accounts.Item(sendUsingAccount);
                }
                catch
                {
                    log($"Account '{sendUsingAccount}' not found - using default account.");
                }
            }

            var processed = 0;
            foreach (var cert in certificates)
            {
                if (string.IsNullOrWhiteSpace(cert.Email))
                {
                    continue;
                }

                dynamic mail = outlook.CreateItem(0);
                if (sendAccount is not null)
                {
                    mail.SendUsingAccount = sendAccount;
                }

                mail.To = cert.Email;
                mail.Subject = $"{cert.CertificateHost} - {cert.EmailTitle} Certificate";
                mail.HTMLBody = $@"<p>Dear {cert.ParticipantName},</p>
<p>Please find attached your participation certificate.</p>
<p>Recorded distance: <strong>{cert.Distance} metres</strong></p>
<p>Thank you for participating.</p>";

                mail.Attachments.Add(cert.CertificateFilePath);
                if (File.Exists(resultsCsvPath))
                {
                    mail.Attachments.Add(resultsCsvPath);
                }

                if (mode == EmailDeliveryMode.Send)
                {
                    mail.Send();
                    log($"Sent: {cert.Email} ({cert.ParticipantName})");
                }
                else
                {
                    MarkAsAppDraft(mail);
                    mail.Save();
                    log($"Saved draft: {cert.Email} ({cert.ParticipantName})");
                }

                processed++;
            }

            return processed;
        }
        finally
        {
            try
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(outlook);
            }
            catch
            {
            }
        }
    }

    public static int SendMarkedDrafts(string sendUsingAccount, Action<string> log)
    {
        var outlookType = Type.GetTypeFromProgID("Outlook.Application")
            ?? throw new InvalidOperationException("Outlook 16 COM object not available.");

        dynamic outlook = Activator.CreateInstance(outlookType)
            ?? throw new InvalidOperationException("Failed to create Outlook COM object.");

        dynamic? sendAccount = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(sendUsingAccount))
            {
                try
                {
                    sendAccount = outlook.Session.Accounts.Item(sendUsingAccount);
                }
                catch
                {
                    log($"Account '{sendUsingAccount}' not found - using default account.");
                }
            }

            const int olFolderDrafts = 16;
            dynamic draftsFolder = outlook.Session.GetDefaultFolder(olFolderDrafts);
            dynamic items = draftsFolder.Items;

            var sent = 0;
            for (var i = items.Count; i >= 1; i--)
            {
                dynamic item = items.Item(i);
                if (!IsMarkedDraft(item))
                {
                    continue;
                }

                try
                {
                    dynamic mail = item;
                    try
                    {
                        var entryId = (string?)mail.EntryID;
                        if (!string.IsNullOrWhiteSpace(entryId))
                        {
                            mail = outlook.Session.GetItemFromID(entryId);
                        }
                    }
                    catch
                    {
                    }

                    if (sendAccount is not null)
                    {
                        try
                        {
                            mail.SendUsingAccount = sendAccount;
                        }
                        catch (Exception ex)
                        {
                            log($"Draft account override skipped: {ex.Message}");
                        }
                    }

                    try
                    {
                        mail.Send();
                    }
                    catch (Exception ex) when (IsInlineResponseError(ex))
                    {
                        log("Draft is inline-response; rebuilding message and retrying send.");
                        SendUsingRebuiltMail(outlook, mail, sendAccount, log);

                        try
                        {
                            mail.Delete();
                        }
                        catch
                        {
                        }
                    }

                    sent++;
                }
                catch (Exception ex)
                {
                    log($"Skipped draft item: {ex.Message}");
                }
            }

            return sent;
        }
        finally
        {
            try
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(outlook);
            }
            catch
            {
            }
        }
    }

    public static int ClearMarkedDrafts(Action<string> log)
    {
        var outlookType = Type.GetTypeFromProgID("Outlook.Application")
            ?? throw new InvalidOperationException("Outlook 16 COM object not available.");

        dynamic outlook = Activator.CreateInstance(outlookType)
            ?? throw new InvalidOperationException("Failed to create Outlook COM object.");

        try
        {
            const int olFolderDrafts = 16;
            dynamic draftsFolder = outlook.Session.GetDefaultFolder(olFolderDrafts);
            dynamic items = draftsFolder.Items;

            var deleted = 0;
            for (var i = items.Count; i >= 1; i--)
            {
                dynamic item = items.Item(i);
                if (!IsMarkedDraft(item))
                {
                    continue;
                }

                try
                {
                    item.Delete();
                    deleted++;
                }
                catch (Exception ex)
                {
                    log($"Skipped draft delete: {ex.Message}");
                }
            }

            return deleted;
        }
        finally
        {
            try
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(outlook);
            }
            catch
            {
            }
        }
    }

    private static void MarkAsAppDraft(dynamic mail)
    {
        dynamic userProps = mail.UserProperties;
        const int olYesNo = 6;
        dynamic prop = userProps.Find(DraftMarkerProperty);
        if (prop is null)
        {
            prop = userProps.Add(DraftMarkerProperty, olYesNo, true);
        }

        prop.Value = true;
    }

    private static bool IsMarkedDraft(dynamic item)
    {
        try
        {
            dynamic userProps = item.UserProperties;
            dynamic prop = userProps.Find(DraftMarkerProperty);
            if (prop is null)
            {
                return false;
            }

            return Convert.ToBoolean(prop.Value);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsInlineResponseError(Exception ex)
        => ex.Message?.IndexOf("inline response", StringComparison.OrdinalIgnoreCase) >= 0;

    private static void SendUsingRebuiltMail(dynamic outlook, dynamic sourceMail, dynamic? sendAccount, Action<string> log)
    {
        dynamic newMail = outlook.CreateItem(0);

        if (sendAccount is not null)
        {
            try
            {
                newMail.SendUsingAccount = sendAccount;
            }
            catch (Exception accountEx)
            {
                log($"Rebuilt draft account override skipped: {accountEx.Message}");
            }
        }

        try { newMail.To = sourceMail.To; } catch { }
        try { newMail.CC = sourceMail.CC; } catch { }
        try { newMail.BCC = sourceMail.BCC; } catch { }
        try { newMail.Subject = sourceMail.Subject; } catch { }

        var bodySet = false;
        try
        {
            newMail.HTMLBody = sourceMail.HTMLBody;
            bodySet = true;
        }
        catch
        {
        }

        if (!bodySet)
        {
            try { newMail.Body = sourceMail.Body; } catch { }
        }

        try
        {
            dynamic attachments = sourceMail.Attachments;
            var count = (int)attachments.Count;
            for (var ai = 1; ai <= count; ai++)
            {
                try
                {
                    dynamic attachment = attachments.Item(ai);
                    var path = (string?)attachment.PathName;
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        newMail.Attachments.Add(path);
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        newMail.Send();
    }
}
