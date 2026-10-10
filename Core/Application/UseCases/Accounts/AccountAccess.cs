using Domain.Base;
using Domain.Contracts.Security;

namespace Application.UseCases.Accounts;

internal static class AccountAccess
{
    public static void Authorize(ICurrentUser currentUser)
    {
        if (!currentUser.CanEvaluate || !currentUser.CanUseRealAccounts || string.IsNullOrWhiteSpace(currentUser.Subject))
            throw new AccessDeniedException();
    }
}
