-- =============================================================================
-- Rollback:    V006__add_push_subscription_endpoint_index
-- Purpose:     Reverses V006: drops the unique index on PushSubscriptions.Endpoint. Run only through the migrator
--              (`rollback V006__add_push_subscription_endpoint_index.sql`), which also removes V006 from
--              dbo.SchemaVersions so the next run applies it again. Development only.
-- Author:      JulianH2
-- Jira:        ITACR-15
-- =============================================================================

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PushSubscriptions_Endpoint' AND object_id = OBJECT_ID(N'dbo.PushSubscriptions'))
BEGIN
    DROP INDEX [UX_PushSubscriptions_Endpoint] ON [dbo].[PushSubscriptions];
END;
