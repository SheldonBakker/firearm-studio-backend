using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Packages;

public sealed record PackageItemDto(Guid Id, string Description, decimal Quantity, int SortOrder)
{
    public static PackageItemDto FromEntity(PackageItem i) =>
        new(i.Id, i.Description, i.Quantity, i.SortOrder);
}

public sealed record PackageResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int DurationMinutes,
    int MaxShooters,
    bool IsActive,
    IReadOnlyList<PackageItemDto> Items)
{
    public static Expression<Func<Package, PackageResponse>> QueryProjection => p => new PackageResponse(
        p.Id, p.Name, p.Description, p.Price, p.DurationMinutes, p.MaxShooters, p.IsActive,
        p.Items
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(i => new PackageItemDto(i.Id, i.Description, i.Quantity, i.SortOrder))
            .ToList());

    public static PackageResponse FromEntity(Package p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.DurationMinutes, p.MaxShooters, p.IsActive,
            p.Items
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .Select(PackageItemDto.FromEntity)
                .ToList());
}

public sealed record PackageListItemDto(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxShooters,
    bool IsActive,
    int ItemCount)
{
    public static Expression<Func<Package, PackageListItemDto>> QueryProjection => p => new PackageListItemDto(
        p.Id, p.Name, p.Price, p.DurationMinutes, p.MaxShooters, p.IsActive, p.Items.Count);
}

public sealed record PublicPackageResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int DurationMinutes,
    int MaxShooters,
    IReadOnlyList<PublicPackageItemDto> Items)
{
    public static Expression<Func<Package, PublicPackageResponse>> QueryProjection => p => new PublicPackageResponse(
        p.Id, p.Name, p.Description, p.Price, p.DurationMinutes, p.MaxShooters,
        p.Items
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(i => new PublicPackageItemDto(i.Description, i.Quantity))
            .ToList());
}

public sealed record PublicPackageItemDto(string Description, decimal Quantity);

public sealed record PackageItemRequest(string Description, decimal Quantity, int SortOrder);

public sealed record CreatePackageRequest(
    string Name,
    string? Description,
    decimal Price,
    int DurationMinutes,
    int MaxShooters,
    IReadOnlyList<PackageItemRequest> Items);

public sealed record UpdatePackageRequest(
    Optional<string> Name,
    Optional<string?> Description,
    Optional<decimal> Price,
    Optional<int> DurationMinutes,
    Optional<int> MaxShooters,
    Optional<bool> IsActive,
    Optional<IReadOnlyList<PackageItemRequest>> Items);
