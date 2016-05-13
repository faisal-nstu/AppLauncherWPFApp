using System.Diagnostics;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DragEventArgs = System.Windows.DragEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace AppLauncherWPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _tickCount;
        private bool _freezeWindow;
        private bool _isDropZoneOpen;
        private bool _isKeepOpenChecked;
        private AppDetails _appToBeRenamed;
        public ObservableCollection<AppItem> AppItems { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            RenamerControl.Visibility = Visibility.Collapsed;
            DropBgGrid.Visibility = Visibility.Collapsed;
            BrowseButton.Visibility = Visibility.Collapsed;
            RenamerControl.OkButton.Click += OkButtonOnClick;
            RenamerControl.CancelButton.Click += CancelButtonOnClick;
            SetWindow();
            SetListViewItemsSource();            
        }

        private void SetWindow()
        {
            _freezeWindow = false;
            _isDropZoneOpen = false;
            this.Deactivated += OnWindowDeactivated;
            // get mouse position
            double workingAreaHeight = Screen.PrimaryScreen.WorkingArea.Height;
            double workingAreaWidth = Screen.PrimaryScreen.WorkingArea.Width;
            var xPos = GetMousePositionWindowsForms().X;
            // set window size
            var noOfApps = GetAppList().Count;
            var noOfAppsPerRow = Math.Ceiling(Math.Sqrt(noOfApps));
            var noOfAppsPerColumn = (noOfAppsPerRow * noOfAppsPerRow) - noOfApps < noOfAppsPerRow ? noOfAppsPerRow : (noOfAppsPerRow - 1);
            this.Width = 0;
            this.Height = 0;
            var finalWidth = (76 * noOfAppsPerRow) < 200 ? 200 : (76 * noOfAppsPerRow);
            var finalHeight = (100 * noOfAppsPerColumn + 40) < 200 ? 200 : (100 * noOfAppsPerColumn + 40);
            finalHeight += 0;
            finalWidth += 10;
            // set window position
            if (xPos < (finalWidth))
            {
                this.Left = 0;
            }
            else if ((xPos + finalWidth) > workingAreaWidth)
            {
                this.Left = workingAreaWidth - finalWidth;
            }
            else
            {
                this.Left = xPos - (finalWidth / 2);
            }
            finalHeight = finalHeight < 400 ? 400 : finalHeight;
            this.Top = workingAreaHeight - finalHeight;
            AnimateWindow(finalWidth,finalHeight);
        }

        private void AnimateWindow(double finalWidth, double finalHeight)
        {
            this.Width = finalWidth < 350 ? 350 : finalWidth;
            this.Height = finalHeight;
        }

        public Point GetMousePositionWindowsForms()
        {
            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
            return new Point(point.X, point.Y);
        }

        private void OnWindowDeactivated(object sender, EventArgs eventArgs)
        {
            if (_freezeWindow == false)
            {
                Timer timer = new Timer();
                timer.Interval = 500;
                timer.Tick += TimerOnTick;
                timer.Start();
            }
            else
            {
                return;
            }
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            _tickCount++;
            if (_tickCount == 3)
            {
                this.Close();
            }
        }

        private void SetListViewItemsSource()
        {
            LoadApps();
            AppListView.ItemsSource = AppItems;
        }

        public void LoadApps()
        {
            AppItems = new ObservableCollection<AppItem>();
            var appList = GetAppList();
            foreach (var app in appList)
            {
                var appItem = new AppItem();
                appItem.AppName = app.AppName.Length>18 ? app.AppName.Substring(0,15)+"...": app.AppName;
                appItem.AppLocation = app.AppLocation;
                AppItems.Add(appItem);
            }
        }

        

        private void AppListView_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AppItem clickedApp = new AppItem();
            var clickSource = e.OriginalSource;

            if (clickSource is Grid || clickSource is Border || clickSource is TextBlock || clickSource is Image)
            {
                var frameworkElement = e.OriginalSource as FrameworkElement;
                if (frameworkElement != null && frameworkElement.DataContext is AppItem)
                {
                    clickedApp = (e.OriginalSource as FrameworkElement).DataContext as AppItem;
                }
                if (clickedApp != null && clickedApp.AppLocation != "")
                    System.Diagnostics.Process.Start(clickedApp.AppLocation);
            }
            
            
        }

        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            if (!_isDropZoneOpen)
            {
                _isDropZoneOpen = true;
                _freezeWindow = true;
                //AddButton.Content = "✕";
                //AddButton.Content = "✖";
                //AddButton.Content = "×";
                //AddButton.Content = "⨯";
                AddButton.Content = "⨉";
                DropBgGrid.Visibility = Visibility.Visible;
                BrowseButton.Visibility = Visibility.Visible;
            }
            else
            {
                _isDropZoneOpen = false;
                if (!_isKeepOpenChecked)
                {
                    _freezeWindow = false;
                }
                AddButton.Content = "+";
                DropBgGrid.Visibility = Visibility.Collapsed;
                BrowseButton.Visibility = Visibility.Collapsed;
            }
        }

        private void AddApp(string selectedApp)
        {
            AppDetails newApp = new AppDetails();
            newApp.AppLocation = selectedApp;
            newApp.AppName = System.IO.Path.GetFileNameWithoutExtension(selectedApp);

            List<AppDetails> AppList = GetAppList();
            var isAppExists = AppList.Exists(m => m.AppLocation == newApp.AppLocation);
            if (isAppExists == false)
            {
                AppList.Add(newApp);
            }
            else
            {
                return;
            }
            WriteToFile(AppList);
            SetListViewItemsSource();
        }

        private static void WriteToFile(List<AppDetails> AppList)
        {
            System.Xml.Serialization.XmlSerializer writer =
                new System.Xml.Serialization.XmlSerializer(typeof(List<AppDetails>));
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            System.IO.StreamWriter file = new System.IO.StreamWriter(appDataPath + "\\AppLauncher.xml");
            writer.Serialize(file, AppList);
            file.Close();
        }

        private List<AppDetails> GetAppList() 
        {
            System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(List<AppDetails>));
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!File.Exists(appDataPath + "\\AppLauncher.xml"))
            {
                string windowsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                if (File.Exists(windowsFolderPath + "\\notepad.exe"))
                {
                    //FirstRunInit(windowsFolderPath + "\\notepad.exe");   
                    FirstRunInit(null); 
                }
                else
                {
                    FirstRunInit(null);   
                }

            }

            System.IO.StreamReader file2 = new System.IO.StreamReader(appDataPath + "\\AppLauncher.xml");
            List<AppDetails> appList = new List<AppDetails>();
            appList = (List<AppDetails>)reader.Deserialize(file2);
            file2.Close();
            return appList;
        }

        private void FirstRunInit(string path)
        {
            List<AppDetails> initAppList = new List<AppDetails>();
            if (!string.IsNullOrEmpty(path))
            {
                initAppList.Add(new AppDetails()
                {
                    AppLocation = path,
                    AppName = System.IO.Path.GetFileNameWithoutExtension(path)
                });
            }
            WriteToFile(initAppList);
        }

        private void RemoveMenuItemClicked(object sender, RoutedEventArgs e)
        {
            _freezeWindow = true;
            AppDetails clickedApp = new AppDetails();
            if (AppListView.SelectedItem != null )
            {
                clickedApp.AppName = (AppListView.SelectedItem as AppItem).AppName;
                clickedApp.AppLocation = (AppListView.SelectedItem as AppItem).AppLocation;
            }
            var appList = GetAppList();
            var appToBeRemoved = appList.Find(a => a.AppLocation == clickedApp.AppLocation);
            appList.Remove(appToBeRemoved);
            WriteToFile(appList);
            SetListViewItemsSource();
            if (!_isKeepOpenChecked)
            {
                _freezeWindow = false;
            }
        }

        private void OpenFileLocationClicked(object sender, RoutedEventArgs e)
        {
            if (AppListView.SelectedItem != null)
            {
                var appItem = AppListView.SelectedItem as AppItem;
                if (appItem != null)
                {
                    var folderPath = System.IO.Path.GetDirectoryName(appItem.AppLocation);
                    if (folderPath != null) Process.Start(folderPath);
                }
            }
        }

        private void RenameMenuItemClicked(object sender, RoutedEventArgs e)
        {
            _freezeWindow = true;
            AppDetails clickedApp = new AppDetails();
            if (AppListView.SelectedItem != null)
            {
                clickedApp.AppName = (AppListView.SelectedItem as AppItem).AppName;
                clickedApp.AppLocation = (AppListView.SelectedItem as AppItem).AppLocation;
            }
            var appList = GetAppList();
            _appToBeRenamed = appList.Find(a => a.AppLocation == clickedApp.AppLocation);
            RenamerControl.Visibility = Visibility.Visible;
            RenamerControl.NameInpuTextBox.Text = _appToBeRenamed.AppName;
            RenamerControl.NameInpuTextBox.Focus();
        }

        private void CancelButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            RenamerControl.Visibility = Visibility.Collapsed;
            if (!_isKeepOpenChecked)
            {
                _freezeWindow = false;
            }
        }

        private void OkButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!string.IsNullOrEmpty(RenamerControl.NameInpuTextBox.Text))
            {
                var appList = GetAppList();
                appList.Find(a => a.AppName == _appToBeRenamed.AppName).AppName =
                    RenamerControl.NameInpuTextBox.Text.Trim();
                WriteToFile(appList);
                SetListViewItemsSource();
                RenamerControl.Visibility = Visibility.Collapsed;
                if (!_isKeepOpenChecked)
                {
                    _freezeWindow = false;
                }
            }
        }

        private void DropZone_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (var file in files)
                {
                    AddApp(file);
                }
            }
            _isDropZoneOpen = false;
            AddButton.Content = "+";
            DropBgGrid.Visibility = Visibility.Collapsed;
            BrowseButton.Visibility = Visibility.Collapsed;
            if (!_isKeepOpenChecked)
            {
                _freezeWindow = false;
            }
        }

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileBrowser = new OpenFileDialog();
            fileBrowser.ShowDialog();

            var selectedApp = fileBrowser.FileName;
            if (!string.IsNullOrEmpty(selectedApp))
            {
                AddApp(selectedApp);
            }
            _isDropZoneOpen = false;
            AddButton.Content = "+";
            DropBgGrid.Visibility = Visibility.Collapsed;
            BrowseButton.Visibility = Visibility.Collapsed;
            if (!_isKeepOpenChecked)
            {
                _freezeWindow = false;   
            }
        }
        private void KeepOpenRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isKeepOpenChecked)
            {
                _isKeepOpenChecked = false;
                _freezeWindow = false;
                //KeepOpenRadioButton.IsChecked = false;
                PinImage.Source = new BitmapImage(new Uri("/assets/pin24x24.png", UriKind.Relative));
            }
            else
            {
                _isKeepOpenChecked = true;
                _freezeWindow = true;
                PinImage.Source = new BitmapImage(new Uri("/assets/unpin24x24.png", UriKind.Relative));
            }
        }
    }
}
