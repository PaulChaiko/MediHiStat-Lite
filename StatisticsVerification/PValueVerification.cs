using System.Globalization;
using MediHiStat;

internal static class PValueVerification
{
    public static void Run()
    {
        // Independent references: Python math.erfc(z / sqrt(2)); Mann–Whitney z
        // uses the tie-corrected rank variance and a continuity correction of 0.5.
        Relative(0.31731050786291415, StatisticalMath.NormalTwoSidedTail(1), "Normal tail z=1");
        Relative(0.08326451666355043, StatisticalMath.NormalTwoSidedTail(Math.Sqrt(3)), "Normal tail at gamma branch boundary");
        Relative(0.05000000000000004, StatisticalMath.NormalTwoSidedTail(1.959963984540054), "Normal tail near p=.05");
        Relative(1.1445972126542467e-18, StatisticalMath.NormalTwoSidedTail(8.82), "Normal tiny tail");
        Relative(5.507248237212663e-89, StatisticalMath.NormalTwoSidedTail(20), "Normal very small tail");
        Relative(StatisticalMath.NormalTwoSidedTail(8.82), StatisticalMath.NormalTwoSidedTail(-8.82), "Two-sided symmetry");
        Check(StatisticalMath.NormalTwoSidedTail(0) == 1, "Normal tail at zero must equal one.");
        Check(double.IsNaN(StatisticalMath.NormalTwoSidedTail(double.NaN)), "NaN must not become a valid tail probability.");
        foreach (double extreme in new[] { double.PositiveInfinity, double.NegativeInfinity, double.MaxValue, -double.MaxValue })
            Check(StatisticalMath.NormalTwoSidedTail(extreme) == 0, "Infinite/overflowing normal distance must yield a zero representable tail.");

        double[] first = Enumerable.Range(0, 50).Select(value => (double)value).ToArray();
        double[] second = Enumerable.Range(50, 55).Select(value => (double)value).ToArray();
        MannWhitneyResult separated = StatisticsCalculator.CalculateMannWhitney(first, second);
        Check(separated.U == 0 && !separated.UsedExactPValue, "The 50/55 separated cohort must use asymptotic U=0.");
        Relative(1.155546222094351e-18, separated.PValue, "Mann–Whitney separated 50/55");
        MannWhitneyResult reversed = StatisticsCalculator.CalculateMannWhitney(second, first);
        Relative(separated.PValue, reversed.PValue, "Swapped groups must preserve p");
        Check(reversed.U == separated.U && reversed.U1 == separated.U2 && reversed.U2 == separated.U1,
            "Swapping cohorts must only reverse the directional rank statistics.");

        MannWhitneyResult tiedSeparated = StatisticsCalculator.CalculateMannWhitney(
            Enumerable.Repeat(1.0, 50), Enumerable.Repeat(2.0, 55));
        Check(tiedSeparated.U == 0 && !tiedSeparated.UsedExactPValue, "Large tied cohorts must retain asymptotic U=0.");
        Relative(2.101873814570361e-24, tiedSeparated.PValue, "Mann–Whitney tied separated 50/55");
        MannWhitneyResult allEqual = StatisticsCalculator.CalculateMannWhitney(
            Enumerable.Repeat(1.0, 50), Enumerable.Repeat(1.0, 55));
        Check(allEqual.PValue == 1, "Zero rank variance must yield p=1.");
        MannWhitneyResult exactTied = StatisticsCalculator.CalculateMannWhitney(
            new[] { 1.0, 2.0, 2.0 }, new[] { 2.0, 3.0, 3.0 });
        // Independent enumeration of all C(6,3)=20 cohort allocations gives 6 extreme allocations.
        Check(exactTied.UsedExactPValue && exactTied.U == 1, "The small tied cohort must keep its exact calculation.");
        Relative(0.3, exactTied.PValue, "Exact tied Mann–Whitney p");

        CategoricalComparisonResult separatedCategories = StatisticsCalculator.CalculateCategoricalComparison(
            Enumerable.Repeat("Да", 50), Enumerable.Repeat("Нет", 55));
        Check(Math.Abs(separatedCategories.ChiSquare - 105) <= 1e-10 && separatedCategories.DegreesOfFreedom == 1
            && !separatedCategories.UsedFisherExact && separatedCategories.MinimumExpectedCount >= 5,
            "The separated 2×2 cohort must keep the Pearson χ²=105 calculation.");
        // Independent reference: scipy.stats.chi2.sf(105, 1).
        Relative(1.221358380729662e-24, separatedCategories.ReportedPValue, "Pearson χ² tiny upper tail");

        double[] family = Enumerable.Repeat(separated.PValue, 10).Append(0.049979).ToArray();
        double[] holm = StatisticsCalculator.AdjustPValuesHolm(family);
        foreach (double adjusted in holm.Take(10))
            Relative(11 * separated.PValue, adjusted, "Holm must retain nonzero tiny p");
        Relative(0.049979, holm[10], "Holm must preserve the final p below .05");

        foreach (string cultureName in new[] { "ru-RU", "en-US" })
            VerifyFormatting(CultureInfo.GetCultureInfo(cultureName));
    }

    private static void VerifyFormatting(CultureInfo culture)
    {
        string tiny = "<" + 0.0001.ToString("0.0000", culture);
        foreach (double value in new[] { 0.0, -0.0, double.Epsilon, 1.155546222094351e-18, Math.BitDecrement(0.0001) })
            Check(StatisticalFormatting.PValue(value, culture) == tiny, "Tiny p must be displayed as a localized bound, including underflow to zero.");
        Check(StatisticalFormatting.PValue(0.0001, culture) == 0.0001.ToString("0.0000", culture), "The display cutoff itself must remain numeric.");
        foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0.1, 1.1 })
            Check(StatisticalFormatting.PValue(value, culture) == "—", "Nonfinite/out-of-range p must be unavailable.");
        Check(StatisticalFormatting.PValue(1, culture) == "1", "p=1 must remain one.");
        Check(StatisticalFormatting.PValue(0.049979, culture) == 0.04998.ToString("0.00000", culture),
            "The exported p=0.049979 must not round to .05.");
        foreach (double boundary in new[] { 0.001, 0.01, 0.05 })
        {
            foreach (double value in new[] { Math.BitDecrement(boundary), boundary, Math.BitIncrement(boundary), boundary - 0.000021, boundary + 0.000021 })
            {
                string rendered = StatisticalFormatting.PValue(value, culture);
                Check(double.TryParse(rendered, NumberStyles.Float, culture, out double shown), "A boundary p must remain parseable.");
                Check(Math.Sign(value - boundary) == Math.Sign(shown - boundary),
                    $"Formatting {value:R} as {rendered} changes its side of significance boundary {boundary:R}.");
            }
        }
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            Check(StatisticalFormatting.PValue(1e-18) == tiny, "The default formatter must use the current culture.");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static void Relative(double expected, double actual, string message)
    {
        Check(double.IsFinite(actual) && actual > 0, $"{message}: expected a positive finite p, received {actual:R}.");
        Check(Math.Abs(actual - expected) / expected <= 2e-12,
            $"{message}: expected {expected:R}, received {actual:R}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
