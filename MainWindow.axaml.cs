﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace xdelta3_cross_gui
{
    public class MainWindow : Window, INotifyPropertyChanged
    {
        public static string VERSION = GetVersion();
        public static string TITLE = "xDelta3 Cross GUI " + VERSION;
        public static string XDELTA3_PATH = "";

        public const string XDELTA3_BINARY_WINDOWS = "xdelta3_x86_64_win.exe";
        public const string XDELTA3_BINARY_LINUX = "xdelta3_x64_linux";
        public const string XDELTA3_BINARY_MACOS = "xdelta3_mac";

        public string Credits { get { return "xDelta3 Cross-Platform GUI by dan0v, using xDelta 3.1.0\n\nHeavily inspired by xDelta GUI 2\nby Jordi Vermeulen (Modified by Brian Campbell)"; } }
        private bool _XDeltaOnSystemPath { get; set; }
        public bool XDeltaOnSystemPath { get => _XDeltaOnSystemPath;
            set
            {
                if (value != _XDeltaOnSystemPath)
                {
                    _XDeltaOnSystemPath = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _EqualFileCount { get; set; }
        public bool EqualFileCount
        {
            get => _EqualFileCount;
            set
            {
                if (value != _EqualFileCount)
                {
                    _EqualFileCount = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _PatchProgress { get; set; }
        public double PatchProgress
        {
            get => _PatchProgress;
            set
            {
                if (value != _PatchProgress)
                {
                    _PatchProgress = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _PatchProgressIsIndeterminate { get; set; }
        public bool PatchProgressIsIndeterminate
        {
            get => _PatchProgressIsIndeterminate;
            set
            {
                if (value != _PatchProgressIsIndeterminate)
                {
                    _PatchProgressIsIndeterminate = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _AlreadyBusy { get; set; }
        public bool AlreadyBusy
        {
            get => _AlreadyBusy;
            set
            {
                if (value != _AlreadyBusy)
                {
                    _AlreadyBusy = value;
                    OnPropertyChanged();
                }
            }
        }
        public string XDeltaOnSystemPathMessage1 { get { return this.XDeltaOnSystemPath ? "has" : "has not"; } } //has | has not
        public string XDeltaOnSystemPathMessage2 { get { return this.XDeltaOnSystemPath ? "" : ", so the locally bundled xDelta3 binary will be used"; } } // | , so locally bundled xDelta binary will be used
        public List<PathFileComponent> OldFilesList { get; set; }
        public List<PathFileComponent> NewFilesList { get; set; }
        private int _OldFilesListCount { get; set; }
        public int OldFilesListCount
        {
            get => _OldFilesListCount;
            set
            {
                if (value != _OldFilesListCount)
                {
                    _OldFilesListCount = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _NewFilesListCount { get; set; }
        public int NewFilesListCount
        {
            get => _NewFilesListCount;
            set
            {
                if (value != _NewFilesListCount)
                {
                    _NewFilesListCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowTerminal { get => Options.ShowTerminal;
            set
            { 
                Options.ShowTerminal = value;
                try
                {
                    if (value)
                    {
                        this.Console.Show();
                    }
                    else
                    {
                        this.Console.Hide();
                    }
                }
                catch (Exception e) { Debug.WriteLine(e);
                    this._Console = new Console();
                }
            }
        }

        private bool _AllOldFilesSelected = false;
        private bool _AllNewFilesSelected = false;

        private Console _Console = new Console();
        public Console Console { get => _Console; }

        private Options _Options = new Options();
        public Options Options { get { return this._Options; } }

        Button btn_ToggleAllOldFilesSelection;
        Button btn_ToggleAllNewFilesSelection;
        Button btn_AddOld;
        Button btn_UpOld;
        Button btn_DownOld;
        Button btn_DeleteOld;
        Button btn_AddNew;
        Button btn_UpNew;
        Button btn_DownNew;
        Button btn_DeleteNew;
        Button btn_BrowsePathDestination;
        Button btn_ResetDefaults;
        Button btn_SaveSettings;
        Button btn_Go;
        StackPanel sp_OldFilesDisplay;
        StackPanel sp_NewFilesDisplay;
        ScrollViewer sv_OldFilesDisplay;
        ScrollViewer sv_NewFilesDisplay;
        CheckBox chk_UseShortNames;
        public ProgressBar pb_Progress;
        TextBox txt_bx_PatchDestination;

        public enum FileVersion { New, Old };

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.Configure();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Configure()
        {
            this.Title = TITLE;

            this.EqualFileCount = false;
            this.XDeltaOnSystemPath = false;
            this.AlreadyBusy = false;
            this.PatchProgressIsIndeterminate = false;
            this.OldFilesListCount = 0;
            this.NewFilesListCount = 0;

            this.Options.LoadSaved();
            this.CheckXDeltaIsOnSystemPath();

            this.OldFilesList = new List<PathFileComponent>();
            this.NewFilesList = new List<PathFileComponent>();

            // Button Click event does not compile in XAML, so has to be manually added https://github.com/AvaloniaUI/Avalonia/issues/3898
            this.btn_ToggleAllOldFilesSelection = this.FindControl<Button>("btn_ToggleAllOldFilesSelection");
            this.btn_ToggleAllNewFilesSelection = this.FindControl<Button>("btn_ToggleAllNewFilesSelection");
            this.btn_AddOld = this.FindControl<Button>("btn_AddOld");
            this.btn_UpOld = this.FindControl<Button>("btn_UpOld");
            this.btn_DownOld = this.FindControl<Button>("btn_DownOld");
            this.btn_DeleteOld = this.FindControl<Button>("btn_DeleteOld");
            this.btn_AddNew = this.FindControl<Button>("btn_AddNew");
            this.btn_UpNew = this.FindControl<Button>("btn_UpNew");
            this.btn_DownNew = this.FindControl<Button>("btn_DownNew");
            this.btn_DeleteNew = this.FindControl<Button>("btn_DeleteNew");
            this.btn_BrowsePathDestination = this.FindControl<Button>("btn_BrowsePathDestination");
            this.sp_OldFilesDisplay = this.FindControl<StackPanel>("sp_OldFilesDisplay");
            this.sp_NewFilesDisplay = this.FindControl<StackPanel>("sp_NewFilesDisplay");
            this.sv_OldFilesDisplay = this.FindControl<ScrollViewer>("sv_OldFilesDisplay");
            this.sv_NewFilesDisplay = this.FindControl<ScrollViewer>("sv_NewFilesDisplay");
            this.chk_UseShortNames = this.FindControl<CheckBox>("chk_UseShortNames");
            this.txt_bx_PatchDestination = this.FindControl<TextBox>("txt_bx_PatchDestination");
            this.btn_SaveSettings = this.FindControl<Button>("btn_SaveSettings");
            this.btn_ResetDefaults = this.FindControl<Button>("btn_ResetDefaults");
            this.btn_Go = this.FindControl<Button>("btn_Go");
            this.pb_Progress = this.FindControl<ProgressBar>("pb_Progress");

            // Bindings
            this.btn_ToggleAllOldFilesSelection.Click += ToggleAllOldFilesSelection;
            this.btn_ToggleAllNewFilesSelection.Click += ToggleAllNewFilesSelection;
            this.btn_AddOld.Click += AddOldFile;
            this.btn_UpOld.Click += MoveOldFileUp;
            this.btn_DownOld.Click += MoveOldFileDown;
            this.btn_DeleteOld.Click += DeleteOldFiles;
            this.btn_AddNew.Click += AddNewFile;
            this.btn_UpNew.Click += MoveNewFileUp;
            this.btn_DownNew.Click += MoveNewFileDown;
            this.btn_DeleteNew.Click += DeleteNewFiles;
            this.btn_SaveSettings.Click += SaveSettingsClicked;
            this.btn_ResetDefaults.Click += ResetDefaultsClicked;
            this.btn_Go.Click += GoClicked;
            this.btn_BrowsePathDestination.Click += BrowseOutputDirectory;
            this.chk_UseShortNames.Click += UseShortNamesChecked;

            this.sv_OldFilesDisplay.AddHandler(DragDrop.DropEvent, OldFilesDropped);
            this.sv_NewFilesDisplay.AddHandler(DragDrop.DropEvent, NewFilesDropped);

        }

        private void CheckFileCounts()
        {
            if (this.OldFilesList.Count != this.NewFilesList.Count || this.OldFilesList.Count == 0)
            {
                this.EqualFileCount = false;
            } else
            {
                this.EqualFileCount = true;
            }
            this.OldFilesListCount = this.OldFilesList.Count;
            this.NewFilesListCount = this.NewFilesList.Count;
        }
        public void ReloadOldFiles(bool forceReloadContents = false)
        {
            this.sp_OldFilesDisplay.Children.Clear();

            if (forceReloadContents)
            {
                for (int i = 0; i < this.OldFilesList.Count; i++)
                {
                    this.OldFilesList[i].Index = i;
                    this.OldFilesList[i]._Shifted = false;
                    this.OldFilesList[i].UpdateValues();
                }
            }

            this.sp_OldFilesDisplay.Children.AddRange(this.OldFilesList);
        }
        public void ReloadNewFiles(bool forceReloadContents = false)
        {
            this.sp_NewFilesDisplay.Children.Clear();

            if (forceReloadContents)
            {
                for (int i = 0; i < this.NewFilesList.Count; i++)
                {
                    this.NewFilesList[i].Index = i;
                    this.NewFilesList[i]._Shifted = false;
                    this.NewFilesList[i].UpdateValues();
                }
            }

            this.sp_NewFilesDisplay.Children.AddRange(this.NewFilesList);
        }

        public void OldFilesDropped(object sender, DragEventArgs args)
        {
            if (args.Data.Contains(DataFormats.FileNames))
            {
                List<string> url = new List<string>(args.Data.GetFileNames());
                if (url.Count > 0)
                {
                    foreach (string path in url)
                    {
                        this.OldFilesList.Add(new PathFileComponent(this, path, this.OldFilesList.Count, FileVersion.Old));
                    }
                    this.ReloadOldFiles();
                    this.CheckFileCounts();
                }
            } 
        }
        public async void AddOldFile(object sender, RoutedEventArgs args)
        {
            try
            {
                string[] url = await this.OpenFileBrowser();
                if (url.Length > 0)
                {
                    foreach (string path in url)
                    {
                        this.OldFilesList.Add(new PathFileComponent(this, path, this.OldFilesList.Count, FileVersion.Old));
                    }
                    this.ReloadOldFiles();
                    this.CheckFileCounts();
                }
            } catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        public void MoveOldFileUp(object sender, RoutedEventArgs args)
        {
            List<PathFileComponent> list = this.OldFilesList;
            List<PathFileComponent> selectedList = list.FindAll(c => c.IsSelected == true);
            this.SortListInPlaceByIndex(selectedList);
            for (int i = 0; i < selectedList.Count; i++)
            {
                PathFileComponent component = selectedList[i];
                // Do not shift up if element before it was not shifted
                if (component.Index != 0 && (i == 0 || (i > 0 && (list.IndexOf(component) - list.IndexOf(selectedList[i - 1]) > 1) || selectedList[i - 1]._Shifted == true)))
                {
                    list[list.IndexOf(component) - 1].Index++;
                    component.Index--;
                    component._Shifted = true;
                    this.SortListInPlaceByIndex(list);
                }
            }
            this.ReloadOldFiles(true);
        }
        public void MoveOldFileDown(object sender, RoutedEventArgs args)
        {
            List<PathFileComponent> list = this.OldFilesList;
            List<PathFileComponent> selectedList = list.FindAll(c => c.IsSelected == true);
            this.SortListInPlaceByIndex(selectedList);
            for (int i = selectedList.Count - 1; i >= 0; i--)
            {
                PathFileComponent component = selectedList[i];
                // Do not shift down if element after it was not shifted
                if (component.Index != list.Count - 1 && (i == selectedList.Count - 1 || (i < selectedList.Count - 1 && (list.IndexOf(selectedList[i + 1]) - list.IndexOf(component)) > 1) || selectedList[i + 1]._Shifted == true))
                {
                    list[list.IndexOf(component) + 1].Index--;
                    component.Index++;
                    component._Shifted = true;
                    this.SortListInPlaceByIndex(list);
                }
            }
            this.ReloadOldFiles(true);
        }
        public void DeleteOldFiles(object sender, RoutedEventArgs args)
        {
            try
            {
                List<PathFileComponent> list = this.OldFilesList.FindAll(c => c.IsSelected == true);
                foreach (PathFileComponent component in list)
                {
                    this.OldFilesList.Remove(component);
                }
                this.ReloadOldFiles(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            this.CheckFileCounts();
        }
        public void ToggleAllOldFilesSelection(object sender, RoutedEventArgs args)
        {
            if (_AllOldFilesSelected)
            {
                this.OldFilesList.ForEach(c => c.IsSelected = false);
                this._AllOldFilesSelected = false;
            }
            else
            {
                this.OldFilesList.ForEach(c => c.IsSelected = true);
                this._AllOldFilesSelected = true;
            }
            ReloadOldFiles(true);
        }
        public void NewFilesDropped(object sender, DragEventArgs args)
        {
            if (args.Data.Contains(DataFormats.FileNames))
            {
                List<string> url = new List<string>(args.Data.GetFileNames());
                if (url.Count > 0)
                {
                    foreach (string path in url)
                    {
                        this.NewFilesList.Add(new PathFileComponent(this, path, this.NewFilesList.Count, FileVersion.New));
                    }
                    this.ReloadNewFiles();
                    this.CheckFileCounts();
                }
            }
        }
        public async void AddNewFile(object sender, RoutedEventArgs args)
        {
            try
            {
                string[] url = await this.OpenFileBrowser();
                if (url.Length > 0)
                {
                    foreach (string path in url)
                    {
                        this.NewFilesList.Add(new PathFileComponent(this, path, this.NewFilesList.Count, FileVersion.New));
                    }
                    this.ReloadNewFiles();
                    this.CheckFileCounts();
                    if (this.Options.PatchFileDestination == "")
                    {
                        this.Options.PatchFileDestination = Path.Combine(Path.GetDirectoryName(this.NewFilesList[0].FullPath), "xDelta3_Output");
                    }
                }
            } catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        public void MoveNewFileUp(object sender, RoutedEventArgs args)
        {
            List<PathFileComponent> list = this.NewFilesList;
            List<PathFileComponent> selectedList = list.FindAll(c => c.IsSelected == true);
            this.SortListInPlaceByIndex(selectedList);
            for (int i = 0; i < selectedList.Count; i++)
            {
                PathFileComponent component = selectedList[i];
                // Do not shift up if element before it was not shifted
                if (component.Index != 0 && (i == 0 || (i > 0 && (list.IndexOf(component) - list.IndexOf(selectedList[i - 1]) > 1) || selectedList[i - 1]._Shifted == true)))
                {
                    list[list.IndexOf(component) - 1].Index++;
                    component.Index--;
                    component._Shifted = true;
                    this.SortListInPlaceByIndex(list);
                }
            }
            this.ReloadNewFiles(true);
        }
        public void MoveNewFileDown(object sender, RoutedEventArgs args)
        {
            List<PathFileComponent> list = this.NewFilesList;
            List<PathFileComponent> selectedList = list.FindAll(c => c.IsSelected == true);
            this.SortListInPlaceByIndex(selectedList);
            for (int i = selectedList.Count - 1; i >= 0; i--)
            {
                PathFileComponent component = selectedList[i];
                // Do not shift down if element after it was not shifted
                if (component.Index != list.Count - 1 && (i == selectedList.Count - 1 || (i < selectedList.Count - 1 && (list.IndexOf(selectedList[i + 1]) - list.IndexOf(component)) > 1) || selectedList[i + 1]._Shifted == true))
                {
                    list[list.IndexOf(component) + 1].Index--;
                    component.Index++;
                    component._Shifted = true;
                    this.SortListInPlaceByIndex(list);
                }
            }
            this.ReloadNewFiles(true);
        }
        public void DeleteNewFiles(object sender, RoutedEventArgs args)
        {
            try
            {
                List<PathFileComponent> list = this.NewFilesList.FindAll(c => c.IsSelected == true);
                foreach (PathFileComponent component in list)
                {
                    this.NewFilesList.Remove(component);
                }
                this.ReloadNewFiles(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            this.CheckFileCounts();
        }
        public void ToggleAllNewFilesSelection(object sender, RoutedEventArgs args)
        {
            if(_AllNewFilesSelected)
            {
                this.NewFilesList.ForEach(c => c.IsSelected = false);
                this._AllNewFilesSelected = false;
            } else
            {
                this.NewFilesList.ForEach(c => c.IsSelected = true);
                this._AllNewFilesSelected = true;
            }
            ReloadNewFiles(true);
        }
        public void SaveSettingsClicked(object sender, RoutedEventArgs args)
        {
            this.Options.SaveCurrent();
        }

        public void ResetDefaultsClicked(object sender, RoutedEventArgs args)
        {
            this.Options.ResetToDefault();
            this.Options.SaveCurrent();
        }

        public void GoClicked(object sender, RoutedEventArgs args)
        {
            bool failed = false;
            List<string> missingOldFiles = new List<string>();
            List<string> missingNewFiles = new List<string>();
            bool missingDestinationDirectory = false;

            foreach (PathFileComponent component in  this.OldFilesList)
            {
                if (!File.Exists(component.FullPath))
                {
                    missingOldFiles.Add(component.FullPath);
                    failed = true;
                }
            }

            foreach (PathFileComponent component in this.NewFilesList)
            {
                if (!File.Exists(component.FullPath))
                {
                    missingNewFiles.Add(component.FullPath);
                    failed = true;
                }
            }

            if (!File.Exists(this.Options.PatchFileDestination))
            {
                missingDestinationDirectory = true;
            }

            if (!this.Options.Validate())
            {
                failed = true;
            }

            if (!failed)
            {
                PatchCreator patcher = new PatchCreator(this);
                this.AlreadyBusy = true;
                patcher.CreateReadme();
                if (this.Options.CopyExecutables)
                {
                    patcher.CopyExecutables();
                }
                patcher.CreatePatchingBatchFiles();
            } else
            {
                ErrorDialog dialog = new ErrorDialog(missingOldFiles, missingNewFiles);
                dialog.Show();
                dialog.Topmost = true;
                this.AlreadyBusy = false;
            }
        }

        public async void BrowseOutputDirectory(object sender, RoutedEventArgs args)
        {
            try
            {
                string url = await this.OpenFolderBrowser();
                this.Options.PatchFileDestination = url;
            } catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public void UseShortNamesChecked(object sender, RoutedEventArgs args)
        {
            this.ReloadNewFiles(true);
            this.ReloadOldFiles(true);
        }

        public void SortListInPlaceByIndex(List<PathFileComponent> list)
        {
            list.Sort((x, y) => x.Index.CompareTo(y.Index));
        }

        private static string GetVersion()
        {
            string version = "";
            try
            {
                version = System.Reflection.Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            }
            catch (Exception e) { Debug.WriteLine(e); }
            return version;
        }

        private async Task<string[]> OpenFileBrowser()
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Select file(s)",
                AllowMultiple = true,
            };
            return await dialog.ShowAsync(GetWindow());
        }
        private async Task<string> OpenFolderBrowser()
        {
            var dialog = new OpenFolderDialog()
            {
                Title = "Select output directory",

            };
            return await dialog.ShowAsync(GetWindow());
        }
        Window GetWindow() => (Window)this.VisualRoot;

        private void CheckXDeltaIsOnSystemPath()
        {
            if (File.Exists("xdelta3"))
            {
                XDELTA3_PATH = Path.GetFullPath("xdelta3");
                this.XDeltaOnSystemPath = true;
            }

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, "xdelta3");
                if (File.Exists(fullPath))
                {
                    XDELTA3_PATH = fullPath;
                    this.XDeltaOnSystemPath = true;
                }
            }
            this.XDeltaOnSystemPath = false;

            string location = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "exec");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                XDELTA3_PATH = Path.Combine(location, XDELTA3_BINARY_WINDOWS);
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                XDELTA3_PATH = Path.Combine(location, XDELTA3_BINARY_LINUX);
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                XDELTA3_PATH = Path.Combine(location, XDELTA3_BINARY_MACOS);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.Console.CanClose = true;
            this.Console.Close();
            base.OnClosing(e);
        }

    }
}
