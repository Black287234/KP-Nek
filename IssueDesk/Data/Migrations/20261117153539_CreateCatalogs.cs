using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueDesk.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonnelNumber = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 6, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    MiddleName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Position = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DismissedOn = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InventoryNumber = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Condition = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    WrittenOffOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    WriteOffReason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PersonnelNumber",
                table: "Employees",
                column: "PersonnelNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_InventoryNumber",
                table: "Equipment",
                column: "InventoryNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Equipment");
        }
    }
}
