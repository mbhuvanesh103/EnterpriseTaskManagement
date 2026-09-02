using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Projects_PublicId",
                schema: "etm",
                table: "Projects",
                column: "PublicId");

            migrationBuilder.CreateTable(
                name: "Tasks",
                schema: "etm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProjectId = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    AssignedUserId = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    Priority = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Medium"),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Todo"),
                    DueDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "etm",
                        principalTable: "Projects",
                        principalColumn: "PublicId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalSchema: "etm",
                        principalTable: "Users",
                        principalColumn: "PublicId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedUserId",
                schema: "etm",
                table: "Tasks",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                schema: "etm",
                table: "Tasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_PublicId",
                schema: "etm",
                table: "Tasks",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status",
                schema: "etm",
                table: "Tasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tasks",
                schema: "etm");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Projects_PublicId",
                schema: "etm",
                table: "Projects");
        }
    }
}
