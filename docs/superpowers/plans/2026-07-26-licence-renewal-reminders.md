# Licence Renewal Reminders Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Nightly job that transitions licence statuses and emits tiered (90/60/30/expired) Klaviyo renewal-reminder events through the existing outbox pipeline.

**Architecture:** A pure domain planner computes the reminder tier and target status per licence. A nightly hosted service iterates active tenants, applies status changes, dedups via a `licence_reminders` table (unique on licence + tier), and enqueues outbox messages. The existing `OutboxProcessorService` routes the new message type to a Klaviyo dispatcher; Klaviyo flows own the actual emails.

**Tech Stack:** .NET 10, ASP.NET Core hosted services, EF Core 10 + Npgsql (native Postgres enums, snake_case via EFCore.NamingConventions), xUnit (new test project), Klaviyo HTTP client (existing).

**Spec:** `docs/superpowers/specs/2026-07-26-licence-renewal-reminders-design.md`

## Global Constraints

- Never use the em-dash character anywhere (code, comments, commits, docs); use a plain hyphen.
- No Claude/AI attribution or Co-Authored-By lines in commits.
- NEVER run `dotnet ef database update` or apply migrations. The connection string in `.env` may point at production (this caused a real incident, see `docs/code-review-2026-07-25.md`). Generate migrations only; the user applies them manually.
- Work on branch `feature/licence-renewal-reminders` (branch off `main` before Task 1).
- Central package management: package versions go in `Directory.Packages.props`, project files reference packages without `Version` attributes.
- `TargetFramework` (net10.0), `Nullable`, `ImplicitUsings` come from `Directory.Build.props`; do not redeclare in csproj.
- British spelling: Licence, not License.
- Build check command: `dotnet build FirearmStudio.slnx`.

---

### Task 1: Test project + LicenceReminderTier enum + LicenceReminderPlanner

**Files:**
- Modify: `Directory.Packages.props` (add test packages)
- Create: `tests/FirearmStudio.Domain.Tests/FirearmStudio.Domain.Tests.csproj`
- Create: `tests/FirearmStudio.Domain.Tests/LicenceReminderPlannerTests.cs`
- Create: `src/FirearmStudio.Domain/Enums/LicenceReminderTier.cs`
- Create: `src/FirearmStudio.Domain/Services/LicenceReminderPlanner.cs` (new `Services` folder)

**Interfaces:**
- Consumes: `LicenceStatus` enum (`FirearmStudio.Domain.Enums`: Valid, RenewalDue, Expired, Unknown).
- Produces: `LicenceReminderTier` enum (`Days90, Days60, Days30, Expired`) and `LicenceReminderPlanner.Plan(LicenceStatus currentStatus, DateOnly expiresOn, DateOnly today)` returning `LicenceReminderPlan(LicenceReminderTier? Tier, LicenceStatus Status)`. Tasks 2 and 4 rely on these exact names.

- [ ] **Step 1: Create branch**

```bash
git checkout -b feature/licence-renewal-reminders
```

- [ ] **Step 2: Add test packages to central package management**

In `Directory.Packages.props`, add inside the existing `<ItemGroup>`:

```xml
    <!-- Testing -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.0" />
```

(If restore reports a newer stable patch, use it; these are floors, not pins.)

- [ ] **Step 3: Create the test project and add it to the solution**

`tests/FirearmStudio.Domain.Tests/FirearmStudio.Domain.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- TargetFramework / Nullable / ImplicitUsings come from Directory.Build.props -->

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\FirearmStudio.Domain\FirearmStudio.Domain.csproj" />
  </ItemGroup>

</Project>
```

Then:

```bash
dotnet sln FirearmStudio.slnx add tests/FirearmStudio.Domain.Tests/FirearmStudio.Domain.Tests.csproj
```

Note: `Directory.Build.props` sits at the repo root, so it applies to `tests/` too. If the build complains about missing usings for xunit, that is expected until Step 4's file exists.

- [ ] **Step 4: Write the failing tests**

`tests/FirearmStudio.Domain.Tests/LicenceReminderPlannerTests.cs`:

