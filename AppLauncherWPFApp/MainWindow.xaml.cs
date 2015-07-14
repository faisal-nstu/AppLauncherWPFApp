using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        private AppDetails _appToBeRenamed;
        public ObservableCollection<AppItem> AppItems { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            RenamerControl.Visibility = Visibility.Collapsed;
            RenamerControl.OkButton.Click += OkButtonOnClick;
            RenamerControl.CancelButton.Click += CancelButtonOnClick;
            SetWindow();
            SetListViewItemsSource();
        }

        private void SetWindow()
        {
            _freezeWindow = false;
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
            finalHeight += 20;
            finalWidth += 40;
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
            this.Top = workingAreaHeight - finalHeight;
            AnimateWindow(finalWidth,finalHeight);
        }

        private void AnimateWindow(double finalWidth, double finalHeight)
        {
            this.Width = finalWidth;
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
                appItem.AppName = app.AppName;
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
            _freezeWindow = true;
            OpenFileDialog fileBrowser = new OpenFileDialog();
            fileBrowser.ShowDialog();
            var selectedApp = fileBrowser.FileName;
            if (!string.IsNullOrEmpty(selectedApp))
            {
                AddApp(selectedApp);
            }
            _freezeWindow = false;
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
                FirstRunInit(windowsFolderPath + "\\notepad.exe");

            }

            System.IO.StreamReader file2 = new System.IO.StreamReader(appDataPath + "\\AppLauncher.xml");
            List<AppDetails> appList = new List<AppDetails>();
            appList = (List<AppDetails>)reader.Deserialize(file2);
            file2.Close();
            return appList;
        }

        private void FirstRunInit(string path)
        {
            List<AppDetails> initAppList = new List<AppDetails>() 
            {
                new AppDetails()
                {
                    AppLocation = path,
                    AppName = System.IO.Path.GetFileNameWithoutExtension(path)
                }
            };
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
            _freezeWindow = false;
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
            _freezeWindow = false;
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
                _freezeWindow = false;
            }
        }
    }
}
