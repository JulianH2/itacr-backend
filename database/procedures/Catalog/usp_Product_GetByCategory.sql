-- =============================================================================
-- Object:      dbo.usp_Product_GetByCategory
-- Type:        stored procedure
-- Purpose:     Lists the products of one menu category ordered by name, on top of dbo.fn_GetProductsByCategory
--              (an inline function cannot ORDER BY). Ownership chaining lets callers with EXECUTE only read it.
-- Parameters:  @CategoryId uniqueidentifier - category whose products are returned
-- Returns:     Id uniqueidentifier, CategoryId uniqueidentifier, Name nvarchar(100), Description nvarchar(500),
--              Price decimal(10,2), ImageUrl nvarchar(500), IsCombo bit
-- Author:      JulianH2
-- Jira:        ITACR-36
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Product_GetByCategory
    @CategoryId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.Id, p.CategoryId, p.Name, p.Description, p.Price, p.ImageUrl, p.IsCombo
    FROM dbo.fn_GetProductsByCategory(@CategoryId) AS p
    ORDER BY p.Name;
END;
