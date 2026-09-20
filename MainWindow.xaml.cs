using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Data.Sqlite;



namespace MediHiStat
{
    public class Person
    {
        public string PersonID { get; set; }
        public string PatientGroup { get; set; }
        public int Age { get; set; }
        public string Sex { get; set; }
        public double Height { get; set; }
        public double Weight { get; set; }
        public string Complaints { get; set; }

        public string Duration { get; set; }
        public string Diagnosis { get; set; }
        public string AddDiagnosis { get; set; }
        public string Operation { get; set; }

        //public Person()
        //{
        //    PatientGroup = "";
        //    Sex = "";
        //    Complaints = "";
        //    Diagnosis = "";
        //    AddDiagnosis = "";
        //    Operation = "";

        //}

    }

    public class PersonDataGrid
    {
        public string Header { get; set; }
        public string Info { get; set; }
    }

    public class Test : INotifyPropertyChanged
    {
        private string _PersonID;
        public string PersonID
        {
            get => _PersonID;
            set { _PersonID = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// ///////////////////////////////////
        /// </summary>
        private string _testName;
        public string TestName
        {
            get => _testName;
            set { _testName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        private string _Day0;
        public string Day0
        {
            get => _Day0;
            set { _Day0 = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 
        private string _Day1;
        public string Day1
        {
            get => _Day1;
            set { _Day1 = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 

        private string _Day2;

        public string Day2
        {
            get => _Day2;
            set { _Day2 = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 

        private string _Day3;

        public string Day3
        {
            get => _Day3;
            set { _Day3 = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 
        private string _Day4;
        public string Day4
        {
            get => _Day4;
            set { _Day4 = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 
        private string _Day5;
        public string Day5
        {
            get => _Day5;
            set { _Day5 = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 
        private string _Day6;
        public string Day6
        {
            get => _Day6;
            set { _Day6 = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 
        private string _Day7;
        public string Day7
        {
            get => _Day7;
            set { _Day7 = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 
        private string _Day8;

        public string Day8
        {
            get => _Day8;
            set { _Day8 = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 
        private string _Day9_12;
        public string Day9_12
        {
            get => _Day9_12;
            set { _Day9_12 = value; OnPropertyChanged(); }
        }


        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 

        private string _Day12_16;
        public string Day12_16
        {
            get => _Day12_16;
            set { _Day12_16 = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        /// 


        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TestNum
    {
        public string PersonID ="";
        public string TestName ="";
        public double Day0;
        public double Day1;
        public double Day2;
        public double Day3;
        public double Day4;
        public double Day5;
        public double Day6;
        public double Day7;
        public double Day8;
        public double Day9_12;
        public double Day12_16;
    }


    public partial class MainWindow : Window
    {

        public List<Person> Persons = new List<Person>();

        public List<Test> Tests = new List<Test>();

        public List<Test> TestsOfOne = new List<Test>();

        int N0=0;


        public ObservableCollection<PersonDataGrid> personDataGrids = new ObservableCollection<PersonDataGrid>
        { 
          new PersonDataGrid {Header = "Пациент", Info=""},
          new PersonDataGrid {Header = "Группа", Info=""},
          new PersonDataGrid {Header = "Возраст", Info=""},
          new PersonDataGrid {Header = "Пол", Info=""},
          new PersonDataGrid {Header = "Рост", Info=""},
          new PersonDataGrid {Header = "Вес", Info=""},
          new PersonDataGrid {Header = "Жалобы", Info=""},
          new PersonDataGrid {Header = "Длительность желтухи", Info=""},
          new PersonDataGrid {Header = "Основной диагноз", Info=""},
          new PersonDataGrid {Header = "Сопустcтвующий диагноз", Info=""},
          new PersonDataGrid {Header = "Операция", Info=""}

        };

        public ObservableCollection<PersonDataGrid> personDataGrids2 = new ObservableCollection<PersonDataGrid>
        { new PersonDataGrid {Header = "Пациент", Info=""},
          new PersonDataGrid {Header = "Группа", Info=""},
          new PersonDataGrid {Header = "Возраст", Info=""},
          new PersonDataGrid {Header = "Пол", Info=""},
          new PersonDataGrid {Header = "Рост", Info=""},
          new PersonDataGrid {Header = "Вес", Info=""},
          new PersonDataGrid {Header = "Жалобы", Info=""},
          new PersonDataGrid {Header = "Длительность желтухи", Info=""},
          new PersonDataGrid {Header = "Основной диагноз", Info=""},
          new PersonDataGrid {Header = "Сопустcтвующий диагноз", Info=""},
          new PersonDataGrid {Header = "Операция", Info=""}

        };

        ObservableCollection<string> PatientL = new ObservableCollection<string>();

        List<TestNum> TestCal = new List<TestNum>();
        List<string> TestsList = new List<string>();
        List<string> CurrentGroupPatientIds = new List<string>();
        List<Test> CurrentGroupTests = new List<Test>();
        List<string> Group1PatientIds = new List<string>();
        List<string> Group2PatientIds = new List<string>();
        List<Test> Group1Observations = new List<Test>();
        List<Test> Group2Observations = new List<Test>();

        TestNum G1 = new TestNum();
        TestNum G2 = new TestNum();

        bool Check1 = true;
        bool Check2 = true;

        private readonly List<UIElement> _graphElements = new List<UIElement>();
        private List<double>[]? _lastGroup1Samples;
        private List<double>[]? _lastGroup2Samples;

        private static readonly string[] TimePointNames =
        {
            "0 сутки",
            "1 сутки",
            "2 сутки",
            "3 сутки",
            "4 сутки",
            "5 сутки",
            "6 сутки",
            "7 сутки",
            "8 сутки",
            "9–12 сутки",
            "12–16 сутки"
        };


        void NewActvePerson(ObservableCollection<PersonDataGrid> A, Person B)
        {
            A[0].Info = B.PersonID;
            A[1].Info = B.PatientGroup;
            A[2].Info = B.Age.ToString();
            A[3].Info = B.Sex;
            A[4].Info = B.Height.ToString();
            A[5].Info = B.Weight.ToString();
            A[6].Info = B.Complaints;
            A[7].Info = B.Duration;
            A[8].Info = B.Diagnosis;
            A[9].Info = B.AddDiagnosis;
            A[10].Info = B.Operation;


            TableOnePerson.ItemsSource = null;
            TableOnePerson.ItemsSource = A;
            TestsOfOne.Clear();

            foreach (Test T in Tests)
            {
                if (B.PersonID == T.PersonID) TestsOfOne.Add(T);
            }

            MainTestsOfOne.ItemsSource = null;
            MainTestsOfOne.ItemsSource = TestsOfOne;
        }





        public MainWindow()
        {
            InitializeComponent();
            TableOnePerson.ItemsSource = personDataGrids;
            //TableAllPerson.ItemsSource = personDataGrids;
            // MainTestsOfOne.ItemsSource = Tests;
            TableOnePersonPatientSearch.ItemsSource = PatientL;
            TableAllPerson.ItemsSource = personDataGrids2;


            PersonsPull();
            TestPull();
           







            //using (var connection = new SqliteConnection("Data Source=mydatabase.db"))
            //{
            //    connection.Open();


            //    var sql = "SELECT * FROM Person";
            //    var command = new SqliteCommand(sql, connection);


            //    using (var reader = command.ExecuteReader())
            //    {
            //        while (reader.Read())
            //        {
            //            Persons.Add(new Person
            //            {
            //                PersonID = reader["PersonID"].ToString(),
            //                PatientGroup = reader["PatientGroup"].ToString(),
            //                Age = Convert.ToInt32(reader["Age"]),
            //                Sex = reader["Sex"].ToString(),
            //                Height = Convert.ToDouble(reader["Height"]),
            //                Weight = Convert.ToDouble(reader["Weight"]),
            //                Complaints = reader["Complaints"].ToString(),
            //                Duration = reader["Duration"].ToString(),
            //                Diagnosis = reader["Diagnosis"].ToString(),
            //                AddDiagnosis = reader["AddDiagnosis"].ToString(),
            //                Operation = reader["Operation"].ToString()

            //            });
            //        }
            //    }

            //    PatientCount.Text = $"Всего пациентов: {Persons.Count}";


            //}



            if (Persons.Count != 0) NewActvePerson(personDataGrids, Persons[0]);





            //PatientCount.Text = $"Всего пациентов: {Persons.Count}";



            //MAIN////////////////////////////
        }

        private void AddPerson_Click(object sender, RoutedEventArgs e)
        {
            AddingPerson _Adding = new AddingPerson();
            _Adding.ShowDialog();
            PersonsPull();
            TestPull();

        }

        private void RemovePerson_Click(object sender, RoutedEventArgs e)
        {
            Remover _Remover = new Remover();
            _Remover.ShowDialog();
            PersonsPull();
            TestPull();

        }

        void PersonsPull()
        {
            using (var connection = new SqliteConnection("Data Source=mydatabase.db"))
            {
                connection.Open();
                Persons.Clear();


                var sql = "SELECT * FROM Person";
                var command = new SqliteCommand(sql, connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Persons.Add(new Person
                        {
                            PersonID = reader["PersonID"].ToString(),
                            PatientGroup = reader["PatientGroup"].ToString(),
                            Age = Convert.ToInt32(reader["Age"]),
                            Sex = reader["Sex"].ToString(),
                            Height = Convert.ToDouble(reader["Height"]),
                            Weight = Convert.ToDouble(reader["Weight"]),
                            Complaints = reader["Complaints"].ToString(),
                            Duration = reader["Duration"].ToString(),
                            Diagnosis = reader["Diagnosis"].ToString(),
                            AddDiagnosis = reader["AddDiagnosis"].ToString(),
                            Operation = reader["Operation"].ToString()

                        });
                    }
                }

                PatientCount.Text = $"Всего пациентов: {Persons.Count}";

                PatientL.Clear();
                foreach (Person person in Persons)
                {
                    PatientL.Add(person.PersonID);

                }


            }
        }

        void TestPull()
        {
            using (var connection = new SqliteConnection("Data Source=mydatabase.db"))
            {
                connection.Open();
                Tests.Clear();

                var sql = "SELECT * FROM Test";
                var command = new SqliteCommand(sql, connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Tests.Add(new Test
                        {
                            PersonID = reader["PersonID"].ToString(),
                            TestName = reader["TestName"].ToString(),
                            Day0 = reader["Day0"].ToString(),
                            Day1 = reader["Day1"].ToString(),
                            Day2 = reader["Day2"].ToString(),
                            Day3 = reader["Day3"].ToString(),
                            Day4 = reader["Day4"].ToString(),
                            Day5 = reader["Day5"].ToString(),
                            Day6 = reader["Day6"].ToString(),
                            Day7 = reader["Day7"].ToString(),
                            Day8 = reader["Day8"].ToString(),
                            Day9_12 = reader["Day9_12"].ToString(),
                            Day12_16 = reader["Day12_16"].ToString()

                        });
                    }
                }
            }
        }

        private void TableOnePersonPatientSearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TableOnePersonPatientSearch.SelectedItem is not string selectedPersonId)
            {
                return;
            }

            foreach (Person person in Persons)
            {
                if (person.PersonID == selectedPersonId)
                {
                    NewActvePerson(personDataGrids, person);
                    break;
                }
            }

        }

        private static bool MatchesTextFilter(string? value, string? filter, bool contains)
        {
            string normalizedFilter = filter?.Trim() ?? string.Empty;
            if (normalizedFilter.Length == 0)
            {
                return true;
            }

            string normalizedValue = value ?? string.Empty;
            return contains
                ? normalizedValue.Contains(normalizedFilter, StringComparison.CurrentCultureIgnoreCase)
                : string.Equals(normalizedValue, normalizedFilter, StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool MatchesNumericFilter(double value, string? filter)
        {
            string normalized = (filter ?? string.Empty)
                .Trim()
                .Replace(" ", string.Empty)
                .Replace(',', '.');

            if (normalized.Length == 0)
            {
                return true;
            }

            if (normalized.StartsWith("<=", StringComparison.Ordinal))
            {
                return value <= ParseFilterNumber(normalized[2..]);
            }

            if (normalized.StartsWith(">=", StringComparison.Ordinal))
            {
                return value >= ParseFilterNumber(normalized[2..]);
            }

            if (normalized.StartsWith('<'))
            {
                return value < ParseFilterNumber(normalized[1..]);
            }

            if (normalized.StartsWith('>'))
            {
                return value > ParseFilterNumber(normalized[1..]);
            }

            int rangeSeparatorIndex = normalized.IndexOf('-', 1);
            if (rangeSeparatorIndex > 0)
            {
                double lowerBound = ParseFilterNumber(normalized[..rangeSeparatorIndex]);
                double upperBound = ParseFilterNumber(normalized[(rangeSeparatorIndex + 1)..]);

                if (lowerBound > upperBound)
                {
                    (lowerBound, upperBound) = (upperBound, lowerBound);
                }

                return value >= lowerBound && value <= upperBound;
            }

            return value.Equals(ParseFilterNumber(normalized));
        }

        private static double ParseFilterNumber(string text)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                throw new FormatException($"Не удалось распознать числовой фильтр: {text}");
            }

            return value;
        }

        private static string[] GetTimePointValues(Test test)
        {
            return new[]
            {
                test.Day0,
                test.Day1,
                test.Day2,
                test.Day3,
                test.Day4,
                test.Day5,
                test.Day6,
                test.Day7,
                test.Day8,
                test.Day9_12,
                test.Day12_16
            };
        }

        private static void SetTimePointMeans(TestNum target, IReadOnlyList<double> means)
        {
            target.Day0 = means[0];
            target.Day1 = means[1];
            target.Day2 = means[2];
            target.Day3 = means[3];
            target.Day4 = means[4];
            target.Day5 = means[5];
            target.Day6 = means[6];
            target.Day7 = means[7];
            target.Day8 = means[8];
            target.Day9_12 = means[9];
            target.Day12_16 = means[10];
        }

        private static Test CreateDescriptiveStatisticsRow(
            string testName,
            IReadOnlyList<DescriptiveStatistics> statistics)
        {
            return new Test
            {
                TestName = testName,
                Day0 = FormatDescriptiveStatistics(statistics[0]),
                Day1 = FormatDescriptiveStatistics(statistics[1]),
                Day2 = FormatDescriptiveStatistics(statistics[2]),
                Day3 = FormatDescriptiveStatistics(statistics[3]),
                Day4 = FormatDescriptiveStatistics(statistics[4]),
                Day5 = FormatDescriptiveStatistics(statistics[5]),
                Day6 = FormatDescriptiveStatistics(statistics[6]),
                Day7 = FormatDescriptiveStatistics(statistics[7]),
                Day8 = FormatDescriptiveStatistics(statistics[8]),
                Day9_12 = FormatDescriptiveStatistics(statistics[9]),
                Day12_16 = FormatDescriptiveStatistics(statistics[10])
            };
        }

        private static string FormatDescriptiveStatistics(DescriptiveStatistics statistics)
        {
            if (statistics.Count == 0)
            {
                return string.Empty;
            }

            string mean = FormatNumber(statistics.Mean);
            string standardDeviation = double.IsNaN(statistics.SampleStandardDeviation)
                ? "—"
                : FormatNumber(statistics.SampleStandardDeviation);
            string median = FormatNumber(statistics.Median);
            string firstQuartile = FormatNumber(statistics.FirstQuartile);
            string thirdQuartile = FormatNumber(statistics.ThirdQuartile);
            return $"n={statistics.Count}; среднее±SD: {mean}±{standardDeviation}\n" +
                $"Me [Q1; Q3]: {median} [{firstQuartile}; {thirdQuartile}]";
        }

        private static string FormatNumber(double value, string format = "0.##")
        {
            return Math.Round(value, 4).ToString(format, CultureInfo.CurrentCulture);
        }

        private void TableStarter_Click(object sender, RoutedEventArgs e)
        {
            Check1 = true;
            Check2 = true;

            try
            {
                CurrentGroupPatientIds = Persons
                    .Where(person =>
                        MatchesTextFilter(person.PersonID, personDataGrids2[0].Info, false)
                        && MatchesTextFilter(person.PatientGroup, personDataGrids2[1].Info, false)
                        && MatchesNumericFilter(person.Age, personDataGrids2[2].Info)
                        && MatchesTextFilter(person.Sex, personDataGrids2[3].Info, false)
                        && MatchesNumericFilter(person.Height, personDataGrids2[4].Info)
                        && MatchesNumericFilter(person.Weight, personDataGrids2[5].Info)
                        && MatchesTextFilter(person.Complaints, personDataGrids2[6].Info, true)
                        && MatchesTextFilter(person.Duration, personDataGrids2[7].Info, false)
                        && MatchesTextFilter(person.Diagnosis, personDataGrids2[8].Info, true)
                        && MatchesTextFilter(person.AddDiagnosis, personDataGrids2[9].Info, true)
                        && MatchesTextFilter(person.Operation, personDataGrids2[10].Info, true))
                    .Select(person => person.PersonID)
                    .ToList();
            }
            catch (FormatException exception)
            {
                MessageBox.Show(exception.Message, "Проверка фильтров", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            N0 = CurrentGroupPatientIds.Count;
            var selectedPatientIds = CurrentGroupPatientIds.ToHashSet();
            CurrentGroupTests = Tests
                .Where(test => selectedPatientIds.Contains(test.PersonID))
                .ToList();

            TestCal.Clear();
            TestsList.Clear();

            var displayRows = new List<Test>();
            IEnumerable<string> testNames = CurrentGroupTests
                .Select(test => test.TestName)
                .Where(testName => !string.IsNullOrWhiteSpace(testName))
                .Distinct();

            foreach (string testName in testNames)
            {
                List<Test> observations = CurrentGroupTests
                    .Where(test => test.TestName == testName)
                    .ToList();
                var statisticsByTimePoint = new List<DescriptiveStatistics>(11);
                var means = new List<double>(11);

                for (int timePointIndex = 0; timePointIndex < 11; timePointIndex++)
                {
                    var values = new List<double>();
                    foreach (Test observation in observations)
                    {
                        string valueText = GetTimePointValues(observation)[timePointIndex];
                        if (StatisticsCalculator.TryParseMeasurement(valueText, out double value))
                        {
                            values.Add(value);
                        }
                    }

                    DescriptiveStatistics statistics = StatisticsCalculator.CalculateDescriptive(values);
                    statisticsByTimePoint.Add(statistics);
                    means.Add(statistics.Count == 0 ? 0 : Math.Round(statistics.Mean, 2));
                }

                var meanRow = new TestNum { TestName = testName };
                SetTimePointMeans(meanRow, means);
                TestCal.Add(meanRow);
                TestsList.Add(testName);
                displayRows.Add(CreateDescriptiveStatisticsRow(testName, statisticsByTimePoint));
            }

            MainTestsOfMany.ItemsSource = displayRows;
            HowMany.Text = $"Отображено пациентов: {CurrentGroupPatientIds.Count}";

            Group1.ItemsSource = null;
            Group2.ItemsSource = null;
            Group1.ItemsSource = TestsList;
            Group2.ItemsSource = TestsList;
        }

        private void Group1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Group1.SelectedItem is not string selectedTestName || TestCal.Count == 0)
            {
                return;
            }

            TestNum? selectedMean = TestCal.FirstOrDefault(test => test.TestName == selectedTestName);
            if (selectedMean is null)
            {
                return;
            }

            G1 = selectedMean;
            Group1PatientIds = new List<string>(CurrentGroupPatientIds);
            Group1Observations = CurrentGroupTests
                .Where(test => test.TestName == selectedTestName)
                .ToList();


            if (Check1)
            {
                Group1name.Text = "Группа 1: ";

                foreach (var item in personDataGrids2)
                {
                    if (!string.IsNullOrWhiteSpace(item.Info))
                    {
                        Group1name.Text += item.Info;
                        Group1name.Text += "; ";
                    }
                }
            }

            if (Group1name.Text == "Группа 1: ")
            {
                Group1name.Text += "Все пациенты";
            }

            Check1 = false;
        }

        private void Group2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Group2.SelectedItem is not string selectedTestName || TestCal.Count == 0)
            {
                return;
            }

            TestNum? selectedMean = TestCal.FirstOrDefault(test => test.TestName == selectedTestName);
            if (selectedMean is null)
            {
                return;
            }

            G2 = selectedMean;
            Group2PatientIds = new List<string>(CurrentGroupPatientIds);
            Group2Observations = CurrentGroupTests
                .Where(test => test.TestName == selectedTestName)
                .ToList();

            if (Check2)
            {
                Group2name.Text = "Группа 2: ";

                foreach (var item in personDataGrids2)
                {
                    if (!string.IsNullOrWhiteSpace(item.Info))
                    {
                        Group2name.Text += item.Info;
                        Group2name.Text += "; ";
                    }
                }
            }

            if (Group2name.Text == "Группа 2: ")
            {
                Group2name.Text += "Все пациенты";
            }

            Check2 = false;
        }

        private static List<double>[] ExtractTimePointSamples(IEnumerable<Test> observations)
        {
            List<double>[] samples = Enumerable.Range(0, 11)
                .Select(_ => new List<double>())
                .ToArray();

            foreach (Test observation in observations)
            {
                string[] values = GetTimePointValues(observation);
                for (int index = 0; index < values.Length; index++)
                {
                    if (StatisticsCalculator.TryParseMeasurement(values[index], out double value))
                    {
                        samples[index].Add(value);
                    }
                }
            }

            return samples;
        }

        private static List<string>[] ExtractCategoricalTimePointSamples(IEnumerable<Test> observations)
        {
            List<string>[] samples = Enumerable.Range(0, 11)
                .Select(_ => new List<string>())
                .ToArray();

            foreach (Test observation in observations)
            {
                string[] values = GetTimePointValues(observation);
                for (int index = 0; index < values.Length; index++)
                {
                    if (!string.IsNullOrWhiteSpace(values[index]))
                    {
                        samples[index].Add(values[index]);
                    }
                }
            }

            return samples;
        }

        private void DrawComparisonGraph(
            IReadOnlyList<List<double>> group1Samples,
            IReadOnlyList<List<double>> group2Samples)
        {
            DescriptiveStatistics[] group1Statistics = group1Samples
                .Select(StatisticsCalculator.CalculateDescriptive)
                .ToArray();
            DescriptiveStatistics[] group2Statistics = group2Samples
                .Select(StatisticsCalculator.CalculateDescriptive)
                .ToArray();
            bool showMedian = GraphStatisticMode.SelectedIndex == 1;

            (double?[] group1Centers, double?[] group1Lower, double?[] group1Upper) =
                CreateGraphSeries(group1Statistics, showMedian);
            (double?[] group2Centers, double?[] group2Lower, double?[] group2Upper) =
                CreateGraphSeries(group2Statistics, showMedian);

            double[] availableBounds = group1Lower
                .Concat(group1Upper)
                .Concat(group2Lower)
                .Concat(group2Upper)
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToArray();

            if (availableBounds.Length == 0)
            {
                return;
            }

            double scaleMinimum = Math.Min(0, availableBounds.Min());
            double scaleMaximum = Math.Max(0, availableBounds.Max());
            if (Math.Abs(scaleMaximum - scaleMinimum) < 1e-12)
            {
                scaleMaximum = scaleMinimum + 1;
            }
            else
            {
                double padding = (scaleMaximum - scaleMinimum) * 0.05;
                if (scaleMinimum < 0)
                {
                    scaleMinimum -= padding;
                }
                if (scaleMaximum > 0)
                {
                    scaleMaximum += padding;
                }
            }

            SetGraphAxisLabels(scaleMinimum, scaleMaximum);
            ClearGraph();
            AddGraphSeries(group1Centers, group1Lower, group1Upper, scaleMinimum, scaleMaximum, Brushes.Red);
            AddGraphSeries(group2Centers, group2Lower, group2Upper, scaleMinimum, scaleMaximum, Brushes.Blue);

            string presentation = showMedian ? "медиана [Q1; Q3]" : "среднее ± SD";
            DeskGroup1.Text = $"{Group1name.Text} {G1.TestName}; {presentation}";
            DeskGroup2.Text = $"{Group2name.Text} {G2.TestName}; {presentation}";
        }

        private static (double?[] Centers, double?[] Lower, double?[] Upper) CreateGraphSeries(
            IReadOnlyList<DescriptiveStatistics> statistics,
            bool showMedian)
        {
            var centers = new double?[statistics.Count];
            var lower = new double?[statistics.Count];
            var upper = new double?[statistics.Count];

            for (int index = 0; index < statistics.Count; index++)
            {
                DescriptiveStatistics item = statistics[index];
                if (item.Count == 0)
                {
                    continue;
                }

                if (showMedian)
                {
                    centers[index] = item.Median;
                    lower[index] = item.FirstQuartile;
                    upper[index] = item.ThirdQuartile;
                }
                else
                {
                    centers[index] = item.Mean;
                    double deviation = double.IsNaN(item.SampleStandardDeviation)
                        ? 0
                        : item.SampleStandardDeviation;
                    lower[index] = item.Mean - deviation;
                    upper[index] = item.Mean + deviation;
                }
            }

            return (centers, lower, upper);
        }

        private void AddGraphSeries(
            IReadOnlyList<double?> centers,
            IReadOnlyList<double?> lower,
            IReadOnlyList<double?> upper,
            double scaleMinimum,
            double scaleMaximum,
            Brush color)
        {
            for (int index = 0; index < centers.Count; index++)
            {
                if (!centers[index].HasValue || !lower[index].HasValue || !upper[index].HasValue)
                {
                    continue;
                }

                double x = 240 + 120 * index;
                double lowerY = GetGraphY(lower[index]!.Value, scaleMinimum, scaleMaximum);
                double upperY = GetGraphY(upper[index]!.Value, scaleMinimum, scaleMaximum);
                AddGraphElement(new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = lowerY,
                    Y2 = upperY,
                    Stroke = color,
                    StrokeThickness = 2,
                    Opacity = 0.55
                });
                AddGraphElement(new Line
                {
                    X1 = x - 8,
                    X2 = x + 8,
                    Y1 = lowerY,
                    Y2 = lowerY,
                    Stroke = color,
                    StrokeThickness = 2,
                    Opacity = 0.55
                });
                AddGraphElement(new Line
                {
                    X1 = x - 8,
                    X2 = x + 8,
                    Y1 = upperY,
                    Y2 = upperY,
                    Stroke = color,
                    StrokeThickness = 2,
                    Opacity = 0.55
                });
            }

            var points = new PointCollection();

            for (int index = 0; index < centers.Count; index++)
            {
                if (!centers[index].HasValue)
                {
                    continue;
                }

                double x = 240 + 120 * index;
                double y = GetGraphY(centers[index]!.Value, scaleMinimum, scaleMaximum);
                points.Add(new Point(x, y));
            }

            AddGraphElement(new Polyline
            {
                Points = points,
                Stroke = color,
                StrokeThickness = 3
            });
        }

        private static double GetGraphY(double value, double scaleMinimum, double scaleMaximum)
        {
            return 1100 - (value - scaleMinimum) / (scaleMaximum - scaleMinimum) * 1000;
        }

        private void SetGraphAxisLabels(double scaleMinimum, double scaleMaximum)
        {
            TextBox[] labels = { Y0, Y1, Y2, Y3, Y4, Y5, Y6, Y7, Y8, Y9, Y10 };
            for (int index = 0; index < labels.Length; index++)
            {
                double value = scaleMinimum + (scaleMaximum - scaleMinimum) * index / 10;
                labels[index].Text = FormatNumber(value);
            }
        }

        private void AddGraphElement(UIElement element)
        {
            Panel.SetZIndex(element, 3);
            _graphElements.Add(element);
            Desk.Children.Add(element);
        }

        private void ClearGraph()
        {
            foreach (UIElement element in _graphElements)
            {
                Desk.Children.Remove(element);
            }
            _graphElements.Clear();
        }

        private void GraphStatisticMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_lastGroup1Samples is not null && _lastGroup2Samples is not null)
            {
                DrawComparisonGraph(_lastGroup1Samples, _lastGroup2Samples);
            }
        }

        private void AnalysisMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GraphStatisticMode is not null)
            {
                GraphStatisticMode.IsEnabled = AnalysisMethod.SelectedIndex == 0;
            }
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            if (Group1Observations.Count == 0 || Group2Observations.Count == 0)
            {
                MessageBox.Show("Сначала сформируйте обе группы и выберите показатель для каждой из них.", "Статистическая обработка", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.Equals(G1.TestName, G2.TestName, StringComparison.Ordinal))
            {
                MessageBox.Show("Для сравнения двух групп выберите один и тот же показатель.", "Статистическая обработка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string[] overlappingPatients = Group1PatientIds
                .Intersect(Group2PatientIds)
                .ToArray();
            if (overlappingPatients.Length > 0)
            {
                MessageBox.Show(
                    "Группы пересекаются по пациентам. Выбранные методы применяются к независимым группам; сформируйте непересекающиеся выборки.",
                    "Статистическая обработка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (AnalysisMethod.SelectedIndex == 1)
            {
                ProcessCategoricalComparison();
            }
            else
            {
                ProcessNumericComparison();
            }
        }

        private void ProcessNumericComparison()
        {
            List<double>[] group1Samples = ExtractTimePointSamples(Group1Observations);
            List<double>[] group2Samples = ExtractTimePointSamples(Group2Observations);
            _lastGroup1Samples = group1Samples;
            _lastGroup2Samples = group2Samples;
            DrawComparisonGraph(group1Samples, group2Samples);

            var results = new MannWhitneyResult?[TimePointNames.Length];
            for (int index = 0; index < TimePointNames.Length; index++)
            {
                if (group1Samples[index].Count > 0 && group2Samples[index].Count > 0)
                {
                    results[index] = StatisticsCalculator.CalculateMannWhitney(
                        group1Samples[index],
                        group2Samples[index]);
                }
            }

            double[] adjustedPValues = StatisticsCalculator.AdjustPValuesHolm(
                results
                    .Where(result => result.HasValue)
                    .Select(result => result!.Value.PValue)
                    .ToArray());
            int adjustedPValueIndex = 0;
            var rows = new List<StatisticalResultRow>(TimePointNames.Length);

            for (int index = 0; index < TimePointNames.Length; index++)
            {
                if (!results[index].HasValue)
                {
                    rows.Add(new StatisticalResultRow
                    {
                        TimePoint = TimePointNames[index],
                        Group1Summary = FormatAnalysisSummary(
                            StatisticsCalculator.CalculateDescriptive(group1Samples[index])),
                        Group2Summary = FormatAnalysisSummary(
                            StatisticsCalculator.CalculateDescriptive(group2Samples[index])),
                        Notes = "Недостаточно числовых данных"
                    });
                    continue;
                }

                MannWhitneyResult result = results[index]!.Value;
                double adjustedPValue = adjustedPValues[adjustedPValueIndex++];
                string method = result.UsedExactPValue ? "точный" : "асимптотический";
                double rankBiserialCorrelation = 2 * result.U1
                    / (result.Group1Count * (double)result.Group2Count)
                    - 1;
                rows.Add(new StatisticalResultRow
                {
                    TimePoint = TimePointNames[index],
                    Group1Summary = FormatAnalysisSummary(
                        StatisticsCalculator.CalculateDescriptive(group1Samples[index])),
                    Group2Summary = FormatAnalysisSummary(
                        StatisticsCalculator.CalculateDescriptive(group2Samples[index])),
                    Statistic = $"U={FormatNumber(result.U, "0.###")}",
                    PValue = FormatPValue(result.PValue),
                    AdjustedPValue = FormatPValue(adjustedPValue),
                    EffectSize = $"rᵣᵦ={FormatNumber(rankBiserialCorrelation, "0.###")}",
                    Notes = $"Манн—Уитни, {method} двусторонний p; поправка Холма"
                });
            }

            ShowResultsWindow(
                "U-критерий Манна—Уитни; двусторонний p-value; поправка Холма. " +
                "В таблице приведены среднее ± SD и медиана [Q1; Q3].",
                rows);
        }

        private void ProcessCategoricalComparison()
        {
            List<string>[] group1Samples = ExtractCategoricalTimePointSamples(Group1Observations);
            List<string>[] group2Samples = ExtractCategoricalTimePointSamples(Group2Observations);
            var results = new CategoricalComparisonResult?[TimePointNames.Length];

            for (int index = 0; index < TimePointNames.Length; index++)
            {
                try
                {
                    results[index] = StatisticsCalculator.CalculateCategoricalComparison(
                        group1Samples[index],
                        group2Samples[index]);
                }
                catch (ArgumentException)
                {
                    results[index] = null;
                }
            }

            double[] adjustedPValues = StatisticsCalculator.AdjustPValuesHolm(
                results
                    .Where(result => result is not null)
                    .Select(result => result!.ReportedPValue)
                    .ToArray());
            int adjustedPValueIndex = 0;
            var rows = new List<StatisticalResultRow>(TimePointNames.Length);

            for (int index = 0; index < TimePointNames.Length; index++)
            {
                CategoricalComparisonResult? result = results[index];
                if (result is null)
                {
                    rows.Add(new StatisticalResultRow
                    {
                        TimePoint = TimePointNames[index],
                        Group1Summary = $"n={group1Samples[index].Count}",
                        Group2Summary = $"n={group2Samples[index].Count}",
                        Notes = "Недостаточно данных или представлена только одна категория"
                    });
                    continue;
                }

                double adjustedPValue = adjustedPValues[adjustedPValueIndex++];
                string method = result.UsedFisherExact
                    ? "Точный критерий Фишера выбран автоматически из-за малых ожидаемых частот"
                    : "χ² Пирсона";
                string frequencyWarning = result.ExpectedCountsBelowFive > 0 && !result.UsedFisherExact
                    ? $"; внимание: ячеек с ожидаемой частотой <5 — {result.ExpectedCountsBelowFive}"
                    : string.Empty;

                rows.Add(new StatisticalResultRow
                {
                    TimePoint = TimePointNames[index],
                    Group1Summary = FormatCategoryCounts(
                        result.Group1Count,
                        result.Categories,
                        result.Group1Counts),
                    Group2Summary = FormatCategoryCounts(
                        result.Group2Count,
                        result.Categories,
                        result.Group2Counts),
                    Statistic = $"χ²={FormatNumber(result.ChiSquare, "0.###")}",
                    DegreesOfFreedom = result.DegreesOfFreedom.ToString(CultureInfo.CurrentCulture),
                    PValue = FormatPValue(result.ReportedPValue),
                    AdjustedPValue = FormatPValue(adjustedPValue),
                    EffectSize = $"V={FormatNumber(result.CramersV, "0.###")}",
                    Notes = $"{method}; min ожидаемая частота={FormatNumber(result.MinimumExpectedCount)}" +
                        frequencyWarning + "; поправка Холма"
                });
            }

            ClearGraph();
            _lastGroup1Samples = null;
            _lastGroup2Samples = null;
            DeskGroup1.Text = $"{Group1name.Text} {G1.TestName}";
            DeskGroup2.Text = "Категориальный анализ: результаты представлены в таблице";
            ShowResultsWindow(
                "χ² Пирсона для независимых категориальных данных; для разреженных таблиц 2×2 " +
                "автоматически используется точный критерий Фишера; поправка Холма.",
                rows);
        }

        private static string FormatAnalysisSummary(DescriptiveStatistics statistics)
        {
            if (statistics.Count == 0)
            {
                return "n=0";
            }

            string standardDeviation = double.IsNaN(statistics.SampleStandardDeviation)
                ? "—"
                : FormatNumber(statistics.SampleStandardDeviation);
            return $"n={statistics.Count}; среднее±SD: {FormatNumber(statistics.Mean)}±{standardDeviation}; " +
                $"Me [Q1; Q3]: {FormatNumber(statistics.Median)} " +
                $"[{FormatNumber(statistics.FirstQuartile)}; {FormatNumber(statistics.ThirdQuartile)}]";
        }

        private static string FormatCategoryCounts(
            int totalCount,
            IReadOnlyList<string> categories,
            IReadOnlyList<int> counts)
        {
            return $"n={totalCount}; " + string.Join(
                "; ",
                categories.Select((category, index) => $"{category}: {counts[index]}"));
        }

        private static string FormatPValue(double pValue)
        {
            if (pValue > 0 && pValue < 0.0001)
            {
                return "<0,0001";
            }
            return pValue.ToString("0.####", CultureInfo.CurrentCulture);
        }

        private void ShowResultsWindow(
            string methodDescription,
            IReadOnlyList<StatisticalResultRow> rows)
        {
            var window = new StatisticalResultsWindow(G1.TestName, methodDescription, rows)
            {
                Owner = this
            };
            window.Show();
        }

    }
}
