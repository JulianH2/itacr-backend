-- =============================================================================
-- Object:      dbo.usp_Order_GetActiveByWaiter
-- Type:        stored procedure
-- Purpose:     Returns the orders a waiter took that are still active (New, InPreparation, or Ready), newest first, for
--              the waiter's dashboard. Delivered orders are not active. It never returns the orders of another waiter,
--              and a pickup order (no waiter) is never anyone's.
-- Parameters:  @WaiterId uniqueidentifier - waiter who took the orders
--              @Take int - maximum number of orders
-- Returns:     0..@Take rows: Id, OrderNumber, Channel, Status, TableNumber (null when the order has no table),
--              CreatedAt, LastStatusChangeAt
-- Author:      GuillermoRH19
-- Jira:        ITACR-14
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Order_GetActiveByWaiter
    @WaiterId uniqueidentifier,
    @Take int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Take) o.Id, o.OrderNumber, o.Channel, o.Status, t.Number AS TableNumber, o.CreatedAt, o.LastStatusChangeAt
    FROM dbo.Orders AS o
    LEFT JOIN dbo.DiningTables AS t ON t.Id = o.TableId
    WHERE o.WaiterId = @WaiterId AND o.Status IN (N'New', N'InPreparation', N'Ready')
    ORDER BY o.CreatedAt DESC, o.OrderNumber DESC;
END;
