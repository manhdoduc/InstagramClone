using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InstagramClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRoleSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityRole");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("0114c45b-eb0a-4d57-950c-b435f395087f"), "24e38ef9-0968-4fed-bc6f-8e6b2f41420b", "Administrator", "ADMINISTRATOR" },
                    { new Guid("3ba43d62-5360-4a30-a29a-d3f2bb371cc1"), "4ebeedfc-8a96-4459-80aa-94e7c2b1fa22", "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("0114c45b-eb0a-4d57-950c-b435f395087f"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3ba43d62-5360-4a30-a29a-d3f2bb371cc1"));

            migrationBuilder.CreateTable(
                name: "IdentityRole",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityRole", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "0114C45B-EB0A-4D57-950C-B435F395087F", "24e38ef9-0968-4fed-bc6f-8e6b2f41420b", "Administrator", "ADMINISTRATOR" },
                    { "3BA43D62-5360-4A30-A29A-D3F2BB371CC1", "4ebeedfc-8a96-4459-80aa-94e7c2b1fa22", "User", "USER" }
                });
        }
    }
}
