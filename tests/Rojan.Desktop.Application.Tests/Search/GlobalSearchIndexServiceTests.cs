using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Application.Tests.Search;

/// <summary>Exercises <see cref="GlobalSearchIndexService"/>'s aggregation/mapping of live business data from every module's query service into <see cref="SearchCandidate"/>.</summary>
public sealed class GlobalSearchIndexServiceTests
{
    private readonly GlobalSearchIndexService _service = new(
        new StubCustomerQueryServiceForSearch(),
        new StubBookingQueryServiceForSearch(),
        new StubSpecialistQueryServiceForSearch(),
        new StubServiceQueryServiceForSearch(),
        new StubProductQueryServiceForSearch());

    [Fact]
    public async Task GetCandidatesAsync_ReturnsOneCandidatePerSeededRowAcrossEveryModule()
    {
        var candidates = await _service.GetCandidatesAsync();

        Assert.Equal(5, candidates.Count);
    }

    [Fact]
    public async Task GetCandidatesAsync_MapsCustomerWithCorrectTypeTitleAndActionKey()
    {
        var candidates = await _service.GetCandidatesAsync();

        var customer = Assert.Single(candidates, c => c.Type == SearchResultType.Customer);
        Assert.Equal("Sarah Johnson", customer.Title);
        Assert.Equal("page:customers", customer.ActionKey);
        Assert.Contains("sarah@acme.com", customer.Keywords);
    }

    [Fact]
    public async Task GetCandidatesAsync_MapsBookingWithDenormalizedTitle()
    {
        var candidates = await _service.GetCandidatesAsync();

        var booking = Assert.Single(candidates, c => c.Type == SearchResultType.Booking);
        Assert.Contains("Sarah Johnson", booking.Title);
        Assert.Contains("Haircut", booking.Title);
        Assert.Equal("page:bookings", booking.ActionKey);
    }

    [Fact]
    public async Task GetCandidatesAsync_MapsSpecialistWithTitleAsSubtitle()
    {
        var candidates = await _service.GetCandidatesAsync();

        var specialist = Assert.Single(candidates, c => c.Type == SearchResultType.Specialist);
        Assert.Equal("Alex Stylist", specialist.Title);
        Assert.Equal("Senior Stylist", specialist.Subtitle);
        Assert.Equal("page:specialists", specialist.ActionKey);
    }

    [Fact]
    public async Task GetCandidatesAsync_MapsServiceWithCategoryAsKeyword()
    {
        var candidates = await _service.GetCandidatesAsync();

        var service = Assert.Single(candidates, c => c.Type == SearchResultType.Service);
        Assert.Equal("Haircut", service.Title);
        Assert.Equal("page:services", service.ActionKey);
        Assert.Contains("Hair", service.Keywords);
    }

    [Fact]
    public async Task GetCandidatesAsync_MapsProductWithSkuAsSubtitleAndKeyword()
    {
        var candidates = await _service.GetCandidatesAsync();

        var product = Assert.Single(candidates, c => c.Type == SearchResultType.Product);
        Assert.Equal("Shampoo", product.Title);
        Assert.Equal("SKU-100", product.Subtitle);
        Assert.Equal("page:inventory", product.ActionKey);
        Assert.Contains("SKU-100", product.Keywords);
    }

    [Fact]
    public async Task GetCandidatesAsync_EveryCandidateHasAUniqueId()
    {
        var candidates = await _service.GetCandidatesAsync();

        var distinctIds = candidates.Select(c => c.Id).Distinct().Count();
        Assert.Equal(candidates.Count, distinctIds);
    }
}
