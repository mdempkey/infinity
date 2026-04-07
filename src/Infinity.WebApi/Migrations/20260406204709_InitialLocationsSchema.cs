using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infinity.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialLocationsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "varchar(50)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parks",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(20)", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    resort = table.Column<string>(type: "varchar(255)", nullable: true),
                    city = table.Column<string>(type: "varchar(100)", nullable: true),
                    country = table.Column<string>(type: "varchar(100)", nullable: true),
                    lat = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    lng = table.Column<decimal>(type: "numeric(9,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attractions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    park_id = table.Column<string>(type: "varchar(20)", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    lat = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    lng = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    image_urls = table.Column<string>(type: "jsonb", nullable: true),
                    tags = table.Column<string>(type: "jsonb", nullable: true),
                    avg_rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    review_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attractions", x => x.id);
                    table.ForeignKey(
                        name: "FK_attractions_parks_park_id",
                        column: x => x.park_id,
                        principalTable: "parks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attraction_categories",
                columns: table => new
                {
                    attraction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attraction_categories", x => new { x.attraction_id, x.category_id });
                    table.ForeignKey(
                        name: "FK_attraction_categories_attractions_attraction_id",
                        column: x => x.attraction_id,
                        principalTable: "attractions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attraction_categories_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attraction_categories_category_id",
                table: "attraction_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_attractions_park_id",
                table: "attractions",
                column: "park_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_name",
                table: "categories",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attraction_categories");

            migrationBuilder.DropTable(
                name: "attractions");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "parks");
        }
    }
}
