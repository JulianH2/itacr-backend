namespace Itacr.Application.Manager.GetAccessRequests;

/// <summary>The section of requests of the manager's dashboard (HU_15): the accounts to approve and the PIN resets to answer.</summary>
public sealed class GetAccessRequestsHandler(IStaffAccessRepository staffAccessRepository)
{
    public async Task<AccessRequestsResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var pending = await staffAccessRepository.GetPendingAsync(cancellationToken);
        return new AccessRequestsResponse(
            [.. pending.Accounts.Select(account => new PendingAccountResponse(account.Id, account.FullName, account.Role, account.RequestedAt))],
            [.. pending.PinResets.Select(reset => new PendingPinResetResponse(reset.EmployeeId, reset.FullName, reset.Role, reset.RequestedAt))]);
    }
}
