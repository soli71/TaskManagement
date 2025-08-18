using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddNotStartedTaskStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId1",
                table: "ProjectAccess",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAccess_ProjectId1",
                table: "ProjectAccess",
                column: "ProjectId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAccess_Projects_ProjectId1",
                table: "ProjectAccess",
                column: "ProjectId1",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAccess_Projects_ProjectId1",
                table: "ProjectAccess");

            migrationBuilder.DropIndex(
                name: "IX_ProjectAccess_ProjectId1",
                table: "ProjectAccess");

            migrationBuilder.DropColumn(
                name: "ProjectId1",
                table: "ProjectAccess");
        }
    }
}
