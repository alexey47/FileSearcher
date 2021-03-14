using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;

// TODO: TextReader


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

            _fileSearcher = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
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
        private int _progress;
        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress == value)
                {
                    return;
                }
                _progress = value;
                OnPropertyChanged(nameof(Progress));
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
        public ICommand SearchFilesAsyncCommand
        {
            get
            {
                if (!_isFileSearcherAttached)
                {
                    _fileSearcher.DoWork += (sender, args) =>
                    {
                        //  Все файлы с указанными масками
                        var files = Array.Empty<string>();
                        var fileMask = FileMask.Split('\\', '/', ':', '"', '<', '>', '|').Where(mask => mask != string.Empty).ToArray();
                        foreach (var mask in fileMask)
                        {
                            if (_fileSearcher.CancellationPending)
                            {
                                args.Cancel = true;
                                break;
                            }
                            files = files.Concat(Directory.GetFiles(DirectoryPath, mask, (IsSearchAllDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))).ToArray();
                        }

                        //  Исключаемые файлы с указанными масками
                        var excludedFiles = Array.Empty<string>();
                        if (ExcludeFileMask != string.Empty)
                        {
                            var excludedFileMask = ExcludeFileMask.Split('\\', '/', ':', '"', '<', '>', '|').Where(mask => mask != string.Empty).ToArray();
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
                        var iteration = 0;
                        var size = files.Except(excludedFiles).Count();
                        foreach (var file in files.Except(excludedFiles))
                        {
                            var progress = 100 - (iteration * 100 / size);
                            
                            if (_fileSearcher.CancellationPending)
                            {
                                args.Cancel = true;
                                break;
                            }
                            var fileInfo = new FileInfo(file);
                            _fileSearcher.ReportProgress(progress, new FileDescription
                            {
                                Name = fileInfo.Name,
                                Path = file,
                                Size = fileInfo.Length,
                                CreatedTime = File.GetCreationTime(file)
                            });

                            if (progress % 10 == 0)
                            {
                                Thread.Sleep(1);
                            }
                            iteration++;
                        }
                    };
                    _fileSearcher.ProgressChanged += (sender, args) =>
                    {
                        DirectoryContentCollection.Insert(0, args.UserState as FileDescription);
                        Progress = args.ProgressPercentage;
                    };
                    _fileSearcher.RunWorkerCompleted += (sender, args) =>
                    {
                        if (args.Error != null)
                        {
                            DirectoryPath = args.Error.Message;
                        }
                        else if (args.Cancelled)
                        {
                            
                        }
                        else
                        {
                            
                        }

                        IsFileSearcherBusy = false;
                        Progress = 100;
                    };

                    _isFileSearcherAttached = true;
                }

                return new RelayCommand(() =>
                {
                    DirectoryContentCollection.Clear();
                    IsFileSearcherBusy = true;
                    _fileSearcher.RunWorkerAsync(this);
                });
            }
        }
        public ICommand SearchFilesAsyncCancelCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    _fileSearcher.CancelAsync();
                });
            }
        }

    }
}
