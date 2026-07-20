namespace Rojan.Desktop.Application.AI;

/// <summary>Classifies a user message's likely <see cref="InsightCategory"/> so <see cref="PromptBuilder"/> can pick the most relevant prompt template and business context slice - keyword-based, no ML model, consistent with this phase's Mock-only scope.</summary>
public interface IIntentClassifier
{
    public InsightCategory ClassifyIntent(string userMessage);
}
