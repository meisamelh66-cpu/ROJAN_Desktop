namespace Rojan.Desktop.Presentation.ViewModels.AI;

/// <summary>The AI Center page's local section switcher - same shape as <c>Reporting.ReportingSection</c>. Home hosts the Business Health Score/Daily Summary/Smart Notifications; Insights covers Trend/Risk/Opportunity detection across every business area; Recommendations hosts the Action Center's suggested tasks; History is the Conversation Viewer; Settings hosts the Model Selector, Prompt Templates, and Usage Dashboard.</summary>
public enum AiCenterSection
{
    Home,
    Chat,
    Insights,
    Recommendations,
    History,
    Settings,
}
