using System.IO;
using System.IO.Compression;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DocumentSandwich;
static class SelfTest
{
    public static void Run(string root)
    {
        Directory.CreateDirectory(root);
        string P(string n) => Path.Combine(root,n);
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        using (var a = new PdfDocument()) { var p=a.AddPage(); p.Width=321; p.Height=456; a.Save(P("first.pdf")); }
        using (var b = new PdfDocument()) { var p=b.AddPage(); p.Width=432; p.Height=567; b.Save(P("last.pdf")); }
        using (var zip = ZipFile.Open(P("middle.docx"),ZipArchiveMode.Create))
        {
            void Entry(string path,string text) { using var writer = new StreamWriter(zip.CreateEntry(path).Open()); writer.Write(text); }
            Entry("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            Entry("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
            Entry("word/document.xml", "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>Document Sandwich - Word conversion test</w:t></w:r></w:p><w:p><w:r><w:br w:type=\"page\"/><w:t>Second Word page</w:t></w:r></w:p><w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/></w:sectPr></w:body></w:document>");
        }
        var count=Combiner.Combine([P("first.pdf"),P("middle.docx"),P("last.pdf")],P("combined.pdf"),false,_=>{});
        using (var merged=PdfReader.Open(P("combined.pdf"),PdfDocumentOpenMode.Import))
        {
            Check(count==4 && merged.PageCount==4,"Mixed PDF/Word merge should contain four pages.");
            Check(Math.Abs(merged.Pages[0].Width.Point-321)<1 && Math.Abs(merged.Pages[3].Width.Point-432)<1,"Page order was not preserved.");
        }
        Check(Combiner.ValidateName("Example.pdf")=="Example.pdf","Extension normalization failed.");
        foreach (var invalid in new[] { "", "../bad", "CON", "a:b" }) { bool rejected=false; try { Combiner.ValidateName(invalid); } catch { rejected=true; } Check(rejected,"Invalid name accepted: " + invalid); }
        var original=File.ReadAllBytes(P("combined.pdf"));
        File.WriteAllText(P("broken.pdf"),"not a PDF");
        bool failed=false; try { Combiner.Combine([P("broken.pdf")],P("combined.pdf"),true,_=>{}); } catch { failed=true; }
        Check(failed && original.SequenceEqual(File.ReadAllBytes(P("combined.pdf"))),"Failed conversion damaged the existing output.");
        failed=false; try { Combiner.Combine([P("first.pdf")],P("first.pdf"),true,_=>{}); } catch { failed=true; } Check(failed,"Input overwrite was allowed.");
        File.WriteAllText(P("passed.txt"),"PASS: mixed PDF/Word conversion, 4-page count, page order, name validation, failure preserves existing output, input overwrite prevention.");
    }
}
