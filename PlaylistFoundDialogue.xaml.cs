using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Диалоговое окно содержимого" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace YTExtractor
{
    public sealed partial class PlaylistFoundDialogue : ContentDialog
    {
        public PlaylistFoundDialogue(string _playlistTitle, string _playlistThumbnail, string _channelTitle, string _channelThumbnail)
        {
            this.InitializeComponent();
            playlistTitle.Text = _playlistTitle;
            playlistThumbnail.Source = new BitmapImage(new Uri(_playlistThumbnail));
            channelTitle.Text = _channelTitle;
            channelThumbnail.Source = new BitmapImage(new Uri(_channelThumbnail));
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
