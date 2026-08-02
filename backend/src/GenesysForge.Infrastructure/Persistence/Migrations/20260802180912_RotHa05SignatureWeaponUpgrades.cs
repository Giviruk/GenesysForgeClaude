using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotHa05SignatureWeaponUpgrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Improvement",
                table: "CharacterSignatureWeapons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SupremeAttachmentDefId",
                table: "CharacterSignatureWeapons",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSignatureWeapons_SupremeAttachmentDefId",
                table: "CharacterSignatureWeapons",
                column: "SupremeAttachmentDefId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterSignatureWeapons_AttachmentDefs_SupremeAttachmentD~",
                table: "CharacterSignatureWeapons",
                column: "SupremeAttachmentDefId",
                principalTable: "AttachmentDefs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterSignatureWeapons_AttachmentDefs_SupremeAttachmentD~",
                table: "CharacterSignatureWeapons");

            migrationBuilder.DropIndex(
                name: "IX_CharacterSignatureWeapons_SupremeAttachmentDefId",
                table: "CharacterSignatureWeapons");

            migrationBuilder.DropColumn(
                name: "Improvement",
                table: "CharacterSignatureWeapons");

            migrationBuilder.DropColumn(
                name: "SupremeAttachmentDefId",
                table: "CharacterSignatureWeapons");
        }
    }
}
