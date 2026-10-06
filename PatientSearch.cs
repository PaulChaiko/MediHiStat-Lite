using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace MediHiStat
{
    /// <summary>Independent editable patient picker shared by all patient dialogs.</summary>
    public static class PatientSearch
    {
        private static readonly ConditionalWeakTable<ComboBox, SearchState> States = new();

        public static void Configure(ComboBox combo, IEnumerable<string> patientIds)
        {
            ArgumentNullException.ThrowIfNull(combo);
            ArgumentNullException.ThrowIfNull(patientIds);
            if (States.TryGetValue(combo, out SearchState? previous))
            {
                previous.Detach();
                States.Remove(combo);
            }

            combo.IsEditable = true;
            combo.IsTextSearchEnabled = false;
            combo.StaysOpenOnEdit = true;
            combo.MaxDropDownHeight = 320;
            // A private view prevents filtering one picker from filtering another.
            var ids = patientIds.Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.CurrentCultureIgnoreCase).ToList();
            var state = new SearchState(combo, new ListCollectionView(ids));
            combo.ItemsSource = state.View;
            States.Add(combo, state);
            state.Attach();
        }

        private sealed class SearchState
        {
            private readonly ComboBox _combo;
            private readonly TextChangedEventHandler _textChangedHandler;
            private bool _updating;
            private string _query = string.Empty;
            internal ListCollectionView View { get; }

            internal SearchState(ComboBox combo, ListCollectionView view)
            {
                _combo = combo;
                View = view;
                View.Filter = item => item is string id
                    && id.Contains(_query, StringComparison.CurrentCultureIgnoreCase);
                _textChangedHandler = OnTextChanged;
            }

            internal void Attach()
            {
                _combo.AddHandler(TextBoxBase.TextChangedEvent, _textChangedHandler);
                _combo.SelectionChanged += OnSelectionChanged;
                _combo.PreviewKeyDown += OnPreviewKeyDown;
            }

            internal void Detach()
            {
                _combo.RemoveHandler(TextBoxBase.TextChangedEvent, _textChangedHandler);
                _combo.SelectionChanged -= OnSelectionChanged;
                _combo.PreviewKeyDown -= OnPreviewKeyDown;
            }

            private void OnTextChanged(object sender, TextChangedEventArgs e)
            {
                if (_updating || e.OriginalSource is not TextBox editor
                    || !ReferenceEquals(editor, GetEditor())) return;
                if (_combo.SelectedItem is string selected && selected == editor.Text) return;

                string text = editor.Text;
                int selectionStart = editor.SelectionStart;
                int selectionLength = editor.SelectionLength;
                _updating = true;
                try
                {
                    _query = text;
                    _combo.SelectedIndex = -1;
                    View.Refresh();
                    // Refresh can replace the editable text when selection disappears.
                    // Restore both input and caret in the same guarded event turn.
                    _combo.Text = text;
                    editor.Text = text;
                    editor.Select(Math.Min(selectionStart, text.Length),
                        Math.Min(selectionLength, Math.Max(0, text.Length - selectionStart)));
                    if (editor.IsKeyboardFocusWithin) _combo.IsDropDownOpen = true;
                }
                finally { _updating = false; }
            }

            private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                if (_updating || _combo.SelectedItem is not string selected) return;
                _updating = true;
                try
                {
                    _query = string.Empty;
                    View.Refresh();
                    _combo.Text = selected;
                    if (GetEditor() is TextBox editor)
                    {
                        editor.Text = selected;
                        editor.CaretIndex = selected.Length;
                    }
                }
                finally { _updating = false; }
            }

            private void OnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key != Key.Enter) return;
                string query = _combo.Text;
                string? exact = View.Cast<string>().FirstOrDefault(id =>
                    string.Equals(id, query, StringComparison.CurrentCultureIgnoreCase));
                string? match = exact ?? (View.Count == 1 ? View.GetItemAt(0) as string : null);
                if (match is null) return;
                _combo.SelectedItem = match;
                _combo.IsDropDownOpen = false;
                e.Handled = true;
            }

            private TextBox? GetEditor() =>
                _combo.Template?.FindName("PART_EditableTextBox", _combo) as TextBox;
        }
    }
}
