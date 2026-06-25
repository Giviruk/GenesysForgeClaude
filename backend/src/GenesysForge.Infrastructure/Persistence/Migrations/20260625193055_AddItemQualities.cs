using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemQualities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityDefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    HasRating = table.Column<bool>(type: "boolean", nullable: false),
                    ActivationCost = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SafeDescription = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Source = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityDefs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemQualityValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemQualityValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemQualityValues_ItemDefs_ItemDefId",
                        column: x => x.ItemDefId,
                        principalTable: "ItemDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemQualityValues_QualityDefs_QualityDefId",
                        column: x => x.QualityDefId,
                        principalTable: "QualityDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemQualityValues_ItemDefId",
                table: "ItemQualityValues",
                column: "ItemDefId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemQualityValues_QualityDefId",
                table: "ItemQualityValues",
                column: "QualityDefId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityDefs_Code",
                table: "QualityDefs",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemQualityValues");

            migrationBuilder.DropTable(
                name: "QualityDefs");
        }
    }
}
