-- =============================================================================
-- Rollback:    V008__add_employee_privacy_notice_date
-- Purpose:     Reverses V008: drops Employees.PrivacyNoticeAcceptedAt. Run only through the migrator
--              (`rollback V008__add_employee_privacy_notice_date.sql`), which also removes V008 from dbo.SchemaVersions so
--              the next run applies it again. Development only.
-- Author:      GuillermoRH19
-- Jira:        ITACR-20
-- =============================================================================

IF COL_LENGTH(N'dbo.Employees', N'PrivacyNoticeAcceptedAt') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Employees DROP COLUMN PrivacyNoticeAcceptedAt;
END;