```csharp
using FirearmStudio.Domain.Enums;
using FirearmStudio.Domain.Services;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class LicenceReminderPlannerTests
{
    private static readonly DateOnly Today = new(2026, 7, 26);

    private static LicenceReminderPlan PlanWithDaysRemaining(
        int daysRemaining, LicenceStatus currentStatus = LicenceStatus.Valid)
        => LicenceReminderPlanner.Plan(currentStatus, Today.AddDays(daysRemaining), Today);

    [Theory]
    [InlineData(120, null)]
    [InlineData(91, null)]
    [InlineData(90, LicenceReminderTier.Days90)]
    [InlineData(61, LicenceReminderTier.Days90)]
    [InlineData(60, LicenceReminderTier.Days60)]
    [InlineData(31, LicenceReminderTier.Days60)]
    [InlineData(30, LicenceReminderTier.Days30)]
    [InlineData(1, LicenceReminderTier.Days30)]
    [InlineData(0, LicenceReminderTier.Days30)]
    [InlineData(-1, LicenceReminderTier.Expired)]
    [InlineData(-365, LicenceReminderTier.Expired)]
    public void Plan_returns_expected_tier(int daysRemaining, LicenceReminderTier? expectedTier)
    {
        var plan = PlanWithDaysRemaining(daysRemaining);

        Assert.Equal(expectedTier, plan.Tier);
    }

    [Theory]
    [InlineData(120, LicenceStatus.Valid)]
    [InlineData(91, LicenceStatus.Valid)]
    [InlineData(90, LicenceStatus.RenewalDue)]
    [InlineData(30, LicenceStatus.RenewalDue)]
    [InlineData(0, LicenceStatus.RenewalDue)]
    [InlineData(-1, LicenceStatus.Expired)]
    public void Plan_returns_expected_status(int daysRemaining, LicenceStatus expectedStatus)
    {
        var plan = PlanWithDaysRemaining(daysRemaining);

        Assert.Equal(expectedStatus, plan.Status);
    }

    [Theory]
    [InlineData(120)]
    [InlineData(45)]
    [InlineData(-10)]
    public void Plan_skips_unknown_licences_entirely(int daysRemaining)
    {
        var plan = PlanWithDaysRemaining(daysRemaining, LicenceStatus.Unknown);

        Assert.Null(plan.Tier);
        Assert.Equal(LicenceStatus.Unknown, plan.Status);
    }

    [Fact]
    public void Plan_recovers_status_when_expiry_moves_out()
    {
        // A licence marked Expired whose ExpiresOn was corrected to the future goes back to Valid.
        var plan = PlanWithDaysRemaining(120, LicenceStatus.Expired);

        Assert.Equal(LicenceStatus.Valid, plan.Status);
        Assert.Null(plan.Tier);
    }
}
```

- [ ] **Step 5: Run tests to verify they fail**

Run: `dotnet test tests/FirearmStudio.Domain.Tests`
Expected: build FAILURE with "The type or namespace name 'Services' does not exist" and unknown `LicenceReminderTier` (compile-time failure counts as red here).

- [ ] **Step 6: Implement the enum and planner**

`src/FirearmStudio.Domain/Enums/LicenceReminderTier.cs`:

```csharp
namespace FirearmStudio.Domain.Enums;

public enum LicenceReminderTier
{
    Days90,
    Days60,
    Days30,
    Expired,
}
```

`src/FirearmStudio.Domain/Services/LicenceReminderPlanner.cs`:

