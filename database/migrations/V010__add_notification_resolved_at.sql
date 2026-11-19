-- =============================================================================
-- Migration:   V010__add_notification_resolved_at
-- Purpose:     A PIN reset request (HU_01) is one PinReset notification per active manager. When a manager assigns the
--              temporary PIN (HU_15) the request is handled for all of them, which ReadAt cannot say: it records that
--              one recipient opened the notice, not that the request was attended. ResolvedAt is the time a manager
--              assigned the PIN; a PinReset notification without it is a pending request. It is NULL for every
--              notification that exists now, so the PIN resets already asked stay pending. It only adds the column when
--              it is missing, so running it again changes nothing.
-- Author:      JulianH2
-- Jira:        ITACR-21
-- =============================================================================

IF COL_LENGTH(N'dbo.Notifications', N'ResolvedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Notifications ADD ResolvedAt datetime2 NULL;
END;
