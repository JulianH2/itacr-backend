-- =============================================================================
-- Object:      dbo.usp_Employee_RegisterFailedPinLogin
-- Type:        stored procedure
-- Purpose:     Counts a wrong PIN and locks the PIN sign-in when the count reaches the maximum, in a single
--              UPDATE, so two wrong PINs at the same time never overwrite each other's count. Locking resets
--              the count. An employee that is already locked is not changed.
-- Parameters:  @Id uniqueidentifier - employee who entered the wrong PIN
--              @MaxFailedAttempts int - failed attempts that lock the PIN sign-in
--              @LockoutEndAt datetime2 - end of the lockout if this attempt locks it (UTC)
--              @Now datetime2 - current time (UTC)
-- Returns:     IsLocked bit, LockoutEndAt datetime2 (no row if the employee does not exist)
-- Author:      JulianH2
-- Jira:        ITACR-36
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Employee_RegisterFailedPinLogin
    @Id uniqueidentifier,
    @MaxFailedAttempts int,
    @LockoutEndAt datetime2,
    @Now datetime2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Result TABLE (IsLocked bit NOT NULL, LockoutEndAt datetime2 NULL);

    UPDATE dbo.Employees
    SET FailedLoginAttempts = CASE WHEN FailedLoginAttempts + 1 >= @MaxFailedAttempts THEN 0 ELSE FailedLoginAttempts + 1 END,
        LockoutEndAt = CASE WHEN FailedLoginAttempts + 1 >= @MaxFailedAttempts THEN @LockoutEndAt ELSE LockoutEndAt END,
        UpdatedAt = @Now
    OUTPUT CAST(CASE WHEN deleted.FailedLoginAttempts + 1 >= @MaxFailedAttempts THEN 1 ELSE 0 END AS bit), inserted.LockoutEndAt
        INTO @Result (IsLocked, LockoutEndAt)
    WHERE Id = @Id AND (LockoutEndAt IS NULL OR LockoutEndAt <= @Now);

    -- Another request locked it between the caller's read and this UPDATE.
    IF NOT EXISTS (SELECT 1 FROM @Result)
    BEGIN
        INSERT INTO @Result (IsLocked, LockoutEndAt)
        SELECT CAST(1 AS bit), e.LockoutEndAt
        FROM dbo.Employees AS e
        WHERE e.Id = @Id;
    END;

    SELECT r.IsLocked, r.LockoutEndAt
    FROM @Result AS r;
END;
