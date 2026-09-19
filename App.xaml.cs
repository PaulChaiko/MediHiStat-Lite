using Microsoft.Data.Sqlite;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MediHiStat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string DatabaseConnectionString = "Data Source=mydatabase.db";
        private bool _isInitialized;

        private static void InitializeDatabase()
        {
            using var connection = new SqliteConnection(DatabaseConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS Person (
                    PersonID TEXT,
                    PatientGroup TEXT,
                    Age INTEGER,
                    Sex TEXT,
                    Height REAL,
                    Weight REAL,
                    Complaints TEXT,
                    Duration TEXT,
                    Diagnosis TEXT,
                    AddDiagnosis TEXT,
                    Operation TEXT
                );

                CREATE TABLE IF NOT EXISTS Test (
                    PersonID TEXT,
                    TestName TEXT,
                    Day0 TEXT,
                    Day1 TEXT,
                    Day2 TEXT,
                    Day3 TEXT,
                    Day4 TEXT,
                    Day5 TEXT,
                    Day6 TEXT,
                    Day7 TEXT,
                    Day8 TEXT,
                    Day9_12 TEXT,
                    Day12_16 TEXT
                );
                """;
            command.ExecuteNonQuery();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            base.OnStartup(e);

            try
            {
                InitializeDatabase();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"Не удалось инициализировать базу данных.\n\n{exception.Message}",
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var logoWindow = new LogoWindow();


            logoWindow.Show();

            // Анимация появления логотипа
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            logoWindow.LogoImage.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            // Анимация появления текста статуса с задержкой
            var textFadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                BeginTime = TimeSpan.FromSeconds(0.5),
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            logoWindow.StatusText.BeginAnimation(UIElement.OpacityProperty, textFadeIn);

            // Анимация прогресс-бара
            var progressAnimation = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = TimeSpan.FromSeconds(3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // Обновление текста статуса во время прогресса
            progressAnimation.CurrentTimeInvalidated += (s, args) =>
            {
                var progress = (int)logoWindow.LoadingProgressBar.Value;
                logoWindow.StatusText.Text = progress switch
                {
                    < 30 => "Инициализация...",
                    < 60 => "Загрузка данных...",
                    < 90 => "Завершение...",
                    _ => "Готово!"
                };
            };

            logoWindow.LoadingProgressBar.BeginAnimation(RangeBase.ValueProperty, progressAnimation);

            // Таймер для закрытия окна
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };

            EventHandler handler = null;

            handler = (sender, args) =>
            {
                timer.Tick -= handler;
                timer.Stop();
                timer = null;
                var mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                logoWindow.Owner = null;
                logoWindow.Close();
                logoWindow = null;
            };

            timer.Tick += handler;
            timer.Start();


        }
    }

}
