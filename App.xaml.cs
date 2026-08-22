using System.Configuration;
using System.Data;
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
        private bool _isInitialized;
        protected override void OnStartup(StartupEventArgs e)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            base.OnStartup(e);

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
