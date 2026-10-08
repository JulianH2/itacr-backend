using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itacr.Infrastructure.Persistence.Migrations;

/// <summary>Deploys database/functions/fn_GetProductsByCategory.sql. Keep both in sync when the function changes.</summary>
[DbContext(typeof(ItacrDbContext))]
[Migration("20261002130000_AddFnGetProductsByCategory")]
public partial class AddFnGetProductsByCategory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- =============================================================================
            -- Object:      dbo.fn_GetProductsByCategory
            -- Type:        inline table-valued function
            -- Purpose:     Returns the products of one menu category for the catalog endpoint.
            -- Parameters:  @CategoryId uniqueidentifier - category whose products are returned
            -- Returns:     TABLE (Id uniqueidentifier, CategoryId uniqueidentifier, Name nvarchar(100),
            --                     Description nvarchar(500), Price decimal(10,2), ImageUrl nvarchar(500), IsCombo bit)
            -- Author:      GuillermoRH19
            -- Jira:        ITACR-8
            -- =============================================================================
            CREATE OR ALTER FUNCTION dbo.fn_GetProductsByCategory (@CategoryId uniqueidentifier)
            RETURNS TABLE
            AS
            RETURN
                SELECT p.Id, p.CategoryId, p.Name, p.Description, p.Price, p.ImageUrl, p.IsCombo
                FROM dbo.Products AS p
                WHERE p.CategoryId = @CategoryId;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.fn_GetProductsByCategory;");
    }
}
