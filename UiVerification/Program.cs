using System.Collections;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MediHiStat;
using Microsoft.Data.Sqlite;

internal static class Program
{
    private const string Indicator = "Общий билирубин";

    [STAThread]
    private static int Main()
    {
        string previousDirectory = Environment.CurrentDirectory;
        string directory = Path.Combine(Path.GetTempPath(), "MediHiStat-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Environment.CurrentDirectory = directory;
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        MainWindow? main = null;
        try
        {
            CreateDatabase();
            var store = new PatientStore();
            store.Save(new[]
            {
                Record("Альфа 101", "A", "жен", "10"), Record("Альфа 102", "A", "муж", "12"),
                Record("Бета 201", "B", "жен", "14"), Record("Бета 202", "B", "муж", "16")
            }, false);
            main = new MainWindow();
            main.Show();
            Pump();
            VerifySearch(main);
            Console.WriteLine("PASS: patient typing, filtering, caret and independent dropdown views");
            VerifyGroupsAndTables(main);
            Console.WriteLine("PASS: group snapshots, labels, independent reset and read-only table snapshots");
            VerifyDatabaseReload(main, store);
            Console.WriteLine("PASS: overwrite/delete refresh patient data and invalidate stored analyses");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (main is not null)
            {
                foreach (Window window in main.OwnedWindows.Cast<Window>().ToArray()) window.Close();
                main.Close();
            }
            application.Shutdown();
            SqliteConnection.ClearAllPools();
            Environment.CurrentDirectory = previousDirectory;
            Directory.Delete(directory, true);
        }
    }

    private static void VerifySearch(MainWindow main)
    {
        var search = Named<ComboBox>(main, "TableOnePersonPatientSearch");
        Require(search.IsEditable && !search.IsTextSearchEnabled, "The main patient picker must accept filtered text.");
        search.ApplyTemplate();
        var editor = (TextBox?)search.Template.FindName("PART_EditableTextBox", search)
            ?? throw new InvalidOperationException("Editable patient picker has no text editor.");
        editor.Focus();
        Keyboard.Focus(editor);
        editor.Text = string.Empty;
        editor.Select(0, 0);
        TypeText(editor, "альФа");
        Require(editor.Text == "альФа", "Filtering changed typed Cyrillic text.");
        Require(editor.CaretIndex == 5, "Filtering moved the typing caret.");
        Require(search.Items.Cast<string>().SequenceEqual(new[] { "Альфа 101", "Альфа 102" }),
            "Case-insensitive patient filtering did not exclude nonmatching patients.");

        var other = new ComboBox { MinWidth = 200 };
        PatientSearch.Configure(other, main.Persons.Select(person => person.PersonID));
        Require(other.Items.Count == 4, "A new dropdown inherited another picker's filter.");
        editor.Select(2, 0);
        TypeText(editor, "Ф");
        Require(editor.Text == "алФьФа" && editor.CaretIndex == 3,
            "Inserting text inside a query lost input or caret position.");
        Require(search.Items.Count == 0 && other.Items.Count == 4,
            "No-match filtering leaked into a different patient picker.");

        editor.Text = "бЕТА 201";
        editor.CaretIndex = editor.Text.Length;
        search.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(search)!, 0, Key.Enter) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        Pump();
        Require(search.SelectedItem as string == "Бета 201", "Enter failed to select an exact case-insensitive patient match.");
        Require(main.personDataGrids[0].Info == "Бета 201", "Patient selection did not refresh the active card.");
        Require(search.Items.Count == 4, "Dropdown selection did not restore available patient choices.");
    }

