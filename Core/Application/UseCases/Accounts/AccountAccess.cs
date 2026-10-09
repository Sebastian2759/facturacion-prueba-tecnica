using Domain.Base;
using Domain.Contracts.Accounts;
using Domain.Contracts.Security;

namespace Application.UseCases.Accounts;

internal static class AccountAccess
{
    public static AccountExecutionMode Authorize(string mode, ICurrentUser currentUser)
    {
        if (!currentUser.CanEvaluate || string.IsNullOrWhiteSpace(currentUser.Subject))
            throw new AccessDeniedException();

        var executionMode = mode switch
        {
            "simulated" => AccountExecutionMode.Simulated,
            "real" => AccountExecutionMode.Real,
            _ => throw new AccountsIntegrationException("ACCOUNTS_MODE_INVALID", 400, "El modo debe ser simulated o real."),
        };

        if (executionMode == AccountExecutionMode.Real && !currentUser.CanUseRealAccounts)
            throw new AccessDeniedException();

        return executionMode;
    }
}
