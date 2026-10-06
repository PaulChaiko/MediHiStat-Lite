using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using MediHiStat;
using Microsoft.Data.Sqlite;

/// <summary>
/// Synthetic transfer and persistence checks. Never accesses the user's database or patient files.
/// </summary>
internal static class PatientTransferVerification
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "MediHiStat-update4-verification-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            VerifySchema();
            VerifyRoundTrip(directory);
            VerifyTemplates(directory);
            VerifyImportedSchema(directory);
            VerifyDatabase(directory);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
        Console.WriteLine("Проверки update4: схема, XLSX/CSV, совпадения пациентов и транзакции SQLite выполнены успешно.");
    }

    private static void VerifySchema()
    {
        Assert(PatientTableSchema.PatientHeaders.Length == 11, "Схема должна содержать 11 характеристик пациента.");
        Assert(PatientTableSchema.TestNames.Length == 69, "Схема должна содержать 69 показателей.");
        Assert(PatientTableSchema.TimeHeaders.Length == 11, "Схема должна содержать 11 сроков.");
        Assert(!PatientTableSchema.PatientHeaders.Any(value => value.Contains("Особенности", StringComparison.OrdinalIgnoreCase)),
            "Исключённый пользователем столбец «Особенности» вернулся в схему.");

        PatientTableSchema.ValidateRecord(CreatePatient("  000701  ", 1));
        Assert(CreatePatient("  000701  ", 1).PatientId == "000701", "Идентификатор пациента не обрезан или потерял ведущие нули.");
        AssertRejects(() => PatientTableSchema.ValidateRecord(CreatePatient(" ", 1)), "Пустой пациент разрешён.");
        foreach (int index in new[] { 2, 4, 5 })
        {
            PatientRecord invalid = AlterCharacteristics(CreatePatient("invalid", 1), index, "не число");
            AssertRejects(() => PatientTableSchema.ValidateRecord(invalid), "Неверное числовое паспортное поле разрешено.");
        }
        AssertRejects(() => PatientTableSchema.ValidateRecord(AlterCharacteristics(CreatePatient("invalid", 1), 2, "28.5")),
            "Дробный возраст разрешён как целочисленное поле.");
        AssertRejects(() => PatientTableSchema.ValidateRecord(AlterCharacteristics(CreatePatient("invalid", 1), 4, "Infinity")),
            "Бесконечный рост разрешён.");
        AssertRejects(() => PatientTableSchema.ValidateRecord(AlterCharacteristics(CreatePatient("invalid", 1), 4, "0")),
            "Нулевой рост разрешён.");
        AssertRejects(() => PatientTableSchema.ValidateRecord(AlterCharacteristics(CreatePatient("invalid", 1), 5, "0")),
            "Нулевой вес разрешён.");
        PatientTableSchema.ValidateRecord(AlterCharacteristics(CreatePatient("zero-age", 1), 2, "0"));
        PatientTableSchema.ValidateRecord(AlterCharacteristics(CreatePatient("comma-height", 1), 4, "185,5"));
        Dictionary<string, string[]> missing = CreatePatient("missing", 1).Measurements.ToDictionary(item => item.Key, item => item.Value);
        missing.Remove(PatientTableSchema.TestNames[0]);
        AssertRejects(() => PatientTableSchema.ValidateRecord(new PatientRecord(CreatePatient("missing", 1).Characteristics, missing)),
            "Неполный набор показателей разрешён.");
        Dictionary<string, string[]> wrongDays = CreatePatient("days", 1).Measurements.ToDictionary(item => item.Key, item => item.Value);
        wrongDays[PatientTableSchema.TestNames[0]] = new[] { "1" };
        AssertRejects(() => PatientTableSchema.ValidateRecord(new PatientRecord(CreatePatient("days", 1).Characteristics, wrongDays)),
            "Неверное число сроков разрешено.");
    }

    private static void VerifyRoundTrip(string directory)
    {
        PatientRecord first = CreatePatient("000701", 1);
        PatientRecord second = CreatePatient("Синтетический пациент Б", 2);
        PatientRecord third = CreatePatient("Синтетический пациент В", 3);
        string workbook = Path.Combine(directory, "roundtrip.xlsx");
        PatientTableFiles.Write(workbook, new[] { first, second, third });
        IReadOnlyList<PatientImportEntry> imported = PatientTableFiles.Read(workbook);
        Assert(imported.Count == 3, "Не все листы XLSX проверены.");
        for (int index = 0; index < imported.Count; index++)
        {
            Assert(imported[index].Patient is not null && string.IsNullOrEmpty(imported[index].Error) && !imported[index].IsEmpty,
                "Корректный лист XLSX не распознан: " + imported[index].Error);
            AssertEqual(new[] { first, second, third }[index], imported[index].Patient!, "XLSX изменил исходные данные.");
        }

        string csv = Path.Combine(directory, "roundtrip.csv");
        PatientTableFiles.Write(csv, new[] { first });
        byte[] bytes = File.ReadAllBytes(csv);
        Assert(bytes.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }), "CSV не имеет UTF-8 BOM для Excel.");
        AssertEqual(first, SinglePatient(PatientTableFiles.Read(csv)), "CSV изменил исходные данные.");

        foreach (char separator in new[] { ';', ',', '\t' })
        {
            string text = "\uFEFF" + SerializeTable(CreateTable(first), separator);
            AssertEqual(first, SinglePatient(PatientTableFiles.ReadText(text)),
                "Буфер обмена потерял кавычки, разделители, переносы, десятичную запятую или ведущие нули.");
        }

        string[][] aliasTable = CreateTable(first);
        int thrombin = Array.IndexOf(PatientTableSchema.TestNames, "Т");
        int numbers = Array.IndexOf(PatientTableSchema.TestNames, "Тест связи чисел");
        Assert(thrombin >= 0 && numbers >= 0, "Из схемы исчезли показатели для проверки исходных псевдонимов.");
        aliasTable[thrombin + 1][11] = "T";
        aliasTable[numbers + 1][11] = "Тест связи чисел (сек.)";
        AssertEqual(first, SinglePatient(PatientTableFiles.ReadText(SerializeTable(aliasTable, ';'))),
            "Псевдонимы исходной таблицы не сопоставлены с названиями показателей в базе.");
        string[][] sourceWithFeatures = CreateTable(first).Select(row => row.Append("").ToArray()).ToArray();
        sourceWithFeatures[0][23] = "Особенности";
        sourceWithFeatures[1][23] = "Игнорируемое примечание исходной схемы";
        AssertEqual(first, SinglePatient(PatientTableFiles.ReadText(SerializeTable(sourceWithFeatures, ';'))),
            "Исключённый столбец исходной схемы мешает импорту или попал в измерения.");
        string[][] rearranged = CreateTable(first);
        foreach (string[] row in rearranged)
        {
            (row[0], row[9]) = (row[9], row[0]);
            (row[12], row[22]) = (row[22], row[12]);
        }
        rearranged = new[] { Array.Empty<string>(), Array.Empty<string>() }
            .Concat(rearranged.Select(row => new[] { "" }.Concat(row).ToArray())).ToArray();
        AssertEqual(first, SinglePatient(PatientTableFiles.ReadText(SerializeTable(rearranged, ';'))),
            "Схема сопоставляет характеристики и сроки по координатам вместо заголовков.");

        // CSV cannot distinguish a formula from plain text once opened in spreadsheet software.
        // XLSX must preserve that text as a string, while normal negative numbers remain transferable.
        foreach (string formulaLike in new[] { "=1+1", "+SUM(A1)", "-SUM(A1)", "@SUM(A1)" })
        {
            PatientRecord withFormulaText = AlterCharacteristics(first, 6, formulaLike);
            AssertRejects(() => PatientTableFiles.Write(csv, new[] { withFormulaText }),
                "CSV экспорт разрешил опасный для электронной таблицы текст: " + formulaLike, "XLSX");
            PatientTableFiles.Write(workbook, new[] { withFormulaText });
            AssertEqual(withFormulaText, SinglePatient(PatientTableFiles.Read(workbook)), "XLSX превратил формулоподобный текст в формулу.");
        }
        AssertRejects(() => PatientTableFiles.Write(csv, new[] { first, second }), "CSV разрешил экспорт нескольких пациентов.");
    }

    private static void VerifyTemplates(string directory)
    {
        string workbook = Path.Combine(directory, "template.xlsx");
        PatientTableFiles.WriteTemplate(workbook, 4);
        IReadOnlyList<PatientImportEntry> entries = PatientTableFiles.Read(workbook);
        Assert(entries.Count == 4 && entries.All(value => value.IsEmpty && value.Patient is null && string.IsNullOrEmpty(value.Error)),
            "Пустой XLSX шаблон содержит пациентов, ошибочные записи или неверное число листов.");
        using (ZipArchive archive = ZipFile.OpenRead(workbook))
        {
            string[] sheetNames = archive.Entries.Where(value => value.FullName.StartsWith("xl/worksheets/sheet", StringComparison.Ordinal)
                && value.FullName.EndsWith(".xml", StringComparison.Ordinal)).Select(value => value.FullName).ToArray();
            Assert(sheetNames.Length == 4, "Шаблон не содержит запрошенные четыре листа.");
            foreach (string sheetName in sheetNames)
            {
                Dictionary<string, string> cells = ReadCells(archive, sheetName);
                Assert(!cells.Values.Any(value => value.Contains("Особенности", StringComparison.OrdinalIgnoreCase)),
                    "Шаблон содержит исключённый столбец «Особенности».");
                for (int characteristic = 0; characteristic < 11; characteristic++)
                    Assert(Cell(cells, Address(characteristic + 1, 2)) == "", "В шаблоне остались характеристики пациента.");
                for (int test = 0; test < 69; test++)
                for (int day = 0; day < 11; day++)
                    Assert(Cell(cells, Address(day + 13, test + 2)) == "", "В шаблоне остались измерения.");
            }
        }
        string csv = Path.Combine(directory, "template.csv");
        PatientTableFiles.WriteTemplate(csv);
        Assert(PatientTableFiles.Read(csv).Single().IsEmpty, "Пустой CSV шаблон ошибочно стал пациентом.");
        AssertRejects(() => PatientTableFiles.WriteTemplate(workbook, 0), "Разрешён XLSX шаблон без листов.");
        AssertRejects(() => PatientTableFiles.WriteTemplate(csv, 2), "Разрешён CSV шаблон с несколькими пациентами.");
    }

    private static void VerifyImportedSchema(string directory)
    {
        PatientRecord patient = CreatePatient("Схема", 4);
        string[][] table = CreateTable(patient);
        foreach (Action<string[][]> mutation in new Action<string[][]>[]
        {
            rows => rows[2][11] = rows[1][11],
            rows => rows[2][11] = "Несуществующий лабораторный показатель",
            rows => rows[2][11] = "",
            rows => rows[0][13] = rows[0][12],
            rows => rows[0][13] = "Неизвестный срок",
            rows => rows[0][13] = "",
            rows => rows[0][1] = rows[0][0],
            rows => rows[0][1] = "Неизвестная характеристика",
            rows => rows[0][0] = ""
        })
        {
            string[][] changed = table.Select(row => (string[])row.Clone()).ToArray();
            mutation(changed);
            AssertError(PatientTableFiles.ReadText(SerializeTable(changed, ';')), "Повреждённая CSV схема принята.");
        }

        string original = Path.Combine(directory, "valid-schema.xlsx");
        PatientTableFiles.Write(original, new[] { patient });
        foreach ((string address, string value) in new[]
        {
            ("L3", PatientTableSchema.TestNames[0]), ("L3", "Несуществующий показатель"), ("L3", ""),
            ("N1", PatientTableSchema.TimeHeaders[0]), ("N1", "Неизвестный срок"), ("N1", "")
        })
        {
            string malformed = Path.Combine(directory, "invalid-schema.xlsx");
            File.Copy(original, malformed, overwrite: true);
            ChangeCell(malformed, "xl/worksheets/sheet1.xml", address, value);
            AssertError(PatientTableFiles.Read(malformed), "Повреждённая XLSX схема принята: " + address);
        }

        string duplicates = Path.Combine(directory, "duplicates.xlsx");
        PatientTableFiles.Write(duplicates, new[] { patient, CreatePatient("Другой пациент", 5) });
        ChangeCell(duplicates, "xl/worksheets/sheet2.xml", "A2", " схема ");
        IReadOnlyList<PatientImportEntry> duplicateEntries = PatientTableFiles.Read(duplicates);
        AssertDuplicates(duplicateEntries, "Схема", 2);
        ChangeCell(duplicates, "xl/worksheets/sheet2.xml", "E2", "bad height");
        AssertDuplicates(PatientTableFiles.Read(duplicates), "Схема", 2);
        ChangeCell(duplicates, "xl/worksheets/sheet2.xml", "E2", "185.5");
        ChangeRawCell(duplicates, "xl/worksheets/sheet2.xml", "M2", "n", "2", formula: "1+1");
        AssertDuplicates(PatientTableFiles.Read(duplicates), "Схема", 2);

        string excludedColumn = Path.Combine(directory, "excluded-features.xlsx");
        File.Copy(original, excludedColumn, overwrite: true);
        ChangeCell(excludedColumn, "xl/worksheets/sheet1.xml", "X1", "Особенности");
        ChangeRawCell(excludedColumn, "xl/worksheets/sheet1.xml", "X2", "n", "2", formula: "1+1");
        ChangeRawCell(excludedColumn, "xl/worksheets/sheet1.xml", "X3", "e", "#N/A");
        AssertEqual(patient, SinglePatient(PatientTableFiles.Read(excludedColumn)),
            "Исключённый столбец «Особенности» с формулами или ошибкой Excel мешает импорту.");
        ChangeRawCell(excludedColumn, "xl/worksheets/sheet1.xml", "M2", "n", "2", formula: "1+1");
        PatientImportEntry formulaError = PatientTableFiles.Read(excludedColumn).Single();
        Assert(formulaError.Patient is null && !string.IsNullOrEmpty(formulaError.Error) && !formulaError.IsDuplicate
            && formulaError.PatientId == patient.PatientId,
            "Формула в клиническом показателе принята или ошибка потеряла идентификатор пациента.");
        ChangeCell(excludedColumn, "xl/worksheets/sheet1.xml", "M2", "");
        ChangeCell(excludedColumn, "xl/worksheets/sheet1.xml", "E2", "bad height");
        PatientImportEntry validationError = PatientTableFiles.Read(excludedColumn).Single();
        Assert(validationError.Patient is null && !string.IsNullOrEmpty(validationError.Error) && !validationError.IsDuplicate
            && validationError.PatientId == patient.PatientId,
            "Ошибка в характеристиках потеряла идентификатор пациента.");
    }

    private static void VerifyDatabase(string directory)
    {
        string database = Path.Combine(directory, "synthetic.db");
        CreateDatabase(database);
        PatientStore store = new(database);
        PatientRecord first = CreatePatient("  Пациент Ё  ", 1);
        PatientRecord second = CreatePatient("000701", 2);
        PatientSaveResult added = store.Save(new[] { first, second }, overwriteExisting: false);
        Assert(added.Added == 2 && added.Replaced == 0 && added.Skipped == 0, "Новые пациенты сохранены с неверным результатом.");
        Assert(store.GetExistingIds().Count == 2, "Список существующих пациентов неверен.");
        AssertEqual(first, FindPatient(store, first.PatientId), "SQLite потерял исходные характеристики или измерения.");
        AssertEqual(second, FindPatient(store, second.PatientId), "SQLite потерял нули, текст или ведущие нули идентификатора.");

        PatientRecord replacement = CreatePatient(" пациент ё ", 9);
        PatientSaveResult skipped = store.Save(new[] { replacement }, overwriteExisting: false);
        Assert(skipped.Added == 0 && skipped.Replaced == 0 && skipped.Skipped == 1, "Совпадение регистра/пробелов не распознано.");
        AssertEqual(first, FindPatient(store, first.PatientId), "Пропуск совпадающего пациента изменил прежние данные.");

        // A legacy extra indicator must disappear on explicit full replacement.
        Execute(database, "INSERT INTO Test (PersonID, TestName, Day0) VALUES ('Пациент Ё', 'Старый показатель', '123');");
        PatientSaveResult replaced = store.Save(new[] { replacement }, overwriteExisting: true);
        Assert(replaced.Added == 0 && replaced.Replaced == 1 && replaced.Skipped == 0, "Явная перезапись не заменяет пациента.");
        Assert(Scalar(database, "SELECT COUNT(*) FROM Test WHERE TestName = 'Старый показатель';") == 0,
            "Полная перезапись сохранила прежние показатели.");
        AssertEqual(replacement, FindPatient(store, replacement.PatientId), "Явная перезапись не обновила весь профиль пациента.");
        Assert(Scalar(database, "SELECT COUNT(*) FROM Test;") == 138, "Перезапись накопила дубли измерений.");

        AssertRejects(() => store.Save(new[] { CreatePatient("Повтор", 1), CreatePatient(" повтор ", 2) }, false),
            "Два совпадающих пациента в одной операции сохранения разрешены.");
        Assert(store.GetExistingIds().Count == 2, "Отвергнутый пакет частично попал в базу.");
        AssertRejects(() => store.Save(new[] { CreatePatient("Новый", 1), AlterCharacteristics(CreatePatient("Ошибка", 1), 2, "bad") }, false),
            "Неверное числовое поле разрешено при сохранении.");
        Assert(store.GetExistingIds().Count == 2, "Валидация пакета оставила частично сохранённого пациента.");

        Execute(database, "CREATE TRIGGER fail_update4 BEFORE INSERT ON Test WHEN NEW.PersonID = 'пациент ё' BEGIN SELECT RAISE(ABORT, 'synthetic rollback check'); END;");
        AssertRejects(() => store.Save(new[] { CreatePatient("После сбоя", 1), CreatePatient("пациент ё", 8) }, true),
            "Искусственный сбой записи не распространился из транзакции.");
        Execute(database, "DROP TRIGGER fail_update4;");
        Assert(store.GetExistingIds().Count == 2, "После сбоя транзакции осталась часть нового пакета.");
        AssertEqual(replacement, FindPatient(store, replacement.PatientId), "После сбоя перезаписи прежние данные пациента повреждены.");

        store.Delete(" ПАЦИЕНТ Ё ");
        Assert(store.ReadAll().Count == 1 && Scalar(database, "SELECT COUNT(*) FROM Test;") == 69,
            "Удаление не нормализует пациента или оставляет его измерения.");
        Execute(database, "INSERT INTO Test SELECT * FROM Test LIMIT 1;");
        AssertRejects(() => store.ReadAll(), "Дубли лабораторных строк в старой базе незаметно объединены.");
    }

    private static PatientRecord CreatePatient(string id, int seed)
    {
        string[] characteristics = { id, "Ремаксол 2", "28", "жен", "185.5", "85.25", "Жалобы; кавычки \"текст\"\nвторая строка", "3 дня", "Диагноз А, Б", "Дополнительный диагноз", "Операция" };
        Dictionary<string, string[]> measurements = new(StringComparer.Ordinal);
        for (int test = 0; test < PatientTableSchema.TestNames.Length; test++)
        {
            string[] values = Enumerable.Range(0, 11).Select(day => (seed * 100m + test + day / 100m).ToString(CultureInfo.InvariantCulture)).ToArray();
            measurements.Add(PatientTableSchema.TestNames[test], values);
        }
        measurements[PatientTableSchema.TestNames[0]][0] = "";
        measurements[PatientTableSchema.TestNames[0]][1] = "0";
        measurements[PatientTableSchema.TestNames[0]][2] = "-3,25";
        measurements[PatientTableSchema.TestNames[1]][0] = "А/Д";
        measurements[PatientTableSchema.TestNames[1]][1] = "Т";
        measurements[PatientTableSchema.TestNames[1]][2] = "<5";
        measurements[PatientTableSchema.TestNames[1]][3] = "Текст; \"с кавычками\"\nи переносом";
        return new PatientRecord(characteristics, measurements);
    }

    private static PatientRecord AlterCharacteristics(PatientRecord patient, int index, string value)
    {
        string[] characteristics = patient.Characteristics.ToArray();
        characteristics[index] = value;
        return new PatientRecord(characteristics, patient.Measurements);
    }

    private static string[][] CreateTable(PatientRecord patient)
    {
        string[][] rows = Enumerable.Range(0, 70).Select(_ => Enumerable.Repeat("", 23).ToArray()).ToArray();
        Array.Copy(PatientTableSchema.PatientHeaders, rows[0], 11);
        Array.Copy(patient.Characteristics, rows[1], 11);
        rows[0][11] = "Анализы и показатели";
        Array.Copy(PatientTableSchema.TimeHeaders, 0, rows[0], 12, 11);
        for (int index = 0; index < PatientTableSchema.TestNames.Length; index++)
        {
            string name = PatientTableSchema.TestNames[index];
            rows[index + 1][11] = name;
            Array.Copy(patient.Measurements[name], 0, rows[index + 1], 12, 11);
        }
        return rows;
    }

    private static string SerializeTable(IEnumerable<string[]> rows, char separator)
        => string.Join("\r\n", rows.Select(row => string.Join(separator, row.Select(value => "\"" + value.Replace("\"", "\"\"") + "\""))));

    private static PatientRecord SinglePatient(IReadOnlyList<PatientImportEntry> entries)
    {
        Assert(entries.Count == 1 && entries[0].Patient is not null && string.IsNullOrEmpty(entries[0].Error),
            "Ожидался один корректный пациент: " + string.Join("; ", entries.Select(entry => entry.Error)));
        return entries[0].Patient!;
    }

    private static PatientRecord FindPatient(PatientStore store, string id)
        => store.ReadAll().Single(patient => string.Equals(patient.PatientId, id.Trim(), StringComparison.OrdinalIgnoreCase));

    private static void AssertEqual(PatientRecord expected, PatientRecord actual, string message)
    {
        Assert(expected.PatientId == actual.PatientId, message + " Идентификатор изменён.");
        string[] expectedCharacteristics = expected.Characteristics.ToArray();
        string[] actualCharacteristics = actual.Characteristics.ToArray();
        expectedCharacteristics[0] = expectedCharacteristics[0].Trim();
        actualCharacteristics[0] = actualCharacteristics[0].Trim();
        Assert(expectedCharacteristics.SequenceEqual(actualCharacteristics), message + " Характеристики изменены.");
        Assert(expected.Measurements.Count == actual.Measurements.Count, message + " Число показателей изменено.");
        foreach ((string name, string[] values) in expected.Measurements)
            Assert(actual.Measurements.TryGetValue(name, out string[]? actualValues) && values.SequenceEqual(actualValues), message + " Показатель: " + name);
    }

    private static void AssertError(IReadOnlyList<PatientImportEntry> entries, string message)
        => Assert(entries.Count > 0 && entries.All(entry => entry.Patient is null && !string.IsNullOrEmpty(entry.Error)), message);

    private static void AssertDuplicates(IReadOnlyList<PatientImportEntry> entries, string patientId, int count)
        => Assert(entries.Count == count && entries.All(entry => entry.Patient is null && !string.IsNullOrEmpty(entry.Error)
            && entry.IsDuplicate && string.Equals(entry.PatientId, patientId, StringComparison.OrdinalIgnoreCase)),
            "Повторы внутри книги неверно подсчитаны, потеряли идентификатор или один лист ошибочно разрешён.");

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertRejects(Action action, string message, string? requiredMessageText = null)
    {
        try { action(); }
        catch (Exception exception)
        {
            Assert(requiredMessageText is null || exception.Message.Contains(requiredMessageText, StringComparison.OrdinalIgnoreCase),
                message + " В ошибке нет инструкции: " + requiredMessageText);
            return;
        }
        throw new InvalidOperationException(message);
    }

    private static string Address(int column, int row)
    {
        string letters = "";
        while (column > 0) { column--; letters = (char)('A' + column % 26) + letters; column /= 26; }
        return letters + row.ToString(CultureInfo.InvariantCulture);
    }

    private static string Cell(IReadOnlyDictionary<string, string> cells, string address)
        => cells.TryGetValue(address, out string? value) ? value : "";

    private static Dictionary<string, string> ReadCells(ZipArchive archive, string sheetName)
    {
        string[] shared = Array.Empty<string>();
        ZipArchiveEntry? sharedEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedEntry is not null)
        {
            using Stream sharedStream = sharedEntry.Open();
            shared = XDocument.Load(sharedStream).Descendants(Spreadsheet + "si")
                .Select(element => string.Concat(element.Descendants(Spreadsheet + "t").Select(text => text.Value))).ToArray();
        }
        using Stream stream = archive.GetEntry(sheetName)!.Open();
        return XDocument.Load(stream).Descendants(Spreadsheet + "c").ToDictionary(
            cell => cell.Attribute("r")!.Value,
            cell => cell.Attribute("t")?.Value switch
            {
                "s" => shared[int.Parse(cell.Element(Spreadsheet + "v")!.Value, CultureInfo.InvariantCulture)],
                "inlineStr" => string.Concat(cell.Descendants(Spreadsheet + "t").Select(element => element.Value)),
                _ => cell.Element(Spreadsheet + "v")?.Value ?? ""
            }, StringComparer.Ordinal);
    }

    private static void ChangeCell(string workbook, string sheetName, string address, string value)
        => ChangeXmlPart(workbook, sheetName, document =>
        {
            XElement cell = FindOrAddCell(document, address);
            cell.RemoveNodes();
            cell.SetAttributeValue("t", "inlineStr");
            cell.Add(new XElement(Spreadsheet + "is", new XElement(Spreadsheet + "t", value)));
        });

    private static void ChangeRawCell(string workbook, string sheetName, string address, string type, string value, string? formula = null)
        => ChangeXmlPart(workbook, sheetName, document =>
        {
            XElement cell = FindOrAddCell(document, address);
            cell.RemoveNodes();
            cell.SetAttributeValue("t", type);
            if (formula is not null) cell.Add(new XElement(Spreadsheet + "f", formula));
            cell.Add(new XElement(Spreadsheet + "v", value));
        });

    private static XElement FindOrAddCell(XDocument document, string address)
    {
        XElement? cell = document.Descendants(Spreadsheet + "c").SingleOrDefault(element => element.Attribute("r")?.Value == address);
        if (cell is not null) return cell;
        string rowNumber = new string(address.Where(char.IsAsciiDigit).ToArray());
        XElement data = document.Root!.Element(Spreadsheet + "sheetData")!;
        XElement? row = data.Elements(Spreadsheet + "row").SingleOrDefault(element => element.Attribute("r")?.Value == rowNumber);
        if (row is null) { row = new XElement(Spreadsheet + "row", new XAttribute("r", rowNumber)); data.Add(row); }
        cell = new XElement(Spreadsheet + "c", new XAttribute("r", address));
        row.Add(cell);
        return cell;
    }

    private static void ChangeXmlPart(string workbook, string partName, Action<XDocument> change)
    {
        using ZipArchive archive = ZipFile.Open(workbook, ZipArchiveMode.Update);
        ZipArchiveEntry entry = archive.GetEntry(partName)!;
        XDocument document;
        using (Stream stream = entry.Open()) document = XDocument.Load(stream);
        change(document);
        entry.Delete();
        using Stream output = archive.CreateEntry(partName).Open();
        document.Save(output);
    }

    private static void CreateDatabase(string database)
        => Execute(database, """
            CREATE TABLE Person (PersonID TEXT, PatientGroup TEXT, Age INTEGER, Sex TEXT, Height REAL, Weight REAL,
                Complaints TEXT, Duration TEXT, Diagnosis TEXT, AddDiagnosis TEXT, Operation TEXT);
            CREATE TABLE Test (PersonID TEXT, TestName TEXT, Day0 TEXT, Day1 TEXT, Day2 TEXT, Day3 TEXT, Day4 TEXT,
                Day5 TEXT, Day6 TEXT, Day7 TEXT, Day8 TEXT, Day9_12 TEXT, Day12_16 TEXT);
            """);

    private static void Execute(string database, string sql)
    {
        using SqliteConnection connection = new(new SqliteConnectionStringBuilder { DataSource = database }.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static long Scalar(string database, string sql)
    {
        using SqliteConnection connection = new(new SqliteConnectionStringBuilder { DataSource = database }.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
