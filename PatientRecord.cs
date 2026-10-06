using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MediHiStat
{
    /// <summary>A patient's passport and unmodified, string-valued observations.</summary>
    public sealed class PatientRecord
    {
        public PatientRecord(string[] characteristics, IReadOnlyDictionary<string, string[]> measurements)
        {
            ArgumentNullException.ThrowIfNull(characteristics);
            ArgumentNullException.ThrowIfNull(measurements);
            Characteristics = (string[])characteristics.Clone();
            var copy = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string[]> item in measurements)
            {
                if (item.Value is null)
                {
                    throw new ArgumentException("Значения показателя не могут быть null.", nameof(measurements));
                }
                copy.Add(item.Key, (string[])item.Value.Clone());
            }
            Measurements = new ReadOnlyDictionary<string, string[]>(copy);
        }

        public string[] Characteristics { get; }
        public IReadOnlyDictionary<string, string[]> Measurements { get; }
        public string PatientId => Characteristics.Length > 0 ? (Characteristics[0] ?? string.Empty).Trim() : string.Empty;
    }
}
