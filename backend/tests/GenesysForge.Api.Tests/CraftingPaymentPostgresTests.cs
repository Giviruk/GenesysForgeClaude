using System.Text.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Application.Features.Characters;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GenesysForge.Api.Tests;

[CollectionDefinition("PostgreSQL migrations", DisableParallelization = true)]
public class PostgresMigrationCollection;

[Collection("PostgreSQL migrations")]
public class CraftingPaymentPostgresTests
{
    [PostgresMigrationFact]
    public async Task PaidCost_MigratesLegacyRows_AndProtectsRealWalletTransactions()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GENESYS_MIGRATION_TEST_CONNECTION"));
        var database = $"gf_crafting_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            connection.Database = database;
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString, pg => pg.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).Options;
            await using var db = new AppDbContext(options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261014100143_NormalizeNpcStrain");
            var owner = new User { Id = Guid.NewGuid(), Email = "crafting@test.local", DisplayName = "Regression", PasswordHash = "test-only" };
            var archetype = new ArchetypeDef { Id = Guid.NewGuid(), Name = "Regression", System = GameSystem.RealmsOfTerrinoth };
            var career = new CareerDef { Id = Guid.NewGuid(), Name = "Regression", System = GameSystem.RealmsOfTerrinoth };
            var target = new ItemDef { Id = Guid.NewGuid(), Name = "Regression item", System = GameSystem.RealmsOfTerrinoth, Kind = ItemKind.Weapon, Price = 150, Rarity = 2 };
            var c = new Character { Id = Guid.NewGuid(), OwnerUserId = owner.Id, Name = "Regression", System = GameSystem.RealmsOfTerrinoth, ArchetypeId = archetype.Id, CareerId = career.Id, Money = 100 };
            db.AddRange(owner, archetype, career, target, c);
            await db.SaveChangesAsync();
            // Raw inserts deliberately use the old schema, before PaidCost exists.
            var legacy = Guid.NewGuid();
            var auditPaid = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "CraftingProjects" ("Id", "CharacterId", "ItemDefId", "Kind", "Status", "TargetName", "SkillName", "BaseDifficulty", "Difficulty", "DifficultyReason", "BaseTime", "Time", "TimeReason", "ListedCost", "CostPercent", "CostOverrideReason", "Cost", "Requirements", "Intent", "RoughSurvival", "NetSuccesses", "Advantages", "Threats", "Triumphs", "Despairs", "Outcome", "CreatedAt", "Craftsmanship", "ImplementMaterial")
                SELECT id, {c.Id}, {target.Id}, 0, 0, 'Legacy', 'Mechanics', 1, 1, '', 1, 1, '', 75, 100, '', 75, '', '', false, 0, 0, 0, 0, 0, '', now(), 2, 0
                FROM unnest(ARRAY[{legacy}, {auditPaid}]) AS id;
                """);
            // Whitespace differs from the old serializer; malformed history must not break upgrade.
            db.CharacterAuditEntries.AddRange(
                new CharacterAuditEntry { Id = Guid.NewGuid(), CharacterId = c.Id, UserId = owner.Id, Action = CharacterAuditAction.CraftingStarted, DataJson = $"{{ \"projectId\" : \"{auditPaid}\", \"chargedCost\" : 75 }}" },
                new CharacterAuditEntry { Id = Guid.NewGuid(), CharacterId = c.Id, UserId = owner.Id, Action = CharacterAuditAction.CraftingStarted, DataJson = "legacy non-JSON" });
            await db.SaveChangesAsync();
            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();
            Assert.Null((await db.CraftingProjects.SingleAsync(p => p.Id == legacy)).PaidCost);
            Assert.Equal(75, (await db.CraftingProjects.SingleAsync(p => p.Id == auditPaid)).PaidCost);

            // Force both requests to finish loading the same balance before either saves.
            var input = new CraftingProjectInput(target.Id, CostOverride: 75, CostOverrideReason: "agreed");
            var gate = new SaveGate();
            await using var first = new GatedContext(options, gate);
            await using var second = new GatedContext(options, gate);
            var starts = await Task.WhenAll(
                Attempt(() => new StartCraftingHandler(first).Handle(new(owner.Id, c.Id, input))),
                Attempt(() => new StartCraftingHandler(second).Handle(new(owner.Id, c.Id, input))));
            Assert.Single(starts, error => error is null);
            Assert.Single(starts, error => error is ConflictException);
            db.ChangeTracker.Clear();
            Assert.Equal(25, (await db.Characters.SingleAsync()).Money);
            var paid = await db.CraftingProjects.SingleAsync(p => p.Id != legacy && p.Id != auditPaid);
            Assert.Equal(75, paid.PaidCost);
            Assert.Equal(3, await db.CraftingProjects.CountAsync()); // failed transaction inserted nothing

            gate = new SaveGate();
            await using var cancel1 = new GatedContext(options, gate);
            await using var cancel2 = new GatedContext(options, gate);
            var cancellations = await Task.WhenAll(
                Attempt(() => new CancelCraftingHandler(cancel1).Handle(new(owner.Id, c.Id, paid.Id))),
                Attempt(() => new CancelCraftingHandler(cancel2).Handle(new(owner.Id, c.Id, paid.Id))));
            Assert.Single(cancellations, error => error is null);
            Assert.Single(cancellations, error => error is ConflictException);
            db.ChangeTracker.Clear();
            Assert.Equal(100, (await db.Characters.SingleAsync()).Money);
            Assert.Single(await db.CharacterAuditEntries.Where(a => a.Action == CharacterAuditAction.CraftingCancelled).ToListAsync());

            // Even a free project (no Money UPDATE) cannot be cancelled and resolved together.
            var free = await new StartCraftingHandler(db).Handle(new(owner.Id, c.Id, input with { CostOverride = 0 }));
            var auditCount = await db.CharacterAuditEntries.CountAsync();
            gate = new SaveGate();
            await using var cancelFree = new GatedContext(options, gate);
            await using var resolveFree = new GatedContext(options, gate);
            var outcomes = await Task.WhenAll(
                Attempt(() => new CancelCraftingHandler(cancelFree).Handle(new(owner.Id, c.Id, free))),
                Attempt(() => new ResolveCraftingHandler(resolveFree).Handle(new(owner.Id, c.Id, free, new CraftingResolveInput(0)))));
            Assert.Single(outcomes, error => error is null);
            Assert.Single(outcomes, error => error is ConflictException);
            db.ChangeTracker.Clear();
            Assert.Equal(100, (await db.Characters.SingleAsync()).Money);
            Assert.Equal(auditCount + 1, await db.CharacterAuditEntries.CountAsync());
            Assert.NotEqual(CraftingProjectStatus.Draft, (await db.CraftingProjects.SingleAsync(p => p.Id == free)).Status);

            // The varchar audit limit is enforced by PostgreSQL, unlike InMemory.
            var longInput = input with { Requirements = new string('я', 2000), Intent = new string('ж', 2000), CostOverrideReason = new string('ю', 200) };
            var longProject = await new StartCraftingHandler(db).Handle(new(owner.Id, c.Id, longInput));
            var audit = await db.CharacterAuditEntries.SingleAsync(a => a.Action == CharacterAuditAction.CraftingStarted && a.DataJson.Contains(longProject.ToString()));
            Assert.True(audit.DataJson.Length < 2000);
            using var data = JsonDocument.Parse(audit.DataJson);
            Assert.Equal(75, data.RootElement.GetProperty("chargedCost").GetInt32());
            Assert.Equal(longInput.Requirements, (await db.CraftingProjects.SingleAsync(p => p.Id == longProject)).Requirements);
            await new CancelCraftingHandler(db).Handle(new(owner.Id, c.Id, longProject));
            Assert.Equal(100, (await db.Characters.SingleAsync()).Money);
            var projectCount = await db.CraftingProjects.CountAsync();
            await migrator.MigrateAsync("20261014100143_NormalizeNpcStrain");
            // Down only removes payment metadata; it preserves all project rows.
            await using (var verify = new NpgsqlConnection(connection.ConnectionString))
            {
                await verify.OpenAsync();
                await using var count = new NpgsqlCommand("SELECT count(*) FROM \"CraftingProjects\"", verify);
                Assert.Equal((long)projectCount, await count.ExecuteScalarAsync());
            }
            await migrator.MigrateAsync();
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task<Exception?> Attempt<T>(Func<Task<T>> run)
    {
        try { await run(); return null; }
        catch (Exception error) { return error; }
    }

    private sealed class SaveGate
    {
        private int arrivals;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task Meet()
        {
            if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
    }
    private sealed class GatedContext(DbContextOptions<AppDbContext> options, SaveGate gate) : AppDbContext(options)
    {
        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            await gate.Meet();
            return await base.SaveChangesAsync(ct);
        }
    }
}
