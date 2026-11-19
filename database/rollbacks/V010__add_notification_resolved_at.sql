-- =============================================================================
-- Rollback:    V010__add_notification_resolved_at
-- Purpose:     Reverses V010: drops Notifications.ResolvedAt. Run only through the migrator
--              (`rollback V010__add_notification_resolved_at.sql`), which also removes V010 from dbo.SchemaVersions so
--              the next run applies it again. Development only.
-- Author:      JulianH2
-- Jira:        ITACR-21
-- =============================================================================

IF COL_LENGTH(N'dbo.Notifications', N'ResolvedAt') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Notifications DROP COLUMN ResolvedAt;
END;
