using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace unilib.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "faculty",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faculty", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "library",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_library", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "book",
                columns: table => new
                {
                    isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    faculty_id = table.Column<int>(type: "integer", nullable: true),
                    library_id = table.Column<int>(type: "integer", nullable: true),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    publication_year = table.Column<int>(type: "integer", nullable: true),
                    pages = table.Column<int>(type: "integer", nullable: true),
                    language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_book", x => x.isbn);
                    table.ForeignKey(
                        name: "fk_book_category_category_id",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_book_faculty_faculty_id",
                        column: x => x.faculty_id,
                        principalTable: "faculty",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_book_library_library_id",
                        column: x => x.library_id,
                        principalTable: "library",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    faculty_id = table.Column<int>(type: "integer", nullable: true),
                    library_id = table.Column<int>(type: "integer", nullable: true),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    student_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_faculty_faculty_id",
                        column: x => x.faculty_id,
                        principalTable: "faculty",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_library_library_id",
                        column: x => x.library_id,
                        principalTable: "library",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "author",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    biography = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_author", x => x.id);
                    table.ForeignKey(
                        name: "fk_author_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "favorite",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    book_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    added_at = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_favorite", x => new { x.user_id, x.book_id });
                    table.ForeignKey(
                        name: "fk_favorite_book_book_id",
                        column: x => x.book_id,
                        principalTable: "book",
                        principalColumn: "isbn",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_favorite_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservation",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    book_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    reservation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservation", x => x.id);
                    table.ForeignKey(
                        name: "fk_reservation_book_book_id",
                        column: x => x.book_id,
                        principalTable: "book",
                        principalColumn: "isbn",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reservation_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_book",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    book_isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    borrow_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    return_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_book", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_book_book_book_isbn",
                        column: x => x.book_isbn,
                        principalTable: "book",
                        principalColumn: "isbn",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_book_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "book_author",
                columns: table => new
                {
                    book_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    author_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_book_author", x => new { x.book_id, x.author_id });
                    table.ForeignKey(
                        name: "fk_book_author_author_author_id",
                        column: x => x.author_id,
                        principalTable: "author",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_book_author_book_book_id",
                        column: x => x.book_id,
                        principalTable: "book",
                        principalColumn: "isbn",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_author_user_id",
                table: "author",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_category_id",
                table: "book",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_faculty_id",
                table: "book",
                column: "faculty_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_library_id",
                table: "book",
                column: "library_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_author_author_id",
                table: "book_author",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_category_name",
                table: "category",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_faculty_name",
                table: "faculty",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_favorite_book_id",
                table: "favorite",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservation_book_id",
                table: "reservation",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservation_user_id",
                table: "reservation",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_name",
                table: "role",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_email",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_faculty_id",
                table: "user",
                column: "faculty_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_library_id",
                table: "user",
                column: "library_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_id",
                table: "user",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_student_id",
                table: "user",
                column: "student_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_book_book_isbn",
                table: "user_book",
                column: "book_isbn");

            migrationBuilder.CreateIndex(
                name: "ix_user_book_user_id",
                table: "user_book",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "book_author");

            migrationBuilder.DropTable(
                name: "favorite");

            migrationBuilder.DropTable(
                name: "reservation");

            migrationBuilder.DropTable(
                name: "user_book");

            migrationBuilder.DropTable(
                name: "author");

            migrationBuilder.DropTable(
                name: "book");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "faculty");

            migrationBuilder.DropTable(
                name: "library");

            migrationBuilder.DropTable(
                name: "role");
        }
    }
}
