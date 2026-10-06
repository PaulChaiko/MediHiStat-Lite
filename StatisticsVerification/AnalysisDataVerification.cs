using MediHiStat;

internal static class AnalysisDataVerification
{
    public static void Run()
    {
        MeasurementRecord[] records =
        {
            new("p3", "Bilirubin", new string?[] { "9", null, "7" }),
            new("p1", "Bilirubin", new string?[] { "10", "8", "4" }),
            new("outside", "Bilirubin", new string?[] { "1", "2", "3" }),
            new("p5", "Bilirubin", new string?[] { "0", "0", "0" }),
            new("p2", "Bilirubin", new string?[] { null, "9", "6" }),
            new("p4", "Bilirubin", new string?[] { "5", "bad", null }),
            new("p6", "Bilirubin", new string?[] { "NaN", "Infinity", "-Infinity" }),
            new("p1", "Other", new string?[] { "99", "98", "97" })
        };
        string[] selected = { "p1", "p2", "p3", "p4", "p5", "p6", "absent" };

        NumericSample early = AnalysisData.GetNumericSample(records, selected, "Bilirubin", 0);
        Assert(early.TotalPatients == 7 && early.MissingCount == 3, "Неверные знаменатель и пропуски числовой выборки.");
        AssertSequence(new[] { 10.0, 9.0, 5.0, 0.0 }, early.Values, "Числовая выборка содержит лишние строки или потеряла измеренный ноль.");

        PairedSample pair = AnalysisData.GetPairedSample(records, selected, "Bilirubin", 0, 2);
        Assert(pair.TotalPatients == 7 && pair.MissingCount == 4, "Неверное число полных пар и пропусков.");
        AssertSequence(new[] { 10.0, 9.0, 0.0 }, pair.Earlier, "Исходные значения полных пар собраны неверно.");
        AssertSequence(new[] { 4.0, 7.0, 0.0 }, pair.Later, "Поздние значения сопоставлены по позиции вместо пациента.");
        AssertSequence(new[] { -6.0, -2.0, 0.0 }, pair.Differences, "Разности должны рассчитываться только из полных пар одного пациента.");

        PairedSample middlePair = AnalysisData.GetPairedSample(records, selected.Reverse(), "Bilirubin", 0, 1);
        AssertSequence(new[] { 0.0, -2.0 }, middlePair.Differences, "Перестановка пациентов нарушила сопоставление измерений.");

        MeasurementRecord comma = new("comma", "Bilirubin", new string?[] { "1,25", "0,25" });
        AssertSequence(new[] { -1.0 }, AnalysisData.GetPairedSample(new[] { comma }, new[] { "comma" }, "Bilirubin", 0, 1).Differences,
            "Парный анализ не поддерживает десятичную запятую.");

        MeasurementRecord overflow = new("overflow", "Bilirubin", new string?[] { "-1.7E308", "1.7E308" });
        PairedSample overflowSample = AnalysisData.GetPairedSample(new[] { overflow }, new[] { "overflow" }, "Bilirubin", 0, 1);
        Assert(overflowSample.MissingCount == 1 && overflowSample.Differences.Count == 0, "Бесконечная разность попала в расчёт.");

        AssertThrows<ArgumentException>(() => AnalysisData.GetNumericSample(records.Append(records[1]), selected, "Bilirubin", 0),
            "Дубли строки пациента и показателя не заблокированы.");
        AssertThrows<ArgumentException>(() => AnalysisData.GetPairedSample(records.Append(records[1]), selected, "Bilirubin", 0, 2),
            "Дубли не заблокированы в парном анализе.");
        AssertThrows<ArgumentException>(() => AnalysisData.GetTestCategorySample(records.Append(records[1]), selected, "Bilirubin", 0),
            "Дубли не заблокированы в категориальном анализе.");
        AssertThrows<ArgumentException>(() => AnalysisData.GetNumericSample(records, new[] { "p1", "p1" }, "Bilirubin", 0),
            "Повторный выбор пациента не заблокирован.");
        AssertThrows<ArgumentException>(() => AnalysisData.GetNumericSample(records, new[] { " " }, "Bilirubin", 0),
            "Пустой идентификатор пациента не заблокирован.");
        AssertThrows<ArgumentException>(() => AnalysisData.GetNumericSample(records, selected, " ", 0),
            "Пустое название показателя не заблокировано.");
        AssertThrows<ArgumentOutOfRangeException>(() => AnalysisData.GetNumericSample(records, selected, "Bilirubin", -1),
            "Отрицательный индекс срока не заблокирован.");
        AssertThrows<ArgumentOutOfRangeException>(() => AnalysisData.GetNumericSample(records, selected, "Bilirubin", 3),
            "Несуществующий срок не заблокирован.");
        AssertThrows<ArgumentException>(() => AnalysisData.GetPairedSample(records, selected, "Bilirubin", 2, 0),
            "Обратный порядок сроков не заблокирован.");
        AssertThrows<ArgumentException>(() => AnalysisData.GetPairedSample(records, selected, "Bilirubin", 1, 1),
            "Сравнение одного срока с самим собой не заблокировано.");

        // Invalid or duplicated rows belonging to other people/indicators are not part of the selected sample.
        NumericSample ignoredRows = AnalysisData.GetNumericSample(
            records.Append(records[2]).Append(records[7]), new[] { "p1" }, "Bilirubin", 0);
        AssertSequence(new[] { 10.0 }, ignoredRows.Values, "Строки вне выбранной выборки мешают анализу.");

        AnalysisData.EnsureIndependentGroups(new[] { "p1", "p2" }, new[] { "p3", "p4" });
        AssertThrows<ArgumentException>(() => AnalysisData.EnsureIndependentGroups(new[] { "p1", "p2" }, new[] { "p2", "p3" }),
            "Пересечение независимых групп не заблокировано.");
        AssertThrows<ArgumentException>(() => AnalysisData.EnsureIndependentGroups(new[] { "p1", "p1" }, new[] { "p2" }),
            "Повторный пациент в группе не заблокирован.");

        MeasurementRecord[] categories =
        {
            new("p3", "Complication", new string?[] { "  " }),
            new("p2", "Complication", new string?[] { " да " }),
            new("p1", "Complication", new string?[] { "Нет" }),
            new("p4", "Complication", new string?[] { "0" }),
            new("outside", "Complication", new string?[] { "unexpected" })
        };
        CategorySample testCategories = AnalysisData.GetTestCategorySample(categories, selected, "Complication", 0);
        Assert(testCategories.TotalPatients == 7 && testCategories.MissingCount == 4, "Неверный знаменатель категориальных измерений.");
        Assert(testCategories.Values.SequenceEqual(new[] { "Нет", "да", "0" }), "Категории не обрезаны или измеренная категория 0 потеряна.");

        MeasurementRecord[] whitespaceCategories =
        {
            new("p1", "OperationCategory", new string?[] { " операция  А " }),
            new("p2", "OperationCategory", new string?[] { "\tОПЕРАЦИЯ\tА\n" }),
            new("p3", "OperationCategory", new string?[] { "операция\nБ" }),
            new("p4", "OperationCategory", new string?[] { "операция А" }),
            new("p5", "OperationCategory", new string?[] { "ОПЕРАЦИЯ Б" })
        };
        CategorySample categoryFirst = AnalysisData.GetTestCategorySample(
            whitespaceCategories, new[] { "p1", "p2", "p3" }, "OperationCategory", 0);
        CategorySample categorySecond = AnalysisData.GetTestCategorySample(
            whitespaceCategories, new[] { "p4", "p5" }, "OperationCategory", 0);
        Assert(categoryFirst.Values.SequenceEqual(new[] { "операция А", "ОПЕРАЦИЯ А", "операция Б" }),
            "Внутренние пробельные символы категорий нормализованы неверно.");
        CategoricalComparisonResult whitespaceComparison = StatisticsCalculator.CalculateCategoricalComparison(
            categoryFirst.Values, categorySecond.Values);
        Assert(whitespaceComparison.Categories.Count == 2, "Пробелы или регистр создали лишние статистические категории.");
        for (int index = 0; index < whitespaceComparison.Categories.Count; index++)
        {
            string category = whitespaceComparison.Categories[index];
            int previewFirstCount = categoryFirst.Values.Count(value => string.Equals(value, category, StringComparison.CurrentCultureIgnoreCase));
            int previewSecondCount = categorySecond.Values.Count(value => string.Equals(value, category, StringComparison.CurrentCultureIgnoreCase));
            Assert(previewFirstCount == whitespaceComparison.Group1Counts[index]
                && previewSecondCount == whitespaceComparison.Group2Counts[index],
                "Частоты в предварительном просмотре не совпадают со статистической таблицей.");
        }

        AnalysisPatientRecord[] patients =
        {
            new("p2", "800", " Ж ", null, "operation \t A"),
            new("p1", "400", "М", "disease A; disease B", "operation A"),
            new("p3", "400", "", "disease B", null),
            new("outside", "400", "other", "other", "other")
        };
        CategorySample sex = AnalysisData.GetPatientCategorySample(patients, selected, "Sex");
        Assert(sex.TotalPatients == 7 && sex.MissingCount == 5, "Неверный знаменатель характеристик пациентов.");
        Assert(sex.Values.SequenceEqual(new[] { "М", "Ж" }), "Характеристики пациентов неправильно сопоставлены.");
        CategorySample diagnosis = AnalysisData.GetPatientCategorySample(patients, selected, "Diagnosis");
        Assert(diagnosis.Values.SequenceEqual(new[] { "disease A; disease B", "disease B" }), "Свободный текст диагноза автоматически разобран.");
        CategorySample operation = AnalysisData.GetPatientCategorySample(patients, selected, "Operation");
        Assert(operation.Values.SequenceEqual(new[] { "operation A", "operation A" }) && operation.MissingCount == 5,
            "Операции или их пропуски собраны неверно.");
        AssertThrows<ArgumentException>(() => AnalysisData.GetPatientCategorySample(patients.Append(patients[1]), selected, "Sex"),
            "Дубли записей характеристик пациента не заблокированы.");
        AssertThrows<ArgumentException>(() => AnalysisData.GetPatientCategorySample(patients, selected, "AddDiagnosis"),
            "Неизвестное или неоднозначное поле характеристик не заблокировано.");

        NumericSample absentRows = AnalysisData.GetNumericSample(Array.Empty<MeasurementRecord>(), selected, "Bilirubin", 0);
        Assert(absentRows.TotalPatients == selected.Length && absentRows.MissingCount == selected.Length && absentRows.Values.Count == 0,
            "Отсутствие строк ошибочно заменено нулевыми измерениями.");
        NumericSample empty = AnalysisData.GetNumericSample(records, Array.Empty<string>(), "Bilirubin", 0);
        Assert(empty.TotalPatients == 0 && empty.MissingCount == 0 && empty.Values.Count == 0, "Пустая выборка имеет неверные счётчики.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertSequence(IEnumerable<double> expected, IReadOnlyList<double> actual, string message)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
