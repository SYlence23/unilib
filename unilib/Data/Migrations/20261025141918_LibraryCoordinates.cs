using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace unilib.Data.Migrations
{
    /// <inheritdoc />
    public partial class LibraryCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The text address is dropped on purpose: coordinates replace it and the table had no
            // addresses to convert. Down() restores the empty column, not the old values.
            migrationBuilder.DropColumn(
                name: "address",
                table: "library");

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "library",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "library",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_library_coordinates_pair",
                table: "library",
                sql: "(latitude IS NULL) = (longitude IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_library_latitude_range",
                table: "library",
                sql: "latitude BETWEEN -90 AND 90");

            migrationBuilder.AddCheckConstraint(
                name: "ck_library_longitude_range",
                table: "library",
                sql: "longitude BETWEEN -180 AND 180");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_library_coordinates_pair",
                table: "library");

            migrationBuilder.DropCheckConstraint(
                name: "ck_library_latitude_range",
                table: "library");

            migrationBuilder.DropCheckConstraint(
                name: "ck_library_longitude_range",
                table: "library");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "library");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "library");

            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "library",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
