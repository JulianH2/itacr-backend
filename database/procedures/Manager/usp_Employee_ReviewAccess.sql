-- =============================================================================
-- Object:      dbo.usp_Employee_ReviewAccess
-- Type:        stored procedure
-- Purpose:     A manager approves or rejects an account that asked for access (HU_15), in one transaction. The account
--              only changes while it is still PendingApproval, so when two managers act on it at the same moment the
--              second one changes nothing. Approving records the manager in ApprovedById; rejecting keeps the row as
--              Rejected (its unique email stays taken) and leaves ApprovedById empty.
-- Parameters:  @Id uniqueidentifier - the employee that asked for the account
--              @NewStatus nvarchar(20) - Active or Rejected
--              @ReviewedById uniqueidentifier - the manager who decided
--              @Now datetime2 - current time (UTC)
-- Returns:     Outcome int: 1 the status changed, 2 the account was not pending (already handled), 0 it does not exist
-- Author:      JulianH2
-- Jira:        ITACR-21
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Employee_ReviewAccess
    @Id uniqueidentifier,
    @NewStatus nvarchar(20),
    @ReviewedById uniqueidentifier,
    @Now datetime2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.Employees
        SET Status = @NewStatus,
            ApprovedById = CASE WHEN @NewStatus = N'Active' THEN @ReviewedById ELSE ApprovedById END,
            UpdatedAt = @Now
        WHERE Id = @Id AND Status = N'PendingApproval';

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Employees WHERE Id = @Id) THEN 2 ELSE 0 END AS Outcome;
            RETURN;
        END;

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
