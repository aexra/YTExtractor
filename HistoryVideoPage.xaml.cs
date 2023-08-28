using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace YTExtractor
{
    public sealed partial class HistoryVideoPage : UserControl
    {
        public HistoryVideoPage(string _videoTitle, string _videoThumbnail, string _channelTitle, string _channelThumbnail)
        {
            this.InitializeComponent();

            VideoTitle.Text = _videoTitle;
            VideoThumbnail.Source = new BitmapImage(new Uri(_videoThumbnail));;
            ChannelTitle.Text = _channelTitle;
            ChannelThumbnail.Source = new BitmapImage(new Uri(_channelThumbnail));
        }

        public void SetProgress(int value)
        {
            ProgressBar.Value = value;
            if (value == 100)
                ProgressBar.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
        }
    }
}
