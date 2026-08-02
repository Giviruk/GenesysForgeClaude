using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotHa02SignatureBaseAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BaseAttachmentDefId",
                table: "CharacterSignatureWeapons",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSignatureWeapons_BaseAttachmentDefId",
                table: "CharacterSignatureWeapons",
                column: "BaseAttachmentDefId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterSignatureWeapons_AttachmentDefs_BaseAttachmentDefId",
                table: "CharacterSignatureWeapons",
                column: "BaseAttachmentDefId",
                principalTable: "AttachmentDefs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterSignatureWeapons_AttachmentDefs_BaseAttachmentDefId",
                table: "CharacterSignatureWeapons");

            migrationBuilder.DropIndex(
                name: "IX_CharacterSignatureWeapons_BaseAttachmentDefId",
                table: "CharacterSignatureWeapons");

            migrationBuilder.DropColumn(
                name: "BaseAttachmentDefId",
                table: "CharacterSignatureWeapons");
        }
    }
}
