-- =============================================================================
-- Object:      dbo.usp_Employee_GetByEmail
-- Type:        stored procedure
-- Purpose:     Returns the employee with an email, for the email and password sign-in.
-- Parameters:  @Email nvarchar(254) - email, already trimmed by the caller
-- Returns:     0 or 1 row of dbo.Employees
-- Author:      JulianH2
-- Jira:        ITACR-36
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Employee_GetByEmail
    @Email nvarchar(254)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.Id, e.FullName, e.Email, e.Phone, e.Role, e.PinHash, e.PasswordHash, e.Status, e.ApprovedById,
        e.FailedLoginAttempts, e.LockoutEndAt, e.CreatedAt, e.UpdatedAt
    FROM dbo.Employees AS e
    WHERE e.Email = @Email;
END;