```csharp
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Services;

public readonly record struct LicenceReminderPlan(LicenceReminderTier? Tier, LicenceStatus Status);

/// <summary>
/// Pure scheduling rules for licence renewal reminders. Only the tier the licence is
/// currently in is ever returned; missed earlier tiers are never backfilled. Licences
/// with status <see cref="LicenceStatus.Unknown"/> are left untouched: Unknown is a
/// data-quality signal, not a scheduling state.
/// </summary>
public static class LicenceReminderPlanner
{
    public static LicenceReminderPlan Plan(LicenceStatus currentStatus, DateOnly expiresOn, DateOnly today)
    {
        if (currentStatus == LicenceStatus.Unknown)
        {
            return new LicenceReminderPlan(null, LicenceStatus.Unknown);
        }

        var daysRemaining = expiresOn.DayNumber - today.DayNumber;

        // The 90-day boundary matches the renewal_due_on computed column (expires_on - 90).
        var tier = daysRemaining switch
        {
            < 0 => LicenceReminderTier.Expired,
            <= 30 => LicenceReminderTier.Days30,
            <= 60 => LicenceReminderTier.Days60,
            <= 90 => LicenceReminderTier.Days90,
            _ => (LicenceReminderTier?)null,
        };

        var status = daysRemaining switch
        {
            < 0 => LicenceStatus.Expired,
            <= 90 => LicenceStatus.RenewalDue,
            _ => LicenceStatus.Valid,
        };

        return new LicenceReminderPlan(tier, status);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/FirearmStudio.Domain.Tests`
Expected: PASS, 21 test cases, 0 failures.

- [ ] **Step 8: Build the full solution**

Run: `dotnet build FirearmStudio.slnx`
Expected: no errors, no new warnings.

- [ ] **Step 9: Commit**

```bash
git add Directory.Packages.props FirearmStudio.slnx tests/ src/FirearmStudio.Domain/
git commit -m "feat: add licence reminder planner with tier and status rules"
```

---

### Task 2: LicenceReminder entity, EF configuration, and migration

**Files:**
- Create: `src/FirearmStudio.Domain/Entities/LicenceReminder.cs`
- Create: `src/FirearmStudio.Infrastructure/Persistence/Configurations/LicenceReminderConfigurations.cs`
- Modify: `src/FirearmStudio.Infrastructure/Persistence/SupabaseDataSourceFactory.cs` (both MapEnum methods)
- Modify: `src/FirearmStudio.Application/Abstractions/IApplicationDbContext.cs` (new DbSet)
- Modify: `src/FirearmStudio.Infrastructure/Persistence/ApplicationDbContext.cs` (new DbSet)
- Create (generated): `src/FirearmStudio.Infrastructure/Migrations/<timestamp>_AddLicenceReminders.cs` + Designer

**Interfaces:**
- Consumes: `LicenceReminderTier` (Task 1), `BaseEntity`, `ITenantEntity`, `FirearmLicence`.
- Produces: `LicenceReminder` entity with `CompanyId`, `LicenceId`, `Tier` and `DbSet<LicenceReminder> LicenceReminders` on `IApplicationDbContext`. Task 4 relies on these exact names.

- [ ] **Step 1: Create the entity**

`src/FirearmStudio.Domain/Entities/LicenceReminder.cs`:

```csharp
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public sealed class LicenceReminder : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid LicenceId { get; set; }

    public LicenceReminderTier Tier { get; set; }

    public FirearmLicence? Licence { get; set; }
}
```

- [ ] **Step 2: Create the EF configuration**

First read `src/FirearmStudio.Infrastructure/Persistence/Configurations/FirearmConfigurations.cs` (licence section around lines 70-95) to copy the exact `ConfigureTenant()` usage and style. Then create `LicenceReminderConfigurations.cs`:

