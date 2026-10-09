-- =============================================================================
-- Object:      dbo.usp_Employee_GetActiveByRole
-- Type:        stored procedure
-- Purpose:     Lists the active employees of one role for the login picker, ordered by name.
-- Parameters:  @Role nvarchar(20) - Waiter, Kitchen, or Manager
-- Returns:     Id uniqueidentifier, FullName nvarchar(120)
-- Author:      JulianH2
-- Jira:        ITACR-36
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Employee_GetActiveByRole
    @Role nvarchar(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.Id, e.FullName
    FROM dbo.Employees AS e
    WHERE e.Role = @Role AND e.Status = N'Active'
    ORDER BY e.FullName;
END;
