using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotItemAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormTraits",
                table: "ItemDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AttachmentDefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    System = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameRu = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    HardPointCost = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: true),
                    Rarity = table.Column<int>(type: "integer", nullable: false),
                    IsEnchantment = table.Column<bool>(type: "boolean", nullable: false),
                    HostKind = table.Column<int>(type: "integer", nullable: false),
                    RequiredTraits = table.Column<int>(type: "integer", nullable: false),
                    RequiredAnyTraits = table.Column<int>(type: "integer", nullable: false),
                    ForbiddenTraits = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SafeDescription = table.Column<string>(type: "text", nullable: false),
                    DescriptionEn = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    HomebrewPackId = table.Column<Guid>(type: "uuid", nullable: true),
                    Retired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentDefs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttachmentEffects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttachmentDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    QualityCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    OppositeQualityCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    SkillName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Increment = table.Column<int>(type: "integer", nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachmentEffects_AttachmentDefs_AttachmentDefId",
                        column: x => x.AttachmentDefId,
                        principalTable: "AttachmentDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttachmentDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostCharacterItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provenance = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAttachments_AttachmentDefs_AttachmentDefId",
                        column: x => x.AttachmentDefId,
                        principalTable: "AttachmentDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterAttachments_CharacterItems_HostCharacterItemId",
                        column: x => x.HostCharacterItemId,
                        principalTable: "CharacterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CharacterAttachments_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentDefs_System_Code",
                table: "AttachmentDefs",
                columns: new[] { "System", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentEffects_AttachmentDefId",
                table: "AttachmentEffects",
                column: "AttachmentDefId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAttachments_AttachmentDefId",
                table: "CharacterAttachments",
                column: "AttachmentDefId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAttachments_CharacterId",
                table: "CharacterAttachments",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAttachments_HostCharacterItemId",
                table: "CharacterAttachments",
                column: "HostCharacterItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttachmentEffects");

            migrationBuilder.DropTable(
                name: "CharacterAttachments");

            migrationBuilder.DropTable(
                name: "AttachmentDefs");

            migrationBuilder.DropColumn(
                name: "FormTraits",
                table: "ItemDefs");
        }
    }
}
