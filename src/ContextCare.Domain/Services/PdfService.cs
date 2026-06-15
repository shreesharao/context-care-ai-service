using System.Text;
using ContextCare.Domain.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ContextCare.Domain.Services;

public class PdfService : IPdfService
{
    public async Task<string> ExtractTextFromPdfAsync(Stream pdfFileStream)
    {
        var extractedText = new StringBuilder();
        using (PdfDocument document = PdfDocument.Open(pdfFileStream))
        {
            foreach (Page page in document.GetPages())
            {
                string text = ContentOrderTextExtractor.GetText(page);
                extractedText.AppendLine(text);
            }
        }
        return extractedText.ToString();
    }
}
