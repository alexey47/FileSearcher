using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileSearcher
{
    public partial class Styles
    {
        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(Application.Current.MainWindow);
        }
        private void MaximizeButtonClick(object sender, RoutedEventArgs e)
        {
            Window window = Application.Current.MainWindow;
            if (window == null)
            {
                return;
            }
            if (window.WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(window);
            }
            else
            {
                SystemCommands.MaximizeWindow(window);
            }
        }
        private void MinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(Application.Current.MainWindow);
        }
    }
}
