using System.Text;

using DocumentFormat.OpenXml.Packaging;

using UglyToad.PdfPig;

namespace InterviewAi.Api.Documents;

/// <summary>
/// Reads text from PDF and DOCX resumes. The file type is detected from its
/// content (magic bytes), not from the file name, which a client can fake.
/// </summary>
public class ResumeTextExtractor(ILogger<ResumeTextExtractor> logger) : IResumeTextExtractor
{
    public async Task<string> ExtractTextAsync(Stream file, string fileName, CancellationToken cancellationToken)
    {
        // Copy to memory: both libraries need a seekable stream
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        var text = DetectFileType(memory) switch
        {
            ResumeFileType.Pdf => ExtractFromPdf(memory),
            ResumeFileType.Docx => ExtractFromDocx(memory),
            _ => throw new ResumeExtractionException("Only PDF and DOCX resumes are supported."),
        };

        text = Normalize(text);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ResumeExtractionException(
                "No text could be read from this file. If it is a scanned document, please paste your details in the 'About you' field instead.");
        }

        if (text.Length > IResumeTextExtractor.MaxTextLength)
        {
            text = text[..IResumeTextExtractor.MaxTextLength];
        }

        logger.LogInformation(
            "Extracted {CharacterCount} characters from an uploaded resume ({Extension})",
            text.Length,
            Path.GetExtension(fileName));

        return text;
    }

    // ---------- file type detection ----------

    private enum ResumeFileType
    {
        Unknown = 0,
        Pdf = 1,
        Docx = 2,
    }

    private static ResumeFileType DetectFileType(MemoryStream stream)
    {
        Span<byte> header = stackalloc byte[4];

        if (stream.Read(header) < 4)
        {
            return ResumeFileType.Unknown;
        }

        stream.Position = 0;

        // "%PDF"
        if (header is [0x25, 0x50, 0x44, 0x46])
        {
            return ResumeFileType.Pdf;
        }

        // "PK\x03\x04" = ZIP, which is what a DOCX really is
        if (header is [0x50, 0x4B, 0x03, 0x04])
        {
            return ResumeFileType.Docx;
        }

        return ResumeFileType.Unknown;
    }

    // ---------- extraction ----------

    private static string ExtractFromPdf(MemoryStream stream)
    {
        try
        {
            using var document = PdfDocument.Open(stream);
            var builder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                builder.AppendLine(page.Text);
            }

            return builder.ToString();
        }
        catch (Exception ex)
        {
            throw new ResumeExtractionException("This PDF could not be read. It may be damaged or password-protected.", ex);
        }
    }

    private static string ExtractFromDocx(MemoryStream stream)
    {
        try
        {
            using var document = WordprocessingDocument.Open(stream, isEditable: false);
            var body = document.MainDocumentPart?.Document?.Body;

            return body is null ? string.Empty : body.InnerText;
        }
        catch (Exception ex)
        {
            throw new ResumeExtractionException("This Word document could not be read. It may be damaged.", ex);
        }
    }

    private static string Normalize(string text) =>
        string.Join('\n', text
            .ReplaceLineEndings("\n")
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0));
}