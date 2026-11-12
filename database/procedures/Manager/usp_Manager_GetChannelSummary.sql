-- =============================================================================
-- Object:      dbo.usp_Manager_GetChannelSummary
-- Type:        stored procedure
-- Purpose:     The numbers of the manager's dashboard for each channel of a date range (HU_16), computed from the orders:
--              nothing is typed by hand and there is no table of metrics. The channels are two: Waiter (the orders a waiter
--              took, for a table or to take away) and Pickup (the orders a customer sent from the phone). Orders counts the
--              orders created in the range. The preparation time of an order is the time between its creation and the
--              moment it became Ready; an order that is not Ready yet (or went back from Ready) has none, and so does a
--              negative one (a device clock ahead of the server), so neither counts in the average. The sales are the
--              total of the orders delivered in the range, by the time they were delivered.
-- Parameters:  @From datetime2 - start of the range, inclusive (UTC)
--              @To datetime2 - end of the range, exclusive (UTC)
-- Returns:     Always two rows, Waiter first: ChannelGroup, Orders, PreparedOrders, AveragePreparationSeconds (null when no
--              order was prepared), DeliveredOrders, Sales
-- Author:      GuillermoRH19
-- Jira:        ITACR-22
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Manager_GetChannelSummary
    @From datetime2,
    @To datetime2
AS
BEGIN
    SET NOCOUNT ON;

    WITH Created AS (
        SELECT
            CASE WHEN o.Channel = N'MobileApp' THEN N'Pickup' ELSE N'Waiter' END AS ChannelGroup,
            COUNT(*) AS Orders,
            COUNT(CASE WHEN o.ReadyAt >= o.CreatedAt THEN 1 END) AS PreparedOrders,
            AVG(CASE WHEN o.ReadyAt >= o.CreatedAt THEN CAST(DATEDIFF_BIG(SECOND, o.CreatedAt, o.ReadyAt) AS float) END) AS AveragePreparationSeconds
        FROM dbo.Orders AS o
        WHERE o.CreatedAt >= @From AND o.CreatedAt < @To
        GROUP BY CASE WHEN o.Channel = N'MobileApp' THEN N'Pickup' ELSE N'Waiter' END
    ),
    Delivered AS (
        SELECT
            CASE WHEN o.Channel = N'MobileApp' THEN N'Pickup' ELSE N'Waiter' END AS ChannelGroup,
            COUNT(*) AS DeliveredOrders,
            SUM(o.Total) AS Sales
        FROM dbo.Orders AS o
        WHERE o.Status = N'Delivered' AND o.DeliveredAt >= @From AND o.DeliveredAt < @To
        GROUP BY CASE WHEN o.Channel = N'MobileApp' THEN N'Pickup' ELSE N'Waiter' END
    )
    SELECT
        g.ChannelGroup,
        ISNULL(c.Orders, 0) AS Orders,
        ISNULL(c.PreparedOrders, 0) AS PreparedOrders,
        c.AveragePreparationSeconds,
        ISNULL(d.DeliveredOrders, 0) AS DeliveredOrders,
        CAST(ISNULL(d.Sales, 0) AS decimal(12, 2)) AS Sales
    FROM (VALUES (N'Waiter', 1), (N'Pickup', 2)) AS g (ChannelGroup, SortOrder)
    LEFT JOIN Created AS c ON c.ChannelGroup = g.ChannelGroup
    LEFT JOIN Delivered AS d ON d.ChannelGroup = g.ChannelGroup
    ORDER BY g.SortOrder;
END;
