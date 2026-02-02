using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodRecordsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 若表已存在则跳过，避免重复执行报错
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'FoodRecords')
BEGIN
    CREATE TABLE [dbo].[FoodRecords] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Amount] float NOT NULL,
        [EmissionFactor] decimal(18,4) NOT NULL,
        [Emission] decimal(18,4) NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FoodRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FoodRecords_ApplicationUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[ApplicationUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_FoodRecords_UserId] ON [dbo].[FoodRecords] ([UserId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.tables WHERE name = N'FoodRecords')
    DROP TABLE [dbo].[FoodRecords];
");
        }
    }
}
