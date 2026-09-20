using MediHiStat;

Assert(StatisticsCalculator.TryParseMeasurement("1,25", out double commaValue), "Значение с десятичной запятой не распознано.");
AssertClose(1.25, commaValue, "Некорректно распознано значение с десятичной запятой.");
Assert(!StatisticsCalculator.TryParseMeasurement("", out _), "Пустая строка ошибочно распознана как измерение.");
Assert(StatisticsCalculator.TryParseMeasurement("0", out double zeroValue), "Измеренный ноль ошибочно признан отсутствующим значением.");
AssertClose(0, zeroValue, "Некорректно распознан измеренный ноль.");

DescriptiveStatistics descriptive = StatisticsCalculator.CalculateDescriptive(new[] { 1.0, 2.0, 3.0 });
Assert(descriptive.Count == 3, "Некорректный размер выборки описательной статистики.");
AssertClose(2, descriptive.Mean, "Некорректное среднее.");
AssertClose(1, descriptive.SampleStandardDeviation, "Некорректное выборочное стандартное отклонение.");
AssertClose(2, descriptive.Median, "Некорректная медиана.");
AssertClose(1.5, descriptive.FirstQuartile, "Некорректный первый квартиль.");
AssertClose(2.5, descriptive.ThirdQuartile, "Некорректный третий квартиль.");

DescriptiveStatistics fourValues = StatisticsCalculator.CalculateDescriptive(new[] { 1.0, 2.0, 3.0, 4.0 });
AssertClose(2.5, fourValues.Median, "Некорректная медиана для чётного числа наблюдений.");
AssertClose(1.75, fourValues.FirstQuartile, "Некорректная линейная интерполяция первого квартиля.");
AssertClose(3.25, fourValues.ThirdQuartile, "Некорректная линейная интерполяция третьего квартиля.");

DescriptiveStatistics zeros = StatisticsCalculator.CalculateDescriptive(new[] { 0.0, 0.0, 0.0 });
Assert(zeros.Count == 3, "Нулевые измерения исключены из выборки.");
AssertClose(0, zeros.Mean, "Некорректное среднее для нулевых измерений.");
AssertClose(0, zeros.SampleStandardDeviation, "Некорректное стандартное отклонение для нулевых измерений.");

MannWhitneyResult identical = StatisticsCalculator.CalculateMannWhitney(
    new[] { 1.0, 2.0, 3.0 },
    new[] { 1.0, 2.0, 3.0 });
AssertClose(4.5, identical.U, "Некорректный U для совпадающих распределений.");
AssertClose(1, identical.PValue, "Некорректный p-value для совпадающих распределений.");

MannWhitneyResult separated = StatisticsCalculator.CalculateMannWhitney(
    new[] { 1.0, 2.0, 3.0 },
    new[] { 4.0, 5.0, 6.0 });
AssertClose(0, separated.U, "Некорректный U для полностью разделённых групп.");
AssertClose(0.1, separated.PValue, "Некорректный точный двусторонний p-value.");
Assert(separated.UsedExactPValue, "Для малого контрольного набора не использован точный перестановочный расчёт.");

double[] holmAdjusted = StatisticsCalculator.AdjustPValuesHolm(new[] { 0.01, 0.04, 0.03 });
AssertClose(0.03, holmAdjusted[0], "Некорректная поправка Холма для минимального p-value.");
AssertClose(0.06, holmAdjusted[1], "Нарушена монотонность поправки Холма.");
AssertClose(0.06, holmAdjusted[2], "Некорректная поправка Холма для второго p-value.");

CategoricalComparisonResult chiSquare = StatisticsCalculator.CalculateCategoricalComparison(
    Enumerable.Repeat("Да", 10).Concat(Enumerable.Repeat("Нет", 20)),
    Enumerable.Repeat("Да", 20).Concat(Enumerable.Repeat("Нет", 10)));
AssertClose(6.666666666666667, chiSquare.ChiSquare, "Некорректное значение χ² Пирсона.");
AssertClose(0.009823274507519235, chiSquare.ReportedPValue, "Некорректный p-value χ² Пирсона.", 1e-7);
Assert(chiSquare.DegreesOfFreedom == 1, "Некорректное число степеней свободы χ².");
AssertClose(1.0 / 3.0, chiSquare.CramersV, "Некорректное V Крамера.");
Assert(!chiSquare.UsedFisherExact, "Критерий Фишера ошибочно применён при достаточных ожидаемых частотах.");

CategoricalComparisonResult fisher = StatisticsCalculator.CalculateCategoricalComparison(
    Enumerable.Repeat("Да", 1).Concat(Enumerable.Repeat("Нет", 9)),
    Enumerable.Repeat("Да", 8).Concat(Enumerable.Repeat("Нет", 2)));
Assert(fisher.UsedFisherExact, "Для разреженной таблицы 2×2 не выбран точный критерий Фишера.");
AssertClose(0.005477494641581329, fisher.ReportedPValue, "Некорректный двусторонний p-value Фишера.", 1e-9);

CategoricalComparisonResult normalizedCategories = StatisticsCalculator.CalculateCategoricalComparison(
    new[] { " Да ", "да", "Нет" },
    new[] { "ДА", "нет", "Нет" });
Assert(normalizedCategories.Categories.Count == 2, "Регистр и пробелы ошибочно создали дополнительные категории.");

Console.WriteLine("Все контрольные статистические расчёты выполнены успешно.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertClose(double expected, double actual, string message, double tolerance = 1e-9)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"{message} Ожидалось: {expected}; получено: {actual}.");
    }
}
