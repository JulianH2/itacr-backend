-- =============================================================================
-- Object:      dbo.usp_Order_GetRecentByWaiter
-- Type:        stored procedure
-- Purpose:     Returns the latest orders a waiter sent since a given time, newest first, so the waiter can open their
--              detail from the dashboard.
-- Parameters:  @WaiterId uniqueidentifier - waiter who took the orders
--              @Since datetime2 - only orders created at or after this time (UTC)
--              @Take int - maximum number of orders
-- Returns:     0..@Take rows: Id, OrderNumber, Channel, Status, TableNumber (null when the order has no table), CreatedAt
-- Author:      JulianH2
-- Jira:        ITACR-11
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Order_GetRecentByWaiter
    @WaiterId uniqueidentifier,
    @Since datetime2,
    @Take int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Take) o.Id, o.OrderNumber, o.Channel, o.Status, t.Number AS TableNumber, o.CreatedAt
    FROM dbo.Orders AS o
    LEFT JOIN dbo.DiningTables AS t ON t.Id = o.TableId
    WHERE o.WaiterId = @WaiterId AND o.CreatedAt >= @Since
    ORDER BY o.CreatedAt DESC, o.OrderNumber DESC;
END;
