using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rojan.Desktop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservedSlots",
                columns: table => new
                {
                    SpecialistId = table.Column<string>(type: "TEXT", nullable: false),
                    Start = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    End = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservedSlots", x => new { x.SpecialistId, x.Start, x.End });
                });

            migrationBuilder.CreateTable(
                name: "WorkingSchedules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SpecialistId = table.Column<string>(type: "TEXT", nullable: false),
                    SpecialistName = table.Column<string>(type: "TEXT", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkingScheduleBreaks",
                columns: table => new
                {
                    WorkingScheduleId = table.Column<string>(type: "TEXT", nullable: false),
                    Start = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    End = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingScheduleBreaks", x => new { x.WorkingScheduleId, x.Start, x.End });
                    table.ForeignKey(
                        name: "FK_WorkingScheduleBreaks_WorkingSchedules_WorkingScheduleId",
                        column: x => x.WorkingScheduleId,
                        principalTable: "WorkingSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkingSchedules_SpecialistId_DayOfWeek",
                table: "WorkingSchedules",
                columns: new[] { "SpecialistId", "DayOfWeek" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservedSlots");

            migrationBuilder.DropTable(
                name: "WorkingScheduleBreaks");

            migrationBuilder.DropTable(
                name: "WorkingSchedules");
        }
    }
}
