using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalisApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase5Admin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "MediaAssets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VideoId",
                table: "MediaAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Achievements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_VideoId",
                table: "MediaAssets",
                column: "VideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_MediaAssets_Videos_VideoId",
                table: "MediaAssets",
                column: "VideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaAssets_Videos_VideoId",
                table: "MediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_VideoId",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Achievements");
        }
    }
}
