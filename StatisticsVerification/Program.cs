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

Console.WriteLine("Все контрольные статистические расчёты выполнены успешно.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertClose(double expected, double actual, string message)
{
    if (Math.Abs(expected - actual) > 1e-9)
    {
        throw new InvalidOperationException($"{message} Ожидалось: {expected}; получено: {actual}.");
    }
}
