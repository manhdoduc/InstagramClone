using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstagramClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingIndexesAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Follows_FolloweeId",
                table: "Follows");

            migrationBuilder.DropIndex(
                name: "IX_Comments_PostId",
                table: "Comments");

            migrationBuilder.RenameIndex(
                name: "IX_Follows_FollowerId_FolloweeId",
                table: "Follows",
                newName: "IX_Follows_FollowerId_FolloweeId_Unique");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Hashtags",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Hashtags_Name_Unique",
                table: "Hashtags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Follows_FolloweeId_Status_CreatedAt",
                table: "Follows",
                columns: new[] { "FolloweeId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Follows_FollowerId_Status_CreatedAt",
                table: "Follows",
                columns: new[] { "FollowerId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PostId_CreatedAt",
                table: "Comments",
                columns: new[] { "PostId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Hashtags_Name_Unique",
                table: "Hashtags");

            migrationBuilder.DropIndex(
                name: "IX_Follows_FolloweeId_Status_CreatedAt",
                table: "Follows");

            migrationBuilder.DropIndex(
                name: "IX_Follows_FollowerId_Status_CreatedAt",
                table: "Follows");

            migrationBuilder.DropIndex(
                name: "IX_Comments_PostId_CreatedAt",
                table: "Comments");

            migrationBuilder.RenameIndex(
                name: "IX_Follows_FollowerId_FolloweeId_Unique",
                table: "Follows",
                newName: "IX_Follows_FollowerId_FolloweeId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Hashtags",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId",
                table: "Notifications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_FolloweeId",
                table: "Follows",
                column: "FolloweeId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PostId",
                table: "Comments",
                column: "PostId");
        }
    }
}
