using MediHiStat;

internal static class PairedVerification
{
    public static void Run()
    {
        // Independent references: scipy 1.17.0, scipy.stats.wilcoxon.
        // Unique nonzero ranks: method='exact', alternative='two-sided'.
        WilcoxonResult unique = PairedStatisticsCalculator.CalculateWilcoxon(new[] { 1.0, 2.0, 3.0, 4.0, -5.0, -6.0, 7.0 });
        Close(11, unique.Statistic, "Wilcoxon: уникальные ранги, статистика");
        Close(0.6875, unique.PValue, "Wilcoxon: scipy exact p");
        Check(unique.UsedExactPValue, "Для малого набора не использован точный расчёт.");

        // Tied/zero ranks: scipy PermutationMethod(n_resamples=np.inf), NOT
        // scipy's untied-rank method='exact' distribution.
        double[][] tiedSamples =
        {
            new[] { 0.0, 1.0, 1.0, -1.0, 2.0, -2.0, -2.0 },
            new[] { 1.0, 1.0, 1.0, -1.0, 2.0, -2.0, 3.0, -3.0, 4.0, -4.0 },
            new[] { 1.0, 1.0, 2.0, 3.0, 4.0, -4.0 },
            new[] { 1.0, 1.0, -1.0, -1.0 }
        };
        double[] referenceP = { 0.90625, 0.837890625, 0.375, 1 };
        for (int index = 0; index < tiedSamples.Length; index++)
        {
            WilcoxonResult actual = PairedStatisticsCalculator.CalculateWilcoxon(tiedSamples[index]);
            Close(referenceP[index], actual.PValue, "Wilcoxon: scipy полный перебор знаков");
            Close(EnumerateSignFlipPValue(tiedSamples[index]), actual.PValue, "Wilcoxon: независимый полный перебор");
            Check(actual.UsedExactPValue, "Связанные ранги ошибочно исключили точный расчёт.");
        }
        WilcoxonResult tiedWithZero = PairedStatisticsCalculator.CalculateWilcoxon(tiedSamples[0]);
        Check(tiedWithZero.PairCount == 7 && tiedWithZero.NonZeroCount == 6 && tiedWithZero.ZeroCount == 1,
            "Некорректно подсчитаны полные/ненулевые/нулевые пары.");
        Close(9, tiedWithZero.PositiveRankSum, "Связанные ранги W+");
        Close(12, tiedWithZero.NegativeRankSum, "Связанные ранги W−");
        Close(-1.0 / 7, tiedWithZero.RankBiserialCorrelation, "Направление рангового эффекта");

        // At the exact threshold, probabilities must not vanish through rounding
        // or be replaced with the distribution for an asymptotic test.
        WilcoxonResult hundredPositive = PairedStatisticsCalculator.CalculateWilcoxon(
            Enumerable.Range(1, 100).Select(value => (double)value));
        Check(hundredPositive.UsedExactPValue, "100 ненулевых пар должны использовать точное распределение.");
        CloseRelative(Math.Pow(2, -99), hundredPositive.PValue, "Точная малая вероятность n=100");
        Close(1, hundredPositive.RankBiserialCorrelation, "Эффект полностью положительных изменений");

        // scipy: method='asymptotic', zero_method='wilcox', correction=True.
        WilcoxonResult largePositive = PairedStatisticsCalculator.CalculateWilcoxon(
            Enumerable.Range(1, 120).Select(value => (double)value));
        Check(!largePositive.UsedExactPValue, "n>100 должно использовать асимптотический расчёт.");
        CloseRelative(1.996919818078046e-21, largePositive.PValue, "Асимптотическая малая вероятность scipy");
        WilcoxonResult largeTied = PairedStatisticsCalculator.CalculateWilcoxon(
            Enumerable.Range(0, 120).Select(index => (index % 2 == 0 ? 1.0 : -1.0) * (index / 3 + 1)));
        Close(3600, largeTied.Statistic, "Большой набор со связанными рангами");
        Close(0.9384151697479838, largeTied.PValue, "Поправка на связи и непрерывность scipy", 2e-13);
        WilcoxonResult largeMixed = PairedStatisticsCalculator.CalculateWilcoxon(
            Enumerable.Range(0, 120).Select(index => (index % 5 == 0 ? -1.0 : 1.0) * (index + 1)));
        CloseRelative(5.599713853831866e-9, largeMixed.PValue, "Асимптотическое scipy p смешанных изменений");

        WilcoxonResult allZero = PairedStatisticsCalculator.CalculateWilcoxon(new[] { 0.0, -0.0, 0.0 });
        Check(allZero.PairCount == 3 && allZero.NonZeroCount == 0 && allZero.ZeroCount == 3,
            "Все нулевые разности неправильно подсчитаны.");
        Close(0, allZero.Statistic, "Нулевые изменения, статистика");
        Close(1, allZero.PValue, "Нулевые изменения, p по соглашению");
        Close(0, allZero.RankBiserialCorrelation, "Нулевые изменения, эффект по соглашению");
        Check(allZero.Notes.Contains("по соглашению"), "Нулевой набор требует пояснения соглашения.");
        WilcoxonResult single = PairedStatisticsCalculator.CalculateWilcoxon(new[] { -8.0 });
        Close(1, single.PValue, "Одна ненулевая пара, двустороннее p");
        Close(-1, single.RankBiserialCorrelation, "Отрицательное направление эффекта");
        WilcoxonResult reversed = PairedStatisticsCalculator.CalculateWilcoxon(tiedSamples[0].Select(value => -value));
        Close(tiedWithZero.PValue, reversed.PValue, "Изменение знака не должно менять двустороннее p");
        Close(-tiedWithZero.RankBiserialCorrelation, reversed.RankBiserialCorrelation, "Инверсия направления эффекта");

        Throws(() => PairedStatisticsCalculator.CalculateWilcoxon(Array.Empty<double>()), "Пустые разности должны отклоняться.");
        Throws(() => PairedStatisticsCalculator.CalculateWilcoxon(new[] { 1.0, double.NaN }), "NaN должен отклоняться.");
        Throws(() => PairedStatisticsCalculator.CalculateWilcoxon(new[] { double.PositiveInfinity }), "Бесконечность должна отклоняться.");
        double[] bonferroni = PairedStatisticsCalculator.AdjustPValuesBonferroni(new[] { 0.01, 0.4, 0.03, 0.0 });
        double[] expectedBonferroni = { 0.04, 1, 0.12, 0 };
        for (int index = 0; index < bonferroni.Length; index++)
        {
            Close(expectedBonferroni[index], bonferroni[index], "Бонферрони");
        }
        Check(PairedStatisticsCalculator.AdjustPValuesBonferroni(Array.Empty<double>()).Length == 0,
            "Пустое семейство Бонферрони должно возвращать пустой массив.");
        Throws(() => PairedStatisticsCalculator.AdjustPValuesBonferroni(new[] { double.NaN }), "Бонферрони: NaN должен отклоняться.");
        Throws(() => PairedStatisticsCalculator.AdjustPValuesBonferroni(new[] { 1.01 }), "Бонферрони: p>1 должен отклоняться.");
        Throws(() => PairedStatisticsCalculator.AdjustPValuesBonferroni(new[] { -0.01 }), "Бонферрони: p<0 должен отклоняться.");
    }

