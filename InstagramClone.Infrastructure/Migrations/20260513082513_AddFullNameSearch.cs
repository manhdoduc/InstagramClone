using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstagramClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullNameSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MessageReactions_UserId",
                table: "MessageReactions");

            migrationBuilder.AddColumn<string>(
                name: "FullNameSearch",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PostId1",
                table: "SavedPosts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_FullNameSearch",
                table: "Users",
                column: "FullNameSearch");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPosts_PostId1",
                table: "SavedPosts",
                column: "PostId1");

            migrationBuilder.CreateIndex(
                name: "IX_Unique_User_Message_Reaction",
                table: "MessageReactions",
                columns: new[] { "UserId", "MessageId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedPosts_Posts_PostId1",
                table: "SavedPosts",
                column: "PostId1",
                principalTable: "Posts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedPosts_Posts_PostId1",
                table: "SavedPosts");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_FullNameSearch",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_UserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SavedPosts_PostId1",
                table: "SavedPosts");

            migrationBuilder.DropIndex(
                name: "IX_Unique_User_Message_Reaction",
                table: "MessageReactions");

            migrationBuilder.DropColumn(
                name: "FullNameSearch",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PostId1",
                table: "SavedPosts");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_UserId",
                table: "MessageReactions",
                column: "UserId");
        }
    }
}
