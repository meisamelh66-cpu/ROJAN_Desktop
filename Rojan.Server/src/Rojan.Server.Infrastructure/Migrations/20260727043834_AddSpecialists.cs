using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rojan.Server.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Specialists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BranchId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialists", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Specialists_BranchId",
                table: "Specialists",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialists_OrganizationId",
                table: "Specialists",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialists_Phone",
                table: "Specialists",
                column: "Phone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Specialists");
        }
    }
}