```csharp
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FirearmStudio.Infrastructure.Persistence.Configurations;

internal sealed class LicenceReminderConfiguration : IEntityTypeConfiguration<LicenceReminder>
{
    public void Configure(EntityTypeBuilder<LicenceReminder> builder)
    {
        builder.ConfigureTenant();

        builder.HasIndex(x => new { x.LicenceId, x.Tier }).IsUnique();

        builder.HasOne(x => x.Licence)
            .WithMany()
            .HasForeignKey(x => x.LicenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Match `ConfigureTenant()`'s actual signature from the neighboring configurations (it is the shared tenant FK + filter helper). Configurations are picked up the same way the existing ones are (check `ApplicationDbContext.OnModelCreating` for `ApplyConfigurationsFromAssembly` or explicit application, and follow suit).

- [ ] **Step 3: Register the Postgres enum mapping**

In `src/FirearmStudio.Infrastructure/Persistence/SupabaseDataSourceFactory.cs`, both mapping methods gain one line each, matching the existing style:

```csharp
builder.MapEnum<LicenceReminderTier>("licence_reminder_tier");
```

and

```csharp
options.MapEnum<LicenceReminderTier>("licence_reminder_tier");
```

- [ ] **Step 4: Add the DbSet to both context surfaces**

`IApplicationDbContext.cs` (next to `DbSet<FirearmLicence> FirearmLicences`):

```csharp
DbSet<LicenceReminder> LicenceReminders { get; }
```

`ApplicationDbContext.cs`: add the matching `public DbSet<LicenceReminder> LicenceReminders => Set<LicenceReminder>();` or auto-property, whichever style the existing DbSets use (read the file and match exactly).

- [ ] **Step 5: Build**

Run: `dotnet build FirearmStudio.slnx`
Expected: no errors.

- [ ] **Step 6: Generate the migration (do NOT apply it)**

```bash
dotnet ef migrations add AddLicenceReminders -p src/FirearmStudio.Infrastructure -s src/FirearmStudio.WebApi
```

Inspect the generated migration:
- It must create table `licence_reminders` with `company_id`, `licence_id`, `tier` (type `licence_reminder_tier`), `created_at`, `updated_at`.
- It must create/alter the database enum `licence_reminder_tier` with labels `days90, days60, days30, expired` (via the `Npgsql:Enum` annotation / `AlterDatabase`). If labels differ, that is fine as long as they come from the Npgsql name translator; do not hand-edit labels.
- Unique index on `(licence_id, tier)` present.

- [ ] **Step 7: Append the RLS hardening SQL to the migration**

At the end of the generated `Up` method, matching the pattern in `20260704235343_AddRangeBookingModule.cs` lines 293-308:

```csharp
migrationBuilder.Sql(
    """
    ALTER TABLE public.licence_reminders ENABLE ROW LEVEL SECURITY;

    REVOKE ALL PRIVILEGES ON TABLE public.licence_reminders FROM anon, authenticated;
    """);
```

- [ ] **Step 8: Build again and run tests**

Run: `dotnet build FirearmStudio.slnx && dotnet test tests/FirearmStudio.Domain.Tests`
Expected: both green. Do NOT run `dotnet ef database update`.

- [ ] **Step 9: Commit**

```bash
git add src/FirearmStudio.Domain/Entities/LicenceReminder.cs src/FirearmStudio.Infrastructure/ src/FirearmStudio.Application/Abstractions/IApplicationDbContext.cs
git commit -m "feat: add licence_reminders table for reminder dedup tracking"
```

---

### Task 3: Outbox message type, payload, Klaviyo dispatcher, processor routing

**Files:**
- Modify: `src/FirearmStudio.Application/Abstractions/OutboxMessageTypes.cs`
- Modify: `src/FirearmStudio.Application/Model/Options/KlaviyoSettings.cs`
- Create: `src/FirearmStudio.Application/Abstractions/OutboxJson.cs` (moved from Bookings)
- Modify: `src/FirearmStudio.Application/Bookings/BookingRequestedPayload.cs` (remove OutboxJson class)
- Create: `src/FirearmStudio.Application/Licences/Reminders/LicenceRenewalReminderPayload.cs`
- Create: `src/FirearmStudio.Application/Abstractions/ILicenceRenewalReminderDispatcher.cs`
- Create: `src/FirearmStudio.Application/Licences/Reminders/LicenceRenewalReminderDispatcher.cs`
- Modify: `src/FirearmStudio.Application/Extensions/DependencyInjection.cs`
- Modify: `src/FirearmStudio.WebApi/BackgroundJobs/OutboxProcessorService.cs`

**Interfaces:**
- Consumes: `IKlaviyoClient.TrackEventAsync(string metricName, string email, string? name, IReadOnlyDictionary<string, object?> properties, CancellationToken ct)` (verify the exact signature in `src/FirearmStudio.Application/Abstractions/IKlaviyoClient.cs` and match it), `KlaviyoSettings`, `OutboxMessageTypes`.
- Produces: `OutboxMessageTypes.LicenceRenewalReminder` constant, `LicenceRenewalReminderPayload` record, `ILicenceRenewalReminderDispatcher.DispatchAsync(string payloadJson, CancellationToken ct)`, `KlaviyoSettings.LicenceRenewalMetricName`, and shared `OutboxJson` in `FirearmStudio.Application.Abstractions`. Task 4 serializes the payload with `OutboxJson.Options`.

- [ ] **Step 1: Move OutboxJson to Abstractions**

Create `src/FirearmStudio.Application/Abstractions/OutboxJson.cs`:

```csharp
using System.Text.Json;

