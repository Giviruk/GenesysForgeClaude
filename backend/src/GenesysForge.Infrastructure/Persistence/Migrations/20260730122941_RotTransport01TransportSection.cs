using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Раздел «Транспорт» (ROT-TRANSPORT-01): тип и режим движения у профиля, тяга у экземпляра,
    /// груз у позиций инвентаря.
    /// <para>
    /// Удаление <c>CharacterMounts.CarriedLoad</c> — необратимый шаг, принятый владельцем явно:
    /// прежнее число описывало груз без описи, и переносить его в позиции нечем. После миграции
    /// загрузка считается только по позициям груза.
    /// </para>
    /// </summary>
    public partial class RotTransport01TransportSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarriedLoad",
                table: "CharacterMounts");

            migrationBuilder.AddColumn<int>(
                name: "MovementMode",
                table: "MountDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresTraction",
                table: "MountDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TransportKind",
                table: "MountDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DrawnByMountId",
                table: "CharacterMounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CarriedByMountId",
                table: "CharacterItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInstalledOnMount",
                table: "CharacterItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterMounts_DrawnByMountId",
                table: "CharacterMounts",
                column: "DrawnByMountId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterItems_CarriedByMountId",
                table: "CharacterItems",
                column: "CarriedByMountId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterItems_CharacterMounts_CarriedByMountId",
                table: "CharacterItems",
                column: "CarriedByMountId",
                principalTable: "CharacterMounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterMounts_CharacterMounts_DrawnByMountId",
                table: "CharacterMounts",
                column: "DrawnByMountId",
                principalTable: "CharacterMounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterItems_CharacterMounts_CarriedByMountId",
                table: "CharacterItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterMounts_CharacterMounts_DrawnByMountId",
                table: "CharacterMounts");

            migrationBuilder.DropIndex(
                name: "IX_CharacterMounts_DrawnByMountId",
                table: "CharacterMounts");

            migrationBuilder.DropIndex(
                name: "IX_CharacterItems_CarriedByMountId",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "MovementMode",
                table: "MountDefs");

            migrationBuilder.DropColumn(
                name: "RequiresTraction",
                table: "MountDefs");

            migrationBuilder.DropColumn(
                name: "TransportKind",
                table: "MountDefs");

            migrationBuilder.DropColumn(
                name: "DrawnByMountId",
                table: "CharacterMounts");

            migrationBuilder.DropColumn(
                name: "CarriedByMountId",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "IsInstalledOnMount",
                table: "CharacterItems");

            migrationBuilder.AddColumn<int>(
                name: "CarriedLoad",
                table: "CharacterMounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
