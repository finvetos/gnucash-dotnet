using GnuCash.DotNet.Models;
using System.Numerics;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    private static bool Matches(GnuCashAccount account, GnuCashAccountQuery? query)
    {
        if (query is null)
        {
            return true;
        }

        return MatchesExact(account.Id, query.Id) &&
               Contains(account.Name, query.NameContains) &&
               MatchesExact(account.Type, query.Type) &&
               MatchesExact(account.ParentId, query.ParentId) &&
               MatchesExact(account.CommoditySpace, query.CommoditySpace) &&
               MatchesExact(account.CommodityId, query.CommodityId);
    }

    private static bool Matches(GnuCashTransaction transaction, GnuCashTransactionQuery? query)
    {
        if (query is null)
        {
            return true;
        }

        if (!MatchesExact(transaction.Number, query.Number) ||
            !Contains(transaction.Description, query.DescriptionContains) ||
            !MatchesExact(transaction.CurrencySpace, query.CurrencySpace) ||
            !MatchesExact(transaction.CurrencyId, query.CurrencyId))
        {
            return false;
        }

        if (query.PostedFrom is not null &&
            (transaction.PostedAt is null || transaction.PostedAt < query.PostedFrom))
        {
            return false;
        }

        if (query.PostedTo is not null &&
            (transaction.PostedAt is null || transaction.PostedAt > query.PostedTo))
        {
            return false;
        }

        var hasSplitLevelFilter = !string.IsNullOrWhiteSpace(query.AccountId) ||
                                  !string.IsNullOrWhiteSpace(query.ReconciledState);

        return !hasSplitLevelFilter || transaction.Splits.Any(split => Matches(split, query));
    }

    private static bool Matches(GnuCashPrice price, GnuCashPriceQuery? query)
    {
        if (query is null)
        {
            return true;
        }

        if (!MatchesExact(price.CommoditySpace, query.CommoditySpace) ||
            !MatchesExact(price.CommodityId, query.CommodityId) ||
            !MatchesExact(price.CurrencySpace, query.CurrencySpace) ||
            !MatchesExact(price.CurrencyId, query.CurrencyId) ||
            !MatchesExact(price.Source, query.Source) ||
            !MatchesExact(price.Type, query.Type))
        {
            return false;
        }

        if (query.From is not null &&
            (price.Time is null || price.Time < query.From))
        {
            return false;
        }

        return query.To is null ||
               (price.Time is not null && price.Time <= query.To);
    }

    private static bool Matches(GnuCashSplit split, GnuCashTransactionQuery? query)
    {
        if (query is null)
        {
            return true;
        }

        return MatchesExact(split.AccountId, query.AccountId) &&
               MatchesExact(split.ReconciledState, query.ReconciledState);
    }

    private static GnuCashAccountBalance CreateBalance(
        GnuCashAccount account,
        IReadOnlyList<GnuCashTransaction> transactions,
        GnuCashTransactionQuery? query)
    {
        var splits = transactions
            .SelectMany(transaction => transaction.Splits)
            .Where(split => MatchesExact(split.AccountId, account.Id))
            .Where(split => MatchesExact(split.ReconciledState, query?.ReconciledState))
            .ToArray();

        return new GnuCashAccountBalance(
            account.Id,
            account.Name,
            account.CommoditySpace,
            account.CommodityId,
            Sum(splits.Select(split => split.Value)),
            splits.Length);
    }

    private static GnuCashAmount Sum(IEnumerable<GnuCashAmount> amounts)
    {
        var numerator = 0L;
        var denominator = 1L;

        foreach (var amount in amounts)
        {
            if (amount.Numerator is null || amount.Denominator is null or 0)
            {
                throw new InvalidOperationException(
                    $"Cannot calculate a balance because amount '{amount.RawValue}' is not a rational GnuCash value.");
            }

            (numerator, denominator) = Add(
                numerator,
                denominator,
                amount.Numerator.Value,
                amount.Denominator.Value);
        }

        return new GnuCashAmount($"{numerator}/{denominator}", numerator, denominator);
    }

    private static GnuCashAmount Subtract(GnuCashAmount left, GnuCashAmount right) =>
        Sum([left, Negate(right)]);

    private static GnuCashAmount Negate(GnuCashAmount amount)
    {
        if (amount.Numerator is null || amount.Denominator is null or 0)
        {
            throw new InvalidOperationException(
                $"Cannot negate amount '{amount.RawValue}' because it is not a rational GnuCash value.");
        }

        var numerator = checked(-amount.Numerator.Value);
        return new GnuCashAmount($"{numerator}/{amount.Denominator}", numerator, amount.Denominator);
    }

    private static bool IsZero(GnuCashAmount amount) =>
        amount.Numerator == 0 &&
        amount.Denominator is not null and not 0;

    private static bool AreEquivalentAmounts(GnuCashAmount? left, GnuCashAmount? right)
    {
        if (left?.Numerator is null ||
            left.Denominator is null or 0 ||
            right?.Numerator is null ||
            right.Denominator is null or 0)
        {
            return false;
        }

        return new BigInteger(left.Numerator.Value) * right.Denominator.Value ==
               new BigInteger(right.Numerator.Value) * left.Denominator.Value;
    }

    private static (long Numerator, long Denominator) Add(
        long leftNumerator,
        long leftDenominator,
        long rightNumerator,
        long rightDenominator)
    {
        if (leftDenominator < 0)
        {
            leftNumerator = -leftNumerator;
            leftDenominator = -leftDenominator;
        }

        if (rightDenominator < 0)
        {
            rightNumerator = -rightNumerator;
            rightDenominator = -rightDenominator;
        }

        var denominator = checked(leftDenominator / GreatestCommonDivisor(leftDenominator, rightDenominator) * rightDenominator);
        var numerator = checked(
            leftNumerator * (denominator / leftDenominator) +
            rightNumerator * (denominator / rightDenominator));

        return (numerator, denominator);
    }

    private static long GreatestCommonDivisor(long left, long right)
    {
        while (right != 0)
        {
            var remainder = left % right;
            left = right;
            right = remainder;
        }

        return left == 0 ? 1 : Math.Abs(left);
    }

    private static bool MatchesExact(string? actual, string? expected) =>
        string.IsNullOrWhiteSpace(expected) ||
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string? actual, string? expected) =>
        string.IsNullOrWhiteSpace(expected) ||
        actual?.Contains(expected, StringComparison.OrdinalIgnoreCase) == true;
}
