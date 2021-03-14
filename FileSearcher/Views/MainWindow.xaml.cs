using FileSearcher.ViewModels;

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
