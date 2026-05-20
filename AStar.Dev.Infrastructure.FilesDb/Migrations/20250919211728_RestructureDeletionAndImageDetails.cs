using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class RestructureDeletionAndImageDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HardDeletePending",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "ImageHeight",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "ImageWidth",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "SoftDeletePending",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "SoftDeleted",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "HardDeletePending",
                schema: "files",
                table: "FileAccessDetail");

            migrationBuilder.DropColumn(
                name: "HardDeleted",
                schema: "files",
                table: "FileAccessDetail");

            migrationBuilder.DropColumn(
                name: "SoftDeletePending",
                schema: "files",
                table: "FileAccessDetail");

            migrationBuilder.DropColumn(
                name: "SoftDeleted",
                schema: "files",
                table: "FileAccessDetail");

            migrationBuilder.AddColumn<int>(
                name: "DeletionStatusId",
                schema: "files",
                table: "FileDetail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImageDetailId",
                schema: "files",
                table: "FileDetail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DeletionStatus",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoftDeleted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SoftDeletePending = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HardDeletePending = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletionStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageDetail",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageWidth = table.Column<int>(type: "int", nullable: true),
                    ImageHeight = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageDetail", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_DeletionStatusId",
                schema: "files",
                table: "FileDetail",
                column: "DeletionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_ImageDetailId",
                schema: "files",
                table: "FileDetail",
                column: "ImageDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileDetail_DeletionStatus_DeletionStatusId",
                schema: "files",
                table: "FileDetail",
                column: "DeletionStatusId",
                principalSchema: "files",
                principalTable: "DeletionStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileDetail_ImageDetail_ImageDetailId",
                schema: "files",
                table: "FileDetail",
                column: "ImageDetailId",
                principalSchema: "files",
                principalTable: "ImageDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileDetail_DeletionStatus_DeletionStatusId",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_FileDetail_ImageDetail_ImageDetailId",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.DropTable(
                name: "DeletionStatus",
                schema: "files");

            migrationBuilder.DropTable(
                name: "ImageDetail",
                schema: "files");

            migrationBuilder.DropIndex(
                name: "IX_FileDetail_DeletionStatusId",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.DropIndex(
                name: "IX_FileDetail_ImageDetailId",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "DeletionStatusId",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "ImageDetailId",
                schema: "files",
                table: "FileDetail");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HardDeletePending",
                schema: "files",
                table: "FileDetail",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageHeight",
                schema: "files",
                table: "FileDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageWidth",
                schema: "files",
                table: "FileDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SoftDeletePending",
                schema: "files",
                table: "FileDetail",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SoftDeleted",
                schema: "files",
                table: "FileDetail",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HardDeletePending",
                schema: "files",
                table: "FileAccessDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HardDeleted",
                schema: "files",
                table: "FileAccessDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SoftDeletePending",
                schema: "files",
                table: "FileAccessDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SoftDeleted",
                schema: "files",
                table: "FileAccessDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
