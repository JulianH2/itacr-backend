-- =============================================================================
-- Object:      dbo.usp_Employee_RegisterSuccessfulPinLogin
-- Type:        stored procedure
-- Purpose:     Clears the failed PIN count and the lockout after a right PIN, and stores the new hash when the
--              hasher asked for a rehash. The row is only written when one of those values changes.
-- Parameters:  @Id uniqueidentifier - employee who entered the right PIN
--              @NewPinHash nvarchar(255) - new PIN hash, or NULL to keep the current one
--              @Now datetime2 - current time (UTC)
-- Returns:     nothing
-- Author:      JulianH2
-- Jira:        ITACR-36
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Employee_RegisterSuccessfulPinLogin
    @Id uniqueidentifier,
    @NewPinHash nvarchar(255),
    @Now datetime2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE dbo.Employees
    SET FailedLoginAttempts = 0,
        LockoutEndAt = NULL,
        PinHash = COALESCE(@NewPinHash, PinHash),
        UpdatedAt = @Now
    WHERE Id = @Id
        AND (FailedLoginAttempts <> 0 OR LockoutEndAt IS NOT NULL OR @NewPinHash IS NOT NULL);
END;
