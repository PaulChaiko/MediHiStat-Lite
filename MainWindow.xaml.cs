using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text;

using System.Runtime.CompilerServices;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

    public class Personalisator : INotifyPropertyChanged
    {


        private string _PersonID;

        public string PersonID
        {
            get => _PersonID;
            set { _PersonID = value; OnPropertyChanged(); }
        }

        private string _PatientGroup;

        public string PatientGroup
        {
            get => _PatientGroup;
            set { _PatientGroup = value; OnPropertyChanged(); }
        }

        private int _Age;
        public int Age
        {
            get => _Age; set { _Age = value; OnPropertyChanged(); }
        }


        private string _Sex;
        public string Sex
        {
            get => _Sex; set { _Sex = value; OnPropertyChanged(); }
        }


        private int _Weight;
        public int Weight
        {
            get => _Weight; set { _Weight = value; OnPropertyChanged(); }
        }

        private int _Height;
        public int Height
        {
            get => _Height; set { _Height = value; OnPropertyChanged(); }
        }
        private string _Complaints;
        public string Complaints
        {
            get => _Complaints; set { _Complaints = value; OnPropertyChanged(); }
        }

        private string _Duration;
        public string Duration
        {
            get => _Duration; set { _Duration = value; OnPropertyChanged(); }
        }

        private string _Diagnosis;
        public string Diagnosis
        {
            get => _Diagnosis; set { _Diagnosis = value; OnPropertyChanged(); }
        }

        private string _AddDiagnosis;
        public string AddDiagnosis
        {
            get => _AddDiagnosis; set { _AddDiagnosis = value; OnPropertyChanged(); }
        }

        private string _Operation;

        public string Operation
        {
            get => _Operation; set { _Operation = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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


    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Test> Tests { get; }

        public MainViewModel()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class MainWindow : Window
    {

        public List<Person> Persons = new List<Person>();

        public List<Test> Tests = new List<Test>();

        public List<Test> TestsOfOne = new List<Test>();

        public Person ActivePerson = new Person();

        public Personalisator PL = new Personalisator();

        int N0=0;
        int N1=0;
        int N2=0;


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

        Polyline PG1 = new Polyline();
        Polyline PG2 = new Polyline();


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





        public class TestsFirst
        {
            public int PatientID { get; set; }
            public string Day { get; set; }
            public string BP { get; set; }
            public int HR { get; set; }
            public int RR { get; set; }
            public int T { get; set; }
            public string ECG { get; set; }
            public int BileVolume { get; set; }
            public int Ht { get; set; }
            public int Hb { get; set; }
            public int Erythrocytes { get; set; }
            public int Platelets { get; set; }
            public int Leukocytes { get; set; }
            public int Neutrophils { get; set; }
            public int Lymphocytes { get; set; }
            public int TotalProtein { get; set; }
            public int Albumin { get; set; }
            public int AST { get; set; }
            public int ALT { get; set; }
            public int ALP { get; set; }
            public int GGT { get; set; }
            public int LDH { get; set; }
            public int TotalBilirubin { get; set; }
            public int DirectBilirubin { get; set; }
            public int BloodAmylase { get; set; }
            public int BloodGlucose { get; set; }
            public int Potassium { get; set; }
            public int Sodium { get; set; }
            public int Magnesium { get; set; }
            public int Calcium { get; set; }
            public int Iron { get; set; }
            public int Creatinine { get; set; }
            public int CreatinineClearance { get; set; }
            public int BloodUrea { get; set; }
            public int UricAcid { get; set; }
            public int C_reactiveProtein { get; set; }
            public int TotalCholesterol { get; set; }

            public int Triglycerides { get; set; }
            public int HDL { get; set; }
            public int LDL { get; set; }
            public int Procalcitonin { get; set; }
            public int INR { get; set; }
            public int PT { get; set; }
            public int PTI { get; set; }
            public int APTT { get; set; }
            public int Fibrinogen { get; set; }
            public int UrineWeight { get; set; }
            public int pH { get; set; }
            public int Nitrites { get; set; }
            public int Protein { get; set; }
            public int UrineGlucose { get; set; }
            public int Ketones { get; set; }
            public string Urobilinogen { get; set; }
            public string UrineBilirubin { get; set; }
            public int UrineRedBloodCells { get; set; }
            public int UrineWhiteBloodCells { get; set; }
            public int NumberBondTestSec { get; set; }
            public int PE_stage { get; set; }
            public int CVR { get; set; }
            public int CCR { get; set; }
            public int Choledocholedochus { get; set; }
            public int PancreaticHead { get; set; }
            public string Intrahepatic { get; set; }
            public int GalleryWall { get; set; }
            public int GalleryLength { get; set; }
            public int GalleryWidth { get; set; }
            public string BlockLevel { get; set; }
            public string HIV { get; set; }
            public string Hepatitis_B { get; set; }
            public string Hepatitis_C { get; set; }
            public string Syphilis { get; set; }

            public TestsFirst()
            {
                Day = "";
                BP = "";
                ECG = "";
                Urobilinogen = "";
                UrineBilirubin = "";
                Intrahepatic = "";
                BlockLevel = "";
                HIV = "";
                Hepatitis_B = "";
                Hepatitis_C = "";
                Syphilis = "";
            }

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

            string mean = Math.Round(statistics.Mean, 2).ToString("0.##", CultureInfo.CurrentCulture);
            if (statistics.Count == 1 || double.IsNaN(statistics.SampleStandardDeviation))
            {
                return mean;
            }

            string standardDeviation = Math.Round(statistics.SampleStandardDeviation, 2)
                .ToString("0.##", CultureInfo.CurrentCulture);
            return $"{mean}±{standardDeviation}";
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
            N1 = Group1PatientIds.Count;
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
            N2 = Group2PatientIds.Count;
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

        private void DrawComparisonGraph(
            IReadOnlyList<List<double>> group1Samples,
            IReadOnlyList<List<double>> group2Samples)
        {
            double?[] group1Means = group1Samples
                .Select(sample => sample.Count == 0 ? (double?)null : sample.Average())
                .ToArray();
            double?[] group2Means = group2Samples
                .Select(sample => sample.Count == 0 ? (double?)null : sample.Average())
                .ToArray();

            double[] availableMeans = group1Means
                .Concat(group2Means)
                .Where(mean => mean.HasValue)
                .Select(mean => mean!.Value)
                .ToArray();

            if (availableMeans.Length == 0)
            {
                return;
            }

            double maximum = Math.Max(0, availableMeans.Max());
            double scaleMaximum = maximum > 0 ? maximum * 1.1 : 1;

            Y10.Text = Math.Round(scaleMaximum, 2).ToString(CultureInfo.CurrentCulture);
            Y9.Text = Math.Round(scaleMaximum * 0.9, 2).ToString(CultureInfo.CurrentCulture);
            Y8.Text = Math.Round(scaleMaximum * 0.8, 2).ToString(CultureInfo.CurrentCulture);
            Y7.Text = Math.Round(scaleMaximum * 0.7, 2).ToString(CultureInfo.CurrentCulture);
            Y6.Text = Math.Round(scaleMaximum * 0.6, 2).ToString(CultureInfo.CurrentCulture);
            Y5.Text = Math.Round(scaleMaximum * 0.5, 2).ToString(CultureInfo.CurrentCulture);
            Y4.Text = Math.Round(scaleMaximum * 0.4, 2).ToString(CultureInfo.CurrentCulture);
            Y3.Text = Math.Round(scaleMaximum * 0.3, 2).ToString(CultureInfo.CurrentCulture);
            Y2.Text = Math.Round(scaleMaximum * 0.2, 2).ToString(CultureInfo.CurrentCulture);
            Y1.Text = Math.Round(scaleMaximum * 0.1, 2).ToString(CultureInfo.CurrentCulture);

            Desk.Children.Remove(PG1);
            Desk.Children.Remove(PG2);

            PG1 = CreateGraphLine(group1Means, scaleMaximum, Brushes.Red);
            PG2 = CreateGraphLine(group2Means, scaleMaximum, Brushes.Blue);
            Desk.Children.Add(PG1);
            Desk.Children.Add(PG2);

            DeskGroup1.Text = $"{Group1name.Text} {G1.TestName}";
            DeskGroup2.Text = $"{Group2name.Text} {G2.TestName}";
        }

        private static Polyline CreateGraphLine(
            IReadOnlyList<double?> means,
            double scaleMaximum,
            Brush color)
        {
            var points = new PointCollection();

            for (int index = 0; index < means.Count; index++)
            {
                if (!means[index].HasValue)
                {
                    continue;
                }

                double x = 240 + 120 * index;
                double y = 1300 - ((means[index]!.Value / scaleMaximum * 1000) + 200);
                points.Add(new Point(x, y));
            }

            return new Polyline
            {
                Points = points,
                Stroke = color,
                StrokeThickness = 3
            };
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
                    "Группы пересекаются по пациентам. U-критерий Манна—Уитни применяется к независимым группам; сформируйте непересекающиеся выборки.",
                    "Статистическая обработка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            List<double>[] group1Samples = ExtractTimePointSamples(Group1Observations);
            List<double>[] group2Samples = ExtractTimePointSamples(Group2Observations);
            DrawComparisonGraph(group1Samples, group2Samples);

            string[] timePointNames =
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

            var resultText = new StringBuilder();
            resultText.AppendLine($"Показатель: {G1.TestName}");
            resultText.AppendLine("U-критерий Манна—Уитни, двусторонний p-value");
            resultText.AppendLine();

            for (int index = 0; index < timePointNames.Length; index++)
            {
                if (group1Samples[index].Count == 0 || group2Samples[index].Count == 0)
                {
                    resultText.AppendLine($"{timePointNames[index]}: недостаточно числовых данных");
                    continue;
                }

                MannWhitneyResult result = StatisticsCalculator.CalculateMannWhitney(
                    group1Samples[index],
                    group2Samples[index]);
                string method = result.UsedExactPValue ? "точный" : "асимптотический";
                resultText.AppendLine(
                    $"{timePointNames[index]}: n₁={result.Group1Count}; n₂={result.Group2Count}; " +
                    $"U={Math.Round(result.U, 3)}; p={result.PValue.ToString("0.####", CultureInfo.CurrentCulture)} ({method})");
            }

            MessageBox.Show(resultText.ToString(), "Статистическая обработка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}