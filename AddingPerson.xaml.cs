using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MediHiStat
{
    public partial class AddingPerson : Window
    {
        private readonly ObservableCollection<Test> OCTestsOfOne;
        private readonly PatientStore patientStore = new PatientStore();

        public AddingPerson()
        {
            InitializeComponent();
            OCTestsOfOne = new ObservableCollection<Test>(PatientTableSchema.TestNames.Select(name => new Test { TestName = name }));
            TestsOfOne.ItemsSource = OCTestsOfOne;
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                PasteExcelData();
                e.Handled = true;
            }
        }

        private void PasteExcelData()
        {
            if (!Clipboard.ContainsText()) return;
            try
            {
                var startCell = TestsOfOne.CurrentCell;
                if (startCell.Column == null || startCell.Column.IsReadOnly)
                {
                    MessageBox.Show(this, "Выберите начальную ячейку результата, затем вставьте данные из таблицы.",
                        "Вставка данных", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                int startRowIndex = TestsOfOne.Items.IndexOf(startCell.Item);
                if (startRowIndex < 0) return;
                if (!CommitGrid()) return;

                string[] rows = Clipboard.GetText().Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                if (rows.Length > 0 && rows[^1].Length == 0)
                    rows = rows[..^1];
                int startColIndex = startCell.Column.DisplayIndex;
                if (startRowIndex + rows.Length > OCTestsOfOne.Count ||
                    rows.Any(row => startColIndex + row.Split('\t').Length > TestsOfOne.Columns.Count))
                {
                    MessageBox.Show(this, "Вставляемая область выходит за границы таблицы. Уменьшите область или выберите другую начальную ячейку.",
                        "Вставка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                {
                    string[] cells = rows[rowIndex].Split('\t');
                    for (int columnIndex = 0; columnIndex < cells.Length; columnIndex++)
                    {
                        var column = TestsOfOne.Columns.Single(item => item.DisplayIndex == startColIndex + columnIndex) as DataGridBoundColumn;
                        if (column == null || column.IsReadOnly || column.Binding is not Binding binding) continue;
                        typeof(Test).GetProperty(binding.Path.Path)?.SetValue(OCTestsOfOne[startRowIndex + rowIndex], cells[columnIndex]);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, $"Не удалось вставить данные.\n\n{exception.Message}",
                    "Ошибка вставки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CommitGrid()
        {
            if (TestsOfOne.CommitEdit(DataGridEditingUnit.Cell, true) && TestsOfOne.CommitEdit(DataGridEditingUnit.Row, true))
                return true;
            MessageBox.Show(this, "Завершите редактирование текущей ячейки перед сохранением или вставкой.",
                "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!CommitGrid()) return;
            try
            {
                var record = new PatientRecord(
                    new[] { _PersonID.Text.Trim(), _PatientGroup.Text, _Age.Text, _Sex.Text, _Height.Text, _Weight.Text,
                        _Complaints.Text, _Duration.Text, _Diagnosis.Text, _AddDiagnosis.Text, _Operation.Text },
                    OCTestsOfOne.ToDictionary(test => test.TestName, test => new[]
                    {
                        test.Day0 ?? string.Empty, test.Day1 ?? string.Empty, test.Day2 ?? string.Empty,
                        test.Day3 ?? string.Empty, test.Day4 ?? string.Empty, test.Day5 ?? string.Empty,
                        test.Day6 ?? string.Empty, test.Day7 ?? string.Empty, test.Day8 ?? string.Empty,
                        test.Day9_12 ?? string.Empty, test.Day12_16 ?? string.Empty
                    }));
                PatientTableSchema.ValidateRecord(record);
                // The actual duplicate check runs inside Save's transaction. A skipped result requires explicit consent.
                var result = patientStore.Save(new[] { record }, overwriteExisting: false);
                if (result.Skipped > 0)
                {
                    var consent = MessageBox.Show(this,
                        $"Пациент «{record.PatientId}» уже существует.\n\nПерезаписать его карточку и ВСЕ лабораторные показатели данными этой формы? Пустые ячейки также заменят прежние значения.",
                        "Подтверждение полной перезаписи", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                    if (consent != MessageBoxResult.Yes) return;
                    result = patientStore.Save(new[] { record }, overwriteExisting: true);
                }
                if (result.Added + result.Replaced > 0)
                    DialogResult = true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, $"Не удалось сохранить пациента. Данные этой формы не записаны.\n\n{exception.Message}",
                    "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
