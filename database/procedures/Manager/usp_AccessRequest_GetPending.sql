-- =============================================================================
-- Object:      dbo.usp_AccessRequest_GetPending
-- Type:        stored procedure
-- Purpose:     Lists what the manager has to attend (HU_15): the accounts asked from the login screen that wait for
--              approval, and the PIN resets asked by active employees that nobody has attended. A PIN reset is one row
--              per employee even though every manager got a notification (and a NULL-recipient one when there was none).
-- Parameters:  none
-- Returns:     Result set 1: Id, FullName, Role, RequestedAt of the PendingApproval accounts, oldest first
--              Result set 2: EmployeeId, FullName, Role, RequestedAt (last request) of the active employees with an
--              unresolved PinReset notification, oldest first
-- Author:      JulianH2
-- Jira:        ITACR-21
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_AccessRequest_GetPending
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.Id, e.FullName, e.Role, e.CreatedAt AS RequestedAt
    FROM dbo.Employees AS e
    WHERE e.Status = N'PendingApproval'
    ORDER BY e.CreatedAt, e.Id;

    SELECT e.Id AS EmployeeId, e.FullName, e.Role, MAX(n.SentAt) AS RequestedAt
    FROM dbo.Notifications AS n
    INNER JOIN dbo.Employees AS e ON e.Id = n.SubjectEmployeeId
    WHERE n.Type = N'PinReset' AND n.ResolvedAt IS NULL AND e.Status = N'Active'
    GROUP BY e.Id, e.FullName, e.Role
    ORDER BY MAX(n.SentAt), e.Id;
END;
