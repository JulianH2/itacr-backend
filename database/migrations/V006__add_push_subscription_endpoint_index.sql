-- =============================================================================
-- Migration:   V006__add_push_subscription_endpoint_index
-- Purpose:     A browser push subscription is identified by its endpoint (HU_09). The unique index lets the API save the
--              same subscription again (a new login on the same phone, or the customer asking again) without a duplicate,
--              and find it to delete it when the push service answers that it expired. It stops before changing anything
--              if two rows already share an endpoint.
-- Author:      JulianH2
-- Jira:        ITACR-15
-- =============================================================================

IF EXISTS (SELECT 1 FROM dbo.PushSubscriptions GROUP BY Endpoint HAVING COUNT(*) > 1)
BEGIN
    THROW 50004, N'V006 stopped: two push subscriptions share an endpoint. Delete the duplicates from dbo.PushSubscriptions before running it.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PushSubscriptions_Endpoint' AND object_id = OBJECT_ID(N'dbo.PushSubscriptions'))
BEGIN
    CREATE UNIQUE INDEX [UX_PushSubscriptions_Endpoint] ON [dbo].[PushSubscriptions] ([Endpoint]);
END;
