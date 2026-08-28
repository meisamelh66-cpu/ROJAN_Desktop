using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Support;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Support;

/// <summary>
/// The "پشتیبانی" (Support Center) page - About/Contact/Send Message/Send
/// Email/Contact Super Admin/Development Participation/Report Bug/
/// Suggestions/FAQ/User Guide/Terms &amp; Privacy/Version Info, all on one
/// page (the same "one scrollable page, one <c>DashboardCard</c> per
/// section" shape <c>Settings.SettingsPageViewModel</c> already
/// establishes, rather than inventing a tabbed/master-detail layout for
/// what is mostly static content plus two small forms). "ارسال پیام"/
/// "ارتباط با Super Admin"/"گزارش خطا"/"پیشنهادات و انتقادات" share one
/// message form discriminated by <see cref="SupportMessageType"/> rather
/// than four near-identical forms.
/// </summary>
public sealed partial class SupportPageViewModel : ViewModelBase
{
    private readonly ISupportMessageService _messageService;
    private readonly IDevelopmentApplicationService _applicationService;
    private readonly ILogger<SupportPageViewModel> _logger;

    private SupportMessageType _messageType = SupportMessageType.General;
    private string _messageSubject = string.Empty;
    private string _messageBody = string.Empty;
    private string _messageSenderName = string.Empty;
    private string _messageSenderEmail = string.Empty;
    private string? _messageStatus;
    private string? _messageError;

    private string _applicantFirstName = string.Empty;
    private string _applicantLastName = string.Empty;
    private string _applicantMobile = string.Empty;
    private string _applicantEmail = string.Empty;
    private string _applicantCity = string.Empty;
    private string _collaborationArea = string.Empty;
    private string _gitHubUrl = string.Empty;
    private string _linkedInUrl = string.Empty;
    private string _portfolioUrl = string.Empty;
    private string _resumeUrl = string.Empty;
    private string _applicationDescription = string.Empty;
    private string? _applicationStatus;
    private string? _applicationError;

    public SupportPageViewModel(IRojanBrandConfiguration brandConfiguration, ISupportMessageService messageService, IDevelopmentApplicationService applicationService, ILogger<SupportPageViewModel>? logger = null)
    {
        _messageService = messageService;
        _applicationService = applicationService;
        _logger = logger ?? NullLogger<SupportPageViewModel>.Instance;

        WebsiteUrl = brandConfiguration.WebsiteUrl;
        PhoneNumber = brandConfiguration.PhoneNumber;
        SupportEmail = brandConfiguration.SupportEmail;
        ApiBaseUrl = brandConfiguration.ApiBaseUrl;
        AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

        AvailableMessageTypes = Enum.GetValues<SupportMessageType>();

        OpenWebsiteCommand = new RelayCommand(_ => OpenUrl($"https://{WebsiteUrl}"));
        ComposeEmailCommand = new RelayCommand(_ => OpenUrl($"mailto:{SupportEmail}"));
        SubmitMessageCommand = new AsyncRelayCommand(_ => SubmitMessageAsync(), _ => !string.IsNullOrWhiteSpace(MessageSubject) && !string.IsNullOrWhiteSpace(MessageBody));
        SubmitApplicationCommand = new AsyncRelayCommand(_ => SubmitApplicationAsync(), _ => !string.IsNullOrWhiteSpace(ApplicantFirstName) && !string.IsNullOrWhiteSpace(ApplicantLastName) && !string.IsNullOrWhiteSpace(CollaborationArea));
    }

    public string WebsiteUrl { get; }

    public string PhoneNumber { get; }

    public string SupportEmail { get; }

    public string ApiBaseUrl { get; }

    public string AppVersion { get; }

    public IReadOnlyList<SupportMessageType> AvailableMessageTypes { get; }

    public ICommand OpenWebsiteCommand { get; }

    public ICommand ComposeEmailCommand { get; }

    public ICommand SubmitMessageCommand { get; }

    public ICommand SubmitApplicationCommand { get; }

    public SupportMessageType MessageType
    {
        get => _messageType;
        set => SetProperty(ref _messageType, value);
    }

