using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MediHiStat;

/// <summary>Проверка каждого листа перед атомарным добавлением пациентов.</summary>
public sealed class ImportPatientsWindow : Window
{
    private readonly ObservableCollection<ImportPreviewRow> rows = new();
    private readonly Button fileButton = PatientTransferUi.ActionButton("Выбрать XLSX или CSV…");
    private readonly Button pasteButton = PatientTransferUi.ActionButton("Проверить вставленную таблицу");
    private readonly Button importButton = PatientTransferUi.ActionButton("Добавить выбранных пациентов");
    private readonly Button cancelButton = PatientTransferUi.ActionButton("Закрыть");
    private readonly TextBox pasteText = new()
    {
        AcceptsReturn = true, AcceptsTab = true, TextWrapping = TextWrapping.NoWrap,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        Height = 112, Margin = new Thickness(0, 6, 0, 6), Padding = new Thickness(6)
    };
    private readonly CheckBox overwriteBox = new()
    {
        Content = "Перезаписать существующих пациентов полностью (характеристики и все показатели)",
        Margin = new Thickness(0, 8, 0, 8)
    };
    private readonly TextBlock sourceText = PatientTransferUi.Note("Файл ещё не выбран.");
    private readonly TextBlock summaryText = PatientTransferUi.Note("");
    private readonly TextBlock statusText = PatientTransferUi.Note("");
    private readonly DataGrid preview = new()
    {
        AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false,
        SelectionMode = DataGridSelectionMode.Single, SelectionUnit = DataGridSelectionUnit.FullRow,
        Margin = new Thickness(0, 8, 0, 8), MinHeight = 120,
        RowHeaderWidth = 0, IsReadOnly = false
    };
    private bool busy;

