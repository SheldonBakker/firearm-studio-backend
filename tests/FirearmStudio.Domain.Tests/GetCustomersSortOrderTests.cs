using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class GetCustomersSortOrderTests
{
    private static Customer Individual(string fullName) => new()
    {
        Id = Guid.NewGuid(),
        CustomerType = CustomerType.Individual,
        FullName = fullName,
    };

    private static Customer Company(string companyName) => new()
    {
        Id = Guid.NewGuid(),
        CustomerType = CustomerType.Company,
        FullName = null,
        CompanyName = companyName,
    };

    [Fact]
    public void Company_customers_sort_by_company_name_interleaved_with_individuals()
    {
        var customers = new[]
        {
            Individual("Charlie"),
            Company("Acme Corp"),
            Individual("Alice"),
            Company("Beta Ltd"),
        };

        var sorted = customers.AsQueryable()
            .OrderBy(c => c.FullName ?? c.CompanyName)
            .ThenBy(c => c.Id)
            .ToList();

        Assert.Equal("Acme Corp", sorted[0].CompanyName);
        Assert.Equal("Alice", sorted[1].FullName);
        Assert.Equal("Beta Ltd", sorted[2].CompanyName);
        Assert.Equal("Charlie", sorted[3].FullName);
    }

    [Fact]
    public void Ordering_by_full_name_alone_groups_company_customers_at_null_boundary()
    {
        var customers = new[]
        {
            Individual("Charlie"),
            Company("Acme Corp"),
            Individual("Alice"),
            Company("Beta Ltd"),
        };

        var sorted = customers.AsQueryable()
            .OrderBy(c => c.FullName)
            .ThenBy(c => c.Id)
            .ToList();

        var firstNonNull = sorted.FirstOrDefault(c => c.FullName != null);
        var lastNull = sorted.LastOrDefault(c => c.FullName == null);

        Assert.NotNull(firstNonNull);
        Assert.NotNull(lastNull);

        var firstNonNullIndex = sorted.IndexOf(firstNonNull);
        var lastNullIndex = sorted.IndexOf(lastNull);

        Assert.True(lastNullIndex < firstNonNullIndex,
            "All null-FullName (company) customers group before any individual, not interleaved.");
    }
}