namespace FirearmStudio.Application.Abstractions;

internal static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
```

Delete the `OutboxJson` class (and its now-unused `using System.Text.Json;` if nothing else needs it) from `src/FirearmStudio.Application/Bookings/BookingRequestedPayload.cs`. The two consumers (`BookingRequestedOutbox.cs`, `BookingRequestedDispatcher.cs`) already have `using FirearmStudio.Application.Abstractions;`, so they compile unchanged.

- [ ] **Step 2: Add the outbox type constant and Klaviyo setting**

`OutboxMessageTypes.cs`, below `BookingRequested`:

```csharp
public const string LicenceRenewalReminder = "LicenceRenewalReminder";
```

`KlaviyoSettings.cs`, below `BookingRequestedMetricName`:

```csharp
public string LicenceRenewalMetricName { get; init; } = "Licence Renewal Reminder";
```

- [ ] **Step 3: Create the payload record**

`src/FirearmStudio.Application/Licences/Reminders/LicenceRenewalReminderPayload.cs`:

```csharp
namespace FirearmStudio.Application.Licences.Reminders;

internal sealed record LicenceRenewalReminderPayload(
    string Email,
    string? CustomerName,
    string LicenceNumber,
    DateOnly ExpiresOn,
    int DaysUntilExpiry,
    string Tier,
    string FirearmMake,
    string? FirearmModel,
    string SerialNumber,
    Guid CompanyId,
    string CompanyName);
```

`Tier` is the `LicenceReminderTier` enum name as a string (e.g. "Days30") so the JSON contract is stable regardless of enum reordering.

- [ ] **Step 4: Create the dispatcher interface and implementation**

`src/FirearmStudio.Application/Abstractions/ILicenceRenewalReminderDispatcher.cs`:

```csharp
namespace FirearmStudio.Application.Abstractions;

public interface ILicenceRenewalReminderDispatcher
{
    Task DispatchAsync(string payloadJson, CancellationToken cancellationToken);
}
```

`src/FirearmStudio.Application/Licences/Reminders/LicenceRenewalReminderDispatcher.cs` (mirrors `BookingRequestedDispatcher`):

```csharp
using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;

namespace FirearmStudio.Application.Licences.Reminders;

internal sealed class LicenceRenewalReminderDispatcher(
    IKlaviyoClient klaviyo,
    KlaviyoSettings settings) : ILicenceRenewalReminderDispatcher
{
    public async Task DispatchAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<LicenceRenewalReminderPayload>(payloadJson, OutboxJson.Options)
            ?? throw new InvalidOperationException("Licence-renewal-reminder outbox payload deserialized to null.");

        var properties = new Dictionary<string, object?>
        {
            ["licence_number"] = payload.LicenceNumber,
            ["expires_on"] = payload.ExpiresOn.ToString("yyyy-MM-dd"),
            ["days_until_expiry"] = payload.DaysUntilExpiry,
            ["tier"] = payload.Tier,
            ["firearm_make"] = payload.FirearmMake,
            ["firearm_model"] = payload.FirearmModel,
            ["serial_number"] = payload.SerialNumber,
            ["company_id"] = payload.CompanyId,
            ["company_name"] = payload.CompanyName,
        };

        await klaviyo.TrackEventAsync(
            settings.LicenceRenewalMetricName,
            payload.Email,
            payload.CustomerName,
            properties,
            cancellationToken);
    }
}
```

Properties are already flat, so `BookingRequestedNotifier.Flatten` is not needed. Verify `TrackEventAsync`'s exact parameter types in `IKlaviyoClient.cs` and adjust the call if the dictionary parameter type differs.

- [ ] **Step 5: Register in DI**

`src/FirearmStudio.Application/Extensions/DependencyInjection.cs`, next to the booking dispatcher registration (line ~21):

```csharp
services.AddScoped<ILicenceRenewalReminderDispatcher, LicenceRenewalReminderDispatcher>();
```

Add `using FirearmStudio.Application.Licences.Reminders;` as needed.

- [ ] **Step 6: Route the new type in the outbox processor**

In `src/FirearmStudio.WebApi/BackgroundJobs/OutboxProcessorService.cs`:

Line ~60, next to the existing dispatcher resolution:

```csharp
var licenceReminderDispatcher = scope.ServiceProvider.GetRequiredService<ILicenceRenewalReminderDispatcher>();
```

In the switch (line ~75), add before `default`:

```csharp
case OutboxMessageTypes.LicenceRenewalReminder:
    await licenceReminderDispatcher.DispatchAsync(message.Payload, cancellationToken);
    break;
