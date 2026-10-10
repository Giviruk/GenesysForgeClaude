using System.Data.Common;
using GenesysForge.Application.Common;
using GenesysForge.Application.Features.ContentLibrary;
using GenesysForge.Application.Features.Search;
using Microsoft.EntityFrameworkCore.Diagnostics;
using GenesysForge.Application.Features.Characters;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GenesysForge.Api.Tests;

/// <summary>Run against a disposable PostgreSQL 17 instance; never uses the application's database.</summary>
public sealed class PostgresMigrationFactAttribute : FactAttribute
{
    public PostgresMigrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GENESYS_MIGRATION_TEST_CONNECTION")))
            Skip = "Set GENESYS_MIGRATION_TEST_CONNECTION to a disposable PostgreSQL instance (CREATE DATABASE permission).";
    }
}

public class ContentLibraryMigrationTests
{
    private sealed class QueryLog : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    [PostgresMigrationFact]
    public async Task LegacyUnpackedOwnedDefinitions_AreConnectedOnlyWhereAlreadyUsed()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GENESYS_MIGRATION_TEST_CONNECTION"));
        var database = $"gf_migration_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            connection.Database = database;
            var log = new QueryLog();
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).AddInterceptors(log).Options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261007090302_AddCustomContentChanges");
            var owner = new User { Id = Guid.NewGuid(), Email = "legacy@test.local", DisplayName = "Legacy", PasswordHash = "test-only" };
            var gm = new User { Id = Guid.NewGuid(), Email = "gm@test.local", DisplayName = "GM", PasswordHash = "test-only" };
            var campaign = new Campaign { Id = Guid.NewGuid(), GmUserId = gm.Id, Name = "Legacy campaign", JoinCode = "LEGACY" };
            var otherCampaign = new Campaign { Id = Guid.NewGuid(), GmUserId = gm.Id, Name = "Unused campaign", JoinCode = "UNUSED" };
            var skill = new SkillDef { Id = Guid.NewGuid(), Name = "Legacy skill", OwnerUserId = owner.Id };
            var unused = new SkillDef { Id = Guid.NewGuid(), Name = "Unused own skill", OwnerUserId = owner.Id };
            var zeroRank = new SkillDef { Id = Guid.NewGuid(), Name = "Zero rank", OwnerUserId = owner.Id };
            var foreign = new SkillDef { Id = Guid.NewGuid(), Name = "Foreign used skill", OwnerUserId = gm.Id };
            var talent = new TalentDef { Id = Guid.NewGuid(), Name = "Legacy talent", OwnerUserId = owner.Id, Tier = 1, IsRanked = true };
            var item = new ItemDef { Id = Guid.NewGuid(), Name = "Legacy item", OwnerUserId = owner.Id };
            var archetype = new ArchetypeDef { Id = Guid.NewGuid(), Name = "Legacy archetype", OwnerUserId = owner.Id };
            var career = new CareerDef { Id = Guid.NewGuid(), Name = "Legacy career", OwnerUserId = owner.Id };
            var heroic = new HeroicAbilityDef { Id = Guid.NewGuid(), Name = "Legacy heroic", OwnerUserId = owner.Id };
            var attachment = new AttachmentDef { Id = Guid.NewGuid(), Name = "Legacy attachment", OwnerUserId = owner.Id };
            var mount = new MountDef { Id = Guid.NewGuid(), Name = "Legacy mount", OwnerUserId = owner.Id };
            var character = new Character
            {
                Id = Guid.NewGuid(), OwnerUserId = owner.Id, Name = "Legacy character", ArchetypeId = archetype.Id,
                CareerId = career.Id, HeroicAbilityId = heroic.Id, TotalXp = 100, Brawn = 2, Agility = 2,
                Skills = [new() { Id = Guid.NewGuid(), SkillDefId = skill.Id, Ranks = 1 },
                    new() { Id = Guid.NewGuid(), SkillDefId = foreign.Id, Ranks = 1 },
                    new() { Id = Guid.NewGuid(), SkillDefId = zeroRank.Id, Ranks = 0 }],
                Talents = [new() { Id = Guid.NewGuid(), TalentDefId = talent.Id }],
                Items = [new() { Id = Guid.NewGuid(), ItemDefId = item.Id }],
                Attachments = [new() { Id = Guid.NewGuid(), AttachmentDefId = attachment.Id }],
                Mounts = [new() { Id = Guid.NewGuid(), MountDefId = mount.Id }],
            };
            var searchSkills = Enumerable.Range(0, 13).Select(i => new SkillDef
            {
                Id = Guid.NewGuid(), Name = "Limit regression", NameRu = $"Limit regression {i:00}", Code = $"test.limit.{i:00}", Retired = i == 12,
            }).ToList();
            db.SkillDefs.AddRange(searchSkills);
            db.AddRange(owner, gm, campaign, otherCampaign, skill, unused, zeroRank, foreign, talent, item, archetype, career, heroic, attachment, mount, character);
            db.CampaignCharacters.Add(new() { CampaignId = campaign.Id, CharacterId = character.Id });
            await db.SaveChangesAsync();
            Assert.Null((await db.SkillDefs.SingleAsync(x => x.Id == skill.Id)).HomebrewPackId);
            db.ChangeTracker.Clear();
            await migrator.MigrateAsync();
            var connections = await db.CampaignContentItems.ToListAsync();
            Assert.Equal(8, connections.Count);
            var expected = new[] { skill.Id, talent.Id, item.Id, archetype.Id, career.Id, heroic.Id, attachment.Id, mount.Id };
            Assert.Equal(expected.Order(), connections.Select(x => x.EntryId).Order());
            Assert.All(connections, x => { Assert.Equal(campaign.Id, x.CampaignId); Assert.True(x.IsEnabled); Assert.Equal(ContentConnectionStatus.Active, x.Status); });
            Assert.Empty(await db.HomebrewPackEntries.ToListAsync());
            var policy = await CampaignContentPolicy.LoadAsync(db, owner.Id, GameSystem.GenesysCore, character.Id);
            Assert.All(expected, id => Assert.Contains(id, policy.CustomIds));
            Assert.DoesNotContain(unused.Id, policy.CustomIds);
            Assert.DoesNotContain(zeroRank.Id, policy.CustomIds);
            Assert.DoesNotContain(foreign.Id, policy.CustomIds);
            // Verify SQL search filtering precedes the per-source limit, without full catalogue loads.
            db.CampaignBaseOverrides.AddRange(searchSkills.Take(11).Select(x => new CampaignBaseOverride
            {
                Id = Guid.NewGuid(), CampaignId = campaign.Id, Category = BaseContentCategory.Skill, ContentKey = x.Code, IsEnabled = false,
            }));
            await db.SaveChangesAsync();
            log.Commands.Clear();
            var hits = await new GlobalSearchHandler(db).Handle(new(gm.Id, GameSystem.GenesysCore, "limit regression", CampaignId: campaign.Id));
            Assert.Equal(1, hits.Hits.Count(x => x.Type == "skill"));
            Assert.Single(log.Commands, x => x.Contains("FROM \"SkillDefs\""));
            Assert.DoesNotContain(log.Commands, x => x.Contains("FROM \"AttachmentDefs\"") || x.Contains("FROM \"MountDefs\""));
            log.Commands.Clear();
            await new GetCampaignContentHandler(db).Handle(new(gm.Id, campaign.Id));
            // One built-in snapshot and one direct-item metadata batch. Empty pack entries issue no SQL.
            Assert.Equal(2, log.Commands.Count(x => x.Contains("FROM \"SkillDefs\"")));
            Assert.Single(log.Commands, x => x.Contains("JOIN \"CharacterSkills\""));
            log.Commands.Clear();
            await new SetCampaignBaseHandler(db).Handle(new(gm.Id, campaign.Id,
                new(GameSystem.GenesysCore, [new(BaseContentCategory.Skill, searchSkills[0].Code, true)])));
            Assert.DoesNotContain(log.Commands, x => x.Contains("\"CharacterSkills\"") || x.Contains("\"Characters\""));
            // Exercise the migrated connection through the actual purchase handler.
            await new BuySkillRankHandler(db).Handle(new(owner.Id, character.Id, skill.Id));
            Assert.Equal(2, (await db.CharacterSkills.SingleAsync(x => x.CharacterId == character.Id && x.SkillDefId == skill.Id)).Ranks);
            // Rollback retains definitions and character references.
            await migrator.MigrateAsync("20261007090302_AddCustomContentChanges");
            Assert.Equal(2, (await db.CharacterSkills.SingleAsync(x => x.CharacterId == character.Id && x.SkillDefId == skill.Id)).Ranks);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
