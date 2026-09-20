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
        private static readonly string[] Headers =
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

        public StatisticalResultsWindow(
            string indicator,
            string methodDescription,
            IEnumerable<StatisticalResultRow> rows)
        {
            InitializeComponent();
            IndicatorText.Text = $"Показатель: {indicator}";
            MethodText.Text = methodDescription;
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
            builder.AppendLine(string.Join(delimiter.ToString(), Headers.Select(value => Escape(value, delimiter))));

            foreach (StatisticalResultRow row in _rows)
            {
                string[] values =
                {
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
