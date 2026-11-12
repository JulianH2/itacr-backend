-- =============================================================================
-- Object:      dbo.usp_Manager_GetOrders
-- Type:        stored procedure
-- Purpose:     The table of recent orders of the manager's dashboard (HU_16): the orders created in a date range, newest
--              first, filtered by channel and by status, one page at a time. The channel is Waiter (a table or to take
--              away) or Pickup (the customer's phone); null means both. The preparation time is the time between the
--              creation and the moment the order became Ready; it is null when the order is not Ready.
-- Parameters:  @From datetime2 - start of the range, inclusive (UTC)
--              @To datetime2 - end of the range, exclusive (UTC)
--              @Channel nvarchar(10) - Waiter or Pickup; null for every channel
--              @Status nvarchar(20) - New, InPreparation, Ready, or Delivered; null for every status
--              @Skip int - rows to skip
--              @Take int - rows to return
-- Returns:     Two result sets. First one row: TotalCount, the orders that match the filters (not only the page).
--              Then 0..@Take rows: Id, OrderNumber, Channel, Status, TableNumber (null without a table), Total, CreatedAt,
--              ReadyAt, DeliveredAt, PreparationSeconds
-- Author:      GuillermoRH19
-- Jira:        ITACR-22
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Manager_GetOrders
    @From datetime2,
    @To datetime2,
    @Channel nvarchar(10),
    @Status nvarchar(20),
    @Skip int,
    @Take int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS TotalCount
    FROM dbo.Orders AS o
    WHERE o.CreatedAt >= @From AND o.CreatedAt < @To
        AND (@Status IS NULL OR o.Status = @Status)
        AND (@Channel IS NULL
            OR (@Channel = N'Pickup' AND o.Channel = N'MobileApp')
            OR (@Channel = N'Waiter' AND o.Channel <> N'MobileApp'));

    SELECT o.Id, o.OrderNumber, o.Channel, o.Status, t.Number AS TableNumber, o.Total, o.CreatedAt, o.ReadyAt, o.DeliveredAt,
        CASE WHEN o.ReadyAt >= o.CreatedAt THEN CAST(DATEDIFF_BIG(SECOND, o.CreatedAt, o.ReadyAt) AS int) END AS PreparationSeconds
    FROM dbo.Orders AS o
    LEFT JOIN dbo.DiningTables AS t ON t.Id = o.TableId
    WHERE o.CreatedAt >= @From AND o.CreatedAt < @To
        AND (@Status IS NULL OR o.Status = @Status)
        AND (@Channel IS NULL
            OR (@Channel = N'Pickup' AND o.Channel = N'MobileApp')
            OR (@Channel = N'Waiter' AND o.Channel <> N'MobileApp'))
    ORDER BY o.CreatedAt DESC, o.OrderNumber DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
