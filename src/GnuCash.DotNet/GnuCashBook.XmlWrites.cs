using System.Globalization;
using System.Xml.Linq;
using GnuCash.DotNet.Models;
using GnuCash.DotNet.Xml;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Creates an account in a copied XML book and reopens the copy for verification.
    /// </summary>
    public async Task<GnuCashAccountCreateResult> CreateAccountInCopiedBookAsync(
        GnuCashAccountCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Type);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ParentId);

        var workingPath = ResolveWorkingPath(request.WorkingBookPath, "account-write");
        File.Copy(BookPath, workingPath, overwrite: false);
        var before = await ListAccountsAsync(cancellationToken).ConfigureAwait(false);
        var accountId = GnuCashXmlBookDocument.GuidText();
        var xml = GnuCashXmlBookDocument.Load(workingPath);
        xml.Book.Add(CreateAccountElement(accountId, request));
        xml.Save(workingPath);

        var reopened = await client.OpenBookAsync(workingPath, cancellationToken).ConfigureAwait(false);
        var after = await reopened.ListAccountsAsync(cancellationToken).ConfigureAwait(false);
        var found = after.Any(account => MatchesExact(account.Id, accountId));

        return new GnuCashAccountCreateResult(
            found,
            BookPath,
            workingPath,
            accountId,
            before.Count,
            after.Count,
            found,
            found ? "The copied XML book contains the created account." : "The account was not found after reopen.");
    }

    /// <summary>
    /// Creates a price in a copied XML book and reopens the copy for verification.
    /// </summary>
    public async Task<GnuCashPriceCreateResult> CreatePriceInCopiedBookAsync(
        GnuCashPriceCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CommoditySpace);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CommodityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CurrencySpace);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CurrencyId);

        var workingPath = ResolveWorkingPath(request.WorkingBookPath, "price-write");
        File.Copy(BookPath, workingPath, overwrite: false);
        var before = await ListPricesAsync(cancellationToken).ConfigureAwait(false);
        var priceId = GnuCashXmlBookDocument.GuidText();
        var xml = GnuCashXmlBookDocument.Load(workingPath);
        EnsurePriceDatabase(xml).Add(CreatePriceElement(priceId, request));
        xml.Save(workingPath);

        var reopened = await client.OpenBookAsync(workingPath, cancellationToken).ConfigureAwait(false);
        var after = await reopened.ListPricesAsync(cancellationToken).ConfigureAwait(false);
        var found = after.Any(price => MatchesExact(price.Id, priceId));

        return new GnuCashPriceCreateResult(
            found,
            BookPath,
            workingPath,
            priceId,
            before.Count,
            after.Count,
            found,
            found ? "The copied XML book contains the created price." : "The price was not found after reopen.");
    }

    /// <summary>
    /// Marks split reconciliation state in a copied XML book.
    /// </summary>
    public Task<GnuCashReconciliationWriteResult> MarkSplitsReconciledInCopiedBookAsync(
        GnuCashReconciliationWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SplitIds is null || request.SplitIds.Count == 0)
        {
            throw new ArgumentException("At least one split id is required.", nameof(request));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(request.ReconciledState);
        cancellationToken.ThrowIfCancellationRequested();

        var requested = request.SplitIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var workingPath = ResolveWorkingPath(request.WorkingBookPath, "reconcile-write");
        File.Copy(BookPath, workingPath, overwrite: false);
        var xml = GnuCashXmlBookDocument.Load(workingPath);
        var updated = 0;

        foreach (var split in xml.Document.Descendants().Where(element => GnuCashXmlBookDocument.HasLocalName(element, "split")))
        {
            var splitId = GnuCashXmlBookDocument.ChildText(split, "id");
            if (splitId is null || !requested.Contains(splitId))
            {
                continue;
            }

            SetChild(split, GnuCashXmlBookDocument.Split + "reconciled-state", request.ReconciledState);
            if (request.ReconciledAt is not null)
            {
                SetTimestampChild(split, GnuCashXmlBookDocument.Split + "reconcile-date", request.ReconciledAt.Value);
            }

            updated++;
        }

        xml.Save(workingPath);
        var existingSplitIds = xml.Document
            .Descendants()
            .Where(element => GnuCashXmlBookDocument.HasLocalName(element, "split"))
            .Select(split => GnuCashXmlBookDocument.ChildText(split, "id"))
            .Where(id => id is not null)
            .Select(id => id!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = requested
            .Except(existingSplitIds, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult(new GnuCashReconciliationWriteResult(
            updated > 0 && missing.Length == 0,
            BookPath,
            workingPath,
            requested.Count,
            updated,
            missing,
            missing.Length == 0
                ? "The copied XML book contains the requested reconciliation updates."
                : "One or more requested splits were not found in the copied XML book."));
    }

    private static XElement CreateAccountElement(string accountId, GnuCashAccountCreateRequest request)
    {
        var account = new XElement(
            GnuCashXmlBookDocument.Gnc + "account",
            new XAttribute("version", "2.0.0"),
            new XElement(GnuCashXmlBookDocument.Act + "name", request.Name),
            new XElement(GnuCashXmlBookDocument.Act + "id", new XAttribute("type", "guid"), accountId),
            new XElement(GnuCashXmlBookDocument.Act + "type", request.Type),
            new XElement(
                GnuCashXmlBookDocument.Act + "commodity",
                new XElement(GnuCashXmlBookDocument.Cmdty + "space", request.CommoditySpace),
                new XElement(GnuCashXmlBookDocument.Cmdty + "id", request.CommodityId)),
            new XElement(GnuCashXmlBookDocument.Act + "parent", new XAttribute("type", "guid"), request.ParentId));

        AddOptional(account, GnuCashXmlBookDocument.Act + "code", request.Code);
        AddOptional(account, GnuCashXmlBookDocument.Act + "description", request.Description);
        if (request.IsPlaceholder)
        {
            account.Add(CreateBooleanSlot("placeholder", true));
        }

        return account;
    }

    private static XElement CreatePriceElement(string priceId, GnuCashPriceCreateRequest request) =>
        new(
            GnuCashXmlBookDocument.Price + "price",
            new XElement(GnuCashXmlBookDocument.Price + "id", new XAttribute("type", "guid"), priceId),
            new XElement(
                GnuCashXmlBookDocument.Price + "commodity",
                new XElement(GnuCashXmlBookDocument.Cmdty + "space", request.CommoditySpace),
                new XElement(GnuCashXmlBookDocument.Cmdty + "id", request.CommodityId)),
            new XElement(
                GnuCashXmlBookDocument.Price + "currency",
                new XElement(GnuCashXmlBookDocument.Cmdty + "space", request.CurrencySpace),
                new XElement(GnuCashXmlBookDocument.Cmdty + "id", request.CurrencyId)),
            new XElement(
                GnuCashXmlBookDocument.Price + "time",
                new XElement(GnuCashXmlBookDocument.Ts + "date", FormatTimestamp(request.Time))),
            new XElement(GnuCashXmlBookDocument.Price + "source", request.Source),
            new XElement(GnuCashXmlBookDocument.Price + "type", request.Type),
            new XElement(GnuCashXmlBookDocument.Price + "value", request.Value.RawValue));

    private static XElement EnsurePriceDatabase(GnuCashXmlBookDocument xml)
    {
        var priceDb = xml.FirstBookChild("pricedb");
        if (priceDb is not null)
        {
            return priceDb;
        }

        priceDb = new XElement(GnuCashXmlBookDocument.Gnc + "pricedb", new XAttribute("version", "1"));
        xml.Book.AddFirst(priceDb);
        return priceDb;
    }

    private static string ResolveWorkingPath(string? requestedPath, string operation)
    {
        var path = string.IsNullOrWhiteSpace(requestedPath)
            ? Path.Combine(Path.GetTempPath(), "gnucash-dotnet", operation, Guid.NewGuid().ToString("N") + ".gnucash")
            : Path.GetFullPath(requestedPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path))
        {
            throw new IOException("The working book path already exists: " + path);
        }

        return path;
    }

    private static XElement CreateBooleanSlot(string key, bool value) =>
        new(
            GnuCashXmlBookDocument.Act + "slots",
            new XElement(
                GnuCashXmlBookDocument.Slot + "slot",
                new XElement(GnuCashXmlBookDocument.Slot + "key", key),
                new XElement(GnuCashXmlBookDocument.Slot + "value", new XAttribute("type", "string"), value ? "true" : "false")));

    private static void SetChild(XElement parent, XName name, string value)
    {
        var child = parent.Elements().FirstOrDefault(element => element.Name.LocalName == name.LocalName);
        if (child is null)
        {
            parent.Add(new XElement(name, value));
            return;
        }

        child.Value = value;
    }

    private static void SetTimestampChild(XElement parent, XName name, DateTimeOffset value)
    {
        var child = parent.Elements().FirstOrDefault(element => element.Name.LocalName == name.LocalName);
        var date = new XElement(GnuCashXmlBookDocument.Ts + "date", FormatTimestamp(value));
        if (child is null)
        {
            parent.Add(new XElement(name, date));
            return;
        }

        child.ReplaceNodes(date);
    }

    private static void AddOptional(XElement parent, XName name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parent.Add(new XElement(name, value));
        }
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        FormatGnuCashTimestamp(value.ToUniversalTime());

    private static string FormatGnuCashTimestamp(DateTimeOffset value)
    {
        var text = value.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        return string.Concat(text.AsSpan(0, text.Length - 3), text.AsSpan(text.Length - 2));
    }
}
