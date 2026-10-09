-- =============================================================================
-- Script:      app-execute-only
-- Purpose:     Leaves itacr_app with EXECUTE on dbo only, now that the API reaches every table through a stored
--              procedure (ITACR-36). Run it once, by hand, as the server admin, in the Azure Portal Query editor of
--              the itacr-sql database, only after the release that includes ITACR-36 is deployed: the previous API
--              still reads and writes the tables directly. Not applied by the migrator, because changing role
--              membership needs more than the migrator's db_ddladmin.
-- Author:      JulianH2
-- Jira:        ITACR-36
-- =============================================================================

ALTER ROLE db_datareader DROP MEMBER itacr_app;
ALTER ROLE db_datawriter DROP MEMBER itacr_app;

-- Expected: one row, EXECUTE on SCHEMA dbo, and no role membership.
SELECT p.permission_name, p.class_desc, SCHEMA_NAME(p.major_id) AS schema_name
FROM sys.database_permissions AS p
WHERE p.grantee_principal_id = USER_ID(N'itacr_app') AND p.permission_name <> N'CONNECT';

SELECT r.name AS role_name
FROM sys.database_role_members AS m
JOIN sys.database_principals AS r ON r.principal_id = m.role_principal_id
WHERE m.member_principal_id = USER_ID(N'itacr_app');
