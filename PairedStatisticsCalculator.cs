namespace MediHiStat
{
    internal sealed record WilcoxonResult(
        int PairCount,
        int NonZeroCount,
        int ZeroCount,
        double PositiveRankSum,
        double NegativeRankSum,
        double Statistic,
        double PValue,
        double RankBiserialCorrelation,
        bool UsedExactPValue,
        string Notes);

    /// <summary>Paired signed-rank inference from differences supplied in the late − early direction.</summary>
    internal static class PairedStatisticsCalculator
    {
        // DP retains probabilities on a half-rank lattice, so ties also have an exact
        // conditional sign-permutation distribution. Its memory is O(n²), time O(n³).
        private const int ExactNonZeroLimit = 100;

        public static WilcoxonResult CalculateWilcoxon(IEnumerable<double> differences)
        {
            ArgumentNullException.ThrowIfNull(differences);
            double[] pairs = differences.ToArray();
            if (pairs.Length == 0)
            {
                throw new ArgumentException("Для парного сравнения нужна хотя бы одна полная пара.", nameof(differences));
            }
            if (pairs.Any(value => !double.IsFinite(value)))
            {
                throw new ArgumentException("Разности пар должны быть конечными числами; NaN и бесконечности недопустимы.", nameof(differences));
            }

            // Wilcox zero convention: zero differences count as observed complete pairs,
            // but are removed before ranks are assigned.
            double[] nonZeroDifferences = pairs.Where(value => value != 0).ToArray();
            int zeroCount = pairs.Length - nonZeroDifferences.Length;
            int count = nonZeroDifferences.Length;
            if (count == 0)
            {
                return new WilcoxonResult(pairs.Length, 0, zeroCount, 0, 0, 0, 1, 0, true,
                    "Все разности равны нулю: информативных ненулевых пар нет; p = 1 и ранговый эффект = 0 по соглашению. "
                    + "Нулевые разности исключаются по правилу Wilcox. Это не доказывает эквивалентность измерений.");
            }

            int[] sortedIndexes = Enumerable.Range(0, count)
                .OrderBy(index => Math.Abs(nonZeroDifferences[index]))
                .ToArray();
            int[] doubledRanks = new int[count];
            bool hasTies = false;
            int start = 0;
            while (start < count)
            {
                int end = start + 1;
                double absoluteValue = Math.Abs(nonZeroDifferences[sortedIndexes[start]]);
                while (end < count && Math.Abs(nonZeroDifferences[sortedIndexes[end]]) == absoluteValue)
                {
                    end++;
                }
                hasTies |= end - start > 1;
                // Average of the one-based ranks start+1,...,end, multiplied by two.
                int doubledRank = start + 1 + end;
                for (int index = start; index < end; index++)
                {
                    doubledRanks[sortedIndexes[index]] = doubledRank;
                }
                start = end;
            }

            double positiveRankSum = 0;
            double negativeRankSum = 0;
            double squaredRankSum = 0;
            for (int index = 0; index < count; index++)
            {
                double rank = doubledRanks[index] / 2.0;
                if (nonZeroDifferences[index] > 0)
                {
                    positiveRankSum += rank;
                }
                else
                {
                    negativeRankSum += rank;
                }
                squaredRankSum += rank * rank;
            }

            double statistic = Math.Min(positiveRankSum, negativeRankSum);
            double totalRankSum = positiveRankSum + negativeRankSum;
            double effect = (positiveRankSum - negativeRankSum) / totalRankSum;
            bool exact = count <= ExactNonZeroLimit;
            double pValue;
            string methodNote;
            if (exact)
            {
                pValue = ExactTwoSidedSignPermutationPValue(doubledRanks, (int)(2 * statistic));
                methodNote = "Точное двустороннее p: условное распределение всех перестановок знаков "
                    + "при фиксированных абсолютных разностях (динамическое программирование).";
            }
            else
            {
                // Given the average ranks, each sign is an independent Bernoulli(1/2).
                // Var(W+) = sum(rank²)/4 automatically includes the usual tie correction.
                double mean = totalRankSum / 2;
                double correctedDistance = Math.Max(0, Math.Abs(positiveRankSum - mean) - 0.5);
                double z = correctedDistance / Math.Sqrt(squaredRankSum / 4);
                pValue = NormalTwoSidedTail(z);
                methodNote = "Асимптотическое двустороннее p: нормальное приближение с поправкой "
                    + "дисперсии на связанные ранги и поправкой на непрерывность 0,5 (более 100 ненулевых пар).";
            }

            string notes = methodNote + " Нулевые разности исключаются до ранжирования (Wilcox); "
                + $"исключено нулевых пар: {zeroCount}. "
                + (hasTies ? "Совпадающие абсолютные разности получают средние ранги. " : string.Empty)
                + "Положительный рангово-бисериальный эффект означает увеличение позднего значения относительно раннего. "
                + "Предпосылка: независимые пациенты и симметрия распределения разностей при нулевой гипотезе.";

            return new WilcoxonResult(pairs.Length, count, zeroCount, positiveRankSum,
                negativeRankSum, statistic, Math.Clamp(pValue, 0, 1), effect, exact, notes);
        }

        public static double[] AdjustPValuesBonferroni(IReadOnlyList<double> pValues)
        {
            ArgumentNullException.ThrowIfNull(pValues);
            double[] adjusted = new double[pValues.Count];
            for (int index = 0; index < pValues.Count; index++)
            {
                double value = pValues[index];
                if (!double.IsFinite(value) || value < 0 || value > 1)
                {
                    throw new ArgumentException("Для поправки Бонферрони нужны конечные p в диапазоне [0; 1].", nameof(pValues));
                }
                adjusted[index] = Math.Min(1, value * pValues.Count);
            }
            return adjusted;
        }

        private static double ExactTwoSidedSignPermutationPValue(int[] doubledRanks, int doubledStatistic)
        {
            int maximumSum = doubledRanks.Sum();
            double[] probabilities = new double[maximumSum + 1];
            probabilities[0] = 1;
            int reachableSum = 0;
            foreach (int rank in doubledRanks)
            {
                for (int sum = reachableSum + rank; sum >= 0; sum--)
                {
                    double retainNegative = sum <= reachableSum ? probabilities[sum] : 0;
                    double changeToPositive = sum >= rank && sum - rank <= reachableSum
                        ? probabilities[sum - rank]
                        : 0;
                    probabilities[sum] = 0.5 * (retainNegative + changeToPositive);
                }
                reachableSum += rank;
            }

            double lowerTail = 0;
            for (int sum = 0; sum <= doubledStatistic; sum++)
            {
                lowerTail += probabilities[sum];
            }
            // The distribution is symmetric. Inclusive tails implement the conventional
            // two-sided test; clamp accounts for overlapping tails at the center.
            return Math.Min(1, 2 * lowerTail);
        }

        private static double NormalTwoSidedTail(double z)
        {
            // erfc(z/sqrt(2)) = Q(1/2,z²/2). Evaluate the regularized upper
            // incomplete gamma directly in the tail to avoid subtractive cancellation.
            double x = z * z / 2;
            if (x == 0)
            {
                return 1;
            }
            double factor = Math.Exp(0.5 * Math.Log(x) - x - 0.5 * Math.Log(Math.PI));
            if (x < 1.5)
            {
                double term = 2;
                double sum = term;
                double shape = 0.5;
                for (int iteration = 0; iteration < 10_000; iteration++)
                {
                    shape++;
                    term *= x / shape;
                    sum += term;
                    if (Math.Abs(term) <= Math.Abs(sum) * 1e-15)
                    {
                        break;
                    }
                }
                return Math.Clamp(1 - factor * sum, 0, 1);
            }

            const double tiny = 1e-300;
            double b = x + 0.5;
            double c = 1 / tiny;
            double d = 1 / b;
            double fraction = d;
            for (int iteration = 1; iteration <= 10_000; iteration++)
            {
                double numerator = -iteration * (iteration - 0.5);
                b += 2;
                d = numerator * d + b;
                if (Math.Abs(d) < tiny)
                {
                    d = tiny;
                }
                c = b + numerator / c;
                if (Math.Abs(c) < tiny)
                {
                    c = tiny;
                }
                d = 1 / d;
                double multiplier = d * c;
                fraction *= multiplier;
                if (Math.Abs(multiplier - 1) <= 1e-15)
                {
                    break;
                }
            }
            return Math.Clamp(factor * fraction, 0, 1);
        }
    }
}
