using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Remoting.Activation;
using System.Text;
using System.Threading.Tasks;
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
                Icon _appIcon = null;
                if (AppLocation != "")
                {
                    _appIcon = Icon.ExtractAssociatedIcon(AppLocation);
                    _imgSrc = IconToImageSource(_appIcon);
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
