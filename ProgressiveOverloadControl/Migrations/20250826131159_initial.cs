using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProgressiveOverloadControl.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SetLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Exercise = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SetNo = table.Column<int>(type: "int", nullable: false),
                    Reps = table.Column<int>(type: "int", nullable: false),
                    WeightKg = table.Column<double>(type: "float", nullable: false),
                    RIR = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SetLogs_Date_Exercise",
                table: "SetLogs",
                columns: new[] { "Date", "Exercise" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SetLogs");
        }
    }
}
