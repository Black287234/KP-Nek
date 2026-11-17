using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueDesk.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IssuedCondition = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    DueOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IssueNote = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ReturnedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReturnedCondition = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    ReturnNote = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.CheckConstraint("CK_Assignments_Dates", "\"ReturnedOn\" IS NULL OR \"ReturnedOn\" >= \"IssuedOn\"");
                    table.ForeignKey(
                        name: "FK_Assignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_EmployeeId_ReturnedOn",
                table: "Assignments",
                columns: new[] { "EmployeeId", "ReturnedOn" });

            migrationBuilder.CreateIndex(
                name: "UX_Assignments_OpenPerEquipment",
                table: "Assignments",
                column: "EquipmentId",
                unique: true,
                filter: "\"ReturnedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assignments");
        }
    }
}
