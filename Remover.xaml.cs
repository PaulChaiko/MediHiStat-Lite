using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            if (ToRemove.Text != null)
            {
                using (var connection = new SqliteConnection("Data Source=mydatabase.db"))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {

                            var delete1Cmd = new SqliteCommand(
                                "DELETE FROM Person WHERE PersonID = @PersonID",
                                connection,
                                transaction);

                            delete1Cmd.Parameters.AddWithValue("@PersonID", ToRemove.Text.ToString());
                            delete1Cmd.ExecuteNonQuery();

                            var delete2Cmd = new SqliteCommand(
                               "DELETE FROM Test WHERE PersonId = @PersonID",
                               connection,
                               transaction);

                            delete2Cmd.Parameters.AddWithValue("@PersonID", ToRemove.Text.ToString());
                            delete2Cmd.ExecuteNonQuery();

                            transaction.Commit();
                            MessageBox.Show($"Удаление {ToRemove.Text} проведено успешно!");

                        }
                        catch (Exception ex)
                        {

                            transaction.Rollback();
                            Console.WriteLine($"Ошибка при удалении: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
        }
    }
}
