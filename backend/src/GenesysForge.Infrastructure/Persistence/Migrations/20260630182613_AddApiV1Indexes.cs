using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApiV1Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TalentDefs_System_OwnerUserId",
                table: "TalentDefs",
                columns: new[] { "System", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SpellDefs_System_OwnerUserId",
                table: "SpellDefs",
                columns: new[] { "System", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefs_System_OwnerUserId",
                table: "SkillDefs",
                columns: new[] { "System", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt_RevokedAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId_ExpiresAt_UsedAt",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "ExpiresAt", "UsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_System_Kind_Role",
                table: "Npcs",
                columns: new[] { "System", "Kind", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_System_OwnerUserId",
                table: "Npcs",
                columns: new[] { "System", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_System_Visibility",
                table: "Npcs",
                columns: new[] { "System", "Visibility" });

            migrationBuilder.Sql("""
                CREATE INDEX "IX_Npcs_Tags_Gin"
                ON "Npcs" USING GIN ("Tags");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ItemDefs_System_OwnerUserId",
                table: "ItemDefs",
                columns: new[] { "System", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_HeroicAbilityDefs_OwnerUserId",
                table: "HeroicAbilityDefs",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterShareTokens_CharacterId_CreatedAt",
                table: "CharacterShareTokens",
                columns: new[] { "CharacterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CareerDefs_System_OwnerUserId",
                table: "CareerDefs",
                columns: new[] { "System", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeDefs_System_OwnerUserId",
                table: "ArchetypeDefs",
                columns: new[] { "System", "OwnerUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TalentDefs_System_OwnerUserId",
                table: "TalentDefs");

            migrationBuilder.DropIndex(
                name: "IX_SpellDefs_System_OwnerUserId",
                table: "SpellDefs");

            migrationBuilder.DropIndex(
                name: "IX_SkillDefs_System_OwnerUserId",
                table: "SkillDefs");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt_RevokedAt",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_UserId_ExpiresAt_UsedAt",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_Npcs_System_Kind_Role",
                table: "Npcs");

            migrationBuilder.DropIndex(
                name: "IX_Npcs_System_OwnerUserId",
                table: "Npcs");

            migrationBuilder.DropIndex(
                name: "IX_Npcs_System_Visibility",
                table: "Npcs");

            migrationBuilder.Sql("""
                DROP INDEX "IX_Npcs_Tags_Gin";
                """);

            migrationBuilder.DropIndex(
                name: "IX_ItemDefs_System_OwnerUserId",
                table: "ItemDefs");

            migrationBuilder.DropIndex(
                name: "IX_HeroicAbilityDefs_OwnerUserId",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropIndex(
                name: "IX_CharacterShareTokens_CharacterId_CreatedAt",
                table: "CharacterShareTokens");

            migrationBuilder.DropIndex(
                name: "IX_CareerDefs_System_OwnerUserId",
                table: "CareerDefs");

            migrationBuilder.DropIndex(
                name: "IX_ArchetypeDefs_System_OwnerUserId",
                table: "ArchetypeDefs");
        }
    }
}
