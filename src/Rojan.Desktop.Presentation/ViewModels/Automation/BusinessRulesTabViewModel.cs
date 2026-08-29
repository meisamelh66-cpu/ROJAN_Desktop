using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Automation;

/// <summary>Requirement 32.2's Business Rules Engine tab - "IF Customer is VIP → Apply Discount" etc. Each rule created here has exactly one condition; the underlying engine supports an AND-combined list (<see cref="BusinessRuleDto.Conditions"/>), a documented single-condition-form scope trim consistent with 32.1's "no visual designer yet" boundary.</summary>
public sealed partial class BusinessRulesTabViewModel : ViewModelBase
{
    private readonly IBusinessRuleService _businessRuleService;
    private readonly string _organizationId;
    private readonly string _branchId;
    private readonly ILogger<BusinessRulesTabViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string _newRuleName = string.Empty;
    private string _newRuleField = string.Empty;
    private BusinessRuleOperator _newRuleOperator = BusinessRuleOperator.Equals;
    private string _newRuleValue = string.Empty;
    private BusinessRuleActionType _newRuleActionType = BusinessRuleActionType.RaiseNotification;
    private string _newRuleActionValue = string.Empty;
    private int _newRulePriority;

    public BusinessRulesTabViewModel(IBusinessRuleService businessRuleService, string organizationId, string branchId, ILogger<BusinessRulesTabViewModel>? logger = null)
    {
        _businessRuleService = businessRuleService;
        _organizationId = organizationId;
        _branchId = branchId;
        _logger = logger ?? NullLogger<BusinessRulesTabViewModel>.Instance;

        Rules = [];
        AvailableOperators = Enum.GetValues<BusinessRuleOperator>();
        AvailableActionTypes = Enum.GetValues<BusinessRuleActionType>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        CreateCommand = new AsyncRelayCommand(_ => CreateAsync(), _ => !string.IsNullOrWhiteSpace(NewRuleName) && !string.IsNullOrWhiteSpace(NewRuleField));
        ToggleEnabledCommand = new AsyncRelayCommand(parameter => ToggleEnabledAsync((BusinessRuleDto)parameter!));
        DeleteCommand = new AsyncRelayCommand(parameter => DeleteAsync((BusinessRuleDto)parameter!));
    }

    public ObservableCollection<BusinessRuleDto> Rules { get; }

    public IReadOnlyList<BusinessRuleOperator> AvailableOperators { get; }

    public IReadOnlyList<BusinessRuleActionType> AvailableActionTypes { get; }

    public DashboardState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string NewRuleName
    {
        get => _newRuleName;
        set
        {
            if (SetProperty(ref _newRuleName, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string NewRuleField
    {
        get => _newRuleField;
        set
        {
            if (SetProperty(ref _newRuleField, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public BusinessRuleOperator NewRuleOperator
    {
        get => _newRuleOperator;
        set => SetProperty(ref _newRuleOperator, value);
    }

    public string NewRuleValue
    {
        get => _newRuleValue;
        set => SetProperty(ref _newRuleValue, value);
    }

    public BusinessRuleActionType NewRuleActionType
    {
        get => _newRuleActionType;
        set => SetProperty(ref _newRuleActionType, value);
    }

    /// <summary>Free-form single action parameter - mapped to "percentage" for ApplyDiscount and "workflowId" for TriggerWorkflow when the rule fires (see <c>BusinessRuleService</c>'s own doc comment).</summary>
    public string NewRuleActionValue
    {
        get => _newRuleActionValue;
        set => SetProperty(ref _newRuleActionValue, value);
    }

    public int NewRulePriority
    {
        get => _newRulePriority;
        set => SetProperty(ref _newRulePriority, value);
    }

    public ICommand LoadCommand { get; }

    public ICommand CreateCommand { get; }

    public ICommand ToggleEnabledCommand { get; }

    public ICommand DeleteCommand { get; }

    public async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var rules = await _businessRuleService.GetAllAsync().ConfigureAwait(true);
            Rules.Clear();
            foreach (var rule in rules)
            {
                Rules.Add(rule);
            }

            State = Rules.Count == 0 ? DashboardState.Empty : DashboardState.Loaded;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    private async Task CreateAsync()
    {
        try
        {
            var condition = new BusinessRuleConditionDto(NewRuleField.Trim(), NewRuleOperator, NewRuleValue.Trim());
            var parameterKey = NewRuleActionType switch
            {
                BusinessRuleActionType.ApplyDiscount => "percentage",
                BusinessRuleActionType.TriggerWorkflow => "workflowId",
                _ => "value",
            };
            var action = new BusinessRuleActionDto(NewRuleActionType, new Dictionary<string, string> { [parameterKey] = NewRuleActionValue.Trim() });

            await _businessRuleService.CreateAsync(NewRuleName.Trim(), string.Empty, [condition], action, NewRulePriority, _organizationId, _branchId).ConfigureAwait(true);

            NewRuleName = string.Empty;
            NewRuleField = string.Empty;
            NewRuleValue = string.Empty;
            NewRuleActionValue = string.Empty;
            NewRulePriority = 0;
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
            LogOperationFailed(nameof(CreateAsync));
        }
    }

    private async Task ToggleEnabledAsync(BusinessRuleDto rule)
    {
        try
        {
            await _businessRuleService.SetEnabledAsync(rule.Id, !rule.IsEnabled).ConfigureAwait(true);
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(ToggleEnabledAsync));
        }
    }

    private async Task DeleteAsync(BusinessRuleDto rule)
    {
        try
        {
            await _businessRuleService.DeleteAsync(rule.Id).ConfigureAwait(true);
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(DeleteAsync));
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Automation business rules operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);
}
