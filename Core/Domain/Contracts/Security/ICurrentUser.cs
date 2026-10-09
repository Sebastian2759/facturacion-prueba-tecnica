namespace Domain.Contracts.Security;

public interface ICurrentUser
{
    string Subject { get; }
    bool CanEvaluate { get; }
    bool CanUseRealAccounts { get; }
}
