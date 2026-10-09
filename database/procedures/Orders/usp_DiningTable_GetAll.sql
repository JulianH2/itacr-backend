-- =============================================================================
-- Object:      dbo.usp_DiningTable_GetAll
-- Type:        stored procedure
-- Purpose:     Lists the dining tables in number order, for the waiter to assign an order to one.
-- Parameters:  none
-- Returns:     Id uniqueidentifier, Number int
-- Author:      JulianH2
-- Jira:        ITACR-9
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_DiningTable_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT t.Id, t.Number
    FROM dbo.DiningTables AS t
    ORDER BY t.Number;
END;
