using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MediHiStat
{
    /// <summary>Read-only snapshots of the patient table or the displayed group aggregates.</summary>
    public sealed class FullTableWindow : Window
    {
        private static readonly string[] TimeHeaders =
        {
            "Поступление", "1 сутки", "2 сутки", "3 сутки", "4 сутки", "5 сутки",
            "6 сутки", "7 сутки", "8 сутки", "9–12 сутки", "12–16 сутки"
        };
        private static readonly string[] TimeProperties =
        {
            "Day0", "Day1", "Day2", "Day3", "Day4", "Day5", "Day6", "Day7", "Day8",
            "Day9_12", "Day12_16"
        };

        private FullTableWindow(string title, string caption, IEnumerable<Test> rows,
            IEnumerable<PersonDataGrid>? characteristics = null)
        {
            Title = title;
            Width = 1380;
            Height = 760;
            MinWidth = 850;
            MinHeight = 450;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var root = new DockPanel { Margin = new Thickness(10) };
            Content = root;
            var heading = new TextBlock { Text = caption, FontSize = 15,
                FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10) };
            DockPanel.SetDock(heading, Dock.Top);
            root.Children.Add(heading);

            var close = new Button { Content = "Закрыть", IsCancel = true, MinWidth = 100,
                Padding = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0) };
            close.Click += (_, _) => Close();
            DockPanel.SetDock(close, Dock.Bottom);
            root.Children.Add(close);

            var layout = new Grid();
            if (characteristics is not null)
            {
                layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(290) });
                layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var patientGrid = MakeGrid();
                patientGrid.Columns.Add(MakeColumn("Характеристика", "Header", 135));
                patientGrid.Columns.Add(MakeColumn("Информация", "Info", 150));
                patientGrid.ItemsSource = characteristics.Select(item => new PersonDataGrid
                    { Header = item.Header, Info = item.Info }).ToList();
                patientGrid.Margin = new Thickness(0, 0, 10, 0);
                layout.Children.Add(patientGrid);
            }

            var testsGrid = MakeGrid();
            testsGrid.Columns.Add(MakeColumn("Показатель", "TestName", 200));
            for (int index = 0; index < TimeHeaders.Length; index++)
                testsGrid.Columns.Add(MakeColumn(TimeHeaders[index], TimeProperties[index], 170));
            // WPF coerces this property to the current column count. Set it only
            // after creating columns so the indicator remains fixed when scrolling.
            testsGrid.FrozenColumnCount = 1;
            testsGrid.ItemsSource = rows.Select(CloneRow).ToList();
            if (characteristics is not null) Grid.SetColumn(testsGrid, 1);
            layout.Children.Add(testsGrid);
            root.Children.Add(layout);
        }

        public static FullTableWindow CreatePatient(string patientId,
            IEnumerable<PersonDataGrid> characteristics, IEnumerable<Test> rows) =>
            new($"Полная таблица пациента — {patientId}",
                $"Пациент: {patientId}. Характеристики и лабораторные показатели", rows, characteristics);

        public static FullTableWindow CreateGroup(string groupCaption, IEnumerable<Test> aggregateRows) =>
            new($"Полная таблица группы — {groupCaption}",
                $"{groupCaption}\nДля каждого срока: n, среднее ± SD, Me [Q1; Q3]", aggregateRows);

        public void ShowForOwner(Window owner)
        {
            Owner = owner;
            ShowDialog();
        }

        private static DataGrid MakeGrid() => new()
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            SelectionUnit = DataGridSelectionUnit.Cell,
            SelectionMode = DataGridSelectionMode.Extended,
            ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        private static DataGridTextColumn MakeColumn(string header, string property, double width)
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(5)));
            style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            return new DataGridTextColumn { Header = header, Binding = new Binding(property),
                Width = width, MinWidth = 100, IsReadOnly = true, ElementStyle = style };
        }

        private static Test CloneRow(Test row) => new()
        {
            PersonID = row.PersonID, TestName = row.TestName, Day0 = row.Day0,
            Day1 = row.Day1, Day2 = row.Day2, Day3 = row.Day3, Day4 = row.Day4,
            Day5 = row.Day5, Day6 = row.Day6, Day7 = row.Day7, Day8 = row.Day8,
            Day9_12 = row.Day9_12, Day12_16 = row.Day12_16
        };
    }
}
