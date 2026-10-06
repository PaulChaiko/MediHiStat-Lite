using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace MediHiStat
{
    public sealed class PatientImportEntry
    {
        public PatientImportEntry(string sourceName, PatientRecord? patient = null, string? error = null, bool isEmpty = false,
            bool isDuplicate = false, string? patientId = null)
        {
            SourceName = sourceName;
            Patient = patient;
            Error = error;
            IsEmpty = isEmpty;
            IsDuplicate = isDuplicate;
            PatientId = patient?.PatientId ?? patientId?.Trim() ?? string.Empty;
        }

        public string SourceName { get; }
        public PatientRecord? Patient { get; }
        public string? Error { get; }
        public bool IsEmpty { get; }
        public bool IsDuplicate { get; }
        public string PatientId { get; }
    }

    /// <summary>
    /// Reads the patient's labelled table rather than fixed cell positions.
    /// XLSX values are literal strings; no formula is evaluated on import or export.
    /// </summary>
    public static class PatientTableFiles
    {
        private const int MaximumSheets = 1000;
        private const int MaximumRows = 10000;
        private const int MaximumColumns = 256;
        private const int MaximumCells = 1000000;
        private const long MaximumPartBytes = 50 * 1024 * 1024;
        private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
        private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";

        public static IReadOnlyList<PatientImportEntry> Read(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".xlsx") return ReadWorkbook(path);
            if (extension is ".csv" or ".tsv")
            {
                if (new FileInfo(path).Length > MaximumPartBytes)
                    throw new FormatException("Таблица превышает допустимый размер 50 МБ.");
                return ReadText(File.ReadAllText(path, new UTF8Encoding(false, true)), Path.GetFileName(path));
            }
            throw new FormatException("Выберите книгу XLSX или таблицу CSV. Формат XLS не поддерживается: сохраните его как XLSX.");
        }

        public static IReadOnlyList<PatientImportEntry> ReadText(string text, string sourceName = "Буфер обмена")
        {
            ArgumentNullException.ThrowIfNull(text);
            try
            {
                if (text.Length > MaximumPartBytes)
                    throw new FormatException("Таблица превышает допустимый размер.");
                string content = text.TrimStart('\uFEFF');
                string[][] rows = ParseDelimited(content, DetectDelimiter(content));
                return new[] { ParseTable(rows, sourceName).Entry };
            }
            catch (FormatException exception)
            {
                return new[] { new PatientImportEntry(sourceName, error: exception.Message) };
            }
        }

        public static void WriteTemplate(string path, int sheetCount = 1)
        {
            if (sheetCount < 1 || sheetCount > MaximumSheets)
                throw new ArgumentOutOfRangeException(nameof(sheetCount), $"Количество листов: от 1 до {MaximumSheets}.");
            string extension = GetOutputExtension(path);
            if (extension == ".csv" && sheetCount != 1)
                throw new FormatException("CSV содержит одного пациента. Для нескольких листов выберите XLSX.");
            WriteAtomically(path, stream =>
            {
                if (extension == ".csv") WriteCsv(stream, CreateTable(null));
                else WriteWorkbook(stream, Enumerable.Range(1, sheetCount).Select(index => ($"Пациент {index}", (PatientRecord?)null)).ToArray());
            });
        }

        public static void Write(string path, IReadOnlyList<PatientRecord> patients)
        {
            ArgumentNullException.ThrowIfNull(patients);
            if (patients.Count < 1 || patients.Count > MaximumSheets)
                throw new FormatException($"Выберите от 1 до {MaximumSheets} пациентов.");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PatientRecord patient in patients)
            {
                PatientTableSchema.ValidateRecord(patient);
                if (!ids.Add(patient.PatientId))
                    throw new FormatException($"Пациент «{patient.PatientId}» выбран несколько раз.");
            }
            string extension = GetOutputExtension(path);
            if (extension == ".csv" && patients.Count != 1)
                throw new FormatException("Для нескольких пациентов выберите XLSX: каждому пациенту будет выделен лист.");
            if (extension == ".csv")
            {
                string[][] table = CreateTable(patients[0]);
                if (table.SelectMany(row => row).Any(IsCsvFormulaRisk))
                    throw new FormatException("Данные содержат текст, который Excel может выполнить как формулу при открытии CSV. Выберите XLSX: он сохранит эти значения как текст без изменений.");
                WriteAtomically(path, stream => WriteCsv(stream, table));
            }
            else
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var sheets = patients.Select(patient => (MakeSheetName(patient.PatientId, names), (PatientRecord?)patient)).ToArray();
                WriteAtomically(path, stream => WriteWorkbook(stream, sheets));
            }
        }

        private static IReadOnlyList<PatientImportEntry> ReadWorkbook(string path)
        {
            using var stream = File.OpenRead(path);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            if (archive.Entries.GroupBy(entry => entry.FullName, StringComparer.Ordinal).Any(group => group.Count() > 1))
                throw new FormatException("Книга содержит повторяющиеся части. Пересохраните её как новую книгу XLSX.");
            XDocument workbook = ReadXml(archive, "xl/workbook.xml");
            XDocument relationships = ReadXml(archive, "xl/_rels/workbook.xml.rels");
            var targets = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (XElement relationship in relationships.Root?.Elements(PackageRelationships + "Relationship") ?? Enumerable.Empty<XElement>())
            {
                string id = (string?)relationship.Attribute("Id") ?? string.Empty;
                if ((string?)relationship.Attribute("TargetMode") == "External") continue;
                string target = (string?)relationship.Attribute("Target") ?? string.Empty;
                if (!targets.TryAdd(id, ResolvePart("xl/workbook.xml", target)))
                    throw new FormatException("Книга содержит повторяющиеся связи листов.");
            }
            XElement[] sheets = workbook.Root?.Element(Spreadsheet + "sheets")?.Elements(Spreadsheet + "sheet").ToArray() ?? Array.Empty<XElement>();
            if (sheets.Length == 0 || sheets.Length > MaximumSheets)
                throw new FormatException($"В книге должно быть от 1 до {MaximumSheets} листов.");
            string[] sharedStrings = ReadSharedStrings(archive);
            Dictionary<int, string> formats = ReadNumberFormats(archive);
            var results = new List<(PatientImportEntry Entry, string Id)>();
            foreach (XElement sheet in sheets)
            {
                string name = (string?)sheet.Attribute("name") ?? "Лист";
                try
                {
                    string relationshipId = (string?)sheet.Attribute(OfficeRelationships + "id") ?? string.Empty;
                    if (!targets.TryGetValue(relationshipId, out string? part))
                        throw new FormatException("Не удалось найти данные листа.");
                    string[][] table = ReadWorksheet(archive, part, sharedStrings, formats, out string? worksheetError);
                    var result = ParseTable(table, name);
                    results.Add(worksheetError is null ? result : (new PatientImportEntry(name, error: worksheetError, patientId: result.Id), result.Id));
                }
                catch (Exception exception) when (exception is FormatException or XmlException or InvalidDataException)
                {
                    results.Add((new PatientImportEntry(name, error: exception.Message), string.Empty));
                }
            }
            var duplicateIds = results.Where(result => result.Id.Length > 0)
                .GroupBy(result => result.Id, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1)
                .ToDictionary(group => group.Key, group => string.Join(", ", group.Select(result => result.Entry.SourceName)), StringComparer.OrdinalIgnoreCase);
            return results.Select(result => duplicateIds.TryGetValue(result.Id, out string? sources)
                ? new PatientImportEntry(result.Entry.SourceName, error: $"Пациент «{result.Id}» повторяется внутри книги (листы: {sources}). Устраните повторения перед импортом.",
                    isDuplicate: true, patientId: result.Id)
                : result.Entry).ToArray();
        }

        private static (PatientImportEntry Entry, string Id) ParseTable(IReadOnlyList<string[]> rows, string sourceName)
        {
            string patientId = string.Empty;
            try
            {
                if (rows.All(row => row.All(string.IsNullOrWhiteSpace)))
                    return (new PatientImportEntry(sourceName, isEmpty: true), string.Empty);
                int headerRow = -1;
                for (int index = 0; index < rows.Count; index++)
                {
                    if (rows[index].Any(value => PatientTableSchema.PatientHeaderIndex(value) == 0)
                        && rows[index].Any(value => PatientTableSchema.NormalizeLabel(value) == "АНАЛИЗЫ И ПОКАЗАТЕЛИ"))
                    {
                        if (headerRow >= 0) throw new FormatException("Найдено несколько схем на одном листе. Один лист должен содержать одного пациента.");
                        headerRow = index;
                    }
                }
                if (headerRow < 0)
                    throw new FormatException("Схема не распознана: требуются заголовки «Пациент», «Анализы и показатели» и сроки наблюдения.");
                string[] headers = rows[headerRow];
                int identityColumn = Array.FindIndex(headers, value => PatientTableSchema.PatientHeaderIndex(value) == 0);
                patientId = Cell(rows, headerRow + 1, identityColumn).Trim();
                int[] passportColumns = Enumerable.Repeat(-1, PatientTableSchema.PatientHeaders.Length).ToArray();
                int[] timeColumns = Enumerable.Repeat(-1, PatientTableSchema.TimeHeaders.Length).ToArray();
                int testColumn = -1;
                var ignoredColumns = new HashSet<int>();
                for (int column = 0; column < headers.Length; column++)
                {
                    string header = headers[column];
                    int passportIndex = PatientTableSchema.PatientHeaderIndex(header);
                    int timeIndex = PatientTableSchema.TimeHeaderIndex(header);
                    string label = PatientTableSchema.NormalizeLabel(header);
                    if (passportIndex >= 0)
                    {
                        if (passportColumns[passportIndex] >= 0) throw new FormatException($"Повторяется поле «{header}».");
                        passportColumns[passportIndex] = column;
                    }
                    else if (timeIndex >= 0)
                    {
                        if (timeColumns[timeIndex] >= 0) throw new FormatException($"Повторяется срок «{header}».");
                        timeColumns[timeIndex] = column;
                    }
                    else if (label == "АНАЛИЗЫ И ПОКАЗАТЕЛИ")
                    {
                        if (testColumn >= 0) throw new FormatException("Повторяется столбец «Анализы и показатели».");
                        testColumn = column;
                    }
                    else if (label == "ОСОБЕННОСТИ") ignoredColumns.Add(column);
                    else if (!string.IsNullOrWhiteSpace(header)) throw new FormatException($"Неизвестный заголовок поля или срока «{header}».");
                }
                string[] missingFields = passportColumns.Select((column, index) => (column, index)).Where(item => item.column < 0)
                    .Select(item => PatientTableSchema.PatientHeaders[item.index]).ToArray();
                if (missingFields.Length > 0) throw new FormatException($"Отсутствуют поля пациента: {string.Join(", ", missingFields)}.");
                string[] missingTimes = timeColumns.Select((column, index) => (column, index)).Where(item => item.column < 0)
                    .Select(item => PatientTableSchema.TimeHeaders[item.index]).ToArray();
                if (missingTimes.Length > 0) throw new FormatException($"Отсутствуют сроки: {string.Join(", ", missingTimes)}.");
                string[] characteristics = passportColumns.Select(column => Cell(rows, headerRow + 1, column)).ToArray();
                patientId = characteristics[0].Trim();
                var recognizedColumns = new HashSet<int>(passportColumns.Concat(timeColumns).Append(testColumn).Concat(ignoredColumns));
                var measurements = new Dictionary<string, string[]>(StringComparer.Ordinal);
                for (int row = headerRow + 1; row < rows.Count; row++)
                {
                    for (int column = 0; column < rows[row].Length; column++)
                    {
                        if (!recognizedColumns.Contains(column) && !string.IsNullOrWhiteSpace(rows[row][column]))
                            throw new FormatException($"В столбце {ColumnName(column)} есть данные без распознанного заголовка.");
                    }
                    if (row > headerRow + 1 && passportColumns.Any(column => !string.IsNullOrWhiteSpace(Cell(rows, row, column))))
                        throw new FormatException($"Характеристики пациента должны находиться в одной строке под заголовками (строка {row + 1}).");
                    string testName = Cell(rows, row, testColumn);
                    string[] values = timeColumns.Select(column => Cell(rows, row, column)).ToArray();
                    if (string.IsNullOrWhiteSpace(testName))
                    {
                        if (values.Any(value => !string.IsNullOrWhiteSpace(value)))
                            throw new FormatException($"В строке {row + 1} есть значения без названия показателя.");
                        continue;
                    }
                    string? canonical = PatientTableSchema.CanonicalTestName(testName);
                    if (canonical is null) throw new FormatException($"Неизвестный показатель «{testName}» (строка {row + 1}).");
                    if (!measurements.TryAdd(canonical, values)) throw new FormatException($"Повторяется показатель «{canonical}».");
                }
                string[] missingTests = PatientTableSchema.TestNames.Where(name => !measurements.ContainsKey(name)).ToArray();
                if (missingTests.Length > 0) throw new FormatException($"Отсутствуют показатели: {string.Join(", ", missingTests)}.");
                if (characteristics.All(string.IsNullOrWhiteSpace) && measurements.Values.All(values => values.All(string.IsNullOrWhiteSpace)))
                    return (new PatientImportEntry(sourceName, isEmpty: true), string.Empty);
                var patient = new PatientRecord(characteristics, measurements);
                PatientTableSchema.ValidateRecord(patient);
                return (new PatientImportEntry(sourceName, patient), patientId);
            }
            catch (FormatException exception)
            {
                return (new PatientImportEntry(sourceName, error: exception.Message, patientId: patientId), patientId);
            }
        }

        private static string Cell(IReadOnlyList<string[]> rows, int row, int column) =>
            row >= 0 && row < rows.Count && column >= 0 && column < rows[row].Length ? rows[row][column] : string.Empty;

        private static string[][] ReadWorksheet(ZipArchive archive, string part, string[] sharedStrings, Dictionary<int, string> formats, out string? error)
        {
            XDocument document = ReadXml(archive, part);
            var cells = new Dictionary<(int Row, int Column), string>();
            var sourceCells = new Dictionary<(int Row, int Column), XElement>();
            var errors = new List<string>();
            // Find the labelled header before reading values: excluded «Особенности»
            // contents are deliberately ignored even when Excel stores formulas/errors there.
            foreach (XElement cell in document.Root?.Element(Spreadsheet + "sheetData")?.Elements(Spreadsheet + "row")
                .SelectMany(row => row.Elements(Spreadsheet + "c")) ?? Enumerable.Empty<XElement>())
            {
                string address = (string?)cell.Attribute("r") ?? string.Empty;
                var position = ParseAddress(address);
                if (!sourceCells.TryAdd(position, cell)) throw new FormatException($"Повторяется ячейка {address}.");
                if (sourceCells.Count > MaximumCells) throw new FormatException("На листе слишком много ячеек.");
            }
            int headerRow = -1;
            foreach (var row in sourceCells.GroupBy(item => item.Key.Row))
            {
                string[] labels = row.Select(item => PeekCell(item.Value, sharedStrings)).ToArray();
                if (labels.Any(value => PatientTableSchema.PatientHeaderIndex(value) == 0)
                    && labels.Any(value => PatientTableSchema.NormalizeLabel(value) == "АНАЛИЗЫ И ПОКАЗАТЕЛИ"))
                { headerRow = row.Key; break; }
            }
            var ignoredColumns = sourceCells.Where(item => item.Key.Row == headerRow
                && PatientTableSchema.NormalizeLabel(PeekCell(item.Value, sharedStrings)) == "ОСОБЕННОСТИ")
                .Select(item => item.Key.Column).ToHashSet();
            int rowCount = 0, columnCount = 0;
            foreach (var item in sourceCells)
            {
                XElement cell = item.Value;
                (int rowIndex, int columnIndex) = item.Key;
                string value;
                if (ignoredColumns.Contains(columnIndex) && rowIndex >= headerRow)
                    value = rowIndex == headerRow ? "Особенности" : string.Empty;
                else
                {
                    string address = (string?)cell.Attribute("r") ?? string.Empty;
                    try { value = ReadCell(cell, sharedStrings, formats, address); }
                    catch (FormatException exception)
                    {
                        errors.Add(exception.Message);
                        // Preserve identity for duplicate checks, but never return this sheet as valid.
                        value = PeekCell(cell, sharedStrings);
                    }
                }
                cells.Add((rowIndex, columnIndex), value);
                // Formatting-only empty cells do not enlarge the table or change its schema.
                if (value.Length > 0)
                {
                    rowCount = Math.Max(rowCount, rowIndex + 1);
                    columnCount = Math.Max(columnCount, columnIndex + 1);
                }
            }
            if ((long)rowCount * columnCount > MaximumCells) throw new FormatException("Размер листа превышает допустимый предел.");
            var result = new string[rowCount][];
            for (int row = 0; row < rowCount; row++)
            {
                result[row] = Enumerable.Repeat(string.Empty, columnCount).ToArray();
                for (int column = 0; column < columnCount; column++)
                    if (cells.TryGetValue((row, column), out string? value)) result[row][column] = value;
            }
            error = errors.Count == 0 ? null : string.Join(Environment.NewLine, errors.Distinct().Take(10));
            return result;
        }

        private static string PeekCell(XElement cell, string[] sharedStrings)
        {
            string type = (string?)cell.Attribute("t") ?? "n";
            string value = cell.Element(Spreadsheet + "v")?.Value ?? string.Empty;
            if (type == "inlineStr") return ReadRichText(cell.Element(Spreadsheet + "is"));
            if (type == "s") return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                && index >= 0 && index < sharedStrings.Length ? sharedStrings[index] : string.Empty;
            return DecodeSpreadsheetText(value);
        }

        private static string ReadCell(XElement cell, string[] sharedStrings, Dictionary<int, string> formats, string address)
        {
            if (cell.Element(Spreadsheet + "f") is not null)
                throw new FormatException($"Ячейка {address} содержит формулу. Замените её вычисленным значением перед импортом.");
            string type = (string?)cell.Attribute("t") ?? "n";
            string value = cell.Element(Spreadsheet + "v")?.Value ?? string.Empty;
            if (type == "inlineStr") return ReadRichText(cell.Element(Spreadsheet + "is"));
            if (type == "s")
            {
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int index) || index < 0 || index >= sharedStrings.Length)
                    throw new FormatException($"Недопустимая ссылка на текст в ячейке {address}.");
                return sharedStrings[index];
            }
            if (type == "e") throw new FormatException($"В ячейке {address} ошибка Excel: {value}.");
            if (type == "b") return value switch { "0" => "FALSE", "1" => "TRUE", _ => throw new FormatException($"Недопустимое логическое значение в ячейке {address}.") };
            if (type is "str" or "d") return DecodeSpreadsheetText(value);
            if (type != "n") throw new FormatException($"Неизвестный тип ячейки {address}: {type}.");
            if (value.Length == 0) return string.Empty;
            if (int.TryParse((string?)cell.Attribute("s"), NumberStyles.None, CultureInfo.InvariantCulture, out int style)
                && formats.TryGetValue(style, out string? format))
            {
                if (format == "#DATE#")
                    throw new FormatException($"Ячейка {address} хранится в Excel как дата. Чтобы не импортировать серийный номер вместо клинического значения, задайте текстовый формат и проверьте значение.");
                if (Regex.IsMatch(format, @"^0+$") && value.All(char.IsAsciiDigit))
                    return value.PadLeft(format.Length, '0');
            }
            return value;
        }

        private static string[] ReadSharedStrings(ZipArchive archive)
        {
            if (archive.GetEntry("xl/sharedStrings.xml") is null) return Array.Empty<string>();
            return ReadXml(archive, "xl/sharedStrings.xml").Root?.Elements(Spreadsheet + "si").Select(ReadRichText).ToArray() ?? Array.Empty<string>();
        }

        private static string ReadRichText(XElement? element) => element is null ? string.Empty : DecodeSpreadsheetText(string.Concat(
            element.Descendants(Spreadsheet + "t").Where(text => !text.Ancestors(Spreadsheet + "rPh").Any()).Select(text => text.Value)));

        private static Dictionary<int, string> ReadNumberFormats(ZipArchive archive)
        {
            var result = new Dictionary<int, string>();
            if (archive.GetEntry("xl/styles.xml") is null) return result;
            XDocument document = ReadXml(archive, "xl/styles.xml");
            var custom = new Dictionary<int, string>();
            foreach (XElement item in document.Root?.Element(Spreadsheet + "numFmts")?.Elements(Spreadsheet + "numFmt") ?? Enumerable.Empty<XElement>())
                if (int.TryParse((string?)item.Attribute("numFmtId"), out int id)) custom[id] = (string?)item.Attribute("formatCode") ?? string.Empty;
            int index = 0;
            foreach (XElement item in document.Root?.Element(Spreadsheet + "cellXfs")?.Elements(Spreadsheet + "xf") ?? Enumerable.Empty<XElement>())
            {
                if (int.TryParse((string?)item.Attribute("numFmtId"), out int id))
                {
                    string format = custom.TryGetValue(id, out string? code) ? code : string.Empty;
                    // Quoted labels and escaped characters are not date/time tokens.
                    string tokens = Regex.Replace(format, "\"[^\"]*\"|\\\\.|\\[[^\\]]*\\]", string.Empty);
                    bool date = (id >= 14 && id <= 22) || (id >= 45 && id <= 47) || Regex.IsMatch(tokens, "[ymdhs]", RegexOptions.IgnoreCase);
                    result[index] = date ? "#DATE#" : format;
                }
                index++;
            }
            return result;
        }

        private static XDocument ReadXml(ZipArchive archive, string name)
        {
            ZipArchiveEntry entry = archive.GetEntry(name) ?? throw new FormatException($"В книге отсутствует часть {name}.");
            if (entry.Length > MaximumPartBytes) throw new FormatException("Часть книги превышает допустимый размер 50 МБ.");
            using Stream stream = entry.Open();
            using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumPartBytes
            });
            return XDocument.Load(reader);
        }

        private static string ResolvePart(string basePart, string target)
        {
            if (target.Contains('\\') || target.Contains(':') || target.Contains('#') || target.Contains('?'))
                throw new FormatException("Недопустимый адрес части книги.");
            string path = target.StartsWith('/') ? target.TrimStart('/') : basePart[..(basePart.LastIndexOf('/') + 1)] + target;
            var parts = new List<string>();
            foreach (string component in path.Split('/'))
            {
                if (component is "" or ".") continue;
                if (component == "..")
                {
                    if (parts.Count == 0) throw new FormatException("Недопустимый адрес части книги.");
                    parts.RemoveAt(parts.Count - 1);
                }
                else parts.Add(Uri.UnescapeDataString(component));
            }
            return string.Join("/", parts);
        }

        private static (int Row, int Column) ParseAddress(string address)
        {
            Match match = Regex.Match(address, @"^([A-Za-z]+)([1-9][0-9]*)$");
            if (!match.Success || !int.TryParse(match.Groups[2].Value, out int row) || row > MaximumRows)
                throw new FormatException($"Недопустимый адрес ячейки {address}.");
            int column = 0;
            foreach (char character in match.Groups[1].Value.ToUpperInvariant())
            {
                column = column * 26 + character - 'A' + 1;
                if (column > MaximumColumns) throw new FormatException("Слишком много столбцов на листе.");
            }
            return (row - 1, column - 1);
        }

        private static char DetectDelimiter(string content)
        {
            var counts = new Dictionary<char, int> { ['\t'] = 0, [';'] = 0, [','] = 0 };
            bool quoted = false;
            foreach (char character in content)
            {
                if (character == '"') quoted = !quoted;
                else if (!quoted && character is '\r' or '\n')
                {
                    if (counts.Values.Any(count => count > 0)) break;
                }
                else if (!quoted && counts.ContainsKey(character)) counts[character]++;
            }
            return counts.OrderByDescending(item => item.Value).First().Key;
        }

        private static string[][] ParseDelimited(string content, char delimiter)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var value = new StringBuilder();
            bool quoted = false, closedQuote = false, atStart = true;
            int cellCount = 0;
            void FinishCell()
            {
                row.Add(value.ToString());
                value.Clear();
                atStart = true;
                closedQuote = false;
                if (row.Count > MaximumColumns) throw new FormatException("Слишком много столбцов в таблице.");
            }
            void FinishRow()
            {
                FinishCell();
                cellCount += row.Count;
                rows.Add(row.ToArray());
                row.Clear();
                if (rows.Count > MaximumRows || cellCount > MaximumCells)
                    throw new FormatException("Таблица превышает допустимый размер.");
            }
            for (int index = 0; index < content.Length; index++)
            {
                char character = content[index];
                if (quoted)
                {
                    if (character == '"')
                    {
                        if (index + 1 < content.Length && content[index + 1] == '"') { value.Append('"'); index++; }
                        else { quoted = false; closedQuote = true; }
                    }
                    else value.Append(character);
                    continue;
                }
                if (character == delimiter) { FinishCell(); continue; }
                if (character is '\r' or '\n')
                {
                    if (character == '\r' && index + 1 < content.Length && content[index + 1] == '\n') index++;
                    FinishRow();
                    continue;
                }
                if (closedQuote)
                {
                    if (character is ' ' or '\t') continue;
                    throw new FormatException("После закрывающей кавычки CSV допускается только разделитель или конец строки.");
                }
                if (character == '"')
                {
                    if (!atStart) throw new FormatException("Кавычка внутри поля CSV должна быть удвоена, а поле заключено в кавычки.");
                    quoted = true;
                    atStart = false;
                }
                else { value.Append(character); atStart = false; }
            }
            if (quoted) throw new FormatException("В CSV не закрыта кавычка.");
            if (row.Count > 0 || value.Length > 0 || closedQuote) FinishRow();
            return rows.ToArray();
        }

        private static string[][] CreateTable(PatientRecord? patient)
        {
            int width = PatientTableSchema.PatientHeaders.Length + 1 + PatientTableSchema.TimeHeaders.Length;
            var table = new string[PatientTableSchema.TestNames.Length + 1][];
            for (int row = 0; row < table.Length; row++) table[row] = Enumerable.Repeat(string.Empty, width).ToArray();
            Array.Copy(PatientTableSchema.PatientHeaders, table[0], PatientTableSchema.PatientHeaders.Length);
            table[0][11] = "Анализы и показатели";
            Array.Copy(PatientTableSchema.TimeHeaders, 0, table[0], 12, PatientTableSchema.TimeHeaders.Length);
            if (patient is not null) Array.Copy(patient.Characteristics, table[1], PatientTableSchema.PatientHeaders.Length);
            for (int index = 0; index < PatientTableSchema.TestNames.Length; index++)
            {
                string name = PatientTableSchema.TestNames[index];
                table[index + 1][11] = name;
                if (patient is not null) Array.Copy(patient.Measurements[name], 0, table[index + 1], 12, PatientTableSchema.TimeHeaders.Length);
            }
            return table;
        }

        private static void WriteCsv(Stream stream, string[][] table)
        {
            using var writer = new StreamWriter(stream, new UTF8Encoding(true), 4096, leaveOpen: true);
            writer.NewLine = "\r\n";
            foreach (string[] row in table)
                writer.WriteLine(string.Join(";", row.Select(value => "\"" + value.Replace("\"", "\"\"") + "\"")));
        }

        private static bool IsCsvFormulaRisk(string value)
        {
            string text = value.TrimStart(' ', '\t', '\r', '\n', '\uFEFF', '\u00A0');
            if (text.Length == 0) return false;
            if (text[0] is '=' or '@') return true;
            if (text[0] is not ('+' or '-')) return false;
            return !double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || !double.IsFinite(number);
        }

        private static void WriteWorkbook(Stream stream, IReadOnlyList<(string Name, PatientRecord? Patient)> sheets)
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
            var types = new XElement(ContentTypes + "Types",
                new XElement(ContentTypes + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(ContentTypes + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                new XElement(ContentTypes + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(ContentTypes + "Override", new XAttribute("PartName", "/xl/styles.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")));
            var sheetElements = new XElement(Spreadsheet + "sheets");
            var relationshipElements = new XElement(PackageRelationships + "Relationships");
            for (int index = 0; index < sheets.Count; index++)
            {
                string id = $"rId{index + 1}";
                string part = $"worksheets/sheet{index + 1}.xml";
                sheetElements.Add(new XElement(Spreadsheet + "sheet", new XAttribute("name", sheets[index].Name), new XAttribute("sheetId", index + 1), new XAttribute(OfficeRelationships + "id", id)));
                relationshipElements.Add(new XElement(PackageRelationships + "Relationship", new XAttribute("Id", id), new XAttribute("Type", OfficeRelationships.NamespaceName + "/worksheet"), new XAttribute("Target", part)));
                types.Add(new XElement(ContentTypes + "Override", new XAttribute("PartName", "/xl/" + part), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
                WriteXml(archive, "xl/" + part, CreateWorksheet(CreateTable(sheets[index].Patient)));
            }
            relationshipElements.Add(new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "styles"), new XAttribute("Type", OfficeRelationships.NamespaceName + "/styles"), new XAttribute("Target", "styles.xml")));
            WriteXml(archive, "[Content_Types].xml", types);
            WriteXml(archive, "_rels/.rels", new XElement(PackageRelationships + "Relationships",
                new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", OfficeRelationships.NamespaceName + "/officeDocument"), new XAttribute("Target", "xl/workbook.xml"))));
            WriteXml(archive, "xl/workbook.xml", new XElement(Spreadsheet + "workbook", new XAttribute(XNamespace.Xmlns + "r", OfficeRelationships), sheetElements));
            WriteXml(archive, "xl/_rels/workbook.xml.rels", relationshipElements);
            WriteXml(archive, "xl/styles.xml", CreateStyles());
        }

        private static XElement CreateWorksheet(string[][] table)
        {
            var columns = new XElement(Spreadsheet + "cols");
            double[] widths = { 18, 20, 12, 10, 12, 12, 32, 24, 34, 34, 36, 28 };
            for (int column = 0; column < table[0].Length; column++)
                columns.Add(new XElement(Spreadsheet + "col", new XAttribute("min", column + 1), new XAttribute("max", column + 1),
                    new XAttribute("width", column < widths.Length ? widths[column] : 16), new XAttribute("customWidth", 1)));
            var data = new XElement(Spreadsheet + "sheetData");
            for (int row = 0; row < table.Length; row++)
            {
                var element = new XElement(Spreadsheet + "row", new XAttribute("r", row + 1));
                if (row == 0) { element.Add(new XAttribute("ht", 36)); element.Add(new XAttribute("customHeight", 1)); }
                for (int column = 0; column < table[row].Length; column++)
                    element.Add(new XElement(Spreadsheet + "c", new XAttribute("r", ColumnName(column) + (row + 1).ToString(CultureInfo.InvariantCulture)),
                        new XAttribute("t", "inlineStr"), new XAttribute("s", row == 0 ? 1 : 0),
                        new XElement(Spreadsheet + "is", new XElement(Spreadsheet + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), EncodeSpreadsheetText(table[row][column])))));
                data.Add(element);
            }
            return new XElement(Spreadsheet + "worksheet",
                new XElement(Spreadsheet + "dimension", new XAttribute("ref", $"A1:W{table.Length}")),
                new XElement(Spreadsheet + "sheetViews", new XElement(Spreadsheet + "sheetView", new XAttribute("workbookViewId", 0),
                    new XElement(Spreadsheet + "pane", new XAttribute("ySplit", 1), new XAttribute("topLeftCell", "A2"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
                new XElement(Spreadsheet + "sheetFormatPr", new XAttribute("defaultRowHeight", 20)), columns, data);
        }

        private static XElement CreateStyles()
        {
            XElement Font(bool bold) => new(Spreadsheet + "font", new XElement(Spreadsheet + "sz", new XAttribute("val", 11)),
                new XElement(Spreadsheet + "name", new XAttribute("val", "Calibri")), bold ? new XElement(Spreadsheet + "b") : null);
            XElement Format(int font, int fill) => new(Spreadsheet + "xf", new XAttribute("numFmtId", 49), new XAttribute("fontId", font),
                new XAttribute("fillId", fill), new XAttribute("borderId", 0), new XAttribute("xfId", 0), new XAttribute("applyNumberFormat", 1),
                new XAttribute("applyAlignment", 1), new XElement(Spreadsheet + "alignment", new XAttribute("vertical", "top"), new XAttribute("wrapText", 1)));
            return new XElement(Spreadsheet + "styleSheet",
                new XElement(Spreadsheet + "fonts", new XAttribute("count", 2), Font(false), Font(true)),
                new XElement(Spreadsheet + "fills", new XAttribute("count", 3),
                    new XElement(Spreadsheet + "fill", new XElement(Spreadsheet + "patternFill", new XAttribute("patternType", "none"))),
                    new XElement(Spreadsheet + "fill", new XElement(Spreadsheet + "patternFill", new XAttribute("patternType", "gray125"))),
                    new XElement(Spreadsheet + "fill", new XElement(Spreadsheet + "patternFill", new XAttribute("patternType", "solid"),
                        new XElement(Spreadsheet + "fgColor", new XAttribute("rgb", "FFE7EEF7")), new XElement(Spreadsheet + "bgColor", new XAttribute("indexed", 64))))),
                new XElement(Spreadsheet + "borders", new XAttribute("count", 1), new XElement(Spreadsheet + "border",
                    new XElement(Spreadsheet + "left"), new XElement(Spreadsheet + "right"), new XElement(Spreadsheet + "top"), new XElement(Spreadsheet + "bottom"), new XElement(Spreadsheet + "diagonal"))),
                new XElement(Spreadsheet + "cellStyleXfs", new XAttribute("count", 1), new XElement(Spreadsheet + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 0), new XAttribute("fillId", 0), new XAttribute("borderId", 0))),
                new XElement(Spreadsheet + "cellXfs", new XAttribute("count", 2), Format(0, 0), Format(1, 2)),
                new XElement(Spreadsheet + "cellStyles", new XAttribute("count", 1), new XElement(Spreadsheet + "cellStyle", new XAttribute("name", "Normal"), new XAttribute("xfId", 0), new XAttribute("builtinId", 0))));
        }

        private static void WriteXml(ZipArchive archive, string path, XElement root)
        {
            ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using Stream stream = entry.Open();
            using XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false, CheckCharacters = true });
            new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).Save(writer);
        }

        private static string EncodeSpreadsheetText(string value)
        {
            // Protect literal OOXML escape sequences before encoding XML-forbidden control characters.
            value = Regex.Replace(value, @"_x[0-9a-fA-F]{4}_", match => "_x005F_" + match.Value[1..]);
            var result = new StringBuilder();
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                { result.Append(character); result.Append(value[++index]); }
                else if (character == '\r' || !XmlConvert.IsXmlChar(character))
                    result.Append("_x").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture)).Append('_');
                else result.Append(character);
            }
            return result.ToString();
        }

        private static string DecodeSpreadsheetText(string value) => Regex.Replace(value, @"_x([0-9a-fA-F]{4})_",
            match => ((char)int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString());

        private static string MakeSheetName(string id, HashSet<string> usedNames)
        {
            string baseName = new string(id.Select(character => !XmlConvert.IsXmlChar(character) || "\\/:?*[]".Contains(character) ? '_' : character).ToArray()).Trim().Trim('\'');
            if (baseName.Length == 0) baseName = "Пациент";
            baseName = baseName[..Math.Min(31, baseName.Length)].Trim().Trim('\'');
            if (baseName.Length == 0) baseName = "Пациент";
            if (string.Equals(baseName, "History", StringComparison.OrdinalIgnoreCase)) baseName += "_";
            string name = baseName;
            int number = 2;
            while (!usedNames.Add(name))
            {
                string suffix = $" ({number++})";
                name = baseName[..Math.Min(baseName.Length, 31 - suffix.Length)] + suffix;
            }
            return name;
        }

        private static string ColumnName(int column)
        {
            string result = string.Empty;
            for (int number = column + 1; number > 0; number = (number - 1) / 26) result = (char)('A' + (number - 1) % 26) + result;
            return result;
        }

        private static string GetOutputExtension(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension is not (".xlsx" or ".csv")) throw new FormatException("Для сохранения выберите расширение XLSX или CSV.");
            return extension;
        }

        private static void WriteAtomically(string path, Action<Stream> write)
        {
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath)!;
            string temporary = Path.Combine(directory, "." + Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    write(stream);
                    stream.Flush(flushToDisk: true);
                }
                File.Move(temporary, fullPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