    private static void VerifyGroupsAndTables(MainWindow main)
    {
        main.personDataGrids2[1].Info = "A";
        Click(main, "TableStarter");
        Named<ComboBox>(main, "Group1").SelectedItem = Indicator;
        Require(Field<List<string>>(main, "Group1PatientIds").SequenceEqual(new[] { "Альфа 101", "Альфа 102" }),
            "Group 1 captured the wrong patient identities.");
        Require(Named<TextBox>(main, "Group1name").Text.Contains("A; " + Indicator),
            "Group 1 caption omits its filters or selected laboratory indicator.");

        main.personDataGrids2[1].Info = "B";
        Click(main, "TableStarter");
        Named<ComboBox>(main, "Group2").SelectedItem = Indicator;
        Require(Field<List<string>>(main, "Group1PatientIds").SequenceEqual(new[] { "Альфа 101", "Альфа 102" }),
            "Preparing group 2 changed the saved group 1 cohort.");
        Require(Named<TextBox>(main, "Group1name").Text.Contains("A; " + Indicator)
            && Named<TextBox>(main, "Group2name").Text.Contains("B; " + Indicator),
            "A saved group caption inherited filters from the other group's preview.");

        var aggregateRows = Field<List<Test>>(main, "_currentGroupDisplayRows");
        Test aggregate = aggregateRows.Single(row => row.TestName == Indicator);
        Require(aggregate.Day0.Contains("n=2") && aggregate.Day0.Contains("Me [Q1; Q3]"),
            "The group preview lost the existing formatted descriptive statistics.");
        using (var groupWindow = new WindowScope(FullTableWindow.CreateGroup("B; пациентов: 2", aggregateRows)))
        {
            DataGrid grid = Grids(groupWindow.Window).Single();
            CheckReadOnly(grid);
            Test snapshot = grid.Items.Cast<Test>().Single(row => row.TestName == Indicator);
            Require(snapshot.Day0 == aggregate.Day0 && !ReferenceEquals(snapshot, aggregate),
                "Expanded group table must preserve formatted aggregates in an independent snapshot.");
            string original = aggregate.Day0;
            snapshot.Day0 = "changed snapshot";
            Require(aggregate.Day0 == original, "Expanded group table changed its source aggregate.");
        }
        using (var patientWindow = new WindowScope(FullTableWindow.CreatePatient(
            "Бета 201", main.personDataGrids, main.TestsOfOne)))
        {
            Require(patientWindow.Window.Title == "Полная таблица пациента — Бета 201",
                "Expanded patient table does not identify its patient.");
            DataGrid[] grids = Grids(patientWindow.Window).ToArray();
            Require(grids.Length == 2, "Expanded patient table must include passport data and measurements.");
            foreach (DataGrid grid in grids) CheckReadOnly(grid);
            DataGrid tests = grids.Single(grid => grid.Columns.Count == 12);
            Require(tests.Items.Count == PatientTableSchema.TestNames.Length && tests.FrozenColumnCount == 1,
                "Expanded patient table lost indicators or its fixed indicator column.");
            Test original = main.TestsOfOne.Single(row => row.TestName == Indicator);
            Test snapshot = tests.Items.Cast<Test>().Single(row => row.TestName == Indicator);
            Require(snapshot.Day0 == "14" && !ReferenceEquals(snapshot, original),
                "Expanded patient table did not preserve original measurements independently.");
            DataGrid passport = grids.Single(grid => grid.Columns.Count == 2);
            PersonDataGrid characteristic = passport.Items.Cast<PersonDataGrid>().First();
            characteristic.Info = "changed snapshot";
            Require(main.personDataGrids[0].Info == "Бета 201", "Expanded patient table changed the active patient's card.");
        }

        // Run the actual existing comparison path: it creates graph samples and a modeless results window.
        Click(main, "Process");
        Pump();
        Require(Field<object?>(main, "_lastGroup1Samples") is not null,
            "The numeric comparison did not produce graph samples.");
        Window[] results = main.OwnedWindows.Cast<Window>().OfType<StatisticalResultsWindow>().ToArray();
        Require(results.Length == 1 && results[0].IsVisible, "Comparison results did not open.");
        string[] secondIds = Field<List<string>>(main, "Group2PatientIds").ToArray();
        Test[] secondObservations = Field<List<Test>>(main, "Group2Observations").ToArray();
        string secondName = Named<TextBox>(main, "Group2name").Text;
        object? secondSelected = Named<ComboBox>(main, "Group2").SelectedItem;
        string[] secondFilters = Field<string[]>(main, "_group2Filters").ToArray();
        Invoke(main, "ResetGroup", 1);
        Require(Field<List<string>>(main, "Group1PatientIds").Count == 0
            && Field<List<Test>>(main, "Group1Observations").Count == 0
            && Field<TestNum>(main, "G1").TestName == string.Empty
            && Named<ComboBox>(main, "Group1").SelectedItem is null,
            "Resetting group 1 left its cohort or selected indicator behind.");
        Require(Field<List<string>>(main, "Group2PatientIds").SequenceEqual(secondIds)
            && Field<List<Test>>(main, "Group2Observations").SequenceEqual(secondObservations)
            && Named<TextBox>(main, "Group2name").Text == secondName
            && Equals(Named<ComboBox>(main, "Group2").SelectedItem, secondSelected)
            && Field<string[]>(main, "_group2Filters").SequenceEqual(secondFilters),
            "Resetting group 1 changed the independently saved group 2.");
        Require(main.personDataGrids2.All(row => row.Info == string.Empty)
            && Field<List<string>>(main, "CurrentGroupPatientIds").Count == 0
            && Named<DataGrid>(main, "MainTestsOfMany").Items.Count == 0,
            "Group reset did not clear the shared input filters and preview.");
        Require(Field<object?>(main, "_lastGroup1Samples") is null
            && Field<object?>(main, "_lastGroup2Samples") is null
            && Field<IList>(main, "_graphElements").Count == 0
            && results.All(window => !window.IsVisible),
            "Group reset left obsolete graph samples or statistical results visible.");
    }

