using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediHiStat
{
    public static class PatientTableSchema
    {
        public static readonly string[] PatientHeaders =
        {
            "Пациент", "Группа", "Возраст", "Пол", "Рост", "Вес", "Жалобы",
            "Длительность желтухи", "Осн. диагноз", "Сопутствующий", "Операция"
        };

        public static readonly string[] TestNames =
        {
            "АД", "ЧСС", "ЧДД", "Т", "ЭКГ", "Объём желчи", "Ht", "Hb", "Эритроциты",
            "Тромбоциты", "Лейкоциты", "Нейтрофилы", "Лимфоциты", "Общий белок", "Альбумин",
            "АСТ", "АЛТ", "ЩФ", "ГГТП", "ЛДГ", "Общий билирубин", "Прямой билирубин",
            "Амилаза крови", "Глюкоза крови", "Калий", "Натрий", "Магний", "Кальций", "Железо",
            "Креатинин", "Клиренс креатинина", "Мочевина крови", "Мочевая кислота",
            "С-реактивный белок", "Общий холестерин", "Триглицериды", "ЛВП", "ЛНП",
            "Прокальцитонин", "МНО", "ПВ", "ПТИ", "АЧТВ", "Фибриноген", "Уд. вес мочи", "pH",
            "Нитриты", "Белок", "Глюкоза мочи", "Кетоны", "Уробилиноген", "Билирубин мочи",
            "Эритроциты мочи", "Лейкоциты мочи", "Тест связи чисел", "Стадия ПЭ", "КВР", "ККР",
            "Холедох", "Головка ПЖ", "Внутрипеченочные", "Стенка ЖП", "Длина ЖП", "Ширина ЖП",
            "Уровень блока", "ВИЧ", "Гепатит В", "Гепатит С", "Сифилис"
        };

        public static readonly string[] TimeHeaders =
        {
            "Поступление", "1 сутки", "2 сутки", "3 сутки", "4 сутки", "5 сутки", "6 сутки",
            "7 сутки", "8 сутки", "9-12 сутки", "12-16 сутки"
        };

        public static void ValidateRecord(PatientRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            if (record.Characteristics.Length != PatientHeaders.Length)
                throw new FormatException("Требуется 11 характеристик пациента.");
            if (record.Characteristics.Any(value => value is null))
                throw new FormatException("Характеристики пациента не могут быть null: используйте пустые строки.");
            if (string.IsNullOrWhiteSpace(record.PatientId))
                throw new FormatException("Заполните поле «Пациент».");
            if (!int.TryParse(record.Characteristics[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int age) || age < 0)
                throw new FormatException("Возраст должен быть целым неотрицательным числом.");
            ValidatePositiveNumber(record.Characteristics[4], "Рост");
            ValidatePositiveNumber(record.Characteristics[5], "Вес");
            var expected = new HashSet<string>(TestNames, StringComparer.Ordinal);
            string[] extra = record.Measurements.Keys.Where(name => !expected.Contains(name)).ToArray();
            if (extra.Length > 0)
                throw new FormatException($"Неизвестные показатели: {string.Join(", ", extra)}.");
            string[] missing = TestNames.Where(name => !record.Measurements.ContainsKey(name)).ToArray();
            if (missing.Length > 0)
                throw new FormatException($"Отсутствуют показатели: {string.Join(", ", missing)}.");
            foreach (KeyValuePair<string, string[]> item in record.Measurements)
            {
                if (item.Value is null || item.Value.Length != TimeHeaders.Length)
                    throw new FormatException($"Для показателя «{item.Key}» требуется 11 сроков.");
                if (item.Value.Any(value => value is null))
                    throw new FormatException($"В показателе «{item.Key}» используйте пустые строки вместо null.");
            }
        }

        internal static string NormalizeLabel(string value) => Regex.Replace(value.Trim().Replace('\u00A0', ' '), @"\s+", " ")
            .Replace('ё', 'е').Replace('Ё', 'Е').Replace('–', '-').Replace('—', '-').ToUpperInvariant();

        internal static int PatientHeaderIndex(string value)
        {
            string normalized = NormalizeLabel(value);
            if (normalized is "ОСНОВНОЙ ДИАГНОЗ" or "ОСНОВНОЙ ДИАГНОЗ.") normalized = NormalizeLabel("Осн. диагноз");
            if (normalized is "СОПУТСТВУЮЩИЙ ДИАГНОЗ" or "СОПУТСТВУЮЩИЕ ДИАГНОЗЫ") normalized = NormalizeLabel("Сопутствующий");
            return Array.FindIndex(PatientHeaders, header => NormalizeLabel(header) == normalized);
        }

        internal static int TimeHeaderIndex(string value) => Array.FindIndex(TimeHeaders, header => NormalizeLabel(header) == NormalizeLabel(value));

        internal static string? CanonicalTestName(string value)
        {
            string normalized = NormalizeLabel(value);
            if (normalized == "T") normalized = "Т";
            if (normalized is "ТЕСТ СВЯЗИ ЧИСЕЛ (СЕК.)" or "ТЕСТ СВЯЗИ ЧИСЕЛ (СЕК)") normalized = "ТЕСТ СВЯЗИ ЧИСЕЛ";
            return TestNames.FirstOrDefault(name => NormalizeLabel(name) == normalized);
        }

        private static void ValidatePositiveNumber(string value, string name)
        {
            string normalized = value.Trim().Replace(" ", string.Empty).Replace("\u00A0", string.Empty).Replace(',', '.');
            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                || !double.IsFinite(parsed) || parsed <= 0)
                throw new FormatException($"{name} должен быть положительным конечным числом.");
        }
    }
}
