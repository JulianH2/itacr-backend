-- =============================================================================
-- Object:      dbo.usp_Product_GetByIds
-- Type:        stored procedure
-- Purpose:     Returns the products of an order being placed, so their prices come from the menu and never from
--              the client. Ids that do not exist are not returned.
-- Parameters:  @ProductIdsJson nvarchar(max) - JSON array of product ids, e.g. ["20000000-...-000000000001"]
-- Returns:     Id uniqueidentifier, CategoryId uniqueidentifier, Name nvarchar(100), Description nvarchar(500),
--              Price decimal(10,2), ImageUrl nvarchar(500), IsCombo bit
-- Author:      JulianH2
-- Jira:        ITACR-9
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Product_GetByIds
    @ProductIdsJson nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.Id, p.CategoryId, p.Name, p.Description, p.Price, p.ImageUrl, p.IsCombo
    FROM dbo.Products AS p
    WHERE p.Id IN (SELECT TRY_CAST(ids.value AS uniqueidentifier) FROM OPENJSON(@ProductIdsJson) AS ids);
END;
