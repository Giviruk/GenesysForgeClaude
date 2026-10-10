using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContentLibraryV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProposedByUserId",
                table: "HomebrewPackCampaigns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "HomebrewPackCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatePolicy",
                table: "HomebrewPackCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CampaignBaseOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    System = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    ContentKey = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignBaseOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignBaseOverrides_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryType = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProposedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignContentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignContentItems_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignPackEntryStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HomebrewPackCampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryType = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignPackEntryStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignPackEntryStates_HomebrewPackCampaigns_HomebrewPackC~",
                        column: x => x.HomebrewPackCampaignId,
                        principalTable: "HomebrewPackCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignSystemSettings",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    System = table.Column<int>(type: "integer", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignSystemSettings", x => new { x.CampaignId, x.System });
                    table.ForeignKey(
                        name: "FK_CampaignSystemSettings_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomebrewPackEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HomebrewPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryType = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomebrewPackEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomebrewPackEntries_HomebrewPacks_HomebrewPackId",
                        column: x => x.HomebrewPackId,
                        principalTable: "HomebrewPacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomebrewPackExclusions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HomebrewPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    ContentKey = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomebrewPackExclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomebrewPackExclusions_HomebrewPacks_HomebrewPackId",
                        column: x => x.HomebrewPackId,
                        principalTable: "HomebrewPacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignBaseOverrides_CampaignId_System_Category_ContentKey",
                table: "CampaignBaseOverrides",
                columns: new[] { "CampaignId", "System", "Category", "ContentKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContentItems_CampaignId_EntryType_EntryId",
                table: "CampaignContentItems",
                columns: new[] { "CampaignId", "EntryType", "EntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignPackEntryStates_HomebrewPackCampaignId_EntryType_En~",
                table: "CampaignPackEntryStates",
                columns: new[] { "HomebrewPackCampaignId", "EntryType", "EntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomebrewPackEntries_EntryType_EntryId",
                table: "HomebrewPackEntries",
                columns: new[] { "EntryType", "EntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_HomebrewPackEntries_HomebrewPackId_EntryType_EntryId",
                table: "HomebrewPackEntries",
                columns: new[] { "HomebrewPackId", "EntryType", "EntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomebrewPackExclusions_HomebrewPackId_Category_ContentKey",
                table: "HomebrewPackExclusions",
                columns: new[] { "HomebrewPackId", "Category", "ContentKey" },
                unique: true);

            // Keep stable definition IDs. Named packs become M:N memberships; personal auto-packs dissolve.
            var tables = new[] { "SkillDefs", "TalentDefs", "ItemDefs", "ArchetypeDefs", "CareerDefs", "HeroicAbilityDefs", "AttachmentDefs", "MountDefs" };
            // Before dissolving auto-packs, explicitly preserve only originally unpacked custom
            // definitions already used by their owner's campaign characters. No general personal bypass.
            var uses = new[]
            {
                "JOIN \"CharacterSkills\" u ON u.\"SkillDefId\" = d.\"Id\" AND u.\"Ranks\" > 0 JOIN \"Characters\" c ON c.\"Id\" = u.\"CharacterId\"",
                "JOIN \"CharacterTalents\" u ON u.\"TalentDefId\" = d.\"Id\" JOIN \"Characters\" c ON c.\"Id\" = u.\"CharacterId\"",
                "JOIN \"CharacterItems\" u ON u.\"ItemDefId\" = d.\"Id\" JOIN \"Characters\" c ON c.\"Id\" = u.\"CharacterId\"",
                "JOIN \"Characters\" c ON c.\"ArchetypeId\" = d.\"Id\"",
                "JOIN \"Characters\" c ON c.\"CareerId\" = d.\"Id\"",
                "JOIN \"Characters\" c ON c.\"HeroicAbilityId\" = d.\"Id\"",
                "JOIN \"CharacterAttachments\" u ON u.\"AttachmentDefId\" = d.\"Id\" JOIN \"Characters\" c ON c.\"Id\" = u.\"CharacterId\"",
                "JOIN \"CharacterMounts\" u ON u.\"MountDefId\" = d.\"Id\" JOIN \"Characters\" c ON c.\"Id\" = u.\"CharacterId\"",
            };
            for (var type = 0; type < tables.Length; type++)
                migrationBuilder.Sql($"""
                    INSERT INTO "CampaignContentItems" ("Id", "CampaignId", "EntryType", "EntryId", "IsEnabled", "Status", "AddedAt")
                    SELECT gen_random_uuid(), used."CampaignId", {type}, used."Id", true, 0, now()
                    FROM (
                        SELECT DISTINCT cc."CampaignId", d."Id"
                        FROM "{tables[type]}" d {uses[type]}
                        JOIN "CampaignCharacters" cc ON cc."CharacterId" = c."Id"
                        WHERE d."HomebrewPackId" IS NULL AND d."OwnerUserId" IS NOT NULL AND d."OwnerUserId" = c."OwnerUserId"
                    ) used
                    ON CONFLICT ("CampaignId", "EntryType", "EntryId") DO NOTHING;
                    """);
            for (var type = 0; type < tables.Length; type++)
            {
                migrationBuilder.Sql($"""
                    INSERT INTO "HomebrewPackEntries" ("Id", "HomebrewPackId", "EntryType", "EntryId", "AddedAt")
                    SELECT gen_random_uuid(), p."Id", {type}, d."Id", p."CreatedAt"
                    FROM "{tables[type]}" d JOIN "HomebrewPacks" p ON d."HomebrewPackId" = p."Id"
                    WHERE d."OwnerUserId" IS NOT NULL AND p."Description" NOT LIKE 'Personal custom:%';

                    -- A previously connected personal auto-pack must not silently disappear from a campaign.
                    INSERT INTO "CampaignContentItems" ("Id", "CampaignId", "EntryType", "EntryId", "IsEnabled", "Status", "AddedAt")
                    SELECT gen_random_uuid(), l."CampaignId", {type}, d."Id", l."IsEnabled", 0, l."UpdatedAt"
                    FROM "{tables[type]}" d JOIN "HomebrewPacks" p ON d."HomebrewPackId" = p."Id"
                    JOIN "HomebrewPackCampaigns" l ON l."HomebrewPackId" = p."Id"
                    WHERE d."OwnerUserId" IS NOT NULL AND p."Description" LIKE 'Personal custom:%'
                    ON CONFLICT ("CampaignId", "EntryType", "EntryId") DO NOTHING;

                    UPDATE "{tables[type]}" SET "HomebrewPackId" = NULL
                    WHERE "HomebrewPackId" IN (SELECT "Id" FROM "HomebrewPacks" WHERE "Description" LIKE 'Personal custom:%');
                    """);
            }
            migrationBuilder.Sql("""
                UPDATE "HomebrewPacks" SET "Description" = '' WHERE "Description" LIKE 'Campaign custom:%';
                DELETE FROM "HomebrewPackCharacters" WHERE "HomebrewPackId" IN
                    (SELECT "Id" FROM "HomebrewPacks" WHERE "Description" LIKE 'Personal custom:%');
                DELETE FROM "HomebrewPacks" WHERE "Description" LIKE 'Personal custom:%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var tables = new[] { "SkillDefs", "TalentDefs", "ItemDefs", "ArchetypeDefs", "CareerDefs", "HeroicAbilityDefs", "AttachmentDefs", "MountDefs" };
            for (var type = 0; type < tables.Length; type++)
                migrationBuilder.Sql($"""
                    UPDATE "{tables[type]}" d SET "HomebrewPackId" = (
                        SELECT e."HomebrewPackId" FROM "HomebrewPackEntries" e
                        WHERE e."EntryType" = {type} AND e."EntryId" = d."Id"
                        ORDER BY e."AddedAt", e."Id" LIMIT 1)
                    WHERE d."OwnerUserId" IS NOT NULL;
                    """);

            migrationBuilder.DropTable(
                name: "CampaignBaseOverrides");

            migrationBuilder.DropTable(
                name: "CampaignContentItems");

            migrationBuilder.DropTable(
                name: "CampaignPackEntryStates");

            migrationBuilder.DropTable(
                name: "CampaignSystemSettings");

            migrationBuilder.DropTable(
                name: "HomebrewPackEntries");

            migrationBuilder.DropTable(
                name: "HomebrewPackExclusions");

            migrationBuilder.DropColumn(
                name: "ProposedByUserId",
                table: "HomebrewPackCampaigns");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "HomebrewPackCampaigns");

            migrationBuilder.DropColumn(
                name: "UpdatePolicy",
                table: "HomebrewPackCampaigns");
        }
    }
}
