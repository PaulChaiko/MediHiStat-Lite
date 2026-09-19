using Microsoft.Data.Sqlite;
using System;
using System.Windows;

namespace MediHiStat
{
    /// <summary>
    /// Логика взаимодействия для Remover.xaml
    /// </summary>
    public partial class Remover : Window
    {
        public Remover()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string personId = ToRemove.Text.Trim();

            if (string.IsNullOrWhiteSpace(personId))
            {
                MessageBox.Show("Укажите идентификатор пациента.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var connection = new SqliteConnection("Data Source=mydatabase.db");
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var deleteTestsCommand = new SqliteCommand(
                    "DELETE FROM Test WHERE PersonID = @PersonID",
                    connection,
                    transaction);
                deleteTestsCommand.Parameters.AddWithValue("@PersonID", personId);
                int deletedTests = deleteTestsCommand.ExecuteNonQuery();

                using var deletePersonCommand = new SqliteCommand(
                    "DELETE FROM Person WHERE PersonID = @PersonID",
                    connection,
                    transaction);
                deletePersonCommand.Parameters.AddWithValue("@PersonID", personId);
                int deletedPersons = deletePersonCommand.ExecuteNonQuery();

                transaction.Commit();

                if (deletedPersons == 0 && deletedTests == 0)
                {
                    MessageBox.Show("Пациент с указанным идентификатором не найден.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                MessageBox.Show($"Пациент {personId} удалён.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                MessageBox.Show($"Не удалось удалить пациента.\n\n{exception.Message}", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