    public string MessageSubject
    {
        get => _messageSubject;
        set
        {
            if (SetProperty(ref _messageSubject, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string MessageBody
    {
        get => _messageBody;
        set
        {
            if (SetProperty(ref _messageBody, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string MessageSenderName
    {
        get => _messageSenderName;
        set => SetProperty(ref _messageSenderName, value);
    }

    public string MessageSenderEmail
    {
        get => _messageSenderEmail;
        set => SetProperty(ref _messageSenderEmail, value);
    }

    public string? MessageStatus
    {
        get => _messageStatus;
        private set => SetProperty(ref _messageStatus, value);
    }

    public string? MessageError
    {
        get => _messageError;
        private set => SetProperty(ref _messageError, value);
    }

    public string ApplicantFirstName
    {
        get => _applicantFirstName;
        set
        {
            if (SetProperty(ref _applicantFirstName, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ApplicantLastName
    {
        get => _applicantLastName;
        set
        {
            if (SetProperty(ref _applicantLastName, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ApplicantMobile
    {
        get => _applicantMobile;
        set => SetProperty(ref _applicantMobile, value);
    }

    public string ApplicantEmail
    {
        get => _applicantEmail;
        set => SetProperty(ref _applicantEmail, value);
    }

    public string ApplicantCity
    {
        get => _applicantCity;
        set => SetProperty(ref _applicantCity, value);
    }

    public string CollaborationArea
    {
        get => _collaborationArea;
        set
        {
            if (SetProperty(ref _collaborationArea, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string GitHubUrl
    {
        get => _gitHubUrl;
        set => SetProperty(ref _gitHubUrl, value);
    }

    public string LinkedInUrl
    {
        get => _linkedInUrl;
        set => SetProperty(ref _linkedInUrl, value);
    }

    public string PortfolioUrl
    {
        get => _portfolioUrl;
        set => SetProperty(ref _portfolioUrl, value);
    }

    public string ResumeUrl
    {
        get => _resumeUrl;
        set => SetProperty(ref _resumeUrl, value);
    }

    public string ApplicationDescription
    {
        get => _applicationDescription;
        set => SetProperty(ref _applicationDescription, value);
    }

    public string? ApplicationStatus
    {
        get => _applicationStatus;
        private set => SetProperty(ref _applicationStatus, value);
    }

    public string? ApplicationError
    {
        get => _applicationError;
        private set => SetProperty(ref _applicationError, value);
    }

    private async Task SubmitMessageAsync()
    {
        MessageError = null;
        MessageStatus = null;
        try
        {
            await _messageService.SubmitAsync(MessageType, MessageSubject, MessageBody, MessageSenderName, MessageSenderEmail).ConfigureAwait(true);
            MessageSubject = string.Empty;
            MessageBody = string.Empty;
            MessageStatus = Localization.Strings.Support_Message_Sent;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            MessageError = exception.Message;
            LogOperationFailed(nameof(SubmitMessageAsync));
        }
    }

    // Security: logs the operation name only - never the exception, its message,
    // or any form field (sender name/email, message content, applicant PII/URLs).
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Support page operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    private async Task SubmitApplicationAsync()
    {
        ApplicationError = null;
        ApplicationStatus = null;
        try
        {
            await _applicationService.SubmitAsync(
                ApplicantFirstName, ApplicantLastName, ApplicantMobile, ApplicantEmail, ApplicantCity, CollaborationArea,
                GitHubUrl, LinkedInUrl, PortfolioUrl, ResumeUrl, ApplicationDescription).ConfigureAwait(true);

            ApplicantFirstName = string.Empty;
            ApplicantLastName = string.Empty;
            ApplicantMobile = string.Empty;
            ApplicantEmail = string.Empty;
            ApplicantCity = string.Empty;
            CollaborationArea = string.Empty;
            GitHubUrl = string.Empty;
            LinkedInUrl = string.Empty;
            PortfolioUrl = string.Empty;
            ResumeUrl = string.Empty;
            ApplicationDescription = string.Empty;
            ApplicationStatus = Localization.Strings.Support_Application_Sent;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ApplicationError = exception.Message;
            LogOperationFailed(nameof(SubmitApplicationAsync));
        }
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
