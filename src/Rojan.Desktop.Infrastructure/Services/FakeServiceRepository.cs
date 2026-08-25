using Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Infrastructure.Services;

/// <summary>
/// In-memory <see cref="IServiceRepository"/> providing static sample
/// data - Phase 13 explicitly has no backend integration yet, same as
/// every other vertical slice in this app. Instance (not static) mutable
/// state, same reasoning as <c>Customers.FakeCustomerRepository</c>: this
/// fake has real specialist-assignment commands, so it needs to remember
/// writes for the app's lifetime - registered as a DI singleton (see
/// Infrastructure's ServiceCollectionExtensions). Assigned-specialist seed
/// names match the specialists already seeded in
/// <c>Specialists.FakeSpecialistRepository</c> for a cohesive demo - not a
/// real cross-slice link, just consistent naming. The small artificial
/// delays are deliberate, same reasoning as every other fake repository:
/// without them, Loading states would never actually be observable when
/// running the app.
/// </summary>
public sealed class FakeServiceRepository : IServiceRepository
{
    private readonly List<Service> _services;
    private readonly List<SpecialistService> _assignments;
    private readonly List<ServiceCategoryOption> _categories;

    public FakeServiceRepository()
    {
        _categories =
        [
            new ServiceCategoryOption("category-hair", "مو"),
            new ServiceCategoryOption("category-colour", "رنگ"),
            new ServiceCategoryOption("category-nails", "ناخن"),
            new ServiceCategoryOption("category-skin", "پوست"),
            new ServiceCategoryOption("category-spa", "اسپا"),
            new ServiceCategoryOption("category-consultation", "مشاوره"),
        ];

        _services =
        [
            new Service("service-1", "کوتاهی و استایل مو", ServiceCategory.Hair, ServiceStatus.Active,
                60, "650,000 تومان", "کوتاهی کلاسیک همراه با سشوار و فرم‌دهی نهایی.", CategoryId: "category-hair"),
            new Service("service-2", "اصلاح رنگ ریشه", ServiceCategory.Colour, ServiceStatus.Active,
                90, "1,200,000 تومان", "پوشش ریشه و رنگ تک‌مرحله‌ای مو.", CategoryId: "category-colour"),
            new Service("service-3", "پکیج کامل - بالیاژ و استایل", ServiceCategory.Colour, ServiceStatus.Active,
                150, "2,200,000 تومان", "های‌لایت بالیاژ همراه با استایل نهایی مو.", CategoryId: "category-colour"),
            new Service("service-4", "مانیکور", ServiceCategory.Nails, ServiceStatus.Active,
                45, "400,000 تومان", "مانیکور کلاسیک همراه با لاک.", CategoryId: "category-nails"),
            new Service("service-5", "فیشیال ترمیمی", ServiceCategory.Skin, ServiceStatus.Active,
                60, "850,000 تومان", "پاکسازی عمیق و مراقبت از پوست صورت.", CategoryId: "category-skin"),
            new Service("service-6", "ماساژ", ServiceCategory.Spa, ServiceStatus.Active,
                60, "950,000 تومان", "ماساژ آرامش‌بخش بدن.", CategoryId: "category-spa"),
            new Service("service-7", "مشاوره", ServiceCategory.Consultation, ServiceStatus.Active,
                30, "رایگان", "مشاوره رایگان برای مشتریان جدید.", CategoryId: "category-consultation"),
            new Service("service-8", "کراتینه صافی مو", ServiceCategory.Hair, ServiceStatus.Seasonal,
                120, "1,800,000 تومان", "درمان کراتینه برای کاهش وز مو، خدمت فصلی.", CategoryId: "category-hair"),
            new Service("service-9", "فر مو", ServiceCategory.Hair, ServiceStatus.Discontinued,
                90, "1,100,000 تومان", "فر کلاسیک مو، دیگر ارائه نمی‌شود.", CategoryId: "category-hair"),
        ];

        _assignments =
        [
            new SpecialistService("assignment-1", "service-1", "specialist-1", "کیانا رادمنش"),
            new SpecialistService("assignment-2", "service-1", "specialist-2", "سارا امینی"),
            new SpecialistService("assignment-3", "service-2", "specialist-1", "کیانا رادمنش"),
            new SpecialistService("assignment-4", "service-3", "specialist-1", "کیانا رادمنش"),
            new SpecialistService("assignment-5", "service-4", "specialist-3", "مهسا کریمی"),
            new SpecialistService("assignment-6", "service-5", "specialist-3", "مهسا کریمی"),
            new SpecialistService("assignment-7", "service-6", "specialist-3", "مهسا کریمی"),
            new SpecialistService("assignment-8", "service-7", "specialist-2", "سارا امینی"),
        ];
    }

    public async Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _services.ToList();
    }

    public async Task<Service?> GetServiceByIdAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _services.FirstOrDefault(service => service.Id == serviceId);
    }

    public async Task<IReadOnlyList<SpecialistService>> GetAssignedSpecialistsAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _assignments.Where(assignment => assignment.ServiceId == serviceId).ToList();
    }

    public async Task<SpecialistService> AssignSpecialistAsync(SpecialistService assignment, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _assignments.Add(assignment);
        return assignment;
    }

    public async Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _assignments.RemoveAll(assignment => assignment.ServiceId == serviceId && assignment.Id == assignmentId);
    }

    public async Task<IReadOnlyList<ServiceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _categories.ToList();
    }

    public async Task<Service> CreateServiceAsync(string categoryId, string name, string? description, int durationMinutes, decimal price, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var category = _categories.FirstOrDefault(option => option.Id == categoryId);
        var service = new Service(
            Guid.NewGuid().ToString(),
            name,
            MapCategory(category?.Name),
            ServiceStatus.Active,
            durationMinutes,
            FormatToman(price),
            description ?? string.Empty,
            category?.Name,
            categoryId);
        _services.Add(service);
        return service;
    }

    public async Task<Service> UpdateServiceAsync(
        string serviceId,
        string categoryId,
        string name,
        string? description,
        int durationMinutes,
        decimal price,
        ServiceStatus requestedStatus,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _services.FindIndex(existing => existing.Id == serviceId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Service '{serviceId}' was not found.");
        }

        var existing = _services[index];
        if (requestedStatus != existing.Status && !ServiceRules.IsValidTransition(existing.Status, requestedStatus))
        {
            throw new InvalidOperationException($"Cannot transition service from {existing.Status} to {requestedStatus}.");
        }

        var updated = existing with
        {
            Name = name,
            Description = description ?? string.Empty,
            DurationMinutes = durationMinutes,
            Price = FormatToman(price),
            Status = requestedStatus,
        };
        _services[index] = updated;
        return updated;
    }

    private static ServiceCategory MapCategory(string? categoryName) => categoryName?.Trim().ToLowerInvariant() switch
    {
        "مو" => ServiceCategory.Hair,
        "رنگ" => ServiceCategory.Colour,
        "ناخن" => ServiceCategory.Nails,
        "پوست" => ServiceCategory.Skin,
        "اسپا" => ServiceCategory.Spa,
        "مشاوره" => ServiceCategory.Consultation,
        _ => ServiceCategory.Other,
    };

    private static string FormatToman(decimal amount) => $"{amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} تومان";
}
