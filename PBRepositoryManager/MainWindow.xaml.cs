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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using PrettyBots.Strategies.Repository;

namespace PBRepositoryManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private PrimaryRepository repos;
        private PrimaryRepositoryDbAdapter adapter;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog() { Filter = "*.mdf|*.mdf" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var nr =
                        new PrimaryRepository(
                            string.Format(
                                "Data Source=(LocalDB)\\v11.0;AttachDbFilename=\"{0}\";Integrated Security=True;Connect Timeout=30",
                                ofd.FileName));
                    var na = new PrimaryRepositoryDbAdapter(nr);
                    if (!na.DatabaseExists)
                    {
                        MessageBox.Show("数据库不存在。");
                        return;
                    }
                    if (repos != null) repos.Dispose();
                    repos = nr;
                    adapter = na;
                    ReposDataProvider.DataContext = na;
                }
                catch (Exception ex)
                {
                    Utility.ReportException(ex);
                }

            }
        }

        private void SubmitChangesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                adapter.SubmitChanges();
            }
            catch (Exception ex)
            {
                Utility.ReportException(ex);
            }
        }
    }
}
