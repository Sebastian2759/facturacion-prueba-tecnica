using Domain.Base;
using Domain.Contracts.Security;
using Domain.Invoices;

namespace Application.UseCases.Invoices.Common;

internal static class InvoiceAccess
{
    public static string RequireEvaluator(ICurrentUser user)
    {
        if (!user.CanEvaluate || string.IsNullOrWhiteSpace(user.Subject) ||
            user.Subject.Length > InvoiceRules.OwnerMaxLength)
            throw new AccessDeniedException();

        return user.Subject;
    }
}
