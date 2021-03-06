﻿using System.Diagnostics;
using Forms = System.Windows.Forms;
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
using System.Windows.Media;
using System.Linq;

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
        private AppItem _selectedApp;
        private int minWidth = 350;
        private int minHeight = 400;

        public ObservableCollection<AppItem> AppItems { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            RenamerControl.Visibility = Visibility.Collapsed;
            DropBgGrid.Visibility = Visibility.Collapsed;
            BrowseFileButton.Visibility = Visibility.Collapsed;
            BrowseFolderButton.Visibility = Visibility.Collapsed;
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
            double workingAreaHeight = Forms.Screen.PrimaryScreen.WorkingArea.Height;
            double workingAreaWidth = Forms.Screen.PrimaryScreen.WorkingArea.Width;
            var xPos = GetMousePositionWindowsForms().X;
            // set window size
            var noOfApps = GetAppList().Count;
            var noOfAppsPerRow = Math.Ceiling(Math.Sqrt(noOfApps));
            var noOfAppsPerColumn = (noOfAppsPerRow * noOfAppsPerRow) - noOfApps < noOfAppsPerRow ? noOfAppsPerRow : (noOfAppsPerRow - 1);
            this.Width = 0;
            this.Height = 0;
            var finalWidth = (76 * noOfAppsPerRow) < minWidth ? minWidth : (76 * noOfAppsPerRow);
            var finalHeight = (100 * noOfAppsPerColumn + 40) < minWidth ? minWidth : (100 * noOfAppsPerColumn + 40);
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
            finalHeight = finalHeight < minHeight ? minHeight : finalHeight;
            this.Top = workingAreaHeight - finalHeight;
            AnimateWindow(finalWidth,finalHeight);
        }

        private void AnimateWindow(double finalWidth, double finalHeight)
        {
            this.Width = finalWidth < minWidth ? minWidth : finalWidth;
            this.Height = finalHeight;
        }

        public Point GetMousePositionWindowsForms()
        {
            System.Drawing.Point point = Forms.Control.MousePosition;
            return new Point(point.X, point.Y);
        }

        private void OnWindowDeactivated(object sender, EventArgs eventArgs)
        {
            if (_freezeWindow == false)
            {
                Forms.Timer timer = new Forms.Timer();
                timer.Interval = 500;
                timer.Tick += TimerOnTick;
                timer.Start();
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
                {
                    try
                    {
                        Process.Start(clickedApp.AppLocation);
                    }
                    catch (Exception)
                    {
                        ShowMessage(clickedApp);
                    }
                }
            }
            
            
        }

        private void ShowMessage(AppItem clickedApp)
        {
            _selectedApp = clickedApp;
            NotFoundMessageGrid.Visibility = Visibility.Visible;
        }

        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            if (!_isDropZoneOpen)
            {
                _isDropZoneOpen = true;
                _freezeWindow = true;
                AddButton.Content = "×"; //⨉
                DropBgGrid.Visibility = Visibility.Visible;
                BrowseFileButton.Visibility = Visibility.Visible;
                BrowseFolderButton.Visibility = Visibility.Visible;
            }
            else
            {
                HideFileFolderSelection();
            }
        }

        private void AddAppOrFolder(string selectedApp)
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

            DeleteItem();

            if (!_isKeepOpenChecked)
                _freezeWindow = false;
        }

        private void DeleteItem()
        {
            var clickedApp = new AppDetails();
            var appItem = AppListView.SelectedItem as AppItem;
            if (appItem != null)
            {
                clickedApp.AppName = appItem.AppName;
                clickedApp.AppLocation = appItem.AppLocation;
            }
            var appList = GetAppList();
            var appToBeRemoved = appList.Find(a => a.AppLocation == clickedApp.AppLocation);
            appList.Remove(appToBeRemoved);
            WriteToFile(appList);
            SetListViewItemsSource();
        }

        private void OpenFileLocationClicked(object sender, RoutedEventArgs e)
        {
            if (AppListView.SelectedItem != null)
            {
                var appItem = AppListView.SelectedItem as AppItem;
                var folderPath = System.IO.Path.GetDirectoryName(appItem?.AppLocation);
                if (folderPath != null) Process.Start(folderPath);
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
                    AddAppOrFolder(file);
                }
            }

            HideFileFolderSelection();
        }

        private void BrowseFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            using (var openFileDialog = new Forms.OpenFileDialog())
            {
                Forms.DialogResult result = openFileDialog.ShowDialog();

                if (result == Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
                {
                    AddAppOrFolder(openFileDialog.FileName);
                }
            }

            HideFileFolderSelection();
        }

        private void BrowseFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            using (var fbd = new Forms.FolderBrowserDialog())
            {
                Forms.DialogResult result = fbd.ShowDialog();

                if (result == Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    AddAppOrFolder(fbd.SelectedPath);
                }
            }

            HideFileFolderSelection();
        }

        private void HideFileFolderSelection()
        {
            _isDropZoneOpen = false;
            AddButton.Content = "+";
            DropBgGrid.Visibility = Visibility.Collapsed;
            BrowseFileButton.Visibility = Visibility.Collapsed;
            BrowseFolderButton.Visibility = Visibility.Collapsed;
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
                PinImage.Source = new BitmapImage(new Uri("/assets/unpinned.png", UriKind.Relative));
            }
            else
            {
                _isKeepOpenChecked = true;
                _freezeWindow = true;
                PinImage.Source = new BitmapImage(new Uri("/assets/pinned.png", UriKind.Relative));
            }
        }

        private void YesButton_OnClick(object sender, RoutedEventArgs e)
        {
            DeleteItem();
            NotFoundMessageGrid.Visibility = Visibility.Collapsed;
        }

        private void NoButton_OnClick(object sender, RoutedEventArgs e)
        {
            NotFoundMessageGrid.Visibility = Visibility.Collapsed;
        }


        #region RE-ORDER

        private Point startPoint = new Point();
        private int startIndex = -1;

        private void AppListView_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get current mouse position
            startPoint = e.GetPosition(null);
        }

        // Helper to search up the VisualTree
        private static T FindAnchestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private void AppListView_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                       Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                // Get the dragged ListViewItem
                ListView listView = sender as ListView;
                ListViewItem listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
                if (listViewItem == null) return;           // Abort
                                                            // Find the data behind the ListViewItem
                AppItem item = (AppItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
                if (item == null) return;                   // Abort
                                                            // Initialize the drag & drop operation
                startIndex = listView.SelectedIndex;
                DataObject dragData = new DataObject("WorkItem", item);
                DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Copy | DragDropEffects.Move);
            }
        }

        private void AppListView_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("WorkItem") || sender != e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void AppListView_Drop(object sender, DragEventArgs e)
        {
            int index = -1;

            if (e.Data.GetDataPresent("WorkItem") && sender == e.Source)
            {
                // Get the drop ListViewItem destination
                ListView listView = sender as ListView;
                ListViewItem listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
                if (listViewItem == null)
                {
                    // Abort
                    e.Effects = DragDropEffects.None;
                    return;
                }
                // Find the data behind the ListViewItem
                AppItem item = (AppItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
                // Move item into observable collection 
                // (this will be automatically reflected to lstView.ItemsSource)
                e.Effects = DragDropEffects.Move;
                index = AppItems.IndexOf(item);
                if (startIndex >= 0 && index >= 0)
                {
                    AppItems.Move(startIndex, index);
                }
                startIndex = -1;        // Done!

                SaveNewOrder();
            }
        }

        private void SaveNewOrder()
        {
            var newOrder = AppItems.Select(item => new AppDetails { AppName = item.AppName, AppLocation = item.AppLocation });
            WriteToFile(newOrder.ToList());
        }

        #endregion
    }
}
