using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rojan.Desktop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecialistServices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<string>(type: "TEXT", nullable: false),
                    SpecialistId = table.Column<string>(type: "TEXT", nullable: false),
                    SpecialistName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialistServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialistServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Services_Category",
                table: "Services",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Status",
                table: "Services",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialistServices_ServiceId",
                table: "SpecialistServices",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecialistServices");

            migrationBuilder.DropTable(
                name: "Services");
        }
    }
}
