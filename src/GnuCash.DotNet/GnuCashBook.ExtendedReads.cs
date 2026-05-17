using GnuCash.DotNet.Models;
using GnuCash.DotNet.Xml;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Lists lots and cost-basis containers from an XML book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashLot>> ListLotsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xml = GnuCashXmlBookDocument.Load(BookPath);
        return Task.FromResult<IReadOnlyList<GnuCashLot>>(xml.BookChildren("lot").Select(ReadLot).ToArray());
    }

    /// <summary>
    /// Lists vendor business objects from an XML book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashVendor>> ListVendorsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xml = GnuCashXmlBookDocument.Load(BookPath);
        return Task.FromResult<IReadOnlyList<GnuCashVendor>>(xml.BookChildren("GncVendor").Select(ReadVendor).ToArray());
    }

    /// <summary>
    /// Lists invoices and bills from an XML book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashBusinessDocument>> ListBusinessDocumentsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xml = GnuCashXmlBookDocument.Load(BookPath);
        return Task.FromResult<IReadOnlyList<GnuCashBusinessDocument>>(xml.BookChildren("GncInvoice").Select(ReadBusinessDocument).ToArray());
    }

    /// <summary>
    /// Lists bill terms from an XML book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashBillTerm>> ListBillTermsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xml = GnuCashXmlBookDocument.Load(BookPath);
        return Task.FromResult<IReadOnlyList<GnuCashBillTerm>>(xml.BookChildren("GncBillTerm").Select(ReadBillTerm).ToArray());
    }

    /// <summary>
    /// Lists tax tables from an XML book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashTaxTable>> ListTaxTablesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xml = GnuCashXmlBookDocument.Load(BookPath);
        return Task.FromResult<IReadOnlyList<GnuCashTaxTable>>(xml.BookChildren("GncTaxTable").Select(ReadTaxTable).ToArray());
    }

    /// <summary>
    /// Lists scheduled transactions from an XML book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashScheduledTransaction>> ListScheduledTransactionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xml = GnuCashXmlBookDocument.Load(BookPath);
        return Task.FromResult<IReadOnlyList<GnuCashScheduledTransaction>>(xml.BookChildren("schedxaction").Select(ReadScheduledTransaction).ToArray());
    }

    /// <summary>
    /// Lists budgets from an XML book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashBudget>> ListBudgetsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xml = GnuCashXmlBookDocument.Load(BookPath);
        return Task.FromResult<IReadOnlyList<GnuCashBudget>>(xml.BookChildren("budget").Select(ReadBudget).ToArray());
    }

    /// <summary>
    /// Lists slot metadata values from book, account, transaction, split, and business objects.
    /// </summary>
    public Task<IReadOnlyList<GnuCashSlotValue>> ListSlotsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xml = GnuCashXmlBookDocument.Load(BookPath);
        var slots = xml.Book
            .Descendants()
            .Where(element => GnuCashXmlBookDocument.HasLocalName(element, "slots"))
            .SelectMany(ReadSlots)
            .ToArray();

        return Task.FromResult<IReadOnlyList<GnuCashSlotValue>>(slots);
    }

    private static GnuCashLot ReadLot(System.Xml.Linq.XElement lot) =>
        new(
            ReadIdentity(lot),
            ReadAnyText(lot, "title"),
            ReadAnyText(lot, "notes"),
            lot.Descendants().Count(element => GnuCashXmlBookDocument.HasLocalName(element, "split")));

    private static GnuCashVendor ReadVendor(System.Xml.Linq.XElement vendor)
    {
        var currency = FirstDescendant(vendor, "currency");
        return new GnuCashVendor(
            ReadIdentity(vendor),
            ReadAnyText(vendor, "id"),
            ReadAnyText(vendor, "name"),
            ReadAnyText(currency, "space"),
            ReadAnyText(currency, "id"));
    }

    private static GnuCashBusinessDocument ReadBusinessDocument(System.Xml.Linq.XElement invoice)
    {
        var currency = FirstDescendant(invoice, "currency");
        return new GnuCashBusinessDocument(
            ReadIdentity(invoice),
            ReadAnyText(invoice, "id"),
            invoice.Name.LocalName,
            ReadAnyText(FirstDescendant(invoice, "owner"), "id"),
            ReadAnyText(currency, "space"),
            ReadAnyText(currency, "id"),
            ReadTime(invoice, "opened"),
            ReadTime(invoice, "posted"),
            invoice.Descendants().Count(element => GnuCashXmlBookDocument.HasLocalName(element, "entry")));
    }

    private static GnuCashBillTerm ReadBillTerm(System.Xml.Linq.XElement term) =>
        new(ReadIdentity(term), ReadAnyText(term, "name"), ReadAnyText(term, "desc"));

    private static GnuCashTaxTable ReadTaxTable(System.Xml.Linq.XElement table) =>
        new(
            ReadIdentity(table),
            ReadAnyText(table, "name"),
            table.Descendants().Count(element => GnuCashXmlBookDocument.HasLocalName(element, "entry")));

    private static GnuCashScheduledTransaction ReadScheduledTransaction(System.Xml.Linq.XElement scheduled) =>
        new(
            ReadIdentity(scheduled),
            ReadAnyText(scheduled, "name"),
            ReadAnyText(scheduled, "enabled"),
            ReadAnyText(scheduled, "freqspec"));

    private static GnuCashBudget ReadBudget(System.Xml.Linq.XElement budget) =>
        new(
            ReadIdentity(budget),
            ReadAnyText(budget, "name"),
            ReadAnyText(budget, "description"),
            budget.Descendants().Count(element => GnuCashXmlBookDocument.HasLocalName(element, "amount")));

    private static IEnumerable<GnuCashSlotValue> ReadSlots(System.Xml.Linq.XElement slots)
    {
        var ownerId = ReadIdentity(slots.Parent);
        foreach (var slot in slots.Elements().Where(element => GnuCashXmlBookDocument.HasLocalName(element, "slot")))
        {
            var value = GnuCashXmlBookDocument.Child(slot, "value");
            yield return new GnuCashSlotValue(
                ownerId,
                ReadAnyText(slot, "key") ?? string.Empty,
                value?.Attribute("type")?.Value,
                value?.Value.Trim());
        }
    }

    private static string ReadIdentity(System.Xml.Linq.XElement? element) =>
        ReadAnyText(element, "guid") ??
        ReadAnyText(element, "id") ??
        ReadAnyText(element, "name") ??
        string.Empty;

    private static System.Xml.Linq.XElement? FirstDescendant(System.Xml.Linq.XElement? element, string localName) =>
        element?.Descendants().FirstOrDefault(candidate => GnuCashXmlBookDocument.HasLocalName(candidate, localName));

    private static string? ReadAnyText(System.Xml.Linq.XElement? element, string localName) =>
        element?.Elements().FirstOrDefault(candidate => GnuCashXmlBookDocument.HasLocalName(candidate, localName))?.Value.Trim() ??
        element?.Descendants().FirstOrDefault(candidate => GnuCashXmlBookDocument.HasLocalName(candidate, localName))?.Value.Trim();

    private static DateTimeOffset? ReadTime(System.Xml.Linq.XElement element, string localName)
    {
        var text = FirstDescendant(
            element.Elements().FirstOrDefault(candidate => GnuCashXmlBookDocument.HasLocalName(candidate, localName)),
            "date")?.Value.Trim();

        return DateTimeOffset.TryParse(text, out var parsed) ? parsed : null;
    }
}
