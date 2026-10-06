using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace MediHiStat
{
    public sealed class StatisticalResultRow
    {
        public string TimePoint { get; init; } = string.Empty;
        public string Group1Summary { get; init; } = string.Empty;
        public string Group2Summary { get; init; } = string.Empty;
        public string Statistic { get; init; } = string.Empty;
        public string DegreesOfFreedom { get; init; } = string.Empty;
        public string PValue { get; init; } = string.Empty;
        public string AdjustedPValue { get; init; } = string.Empty;
        public string EffectSize { get; init; } = string.Empty;
        public string Notes { get; init; } = string.Empty;
    }

    public partial class StatisticalResultsWindow : Window
    {
        private readonly string[] _headers =
        {
            "Срок",
            "Группа 1",
            "Группа 2",
            "Статистика",
            "df",
            "p",
            "p (Холм)",
            "Размер эффекта",
            "Метод и примечания"
        };

        private readonly IReadOnlyList<StatisticalResultRow> _rows;
        private readonly string _indicator;
        private readonly string _methodDescription;

        public StatisticalResultsWindow(
            string indicator,
            string methodDescription,
            IEnumerable<StatisticalResultRow> rows,
            string adjustedPHeader = "p (Холм)",
            string firstSampleHeader = "Группа 1",
            string secondSampleHeader = "Группа 2")
        {
            InitializeComponent();
            IndicatorText.Text = $"Показатель: {indicator}";
            MethodText.Text = methodDescription;
            _indicator = indicator;
            _methodDescription = methodDescription;
            _headers[1] = firstSampleHeader;
            _headers[2] = secondSampleHeader;
            _headers[6] = adjustedPHeader;
            ResultsGrid.Columns[1].Header = firstSampleHeader;
            ResultsGrid.Columns[2].Header = secondSampleHeader;
            ResultsGrid.Columns[6].Header = adjustedPHeader;
            _rows = rows.ToArray();
            ResultsGrid.ItemsSource = _rows;
        }

        private void CopyTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(BuildDelimitedText('\t'));
                MessageBox.Show(
                    "Таблица скопирована. Её можно вставить в Word или Excel.",
                    "Копирование результатов",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"Не удалось скопировать таблицу: {exception.Message}",
                    "Копирование результатов",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Сохранить результаты статистического анализа",
                Filter = "CSV-файл (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                FileName = $"MediHiStat_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                File.WriteAllText(
                    dialog.FileName,
                    BuildDelimitedText(';'),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"Не удалось сохранить файл: {exception.Message}",
                    "Экспорт результатов",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private string BuildDelimitedText(char delimiter)
        {
            var builder = new StringBuilder();
            // Keep the indicator, groups, methods and family definition with each exported row.
            string[] headers = new[] { "Показатель", "Описание анализа" }.Concat(_headers).ToArray();
            builder.AppendLine(string.Join(delimiter.ToString(), headers.Select(value => Escape(value, delimiter))));

            foreach (StatisticalResultRow row in _rows)
            {
                string[] values =
                {
                    _indicator,
                    _methodDescription,
                    row.TimePoint,
                    row.Group1Summary,
                    row.Group2Summary,
                    row.Statistic,
                    row.DegreesOfFreedom,
                    row.PValue,
                    row.AdjustedPValue,
                    row.EffectSize,
                    row.Notes
                };
                builder.AppendLine(string.Join(delimiter.ToString(), values.Select(value => Escape(value, delimiter))));
            }

            return builder.ToString();
        }

        private static string Escape(string value, char delimiter)
        {
            string normalized = value.Replace("\r", " ").Replace("\n", " ");
            if (normalized.Contains(delimiter) || normalized.Contains('"'))
            {
                return $"\"{normalized.Replace("\"", "\"\"")}\"";
            }
            return normalized;
        }
    }
}
