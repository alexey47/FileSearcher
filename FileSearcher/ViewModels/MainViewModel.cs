using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Microsoft.VisualBasic;
using Ookii.Dialogs.Wpf;

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
            SearchProgress = 0;

            _textReplacer = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            IsTextReplacerBusy = false;
            _isTextReplacerAttached = false;
            FindText = string.Empty;
            ReplaceText = string.Empty;
            ReplaceProgress = 0;
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

        #region Searcher
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
        private int _searchProgress;
        public int SearchProgress
        {
            get => _searchProgress;
            set
            {
                if (_searchProgress == value)
                {
                    return;
                }
                _searchProgress = value;
                OnPropertyChanged(nameof(SearchProgress));
            }
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
                        SearchProgress = args.ProgressPercentage;
                    };
                    _fileSearcher.RunWorkerCompleted += (sender, args) =>
                    {
                        if (args.Error != null)
                        {
                            DirectoryPath = args.Error.Message;
                        }

                        SearchProgress = 100;
                        IsFileSearcherBusy = false;
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
        #endregion

        #region Replacer
        private BackgroundWorker _textReplacer;
        private bool _isTextReplacerAttached;
        private bool _isTextReplacerBusy;
        public bool IsTextReplacerBusy
        {
            get => _isTextReplacerBusy;
            set
            {
                if (_isTextReplacerBusy == value)
                {
                    return;
                }
                _isTextReplacerBusy = value;
                OnPropertyChanged(nameof(IsTextReplacerBusy));
            }
        }
        private int _replaceProgress;
        public int ReplaceProgress
        {
            get => _replaceProgress;
            set
            {
                if (_replaceProgress == value)
                {
                    return;
                }
                _replaceProgress = value;
                OnPropertyChanged(nameof(ReplaceProgress));
            }
        }
        public string FindText
        {
            get;
            set;
        }
        public string ReplaceText
        {
            get;
            set;
        }

        public ICommand ReplaceTextAsyncCommand
        {
            get
            {
                if (!_isTextReplacerAttached)
                {
                    _textReplacer.DoWork += (sender, args) =>
                    {
                        if (FindText == string.Empty || ReplaceText == string.Empty)
                        {
                            return;
                        }

                        List<FileDescription> files = new List<FileDescription>(DirectoryContentCollection);

                        int iteration = 0;
                        foreach (var fileDescription in files)
                        {
                            if (_textReplacer.CancellationPending)
                            {
                                args.Cancel = true;
                                break;
                            }

                            using StreamReader reader = new StreamReader(fileDescription.Path);
                            using StreamWriter writer = new StreamWriter(fileDescription.Path + ".tmp");

                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                writer.WriteLine(line.Replace(FindText, ReplaceText));
                            }

                            writer.Close();
                            reader.Close();

                            File.Delete(fileDescription.Path);
                            FileSystem.Rename(fileDescription.Path + ".tmp", fileDescription.Path);

                            var progress = 100 - (iteration * 100 / files.Count);
                            _textReplacer.ReportProgress(progress);
                            iteration++;
                        }
                    };
                    _textReplacer.ProgressChanged += (sender, args) =>
                    {
                        ReplaceProgress = args.ProgressPercentage;
                    };
                    _textReplacer.RunWorkerCompleted += (sender, args) =>
                    {
                        if (args.Error != null)
                        {
                            DirectoryPath = args.Error.Message;
                        }

                        ReplaceProgress = 100;
                        IsTextReplacerBusy = false;
                    };

                    _isTextReplacerAttached = true;
                }

                return new RelayCommand(() =>
                {
                    if (!IsFileSearcherBusy)
                    {
                        IsTextReplacerBusy = true;
                        _textReplacer.RunWorkerAsync(this);
                    }
                });
            }
        }
        public ICommand ReplaceTextAsyncCancelCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    _textReplacer.CancelAsync();
                });
            }
        }
        #endregion

    }
}
