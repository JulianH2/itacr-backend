-- =============================================================================
-- Migration:   V008__add_employee_privacy_notice_date
-- Purpose:     A new employee asks for an account from the login screen and must accept the privacy notice (HU_14). This
--              column keeps when they accepted it, as the proof the data protection law asks for. It is NULL for the
--              employees that existed before, who did not go through that form. It only adds the column when it is
--              missing, so running it again changes nothing.
-- Author:      GuillermoRH19
-- Jira:        ITACR-20
-- =============================================================================

IF COL_LENGTH(N'dbo.Employees', N'PrivacyNoticeAcceptedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD PrivacyNoticeAcceptedAt datetime2 NULL;
END;
