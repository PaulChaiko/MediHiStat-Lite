using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
           _Height.Text= _Height.Text.Replace(" ", "");
            _Height.Text = _Height.Text.Replace(",", ".");
            _Weight.Text = _Weight.Text.Replace(" ", "");
            _Weight.Text = _Weight.Text.Replace(",", ".");
            _Age.Text=_Age.Text.Replace(" ", "");


           
            double H = double.Parse(_Height.Text, CultureInfo.InvariantCulture);
            double W = double.Parse(_Weight.Text, CultureInfo.InvariantCulture);
           // MessageBox.Show($"Height = {H} ?? WEight = {W}");
            Person person = new Person
            {
                PersonID = _PersonID.Text,
                PatientGroup = _PatientGroup.Text,
                Age = Convert.ToInt32(_Age.Text),
                Sex = _Sex.Text,
                Height =H,
                Weight = W,
                Complaints = _Complaints.Text,
                Duration = _Duration.Text,
                Diagnosis = _Diagnosis.Text,
                AddDiagnosis = _AddDiagnosis.Text,
                Operation = _Operation.Text,


            };


            foreach (Test item in OCTestsOfOne)
            {
                item.PersonID = _PersonID.Text;
            }

            using (var connection = new SqliteConnection("Data Source=mydatabase.db"))
            {
                connection.Open();
                string sql = "INSERT INTO Person (PersonID, PatientGroup, Age, Sex, Height, Weight, Complaints, Duration, Diagnosis, AddDiagnosis, Operation) " +
                    "VALUES (@PersonID, @PatientGroup, @Age, @Sex, @Height, @Weight, @Complaints, @Duration, @Diagnosis, @AddDiagnosis, @Operation )";
                var command = new SqliteCommand(sql, connection);

                command.Parameters.AddWithValue("@PersonID", person.PersonID);
                command.Parameters.AddWithValue("@PatientGroup", person.PatientGroup);
                command.Parameters.AddWithValue("@Age", person.Age);
                command.Parameters.AddWithValue("@Sex", person.Sex);
                command.Parameters.AddWithValue("@Height", person.Height);
                command.Parameters.AddWithValue("@Weight", person.Weight);
                command.Parameters.AddWithValue("@Complaints", person.Complaints);
                command.Parameters.AddWithValue("@Duration", person.Duration);
                command.Parameters.AddWithValue("@Diagnosis", person.Diagnosis);
                command.Parameters.AddWithValue("@AddDiagnosis", person.AddDiagnosis);
                command.Parameters.AddWithValue("@Operation", person.Operation);

                command.ExecuteNonQuery();

            }


            foreach (Test item in OCTestsOfOne)
            {
                using (var connection = new SqliteConnection("Data Source=mydatabase.db"))
                {
                    connection.Open();
                    string sql = "INSERT INTO Test (PersonID, TestName, Day0, Day1, Day2, Day3, Day4, Day5, Day6, Day7, Day8, Day9_12, Day12_16) " +
                        "VALUES (@PersonID, @TestName, @Day0, @Day1, @Day2, @Day3, @Day4, @Day5, @Day6, @Day7, @Day8, @Day9_12, @Day12_16 )";
                    var command = new SqliteCommand(sql, connection);

                    command.Parameters.AddWithValue("@PersonID", item.PersonID==null ? "": item.PersonID);
                    command.Parameters.AddWithValue("@TestName", item.TestName==null ? "" : item.TestName);
                    command.Parameters.AddWithValue("@Day0", item.Day0 == null ? "" : item.Day0);
                    command.Parameters.AddWithValue("@Day1", item.Day1 == null ? "" : item.Day1);
                    command.Parameters.AddWithValue("@Day2", item.Day2 == null ? "" : item.Day2);
                    command.Parameters.AddWithValue("@Day3", item.Day3 == null ? "" : item.Day3);
                    command.Parameters.AddWithValue("@Day4", item.Day4 == null ? "" : item.Day4);
                    command.Parameters.AddWithValue("@Day5", item.Day5 == null ? "" : item.Day5);
                    command.Parameters.AddWithValue("@Day6", item.Day6 == null ? "" : item.Day6);
                    command.Parameters.AddWithValue("@Day7", item.Day7 == null ? "" : item.Day7);
                    command.Parameters.AddWithValue("@Day8", item.Day8 == null ? "" : item.Day8);
                    command.Parameters.AddWithValue("@Day9_12", item.Day9_12 == null ? "" : item.Day9_12);
                    command.Parameters.AddWithValue("@Day12_16", item.Day12_16 == null ? "" : item.Day12_16);


                    command.ExecuteNonQuery();

                }




            }
            this.Close();
        }
    }
}

//public class DatabaseService
//{
//    private string _connectionString = "Data Source=mydatabase.db;Version=3;";

//    public void InitializeDatabase()
//    {
//        if (!File.Exists("mydatabase.db"))
//        {
//            SQLiteConnection.CreateFile("mydatabase.db");

//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();

//                string sql = @"CREATE TABLE Products (
//                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
//                                Name TEXT NOT NULL,
//                                Price REAL NOT NULL)";

//                var command = new SQLiteCommand(sql, connection);
//                command.ExecuteNonQuery();
//            }
//        }
//    }

//    public List<Product> GetProducts()
//    {
//        var products = new List<Product>();

//        using (var connection = new SQLiteConnection(_connectionString))
//        {
//            connection.Open();

//            string sql = "SELECT * FROM Products";
//            var command = new SQLiteCommand(sql, connection);

//            using (var reader = command.ExecuteReader())
//            {
//                while (reader.Read())
//                {
//                    products.Add(new Product
//                    {
//                        Id = Convert.ToInt32(reader["Id"]),
//                        Name = reader["Name"].ToString(),
//                        Price = Convert.ToDecimal(reader["Price"])
//                    });
//                }
//            }
//        }

//        return products;
//    }

//    public void AddProduct(Product product)
//    {
//        using (var connection = new SQLiteConnection(_connectionString))
//        {
//            connection.Open();

//            string sql = "INSERT INTO Products (Name, Price) VALUES (@Name, @Price)";
//            var command = new SQLiteCommand(sql, connection);

//            command.Parameters.AddWithValue("@Name", product.Name);
//            command.Parameters.AddWithValue("@Price", product.Price);

//            command.ExecuteNonQuery();
//        }
//    }
//}