```

- [ ] **Step 7: Build and test**

Run: `dotnet build FirearmStudio.slnx && dotnet test tests/FirearmStudio.Domain.Tests`
Expected: both green.

- [ ] **Step 8: Commit**

```bash
git add src/FirearmStudio.Application/ src/FirearmStudio.WebApi/BackgroundJobs/OutboxProcessorService.cs
git commit -m "feat: dispatch licence renewal reminder outbox messages to Klaviyo"
```

---

### Task 4: Reminder generator and nightly hosted service

**Files:**
- Create: `src/FirearmStudio.Application/Licences/Reminders/ILicenceReminderGenerator.cs`
- Create: `src/FirearmStudio.Application/Licences/Reminders/LicenceReminderGenerator.cs`
- Modify: `src/FirearmStudio.Application/Extensions/DependencyInjection.cs`
- Create: `src/FirearmStudio.WebApi/BackgroundJobs/LicenceReminderService.cs`
- Modify: `src/FirearmStudio.WebApi/Program.cs` (line ~70, hosted service registration)

**Interfaces:**
- Consumes: `LicenceReminderPlanner.Plan(LicenceStatus, DateOnly, DateOnly)` (Task 1), `LicenceReminder` entity + `db.LicenceReminders` (Task 2), `OutboxMessageTypes.LicenceRenewalReminder`, `LicenceRenewalReminderPayload`, `OutboxJson.Options` (Task 3), `IApplicationDbContext`, `ITenantContext.BeginCompanyScope(Guid)`.
- Produces: `ILicenceReminderGenerator.GenerateAsync(LicenceReminderCompany company, DateOnly today, CancellationToken ct)` returning `LicenceReminderRunResult(int RemindersQueued, int StatusesUpdated, int SkippedNoEmail)`; hosted `LicenceReminderService` running daily at 03:00 UTC.

- [ ] **Step 1: Create the generator interface**

`src/FirearmStudio.Application/Licences/Reminders/ILicenceReminderGenerator.cs`:

```csharp
namespace FirearmStudio.Application.Licences.Reminders;

public sealed record LicenceReminderCompany(Guid Id, string Name);

public sealed record LicenceReminderRunResult(int RemindersQueued, int StatusesUpdated, int SkippedNoEmail);

