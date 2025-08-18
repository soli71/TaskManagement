using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectAccessModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ProjectAccess",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokeReason",
                table: "ProjectAccess",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "ProjectAccess",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevokedById",
                table: "ProjectAccess",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAccess_RevokedById",
                table: "ProjectAccess",
                column: "RevokedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAccess_AspNetUsers_RevokedById",
                table: "ProjectAccess",
                column: "RevokedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAccess_AspNetUsers_RevokedById",
                table: "ProjectAccess");

            migrationBuilder.DropIndex(
                name: "IX_ProjectAccess_RevokedById",
                table: "ProjectAccess");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ProjectAccess");

            migrationBuilder.DropColumn(
                name: "RevokeReason",
                table: "ProjectAccess");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "ProjectAccess");

            migrationBuilder.DropColumn(
                name: "RevokedById",
                table: "ProjectAccess");
        }
    }
}
