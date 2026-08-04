using Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Domain.Tests.Customers;

/// <summary>
/// Minimal smoke coverage - Domain currently has no behavior beyond data
/// shapes and repository contracts (see Phase 08 Testing Strategy plan),
/// so this documents the value-equality contract CustomerPageViewModel's
/// re-selection logic (Presentation.Tests) implicitly relies on, rather
/// than testing compiler-generated behavior for its own sake.
/// </summary>
public sealed class CustomerTests
{
    private static Customer MakeCustomer(string id = "customer-1") =>
        new(id, "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231",
            CustomerStatus.Active, "$4,820", DateTimeOffset.UnixEpoch, "Notes", "org-1", "branch-1");

    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var first = MakeCustomer();
        var second = MakeCustomer();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_DifferentId_AreNotEqual()
    {
        var first = MakeCustomer("customer-1");
        var second = MakeCustomer("customer-2");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void UserId_OmittedAtConstruction_DefaultsToNull()
    {
        // Owner App Customer CRM Integration: UserId is a trailing optional param specifically so
        // every pre-existing positional Customer(...) call site (local/EF-backed data has no such
        // concept) keeps compiling and behaving unchanged.
        var customer = MakeCustomer();

        Assert.Null(customer.UserId);
    }
}
