-- =============================================================================
-- Object:      dbo.usp_Health_Ping
-- Type:        stored procedure
-- Purpose:     Used by GET /api/v1/health: running it proves the API can connect and has EXECUTE on dbo.
-- Parameters:  none
-- Returns:     Ok int (always 1)
-- Author:      JulianH2
-- Jira:        ITACR-36
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Health_Ping
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 1 AS Ok;
END;
