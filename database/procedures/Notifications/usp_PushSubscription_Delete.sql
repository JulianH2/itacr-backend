-- =============================================================================
-- Object:      dbo.usp_PushSubscription_Delete
-- Type:        stored procedure
-- Purpose:     Deletes a push subscription the push service reported as expired or unsubscribed (404 or 410, HU_09).
-- Parameters:  @Endpoint nvarchar(450) - the subscription's endpoint
-- Returns:     nothing
-- Author:      JulianH2
-- Jira:        ITACR-15
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_PushSubscription_Delete
    @Endpoint nvarchar(450)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.PushSubscriptions WHERE Endpoint = @Endpoint;
END;
