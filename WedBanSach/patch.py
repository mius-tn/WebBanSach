import re
import os

filepath = ''
for f in os.listdir('Migrations'):
    if f.endswith('_AddInventoryAndPromotionModule.cs'):
        filepath = os.path.join('Migrations', f)
        break

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Inject drop constraints SQL
drop_sql = '''
            var dropConstraintsSql = @"
                DECLARE @sql NVARCHAR(MAX) = '';
                -- Default constraints
                SELECT @sql += 'ALTER TABLE [Books] DROP CONSTRAINT [' + obj.name + '];'
                FROM sys.default_constraints obj
                INNER JOIN sys.columns col ON obj.parent_object_id = col.object_id AND obj.parent_column_id = col.column_id
                WHERE obj.parent_object_id = OBJECT_ID('Books')
                AND col.name IN ('StockQuantity', 'SoldQuantity', 'Price', 'DiscountPrice');
                
                -- Check constraints directly linked
                SELECT @sql += 'ALTER TABLE [Books] DROP CONSTRAINT [' + obj.name + '];'
                FROM sys.check_constraints obj
                INNER JOIN sys.columns col ON obj.parent_object_id = col.object_id AND obj.parent_column_id = col.column_id
                WHERE obj.parent_object_id = OBJECT_ID('Books')
                AND col.name IN ('StockQuantity', 'SoldQuantity', 'Price', 'DiscountPrice');

                -- Check constraints not directly linked to columns via parent_column_id
                SELECT @sql += 'ALTER TABLE [Books] DROP CONSTRAINT [' + obj.name + '];'
                FROM sys.check_constraints obj
                WHERE obj.parent_object_id = OBJECT_ID('Books') AND obj.parent_column_id = 0
                AND (definition LIKE '%StockQuantity%' OR definition LIKE '%SoldQuantity%' OR definition LIKE '%Price%' OR definition LIKE '%DiscountPrice%');

                IF @sql <> '' EXEC sp_executesql @sql;
            ";
            migrationBuilder.Sql(dropConstraintsSql);
'''
content = re.sub(r'protected override void Up\(MigrationBuilder migrationBuilder\)\s*\{', 'protected override void Up(MigrationBuilder migrationBuilder)\n        {' + drop_sql, content)

# 2. Remove AddColumn for FaultyQuantity
content = re.sub(r'\s*migrationBuilder\.AddColumn<int>\(\s*name:\s*"FaultyQuantity",\s*table:\s*"Books",\s*type:\s*"int",\s*nullable:\s*false,\s*defaultValue:\s*0\);', '', content)

# 3. Remove DropColumn for FaultyQuantity
content = re.sub(r'\s*migrationBuilder\.DropColumn\(\s*name:\s*"FaultyQuantity",\s*table:\s*"Books"\);', '', content)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)
print("Migration patched!")
