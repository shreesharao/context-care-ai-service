namespace ContextCare.Domain.Interfaces;

public interface IPdfService
{
    Task<string> ExtractTextFromPdfAsync(Stream pdfFileStream);
}
