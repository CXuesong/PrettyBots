using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PBRepositoryManager
{
    internal static class Utility
    {
        public static void ReportException(Exception ex)
        {
            if (ex == null) return;
            MessageBox.Show(ex.ToString(), "Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
}
}
