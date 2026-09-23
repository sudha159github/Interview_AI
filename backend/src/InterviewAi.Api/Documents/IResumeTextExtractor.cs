namespace InterviewAi.Api.Documents;

/// <summary>
/// Extracts plain text from an uploaded resume (PDF or DOCX).
/// </summary>
public interface IResumeTextExtractor
{
    /// <summary>Longest resume text we keep, to bound AI cost.</summary>
    const int MaxTextLength = 20_000;

    Task<string> ExtractTextAsync(Stream file, string fileName, CancellationToken cancellationToken);
}

/// <summary>Thrown when the file can't be read as a resume.</summary>
public class ResumeExtractionException(string message, Exception? innerException = null)
    : Exception(message, innerException);