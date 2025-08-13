using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceEmailLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_Tasks_TaskItemId",
                table: "InvoiceLines");

            migrationBuilder.CreateTable(
                name: "InvoiceEmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ToEmail = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    SentBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceEmailLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceEmailLogs_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceEmailLogs_InvoiceId",
                table: "InvoiceEmailLogs",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_Tasks_TaskItemId",
                table: "InvoiceLines",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_Tasks_TaskItemId",
                table: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "InvoiceEmailLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_Tasks_TaskItemId",
                table: "InvoiceLines",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
