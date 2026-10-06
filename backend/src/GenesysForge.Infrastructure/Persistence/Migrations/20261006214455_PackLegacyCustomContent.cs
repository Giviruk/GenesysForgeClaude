using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations;

/// <summary>Assign legacy owner custom definitions to personal packs without changing definition IDs.</summary>
public partial class PackLegacyCustomContent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH owners AS (
            SELECT "OwnerUserId", "System" FROM "SkillDefs" WHERE "OwnerUserId" IS NOT NULL AND "HomebrewPackId" IS NULL
            UNION
            SELECT "OwnerUserId", "System" FROM "TalentDefs" WHERE "OwnerUserId" IS NOT NULL AND "HomebrewPackId" IS NULL
            UNION
            SELECT "OwnerUserId", "System" FROM "ItemDefs" WHERE "OwnerUserId" IS NOT NULL AND "HomebrewPackId" IS NULL
            UNION
            SELECT "OwnerUserId", "System" FROM "ArchetypeDefs" WHERE "OwnerUserId" IS NOT NULL AND "HomebrewPackId" IS NULL
            UNION
            SELECT "OwnerUserId", "System" FROM "CareerDefs" WHERE "OwnerUserId" IS NOT NULL AND "HomebrewPackId" IS NULL
            UNION
            SELECT "OwnerUserId", 1 AS "System" FROM "HeroicAbilityDefs" WHERE "OwnerUserId" IS NOT NULL AND "HomebrewPackId" IS NULL
            )
            INSERT INTO "HomebrewPacks" ("Id", "OwnerUserId", "Name", "Description", "System", "IsShared", "IsEnabledByDefault", "CreatedAt", "UpdatedAt")
            SELECT gen_random_uuid(), o."OwnerUserId",
                   'Моя библиотека (' || CASE o."System" WHEN 0 THEN 'GenesysCore' ELSE 'RealmsOfTerrinoth' END || ')',
                   'Personal custom:' || CASE o."System" WHEN 0 THEN 'GenesysCore' ELSE 'RealmsOfTerrinoth' END,
                   o."System", FALSE, TRUE, NOW(), NOW()
            FROM owners o
            WHERE NOT EXISTS (
                SELECT 1 FROM "HomebrewPacks" p
                WHERE p."OwnerUserId" = o."OwnerUserId" AND p."System" = o."System"
                  AND p."Description" = 'Personal custom:' || CASE o."System" WHEN 0 THEN 'GenesysCore' ELSE 'RealmsOfTerrinoth' END
            );

            UPDATE "SkillDefs" d
            SET "HomebrewPackId" = (
                SELECT p."Id" FROM "HomebrewPacks" p
                WHERE p."OwnerUserId" = d."OwnerUserId" AND p."System" = d."System"
                  AND p."Description" = 'Personal custom:' || CASE d."System" WHEN 0 THEN 'GenesysCore' ELSE 'RealmsOfTerrinoth' END
                ORDER BY p."CreatedAt", p."Id" LIMIT 1
            )
            WHERE d."OwnerUserId" IS NOT NULL AND d."HomebrewPackId" IS NULL;

            UPDATE "TalentDefs" d
            SET "HomebrewPackId" = (
                SELECT p."Id" FROM "HomebrewPacks" p
                WHERE p."OwnerUserId" = d."OwnerUserId" AND p."System" = d."System"
                  AND p."Description" = 'Personal custom:' || CASE d."System" WHEN 0 THEN 'GenesysCore' ELSE 'RealmsOfTerrinoth' END
                ORDER BY p."CreatedAt", p."Id" LIMIT 1
            )
            WHERE d."OwnerUserId" IS NOT NULL AND d."HomebrewPackId" IS NULL;

            UPDATE "ItemDefs" d
            SET "HomebrewPackId" = (
                SELECT p."Id" FROM "HomebrewPacks" p
                WHERE p."OwnerUserId" = d."OwnerUserId" AND p."System" = d."System"
                  AND p."Description" = 'Personal custom:' || CASE d."System" WHEN 0 THEN 'GenesysCore' ELSE 'RealmsOfTerrinoth' END
                ORDER BY p."CreatedAt", p."Id" LIMIT 1
            )
            WHERE d."OwnerUserId" IS NOT NULL AND d."HomebrewPackId" IS NULL;

            UPDATE "ArchetypeDefs" d
            SET "HomebrewPackId" = (
                SELECT p."Id" FROM "HomebrewPacks" p
                WHERE p."OwnerUserId" = d."OwnerUserId" AND p."System" = d."System"
                  AND p."Description" = 'Personal custom:' || CASE d."System" WHEN 0 THEN 'GenesysCore' ELSE 'RealmsOfTerrinoth' END
                ORDER BY p."CreatedAt", p."Id" LIMIT 1
            )
            WHERE d."OwnerUserId" IS NOT NULL AND d."HomebrewPackId" IS NULL;

            UPDATE "CareerDefs" d
            SET "HomebrewPackId" = (
                SELECT p."Id" FROM "HomebrewPacks" p
                WHERE p."OwnerUserId" = d."OwnerUserId" AND p."System" = d."System"
                  AND p."Description" = 'Personal custom:' || CASE d."System" WHEN 0 THEN 'GenesysCore' ELSE 'RealmsOfTerrinoth' END
                ORDER BY p."CreatedAt", p."Id" LIMIT 1
            )
            WHERE d."OwnerUserId" IS NOT NULL AND d."HomebrewPackId" IS NULL;

            UPDATE "HeroicAbilityDefs" d
            SET "HomebrewPackId" = (
                SELECT p."Id" FROM "HomebrewPacks" p
                WHERE p."OwnerUserId" = d."OwnerUserId" AND p."System" = 1
                  AND p."Description" = 'Personal custom:' || CASE 1 WHEN 0 THEN 'GenesysCore' ELSE 'RealmsOfTerrinoth' END
                ORDER BY p."CreatedAt", p."Id" LIMIT 1
            )
            WHERE d."OwnerUserId" IS NOT NULL AND d."HomebrewPackId" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve the repaired ownership links: detaching them could expose custom content in
        // campaigns and cannot distinguish legacy rows from content subsequently added to the pack.
    }
}
