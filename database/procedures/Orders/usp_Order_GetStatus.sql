-- =============================================================================
-- Object:      dbo.usp_Order_GetStatus
-- Type:        stored procedure
-- Purpose:     Returns the current status of an order and its last history change, so the API can check the next
--              step (or an undo) before changing it.
-- Parameters:  @Id uniqueidentifier - order id
-- Returns:     0 or 1 row: Id, Status, WaiterId (null for a pickup order), LastFromStatus (null for the change that created the order), LastToStatus,
--              LastChangedById, LastChangedAt
-- Author:      JulianH2
-- Jira:        ITACR-11, ITACR-16
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Order_GetStatus
    @Id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT o.Id, o.Status, o.WaiterId,
        h.FromStatus AS LastFromStatus, h.ToStatus AS LastToStatus, h.ChangedById AS LastChangedById, h.CreatedAt AS LastChangedAt
    FROM dbo.Orders AS o
    CROSS APPLY (
        SELECT TOP (1) FromStatus, ToStatus, ChangedById, CreatedAt
        FROM dbo.OrderStatusHistory
        WHERE OrderId = o.Id
        ORDER BY CreatedAt DESC, Id DESC
    ) AS h
    WHERE o.Id = @Id;
END;
