using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AppLauncherWPFApp
{
    public class AppItem
    {
        public string AppName { get; set; }
        public string AppLocation { get; set; }

        

        private ImageSource _imgSrc;

        public ImageSource ImageSource
        {
            get 
            {
                if (AppLocation != "")
                {
                    try
                    {
                        FileAttributes attr = File.GetAttributes(AppLocation);

                        //detect whether its a directory or file
                        Icon appIcon;
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            var explorerLocation = Environment.GetEnvironmentVariable("windir") + "\\explorer.exe";
                            appIcon = Icon.ExtractAssociatedIcon(explorerLocation);
                        }
                        else
                        {
                            appIcon = Icon.ExtractAssociatedIcon(AppLocation);
                        }
                        _imgSrc = IconToImageSource(appIcon);
                    }
                    catch (Exception)
                    {
                        return  new BitmapImage(new Uri("/assets/not_found.png", UriKind.Relative));
                    }
                }
                return _imgSrc; 
            }
            set { _imgSrc = value; }
        }

        public ImageSource IconToImageSource(Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            
            return imageSource;
        }

    }
}
