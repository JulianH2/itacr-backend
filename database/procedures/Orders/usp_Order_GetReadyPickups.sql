-- =============================================================================
-- Object:      dbo.usp_Order_GetReadyPickups
-- Type:        stored procedure
-- Purpose:     Returns the pickup orders of customers (no waiter) that are Ready, the ones that wait for the customer to
--              arrive, oldest first, so a waiter can hand one over and mark it as delivered.
-- Parameters:  @Take int - maximum number of orders
-- Returns:     0..@Take rows: Id, OrderNumber, Channel, Status, TableNumber (always null: a pickup order has no
--              table), CreatedAt, LastStatusChangeAt
-- Author:      GuillermoRH19
-- Jira:        ITACR-16
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Order_GetReadyPickups
    @Take int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Take) o.Id, o.OrderNumber, o.Channel, o.Status, t.Number AS TableNumber, o.CreatedAt, o.LastStatusChangeAt
    FROM dbo.Orders AS o
    LEFT JOIN dbo.DiningTables AS t ON t.Id = o.TableId
    WHERE o.WaiterId IS NULL AND o.Status = N'Ready'
    ORDER BY o.LastStatusChangeAt, o.OrderNumber;
END;
