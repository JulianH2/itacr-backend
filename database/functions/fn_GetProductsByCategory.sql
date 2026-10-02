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
