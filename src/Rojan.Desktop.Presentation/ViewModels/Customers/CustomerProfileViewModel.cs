using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Customers;

/// <summary>
/// Drives the Customer 360 profile panel for one selected customer -
/// statistics cards, tags, notes, and the activity timeline. Owned by
/// <see cref="CustomerPageViewModel"/>, constructed fresh whenever the
/// selected customer changes (never resolved from DI directly, since it
/// needs a customerId only known at selection time - same reasoning
/// <c>ModuleDescriptor</c> uses a factory rather than a static DI
/// registration for per-instance construction).
/// </summary>
public sealed class CustomerProfileViewModel : ViewModelBase
{
    private readonly string _customerId;
    private readonly ICustomerProfileQueryService _profileQueryService;
    private readonly ICustomerCommandService _commandService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private CustomerDto? _customer;
    private string _newNoteText = string.Empty;
    private string _newTagText = string.Empty;
    private CustomerStatus _editableStatus;

    public CustomerProfileViewModel(
        string customerId,
        ICustomerProfileQueryService profileQueryService,
        ICustomerCommandService commandService)
    {
        _customerId = customerId;
        _profileQueryService = profileQueryService;
        _commandService = commandService;

        Notes = new ObservableCollection<CustomerNoteDto>();
        Tags = new ObservableCollection<CustomerTagDto>();
        Activity = new ObservableCollection<CustomerActivityDto>();
        Statistics = new ObservableCollection<CustomerStatDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        AddNoteCommand = new AsyncRelayCommand(_ => AddNoteAsync(), _ => !string.IsNullOrWhiteSpace(NewNoteText));
        AddTagCommand = new AsyncRelayCommand(_ => AddTagAsync(), _ => !string.IsNullOrWhiteSpace(NewTagText));
        RemoveTagCommand = new AsyncRelayCommand(parameter => RemoveTagAsync(parameter as CustomerTagDto));
        SaveChangesCommand = new AsyncRelayCommand(_ => SaveChangesAsync());

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same pattern as
        // CustomerPageViewModel/DashboardPageViewModel.
        _ = LoadAsync();
    }

    public ObservableCollection<CustomerNoteDto> Notes { get; }

    public ObservableCollection<CustomerTagDto> Tags { get; }

    public ObservableCollection<CustomerActivityDto> Activity { get; }

    public ObservableCollection<CustomerStatDto> Statistics { get; }

    public IReadOnlyList<CustomerStatus> AvailableStatuses { get; } = Enum.GetValues<CustomerStatus>();

    public ICommand LoadCommand { get; }

    public ICommand AddNoteCommand { get; }

    public ICommand AddTagCommand { get; }

    public ICommand RemoveTagCommand { get; }

    public ICommand SaveChangesCommand { get; }

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

    public CustomerDto? Customer
    {
        get => _customer;
        private set => SetProperty(ref _customer, value);
    }

    public string NewNoteText
    {
        get => _newNoteText;
        set => SetProperty(ref _newNoteText, value);
    }

    public string NewTagText
    {
        get => _newTagText;
        set => SetProperty(ref _newTagText, value);
    }

    /// <summary>Bound by the status ComboBox - a local edit buffer, synced from <see cref="Customer"/> on every load so it always starts matching the persisted value.</summary>
    public CustomerStatus EditableStatus
    {
        get => _editableStatus;
        set => SetProperty(ref _editableStatus, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var profile = await _profileQueryService.GetProfileAsync(_customerId).ConfigureAwait(true);

            Customer = profile.Customer;
            EditableStatus = profile.Customer.Status;
            ReplaceAll(Notes, profile.Notes);
            ReplaceAll(Tags, profile.Tags);
            ReplaceAll(Activity, profile.Activity);
            ReplaceAll(Statistics, profile.Statistics);

            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page/profile ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    private async Task AddNoteAsync()
    {
        await _commandService.AddNoteAsync(_customerId, NewNoteText).ConfigureAwait(true);
        NewNoteText = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task AddTagAsync()
    {
        await _commandService.AddTagAsync(_customerId, NewTagText).ConfigureAwait(true);
        NewTagText = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task RemoveTagAsync(CustomerTagDto? tag)
    {
        if (tag is null)
        {
            return;
        }

        await _commandService.RemoveTagAsync(_customerId, tag.Id).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task SaveChangesAsync()
    {
        if (Customer is null)
        {
            return;
        }

        var request = new UpdateCustomerRequest(
            Customer.Id,
            Customer.FullName,
            Customer.Company,
            Customer.Email,
            Customer.Phone,
            EditableStatus,
            Customer.LifetimeValue,
            Customer.Notes);

        await _commandService.UpdateCustomerAsync(request).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    private static void ReplaceAll<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