    private static double EnumerateSignFlipPValue(double[] sample)
    {
        double[] nonzero = sample.Where(value => value != 0).ToArray();
        // Independent rank construction by counts, deliberately unlike the production
        // sorting/tie-block and DP implementation.
        double[] ranks = nonzero.Select(value =>
            nonzero.Count(other => Math.Abs(other) < Math.Abs(value))
            + (nonzero.Count(other => Math.Abs(other) == Math.Abs(value)) + 1) / 2.0).ToArray();
        double observed = 0;
        for (int index = 0; index < nonzero.Length; index++)
        {
            observed += nonzero[index] > 0 ? ranks[index] : 0;
        }
        double center = ranks.Sum() / 2;
        double observedDistance = Math.Abs(observed - center);
        int extreme = 0;
        int total = 1 << nonzero.Length;
        for (int mask = 0; mask < total; mask++)
        {
            double positive = 0;
            for (int index = 0; index < ranks.Length; index++)
            {
                if ((mask & (1 << index)) != 0)
                {
                    positive += ranks[index];
                }
            }
            if (Math.Abs(positive - center) >= observedDistance)
            {
                extreme++;
            }
        }
        return extreme / (double)total;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Close(double expected, double actual, string name, double tolerance = 1e-12)
    {
        Check(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance,
            $"{name}: ожидалось {expected:R}; получено {actual:R}.");
    }

    private static void CloseRelative(double expected, double actual, string name)
    {
        Close(1, actual / expected, name, 2e-12);
    }

    private static void Throws(Action action, string message)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }
}
