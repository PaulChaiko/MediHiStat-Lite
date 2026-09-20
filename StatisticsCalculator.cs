using System.Globalization;

namespace MediHiStat
{
    internal readonly record struct DescriptiveStatistics(
        int Count,
        double Mean,
        double SampleStandardDeviation,
        double Median,
        double FirstQuartile,
        double ThirdQuartile);

    internal readonly record struct MannWhitneyResult(
        int Group1Count,
        int Group2Count,
        double U1,
        double U2,
        double U,
        double PValue,
        bool UsedExactPValue);

    internal sealed record CategoricalComparisonResult(
        IReadOnlyList<string> Categories,
        IReadOnlyList<int> Group1Counts,
        IReadOnlyList<int> Group2Counts,
        int Group1Count,
        int Group2Count,
        double ChiSquare,
        int DegreesOfFreedom,
        double PearsonPValue,
        double ReportedPValue,
        double CramersV,
        double MinimumExpectedCount,
        int ExpectedCountsBelowFive,
        bool UsedFisherExact);

    internal static class StatisticsCalculator
    {
        private const int ExactPermutationLimit = 200_000;

        public static bool TryParseMeasurement(string? text, out double value)
        {
            value = default;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string normalized = text.Trim().Replace(" ", string.Empty).Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !double.IsNaN(value)
                && !double.IsInfinity(value);
        }

        public static DescriptiveStatistics CalculateDescriptive(IEnumerable<double> values)
        {
            double[] sample = values
                .Where(value => !double.IsNaN(value) && !double.IsInfinity(value))
                .ToArray();

            if (sample.Length == 0)
            {
                return new DescriptiveStatistics(
                    0,
                    double.NaN,
                    double.NaN,
                    double.NaN,
                    double.NaN,
                    double.NaN);
            }

            Array.Sort(sample);
            double mean = sample.Average();
            double median = CalculatePercentile(sample, 0.5);
            double firstQuartile = CalculatePercentile(sample, 0.25);
            double thirdQuartile = CalculatePercentile(sample, 0.75);

            if (sample.Length == 1)
            {
                return new DescriptiveStatistics(
                    1,
                    mean,
                    double.NaN,
                    median,
                    firstQuartile,
                    thirdQuartile);
            }

            double squaredDeviationSum = sample.Sum(value => Math.Pow(value - mean, 2));
            double sampleStandardDeviation = Math.Sqrt(squaredDeviationSum / (sample.Length - 1));

            return new DescriptiveStatistics(
                sample.Length,
                mean,
                sampleStandardDeviation,
                median,
                firstQuartile,
                thirdQuartile);
        }

        public static CategoricalComparisonResult CalculateCategoricalComparison(
            IEnumerable<string?> group1Values,
            IEnumerable<string?> group2Values)
        {
            string[] group1 = group1Values
                .Select(NormalizeCategory)
                .Where(value => value is not null)
                .Select(value => value!)
                .ToArray();
            string[] group2 = group2Values
                .Select(NormalizeCategory)
                .Where(value => value is not null)
                .Select(value => value!)
                .ToArray();

            if (group1.Length == 0 || group2.Length == 0)
            {
                throw new ArgumentException("Обе сравниваемые группы должны содержать категориальные наблюдения.");
            }

            string[] categories = group1
                .Concat(group2)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();

            if (categories.Length < 2)
            {
                throw new ArgumentException("Для сравнения нужны как минимум две категории.");
            }

            int[] group1Counts = categories
                .Select(category => group1.Count(value => string.Equals(
                    value,
                    category,
                    StringComparison.CurrentCultureIgnoreCase)))
                .ToArray();
            int[] group2Counts = categories
                .Select(category => group2.Count(value => string.Equals(
                    value,
                    category,
                    StringComparison.CurrentCultureIgnoreCase)))
                .ToArray();

            int totalCount = group1.Length + group2.Length;
            double chiSquare = 0;
            double minimumExpectedCount = double.PositiveInfinity;
            int expectedCountsBelowFive = 0;

            for (int column = 0; column < categories.Length; column++)
            {
                int columnTotal = group1Counts[column] + group2Counts[column];
                double group1Expected = group1.Length * columnTotal / (double)totalCount;
                double group2Expected = group2.Length * columnTotal / (double)totalCount;

                minimumExpectedCount = Math.Min(
                    minimumExpectedCount,
                    Math.Min(group1Expected, group2Expected));
                if (group1Expected < 5)
                {
                    expectedCountsBelowFive++;
                }
                if (group2Expected < 5)
                {
                    expectedCountsBelowFive++;
                }

                chiSquare += Math.Pow(group1Counts[column] - group1Expected, 2) / group1Expected;
                chiSquare += Math.Pow(group2Counts[column] - group2Expected, 2) / group2Expected;
            }

            int degreesOfFreedom = categories.Length - 1;
            double pearsonPValue = ChiSquareSurvivalProbability(chiSquare, degreesOfFreedom);
            bool useFisherExact = categories.Length == 2 && expectedCountsBelowFive > 0;
            double reportedPValue = useFisherExact
                ? CalculateFisherExactTwoSided(
                    group1Counts[0],
                    group1Counts[1],
                    group2Counts[0],
                    group2Counts[1])
                : pearsonPValue;
            double cramersV = Math.Sqrt(chiSquare / totalCount);

            return new CategoricalComparisonResult(
                categories,
                group1Counts,
                group2Counts,
                group1.Length,
                group2.Length,
                chiSquare,
                degreesOfFreedom,
                pearsonPValue,
                reportedPValue,
                cramersV,
                minimumExpectedCount,
                expectedCountsBelowFive,
                useFisherExact);
        }

