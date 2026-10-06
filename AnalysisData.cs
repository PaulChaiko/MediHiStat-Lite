namespace MediHiStat
{
    internal sealed record MeasurementRecord(
        string PatientId,
        string Indicator,
        IReadOnlyList<string?> Values);

    internal sealed record PatientRecord(
        string PatientId,
        string Group,
        string? Sex,
        string? Diagnosis,
        string? Operation);

    internal sealed record NumericSample(
        int TotalPatients,
        int MissingCount,
        IReadOnlyList<double> Values);

    internal sealed record PairedSample(
        int TotalPatients,
        int MissingCount,
        IReadOnlyList<double> Earlier,
        IReadOnlyList<double> Later,
        IReadOnlyList<double> Differences);

    internal sealed record CategorySample(
        int TotalPatients,
        int MissingCount,
        IReadOnlyList<string> Values);

    /// <summary>
    /// Builds samples by patient identity without changing stored measurements.
    /// Blank, invalid and absent observations are missing, never measured zeroes.
    /// </summary>
    internal static class AnalysisData
    {
        public static NumericSample GetNumericSample(
            IEnumerable<MeasurementRecord> records,
            IEnumerable<string> patientIds,
            string indicator,
            int timeIndex)
        {
            ValidateTimeIndex(timeIndex, nameof(timeIndex));
            string[] selected = GetSelectedPatientIds(patientIds, nameof(patientIds));
            Dictionary<string, MeasurementRecord> measurements = GetSelectedMeasurements(records, selected, indicator);
            var values = new List<double>();

            foreach (string patientId in selected)
            {
                if (measurements.TryGetValue(patientId, out MeasurementRecord? record)
                    && StatisticsCalculator.TryParseMeasurement(GetValue(record, timeIndex), out double value))
                {
                    values.Add(value);
                }
            }

            return new NumericSample(selected.Length, selected.Length - values.Count, values.AsReadOnly());
        }

        public static PairedSample GetPairedSample(
            IEnumerable<MeasurementRecord> records,
            IEnumerable<string> patientIds,
            string indicator,
            int earlierIndex,
            int laterIndex)
        {
            ValidateTimeIndex(earlierIndex, nameof(earlierIndex));
            ValidateTimeIndex(laterIndex, nameof(laterIndex));
            if (earlierIndex >= laterIndex)
            {
                throw new ArgumentException("Ранний срок должен предшествовать позднему.", nameof(laterIndex));
            }

            string[] selected = GetSelectedPatientIds(patientIds, nameof(patientIds));
            Dictionary<string, MeasurementRecord> measurements = GetSelectedMeasurements(records, selected, indicator);
            var earlier = new List<double>();
            var later = new List<double>();
            var differences = new List<double>();

            foreach (string patientId in selected)
            {
                if (!measurements.TryGetValue(patientId, out MeasurementRecord? record))
                {
                    continue;
                }

                // Read both fields before parsing so malformed row width is always detected.
                string? earlierText = GetValue(record, earlierIndex);
                string? laterText = GetValue(record, laterIndex);
                if (!StatisticsCalculator.TryParseMeasurement(earlierText, out double earlierValue)
                    || !StatisticsCalculator.TryParseMeasurement(laterText, out double laterValue))
                {
                    continue;
                }

                double difference = laterValue - earlierValue;
                if (!double.IsFinite(difference))
                {
                    continue;
                }

                earlier.Add(earlierValue);
                later.Add(laterValue);
                differences.Add(difference);
            }

            return new PairedSample(
                selected.Length,
                selected.Length - differences.Count,
                earlier.AsReadOnly(),
                later.AsReadOnly(),
                differences.AsReadOnly());
        }

        public static CategorySample GetTestCategorySample(
            IEnumerable<MeasurementRecord> records,
            IEnumerable<string> patientIds,
            string indicator,
            int timeIndex)
        {
            ValidateTimeIndex(timeIndex, nameof(timeIndex));
            string[] selected = GetSelectedPatientIds(patientIds, nameof(patientIds));
            Dictionary<string, MeasurementRecord> measurements = GetSelectedMeasurements(records, selected, indicator);
            var values = new List<string>();

            foreach (string patientId in selected)
            {
                if (measurements.TryGetValue(patientId, out MeasurementRecord? record))
                {
                    AddCategory(values, GetValue(record, timeIndex));
                }
            }

            return new CategorySample(selected.Length, selected.Length - values.Count, values.AsReadOnly());
        }

        public static CategorySample GetPatientCategorySample(
            IEnumerable<PatientRecord> patients,
            IEnumerable<string> patientIds,
            string field)
        {
            ArgumentNullException.ThrowIfNull(patients);
            if (field is not ("Sex" or "Diagnosis" or "Operation"))
            {
                throw new ArgumentException("Допустимые характеристики: Sex, Diagnosis, Operation.", nameof(field));
            }

            string[] selected = GetSelectedPatientIds(patientIds, nameof(patientIds));
            var selectedSet = new HashSet<string>(selected, StringComparer.Ordinal);
            var selectedPatients = new Dictionary<string, PatientRecord>(StringComparer.Ordinal);
            foreach (PatientRecord patient in patients)
            {
                ArgumentNullException.ThrowIfNull(patient);
                if (!selectedSet.Contains(patient.PatientId))
                {
                    continue;
                }

                if (!selectedPatients.TryAdd(patient.PatientId, patient))
                {
                    throw new ArgumentException(
                        $"Для пациента «{patient.PatientId}» найдено несколько записей характеристик.", nameof(patients));
                }
            }

            var values = new List<string>();
            foreach (string patientId in selected)
            {
                if (!selectedPatients.TryGetValue(patientId, out PatientRecord? patient))
                {
                    continue;
                }

                string? value = field switch
                {
                    "Sex" => patient.Sex,
                    "Diagnosis" => patient.Diagnosis,
                    "Operation" => patient.Operation,
                    _ => throw new InvalidOperationException("Неизвестная характеристика пациента.")
                };
                AddCategory(values, value);
            }

            return new CategorySample(selected.Length, selected.Length - values.Count, values.AsReadOnly());
        }

        public static void EnsureIndependentGroups(IEnumerable<string> group1Ids, IEnumerable<string> group2Ids)
        {
            string[] group1 = GetSelectedPatientIds(group1Ids, nameof(group1Ids));
            string[] group2 = GetSelectedPatientIds(group2Ids, nameof(group2Ids));
            var firstGroup = new HashSet<string>(group1, StringComparer.Ordinal);
            if (group2.Any(firstGroup.Contains))
            {
                throw new ArgumentException("Независимые группы не должны содержать общих пациентов.", nameof(group2Ids));
            }
        }

        private static string[] GetSelectedPatientIds(IEnumerable<string> patientIds, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(patientIds, parameterName);
            var selected = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string patientId in patientIds)
            {
                if (string.IsNullOrWhiteSpace(patientId))
                {
                    throw new ArgumentException("Идентификатор пациента не может быть пустым.", parameterName);
                }

                if (!seen.Add(patientId))
                {
                    throw new ArgumentException($"Пациент «{patientId}» выбран более одного раза.", parameterName);
                }

                selected.Add(patientId);
            }

            return selected.ToArray();
        }

        private static Dictionary<string, MeasurementRecord> GetSelectedMeasurements(
            IEnumerable<MeasurementRecord> records,
            IReadOnlyList<string> selected,
            string indicator)
        {
            ArgumentNullException.ThrowIfNull(records);
            if (string.IsNullOrWhiteSpace(indicator))
            {
                throw new ArgumentException("Название показателя не может быть пустым.", nameof(indicator));
            }

            var selectedSet = new HashSet<string>(selected, StringComparer.Ordinal);
            var measurements = new Dictionary<string, MeasurementRecord>(StringComparer.Ordinal);
            foreach (MeasurementRecord record in records)
            {
                ArgumentNullException.ThrowIfNull(record);
                if (!selectedSet.Contains(record.PatientId)
                    || !string.Equals(record.Indicator, indicator, StringComparison.Ordinal))
                {
                    continue;
                }

                ArgumentNullException.ThrowIfNull(record.Values);
                if (!measurements.TryAdd(record.PatientId, record))
                {
                    throw new ArgumentException(
                        $"У пациента «{record.PatientId}» найдено несколько записей показателя «{indicator}».", nameof(records));
                }
            }

            return measurements;
        }

        private static string? GetValue(MeasurementRecord record, int timeIndex)
        {
            if (timeIndex >= record.Values.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeIndex), timeIndex, "Выбранный срок отсутствует в структуре записи показателя.");
            }

            return record.Values[timeIndex];
        }

        private static void ValidateTimeIndex(int timeIndex, string parameterName)
        {
            if (timeIndex < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, timeIndex, "Индекс срока не может быть отрицательным.");
            }
        }

        private static void AddCategory(List<string> values, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                // Match StatisticsCalculator.NormalizeCategory so previews and tests
                // use the same categories; spelling and synonyms remain unchanged.
                values.Add(string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)));
            }
        }
    }
}
