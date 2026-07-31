using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WedBanSach.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAndPromotionModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop all constraints referencing old column names
            var dropConstraintsSql = @"
                DECLARE @sql NVARCHAR(MAX) = '';
                -- Default constraints
                SELECT @sql += 'ALTER TABLE [Books] DROP CONSTRAINT [' + obj.name + '];'
                FROM sys.default_constraints obj
                INNER JOIN sys.columns col ON obj.parent_object_id = col.object_id AND obj.parent_column_id = col.column_id
                WHERE obj.parent_object_id = OBJECT_ID('Books')
                AND col.name IN ('StockQuantity', 'SoldQuantity', 'Price', 'DiscountPrice');
                
                -- Check constraints
                SELECT @sql += 'ALTER TABLE [Books] DROP CONSTRAINT [' + obj.name + '];'
                FROM sys.check_constraints obj
                WHERE obj.parent_object_id = OBJECT_ID('Books') AND (
                    definition LIKE '%StockQuantity%' OR definition LIKE '%SoldQuantity%' OR 
                    definition LIKE '%Price%' OR definition LIKE '%DiscountPrice%'
                );

                IF @sql <> '' EXEC sp_executesql @sql;
            ";
            migrationBuilder.Sql(dropConstraintsSql);

            // Step 2: Rename existing columns
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Books",
                newName: "OriginalPrice");

            migrationBuilder.RenameColumn(
                name: "DiscountPrice",
                table: "Books",
                newName: "SalePrice");

            migrationBuilder.RenameColumn(
                name: "StockQuantity",
                table: "Books",
                newName: "TotalStock");

            migrationBuilder.RenameColumn(
                name: "SoldQuantity",
                table: "Books",
                newName: "SoldStock");

            // Step 3: Add new columns
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPrice",
                table: "Books",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SalePercent",
                table: "Books",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleStartDate",
                table: "Books",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleEndDate",
                table: "Books",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPromotionActive",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReservedStock",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinimumStock",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 20);

            // Step 4: Initialize CurrentPrice = OriginalPrice for existing data
            migrationBuilder.Sql("UPDATE [Books] SET [CurrentPrice] = [OriginalPrice] WHERE [CurrentPrice] = 0");

            // Step 5: Add new check constraints
            migrationBuilder.AddCheckConstraint(
                name: "CK_Book_TotalStock",
                table: "Books",
                sql: "TotalStock >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Book_ReservedStock",
                table: "Books",
                sql: "ReservedStock >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Book_SoldStock",
                table: "Books",
                sql: "SoldStock >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Book_TotalStock",
                table: "Books");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Book_ReservedStock",
                table: "Books");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Book_SoldStock",
                table: "Books");

            migrationBuilder.DropColumn(name: "CurrentPrice", table: "Books");
            migrationBuilder.DropColumn(name: "SalePercent", table: "Books");
            migrationBuilder.DropColumn(name: "SaleStartDate", table: "Books");
            migrationBuilder.DropColumn(name: "SaleEndDate", table: "Books");
            migrationBuilder.DropColumn(name: "IsPromotionActive", table: "Books");
            migrationBuilder.DropColumn(name: "ReservedStock", table: "Books");
            migrationBuilder.DropColumn(name: "MinimumStock", table: "Books");

            migrationBuilder.RenameColumn(name: "OriginalPrice", table: "Books", newName: "Price");
            migrationBuilder.RenameColumn(name: "SalePrice", table: "Books", newName: "DiscountPrice");
            migrationBuilder.RenameColumn(name: "TotalStock", table: "Books", newName: "StockQuantity");
            migrationBuilder.RenameColumn(name: "SoldStock", table: "Books", newName: "SoldQuantity");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Book_StockQuantity",
                table: "Books",
                sql: "StockQuantity >= 0");
        }
    }
}