    private static void VerifyDatabaseReload(MainWindow main, PatientStore store)
    {
        Require(Field<List<string>>(main, "Group2PatientIds").Count == 2, "The saved group 2 disappeared before reload testing.");
        store.Save(new[] { Record("Бета 201", "C", "жен", "0") }, true);
        Invoke(main, "ReloadDatabaseAndSelections");
        Require(main.personDataGrids[0].Info == "Бета 201" && main.personDataGrids[1].Info == "C",
            "Overwriting a patient did not refresh the active patient's characteristics.");
        Require(main.TestsOfOne.Single(row => row.TestName == Indicator).Day0 == "0",
            "Overwriting a patient retained old observations or converted measured zero into a blank.");
        Require(Field<List<string>>(main, "Group2PatientIds").Count == 0
            && Field<List<Test>>(main, "Group2Observations").Count == 0
            && Field<TestNum>(main, "G2").TestName.Length == 0
            && Field<object?>(main, "_lastGroup2Samples") is null,
            "Reloading changed data left saved statistical cohorts active.");
        store.Delete("Бета 201");
        Invoke(main, "ReloadDatabaseAndSelections");
        Require(main.personDataGrids[0].Info != "Бета 201"
            && main.TestsOfOne.All(row => row.PersonID != "Бета 201")
            && !Named<ComboBox>(main, "TableOnePersonPatientSearch").Items.Cast<string>().Contains("Бета 201")
            && Named<TextBox>(main, "PatientCount").Text.EndsWith("3"),
            "Deleting the active patient left stale card, measurements, picker entry or patient count.");
    }

    private static void CheckReadOnly(DataGrid grid) => Require(grid.IsReadOnly
        && !grid.CanUserAddRows && !grid.CanUserDeleteRows && grid.Columns.All(column => column.IsReadOnly),
        "Expanded tables must be entirely read-only.");

    private static PatientRecord Record(string id, string group, string sex, string day0)
    {
        var values = PatientTableSchema.TestNames.ToDictionary(name => name,
            _ => Enumerable.Repeat(string.Empty, PatientTableSchema.TimeHeaders.Length).ToArray());
        values[Indicator][0] = day0;
        values[Indicator][1] = "8";
        return new PatientRecord(new[] { id, group, "40", sex, "180", "80", "", "", "", "", "" }, values);
    }

    private static void CreateDatabase()
    {
        using var connection = new SqliteConnection("Data Source=mydatabase.db");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Person (PersonID TEXT, PatientGroup TEXT, Age INTEGER, Sex TEXT, Height REAL,
                Weight REAL, Complaints TEXT, Duration TEXT, Diagnosis TEXT, AddDiagnosis TEXT, Operation TEXT);
            CREATE TABLE Test (PersonID TEXT, TestName TEXT, Day0 TEXT, Day1 TEXT, Day2 TEXT, Day3 TEXT,
                Day4 TEXT, Day5 TEXT, Day6 TEXT, Day7 TEXT, Day8 TEXT, Day9_12 TEXT, Day12_16 TEXT);
            """;
        command.ExecuteNonQuery();
    }

    private static void TypeText(TextBox editor, string text)
    {
        editor.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice,
            new TextComposition(InputManager.Current, editor, text))
            { RoutedEvent = TextCompositionManager.TextInputEvent });
        Pump();
    }

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static T Named<T>(Window window, string name) where T : class =>
        window.FindName(name) as T ?? throw new InvalidOperationException($"Missing named control: {name}");

    private static T Field<T>(object instance, string name)
    {
        FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field: {name}");
        return (T)field.GetValue(instance)!;
    }

    private static void Invoke(object instance, string name, params object[] arguments) =>
        (instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing method: {name}")).Invoke(instance, arguments);

    private static void Click(Window window, string name) =>
        Named<Button>(window, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static IEnumerable<DataGrid> Grids(Window window) => Grids((DependencyObject)window.Content);

    private static IEnumerable<DataGrid> Grids(DependencyObject node)
    {
        if (node is DataGrid grid) yield return grid;
        foreach (object child in LogicalTreeHelper.GetChildren(node))
        {
            if (child is not DependencyObject dependency) continue;
            foreach (DataGrid descendant in Grids(dependency)) yield return descendant;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class WindowScope : IDisposable
    {
        internal FullTableWindow Window { get; }
        internal WindowScope(FullTableWindow window) { Window = window; }
        public void Dispose() => Window.Close();
    }
}
