using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;

// TODO: BackgroundWorker, TextReader

namespace FileSearcher.ViewModels
{
    class FileDescription
    {
        public string Name
        {
            get; set;
        }
        public string Path
        {
            get; set;
        }
        public long Size
        {
            get; set;
        }
        public DateTime CreatedTime
        {
            get; set;
        }
    }

    class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            DirectoryContentCollection = new ObservableCollection<FileDescription>();
            DirectoryPath = string.Empty;
            FileMask = string.Empty;
            ExcludeFileMask = string.Empty;
            IsSearchAllDir = false;

            _fileSearcher = new BackgroundWorker();
            IsFileSearcherBusy = false;
            _isFileSearcherAttached = false;
        }

        public ObservableCollection<FileDescription> DirectoryContentCollection
        {
            set;
            get;
        }
        private string _directoryPath;
        public string DirectoryPath
        {
            get => _directoryPath;
            set
            {
                if (_directoryPath == value)
                {
                    return;
                }
                _directoryPath = value;
                OnPropertyChanged(nameof(DirectoryPath));
            }
        }
        public string FileMask
        {
            get;
            set;
        }
        public string ExcludeFileMask
        {
            get;
            set;
        }
        public bool IsSearchAllDir
        {
            get;
            set;
        }

        private BackgroundWorker _fileSearcher;
        private bool _isFileSearcherAttached;
        private bool _isFileSearcherBusy;
        public bool IsFileSearcherBusy
        {
            get => _isFileSearcherBusy;
            set
            {
                if (_isFileSearcherBusy == value)
                {
                    return;
                }
                _isFileSearcherBusy = value;
                OnPropertyChanged(nameof(IsFileSearcherBusy));
            }
        }

        public ICommand OpenFileDialogCommand
        {
            get => new RelayCommand(() =>
            {
                VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();
                if (dlg.ShowDialog() == true)
                {
                    DirectoryPath = dlg.SelectedPath;
                }
            });
        }
        public ICommand SearchFilesSyncCommand
        {
            get => new RelayCommand(() =>
            {
                try
                {
                    //  Все файлы с указанными масками
                    string[] files = Array.Empty<string>();
                    var fileMask = FileMask.Split('\\', '/', ':', '"', '<', '>', '|');
                    fileMask = fileMask.Where(mask => mask != string.Empty).ToArray();
                    foreach (var mask in fileMask)
                    {
                        files = files.Concat(Directory.GetFiles(DirectoryPath, mask, (IsSearchAllDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))).ToArray();
                    }

                    //  Исключить файлы
                    string[] excludedFiles = Array.Empty<string>();
                    if (ExcludeFileMask != string.Empty)
                    {
                        var excludedFileMask = ExcludeFileMask.Split('\\', '/', ':', '"', '<', '>', '|');
                        excludedFileMask = excludedFileMask.Where(mask => mask != string.Empty).ToArray();
                        foreach (var mask in excludedFileMask)
                        {
                            excludedFiles = excludedFiles.Concat(Directory.GetFiles(DirectoryPath, mask, (IsSearchAllDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))).ToArray();
                        }
                    }

                    //  Обновление коллекции
                    DirectoryContentCollection.Clear();
                    foreach (var file in files.Except(excludedFiles))
                    {
                        var fileInfo = new FileInfo(file);
                        DirectoryContentCollection.Add(new FileDescription()
                        {
                            Name = fileInfo.Name,
                            Path = file,
                            Size = fileInfo.Length,
                            CreatedTime = File.GetCreationTime(file)
                        });
                    }
                }
                catch (Exception e)
                {
                    DirectoryPath = e.Message;
                }
            });
        }
        public ICommand SearchFilesAsyncCommand
        {
            get
            {
                if (!_isFileSearcherAttached)
                {
                    _fileSearcher.DoWork += (sender, args) =>
                    {
                        IsFileSearcherBusy = true;

                        //  Все файлы с указанными масками
                        string[] files = Array.Empty<string>();
                        var fileMask = FileMask.Split('\\', '/', ':', '"', '<', '>', '|');
                        fileMask = fileMask.Where(mask => mask != string.Empty).ToArray();
                        foreach (var mask in fileMask)
                        {
                            if (_fileSearcher.CancellationPending)
                            {
                                args.Cancel = true;
                                break;
                            }
                            files = files.Concat(Directory.GetFiles(DirectoryPath, mask, (IsSearchAllDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))).ToArray();
                        }

                        //  Исключить файлы
                        string[] excludedFiles = Array.Empty<string>();
                        if (ExcludeFileMask != string.Empty)
                        {
                            var excludedFileMask = ExcludeFileMask.Split('\\', '/', ':', '"', '<', '>', '|');
                            excludedFileMask = excludedFileMask.Where(mask => mask != string.Empty).ToArray();
                            foreach (var mask in excludedFileMask)
                            {
                                if (_fileSearcher.CancellationPending)
                                {
                                    args.Cancel = true;
                                    break;
                                }
                                excludedFiles = excludedFiles.Concat(Directory.GetFiles(DirectoryPath, mask, (IsSearchAllDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))).ToArray();
                            }
                        }

                        //  Добавление в коллекцию
                        ObservableCollection<FileDescription> collection = new ObservableCollection<FileDescription>();
                        foreach (var file in files.Except(excludedFiles))
                        {
                            if (_fileSearcher.CancellationPending)
                            {
                                args.Cancel = true;
                                break;
                            }
                            var fileInfo = new FileInfo(file);
                            collection.Add(new FileDescription()
                            {
                                Name = fileInfo.Name,
                                Path = file,
                                Size = fileInfo.Length,
                                CreatedTime = File.GetCreationTime(file)
                            });
                        }

                        args.Result = collection;
                    };
                    _fileSearcher.RunWorkerCompleted += (sender, args) =>
                    {
                        if (args.Error != null)
                        {
                            DirectoryPath = args.Error.Message;
                        }
                        else if (args.Cancelled)
                        {
                            DirectoryPath = "Canceled!";
                        }
                        else
                        {
                            DirectoryContentCollection.Clear();
                            DirectoryContentCollection = args.Result as ObservableCollection<FileDescription>;
                            OnPropertyChanged(nameof(DirectoryContentCollection));
                        }

                        IsFileSearcherBusy = false;
                    };

                    _isFileSearcherAttached = true;
                }

                return new RelayCommand(() =>
                {
                    _fileSearcher.RunWorkerAsync(this);
                }, () => !IsFileSearcherBusy);
            }
        }

    }
}
