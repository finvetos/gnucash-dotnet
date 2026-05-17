using GnuCash.DotNet.Models;
using GnuCash.DotNet.Services;

namespace GnuCash.DotNet;

/// <summary>
/// Represents an opened GnuCash book and provides read operations through the bridge.
/// </summary>
public sealed partial class GnuCashBook
{
    private readonly GnuCashClient client;

    internal GnuCashBook(GnuCashClient client, GnuCashBookInfo info)
    {
        this.client = client;
        Info = info;
    }

    /// <summary>
    /// Book metadata captured when the book was opened.
    /// </summary>
    public GnuCashBookInfo Info { get; }

    /// <summary>
    /// Full path to the opened book file.
    /// </summary>
    public string BookPath => Info.BookPath;

    /// <summary>
    /// Lists commodities and currencies defined in the book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashCommodity>> ListCommoditiesAsync(
        CancellationToken cancellationToken = default) =>
        client.ListCommoditiesAsync(BookPath, cancellationToken);

    /// <summary>
    /// Lists accounts defined in the book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashAccount>> ListAccountsAsync(
        CancellationToken cancellationToken = default) =>
        client.ListAccountsAsync(BookPath, cancellationToken);

    /// <summary>
    /// Lists transactions and splits defined in the book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashTransaction>> ListTransactionsAsync(
        CancellationToken cancellationToken = default) =>
        client.ListTransactionsAsync(BookPath, cancellationToken);
}
