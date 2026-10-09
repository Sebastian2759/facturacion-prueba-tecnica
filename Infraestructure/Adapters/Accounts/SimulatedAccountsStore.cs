using Domain.Contracts.Accounts;

namespace Adapters.Accounts;

/// <summary>Bounded, isolated, fictional evaluation state. Restarting the API clears user-created accounts.</summary>
public sealed class SimulatedAccountsStore(TimeProvider timeProvider)
{
    private const int MaximumSubjects = 500;
    private const int MaximumAccountsPerSubject = 100;
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);
    private readonly object sync = new();
    private readonly Dictionary<string, SubjectAccounts> subjects = new(StringComparer.Ordinal);

    private static readonly AccountRecord[] Examples =
    [
        new("DEMO001", "CC", "900000001", "Comercial Ejemplo", "Ana", "Ejemplo", "ana@example.invalid", 1,
            "11001", "11", "169", "Calle de ejemplo 1", null, "3000000001", 1, null,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), null),
        new("DEMO002", "NI", "900000002", "Servicios de Muestra", null, null, "contacto@example.invalid", 2,
            "05001", "05", "169", "Calle de ejemplo 2", null, "3000000002", 2, null,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), null),
    ];

    public IReadOnlyList<AccountRecord> Get(string subject, string? code)
    {
        RequireSubject(subject);
        lock (sync)
        {
            RemoveExpiredSubjects();
            var records = Examples.AsEnumerable();
            if (subjects.TryGetValue(subject, out var state))
                records = records.Concat(state.Accounts.Values);
            return records.Where(account => code is null || account.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                .OrderBy(account => account.Code, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    public void Create(string subject, AccountRecord account)
    {
        RequireSubject(subject);
        lock (sync)
        {
            RemoveExpiredSubjects();
            if (!subjects.TryGetValue(subject, out var state))
            {
                if (subjects.Count >= MaximumSubjects)
                    throw new AccountsIntegrationException("ACCOUNTS_SIMULATION_CAPACITY", 429, "La simulación alcanzó su capacidad temporal.");
                state = new(timeProvider.GetUtcNow());
                subjects.Add(subject, state);
            }

            if (Examples.Any(example => example.Code.Equals(account.Code, StringComparison.OrdinalIgnoreCase)) || state.Accounts.ContainsKey(account.Code))
                throw new AccountsIntegrationException("ACCOUNT_CODE_DUPLICATE", 409, "Ya existe una cuenta con ese código en su simulación.");
            if (state.Accounts.Count >= MaximumAccountsPerSubject)
                throw new AccountsIntegrationException("ACCOUNTS_SIMULATION_CAPACITY", 429, "Su simulación admite hasta 100 cuentas creadas.");

            state.Accounts.Add(account.Code, account);
        }
    }

    private void RemoveExpiredSubjects()
    {
        var cutoff = timeProvider.GetUtcNow() - Retention;
        foreach (var subject in subjects.Where(item => item.Value.CreatedAt <= cutoff).Select(item => item.Key).ToArray())
            subjects.Remove(subject);
    }

    private static void RequireSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new AccountsIntegrationException("ACCOUNTS_IDENTITY_REQUIRED", 403, "Se requiere una identidad de evaluación.");
    }

    private sealed class SubjectAccounts(DateTimeOffset createdAt)
    {
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public Dictionary<string, AccountRecord> Accounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
