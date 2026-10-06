using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediHiStat
{
    public sealed class PatientSaveResult
    {
        public int Added { get; }
        public int Replaced { get; }
        public int Skipped { get; }

        public PatientSaveResult(int added, int replaced, int skipped)
        {
            Added = added;
            Replaced = replaced;
            Skipped = skipped;
        }
    }

    /// <summary>
    /// Keeps patient records and their measurements together without changing the existing database schema.
    /// All identity comparisons use trimmed, ordinal case-insensitive identifiers, including Cyrillic.
    /// </summary>
    public sealed class PatientStore
    {
        private const string IdentityCollation = "MEDIHISTAT_PATIENT_ID";
        private const string PersonColumns = "PersonID, PatientGroup, Age, Sex, Height, Weight, Complaints, Duration, Diagnosis, AddDiagnosis, Operation";
        private const string MeasurementColumns = "Day0, Day1, Day2, Day3, Day4, Day5, Day6, Day7, Day8, Day9_12, Day12_16";
        private readonly string databasePath;

        public PatientStore(string databasePath = "mydatabase.db")
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Не указан путь к базе данных.", nameof(databasePath));
            this.databasePath = databasePath;
        }

        public IReadOnlyList<PatientRecord> ReadAll()
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction(deferred: true);
            var patients = new Dictionary<string, ReadState>(StringComparer.OrdinalIgnoreCase);

            using (var command = CreateCommand(connection, transaction, $"SELECT {PersonColumns} FROM Person"))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string[] characteristics = Enumerable.Range(0, 11).Select(index => ReadValue(reader, index)).ToArray();
                    string patientId = characteristics[0].Trim();
                    if (patientId.Length == 0)
                        throw new FormatException("В базе данных есть карточка без значения «Пациент». Исправьте исходные данные.");
                    if (patients.ContainsKey(patientId))
                        throw new FormatException($"В базе данных несколько карточек пациента «{patientId}». Экспорт неоднозначен. Перезапишите пациента подтверждённой полной таблицей или удалите его и добавьте заново.");
                    characteristics[0] = patientId;
                    patients.Add(patientId, new ReadState(characteristics));
                }
            }

            using (var command = CreateCommand(connection, transaction, $"SELECT PersonID, TestName, {MeasurementColumns} FROM Test"))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string patientId = ReadValue(reader, 0).Trim();
                    string testName = ReadValue(reader, 1).Trim();
                    if (!patients.TryGetValue(patientId, out var patient))
                        throw new FormatException($"В базе данных есть показатели пациента «{patientId}» без его карточки. Исправьте запись перед экспортом.");
                    string? canonicalName = PatientTableSchema.CanonicalTestName(testName);
                    if (canonicalName == null)
                        throw new FormatException($"Пациент «{patientId}»: неизвестный показатель «{testName}». Экспорт не может молча исключить эти данные.");
                    if (!patient.LoadedTests.Add(canonicalName))
                        throw new FormatException($"Пациент «{patientId}»: показатель «{canonicalName}» записан несколько раз. Перезапишите пациента подтверждённой полной таблицей или исправьте исходные данные.");
                    patient.Measurements[canonicalName] = Enumerable.Range(2, 11).Select(index => ReadValue(reader, index)).ToArray();
                }
            }

            var records = patients.Values
                .Select(patient => new PatientRecord(patient.Characteristics, patient.Measurements))
                .OrderBy(record => record.PatientId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            foreach (var record in records)
                PatientTableSchema.ValidateRecord(record);
            transaction.Commit();
            return records;
        }

        /// <summary>Includes orphaned legacy measurements, so adding a record cannot create hidden duplicates.</summary>
        public IReadOnlyList<string> GetExistingIds()
        {
            using var connection = OpenConnection();
            return ReadExistingIds(connection, null)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public PatientSaveResult Save(IEnumerable<PatientRecord> patients, bool overwriteExisting)
        {
            ArgumentNullException.ThrowIfNull(patients);
            // Clone and validate the complete batch before opening a transaction. No partial valid-prefix import.
            var batch = new List<PatientRecord>();
            var batchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var patient in patients)
            {
                ArgumentNullException.ThrowIfNull(patient);
                var record = new PatientRecord(patient.Characteristics, patient.Measurements);
                PatientTableSchema.ValidateRecord(record);
                if (!batchIds.Add(record.PatientId))
                    throw new FormatException($"Пациент «{record.PatientId}» встречается в переданных таблицах несколько раз. Удалите повтор внутри файла.");
                record.Characteristics[0] = record.PatientId;
                batch.Add(record);
            }
            if (batch.Count == 0)
                return new PatientSaveResult(0, 0, 0);

            using var connection = OpenConnection();
            // The write lock precedes the existence check, preventing a second writer from slipping between them.
            using var transaction = connection.BeginTransaction();
            var existingIds = ReadExistingIds(connection, transaction);
            int added = 0;
            int replaced = 0;
            int skipped = 0;
            foreach (var patient in batch)
            {
                bool exists = existingIds.Contains(patient.PatientId);
                if (exists && !overwriteExisting)
                {
                    skipped++;
                    continue;
                }
                if (exists)
                {
                    DeleteRows(connection, transaction, patient.PatientId);
                    replaced++;
                }
                else
                {
                    added++;
                }
                InsertPatient(connection, transaction, patient);
            }
            transaction.Commit();
            return new PatientSaveResult(added, replaced, skipped);
        }

        public bool Delete(string patientId)
        {
            if (string.IsNullOrWhiteSpace(patientId))
                throw new ArgumentException("Укажите значение «Пациент».", nameof(patientId));
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            bool changed = DeleteRows(connection, transaction, patientId.Trim()) > 0;
            transaction.Commit();
            return changed;
        }

        private SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWrite
            }.ToString());
            try
            {
                connection.Open();
                connection.CreateCollation(IdentityCollation, (left, right) =>
                    StringComparer.OrdinalIgnoreCase.Compare(left?.Trim(), right?.Trim()));
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private static HashSet<string> ReadExistingIds(SqliteConnection connection, SqliteTransaction? transaction)
        {
            var existingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var command = CreateCommand(connection, transaction, "SELECT PersonID FROM Person UNION ALL SELECT PersonID FROM Test");
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                string patientId = ReadValue(reader, 0).Trim();
                if (patientId.Length > 0)
                    existingIds.Add(patientId);
            }
            return existingIds;
        }

        private static int DeleteRows(SqliteConnection connection, SqliteTransaction transaction, string patientId)
        {
            int changed = 0;
            foreach (string tableName in new[] { "Test", "Person" })
            {
                using var command = CreateCommand(connection, transaction,
                    $"DELETE FROM {tableName} WHERE PersonID COLLATE {IdentityCollation} = @patientId");
                command.Parameters.AddWithValue("@patientId", patientId);
                changed += command.ExecuteNonQuery();
            }
            return changed;
        }

        private static void InsertPatient(SqliteConnection connection, SqliteTransaction transaction, PatientRecord patient)
        {
            string[] values = patient.Characteristics;
            using (var command = CreateCommand(connection, transaction,
                $"INSERT INTO Person ({PersonColumns}) VALUES (@id, @group, @age, @sex, @height, @weight, @complaints, @duration, @diagnosis, @additionalDiagnosis, @operation)"))
            {
                command.Parameters.AddWithValue("@id", patient.PatientId);
                command.Parameters.AddWithValue("@group", values[1]);
                command.Parameters.AddWithValue("@age", int.Parse(NumericText(values[2]), NumberStyles.Integer, CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@sex", values[3]);
                command.Parameters.AddWithValue("@height", double.Parse(NumericText(values[4]), NumberStyles.Float, CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@weight", double.Parse(NumericText(values[5]), NumberStyles.Float, CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@complaints", values[6]);
                command.Parameters.AddWithValue("@duration", values[7]);
                command.Parameters.AddWithValue("@diagnosis", values[8]);
                command.Parameters.AddWithValue("@additionalDiagnosis", values[9]);
                command.Parameters.AddWithValue("@operation", values[10]);
                command.ExecuteNonQuery();
            }
            using var testCommand = CreateCommand(connection, transaction,
                $"INSERT INTO Test (PersonID, TestName, {MeasurementColumns}) VALUES (@id, @name, @day0, @day1, @day2, @day3, @day4, @day5, @day6, @day7, @day8, @day9, @day10)");
            testCommand.Parameters.AddWithValue("@id", patient.PatientId);
            var nameParameter = testCommand.Parameters.Add("@name", SqliteType.Text);
            var dayParameters = Enumerable.Range(0, 11).Select(index => testCommand.Parameters.Add("@day" + index, SqliteType.Text)).ToArray();
            foreach (string testName in PatientTableSchema.TestNames)
            {
                nameParameter.Value = testName;
                string[] measurements = patient.Measurements[testName];
                for (int index = 0; index < 11; index++)
                    dayParameters[index].Value = measurements[index] ?? string.Empty;
                testCommand.ExecuteNonQuery();
            }
        }

        private static string NumericText(string value) =>
            string.Concat(value.Where(character => !char.IsWhiteSpace(character))).Replace(',', '.');

        private static string ReadValue(SqliteDataReader reader, int ordinal) =>
            reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture) ?? string.Empty;

        private static SqliteCommand CreateCommand(SqliteConnection connection, SqliteTransaction? transaction, string sql)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            return command;
        }

        private sealed class ReadState
        {
            public string[] Characteristics { get; }
            public Dictionary<string, string[]> Measurements { get; } = PatientTableSchema.TestNames
                .ToDictionary(name => name, _ => Enumerable.Repeat(string.Empty, 11).ToArray(), StringComparer.OrdinalIgnoreCase);
            public HashSet<string> LoadedTests { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public ReadState(string[] characteristics) => Characteristics = characteristics;
        }
    }
}
