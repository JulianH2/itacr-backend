-- =============================================================================
-- Object:      dbo.usp_Employee_AssignTemporaryPin
-- Type:        stored procedure
-- Purpose:     A manager answers a PIN reset request (HU_15): the temporary PIN replaces the PIN of the active employee,
--              the failed attempts and the lockout are cleared, and every unresolved PinReset notification of that
--              employee (one per manager) is marked as resolved, all in one transaction. The notifications are resolved
--              first, so when two managers answer the same request at the same moment the second one finds nothing to
--              resolve and changes nothing.
-- Parameters:  @EmployeeId uniqueidentifier - the employee who asked for the reset
--              @PinHash nvarchar(255) - hash of the temporary PIN; the PIN itself is never stored
--              @Now datetime2 - current time (UTC)
-- Returns:     Outcome int: 1 the PIN was assigned, 2 there was no pending request (already handled), 0 the employee
--              does not exist or is not active
-- Author:      JulianH2
-- Jira:        ITACR-21
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Employee_AssignTemporaryPin
    @EmployeeId uniqueidentifier,
    @PinHash nvarchar(255),
    @Now datetime2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE Id = @EmployeeId AND Status = N'Active')
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS Outcome;
            RETURN;
        END;

        UPDATE dbo.Notifications
        SET ResolvedAt = @Now, UpdatedAt = @Now
        WHERE Type = N'PinReset' AND SubjectEmployeeId = @EmployeeId AND ResolvedAt IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 2 AS Outcome;
            RETURN;
        END;

        UPDATE dbo.Employees
        SET PinHash = @PinHash,
            FailedLoginAttempts = 0,
            LockoutEndAt = NULL,
            UpdatedAt = @Now
        WHERE Id = @EmployeeId;

        COMMIT TRANSACTION;
        SELECT 1 AS Outcome;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;
END;