        public static MannWhitneyResult CalculateMannWhitney(
            IEnumerable<double> group1Values,
            IEnumerable<double> group2Values)
        {
            double[] group1 = group1Values
                .Where(value => !double.IsNaN(value) && !double.IsInfinity(value))
                .ToArray();
            double[] group2 = group2Values
                .Where(value => !double.IsNaN(value) && !double.IsInfinity(value))
                .ToArray();

            if (group1.Length == 0 || group2.Length == 0)
            {
                throw new ArgumentException("Обе сравниваемые группы должны содержать числовые наблюдения.");
            }

            var combined = group1
                .Select(value => (Value: value, Group: 1))
                .Concat(group2.Select(value => (Value: value, Group: 2)))
                .OrderBy(item => item.Value)
                .ToArray();

            double[] ranks = new double[combined.Length];
            var tieSizes = new List<int>();

            int start = 0;
            while (start < combined.Length)
            {
                int end = start + 1;
                while (end < combined.Length && combined[end].Value.Equals(combined[start].Value))
                {
                    end++;
                }

                double averageRank = ((start + 1) + end) / 2.0;
                for (int index = start; index < end; index++)
                {
                    ranks[index] = averageRank;
                }

                tieSizes.Add(end - start);
                start = end;
            }

            double group1RankSum = 0;
            for (int index = 0; index < combined.Length; index++)
            {
                if (combined[index].Group == 1)
                {
                    group1RankSum += ranks[index];
                }
            }

            int group1Count = group1.Length;
            int group2Count = group2.Length;
            double u1 = group1RankSum - group1Count * (group1Count + 1) / 2.0;
            double u2 = group1Count * group2Count - u1;
            double u = Math.Min(u1, u2);

            bool useExactPValue = CombinationCountAtMost(combined.Length, group1Count, ExactPermutationLimit);
            double pValue = useExactPValue
                ? CalculateExactTwoSidedPValue(ranks, group1Count, u)
                : CalculateAsymptoticTwoSidedPValue(u, group1Count, group2Count, tieSizes);

            return new MannWhitneyResult(
                group1Count,
                group2Count,
                u1,
                u2,
                u,
                pValue,
                useExactPValue);
        }

        public static double[] AdjustPValuesHolm(IReadOnlyList<double> pValues)
        {
            if (pValues.Any(pValue => double.IsNaN(pValue) || pValue < 0 || pValue > 1))
            {
                throw new ArgumentOutOfRangeException(nameof(pValues), "Каждый p-value должен находиться в диапазоне от 0 до 1.");
            }

            int[] orderedIndexes = Enumerable.Range(0, pValues.Count)
                .OrderBy(index => pValues[index])
                .ToArray();
            double[] adjustedPValues = new double[pValues.Count];
            double previousAdjustedValue = 0;

            for (int rank = 0; rank < orderedIndexes.Length; rank++)
            {
                int originalIndex = orderedIndexes[rank];
                double adjustedValue = Math.Min(1, (pValues.Count - rank) * pValues[originalIndex]);
                adjustedValue = Math.Max(previousAdjustedValue, adjustedValue);
                adjustedPValues[originalIndex] = adjustedValue;
                previousAdjustedValue = adjustedValue;
            }

            return adjustedPValues;
        }

        private static bool CombinationCountAtMost(int total, int selected, int limit)
        {
            selected = Math.Min(selected, total - selected);
            double combinations = 1;

            for (int index = 1; index <= selected; index++)
            {
                combinations *= (total - selected + index) / (double)index;
                if (combinations > limit)
                {
                    return false;
                }
            }

            return true;
        }

        private static double CalculatePercentile(IReadOnlyList<double> sortedValues, double probability)
        {
            if (sortedValues.Count == 1)
            {
                return sortedValues[0];
            }

            double position = probability * (sortedValues.Count - 1);
            int lowerIndex = (int)Math.Floor(position);
            int upperIndex = (int)Math.Ceiling(position);
            double fraction = position - lowerIndex;
            return sortedValues[lowerIndex]
                + (sortedValues[upperIndex] - sortedValues[lowerIndex]) * fraction;
        }

