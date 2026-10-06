using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MediHiStat
{
    public partial class AdvancedAnalysisWindow : Window
    {
        private enum AnalysisMode { Levels, Changes, Wilcoxon, Normality, Categorical }
        private enum Correction { None, Holm, Bonferroni }
        private enum CategorySource { Measurements, Patients }
        private enum NormalityMethod { ShapiroWilk, Lilliefors }
        private sealed record Choice<T>(T Value, string Label);
        private sealed record AnalysisRequest(
            AnalysisMode Mode, string Indicator, int[] Endpoints, int Reference,
            Correction Correction, int SingleGroup, NormalityMethod NormalityMethod,
            bool NormalityChanges, CategorySource CategorySource, string PatientField);
        private sealed record ResultDraft(StatisticalResultRow Row, double? PValue, bool ConservativePValue = false);
        private sealed record AnalysisOutput(StatisticalResultRow[] Rows, int SuccessfulCount,
            string Description, string FirstHeader, string SecondHeader);

        private static readonly string[] TimeNames =
        {
            "0 сутки", "1 сутки", "2 сутки", "3 сутки", "4 сутки", "5 сутки",
            "6 сутки", "7 сутки", "8 сутки", "9–12 сутки", "12–16 сутки"
        };
        private readonly MeasurementRecord[] _measurements;
        private readonly AnalysisPatientRecord[] _patients;
        private readonly string[][] _groups;
        private readonly string[] _groupNames;
        private readonly Dictionary<string, NormalityResult> _normalityCache = new();
        private bool _ready;
        private bool _running;

        internal AdvancedAnalysisWindow(
            IEnumerable<MeasurementRecord> measurements,
            IEnumerable<AnalysisPatientRecord> patients,
            IEnumerable<string> group1Ids,
            IEnumerable<string> group2Ids,
            string group1Name,
            string group2Name,
            string? selectedIndicator = null)
        {
            _measurements = measurements.Select(record => new MeasurementRecord(
                record.PatientId, record.Indicator, record.Values.ToArray())).ToArray();
            _patients = patients.ToArray();
            _groups = new[] { group1Ids.ToArray(), group2Ids.ToArray() };
            _groupNames = new[]
            {
                string.IsNullOrWhiteSpace(group1Name) ? "Группа 1" : group1Name,
                string.IsNullOrWhiteSpace(group2Name) ? "Группа 2" : group2Name
            };
            InitializeComponent();
            GroupsText.Text = $"Группа 1: {_groupNames[0]} (n={_groups[0].Length}); " +
                $"группа 2: {_groupNames[1]} (n={_groups[1].Length}). Группы выбраны в главном окне.";
            SetChoices(ModeBox, new[]
            {
                new Choice<AnalysisMode>(AnalysisMode.Levels, "Сравнение групп по уровню показателя (Манн—Уитни)"),
                new Choice<AnalysisMode>(AnalysisMode.Changes, "Сравнение групп по изменению Δ (Манн—Уитни)"),
                new Choice<AnalysisMode>(AnalysisMode.Wilcoxon, "Сравнение двух сроков внутри группы (Уилкоксон)"),
                new Choice<AnalysisMode>(AnalysisMode.Normality, "Проверка нормальности"),
                new Choice<AnalysisMode>(AnalysisMode.Categorical, "Сравнение категориальных признаков (χ² / Фишер)")
            });
            SetChoices(SingleGroupBox, new[]
            {
                new Choice<int>(0, $"Группа 1: {_groupNames[0]} (n={_groups[0].Length})"),
                new Choice<int>(1, $"Группа 2: {_groupNames[1]} (n={_groups[1].Length})")
            });
            SingleGroupBox.SelectedIndex = _groups[0].Length == 0 && _groups[1].Length > 0 ? 1 : 0;
            SetChoices(NormalityMethodBox, new[]
            {
                new Choice<NormalityMethod>(NormalityMethod.ShapiroWilk, "Шапиро—Уилка"),
                new Choice<NormalityMethod>(NormalityMethod.Lilliefors, "Колмогорова—Смирнова с поправкой Лиллиефорса")
            });
            SetChoices(NormalityValuesBox, new[]
            {
                new Choice<bool>(false, "Уровень показателя"),
                new Choice<bool>(true, "Индивидуальное изменение Δ")
            });
            SetChoices(CategorySourceBox, new[]
            {
                new Choice<CategorySource>(CategorySource.Measurements, "Показатель по срокам"),
                new Choice<CategorySource>(CategorySource.Patients, "Характеристика пациента")
            });
            SetChoices(PatientFieldBox, new[]
            {
                new Choice<string>("Sex", "Пол"),
                new Choice<string>("Diagnosis", "Диагноз"),
                new Choice<string>("Operation", "Операция")
            });
            SetChoices(CorrectionBox, new[]
            {
                new Choice<Correction>(Correction.Holm, "Холма"),
                new Choice<Correction>(Correction.Bonferroni, "Бонферрони"),
                new Choice<Correction>(Correction.None, "Без поправки")
            });
            SetChoices(ReferenceTimeBox, TimeNames.Select((name, index) => new Choice<int>(index, name)));
            string[] indicators = _measurements.Select(record => record.Indicator)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.CurrentCulture).ToArray();
            IndicatorBox.ItemsSource = indicators;
            IndicatorBox.SelectedItem = indicators.Contains(selectedIndicator, StringComparer.Ordinal)
                ? selectedIndicator : indicators.FirstOrDefault();
            for (int index = 0; index < TimeNames.Length; index++)
            {
                var checkBox = new CheckBox
                {
                    Content = TimeNames[index], Tag = index, IsChecked = index == 8,
                    Margin = new Thickness(0, 6, 18, 6), MinWidth = 105
                };
                checkBox.Checked += Endpoint_Changed;
                checkBox.Unchecked += Endpoint_Changed;
                EndpointsHost.Children.Add(checkBox);
            }
            _ready = true;
            RefreshSettings();
        }

        private static void SetChoices<T>(ComboBox box, IEnumerable<Choice<T>> choices)
        {
            box.DisplayMemberPath = nameof(Choice<T>.Label);
            box.ItemsSource = choices.ToArray();
            box.SelectedIndex = 0;
        }

        private static T Selected<T>(ComboBox box) => ((Choice<T>)box.SelectedItem).Value;
        private int[] SelectedEndpoints() => EndpointsHost.Children.OfType<CheckBox>()
            .Where(box => box.IsChecked == true).Select(box => (int)box.Tag).OrderBy(index => index).ToArray();
        private static bool UsesIndependentGroups(AnalysisMode mode) =>
            mode == AnalysisMode.Levels || mode == AnalysisMode.Changes || mode == AnalysisMode.Categorical;
        private static bool UsesPairs(AnalysisMode mode, bool normalityChanges) =>
            mode == AnalysisMode.Changes || mode == AnalysisMode.Wilcoxon ||
            (mode == AnalysisMode.Normality && normalityChanges);

        private void Settings_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_ready) RefreshSettings();
        }

        private void Endpoint_Changed(object sender, RoutedEventArgs e)
        {
            if (_ready) RefreshSettings();
        }

        private void RefreshSettings()
        {
            AnalysisMode mode = Selected<AnalysisMode>(ModeBox);
            bool normality = mode == AnalysisMode.Normality;
            bool categorical = mode == AnalysisMode.Categorical;
            bool personField = categorical && Selected<CategorySource>(CategorySourceBox) == CategorySource.Patients;
            bool paired = UsesPairs(mode, Selected<bool>(NormalityValuesBox));
            IndicatorPanel.Visibility = personField ? Visibility.Collapsed : Visibility.Visible;
            SingleGroupPanel.Visibility = UsesIndependentGroups(mode) ? Visibility.Collapsed : Visibility.Visible;
            NormalityPanel.Visibility = normality ? Visibility.Visible : Visibility.Collapsed;
            CategoryPanel.Visibility = categorical ? Visibility.Visible : Visibility.Collapsed;
            PatientFieldPanel.Visibility = personField ? Visibility.Visible : Visibility.Collapsed;
            CategoryPreviewPanel.Visibility = categorical ? Visibility.Visible : Visibility.Collapsed;
            ReferencePanel.Visibility = paired ? Visibility.Visible : Visibility.Collapsed;
            EndpointsPanel.Visibility = personField ? Visibility.Collapsed : Visibility.Visible;
            EndpointsLabel.Text = paired ? "Поздние сроки (каждый сравнивается с ранним сроком)" : "Сроки для анализа";
            CategoryHint.Text = personField
                ? "Каждый пациент учитывается один раз. Диагноз и операция используются как целиком записанные категории. " +
                    "Синонимы автоматически не объединяются; сопутствующие заболевания из свободного текста не выделяются. " +
                    "Проценты рассчитываются среди пациентов с известным значением; пропуски показаны отдельно."
                : "На каждом сроке один пациент учитывается один раз. Пустое значение означает пропуск. " +
                    "Проценты рассчитываются среди пациентов с известным значением; синонимы категорий не объединяются.";
            int[] endpoints = SelectedEndpoints();
            int reference = Selected<int>(ReferenceTimeBox);
            bool invalidTimes = paired && endpoints.Any(index => index <= reference);
            TimeValidationText.Text = invalidTimes
                ? "Все выбранные поздние сроки должны быть позже раннего. Измените выбор перед расчётом."
                : endpoints.Length == 0 && !personField ? "Выберите хотя бы один срок." : string.Empty;
            string composition = personField ? ((Choice<string>)PatientFieldBox.SelectedItem).Label
                : string.Join(", ", endpoints.Select(index => paired ? $"{TimeNames[reference]} → {TimeNames[index]}" : TimeNames[index]));
            int count = personField ? 1 : endpoints.Length;
            FamilyText.Text = $"Запланировано проверок: {count}. Состав семейства: " +
                (composition.Length == 0 ? "сроки не выбраны" : composition) +
                (normality ? ". Проверки нормальности образуют отдельное семейство." : ".");
            ModeHint.Text = mode switch
            {
                AnalysisMode.Changes => "Сравниваются индивидуальные изменения двух независимых групп. Отрицательное Δ означает снижение. Регрессионная поправка по исходному уровню (ANCOVA) здесь не выполняется.",
                AnalysisMode.Wilcoxon => "Уилкоксон оценивает изменение внутри выбранной группы. Этот результат сам по себе не устанавливает различие между режимами лечения.",
                AnalysisMode.Normality => "Метод проверки выбран пользователем и не меняется автоматически в зависимости от числа пациентов.",
                AnalysisMode.Categorical => "Для таблицы 2 × 2 с малой ожидаемой частотой автоматически выбирается двусторонний Фишер. Для большей таблицы выводится χ² с предупреждением при малых ожидаемых частотах.",
                _ => "На каждом выбранном сроке сравниваются индивидуальные значения двух независимых групп."
            };
            PreviewText.Visibility = Visibility.Collapsed;
            StatusText.Text = string.Empty;
        }

        private AnalysisRequest CaptureRequest()
        {
            AnalysisMode mode = Selected<AnalysisMode>(ModeBox);
            CategorySource source = Selected<CategorySource>(CategorySourceBox);
            bool personField = mode == AnalysisMode.Categorical && source == CategorySource.Patients;
            string indicator = personField ? ((Choice<string>)PatientFieldBox.SelectedItem).Label
                : IndicatorBox.SelectedItem as string ?? string.Empty;
            if (indicator.Length == 0) throw new ArgumentException("Выберите показатель для анализа.");
            int[] endpoints = personField ? new[] { -1 } : SelectedEndpoints();
            if (endpoints.Length == 0) throw new ArgumentException("Выберите хотя бы один срок для анализа.");
            bool normalityChanges = Selected<bool>(NormalityValuesBox);
            int reference = Selected<int>(ReferenceTimeBox);
            if (UsesPairs(mode, normalityChanges) && endpoints.Any(index => index <= reference))
                throw new ArgumentException("Все поздние сроки должны быть позже выбранного раннего срока.");
            int singleGroup = Selected<int>(SingleGroupBox);
            if (UsesIndependentGroups(mode))
            {
                if (_groups[0].Length == 0 || _groups[1].Length == 0)
                    throw new ArgumentException("В главном окне выберите пациентов для обеих сравниваемых групп.");
                AnalysisData.EnsureIndependentGroups(_groups[0], _groups[1]);
            }
            else if (_groups[singleGroup].Length == 0)
                throw new ArgumentException("Выбранная группа пуста. Выберите пациентов в главном окне или другую группу для анализа.");
            return new AnalysisRequest(mode, indicator, endpoints, reference, Selected<Correction>(CorrectionBox),
                singleGroup, Selected<NormalityMethod>(NormalityMethodBox), normalityChanges,
                source, Selected<string>(PatientFieldBox));
        }

        private async void RunAnalysis_Click(object sender, RoutedEventArgs e)
        {
            AnalysisRequest request;
            try { request = CaptureRequest(); }
            catch (ArgumentException exception) { ShowInputError(exception.Message); return; }
            SetRunning(true);
            StatusText.Text = "Выполняется расчёт…";
            try
            {
                // The request and patient/measurement arrays are snapshots; the worker never accesses WPF controls.
                AnalysisOutput output = await Task.Run(() => CalculateFamily(request));
                StatusText.Text = $"Запланировано: {request.Endpoints.Length}; рассчитано: {output.SuccessfulCount}; " +
                    $"недоступно: {request.Endpoints.Length - output.SuccessfulCount}.";
                var window = new StatisticalResultsWindow(request.Indicator, output.Description, output.Rows,
                    AdjustmentHeader(request.Correction), output.FirstHeader, output.SecondHeader) { Owner = this };
                window.Show();
            }
            catch (Exception exception)
            {
                StatusText.Text = "Расчёт не выполнен.";
                ShowInputError(exception.Message);
            }
            finally { SetRunning(false); }
        }

        private AnalysisOutput CalculateFamily(AnalysisRequest request)
        {
            ResultDraft[] drafts = request.Endpoints.Select(endpoint => CalculateEndpoint(request, endpoint)).ToArray();
            // A selected but uncomputable hypothesis still occupies its original family slot.
            double[] plannedPValues = drafts.Select(draft => draft.PValue ?? 1.0).ToArray();
            double[] adjusted = request.Correction switch
            {
                Correction.Holm => StatisticsCalculator.AdjustPValuesHolm(plannedPValues),
                Correction.Bonferroni => PairedStatisticsCalculator.AdjustPValuesBonferroni(plannedPValues),
                _ => plannedPValues
            };
            string family = FamilyDescription(request);
            StatisticalResultRow[] rows = drafts.Select((draft, index) => CompleteRow(draft, adjusted[index], family, request.Correction)).ToArray();
            int successful = drafts.Count(draft => draft.PValue.HasValue);
            string description = $"{ModeDescription(request)} {family} " +
                $"Запланировано: {drafts.Length}; рассчитано: {successful}; недоступно: {drafts.Length - successful}. " +
                (successful < drafts.Length ? "Недоступные проверки сохранены в размере семейства; их p не рассчитаны. " : string.Empty) +
                $"{CorrectionDescription(request.Correction)}. " +
                (UsesIndependentGroups(request.Mode)
                    ? $"Группа 1: {_groupNames[0]}; группа 2: {_groupNames[1]}."
                    : $"Группа: {_groupNames[request.SingleGroup]}.");
            (string first, string second) = request.Mode switch
            {
                AnalysisMode.Wilcoxon => ("Ранний срок", "Поздний срок"),
                AnalysisMode.Normality => ($"Выборка: {_groupNames[request.SingleGroup]}", "—"),
                _ => ($"Группа 1: {_groupNames[0]}", $"Группа 2: {_groupNames[1]}")
            };
            return new AnalysisOutput(rows, successful, description, first, second);
        }

        private ResultDraft CalculateEndpoint(AnalysisRequest request, int endpoint) => request.Mode switch
        {
            AnalysisMode.Levels => CompareLevels(request, endpoint),
            AnalysisMode.Changes => CompareChanges(request, endpoint),
            AnalysisMode.Wilcoxon => CompareWithinGroup(request, endpoint),
            AnalysisMode.Normality => CheckNormality(request, endpoint),
            AnalysisMode.Categorical => CompareCategories(request, endpoint),
            _ => throw new ArgumentException("Неизвестный режим анализа.")
        };

        private ResultDraft CompareLevels(AnalysisRequest request, int endpoint)
        {
            NumericSample first = AnalysisData.GetNumericSample(_measurements, _groups[0], request.Indicator, endpoint);
            NumericSample second = AnalysisData.GetNumericSample(_measurements, _groups[1], request.Indicator, endpoint);
            return MannWhitney(TimeNames[endpoint], FormatNumeric(first), FormatNumeric(second), first.Values, second.Values, "");
        }

        private ResultDraft CompareChanges(AnalysisRequest request, int endpoint)
        {
            PairedSample first = AnalysisData.GetPairedSample(_measurements, _groups[0], request.Indicator, request.Reference, endpoint);
            PairedSample second = AnalysisData.GetPairedSample(_measurements, _groups[1], request.Indicator, request.Reference, endpoint);
            return MannWhitney(PairName(request.Reference, endpoint), FormatChanges(first), FormatChanges(second),
                first.Differences, second.Differences, "Δ = позднее − раннее; отрицательное Δ означает снижение. ");
        }

        private static ResultDraft MannWhitney(string time, string firstSummary, string secondSummary,
            IEnumerable<double> firstValues, IEnumerable<double> secondValues, string notes)
        {
            try
            {
                MannWhitneyResult result = StatisticsCalculator.CalculateMannWhitney(firstValues, secondValues);
                double effect = 2 * result.U1 / (result.Group1Count * (double)result.Group2Count) - 1;
                return new ResultDraft(new StatisticalResultRow
                {
                    TimePoint = time, Group1Summary = firstSummary, Group2Summary = secondSummary,
                    Statistic = $"U={Number(result.U, "0.###")}", DegreesOfFreedom = "—",
                    EffectSize = $"rᵣᵦ={Number(effect, "0.###")}",
                    Notes = notes + $"Манн—Уитни; {(result.UsedExactPValue ? "точный" : "асимптотический")} двусторонний p. " +
                        "Положительный rᵣᵦ соответствует большей величине в группе 1."
                }, result.PValue);
            }
            catch (ArgumentException exception)
            {
                return Unavailable(time, firstSummary, secondSummary, notes + exception.Message);
            }
        }

        private ResultDraft CompareWithinGroup(AnalysisRequest request, int endpoint)
        {
            PairedSample sample = AnalysisData.GetPairedSample(_measurements, _groups[request.SingleGroup],
                request.Indicator, request.Reference, endpoint);
            string counts = $"Полных пар: {sample.Differences.Count}; без полной пары: {sample.MissingCount} " +
                $"из {sample.TotalPatients}; Δ: {Describe(sample.Differences)}. ";
            string earlier = $"{TimeNames[request.Reference]}; {Describe(sample.Earlier)}";
            string later = $"{TimeNames[endpoint]}; {Describe(sample.Later)}";
            try
            {
                WilcoxonResult result = PairedStatisticsCalculator.CalculateWilcoxon(sample.Differences);
                return new ResultDraft(new StatisticalResultRow
                {
                    TimePoint = PairName(request.Reference, endpoint), Group1Summary = earlier, Group2Summary = later,
                    Statistic = $"W={Number(result.Statistic, "0.###")}", DegreesOfFreedom = "—",
                    EffectSize = $"rᵣᵦ={Number(result.RankBiserialCorrelation, "0.###")}",
                    Notes = "Уилкоксон; двусторонний p; Δ = позднее − раннее. " + counts +
                        $"Нулевых разностей: {result.ZeroCount}; ненулевых: {result.NonZeroCount}; " +
                        $"W+={Number(result.PositiveRankSum, "0.###")}, W−={Number(result.NegativeRankSum, "0.###")}. " +
                        "Положительный rᵣᵦ соответствует увеличению. " + result.Notes
                }, result.PValue);
            }
            catch (ArgumentException exception)
            {
                return Unavailable(PairName(request.Reference, endpoint), earlier, later,
                    "Уилкоксон; Δ = позднее − раннее. " + counts + exception.Message);
            }
        }

        private ResultDraft CheckNormality(AnalysisRequest request, int endpoint)
        {
            IEnumerable<double> values;
            string summary;
            string time;
            if (request.NormalityChanges)
            {
                PairedSample sample = AnalysisData.GetPairedSample(_measurements, _groups[request.SingleGroup],
                    request.Indicator, request.Reference, endpoint);
                values = sample.Differences;
                summary = FormatChanges(sample);
                time = PairName(request.Reference, endpoint);
            }
            else
            {
                NumericSample sample = AnalysisData.GetNumericSample(_measurements, _groups[request.SingleGroup],
                    request.Indicator, endpoint);
                values = sample.Values;
                summary = FormatNumeric(sample);
                time = TimeNames[endpoint];
            }
            string method = request.NormalityMethod == NormalityMethod.ShapiroWilk
                ? "Шапиро—Уилка" : "Колмогорова—Смирнова с поправкой Лиллиефорса";
            string direction = request.NormalityChanges ? "Δ = позднее − раннее. " : string.Empty;
            try
            {
                NormalityResult result = CachedNormality(request.NormalityMethod, values);
                return new ResultDraft(new StatisticalResultRow
                {
                    TimePoint = time, Group1Summary = summary, Group2Summary = "—",
                    Statistic = $"{(request.NormalityMethod == NormalityMethod.ShapiroWilk ? "W" : "D")}={Number(result.Statistic, "0.####")}",
                    DegreesOfFreedom = "—", EffectSize = "—",
                    PValue = NormalityPValue(result),
                    Notes = method + "; " + direction + result.Notes
                }, result.PValue, result.PValueBound != NormalityPValueBound.None);
            }
            catch (ArgumentException exception)
            {
                return Unavailable(time, summary, "—", method + "; " + direction + exception.Message);
            }
        }

        private NormalityResult CachedNormality(NormalityMethod method, IEnumerable<double> values)
        {
            double[] sample = values.OrderBy(value => value).ToArray();
            string key = method + ":" + string.Join(",", sample.Select(value => BitConverter.DoubleToInt64Bits(value)));
            if (_normalityCache.TryGetValue(key, out NormalityResult? cached) && cached is not null) return cached;
            NormalityResult result = method == NormalityMethod.ShapiroWilk
                ? NormalityCalculator.ShapiroWilk(sample) : NormalityCalculator.Lilliefors(sample);
            if (_normalityCache.Count >= 100) _normalityCache.Clear();
            _normalityCache[key] = result;
            return result;
        }

        private (CategorySample First, CategorySample Second) GetCategories(AnalysisRequest request, int endpoint)
        {
            if (request.CategorySource == CategorySource.Patients)
                return (AnalysisData.GetPatientCategorySample(_patients, _groups[0], request.PatientField),
                    AnalysisData.GetPatientCategorySample(_patients, _groups[1], request.PatientField));
            return (AnalysisData.GetTestCategorySample(_measurements, _groups[0], request.Indicator, endpoint),
                AnalysisData.GetTestCategorySample(_measurements, _groups[1], request.Indicator, endpoint));
        }

        private ResultDraft CompareCategories(AnalysisRequest request, int endpoint)
        {
            (CategorySample first, CategorySample second) = GetCategories(request, endpoint);
            string[] categories = CombinedCategories(first, second);
            string firstSummary = FormatCategories(first, categories);
            string secondSummary = FormatCategories(second, categories);
            string time = request.CategorySource == CategorySource.Patients ? request.Indicator : TimeNames[endpoint];
            string coding = request.CategorySource == CategorySource.Patients
                ? "Категории характеристики Person используются целиком; сопутствующие заболевания из свободного текста не выделяются. "
                : "Категории показателя Test. ";
            try
            {
                CategoricalComparisonResult result = StatisticsCalculator.CalculateCategoricalComparison(first.Values, second.Values);
                string method = result.UsedFisherExact ? "Точный критерий Фишера; двусторонний p" : "χ² Пирсона";
                string warning = result.ExpectedCountsBelowFive > 0 && !result.UsedFisherExact
                    ? $"Внимание: ячеек с ожидаемой частотой < 5: {result.ExpectedCountsBelowFive}; приближение χ² может быть неточным. "
                    : string.Empty;
                return new ResultDraft(new StatisticalResultRow
                {
                    TimePoint = time, Group1Summary = firstSummary, Group2Summary = secondSummary,
                    Statistic = result.UsedFisherExact ? "— (Фишер)" : $"χ²={Number(result.ChiSquare, "0.###")}",
                    DegreesOfFreedom = result.UsedFisherExact ? "—" : result.DegreesOfFreedom.ToString(CultureInfo.CurrentCulture),
                    EffectSize = $"V={Number(result.CramersV, "0.###")}",
                    Notes = method + "; " + coding +
                        $"min ожидаемая частота={Number(result.MinimumExpectedCount)}. " + warning +
                        "Проценты от известных значений, пропуски отдельно; синонимы не объединены."
                }, result.ReportedPValue);
            }
            catch (ArgumentException exception)
            {
                return Unavailable(time, firstSummary, secondSummary, coding + exception.Message +
                    " Проценты от известных значений; пропуски отдельно.");
            }
        }

        private void PreviewCategories_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AnalysisRequest request = CaptureRequest();
                var builder = new StringBuilder();
                foreach (int endpoint in request.Endpoints)
                {
                    (CategorySample first, CategorySample second) = GetCategories(request, endpoint);
                    string[] categories = CombinedCategories(first, second);
                    builder.AppendLine(request.CategorySource == CategorySource.Patients ? request.Indicator : TimeNames[endpoint]);
                    builder.AppendLine($"Группа 1 ({_groupNames[0]}): {FormatCategories(first, categories)}");
                    builder.AppendLine($"Группа 2 ({_groupNames[1]}): {FormatCategories(second, categories)}");
                    builder.AppendLine();
                }
                PreviewText.Text = builder.ToString().TrimEnd();
                PreviewText.Visibility = Visibility.Visible;
            }
            catch (ArgumentException exception) { ShowInputError(exception.Message); }
        }

        private static string[] CombinedCategories(CategorySample first, CategorySample second) => first.Values
            .Concat(second.Values).Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase).ToArray();

        private static string FormatCategories(CategorySample sample, IEnumerable<string> categories)
        {
            int known = sample.Values.Count;
            string frequencies = string.Join("; ", categories.Select(category =>
            {
                int count = sample.Values.Count(value => string.Equals(value, category, StringComparison.CurrentCultureIgnoreCase));
                string percent = known == 0 ? "—" : Number(100.0 * count / known, "0.#");
                return $"{category}: {count} ({percent}%)";
            }));
            return $"Известно: {known} из {sample.TotalPatients}; пропусков: {sample.MissingCount}" +
                (frequencies.Length > 0 ? "; " + frequencies : string.Empty);
        }

        private static string FormatNumeric(NumericSample sample) =>
            $"{Describe(sample.Values)}; пропусков/нечисловых: {sample.MissingCount} из {sample.TotalPatients}";
        private static string FormatChanges(PairedSample sample) =>
            $"Полных пар: {sample.Differences.Count}; без полной пары: {sample.MissingCount} из {sample.TotalPatients}; " +
            $"Δ: {Describe(sample.Differences)}";
        private static string Describe(IEnumerable<double> values)
        {
            DescriptiveStatistics result = StatisticsCalculator.CalculateDescriptive(values);
            if (result.Count == 0) return "n=0";
            return $"n={result.Count}; среднее±SD: {Number(result.Mean)}±{Number(result.SampleStandardDeviation)}; " +
                $"Me [Q1; Q3]: {Number(result.Median)} [{Number(result.FirstQuartile)}; {Number(result.ThirdQuartile)}]";
        }

        private static ResultDraft Unavailable(string time, string first, string second, string notes) =>
            new(new StatisticalResultRow
            {
                TimePoint = time, Group1Summary = first, Group2Summary = second,
                Statistic = "—", DegreesOfFreedom = "—", EffectSize = "—", Notes = "Расчёт недоступен. " + notes
            }, null);
        private static StatisticalResultRow CompleteRow(ResultDraft draft, double adjusted, string family, Correction correction) => new()
        {
            TimePoint = draft.Row.TimePoint, Group1Summary = draft.Row.Group1Summary,
            Group2Summary = draft.Row.Group2Summary, Statistic = draft.Row.Statistic,
            DegreesOfFreedom = draft.Row.DegreesOfFreedom,
            PValue = draft.PValue.HasValue ? RawPValue(draft) : "—",
            AdjustedPValue = draft.PValue.HasValue
                ? correction == Correction.None ? RawPValue(draft)
                    : draft.ConservativePValue ? "≤ " + PValue(adjusted) : PValue(adjusted)
                : "—",
            EffectSize = draft.Row.EffectSize, Notes = draft.Row.Notes +
                (draft.ConservativePValue && correction != Correction.None
                    ? " Скорректированное p показано как консервативная верхняя граница." : string.Empty) + " " + family
        };
        private static string RawPValue(ResultDraft draft) => string.IsNullOrEmpty(draft.Row.PValue)
            ? PValue(draft.PValue!.Value) : draft.Row.PValue;
        private static string NormalityPValue(NormalityResult result) => result.PValueBound switch
        {
            NormalityPValueBound.LessThanOrEqual => $"≤ {Number(result.BoundValue, "0.000")}",
            NormalityPValueBound.GreaterThanOrEqual => $"≥ {Number(result.BoundValue, "0.000")}",
            _ => PValue(result.PValue)
        };
        private static string PairName(int earlier, int later) => $"{TimeNames[earlier]} → {TimeNames[later]}";
        private static string Number(double value, string format = "0.##") => double.IsFinite(value)
            ? value.ToString(format, CultureInfo.CurrentCulture) : "—";
        private static string PValue(double value) => value >= 0 && value < 0.0001
            ? "<0,0001" : Number(value, "0.####");
        private static string CorrectionDescription(Correction correction) => correction switch
        {
            Correction.Holm => "Поправка Холма", Correction.Bonferroni => "Поправка Бонферрони", _ => "Без поправки"
        };
        private static string AdjustmentHeader(Correction correction) => correction switch
        {
            Correction.Holm => "p (Холм)", Correction.Bonferroni => "p (Бонферрони)", _ => "p (без поправки)"
        };
        private static string FamilyDescription(AnalysisRequest request)
        {
            string composition = request.Mode == AnalysisMode.Categorical && request.CategorySource == CategorySource.Patients
                ? request.Indicator : string.Join(", ", request.Endpoints.Select(index => UsesPairs(request.Mode, request.NormalityChanges)
                    ? PairName(request.Reference, index) : TimeNames[index]));
            string indicator = request.Mode == AnalysisMode.Categorical && request.CategorySource == CategorySource.Patients
                ? "Person" : request.Indicator;
            return $"Семейство: m={request.Endpoints.Length}; {indicator}; {composition}; {CorrectionDescription(request.Correction)}.";
        }
        private static string ModeDescription(AnalysisRequest request) => request.Mode switch
        {
            AnalysisMode.Levels => "Сравнение уровней между группами, Манн—Уитни; двусторонний p.",
            AnalysisMode.Changes => "Сравнение индивидуальных Δ между группами, Манн—Уитни; двусторонний p. Δ = позднее − раннее.",
            AnalysisMode.Wilcoxon => "Сравнение сроков внутри группы, Уилкоксон; двусторонний p. Δ = позднее − раннее.",
            AnalysisMode.Normality => $"Проверка нормальности: {(request.NormalityMethod == NormalityMethod.ShapiroWilk ? "Шапиро—Уилка" : "Колмогорова—Смирнова с поправкой Лиллиефорса")}. " +
                (request.NormalityChanges ? "Индивидуальное Δ = позднее − раннее." : "Уровни показателя."),
            _ => "Сравнение независимых категориальных данных, χ² Пирсона / двусторонний Фишер."
        };

        private void SetRunning(bool running)
        {
            _running = running;
            RunButton.IsEnabled = !running;
            CloseButton.IsEnabled = !running;
            SettingsPanel.IsEnabled = !running;
        }
        private void ShowInputError(string message) => MessageBox.Show(this, message,
            "Статистический анализ", MessageBoxButton.OK, MessageBoxImage.Warning);
        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_running) e.Cancel = true;
        }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
