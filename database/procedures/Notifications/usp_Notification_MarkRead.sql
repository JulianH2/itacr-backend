-- =============================================================================
-- Object:      dbo.usp_Notification_MarkRead
-- Type:        stored procedure
-- Purpose:     Records when the recipient opened a notification (HU_09). Only the first time counts.
-- Parameters:  @Id uniqueidentifier - the notification
--              @Now datetime2 - current time (UTC)
-- Returns:     nothing
-- Author:      JulianH2
-- Jira:        ITACR-15
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Notification_MarkRead
    @Id uniqueidentifier,
    @Now datetime2
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Notifications
    SET ReadAt = @Now, UpdatedAt = @Now
    WHERE Id = @Id AND ReadAt IS NULL;
END;
