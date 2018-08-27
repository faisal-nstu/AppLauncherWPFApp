using System.Windows;
using System.Windows.Controls;

namespace AppLauncherWPFApp.controls
{
    /// <summary>
    /// ImageView displays image files using themselves as their icons.
    /// In order to write our own visual tree of a view, we should override its
    /// DefaultStyleKey and ItemContainerDefaultKey. DefaultStyleKey specifies
    /// the style key of ListView; ItemContainerDefaultKey specifies the style
    /// key of ListViewItem.
    /// </summary>
    public class ImageView : ViewBase
    {
        #region DefaultStyleKey

        protected override object DefaultStyleKey => new ComponentResourceKey(GetType(), "ImageView");

        #endregion

        #region ItemContainerDefaultStyleKey

        protected override object ItemContainerDefaultStyleKey => new ComponentResourceKey(GetType(), "ImageViewItem");

        #endregion

        private void PinImage_OnImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var ff = sender;
        }
    }
}