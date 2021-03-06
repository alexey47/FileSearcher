using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FileSearcher.ViewModels;
using Ookii.Dialogs.Wpf;

namespace FileSearcher.Views
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            DataContext = new MainViewModel();
            InitializeComponent();
        }

    }
}
