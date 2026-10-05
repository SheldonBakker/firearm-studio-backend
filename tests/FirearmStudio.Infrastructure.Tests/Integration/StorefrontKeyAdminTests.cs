using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Companies;
using FirearmStudio.Application.Companies.GetStorefrontAccess;
using FirearmStudio.Application.Companies.RevokeStorefrontKey;
using FirearmStudio.Application.Companies.RotateStorefrontKey;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class StorefrontKeyAdminTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private static async Task<Guid> SeedCompanyAsync(TestDatabaseFixture fix)
    {
        var companyId = Guid.NewGuid();
        await using var seed = fix.CreateDbContext();
        await seed.Database.MigrateAsync();
        seed.Companies.Add(new Company { Id = companyId, Name = "Storefront Test" });
        await seed.SaveChangesAsync();
        return companyId;
    }

    private static FakeCurrentUserService MakeUser(Guid? companyId) =>
        new(new CurrentUser { Id = Guid.NewGuid(), IsAuthenticated = true, CompanyId = companyId });

    [Fact]
    public async Task Get_returns_null_key_when_none_set()
    {
        var companyId = await SeedCompanyAsync(fixture);
        var currentUser = MakeUser(companyId);

        await using var db = fixture.CreateDbContext(companyId);
        var handler = new GetStorefrontAccessQueryHandler(db, currentUser);
        var result = await handler.Handle(new GetStorefrontAccessQuery(), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(companyId, result.Value.CompanyId);
        Assert.Null(result.Value.Key);
    }

    [Fact]
    public async Task Rotate_sets_key_with_correct_format_and_writes_audit_row()
    {
        var companyId = await SeedCompanyAsync(fixture);
        var currentUser = MakeUser(companyId);

        await using var db = fixture.CreateDbContext(companyId);
        var handler = new RotateStorefrontKeyCommandHandler(db, currentUser);
        var result = await handler.Handle(new RotateStorefrontKeyCommand(), CancellationToken.None);

        Assert.False(result.IsError);
        var key = result.Value.Key;
        Assert.NotNull(key);
        Assert.Equal(StorefrontKeyConstants.Length, key.Length);
        Assert.StartsWith(StorefrontKeyConstants.Prefix, key);

        var auditRow = await db.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.CompanyId == companyId && a.EntityType == nameof(Company))
            .SingleAsync();

        Assert.Equal(StorefrontAudit.Rotated, auditRow.Action);
        Assert.Contains(StorefrontKey.AuditSuffix(key), auditRow.NewValue);
        Assert.DoesNotContain(key, auditRow.NewValue);
    }

    [Fact]
    public async Task Rotate_twice_yields_different_keys()
    {
        var companyId = await SeedCompanyAsync(fixture);
        var currentUser = MakeUser(companyId);

        string? firstKey, secondKey;

        await using (var db1 = fixture.CreateDbContext(companyId))
        {
            var r1 = await new RotateStorefrontKeyCommandHandler(db1, currentUser)
                .Handle(new RotateStorefrontKeyCommand(), CancellationToken.None);
            firstKey = r1.Value.Key;
        }

        await using (var db2 = fixture.CreateDbContext(companyId))
        {
            var r2 = await new RotateStorefrontKeyCommandHandler(db2, currentUser)
                .Handle(new RotateStorefrontKeyCommand(), CancellationToken.None);
            secondKey = r2.Value.Key;
        }

        Assert.NotEqual(firstKey, secondKey);
    }

    [Fact]
    public async Task Revoke_nulls_the_key_and_writes_audit_row()
    {
        var companyId = await SeedCompanyAsync(fixture);
        var currentUser = MakeUser(companyId);

        await using (var db1 = fixture.CreateDbContext(companyId))
        {
            await new RotateStorefrontKeyCommandHandler(db1, currentUser)
                .Handle(new RotateStorefrontKeyCommand(), CancellationToken.None);
        }

        await using (var db2 = fixture.CreateDbContext(companyId))
        {
            var result = await new RevokeStorefrontKeyCommandHandler(db2, currentUser)
                .Handle(new RevokeStorefrontKeyCommand(), CancellationToken.None);
            Assert.False(result.IsError);
        }

        await using var db3 = fixture.CreateDbContext(companyId);
        var company = await db3.Companies.Where(c => c.Id == companyId).SingleAsync();
        Assert.Null(company.StorefrontKey);

        var revokeAudit = await db3.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.CompanyId == companyId && a.EntityType == nameof(Company) && a.Action == StorefrontAudit.Revoked)
            .SingleAsync();

        Assert.NotNull(revokeAudit);
    }

    [Fact]
    public async Task Revoke_when_already_null_writes_no_audit_row()
    {
        var companyId = await SeedCompanyAsync(fixture);
        var currentUser = MakeUser(companyId);

        await using var db = fixture.CreateDbContext(companyId);
        var result = await new RevokeStorefrontKeyCommandHandler(db, currentUser)
            .Handle(new RevokeStorefrontKeyCommand(), CancellationToken.None);

        Assert.False(result.IsError);

        var auditCount = await db.AuditLogs
            .IgnoreQueryFilters()
            .CountAsync(a => a.CompanyId == companyId && a.EntityType == nameof(Company));

        Assert.Equal(0, auditCount);
    }

    [Fact]
    public async Task User_without_company_gets_not_found_on_all_three()
    {
        await using var db = fixture.CreateDbContext();
        await db.Database.MigrateAsync();

        var currentUser = MakeUser(null);

        var getResult = await new GetStorefrontAccessQueryHandler(db, currentUser)
            .Handle(new GetStorefrontAccessQuery(), CancellationToken.None);
        Assert.True(getResult.IsError);
        Assert.Equal(GetStorefrontAccessQueryHandler.ErrorCodes.NotFound, getResult.FirstError.Code);

        var rotateResult = await new RotateStorefrontKeyCommandHandler(db, currentUser)
            .Handle(new RotateStorefrontKeyCommand(), CancellationToken.None);
        Assert.True(rotateResult.IsError);
        Assert.Equal(RotateStorefrontKeyCommandHandler.ErrorCodes.NotFound, rotateResult.FirstError.Code);

        var revokeResult = await new RevokeStorefrontKeyCommandHandler(db, currentUser)
            .Handle(new RevokeStorefrontKeyCommand(), CancellationToken.None);
        Assert.True(revokeResult.IsError);
        Assert.Equal(RevokeStorefrontKeyCommandHandler.ErrorCodes.NotFound, revokeResult.FirstError.Code);
    }

    private sealed class FakeCurrentUserService(CurrentUser user) : ICurrentUserService
    {
        public CurrentUser User => user;
    }
}