    public ImportPatientsWindow()
    {
        Title = "Добавить пациентов из таблицы";
        Width = 1080; Height = 790; MinWidth = 760; MinHeight = 610;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = PatientTransferUi.Root(5);
        var introduction = new StackPanel();
        introduction.Children.Add(PatientTransferUi.Heading("Добавление из таблицы"));
        introduction.Children.Add(PatientTransferUi.Note("Один лист Excel — один пациент. Проверяются все листы файла. CSV и вставка таблицы — один пациент."));
        root.Children.Add(introduction);

        var sourcePanel = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        sourcePanel.Children.Add(fileButton);
        sourcePanel.Children.Add(sourceText);
        var pastePanel = new StackPanel();
        pastePanel.Children.Add(PatientTransferUi.Note("Скопируйте всю заполненную схему из Excel или вставьте текст CSV вместе с заголовками."));
        pastePanel.Children.Add(pasteText);
        pastePanel.Children.Add(pasteButton);
        sourcePanel.Children.Add(new Expander { Header = "Вставить таблицу из буфера обмена", Content = pastePanel, Margin = new Thickness(0, 8, 0, 0) });
        PatientTransferUi.Add(root, sourcePanel, 1);

        var checkStyle = new Style(typeof(CheckBox));
        checkStyle.Setters.Add(new Setter(IsEnabledProperty, new Binding(nameof(ImportPreviewRow.CanInclude))));
        checkStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Center));
        preview.Columns.Add(new DataGridCheckBoxColumn
        {
            Header = "Добавить", Width = 80, ElementStyle = checkStyle, EditingElementStyle = checkStyle,
            Binding = new Binding(nameof(ImportPreviewRow.Include)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
        });
        preview.Columns.Add(PatientTransferUi.TextColumn("Лист / источник", nameof(ImportPreviewRow.SourceName), 170));
        preview.Columns.Add(PatientTransferUi.TextColumn("Пациент", nameof(ImportPreviewRow.PatientId), 130));
        preview.Columns.Add(PatientTransferUi.TextColumn("Состояние", nameof(ImportPreviewRow.Status), 150));
        preview.Columns.Add(PatientTransferUi.TextColumn("Пояснение", nameof(ImportPreviewRow.Details), new DataGridLength(1, DataGridLengthUnitType.Star)));
        preview.ItemsSource = rows;
        PatientTransferUi.Add(root, preview, 2);
        root.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);

        var summaryPanel = new StackPanel();
        summaryPanel.Children.Add(summaryText);
        summaryPanel.Children.Add(overwriteBox);
        summaryPanel.Children.Add(PatientTransferUi.Note("Пустые листы, ошибки и повторяющиеся имена внутри файла не добавляются. Перед записью будет показано точное количество выбранных записей."));
        summaryPanel.Children.Add(statusText);
        PatientTransferUi.Add(root, summaryPanel, 3);
        var footer = PatientTransferUi.Footer(importButton, cancelButton);
        PatientTransferUi.Add(root, footer, 4);
        Content = root;

        fileButton.Click += async (_, _) => await ChooseFileAsync();
        pasteButton.Click += async (_, _) => await ReadPastedAsync();
        importButton.Click += async (_, _) => await SaveAsync();
        cancelButton.Click += (_, _) => Close();
        overwriteBox.Checked += (_, _) => SetOverwrite();
        overwriteBox.Unchecked += (_, _) => SetOverwrite();
        Closing += (_, e) => { if (busy) e.Cancel = true; };
        UpdateSummary();
    }

    private async Task ChooseFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите заполненную схему пациента", CheckFileExists = true,
            Filter = "Таблицы пациентов (*.xlsx;*.csv)|*.xlsx;*.csv|Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv"
        };
        if (dialog.ShowDialog(this) != true) return;
        await LoadAsync(() => PatientTableFiles.Read(dialog.FileName), Path.GetFileName(dialog.FileName));
    }

    private async Task ReadPastedAsync()
    {
        string text = pasteText.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, "Вставьте заполненную таблицу вместе с заголовками.", "Добавление из таблицы", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await LoadAsync(() => PatientTableFiles.ReadText(text, "Вставленная таблица"), "Вставленная таблица");
    }

    private async Task LoadAsync(Func<IReadOnlyList<PatientImportEntry>> read, string source)
    {
        SetBusy(true, "Чтение и проверка схемы…");
        rows.Clear();
        sourceText.Text = source;
        try
        {
            var result = await Task.Run(() => (Entries: read(), Existing: new PatientStore().GetExistingIds()));
            var existing = new HashSet<string>(result.Existing.Select(id => id.Trim()), StringComparer.OrdinalIgnoreCase);
            var duplicateIds = result.Entries.Where(entry => !string.IsNullOrWhiteSpace(entry.PatientId) && !entry.IsEmpty)
                .GroupBy(entry => entry.PatientId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1).Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            overwriteBox.IsChecked = false;
            foreach (PatientImportEntry entry in result.Entries)
                rows.Add(new ImportPreviewRow(entry, existing, duplicateIds, UpdateSummary));
            statusText.Text = rows.Count == 0 ? "В таблице нет листов или записей для добавления." : "Проверка завершена. Выберите записи для добавления.";
        }
        catch (Exception exception)
        {
            statusText.Text = "Таблицу не удалось прочитать. Выберите другой файл или исправьте схему.";
            MessageBox.Show(this, $"Не удалось прочитать таблицу.\n\n{exception.Message}", "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { SetBusy(false); }
    }

    private void SetOverwrite()
    {
        bool overwrite = overwriteBox.IsChecked == true;
        foreach (ImportPreviewRow row in rows) row.SetOverwrite(overwrite);
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        int empty = rows.Count(row => row.IsEmpty);
        int duplicates = rows.Count(row => row.IsDuplicate);
        int errors = rows.Count(row => !row.IsEmpty && !row.IsDuplicate && !row.IsValid);
        int existing = rows.Count(row => row.IsValid && row.IsExisting);
        int fresh = rows.Count(row => row.IsValid && !row.IsExisting);
        int selected = rows.Count(row => row.CanInclude && row.Include);
        summaryText.Text = $"Источников: {rows.Count}; новых: {fresh}; уже в базе: {existing}; повторов внутри файла: {duplicates}; ошибок: {errors}; пустых: {empty}.\nВыбрано для записи: {selected}.";
        importButton.IsEnabled = !busy && selected > 0;
    }

    private async Task SaveAsync()
    {
        preview.CommitEdit(DataGridEditingUnit.Cell, true);
        preview.CommitEdit(DataGridEditingUnit.Row, true);
        List<ImportPreviewRow> selectedRows = rows.Where(row => row.CanInclude && row.Include).ToList();
        if (selectedRows.Count == 0) return;
        bool overwrite = overwriteBox.IsChecked == true;
        SetBusy(true, "Проверка совпадений перед записью…");
        try
        {
            var actualIds = await Task.Run(() => new PatientStore().GetExistingIds());
            var existing = new HashSet<string>(actualIds.Select(id => id.Trim()), StringComparer.OrdinalIgnoreCase);
            var patients = selectedRows.Select(row => row.Patient!)
                .Where(patient => overwrite || !existing.Contains(patient.PatientId)).ToList();
            int replaceCount = patients.Count(patient => existing.Contains(patient.PatientId));
            int addCount = patients.Count - replaceCount;
            int omitted = rows.Count - patients.Count;
            if (patients.Count == 0)
            {
                statusText.Text = "Выбранные пациенты уже существуют. Для замены включите перезапись.";
                MessageBox.Show(this, statusText.Text, "Добавление из таблицы", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string confirmation = $"Добавить новых пациентов: {addCount}.\nПерезаписать существующих: {replaceCount}.\nНе добавлять источники: {omitted}.\n\nПодтвердить запись выбранных пациентов?";
            if (MessageBox.Show(this, confirmation, "Подтверждение импорта", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                statusText.Text = "Запись отменена. Выбор пациентов сохранён.";
                return;
            }
            if (replaceCount > 0 && MessageBox.Show(this,
                $"Будут полностью заменены данные {replaceCount} существующих пациентов: их характеристики и все показатели.\n\nПодтвердить перезапись?",
                "Перезапись данных", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                statusText.Text = "Перезапись отменена. Данные не изменены.";
                return;
            }
            statusText.Text = "Сохранение пациентов…";
            var result = await Task.Run(() => new PatientStore().Save(patients, overwrite));
            MessageBox.Show(this, $"Добавлено: {result.Added}.\nПерезаписано: {result.Replaced}.\nСовпадений пропущено: {result.Skipped}.", "Импорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
            if (result.Added + result.Replaced > 0)
            {
                busy = false;
                DialogResult = true;
            }
            else statusText.Text = "Запись не изменилась: выбранные пациенты уже существуют. Для замены включите перезапись.";
        }
        catch (Exception exception)
        {
            statusText.Text = "Добавление не выполнено. Данные выбранных пациентов не записаны.";
            MessageBox.Show(this, $"Не удалось сохранить пациентов.\n\n{exception.Message}", "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool value, string? status = null)
    {
        busy = value;
        fileButton.IsEnabled = !value;
        pasteButton.IsEnabled = !value;
        pasteText.IsEnabled = !value;
        preview.IsEnabled = !value;
        overwriteBox.IsEnabled = !value;
        cancelButton.IsEnabled = !value;
        if (status != null) statusText.Text = status;
        UpdateSummary();
    }

    private sealed class ImportPreviewRow : INotifyPropertyChanged
    {
        private readonly Action changed;
        private bool include;
        private bool overwrite;
        public string SourceName { get; }
        public PatientRecord? Patient { get; }
        public string PatientId { get; }
        public bool IsEmpty { get; }
        public bool IsDuplicate { get; }
        public bool IsExisting { get; }
        public bool IsValid { get; }
        public string Status { get; }
        public string Details { get; }
        public bool CanInclude => IsValid && (!IsExisting || overwrite);
        public bool Include
        {
            get => include;
            set { if (include == value) return; include = value && CanInclude; OnPropertyChanged(); changed(); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        public ImportPreviewRow(PatientImportEntry entry, HashSet<string> existing, HashSet<string> duplicateIds, Action changed)
        {
            this.changed = changed;
            SourceName = entry.SourceName;
            Patient = entry.Patient;
            string id = !string.IsNullOrWhiteSpace(entry.PatientId) ? entry.PatientId.Trim() : Patient?.PatientId ?? string.Empty;
            PatientId = id.Length == 0 ? "—" : id;
            IsEmpty = entry.IsEmpty;
            IsDuplicate = entry.IsDuplicate || (id.Length > 0 && duplicateIds.Contains(id));
            IsExisting = id.Length > 0 && existing.Contains(id);
            IsValid = !IsEmpty && !IsDuplicate && Patient != null && string.IsNullOrWhiteSpace(entry.Error);
            Status = IsEmpty ? "Пустой лист" : IsDuplicate ? "Повтор в файле" : !IsValid ? "Ошибка" : IsExisting ? "Уже в базе" : "Новый";
            Details = IsEmpty ? "Нет заполненных данных пациента." : IsDuplicate ? entry.Error ?? "Имя пациента повторяется внутри файла. Исправьте повторы и проверьте файл снова." : !IsValid ? entry.Error ?? "Данные пациента не распознаны." : IsExisting ? "По умолчанию пропускается. Для замены включите перезапись." : "Схема распознана; пациент готов к добавлению.";
            include = CanInclude;
        }

        public void SetOverwrite(bool value)
        {
            overwrite = value;
            if (IsExisting) include = CanInclude;
            OnPropertyChanged(nameof(CanInclude));
            OnPropertyChanged(nameof(Include));
        }

        private void OnPropertyChanged([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}

/// <summary>Выбор одного или нескольких пациентов для выгрузки полной схемы.</summary>
public sealed class ExportPatientsWindow : Window
{
    private readonly Dictionary<string, PatientRecord> patients;
    private readonly ObservableCollection<string> selected = new();
    private readonly ComboBox search = new() { MinHeight = 30, Margin = new Thickness(0, 6, 8, 0) };
    private readonly ComboBox format = new() { MinHeight = 30, Margin = new Thickness(0, 6, 0, 0) };
    private readonly Button addButton = PatientTransferUi.ActionButton("Добавить в список");
    private readonly Button allButton = PatientTransferUi.ActionButton("Добавить всех");
    private readonly Button removeButton = PatientTransferUi.ActionButton("Убрать выбранных");
    private readonly Button exportButton = PatientTransferUi.ActionButton("Экспортировать…");
    private readonly Button cancelButton = PatientTransferUi.ActionButton("Закрыть");
    private readonly ListBox selection = new() { SelectionMode = SelectionMode.Extended, Margin = new Thickness(0, 8, 0, 8) };
    private readonly TextBlock countText = PatientTransferUi.Note("");
    private readonly TextBlock statusText = PatientTransferUi.Note("");
    private bool busy;

    public ExportPatientsWindow(IEnumerable<PatientRecord> patients)
    {
        ArgumentNullException.ThrowIfNull(patients);
        this.patients = new Dictionary<string, PatientRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (PatientRecord patient in patients)
        {
            ArgumentNullException.ThrowIfNull(patient);
            if (!this.patients.TryAdd(patient.PatientId, patient))
                throw new FormatException($"Пациент «{patient.PatientId}» встречается несколько раз. Исправьте повтор перед экспортом.");
        }
        Title = "Экспортировать пациентов";
        Width = 730; Height = 640; MinWidth = 600; MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = PatientTransferUi.Root(5);
        var header = new StackPanel();
        header.Children.Add(PatientTransferUi.Heading("Экспорт данных пациентов"));
        header.Children.Add(PatientTransferUi.Note("Выберите пациентов по имени. Для нескольких пациентов создаётся файл Excel с отдельным листом для каждого."));
        root.Children.Add(header);

        var choosePanel = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        choosePanel.Children.Add(PatientTransferUi.Note("Поиск пациента"));
        var searchRow = new DockPanel();
        DockPanel.SetDock(addButton, Dock.Right);
        searchRow.Children.Add(addButton);
        searchRow.Children.Add(search);
        choosePanel.Children.Add(searchRow);
        var chooseButtons = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        chooseButtons.Children.Add(allButton);
        chooseButtons.Children.Add(removeButton);
        choosePanel.Children.Add(chooseButtons);
        PatientTransferUi.Add(root, choosePanel, 1);

        selection.ItemsSource = selected;
        PatientTransferUi.Add(root, selection, 2);
        root.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
        var details = new StackPanel();
        details.Children.Add(countText);
        details.Children.Add(PatientTransferUi.Note("Формат файла"));
        details.Children.Add(format);
        details.Children.Add(statusText);
        PatientTransferUi.Add(root, details, 3);
        PatientTransferUi.Add(root, PatientTransferUi.Footer(exportButton, cancelButton), 4);
        Content = root;

        PatientSearch.Configure(search, this.patients.Keys.OrderBy(id => id, StringComparer.CurrentCultureIgnoreCase));
        addButton.Click += (_, _) => AddSelected();
        allButton.Click += (_, _) =>
        {
            foreach (string id in this.patients.Keys.OrderBy(id => id, StringComparer.CurrentCultureIgnoreCase))
                if (!selected.Contains(id, StringComparer.OrdinalIgnoreCase)) selected.Add(id);
            UpdateSelection();
        };
        removeButton.Click += (_, _) =>
        {
            foreach (string id in selection.SelectedItems.Cast<string>().ToList()) selected.Remove(id);
            UpdateSelection();
        };
        selection.SelectionChanged += (_, _) => removeButton.IsEnabled = !busy && selection.SelectedItems.Count > 0;
        exportButton.Click += async (_, _) => await ExportAsync();
        cancelButton.Click += (_, _) => Close();
        Closing += (_, e) => { if (busy) e.Cancel = true; };
        UpdateSelection();
    }

    private void AddSelected()
    {
        string id = (search.SelectedItem as string ?? search.Text).Trim();
        if (!patients.TryGetValue(id, out PatientRecord? patient))
        {
            MessageBox.Show(this, "Выберите пациента из списка найденных записей.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!selected.Contains(patient.PatientId, StringComparer.OrdinalIgnoreCase)) selected.Add(patient.PatientId);
        selection.SelectedItem = patient.PatientId;
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        string? oldFormat = format.SelectedItem as string;
        format.Items.Clear();
        format.Items.Add("Excel (*.xlsx)");
        if (selected.Count <= 1) format.Items.Add("CSV (*.csv)");
        format.SelectedItem = oldFormat != null && format.Items.Contains(oldFormat) ? oldFormat : "Excel (*.xlsx)";
        countText.Text = $"Выбрано пациентов: {selected.Count}.";
        exportButton.IsEnabled = !busy && selected.Count > 0;
        removeButton.IsEnabled = !busy && selection.SelectedItems.Count > 0;
        allButton.IsEnabled = !busy && patients.Count > 0;
        addButton.IsEnabled = !busy && patients.Count > 0;
    }

    private async Task ExportAsync()
    {
        if (selected.Count == 0) return;
        bool csv = selected.Count == 1 && format.SelectedItem as string == "CSV (*.csv)";
        string extension = csv ? ".csv" : ".xlsx";
        string fileName = (selected.Count == 1 ? $"MediHiStat_Пациент_{PatientTransferUi.SafeFileName(selected[0])}" : "MediHiStat_Пациенты") + " - " + UpdateInfo.Label;
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить данные пациентов", AddExtension = true, OverwritePrompt = true,
            DefaultExt = extension, FileName = fileName + extension,
            Filter = csv ? "CSV (*.csv)|*.csv" : "Excel (*.xlsx)|*.xlsx"
        };
        if (dialog.ShowDialog(this) != true) return;
        if (!PatientTransferUi.CheckExtension(this, dialog.FileName, extension)) return;
        var records = selected.Select(id => patients[id]).ToList();
        SetBusy(true, "Создание файла…");
        try
        {
            await Task.Run(() => PatientTableFiles.Write(dialog.FileName, records));
            statusText.Text = $"Сохранено пациентов: {records.Count}.";
            MessageBox.Show(this, $"Сохранено пациентов: {records.Count}.\n\n{dialog.FileName}", "Экспорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            statusText.Text = "Не удалось создать файл.";
            MessageBox.Show(this, $"Не удалось экспортировать данные.\n\n{exception.Message}", "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool value, string? status = null)
    {
        busy = value;
        search.IsEnabled = !value;
        selection.IsEnabled = !value;
        format.IsEnabled = !value;
        cancelButton.IsEnabled = !value;
        if (status != null) statusText.Text = status;
        UpdateSelection();
    }
}

/// <summary>Скачивание чистой схемы с заданным числом листов пациентов.</summary>
public sealed class TemplateDownloadWindow : Window
{
    private readonly ComboBox format = new() { MinHeight = 30, Margin = new Thickness(0, 6, 0, 10) };
    private readonly TextBox sheetCount = new() { Text = "1", MinHeight = 30, Margin = new Thickness(0, 6, 0, 10), Padding = new Thickness(5) };
    private readonly Button saveButton = PatientTransferUi.ActionButton("Сохранить схему…");
    private readonly Button cancelButton = PatientTransferUi.ActionButton("Закрыть");
    private readonly TextBlock statusText = PatientTransferUi.Note("");
    private bool busy;

    public TemplateDownloadWindow()
    {
        Title = "Скачать схему таблицы";
        Width = 590; Height = 390; MinWidth = 490; MinHeight = 350;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = PatientTransferUi.Root(3);
        var header = new StackPanel();
        header.Children.Add(PatientTransferUi.Heading("Схема данных пациента"));
        header.Children.Add(PatientTransferUi.Note("Заполняйте характеристики и показатели в готовой схеме. Один лист Excel — один пациент."));
        root.Children.Add(header);
        var fields = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
        fields.Children.Add(PatientTransferUi.Note("Формат"));
        fields.Children.Add(format);
        fields.Children.Add(PatientTransferUi.Note("Число листов Excel (от 1 до 500)"));
        fields.Children.Add(sheetCount);
        fields.Children.Add(statusText);
        PatientTransferUi.Add(root, fields, 1);
        root.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
        PatientTransferUi.Add(root, PatientTransferUi.Footer(saveButton, cancelButton), 2);
        Content = root;
        format.Items.Add("Excel (*.xlsx)");
        format.Items.Add("CSV (*.csv)");
        format.SelectedIndex = 0;
        format.SelectionChanged += (_, _) => sheetCount.IsEnabled = !busy && format.SelectedIndex == 0;
        saveButton.Click += async (_, _) => await SaveTemplateAsync();
        cancelButton.Click += (_, _) => Close();
        Closing += (_, e) => { if (busy) e.Cancel = true; };
    }

    private async Task SaveTemplateAsync()
    {
        bool csv = format.SelectedIndex == 1;
        int count = 1;
        if (!csv && (!int.TryParse(sheetCount.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count) || count < 1 || count > 500))
        {
            MessageBox.Show(this, "Введите целое число листов от 1 до 500.", "Схема таблицы", MessageBoxButton.OK, MessageBoxImage.Warning);
            sheetCount.Focus();
            sheetCount.SelectAll();
            return;
        }
        string extension = csv ? ".csv" : ".xlsx";
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить чистую схему таблицы", AddExtension = true, OverwritePrompt = true,
            FileName = "MediHiStat_Шаблон_пациента - " + UpdateInfo.Label + extension, DefaultExt = extension,
            Filter = csv ? "CSV (*.csv)|*.csv" : "Excel (*.xlsx)|*.xlsx"
        };
        if (dialog.ShowDialog(this) != true) return;
        if (!PatientTransferUi.CheckExtension(this, dialog.FileName, extension)) return;
        SetBusy(true, "Создание схемы…");
        try
        {
            await Task.Run(() => PatientTableFiles.WriteTemplate(dialog.FileName, count));
            statusText.Text = csv ? "Схема CSV сохранена." : $"Схема Excel сохранена. Листов: {count}.";
            MessageBox.Show(this, $"{statusText.Text}\n\n{dialog.FileName}", "Схема сохранена", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            statusText.Text = "Не удалось создать файл схемы.";
            MessageBox.Show(this, $"Не удалось сохранить схему.\n\n{exception.Message}", "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool value, string? status = null)
    {
        busy = value;
        format.IsEnabled = !value;
        sheetCount.IsEnabled = !value && format.SelectedIndex == 0;
        saveButton.IsEnabled = !value;
        cancelButton.IsEnabled = !value;
        if (status != null) statusText.Text = status;
    }
}

internal static class PatientTransferUi
{
    public static Grid Root(int rows)
    {
        var grid = new Grid { Margin = new Thickness(16) };
        for (int i = 0; i < rows; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        return grid;
    }

    public static void Add(Grid root, UIElement element, int row)
    {
        Grid.SetRow(element, row);
        root.Children.Add(element);
    }

    public static TextBlock Heading(string text) => new() { Text = text, FontSize = 21, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) };
    public static TextBlock Note(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.DimGray, Margin = new Thickness(0, 4, 0, 0) };
    public static Button ActionButton(string text) => new() { Content = text, Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 0), HorizontalAlignment = HorizontalAlignment.Left, MinHeight = 31 };

    public static StackPanel Footer(params Button[] buttons)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        foreach (Button button in buttons) panel.Children.Add(button);
        return panel;
    }

    public static DataGridTextColumn TextColumn(string header, string property, DataGridLength width)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(5)));
        return new DataGridTextColumn { Header = header, Binding = new Binding(property), Width = width, IsReadOnly = true, ElementStyle = style };
    }

    public static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' }).ToHashSet();
        string name = new(value.Select(character => invalid.Contains(character) || char.IsControl(character) ? '_' : character).Take(100).ToArray());
        return string.IsNullOrWhiteSpace(name) ? "пациент" : name.Trim().TrimEnd('.');
    }

    public static bool CheckExtension(Window owner, string path, string extension)
    {
        if (string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase)) return true;
        MessageBox.Show(owner, $"Для выбранного формата укажите имя файла с расширением {extension}.", "Формат файла", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }
}