        private static string? NormalizeCategory(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return string.Join(
                " ",
                value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        private static double CalculateFisherExactTwoSided(int a, int b, int c, int d)
        {
            int firstRowTotal = a + b;
            int firstColumnTotal = a + c;
            int secondColumnTotal = b + d;
            int total = firstRowTotal + c + d;
            int minimumA = Math.Max(0, firstRowTotal - secondColumnTotal);
            int maximumA = Math.Min(firstRowTotal, firstColumnTotal);
            double observedProbability = HypergeometricProbability(
                a,
                firstRowTotal,
                firstColumnTotal,
                secondColumnTotal,
                total);
            double pValue = 0;

            for (int candidateA = minimumA; candidateA <= maximumA; candidateA++)
            {
                double probability = HypergeometricProbability(
                    candidateA,
                    firstRowTotal,
                    firstColumnTotal,
                    secondColumnTotal,
                    total);
                if (probability <= observedProbability + 1e-12)
                {
                    pValue += probability;
                }
            }

            return Math.Clamp(pValue, 0, 1);
        }

        private static double HypergeometricProbability(
            int firstCell,
            int firstRowTotal,
            int firstColumnTotal,
            int secondColumnTotal,
            int total)
        {
            return Math.Exp(
                LogCombination(firstColumnTotal, firstCell)
                + LogCombination(secondColumnTotal, firstRowTotal - firstCell)
                - LogCombination(total, firstRowTotal));
        }

        private static double LogCombination(int total, int selected)
        {
            if (selected < 0 || selected > total)
            {
                return double.NegativeInfinity;
            }

            selected = Math.Min(selected, total - selected);
            double result = 0;
            for (int index = 1; index <= selected; index++)
            {
                result += Math.Log(total - selected + index) - Math.Log(index);
            }
            return result;
        }

        private static double ChiSquareSurvivalProbability(double chiSquare, int degreesOfFreedom)
        {
            if (chiSquare <= 0)
            {
                return 1;
            }

            double halfChiSquare = chiSquare / 2;
            if (degreesOfFreedom % 2 == 0)
            {
                int terms = degreesOfFreedom / 2;
                double term = 1;
                double sum = term;
                for (int index = 1; index < terms; index++)
                {
                    term *= halfChiSquare / index;
                    sum += term;
                }
                return Math.Clamp(Math.Exp(-halfChiSquare) * sum, 0, 1);
            }

            double survivalProbability = 1 - Erf(Math.Sqrt(halfChiSquare));
            double recurrenceTerm = Math.Exp(-halfChiSquare)
                * Math.Sqrt(halfChiSquare)
                / (0.5 * Math.Sqrt(Math.PI));
            int recurrenceSteps = (degreesOfFreedom - 1) / 2;
            for (int step = 0; step < recurrenceSteps; step++)
            {
                survivalProbability += recurrenceTerm;
                recurrenceTerm *= halfChiSquare / (step + 1.5);
            }
            return Math.Clamp(survivalProbability, 0, 1);
        }

        private static double CalculateExactTwoSidedPValue(double[] ranks, int group1Count, double observedU)
        {
            double meanU = group1Count * (ranks.Length - group1Count) / 2.0;
            double observedDistance = Math.Abs(observedU - meanU);
            long totalPermutations = 0;
            long equallyOrMoreExtremePermutations = 0;

            EnumerateRankSums(0, group1Count, 0);

            return equallyOrMoreExtremePermutations / (double)totalPermutations;

            void EnumerateRankSums(int startIndex, int remaining, double rankSum)
            {
                if (remaining == 0)
                {
                    double permutationU = rankSum - group1Count * (group1Count + 1) / 2.0;
                    double distance = Math.Abs(permutationU - meanU);
                    totalPermutations++;

                    if (distance + 1e-12 >= observedDistance)
                    {
                        equallyOrMoreExtremePermutations++;
                    }

                    return;
                }

                int lastStartIndex = ranks.Length - remaining;
                for (int index = startIndex; index <= lastStartIndex; index++)
                {
                    EnumerateRankSums(index + 1, remaining - 1, rankSum + ranks[index]);
                }
            }
        }

        private static double CalculateAsymptoticTwoSidedPValue(
            double u,
            int group1Count,
            int group2Count,
            IEnumerable<int> tieSizes)
        {
            int totalCount = group1Count + group2Count;
            double tieCorrection = tieSizes.Sum(size => Math.Pow(size, 3) - size);
            double variance = group1Count * group2Count / 12.0
                * (totalCount + 1 - tieCorrection / (totalCount * (totalCount - 1.0)));

            if (variance <= 0)
            {
                return 1.0;
            }

            double meanU = group1Count * group2Count / 2.0;
            double correctedDistance = Math.Max(0, Math.Abs(u - meanU) - 0.5);
            double z = correctedDistance / Math.Sqrt(variance);
            double pValue = 2 * (1 - NormalCdf(z));
            return Math.Clamp(pValue, 0, 1);
        }

        private static double NormalCdf(double value)
        {
            return 0.5 * (1 + Erf(value / Math.Sqrt(2)));
        }

        private static double Erf(double value)
        {
            double sign = Math.Sign(value);
            double absoluteValue = Math.Abs(value);
            double t = 1 / (1 + 0.3275911 * absoluteValue);
            double polynomial = (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t;
            return sign * (1 - polynomial * Math.Exp(-absoluteValue * absoluteValue));
        }
    }
}
