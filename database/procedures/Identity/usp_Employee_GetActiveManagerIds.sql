-- =============================================================================
-- Object:      dbo.usp_Employee_GetActiveManagerIds
-- Type:        stored procedure
-- Purpose:     Lists the active managers that receive a PIN reset request, except the employee who asked.
-- Parameters:  @ExcludedEmployeeId uniqueidentifier - employee who asked for the reset
-- Returns:     Id uniqueidentifier
-- Author:      JulianH2
-- Jira:        ITACR-36
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Employee_GetActiveManagerIds
    @ExcludedEmployeeId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.Id
    FROM dbo.Employees AS e
    WHERE e.Role = N'Manager' AND e.Status = N'Active' AND e.Id <> @ExcludedEmployeeId;
END;
