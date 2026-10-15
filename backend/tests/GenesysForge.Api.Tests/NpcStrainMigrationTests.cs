using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GenesysForge.Api.Tests;

[Collection("PostgreSQL migrations")]
public class NpcStrainMigrationTests
{
    [PostgresMigrationFact]
    public async Task Cleanup_OnlyRemovesInvalidStrain_AndRollbackKeepsAllEntities()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GENESYS_MIGRATION_TEST_CONNECTION"));
        var database = $"gf_npc_strain_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            connection.Database = database;
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261009141914_ContentLibraryV2");
            var owner = new User { Id = Guid.NewGuid(), Email = "strain-migration@test.local", DisplayName = "Regression", PasswordHash = "test-only" };
            var campaign = new Campaign { Id = Guid.NewGuid(), GmUserId = owner.Id, Name = "Regression", JoinCode = "STRAIN" };
            var session = new GameSession { Id = Guid.NewGuid(), CampaignId = campaign.Id, Name = "Regression" };
            var npcs = new[] { NpcKind.Minion, NpcKind.Rival, NpcKind.Nemesis }.Select(kind => new Npc
            {
                Id = Guid.NewGuid(), OwnerUserId = owner.Id, Name = kind.ToString(), Kind = kind, StrainThreshold = 12,
            }).ToArray();
            var linked = npcs.Select(npc => new GameParticipant
            {
                Id = Guid.NewGuid(), SessionId = session.Id, NpcId = npc.Id, DisplayName = npc.Name,
                ParticipantType = ParticipantType.Npc, StrainThreshold = 12, StrainCurrent = 3,
            }).ToArray();
            var manual = new[] { ParticipantType.Npc, ParticipantType.MinionGroup, ParticipantType.PlayerCharacter }.Select(type => new GameParticipant
            {
                Id = Guid.NewGuid(), SessionId = session.Id, DisplayName = type.ToString(), ParticipantType = type,
                StrainThreshold = 14, StrainCurrent = 5,
            }).ToArray();
            db.AddRange(owner, campaign, session);
            db.Npcs.AddRange(npcs);
            db.GameParticipants.AddRange(linked.Concat(manual));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            await migrator.MigrateAsync();
            var savedNpcs = await db.Npcs.ToListAsync();
            Assert.All(savedNpcs.Where(n => n.Kind != NpcKind.Nemesis), n => Assert.Null(n.StrainThreshold));
            Assert.Equal(12, savedNpcs.Single(n => n.Kind == NpcKind.Nemesis).StrainThreshold);
            var participants = await db.GameParticipants.ToListAsync();
            var clearedIds = linked.Take(2).Select(p => p.Id).Append(manual[1].Id).ToHashSet();
            Assert.All(participants.Where(p => clearedIds.Contains(p.Id)), p => { Assert.Null(p.StrainThreshold); Assert.Equal(0, p.StrainCurrent); });
            Assert.All(participants.Where(p => !clearedIds.Contains(p.Id)), p => { Assert.NotNull(p.StrainThreshold); Assert.True(p.StrainCurrent > 0); });
            await migrator.MigrateAsync("20261009141914_ContentLibraryV2");
            Assert.Equal(3, await db.Npcs.CountAsync());
            Assert.Equal(6, await db.GameParticipants.CountAsync());
            Assert.All(await db.Npcs.Where(n => n.Kind != NpcKind.Nemesis).ToListAsync(), n => Assert.Null(n.StrainThreshold));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
