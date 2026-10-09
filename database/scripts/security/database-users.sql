-- =============================================================================
-- Script:      database-users
-- Purpose:     Creates the two contained users of the itacr-sql database, so neither the migrator nor
--              the API uses the server admin login. Run it once, by hand, as the server admin, in the
--              Azure Portal Query editor of the itacr-sql database (not master). Never run it automatically.
-- Users:       itacr_migrator - DDL plus reads and writes; used only by Itacr.Database.Migrator
--              itacr_app      - EXECUTE on dbo; used by the API (App Service ConnectionStrings__Default)
-- Placeholders: replace both <...> values with generated passwords before running it, keep them only in a
--              password manager, and never commit or paste the real values anywhere else.
-- Author:      JulianH2
-- Jira:        ITACR-37
-- =============================================================================

CREATE USER itacr_migrator WITH PASSWORD = N'<itacr_migrator password>';
ALTER ROLE db_ddladmin ADD MEMBER itacr_migrator;
ALTER ROLE db_datareader ADD MEMBER itacr_migrator;
ALTER ROLE db_datawriter ADD MEMBER itacr_migrator;

CREATE USER itacr_app WITH PASSWORD = N'<itacr_app password>';
GRANT EXECUTE ON SCHEMA::dbo TO itacr_app;

-- Temporary: the API released before ITACR-36 reads and writes the tables through EF Core. After the release
-- that includes ITACR-36 is deployed, run app-execute-only.sql to remove both roles.
ALTER ROLE db_datareader ADD MEMBER itacr_app;
ALTER ROLE db_datawriter ADD MEMBER itacr_app;
