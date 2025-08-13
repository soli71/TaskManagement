using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLinesAndCleanupTaskItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Invoices_InvoiceId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_InvoiceId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "InvoicedAmount",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "InvoicedHourlyRate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "InvoicedHours",
                table: "Tasks");

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaskItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    PerformerName = table.Column<string>(type: "TEXT", nullable: true),
                    GradeName = table.Column<string>(type: "TEXT", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    Hours = table.Column<double>(type: "REAL", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Tasks_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "Tasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_TaskItemId",
                table: "InvoiceLines",
                column: "TaskItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InvoicedAmount",
                table: "Tasks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InvoicedHourlyRate",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "InvoicedHours",
                table: "Tasks",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_InvoiceId",
                table: "Tasks",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Invoices_InvoiceId",
                table: "Tasks",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
