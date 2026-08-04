using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Salons;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Infrastructure.Customers;

/// <summary>
/// Owner App Customer CRM Integration: the real, backend-connected
/// <see cref="DomainCustomers.ICustomerRepository"/> - replaces
/// <c>EfCustomerRepository</c> (which stays in the codebase, unreferenced,
/// same convention as every earlier Fake/Ef-&gt;Backend swap - see
/// <c>BackendBookingRepository</c>'s own doc comment). Follows that exact
/// same pattern: <see cref="ISalonContextService"/> resolves the salon,
/// every list read pages through <see cref="Application.Api.Contracts.PagedResponse{T}"/>
/// until exhausted, and <see cref="IEnterpriseContext"/> stamps
/// Organization/Branch onto every mapped customer so
/// <c>CustomerQueryService.ScopeToCurrentSession</c>'s existing filter
/// stays a harmless no-op for backend data rather than silently discarding
/// it - see that class's own doc comment.
///
/// Honesty notes on the mapping, all deliberate:
/// <list type="bullet">
/// <item><see cref="DomainCustomers.Customer.UserId"/> is carried straight
/// through from <see cref="CustomerResponse.UserId"/> - null for a walk-in
/// customer with no linked account. This is what lets
/// <c>Application.Customers.CustomerProfileQueryService.BuildBookingSummaryAsync</c>
/// correctly resolve booking history: ROJAN_Backend's own booking records
/// reference the booking customer's <c>UserId</c>, never this Customer
/// CRM record's own id, so an unresolved link there means an honestly
/// empty booking summary, exactly matching ROJAN_Backend's own
/// <c>GetCustomerBookingsUseCase</c> behavior for an unlinked customer.</item>
/// <item><see cref="DomainCustomers.Customer.LifetimeValue"/> is always
/// present - ROJAN_Backend computes it on every read and returns zero
/// (never null/missing) for a customer with no completed bookings, so
/// there is no "unavailable" case to special-case here, only zero.</item>
/// <item><see cref="DomainCustomers.Customer.Notes"/> (the single vestigial
/// field, distinct from the real <see cref="DomainCustomers.CustomerNote"/>
/// list feature) has no backend equivalent - mapped to
/// <see cref="string.Empty"/>, honest rather than fabricated.</item>
/// <item><see cref="DomainCustomers.Customer.LastContactedAt"/> has no
/// backend equivalent either - mapped from <see cref="CustomerResponse.UpdatedAt"/>,
/// the closest honest proxy ("last time this record changed"), not
/// literally "last contacted."</item>
/// <item><see cref="GetActivityAsync"/> synthesizes a fresh id per entry -
/// ROJAN_Backend's timeline is a read-time merge with no id of its own
/// (see <see cref="CustomerTimelineEntryResponse"/>'s own doc comment),
/// fine for this display-only data.</item>
/// </list>
///
/// <see cref="AddActivityAsync"/> throws - see its own doc comment for why
/// there is no backend endpoint to call, and why it should never actually
/// be reached (<c>CustomerCommandService</c> no longer calls it - see
/// <c>ROJAN_Customer_CRM_Integration_Preparation_Report_v1.md</c> §2).
/// </summary>
public sealed class BackendCustomerRepository(
    IApiClient apiClient,
    ISalonContextService salonContextService,
    IEnterpriseContext enterpriseContext) : DomainCustomers.ICustomerRepository
{
    private const int PageSize = 100;

    public async Task<IReadOnlyList<DomainCustomers.Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var responses = await FetchAllCustomersAsync(salonId, cancellationToken).ConfigureAwait(false);
        return responses.Select(MapCustomer).ToList();
    }

    public async Task<DomainCustomers.Customer?> GetCustomerByIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<CustomerResponse>($"/api/v1/salons/{salonId}/customers/{customerId}", cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == 404)
        {
            return null;
        }

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load customer '{customerId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return MapCustomer(response.Data);
    }

    /// <summary>Newest-first, matching this app's own <see cref="DomainCustomers.ICustomerRepository.GetNotesAsync"/> ordering convention (<c>EfCustomerRepository</c>/<c>FakeCustomerRepository</c>'s existing behavior) - the backend endpoint itself returns oldest-first (see its own doc comment), re-sorted here rather than changed server-side.</summary>
    public async Task<IReadOnlyList<DomainCustomers.CustomerNote>> GetNotesAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<CustomerNoteResponse>>($"/api/v1/salons/{salonId}/customers/{customerId}/notes", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load notes for customer '{customerId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data
            .OrderByDescending(note => note.CreatedAt)
            .Select(note => new DomainCustomers.CustomerNote(note.Id, customerId, note.Text, note.CreatedAt))
            .ToList();
    }

    /// <summary>Oldest-first, matching this app's own <see cref="DomainCustomers.ICustomerRepository.GetTagsAsync"/> ordering convention - already the backend endpoint's own order too, but sorted explicitly here rather than relying on it.</summary>
    public async Task<IReadOnlyList<DomainCustomers.CustomerTag>> GetTagsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<CustomerTagResponse>>($"/api/v1/salons/{salonId}/customers/{customerId}/tags", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load tags for customer '{customerId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data
            .OrderBy(tag => tag.CreatedAt)
            .Select(tag => new DomainCustomers.CustomerTag(tag.Id, customerId, tag.Label, tag.CreatedAt))
            .ToList();
    }

    /// <summary>A customer with no notes/tags/status-changes/linked bookings yet returns an empty page from the backend - mapped straight through to an empty list here, never a failure (same "empty is a valid result, not an error" reasoning as <c>CustomerBookingSummaryDto.Empty</c>).</summary>
    public async Task<IReadOnlyList<DomainCustomers.CustomerActivity>> GetActivityAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var entries = await FetchAllTimelineEntriesAsync(salonId, customerId, cancellationToken).ConfigureAwait(false);

        return entries
            .Select(entry => new DomainCustomers.CustomerActivity(Guid.NewGuid().ToString(), customerId, entry.Description, entry.OccurredAt))
            .ToList();
    }

    public async Task<DomainCustomers.Customer> CreateCustomerAsync(DomainCustomers.Customer customer, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new CreateCustomerRequest(
            customer.FullName,
            NullIfEmpty(customer.Phone),
            NullIfEmpty(customer.Email),
            NullIfEmpty(customer.Company));

        var response = await apiClient
            .PostAsync<CreateCustomerRequest, CustomerResponse>($"/api/v1/salons/{salonId}/customers", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to create customer (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return MapCustomer(response.Data);
    }

    /// <summary><c>ICustomerCommandService.UpdateCustomerAsync</c> is a full-field replacement (see its own doc comment), so every field is always sent - the backend's own "absent means unchanged" PATCH semantics never come into play from this client.</summary>
    public async Task<DomainCustomers.Customer> UpdateCustomerAsync(DomainCustomers.Customer customer, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new UpdateCustomerRequest(
            customer.FullName,
            NullIfEmpty(customer.Phone),
            NullIfEmpty(customer.Email),
            NullIfEmpty(customer.Company),
            MapStatusToBackend(customer.Status));

        var response = await apiClient
            .PatchAsync<UpdateCustomerRequest, CustomerResponse>($"/api/v1/salons/{salonId}/customers/{customer.Id}", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to update customer '{customer.Id}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return MapCustomer(response.Data);
    }

    public async Task<DomainCustomers.CustomerNote> AddNoteAsync(DomainCustomers.CustomerNote note, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .PostAsync<AddCustomerNoteRequest, CustomerNoteResponse>(
                $"/api/v1/salons/{salonId}/customers/{note.CustomerId}/notes",
                new AddCustomerNoteRequest(note.Text),
                cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to add a note to customer '{note.CustomerId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return new DomainCustomers.CustomerNote(response.Data.Id, note.CustomerId, response.Data.Text, response.Data.CreatedAt);
    }

    public async Task<DomainCustomers.CustomerTag> AddTagAsync(DomainCustomers.CustomerTag tag, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .PostAsync<AddCustomerTagRequest, CustomerTagResponse>(
                $"/api/v1/salons/{salonId}/customers/{tag.CustomerId}/tags",
                new AddCustomerTagRequest(tag.Label),
                cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to add a tag to customer '{tag.CustomerId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return new DomainCustomers.CustomerTag(response.Data.Id, tag.CustomerId, response.Data.Label, response.Data.CreatedAt);
    }

    public async Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .DeleteAsync<object?>($"/api/v1/salons/{salonId}/customers/{customerId}/tags/{tagId}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess && response.StatusCode != 404)
        {
            throw new ApiException($"Failed to remove tag '{tagId}' from customer '{customerId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    /// <summary>
    /// ROJAN_Backend has no generic "log an arbitrary activity" endpoint -
    /// every activity it records is a side effect of its own use cases
    /// (tag add/remove, a real status change) or a note's own appearance in
    /// the merged timeline (see <see cref="GetActivityAsync"/>'s own doc
    /// comment). <c>Application.Customers.CustomerCommandService</c> no
    /// longer calls this method for any mutation as of the Customer CRM
    /// Integration Preparation pass (see
    /// <c>ROJAN_Customer_CRM_Integration_Preparation_Report_v1.md</c> §2),
    /// so this should never actually be reached against a backend-sourced
    /// customer - throws rather than silently doing nothing, so a future
    /// caller that does invoke it gets an honest signal instead of a
    /// record that looks written but never reached the server.
    /// </summary>
    public Task<DomainCustomers.CustomerActivity> AddActivityAsync(DomainCustomers.CustomerActivity activity, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "ROJAN_Backend has no generic \"log an arbitrary activity\" endpoint - every activity it records is a side " +
            "effect of its own use cases. CustomerCommandService no longer calls this method for backend-sourced " +
            "customers - see BackendCustomerRepository's own doc comment.");

    private async Task<string> ResolveSalonIdAsync(CancellationToken cancellationToken)
    {
        var salonId = await salonContextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(false);
        return salonId ?? throw new ApiException("The signed-in owner does not manage any salon yet - there is nothing to load customers for.");
    }

    private async Task<List<CustomerResponse>> FetchAllCustomersAsync(string salonId, CancellationToken cancellationToken)
    {
        var all = new List<CustomerResponse>();
        var page = 0;

        while (true)
        {
            var response = await apiClient
                .GetAsync<PagedResponse<CustomerResponse>>($"/api/v1/salons/{salonId}/customers?page={page}&size={PageSize}", cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccess || response.Data is null)
            {
                throw new ApiException($"Failed to load customers (status {response.StatusCode}): {response.ErrorMessage}");
            }

            all.AddRange(response.Data.Content);

            page++;
            if (page >= response.Data.TotalPages || response.Data.Content.Count == 0)
            {
                break;
            }
        }

        return all;
    }

    private async Task<List<CustomerTimelineEntryResponse>> FetchAllTimelineEntriesAsync(string salonId, string customerId, CancellationToken cancellationToken)
    {
        var all = new List<CustomerTimelineEntryResponse>();
        var page = 0;

        while (true)
        {
            var response = await apiClient
                .GetAsync<PagedResponse<CustomerTimelineEntryResponse>>(
                    $"/api/v1/salons/{salonId}/customers/{customerId}/timeline?page={page}&size={PageSize}",
                    cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccess || response.Data is null)
            {
                throw new ApiException($"Failed to load the timeline for customer '{customerId}' (status {response.StatusCode}): {response.ErrorMessage}");
            }

            all.AddRange(response.Data.Content);

            page++;
            if (page >= response.Data.TotalPages || response.Data.Content.Count == 0)
            {
                break;
            }
        }

        return all;
    }

    private DomainCustomers.Customer MapCustomer(CustomerResponse response) => new(
        response.Id,
        response.FullName,
        response.Company ?? string.Empty,
        response.Email ?? string.Empty,
        response.PhoneNumber ?? string.Empty,
        MapStatus(response.Status),
        FormatToman(response.LifetimeValue),
        response.UpdatedAt,
        string.Empty,
        enterpriseContext.CurrentOrganizationId ?? string.Empty,
        enterpriseContext.CurrentBranchId ?? string.Empty,
        response.UserId);

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static DomainCustomers.CustomerStatus MapStatus(string backendStatus) => backendStatus switch
    {
        "LEAD" => DomainCustomers.CustomerStatus.Lead,
        "PROSPECT" => DomainCustomers.CustomerStatus.Prospect,
        "ACTIVE" => DomainCustomers.CustomerStatus.Active,
        "VIP" => DomainCustomers.CustomerStatus.Vip,
        "INACTIVE" => DomainCustomers.CustomerStatus.Inactive,
        "CHURNED" => DomainCustomers.CustomerStatus.Churned,
        _ => throw new ApiException($"Unknown customer status '{backendStatus}' returned by the backend."),
    };

    private static string MapStatusToBackend(DomainCustomers.CustomerStatus status) => status switch
    {
        DomainCustomers.CustomerStatus.Lead => "LEAD",
        DomainCustomers.CustomerStatus.Prospect => "PROSPECT",
        DomainCustomers.CustomerStatus.Active => "ACTIVE",
        DomainCustomers.CustomerStatus.Vip => "VIP",
        DomainCustomers.CustomerStatus.Inactive => "INACTIVE",
        DomainCustomers.CustomerStatus.Churned => "CHURNED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain customer status."),
    };

    private static string FormatToman(decimal amount) => $"{amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} تومان";
}
