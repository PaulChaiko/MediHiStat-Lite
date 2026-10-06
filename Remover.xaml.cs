using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MediHiStat
{
    public partial class Remover : Window
    {
        private readonly PatientStore patientStore = new PatientStore();
        private IReadOnlyList<string> patientIds = Array.Empty<string>();

        public Remover()
        {
            InitializeComponent();
            try
            {
                patientIds = patientStore.GetExistingIds();
                PatientSearch.Configure(ToRemove, patientIds);
                RemoveButton.IsEnabled = patientIds.Count > 0;
                if (patientIds.Count == 0)
                    Description.Text = "В базе данных пока нет пациентов для удаления.";
            }
            catch (Exception exception)
            {
                RemoveButton.IsEnabled = false;
                Description.Text = $"Не удалось прочитать список пациентов.\n\n{exception.Message}";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string typedId = ToRemove.Text.Trim();
            string? patientId = patientIds.FirstOrDefault(id => string.Equals(id, typedId, StringComparison.OrdinalIgnoreCase));
            if (patientId == null)
            {
                MessageBox.Show(this, "Выберите пациента из списка или введите его полное название.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var consent = MessageBox.Show(this,
                $"Удалить пациента «{patientId}» и все его лабораторные показатели из базы данных?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (consent != MessageBoxResult.Yes) return;
            try
            {
                if (!patientStore.Delete(patientId))
                {
                    MessageBox.Show(this, "Пациент уже удалён. Обновите список пациентов.",
                        "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                    patientIds = patientStore.GetExistingIds();
                    PatientSearch.Configure(ToRemove, patientIds);
                    RemoveButton.IsEnabled = patientIds.Count > 0;
                    return;
                }
                DialogResult = true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, $"Не удалось удалить пациента.\n\n{exception.Message}",
                    "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
