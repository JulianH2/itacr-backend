-- =============================================================================
-- Object:      dbo.usp_PushSubscription_GetByRecipient
-- Type:        stored procedure
-- Purpose:     Returns the push subscriptions of one employee or one customer, the browsers to notify (HU_09).
-- Parameters:  @EmployeeId uniqueidentifier, @CustomerId uniqueidentifier - one of the two is set
-- Returns:     0..n rows: Endpoint, P256dh, Auth
-- Author:      JulianH2
-- Jira:        ITACR-15
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_PushSubscription_GetByRecipient
    @EmployeeId uniqueidentifier,
    @CustomerId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.Endpoint, s.P256dh, s.Auth
    FROM dbo.PushSubscriptions AS s
    WHERE (@EmployeeId IS NOT NULL AND s.EmployeeId = @EmployeeId)
       OR (@CustomerId IS NOT NULL AND s.CustomerId = @CustomerId);
END;
