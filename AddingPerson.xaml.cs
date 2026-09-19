using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MediHiStat
{
    /// <summary>
    /// Логика взаимодействия для AddingPerson.xaml
    /// </summary>
    public partial class AddingPerson : Window
    {
        ObservableCollection<Test> OCTestsOfOne = new ObservableCollection<Test>();
        public AddingPerson()
        {
            InitializeComponent();

            OCTestsOfOne = new ObservableCollection<Test>
            {
                new Test() {TestName = "АД" },
                new Test() {TestName = "ЧСС" },
                new Test() {TestName = "ЧДД" },
                new Test() {TestName = "Т" },
                new Test() {TestName = "ЭКГ" },
                new Test() {TestName = "Объём желчи" },
                new Test() {TestName = "Ht" },
                new Test() {TestName = "Hb" },
                new Test() {TestName = "Эритроциты" },
                new Test() {TestName = "Тромбоциты" },
                new Test() {TestName = "Лейкоциты" },
                new Test() {TestName = "Нейтрофилы" },
                new Test() {TestName = "Лимфоциты" },
                new Test() {TestName = "Общий белок" },
                new Test() {TestName = "Альбумин" },
                new Test() {TestName = "АСТ" },
                new Test() {TestName = "АЛТ" },
                new Test() {TestName = "ЩФ" },
                new Test() {TestName = "ГГТП" },
                new Test() {TestName = "ЛДГ" },
                new Test() {TestName = "Общий билирубин" },
                new Test() {TestName = "Прямой билирубин" },
                new Test() {TestName = "Амилаза крови" },
                new Test() {TestName = "Глюкоза крови" },
                new Test() {TestName = "Калий" },
                new Test() {TestName = "Натрий" },
                new Test() {TestName = "Магний" },
                new Test() {TestName = "Кальций" },
                new Test() {TestName = "Железо" },
                new Test() {TestName = "Креатинин"},
                new Test() {TestName = "Клиренс креатинина" },
                new Test() {TestName = "Мочевина крови" },
                new Test() {TestName = "Мочевая кислота" },
                new Test() {TestName = "С-реактивный белок" },
                new Test() {TestName = "Общий холестерин" },
                new Test() {TestName = "Триглицериды" },
                new Test() {TestName = "ЛВП" },
                new Test() {TestName = "ЛНП" },
                new Test() {TestName = "Прокальцитонин" },
                new Test() {TestName = "МНО" },
                new Test() {TestName = "ПВ" },
                new Test() {TestName = "ПТИ" },
                new Test() {TestName = "АЧТВ" },
                new Test() {TestName = "Фибриноген" },
                new Test() {TestName = "Уд. вес мочи" },
                new Test() {TestName = "pH" },
                new Test() {TestName = "Нитриты" },
                new Test() {TestName = "Белок" },
                new Test() {TestName = "Глюкоза мочи" },
                new Test() {TestName = "Кетоны" },
                new Test() {TestName = "Уробилиноген" },
                new Test() {TestName = "Билирубин мочи" },
                new Test() {TestName = "Эритроциты мочи" },
                new Test() {TestName = "Лейкоциты мочи" },
                new Test() {TestName = "Тест связи чисел" },
                new Test() {TestName = "Стадия ПЭ" },
                new Test() {TestName = "КВР" },
                new Test() {TestName = "ККР" },
                new Test() {TestName = "Холедох" },
                new Test() {TestName = "Головка ПЖ" },
                new Test() {TestName = "Внутрипеченочные" },
                new Test() {TestName = "Стенка ЖП" },
                new Test() {TestName = "Длина ЖП" },
                new Test() {TestName = "Ширина ЖП" },
                new Test() {TestName = "Уровень блока" },
                new Test() {TestName = "ВИЧ" },
                new Test() {TestName = "Гепатит В" },
                new Test() {TestName = "Гепатит С" },
                new Test() {TestName = "Сифилис" },



            };
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
                var clipboardText = Clipboard.GetText();
                var rows = clipboardText.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                var startCell = TestsOfOne.CurrentCell;
                var startColIndex = startCell.Column.DisplayIndex;
                var startRowIndex = TestsOfOne.Items.IndexOf(startCell.Item);

                for (int r = 0; r < rows.Length; r++)
                {
                    var cells = rows[r].Split('\t');
                    var currentRowIndex = startRowIndex + r;

                    if (currentRowIndex >= TestsOfOne.Items.Count) break;

                    for (int c = 0; c < cells.Length; c++)
                    {
                        var currentColIndex = startColIndex + c;
                        if (currentColIndex >= TestsOfOne.Columns.Count) break;

                        var column = TestsOfOne.Columns[currentColIndex] as DataGridBoundColumn;
                        if (column == null) continue;

                        var binding = column.Binding as Binding;
                        if (binding == null) continue;

                        var item = TestsOfOne.Items[currentRowIndex];
                        var property = item.GetType().GetProperty(binding.Path.Path);
                        if (property == null) continue;

                        try
                        {
                            var value = Convert.ChangeType(cells[c], property.PropertyType);
                            property.SetValue(item, value, null);
                        }
                        catch
                        {
                            // Обработка ошибок преобразования типов
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вставки: {ex.Message}");
            }
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            string personId = _PersonID.Text.Trim();
            string ageText = _Age.Text.Replace(" ", "");
            string heightText = _Height.Text.Replace(" ", "").Replace(",", ".");
            string weightText = _Weight.Text.Replace(" ", "").Replace(",", ".");

            if (string.IsNullOrWhiteSpace(personId))
            {
                MessageBox.Show("Укажите идентификатор пациента.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(ageText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int age) || age < 0)
            {
                MessageBox.Show("Возраст должен быть целым неотрицательным числом.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(heightText, NumberStyles.Float, CultureInfo.InvariantCulture, out double height) || height <= 0)
            {
                MessageBox.Show("Рост должен быть положительным числом.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(weightText, NumberStyles.Float, CultureInfo.InvariantCulture, out double weight) || weight <= 0)
            {
                MessageBox.Show("Вес должен быть положительным числом.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Person person = new Person
            {
                PersonID = personId,
                PatientGroup = _PatientGroup.Text,
                Age = age,
                Sex = _Sex.Text,
                Height = height,
                Weight = weight,
                Complaints = _Complaints.Text,
                Duration = _Duration.Text,
                Diagnosis = _Diagnosis.Text,
                AddDiagnosis = _AddDiagnosis.Text,
                Operation = _Operation.Text
            };

            foreach (Test item in OCTestsOfOne)
            {
                item.PersonID = personId;
            }

            using var connection = new SqliteConnection("Data Source=mydatabase.db");
            connection.Open();

            using (var duplicateCommand = connection.CreateCommand())
            {
                duplicateCommand.CommandText = "SELECT COUNT(1) FROM Person WHERE PersonID = @PersonID";
                duplicateCommand.Parameters.AddWithValue("@PersonID", personId);

                if (Convert.ToInt32(duplicateCommand.ExecuteScalar()) > 0)
                {
                    MessageBox.Show("Пациент с таким идентификатором уже существует.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            using var transaction = connection.BeginTransaction();

            try
            {
                const string personSql = "INSERT INTO Person (PersonID, PatientGroup, Age, Sex, Height, Weight, Complaints, Duration, Diagnosis, AddDiagnosis, Operation) " +
                    "VALUES (@PersonID, @PatientGroup, @Age, @Sex, @Height, @Weight, @Complaints, @Duration, @Diagnosis, @AddDiagnosis, @Operation)";
                using var personCommand = new SqliteCommand(personSql, connection, transaction);
                personCommand.Parameters.AddWithValue("@PersonID", person.PersonID);
                personCommand.Parameters.AddWithValue("@PatientGroup", person.PatientGroup);
                personCommand.Parameters.AddWithValue("@Age", person.Age);
                personCommand.Parameters.AddWithValue("@Sex", person.Sex);
                personCommand.Parameters.AddWithValue("@Height", person.Height);
                personCommand.Parameters.AddWithValue("@Weight", person.Weight);
                personCommand.Parameters.AddWithValue("@Complaints", person.Complaints);
                personCommand.Parameters.AddWithValue("@Duration", person.Duration);
                personCommand.Parameters.AddWithValue("@Diagnosis", person.Diagnosis);
                personCommand.Parameters.AddWithValue("@AddDiagnosis", person.AddDiagnosis);
                personCommand.Parameters.AddWithValue("@Operation", person.Operation);
                personCommand.ExecuteNonQuery();

                const string testSql = "INSERT INTO Test (PersonID, TestName, Day0, Day1, Day2, Day3, Day4, Day5, Day6, Day7, Day8, Day9_12, Day12_16) " +
                    "VALUES (@PersonID, @TestName, @Day0, @Day1, @Day2, @Day3, @Day4, @Day5, @Day6, @Day7, @Day8, @Day9_12, @Day12_16)";

                foreach (Test item in OCTestsOfOne)
                {
                    using var testCommand = new SqliteCommand(testSql, connection, transaction);
                    testCommand.Parameters.AddWithValue("@PersonID", item.PersonID ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@TestName", item.TestName ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day0", item.Day0 ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day1", item.Day1 ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day2", item.Day2 ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day3", item.Day3 ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day4", item.Day4 ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day5", item.Day5 ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day6", item.Day6 ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day7", item.Day7 ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day8", item.Day8 ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day9_12", item.Day9_12 ?? string.Empty);
                    testCommand.Parameters.AddWithValue("@Day12_16", item.Day12_16 ?? string.Empty);
                    testCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                MessageBox.Show($"Не удалось сохранить пациента.\n\n{exception.Message}", "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
        }
    }
}
