using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

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

        private void TableStarterLegacy_Click(object sender, RoutedEventArgs e)
        {
        
            Check1 = true;
            Check2 = true;

            List<string> PTS = new List<string>();

            foreach (string s in PatientL) PTS.Add(s);

            try
            {
                foreach (Person person in Persons)
                {
                    if (person.PersonID != personDataGrids2[0].Info.ToString() && personDataGrids2[0].Info.ToString() != "")
                    {
                        PTS.Remove(person.PersonID);

                        //foreach (string s in PTS) MessageBox.Show(s.ToString()); 
                    }

                    if (person.PatientGroup != personDataGrids2[1].Info.ToString() && personDataGrids2[1].Info.ToString() != "")
                    {
                        PTS.Remove(person.PersonID);

                        //foreach (string s in PTS) MessageBox.Show(s.ToString()); 
                    }

                    if (person.Age.ToString() != personDataGrids2[2].Info.ToString() && personDataGrids2[2].Info.ToString() != "" && !personDataGrids2[2].Info.ToString().Contains('-')
                        && !personDataGrids2[2].Info.ToString().Contains('<') && !personDataGrids2[2].Info.ToString().Contains('>'))
                    {
                        PTS.Remove(person.PersonID);


                    }
                    else
                    {
                        if (personDataGrids2[2].Info.ToString().Contains('-'))
                        {
                            string[] Temp = personDataGrids2[2].Info.ToString().Split(new char[] { '-' });
                            int Temp1 = Int32.Parse(Temp[0]);
                           int Temp2 = Int32.Parse(Temp[1]);

                            if(person.Age<Temp1||person.Age>Temp2) PTS.Remove(person.PersonID);


                        }

                        if (personDataGrids2[2].Info.ToString().Contains('<'))
                        {
                            int Temp = Int32.Parse(personDataGrids2[2].Info.ToString().Replace("<", ""));
                            if (person.Age > Temp ) PTS.Remove(person.PersonID);

                        }

                        if (personDataGrids2[2].Info.ToString().Contains('>'))
                        {
                            int Temp = Int32.Parse(personDataGrids2[2].Info.ToString().Replace(">", ""));
                            if (person.Age < Temp) PTS.Remove(person.PersonID);

                        }

                    }

                    if (person.Sex.ToString() != personDataGrids2[3].Info.ToString() && personDataGrids2[3].Info.ToString() != "")
                    {
                        PTS.Remove(person.PersonID);

                        //foreach (string s in PTS) MessageBox.Show(s.ToString()); 
                    }

                    if (person.Height.ToString() != personDataGrids2[4].Info.ToString() && personDataGrids2[4].Info.ToString() != "" && !personDataGrids2[4].Info.ToString().Contains('-')
                        && !personDataGrids2[4].Info.ToString().Contains('<') && !personDataGrids2[4].Info.ToString().Contains('>'))
                    {
                        PTS.Remove(person.PersonID);

                        //foreach (string s in PTS) MessageBox.Show(s.ToString()); 
                    }
                    else
                    {
                        if (personDataGrids2[4].Info.ToString().Contains('-'))
                        {
                            string[] Temp = personDataGrids2[4].Info.ToString().Split(new char[] { '-' });
                            double Temp1 = double.Parse(Temp[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                            double Temp2 = double.Parse(Temp[1], NumberStyles.Any, CultureInfo.InvariantCulture);

                            if (person.Height < Temp1 || person.Height > Temp2) PTS.Remove(person.PersonID);


                        }

                        if (personDataGrids2[4].Info.ToString().Contains('<'))
                        {
                            double Temp = double.Parse(personDataGrids2[4].Info.ToString().Replace("<", ""), NumberStyles.Any, CultureInfo.InvariantCulture);
                            if (person.Height > Temp) PTS.Remove(person.PersonID);

                        }

                        if (personDataGrids2[4].Info.ToString().Contains('>'))
                        {
                            double Temp = double.Parse(personDataGrids2[4].Info.ToString().Replace(">", ""), NumberStyles.Any, CultureInfo.InvariantCulture);
                            if (person.Height < Temp) PTS.Remove(person.PersonID);

                        }

                    }

                    if (person.Weight.ToString() != personDataGrids2[5].Info.ToString() && personDataGrids2[5].Info.ToString() != "" && !personDataGrids2[5].Info.ToString().Contains('-')
                        && !personDataGrids2[5].Info.ToString().Contains('<') && !personDataGrids2[5].Info.ToString().Contains('>'))
                    {
                        PTS.Remove(person.PersonID);

                        //foreach (string s in PTS) MessageBox.Show(s.ToString()); 
                    }
                    else
                    {
                        if (personDataGrids2[5].Info.ToString().Contains('-'))
                        {
                            string[] Temp = personDataGrids2[5].Info.ToString().Split(new char[] { '-' });
                            double Temp1 = double.Parse(Temp[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                            double Temp2 = double.Parse(Temp[1], NumberStyles.Any, CultureInfo.InvariantCulture);

                            if (person.Weight < Temp1 || person.Weight > Temp2) PTS.Remove(person.PersonID);


                        }

                        if (personDataGrids2[5].Info.ToString().Contains('<'))
                        {
                            double Temp = double.Parse(personDataGrids2[5].Info.ToString().Replace("<", ""), NumberStyles.Any, CultureInfo.InvariantCulture);
                            if (person.Weight > Temp) PTS.Remove(person.PersonID);

                        }

                        if (personDataGrids2[5].Info.ToString().Contains('>'))
                        {
                            double Temp = double.Parse(personDataGrids2[5].Info.ToString().Replace(">", ""), NumberStyles.Any, CultureInfo.InvariantCulture);
                            if (person.Weight < Temp) PTS.Remove(person.PersonID);

                        }

                    }

                    if (!person.Complaints.ToString().Contains(personDataGrids2[6].Info.ToString()) && personDataGrids2[6].Info.ToString() != "")
                    {
                        PTS.Remove(person.PersonID);

                        //foreach (string s in PTS) MessageBox.Show(s.ToString()); 
                    }


                    if (person.Duration != personDataGrids2[7].Info.ToString() && personDataGrids2[7].Info.ToString() != "")
                    {
                        PTS.Remove(person.PersonID);

                        //foreach (string s in PTS) MessageBox.Show(s.ToString()); 
                    }

                    if (!person.Diagnosis.ToString().Contains(personDataGrids2[8].Info.ToString()) && person.Diagnosis.ToString() != personDataGrids2[8].Info.ToString() && personDataGrids2[8].Info.ToString() != "")
                    {
                        PTS.Remove(person.PersonID);

                        //foreach (string s in PTS) MessageBox.Show(s.ToString()); 
                    }


                    if (!person.AddDiagnosis.ToString().Contains(personDataGrids2[9].Info.ToString()) && person.AddDiagnosis.ToString() != personDataGrids2[9].Info.ToString() && personDataGrids2[9].Info.ToString() != "")
                    {
                        PTS.Remove(person.PersonID);

                        //foreach (string s in PTS) MessageBox.Show(s.ToString()); 
                    }


                    if (!person.Operation.ToString().Contains(personDataGrids2[10].Info.ToString()) && personDataGrids2[10].Info.ToString() != "")
                    {
                        PTS.Remove(person.PersonID);

                        //foreach (string s in PTS) MessageBox.Show(s.ToString()); 
                    }




                }
            }

            catch (OperationCanceledException)
            {
                MessageBox.Show("Проверьте введенные данные");
            }
            //foreach (string s in PTS) MessageBox.Show(s.ToString());

           if(PTS.Count!=null) N0 = PTS.Count;

            List<Test> tests1 = new List<Test>();

            foreach (Test t in Tests)
            {
                foreach(string s in  PTS)
                {
                    if(t.PersonID.Equals(s)) tests1.Add(t);
                }
            }

            // MainTestsOfMany.ItemsSource = tests;
           //List<string> to_delete= new List<string>();

            //test1 - Из него идет удаление

            List<Test> Del = new List<Test>();
           foreach(Test t in tests1)
           {
                int Z = 0;
                if (t.Day0 != "" && !double.TryParse(t.Day0, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (t.Day1 != "" && !double.TryParse(t.Day1, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (t.Day2 != "" && !double.TryParse(t.Day2, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (t.Day3 != "" && !double.TryParse(t.Day3, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (t.Day4 != "" && !double.TryParse(t.Day4, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (t.Day5 != "" && !double.TryParse(t.Day5, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (t.Day6 != "" && !double.TryParse(t.Day6, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (t.Day7 != "" && !double.TryParse(t.Day7, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (t.Day8 != "" && !double.TryParse(t.Day8, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (t.Day9_12 != "" && !double.TryParse(t.Day9_12, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (t.Day12_16 != "" && !double.TryParse(t.Day12_16, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) Z++;
                if (Z != 0) Del.Add(t);

           }
            List<Test> tests2 = new List<Test>();

            //test2 - без не парсных

            foreach (Test t in tests1) if(!Del.Contains(t)) tests2.Add(t);
                
            

          //  MainTestsOfMany.ItemsSource = tests2;

            List<string> NT = new List<string>();
            foreach (Test t in tests2)
            {
                if(NT!=null&&!NT.Contains(t.TestName)) NT.Add(t.TestName);
            }

           // MessageBox.Show(NT.Count().ToString());
            List<TestNum> tests3 = new List<TestNum>();

            //test3 - УЖЕ с ЧИСЛОВЫМИ

            //MessageBox.Show("O" + tests2[0].Day0.ToString()+"O");



           foreach (Test t in tests2)
           {
                TestNum Temp = new TestNum();
                Temp.PersonID = t.PersonID;
                Temp.TestName = t.TestName;

                if (t.Day0 != "" && double.TryParse(t.Day0, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    Temp.Day0 = double.Parse(t.Day0);
                else Temp.Day0 = 0.0;

                if (t.Day1 != "" && double.TryParse(t.Day1, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    Temp.Day1 = double.Parse(t.Day1);
                else Temp.Day1 = 0.0;

                if (t.Day2 != "" && double.TryParse(t.Day2, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    Temp.Day2 = double.Parse(t.Day2);
                else Temp.Day2 = 0.0;

                if (t.Day3 != "" && double.TryParse(t.Day3, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    Temp.Day3 = double.Parse(t.Day3);
                else Temp.Day3 = 0.0;

                if (t.Day4 != "" && double.TryParse(t.Day4, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    Temp.Day4 = double.Parse(t.Day4);
                else Temp.Day4 = 0.0;

                if (t.Day5 != "" && double.TryParse(t.Day5, NumberStyles.Any, CultureInfo.InvariantCulture, out _) )
                    Temp.Day5 = double.Parse(t.Day5);
                else Temp.Day5 = 0.0;

                if (t.Day6 != "" && double.TryParse(t.Day6, NumberStyles.Any, CultureInfo.InvariantCulture, out _) )
                    Temp.Day6 = double.Parse(t.Day6);
                else Temp.Day6 = 0.0;

                if (t.Day7 != "" && double.TryParse(t.Day7, NumberStyles.Any, CultureInfo.InvariantCulture, out _) )
                    Temp.Day7 = double.Parse(t.Day7);
                else Temp.Day7 = 0.0;

                if (t.Day8 != "" && double.TryParse(t.Day8, NumberStyles.Any, CultureInfo.InvariantCulture, out _) )
                    Temp.Day8 = double.Parse(t.Day8);
                else Temp.Day8 = 0.0;

                if (t.Day9_12 != "" && double.TryParse(t.Day9_12, NumberStyles.Any, CultureInfo.InvariantCulture, out _) )
                    Temp.Day9_12 = double.Parse(t.Day9_12);
                else Temp.Day9_12 = 0.0;

                if (t.Day12_16 != "" && double.TryParse(t.Day12_16, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                     Temp.Day12_16 = double.Parse(t.Day12_16);
                else Temp.Day12_16 = 0.0;

                tests3.Add(Temp);
            
           }

            TestCal.Clear();




           foreach (TestNum t in tests3)
           {
                int Z = 0;
                foreach (TestNum t2 in TestCal)
                {
                    if (t2.TestName==t.TestName) 
                    { Z++;

                        t2.Day0 += t.Day0;
                        t2.Day1 += t.Day1;
                        t2.Day2 += t.Day2;
                        t2.Day3 += t.Day3;
                        t2.Day4 += t.Day4;
                        t2.Day5 += t.Day5;
                        t2.Day6 += t.Day6;
                        t2.Day7 += t.Day7;
                        t2.Day8 += t.Day8;
                        t2.Day9_12 += t.Day9_12;
                        t2.Day12_16 += t.Day12_16;


                    }

                }

                if(Z==0)
                {
                    TestCal.Add(t);
                }


           }

           List<TestNum> Deviations  = new List<TestNum>();

           foreach (TestNum t in TestCal)
           {
                TestNum test = new TestNum();
                test.PersonID = t.PersonID;
                test.TestName = t.TestName;
                test.Day0 = t.Day0;
                test.Day1 = t.Day1;
                test.Day2 = t.Day2;
                test.Day3 = t.Day3;
                test.Day4 = t.Day4;
                test.Day5 = t.Day5;
                test.Day6 = t.Day6;
                test.Day7 = t.Day7;
                test.Day8 = t.Day8;
                test.Day9_12 = t.Day9_12;
                test.Day12_16 = t.Day12_16;
                Deviations.Add(test);



            }

           foreach(TestNum t in Deviations)
           {
                t.Day0 = 0.0;
                t.Day1 = 0.0;
                t.Day2 = 0.0;
                t.Day3 = 0.0;
                t.Day4 = 0.0;
                t.Day5 = 0.0;
                t.Day6 = 0.0;
                t.Day7 = 0.0;
                t.Day8 = 0.0;
                t.Day9_12 = 0.0;
                t.Day12_16 = 0.0;
           }

           /// Сделать так, чтобы шел учет налмичия либо отсутствия анализа в конкретные дни

           foreach (TestNum t in TestCal)
            
           {
                List<int> corrector = new List<int>();
                for (int i = 0; i < 11; i++) corrector.Add(0);

                foreach (var item in tests3)
                {
                   
                    if (t.TestName == item.TestName)
                       
                    {
                        if (item.Day0 == 0.0) corrector[0]++;
                        if (item.Day1 == 0.0) corrector[1]++;
                        if (item.Day2 == 0.0) corrector[2]++;
                        if (item.Day3 == 0.0) corrector[3]++;
                        if (item.Day4 == 0.0) corrector[4]++;
                        if (item.Day5 == 0.0) corrector[5]++;
                        if (item.Day6 == 0.0) corrector[6]++;
                        if (item.Day7 == 0.0) corrector[7]++;
                        if (item.Day8 == 0.0) corrector[8]++;
                        if (item.Day9_12 == 0.0) corrector[9]++;
                        if (item.Day12_16 == 0.0) corrector[10]++;

                       
                    }
                    
                }


                if (corrector[0] == PTS.Count) corrector[0] = 0;
                t.Day0 = Math.Round(t.Day0 / (PTS.Count - corrector[0]), 2);

                if (corrector[1] == PTS.Count) corrector[1] = 0;
                t.Day1 = Math.Round(t.Day1 / (PTS.Count - corrector[1]), 2);

                if (corrector[2] == PTS.Count) corrector[2] = 0;

                t.Day2 = Math.Round(t.Day2 / (PTS.Count - corrector[2]), 2);



                if (corrector[3] == PTS.Count) corrector[3] = 0;
                t.Day3 = Math.Round(t.Day3 / (PTS.Count - corrector[3]), 2);

                if (corrector[4] == PTS.Count) corrector[4] = 0;
                t.Day4 = Math.Round(t.Day4 / (PTS.Count - corrector[4]), 2);

                if (corrector[5] == PTS.Count) corrector[5] = 0;
                t.Day5 = Math.Round(t.Day5 / (PTS.Count - corrector[5]), 2);

                if (corrector[6] == PTS.Count) corrector[6] = 0;
                t.Day6 = Math.Round(t.Day6 / (PTS.Count - corrector[6]), 2);

                if (corrector[7] == PTS.Count) corrector[7] = 0;
                t.Day7 = Math.Round(t.Day7 / (PTS.Count - corrector[7]), 2);

                if (corrector[8] == PTS.Count) corrector[8] = 0;
                t.Day8 = Math.Round(t.Day8 / (PTS.Count - corrector[8]), 2);

                if (corrector[9] == PTS.Count) corrector[9] = 0;
                t.Day9_12 = Math.Round(t.Day9_12 / (PTS.Count - corrector[9]), 2);

                if (corrector[10] == PTS.Count) corrector[10] = 0;
                t.Day12_16 = Math.Round(t.Day12_16 / (PTS.Count - corrector[10]), 2);
            
           }

           if(PTS.Count>1)foreach (TestNum t in Deviations)
           {
                TestNum Temp = new TestNum();

                foreach( TestNum tt in tests3)
                {
                    if(t.TestName == tt.TestName)
                    {

                        foreach (TestNum ttt in TestCal) if (t.TestName == ttt.TestName) Temp = ttt;

                        t.Day0 = (tt.Day0 - Temp.Day0) *(tt.Day0 - Temp.Day0);
                        t.Day1 = (tt.Day1 - Temp.Day1) * (tt.Day1 - Temp.Day1);
                        t.Day2 = (tt.Day2 - Temp.Day2) * (tt.Day2 - Temp.Day2);
                        t.Day3 = (tt.Day3 - Temp.Day3) * (tt.Day3 - Temp.Day3);
                        t.Day4 = (tt.Day4 - Temp.Day4) * (tt.Day4 - Temp.Day4);
                        t.Day5 = (tt.Day5 - Temp.Day5) * (tt.Day5 - Temp.Day5);
                        t.Day6 = (tt.Day6 - Temp.Day6) * (tt.Day6 - Temp.Day6);
                        t.Day7 = (tt.Day7 - Temp.Day7) * (tt.Day7 - Temp.Day7);
                        t.Day8 = (tt.Day8 - Temp.Day8) * (tt.Day8 - Temp.Day8);
                        t.Day9_12 = (tt.Day9_12 - Temp.Day9_12) * (tt.Day9_12 - Temp.Day9_12);
                        t.Day12_16 = (tt.Day12_16 - Temp.Day12_16) * (tt.Day12_16 - Temp.Day12_16);

                    }
                }


           }

            if (PTS.Count > 1)
                foreach (TestNum t in Deviations)
                {
                    t.Day0 = Math.Sqrt(t.Day0 / (PTS.Count - 1));
                    t.Day1 = Math.Sqrt(t.Day1 / (PTS.Count - 1));
                    t.Day2 = Math.Sqrt(t.Day2 / (PTS.Count - 1));
                    t.Day3 = Math.Sqrt(t.Day3 / (PTS.Count - 1));
                    t.Day4 = Math.Sqrt(t.Day4 / (PTS.Count - 1));
                    t.Day5 = Math.Sqrt(t.Day5 / (PTS.Count - 1));
                    t.Day6 = Math.Sqrt(t.Day6 / (PTS.Count - 1));
                    t.Day7 = Math.Sqrt(t.Day7 / (PTS.Count - 1));
                    t.Day8 = Math.Sqrt(t.Day8 / (PTS.Count - 1));
                    t.Day9_12 = Math.Sqrt(t.Day9_12 / (PTS.Count - 1));
                    t.Day12_16 = Math.Sqrt(t.Day12_16 / (PTS.Count - 1));

                    t.Day0 = Math.Round(t.Day0, 2);
                    t.Day1 = Math.Round(t.Day1, 2);
                    t.Day2 = Math.Round(t.Day2, 2);
                    t.Day3 = Math.Round(t.Day3, 2);
                    t.Day4 = Math.Round(t.Day4, 2);
                    t.Day5 = Math.Round(t.Day5, 2);
                    t.Day6 = Math.Round(t.Day6, 2);
                    t.Day7 = Math.Round(t.Day7, 2);
                    t.Day8 = Math.Round(t.Day8, 2);
                    t.Day9_12 = Math.Round(t.Day9_12, 2);
                    t.Day12_16 = Math.Round(t.Day12_16, 2);



                }

            List<Test> tests4 = new List<Test>();

            

            foreach (var t in TestCal)
            {
                Test test = new Test();
                test.PersonID = t.PersonID;
                test.TestName = t.TestName;
                test.Day0 = t.Day0.ToString();
                test.Day1 = t.Day1.ToString();
                test.Day2 = t.Day2.ToString();
                test.Day3 = t.Day3.ToString();
                test.Day4 = t.Day4.ToString();
                test.Day5 = t.Day5.ToString();
                test.Day6 = t.Day6.ToString();
                test.Day7 = t.Day7.ToString();
                test.Day8 = t.Day8.ToString();
                test.Day9_12 = t.Day9_12.ToString();
                test.Day12_16 = t.Day12_16.ToString();

                tests4.Add(test);
            }

            if (PTS.Count > 1)
            {
                foreach (var t in tests4)
                {
                    foreach (var tt in Deviations)
                    {
                        if(t.TestName==tt.TestName)
                        {
                            if (t.Day0 != "0")
                            { 
                                t.Day0 += "±";
                                t.Day0 += tt.Day0.ToString();
                            }

                            if (t.Day1 != "0")
                            {
                                t.Day1 += "±";
                                t.Day1 += tt.Day1.ToString();
                            }

                            if (t.Day2 != "0")
                            {
                                t.Day2 += "±";
                                t.Day2 += tt.Day2.ToString();
                            }

                            if (t.Day3 != "0")
                            {
                                t.Day3 += "±";
                                t.Day3 += tt.Day3.ToString();
                            }

                            if (t.Day4 != "0")
                            {
                                t.Day4 += "±";
                                t.Day4 += tt.Day4.ToString();
                            }

                            if (t.Day5 != "0")
                            {
                                t.Day5 += "±";
                                t.Day5 += tt.Day5.ToString();
                            }

                            if (t.Day6 != "0")
                            {
                                t.Day6 += "±";
                                t.Day6 += tt.Day6.ToString();
                            }

                            if (t.Day7 != "0")
                            {
                                t.Day7 += "±";
                                t.Day7 += tt.Day7.ToString();
                            }

                            if (t.Day8 != "0")
                            {
                                t.Day8 += "±";
                                t.Day8 += tt.Day8.ToString();
                            }

                            if (t.Day9_12 != "0")
                            {
                                t.Day9_12 += "±";
                                t.Day9_12 += tt.Day9_12.ToString();
                            }

                            if (t.Day12_16 != "0")
                            {
                                t.Day12_16 += "±";
                                t.Day12_16 += tt.Day12_16.ToString();
                            }

                        }
                    }
                }
            }

            foreach (Test t in tests4)
            {
                if (t.Day0 == "0") t.Day0 = "";
                if (t.Day1 == "0") t.Day1 = "";
                if (t.Day2 == "0") t.Day2 = "";
                if (t.Day3 == "0") t.Day3 = "";
                if (t.Day4 == "0") t.Day4 = "";
                if (t.Day5 == "0") t.Day5 = "";
                if (t.Day6 == "0") t.Day6 = "";
                if (t.Day7 == "0") t.Day7 = "";
                if (t.Day8 == "0") t.Day8 = "";
                if (t.Day9_12 == "0") t.Day9_12 = "";
                if (t.Day12_16 == "0") t.Day12_16 = "";
            }

            MainTestsOfMany.ItemsSource = tests4;
            HowMany.Text = $"Отображено пациентов: {PTS.Count()}";


            foreach (var t in tests4)
            {
                TestsList.Add(t.TestName);
            }

            Group1.ItemsSource = TestsList;
            Group2.ItemsSource = TestsList;



        }

        private void Group1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(TestCal.Count!=0)
            {
                foreach (var t in TestCal)
                {
                    if (t.TestName == Group1.SelectedItem.ToString()) G1=t;
                }


                if (Check1)
                {
                    Group1name.Text = "Группа 1: ";

                    foreach (var t in personDataGrids2)
                    {
                        if (t.Info != "") { Group1name.Text += t.Info; Group1name.Text += "; "; }

                    }
                }

                if (Group1name.Text == "Группа 1: ") Group1name.Text += "Все пациенты";
                Check1 = false;

                N1 = N0;
            }
        }

        private void Group2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestCal.Count != 0)
            {
                foreach (var t in TestCal)
                {
                    if (t.TestName == Group2.SelectedItem.ToString()) G2 = t;
                }

                if (Check2)
                {
                    Group2name.Text = "Группа 2: ";

                    foreach (var t in personDataGrids2)
                    {
                        if (t.Info != "") { Group2name.Text += t.Info; Group2name.Text += "; "; }

                    }
                }

                if (Group2name.Text == "Группа 2: ") Group2name.Text += "Все пациенты";
                Check2 = false;

                N2 = N0;
            }
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {

            if(G1.TestName!=""&&G2.TestName!="")
            {


                Desk.Children.Remove(PG1);
                Desk.Children.Remove(PG2);



                List<double> ListG1 = new List<double>();
                List<double> ListG2 = new List<double>();

                double temp;

                temp = G1.Day0;
                ListG1.Add(temp);
                temp = G1.Day1;
                ListG1.Add(temp);
                temp = G1.Day2;
                ListG1.Add(temp);
                temp = G1.Day3;
                ListG1.Add(temp);
                temp = G1.Day4;
                ListG1.Add(temp);
                temp = G1.Day5;
                ListG1.Add(temp);
                temp = G1.Day6;
                ListG1.Add(temp);
                temp = G1.Day7;
                ListG1.Add(temp);
                temp = G1.Day8;
                ListG1.Add(temp);
                temp = G1.Day9_12;
                ListG1.Add(temp);
                temp = G1.Day12_16;
                ListG1.Add(temp);

                temp = G2.Day0;
                ListG2.Add(temp);
                temp = G2.Day1;
                ListG2.Add(temp);
                temp = G2.Day2;
                ListG2.Add(temp);
                temp = G2.Day3;
                ListG2.Add(temp);
                temp = G2.Day4;
                ListG2.Add(temp);
                temp = G2.Day5;
                ListG2.Add(temp);
                temp = G2.Day6;
                ListG2.Add(temp);
                temp = G2.Day7;
                ListG2.Add(temp);
                temp = G2.Day8;
                ListG2.Add(temp);
                temp = G2.Day9_12;
                ListG2.Add(temp);
                temp = G2.Day12_16;
                ListG2.Add(temp);

                double YMax = 0;
                foreach(var item in ListG1)
                {
                    if (YMax<item) YMax = item; 
                }
                foreach (var item in ListG2)
                {
                    if (YMax < item) YMax = item;
                }

                double YMaxMax = YMax * 1.1;
                
                YMaxMax = Math.Round(YMax / 10) * 10;

                double YTemp = YMaxMax;
                Y10.Text = YTemp.ToString();

                YTemp = YMaxMax * 0.9;
                Y9.Text = YTemp.ToString();

                YTemp = YMaxMax * 0.8;
                Y8.Text = YTemp.ToString();

                YTemp = YMaxMax * 0.7;
                Y7.Text = YTemp.ToString();

                YTemp = YMaxMax * 0.6;
                Y6.Text = YTemp.ToString();

                YTemp = YMaxMax * 0.5;
                Y5.Text = YTemp.ToString();

                YTemp = YMaxMax * 0.4;
                Y4.Text = YTemp.ToString();

                YTemp = YMaxMax * 0.3;
                Y3.Text = YTemp.ToString();

                YTemp = YMaxMax * 0.2;
                Y2.Text = YTemp.ToString();

                YTemp = YMaxMax * 0.1;
                Y1.Text = YTemp.ToString();


                PointCollection PC = new PointCollection();

                Point PT = new Point();

                PT.X = 240;
                PT.Y = 1300-(((G1.Day0 / YMaxMax) * 1000)+200);
                if(G1.Day0!=0) PC.Add(PT);

                PT.X = 360;
                PT.Y = 1300 - (((G1.Day1 / YMaxMax) * 1000) + 200);
                if (G1.Day1 != 0) PC.Add(PT);

                PT.X = 480;
                PT.Y = 1300 - (((G1.Day2 / YMaxMax) * 1000) + 200);
                if (G1.Day2 != 0) PC.Add(PT);

                PT.X = 600;
                PT.Y = 1300 - (((G1.Day3 / YMaxMax) * 1000) + 200);
                if (G1.Day3 != 0) PC.Add(PT);

                PT.X = 720;
                PT.Y = 1300 - (((G1.Day4 / YMaxMax) * 1000) + 200);
                if (G1.Day4 != 0) PC.Add(PT);

                PT.X = 840;
                PT.Y = 1300 - (((G1.Day5 / YMaxMax) * 1000) + 200);
                if (G1.Day5 != 0) PC.Add(PT);

                PT.X = 960;
                PT.Y = 1300 - (((G1.Day6 / YMaxMax) * 1000) + 200);
                if (G1.Day6 != 0) PC.Add(PT);

                PT.X = 1080;
                PT.Y = 1300 - (((G1.Day7 / YMaxMax) * 1000) + 200);
                if (G1.Day7 != 0) PC.Add(PT);

                PT.X = 1200;
                PT.Y = 1300 - (((G1.Day8 / YMaxMax) * 1000) + 200);
                if (G1.Day8 != 0) PC.Add(PT);

                PT.X = 1320;
                PT.Y = 1300 - (((G1.Day9_12 / YMaxMax) * 1000) + 200);
                if (G1.Day9_12 != 0) PC.Add(PT);

                PT.X = 1440;
                PT.Y = 1300 - (((G1.Day12_16 / YMaxMax) * 1000) + 200);
                if (G1.Day12_16 != 0) PC.Add(PT);

                PG1.Points = PC;

                PG1.Stroke = Brushes.Red;
                PG1.StrokeThickness = 3;

                Desk.Children.Add(PG1);
                ////////////////////////////////////////////////////////////////////////////////
                PC = new PointCollection();

                
                PT.X = 240;
                PT.Y = 1300 - (((G2.Day0 / YMaxMax) * 1000) + 200);
                if (G2.Day0 != 0) PC.Add(PT);

                PT.X = 360;
                PT.Y = 1300 - (((G2.Day1 / YMaxMax) * 1000) + 200);
                if (G2.Day1 != 0) PC.Add(PT);

                PT.X = 480;
                PT.Y = 1300 - (((G2.Day2 / YMaxMax) * 1000) + 200);
                if (G2.Day2 != 0) PC.Add(PT);

                PT.X = 600;
                PT.Y = 1300 - (((G2.Day3 / YMaxMax) * 1000) + 200);
                if (G2.Day3 != 0) PC.Add(PT);

                PT.X = 720;
                PT.Y = 1300 - (((G2.Day4 / YMaxMax) * 1000) + 200);
                if (G2.Day4 != 0) PC.Add(PT);

                PT.X = 840;
                PT.Y = 1300 - (((G2.Day5 / YMaxMax) * 1000) + 200);
                if (G2.Day5 != 0) PC.Add(PT);

                PT.X = 960;
                PT.Y = 1300 - (((G2.Day6 / YMaxMax) * 1000) + 200);
                if (G2.Day6 != 0) PC.Add(PT);

                PT.X = 1080;
                PT.Y = 1300 - (((G2.Day7 / YMaxMax) * 1000) + 200);
                if (G2.Day7 != 0) PC.Add(PT);

                PT.X = 1200;
                PT.Y = 1300 - (((G2.Day8 / YMaxMax) * 1000) + 200);
                if (G2.Day8 != 0) PC.Add(PT);

                PT.X = 1320;
                PT.Y = 1300 - (((G2.Day9_12 / YMaxMax) * 1000) + 200);
                if (G2.Day9_12 != 0) PC.Add(PT);

                PT.X = 1440;
                PT.Y = 1300 - (((G2.Day12_16 / YMaxMax) * 1000) + 200);
                if (G2.Day12_16 != 0) PC.Add(PT);

                PG2.Points = PC;

                PG2.Stroke = Brushes.Blue;
                PG2.StrokeThickness = 3;

                Desk.Children.Add(PG2);

                DeskGroup1.Text = Group1name.Text;
                DeskGroup1.Text += G1.TestName.ToString();

                DeskGroup2.Text = Group2name.Text;
                DeskGroup2.Text += G2.TestName.ToString();

                List<double> ListAll = new List<double>();

                List<double> ListG1Clear = new List<double>();
                List<double> ListG2Clear = new List<double>();

                foreach (double item in ListG1)
                {
                    if (item!=0) {ListAll.Add(item); ListG1Clear.Add(item); }
                }

                foreach (double item in ListG2)
                {
                    if (item != 0) { ListAll.Add(item); ListG2Clear.Add(item); }
                }

                List<int> ListRang = new List<int>();

                foreach (var item in ListAll)
                {
                    ListRang.Add(1);
                }


                double T;

                for ( int i = 0; i < (ListAll.Count-1); i++)
                {
                    T = ListAll[i];
                    foreach (double item in ListAll)
                    {
                        if (T > item) ListRang[i]++;
                    }
                }

                string GG = "";
                foreach (var item in ListRang) { GG += item.ToString(); GG += " "; }
                MessageBox.Show(GG);

                int R1=0;

                for( int i = 0; i < (ListG1Clear.Count-1); i++)
                {
                    R1 += ListRang[i];
                }

                int R2=0;

                for (int i = ListG2Clear.Count; i < ListAll.Count - 1; i++)
                { R2 += ListRang[i]; }

                MessageBox.Show(R1.ToString() + " " + R2.ToString());

                double U1 = ListG1Clear.Count * ListG2Clear.Count + (ListG1Clear.Count * (ListG1Clear.Count + 1)) / 2 - R1;
                double U2 = ListG1Clear.Count * ListG2Clear.Count + (ListG2Clear.Count * (ListG2Clear.Count + 1)) / 2 - R2;
                double U;
                if (U1<=U2)  U=U1; else U=U2;

                MessageBox.Show($"U1 = {U1} ///  U2 =  {U2}  ///  U= {U}");

                //double Z = U - (ListG1.Count * ListG2.Count / 2) / Math.Sqrt(ListG1.Count * ListG2.Count * (ListG1.Count + ListG2.Count + (ListAll.Count + 1) / 12));

                //MessageBox.Show(Z.ToString());


            }









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