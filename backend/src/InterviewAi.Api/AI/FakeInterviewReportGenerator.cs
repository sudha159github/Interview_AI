namespace InterviewAi.Api.AI;

/// <summary>
/// TEMPORARY (Phase 5): returns fixed sample data instead of calling Gemini.
/// Replaced by the real Gemini generator in Phase 8.
/// </summary>
public class FakeInterviewReportGenerator : IInterviewReportGenerator
{
    public async Task<AiInterviewReportResult> GenerateAsync(
        string jobDescription,
        string? resumeText,
        string? selfDescription,
        CancellationToken cancellationToken)
    {
        // Pretend the AI is thinking (a real call takes several seconds)
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        return new AiInterviewReportResult(
            Title: "Backend .NET Developer (sample report)",
            MatchScore: 72,
            TechnicalQuestions:
            [
                new AiQuestion(
                    "Explain the difference between AddScoped, AddTransient and AddSingleton.",
                    "Checks understanding of dependency injection lifetimes.",
                    "Describe each lifetime, give an example of when to use it, and mention the risk of injecting scoped services into singletons."),
                new AiQuestion(
                    "How does Entity Framework Core track changes?",
                    "Checks ORM fundamentals.",
                    "Explain the change tracker, entity states, and when SaveChanges generates INSERT, UPDATE or DELETE statements."),
                new AiQuestion(
                    "Why should you use async/await for database calls in ASP.NET Core?",
                    "Checks understanding of scalability.",
                    "Explain that awaiting I/O frees the thread to serve other requests, improving throughput under load.")
            ],
            BehavioralQuestions:
            [
                new AiQuestion(
                    "Tell me about a bug that was hard to find. How did you solve it?",
                    "Assesses problem-solving and persistence.",
                    "Use the STAR method: situation, task, action, result. Focus on how you narrowed down the cause."),
                new AiQuestion(
                    "Describe a time you had to learn a new technology quickly.",
                    "Assesses learning ability.",
                    "Explain your learning approach, the resources you used, and what you delivered.")
            ],
            SkillGaps:
            [
                new AiSkillGap("Docker", "High"),
                new AiSkillGap("Automated testing", "Medium"),
                new AiSkillGap("Cloud deployment (Azure)", "Low")
            ],
            PreparationPlan:
            [
                new AiPreparationDay(1, "C# and .NET fundamentals",
                    ["Review dependency injection lifetimes", "Practise LINQ queries", "Read about async/await pitfalls"]),
                new AiPreparationDay(2, "Databases and EF Core",
                    ["Write queries with Include and projections", "Learn about migrations", "Review indexing basics"]),
                new AiPreparationDay(3, "Mock interview",
                    ["Answer the technical questions out loud", "Prepare two STAR stories", "Research the company"])
            ]);
    }
}