public interface ILicenceReminderGenerator
{
    Task<LicenceReminderRunResult> GenerateAsync(
        LicenceReminderCompany company, DateOnly today, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Create the generator implementation**

`src/FirearmStudio.Application/Licences/Reminders/LicenceReminderGenerator.cs`:

```csharp
using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Licences.Reminders;

internal sealed class LicenceReminderGenerator(
    IApplicationDbContext db,
    ILogger<LicenceReminderGenerator> logger) : ILicenceReminderGenerator
{
    public async Task<LicenceReminderRunResult> GenerateAsync(
        LicenceReminderCompany company, DateOnly today, CancellationToken cancellationToken)
    {
        var windowEnd = today.AddDays(90);

        var licences = await db.FirearmLicences
            .Include(l => l.Firearm!).ThenInclude(f => f.Customer)
            .Where(l => l.ExpiresOn <= windowEnd && l.Status != LicenceStatus.Unknown)
            .ToListAsync(cancellationToken);

        if (licences.Count == 0)
        {
            return new LicenceReminderRunResult(0, 0, 0);
        }

        var licenceIds = licences.Select(l => l.Id).ToList();
        var alreadySent = (await db.LicenceReminders
                .Where(r => licenceIds.Contains(r.LicenceId))
                .Select(r => new { r.LicenceId, r.Tier })
                .ToListAsync(cancellationToken))
            .Select(x => (x.LicenceId, x.Tier))
            .ToHashSet();

        var queued = 0;
        var statusesUpdated = 0;
        var skippedNoEmail = 0;

        foreach (var licence in licences)
        {
            var plan = LicenceReminderPlanner.Plan(licence.Status, licence.ExpiresOn, today);

            if (plan.Status != licence.Status)
            {
                licence.Status = plan.Status;
                statusesUpdated++;
            }

            if (plan.Tier is not { } tier || alreadySent.Contains((licence.Id, tier)))
            {
                continue;
            }

            var customer = licence.Firearm?.Customer;
            var email = customer?.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                skippedNoEmail++;
                logger.LogInformation(
                    "Skipped licence renewal reminder for licence {LicenceId} ({Tier}): customer has no email.",
                    licence.Id, tier);
                continue;
            }

            var payload = new LicenceRenewalReminderPayload(
                email,
                customer!.CustomerType == CustomerType.Company ? customer.CompanyName : customer.FullName,
                licence.LicenceNumber,
                licence.ExpiresOn,
                licence.ExpiresOn.DayNumber - today.DayNumber,
                tier.ToString(),
                licence.Firearm!.Make,
                licence.Firearm.Model,
                licence.Firearm.SerialNumber,
                company.Id,
                company.Name);

            db.LicenceReminders.Add(new LicenceReminder
            {
                CompanyId = company.Id,
                LicenceId = licence.Id,
                Tier = tier,
            });

            db.OutboxMessages.Add(new OutboxMessage
            {
                Type = OutboxMessageTypes.LicenceRenewalReminder,
                Payload = JsonSerializer.Serialize(payload, OutboxJson.Options),
                CompanyId = company.Id,
            });

            queued++;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new LicenceReminderRunResult(queued, statusesUpdated, skippedNoEmail);
    }
}
```

Notes for the implementer:
- One `SaveChangesAsync` per tenant: status updates, dedup rows, and outbox messages commit atomically (spec requirement).
- The `(licence_id, tier)` unique index backstops the in-memory dedup under concurrent runs; a unique-violation exception here means another run won the race, which the job's per-company catch logs and the next night self-heals.
- Long-expired licences keep matching `ExpiresOn <= windowEnd`; they no-op after the Expired tier row exists. Accepted at current data volumes; do not add premature filtering.
- Verify `CustomerType.Company` is the correct enum member name in `src/FirearmStudio.Domain/Enums/CustomerType.cs` (migration labels show `company`/`individual`).

- [ ] **Step 3: Register the generator in DI**

`src/FirearmStudio.Application/Extensions/DependencyInjection.cs`:

```csharp
services.AddScoped<ILicenceReminderGenerator, LicenceReminderGenerator>();
```

- [ ] **Step 4: Create the hosted service**

`src/FirearmStudio.WebApi/BackgroundJobs/LicenceReminderService.cs` - clone the structure of `MonthlyInvoiceGenerationService.cs` exactly (03:00 instead of 02:00):

```csharp
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Licences.Reminders;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.BackgroundJobs;

public sealed class LicenceReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<LicenceReminderService> logger) : BackgroundService
{
    // Set to true once the migration check passes; never checked again afterwards.
    private bool _migrationsVerified;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // No run on startup. Compute delay to next 03:00 UTC, wait, then run.
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var today3Am = now.Date.AddHours(3);
            var next3Am = now < today3Am ? today3Am : today3Am.AddDays(1);

            try
            {
                await Task.Delay(next3Am - now, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Licence reminder run failed.");
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        List<LicenceReminderCompany> companies;
        using (var scope = scopeFactory.CreateScope())
        {
            if (!_migrationsVerified)
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pending.Count > 0)
                {
                    logger.LogError(
                        "Skipping licence reminders: {Count} pending database migration(s): {Migrations}. " +
                        "Apply migrations and the job will resume on its next tick.",
                        pending.Count, string.Join(", ", pending));
                    return;
                }

                _migrationsVerified = true;
            }

            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companies = await db.Companies
                .AsNoTracking()
                .Where(company => company.IsActive)
                .Select(company => new LicenceReminderCompany(company.Id, company.Name))
                .ToListAsync(cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var company in companies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var scope = scopeFactory.CreateScope();
                var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                var generator = scope.ServiceProvider.GetRequiredService<ILicenceReminderGenerator>();

                using (tenant.BeginCompanyScope(company.Id))
                {
                    var result = await generator.GenerateAsync(company, today, cancellationToken);

                    if ((result.RemindersQueued > 0 || result.StatusesUpdated > 0)
                        && logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            "Licence reminders for company {CompanyId}: {Queued} queued, {Statuses} status update(s), {Skipped} skipped (no email).",
                            company.Id, result.RemindersQueued, result.StatusesUpdated, result.SkippedNoEmail);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Licence reminder generation failed for company {CompanyId}.", company.Id);
            }
        }
    }
}
```

- [ ] **Step 5: Register the hosted service**

`src/FirearmStudio.WebApi/Program.cs`, after line 71 (`AddHostedService<OutboxProcessorService>()`):

```csharp
builder.Services.AddHostedService<LicenceReminderService>();
```

- [ ] **Step 6: Build and test**

Run: `dotnet build FirearmStudio.slnx && dotnet test tests/FirearmStudio.Domain.Tests`
Expected: both green.

- [ ] **Step 7: Commit**

```bash
git add src/FirearmStudio.Application/ src/FirearmStudio.WebApi/
git commit -m "feat: add nightly licence renewal reminder job"
```

---

### Task 5: End-to-end verification (verifier)

**Files:** none created; read-only plus local run.

**Interfaces:**
- Consumes: everything above.
- Produces: verification report.

- [ ] **Step 1: Static checks**

```bash
dotnet build FirearmStudio.slnx
dotnet test tests/FirearmStudio.Domain.Tests
dotnet ef migrations list -p src/FirearmStudio.Infrastructure -s src/FirearmStudio.WebApi --no-connect
```

Expected: build clean, 21+ tests pass, `AddLicenceReminders` listed as the newest migration (pending).

- [ ] **Step 2: Review the wiring end-to-end**

Confirm by reading code (not assumption):
- `Program.cs` registers `LicenceReminderService`.
- `OutboxProcessorService` routes `LicenceRenewalReminder` to the new dispatcher.
- Migration contains RLS enable + revoke SQL for `licence_reminders`.
- Payload property names produced by `LicenceReminderGenerator` deserialize into `LicenceRenewalReminderPayload` (same record, same `OutboxJson.Options`).
- Planner boundary values match the spec (90/60/30, expired strictly after `ExpiresOn`, Unknown skipped).

- [ ] **Step 3: Live-path check (safe, no Klaviyo send, no production DB)**

CRITICAL: do NOT apply migrations or run the API against the `.env` connection string if it points at a shared/production database (see `docs/code-review-2026-07-25.md` incident note). If a disposable local Postgres is not available, stop after Step 2 and report that the live path was not exercised; do not improvise against production.

If a local disposable database IS available (e.g. local Supabase or `docker run postgres`):
1. Point a copy of the connection string at it, apply migrations there.
2. Seed one company, customer (with email), firearm, and licence with `ExpiresOn` 20 days out.
3. Temporarily invoke the generator (e.g. via a scratch integration entry point or lowering the job schedule) and confirm: licence status becomes `renewal_due`, one `licence_reminders` row (`days30`), one pending `outbox_messages` row with the expected JSON payload.
4. Klaviyo dispatch may be observed only if a sandbox `KlaviyoSettings__ApiKey` is configured; otherwise verify the outbox row and stop.

- [ ] **Step 4: Report**

Report pass/fail per item above, with command output quoted, plus any deviations from the plan.
