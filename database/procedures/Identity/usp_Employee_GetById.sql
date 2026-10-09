-- =============================================================================
-- Object:      dbo.usp_Employee_GetById
-- Type:        stored procedure
-- Purpose:     Returns one employee with the data the sign-in needs (role, status, hashes, lockout).
-- Parameters:  @Id uniqueidentifier - employee to return
-- Returns:     0 or 1 row of dbo.Employees
-- Author:      JulianH2
-- Jira:        ITACR-36
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Employee_GetById
    @Id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.Id, e.FullName, e.Email, e.Phone, e.Role, e.PinHash, e.PasswordHash, e.Status, e.ApprovedById,
        e.FailedLoginAttempts, e.LockoutEndAt, e.CreatedAt, e.UpdatedAt
    FROM dbo.Employees AS e
    WHERE e.Id = @Id;
END;
