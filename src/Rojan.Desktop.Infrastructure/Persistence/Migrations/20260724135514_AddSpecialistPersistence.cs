using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rojan.Desktop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialistPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Specialists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Bio = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecialistSkills",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SpecialistId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialistSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialistSkills_Specialists_SpecialistId",
                        column: x => x.SpecialistId,
                        principalTable: "Specialists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Specialists_Status",
                table: "Specialists",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialistSkills_SpecialistId",
                table: "SpecialistSkills",
                column: "SpecialistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecialistSkills");

            migrationBuilder.DropTable(
                name: "Specialists");
        }
    }
}
