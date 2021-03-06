using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileSearcher.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string this[string columnName]
        {
            get => string.Empty;
        }
        public string Error => null;
    }
}
