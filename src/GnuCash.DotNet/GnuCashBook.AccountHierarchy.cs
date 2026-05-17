using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Lists display paths for accounts in the book.
    /// </summary>
    public async Task<IReadOnlyList<GnuCashAccountPath>> ListAccountPathsAsync(
        GnuCashAccountQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var accounts = await ListAccountsAsync(cancellationToken).ConfigureAwait(false);
        var accountsById = accounts.ToDictionary(account => account.Id, StringComparer.OrdinalIgnoreCase);

        return accounts
            .Where(account => Matches(account, query))
            .Select(account => CreateAccountPath(account, accountsById))
            .ToArray();
    }

    /// <summary>
    /// Finds the display path for a single account.
    /// </summary>
    public async Task<GnuCashAccountPath?> GetAccountPathAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var paths = await ListAccountPathsAsync(
            new GnuCashAccountQuery(Id: accountId),
            cancellationToken).ConfigureAwait(false);

        return paths.SingleOrDefault();
    }

    private static GnuCashAccountPath CreateAccountPath(
        GnuCashAccount account,
        IReadOnlyDictionary<string, GnuCashAccount> accountsById)
    {
        var ids = new Stack<string>();
        var names = new Stack<string>();
        var current = account;

        while (true)
        {
            ids.Push(current.Id);
            names.Push(current.Name);
            if (string.IsNullOrWhiteSpace(current.ParentId) ||
                !accountsById.TryGetValue(current.ParentId, out var parent))
            {
                break;
            }

            current = parent;
        }

        var accountIds = ids.ToArray();
        var accountNames = names.ToArray();
        return new GnuCashAccountPath(
            account.Id,
            string.Join(":", accountNames),
            accountIds,
            accountNames);
    }
}
