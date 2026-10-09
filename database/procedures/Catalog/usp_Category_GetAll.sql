-- =============================================================================
-- Object:      dbo.usp_Category_GetAll
-- Type:        stored procedure
-- Purpose:     Lists the menu categories in display order.
-- Parameters:  none
-- Returns:     Id uniqueidentifier, Name nvarchar(60), SortOrder int
-- Author:      JulianH2
-- Jira:        ITACR-36
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Category_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.Id, c.Name, c.SortOrder
    FROM dbo.Categories AS c
    ORDER BY c.SortOrder;
END;
