using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InstagramClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "0114C45B-EB0A-4D57-950C-B435F395087F", "24e38ef9-0968-4fed-bc6f-8e6b2f41420b", "Administrator", "ADMINISTRATOR" },
                    { "3BA43D62-5360-4A30-A29A-D3F2BB371CC1", "4ebeedfc-8a96-4459-80aa-94e7c2b1fa22", "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "0114C45B-EB0A-4D57-950C-B435F395087F");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "3BA43D62-5360-4A30-A29A-D3F2BB371CC1");
        }
    }
}
