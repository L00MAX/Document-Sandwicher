using System.IO;
using System.Runtime.InteropServices;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DocumentSandwich;
public static class Combiner
{
    public static string ValidateName(string value)
    {
        var name = value.Trim();
        if (name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.EndsWith('.') || name.EndsWith(' '))
            throw new InvalidOperationException("Enter a valid PDF name without folder paths or special characters such as / \\ : * ?.");
        var stem = name.Split('.')[0].ToUpperInvariant();
        if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(stem))
            throw new InvalidOperationException("That name is reserved by Windows. Please choose another name.");
        return name + ".pdf";
    }

    public static Task<int> CombineAsync(string[] files, string output, bool overwrite, Action<string> progress)
    {
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try { completion.SetResult(Combine(files, output, overwrite, progress)); }
            catch (Exception ex) { completion.SetException(ex); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    public static int Combine(string[] files, string output, bool overwrite, Action<string> progress)
    {
        if (files.Length == 0) throw new InvalidOperationException("Add at least one PDF, Word document or image first.");
        if (files.Any(f => !InputTypes.IsSupported(f))) throw new InvalidOperationException("One of the selected files has an unsupported file type.");
        if (files.Any(f => string.Equals(Path.GetFullPath(f), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Choose a different output name. An input file cannot be replaced.");
        var temp = Path.Combine(Path.GetTempPath(), "DocumentSandwich-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var staging = Path.Combine(Path.GetDirectoryName(output)!, ".pdf-sandwich-" + Guid.NewGuid().ToString("N") + ".tmp");
        object? wordObject = null;
        try
        {
            using var result = new PdfDocument();
            result.Info.Title = Path.GetFileNameWithoutExtension(output);
            foreach (var file in files)
            {
                if (!File.Exists(file)) throw new IOException("File not found: " + file);
                progress("Preparing " + Path.GetFileName(file) + "…");
                if (InputTypes.IsImage(file)) { ImagePages.Add(result,file); continue; }
                var pdf = file;
                if (!Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    if (wordObject == null)
                    {
                        var type = Type.GetTypeFromProgID("Word.Application") ?? throw new InvalidOperationException("Microsoft Word is required to combine Word documents. Install Word or export those files to PDF first.");
                        wordObject = Activator.CreateInstance(type)!;
                        dynamic app = wordObject;
                        app.Visible = false;
                        app.DisplayAlerts = 0;
                        app.AutomationSecurity = 3;
                        app.Options.UpdateLinksAtOpen = false;
                    }
                    pdf = Path.Combine(temp, Guid.NewGuid().ToString("N") + ".pdf");
                    ConvertWord(wordObject, file, pdf);
                }
                try
                {
                    using var input = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
                    foreach (var page in input.Pages) result.AddPage(page);
                }
                catch (Exception ex) { throw new InvalidOperationException("Cannot read " + Path.GetFileName(file) + ". It may be protected or damaged. " + ex.Message, ex); }
            }
            if (result.PageCount == 0) throw new InvalidOperationException("The selected files contain no pages.");
            progress("Saving your PDF…");
            var pageCount = result.PageCount;
            result.Save(staging);
            // Keep existing output intact until the complete new PDF is ready.
            File.Move(staging, output, overwrite);
            return pageCount;
        }
        finally
        {
            if (wordObject != null)
            {
                try { ((dynamic)wordObject).Quit(0); } catch { }
                Marshal.FinalReleaseComObject(wordObject);
            }
            try { if (File.Exists(staging)) File.Delete(staging); Directory.Delete(temp, true); } catch { }
        }
    }

    private static void ConvertWord(object wordObject, string input, string output)
    {
        object? documents = null;
        object? doc = null;
        try
        {
            dynamic word = wordObject;
            documents = word.Documents;
            doc = ((dynamic)documents).Open(FileName: input, ConfirmConversions: false, ReadOnly: true,
                AddToRecentFiles: false, PasswordDocument: "DocumentSandwich-NoPassword", PasswordTemplate: "DocumentSandwich-NoPassword",
                Revert: false, WritePasswordDocument: "DocumentSandwich-NoPassword", Visible: false, OpenAndRepair: false, NoEncodingDialog: true);
            ((dynamic)doc).ExportAsFixedFormat(OutputFileName: output, ExportFormat: 17, OpenAfterExport: false);
        }
        catch (Exception ex) { throw new InvalidOperationException("Word could not convert " + Path.GetFileName(input) + ". Check that it opens normally and is not password protected. " + ex.Message, ex); }
        finally
        {
            if (doc != null) { try { ((dynamic)doc).Close(0); } catch { } Marshal.FinalReleaseComObject(doc); }
            if (documents != null) Marshal.FinalReleaseComObject(documents);
        }
    }
}
