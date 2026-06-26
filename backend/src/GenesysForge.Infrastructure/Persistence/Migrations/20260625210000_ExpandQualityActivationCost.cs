using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations;

/// <summary>
/// Расширяет поле после того, как AddItemQualities могла примениться, но startup seed
/// завершился ошибкой на значении длиннее 160 символов.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260625210000_ExpandQualityActivationCost")]
public partial class ExpandQualityActivationCost : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "ActivationCost",
            table: "QualityDefs",
            type: "character varying(400)",
            maxLength: 400,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(160)",
            oldMaxLength: 160);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "ActivationCost",
            table: "QualityDefs",
            type: "character varying(160)",
            maxLength: 160,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(400)",
            oldMaxLength: 400);
    }
}
