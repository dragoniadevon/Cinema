using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinema.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cinemas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cinemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<short>(type: "smallint", nullable: true),
                    Releasedate = table.Column<DateOnly>(type: "date", nullable: true),
                    Rating = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    Posterurl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Trailerurl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Agerating = table.Column<short>(type: "smallint", nullable: true),
                    Languagecode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Countrycode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Isactive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Personroles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personroles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fullname = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pricecategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pricecategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Halls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cinemaid = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rows = table.Column<short>(type: "smallint", nullable: true),
                    Seatsperrow = table.Column<short>(type: "smallint", nullable: true),
                    Halltype = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Halls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Halls_Cinemas_Cinemaid",
                        column: x => x.Cinemaid,
                        principalTable: "Cinemas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Moviegenres",
                columns: table => new
                {
                    Movieid = table.Column<int>(type: "int", nullable: false),
                    Genreid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moviegenres", x => new { x.Movieid, x.Genreid });
                    table.ForeignKey(
                        name: "FK_Moviegenres_Genres_Genreid",
                        column: x => x.Genreid,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Moviegenres_Movies_Movieid",
                        column: x => x.Movieid,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Moviepersons",
                columns: table => new
                {
                    Movieid = table.Column<int>(type: "int", nullable: false),
                    Personid = table.Column<int>(type: "int", nullable: false),
                    Roleid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moviepersons", x => new { x.Movieid, x.Personid, x.Roleid });
                    table.ForeignKey(
                        name: "FK_Moviepersons_Movies_Movieid",
                        column: x => x.Movieid,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Moviepersons_Personroles_Roleid",
                        column: x => x.Roleid,
                        principalTable: "Personroles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Moviepersons_Persons_Personid",
                        column: x => x.Personid,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Firstname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lastname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Passwordhash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Roleid = table.Column<int>(type: "int", nullable: true),
                    Createdat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_Roleid",
                        column: x => x.Roleid,
                        principalTable: "Roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Seats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Hallid = table.Column<int>(type: "int", nullable: true),
                    Rownumber = table.Column<short>(type: "smallint", nullable: true),
                    Seatnumber = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seats_Halls_Hallid",
                        column: x => x.Hallid,
                        principalTable: "Halls",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Movieid = table.Column<int>(type: "int", nullable: true),
                    Hallid = table.Column<int>(type: "int", nullable: true),
                    Starttime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Endtime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Format = table.Column<short>(type: "smallint", nullable: true),
                    Isactive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Halls_Hallid",
                        column: x => x.Hallid,
                        principalTable: "Halls",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sessions_Movies_Movieid",
                        column: x => x.Movieid,
                        principalTable: "Movies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Userid = table.Column<int>(type: "int", nullable: true),
                    Movieid = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Createdat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recommendations_Movies_Movieid",
                        column: x => x.Movieid,
                        principalTable: "Movies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Recommendations_Users_Userid",
                        column: x => x.Userid,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Usergenrepreferences",
                columns: table => new
                {
                    Userid = table.Column<int>(type: "int", nullable: false),
                    Genreid = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usergenrepreferences", x => new { x.Userid, x.Genreid });
                    table.ForeignKey(
                        name: "FK_Usergenrepreferences_Genres_Genreid",
                        column: x => x.Genreid,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Usergenrepreferences_Users_Userid",
                        column: x => x.Userid,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessionprices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sessionid = table.Column<int>(type: "int", nullable: true),
                    Categoryid = table.Column<int>(type: "int", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessionprices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessionprices_Pricecategories_Categoryid",
                        column: x => x.Categoryid,
                        principalTable: "Pricecategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sessionprices_Sessions_Sessionid",
                        column: x => x.Sessionid,
                        principalTable: "Sessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Userid = table.Column<int>(type: "int", nullable: true),
                    Sessionid = table.Column<int>(type: "int", nullable: true),
                    Seatid = table.Column<int>(type: "int", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: true),
                    Bookingtime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Seats_Seatid",
                        column: x => x.Seatid,
                        principalTable: "Seats",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tickets_Sessions_Sessionid",
                        column: x => x.Sessionid,
                        principalTable: "Sessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tickets_Users_Userid",
                        column: x => x.Userid,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ticketid = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Paymentdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Tickets_Ticketid",
                        column: x => x.Ticketid,
                        principalTable: "Tickets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Halls_Cinemaid",
                table: "Halls",
                column: "Cinemaid");

            migrationBuilder.CreateIndex(
                name: "IX_Moviegenres_Genreid",
                table: "Moviegenres",
                column: "Genreid");

            migrationBuilder.CreateIndex(
                name: "IX_Moviepersons_Personid",
                table: "Moviepersons",
                column: "Personid");

            migrationBuilder.CreateIndex(
                name: "IX_Moviepersons_Roleid",
                table: "Moviepersons",
                column: "Roleid");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Ticketid",
                table: "Payments",
                column: "Ticketid",
                unique: true,
                filter: "[Ticketid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_Movieid",
                table: "Recommendations",
                column: "Movieid");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_Userid",
                table: "Recommendations",
                column: "Userid");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_Hallid_Rownumber_Seatnumber",
                table: "Seats",
                columns: new[] { "Hallid", "Rownumber", "Seatnumber" },
                unique: true,
                filter: "[Hallid] IS NOT NULL AND [Rownumber] IS NOT NULL AND [Seatnumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sessionprices_Categoryid",
                table: "Sessionprices",
                column: "Categoryid");

            migrationBuilder.CreateIndex(
                name: "IX_Sessionprices_Sessionid",
                table: "Sessionprices",
                column: "Sessionid");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Hallid",
                table: "Sessions",
                column: "Hallid");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Movieid",
                table: "Sessions",
                column: "Movieid");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Seatid",
                table: "Tickets",
                column: "Seatid");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Sessionid_Seatid",
                table: "Tickets",
                columns: new[] { "Sessionid", "Seatid" },
                unique: true,
                filter: "[Sessionid] IS NOT NULL AND [Seatid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Userid",
                table: "Tickets",
                column: "Userid");

            migrationBuilder.CreateIndex(
                name: "IX_Usergenrepreferences_Genreid",
                table: "Usergenrepreferences",
                column: "Genreid");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Roleid",
                table: "Users",
                column: "Roleid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Moviegenres");

            migrationBuilder.DropTable(
                name: "Moviepersons");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Recommendations");

            migrationBuilder.DropTable(
                name: "Sessionprices");

            migrationBuilder.DropTable(
                name: "Usergenrepreferences");

            migrationBuilder.DropTable(
                name: "Personroles");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Pricecategories");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Seats");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Halls");

            migrationBuilder.DropTable(
                name: "Movies");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Cinemas");
        }
    }
}
