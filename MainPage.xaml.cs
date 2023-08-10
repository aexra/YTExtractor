﻿using ConsoleApp1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static System.Net.Mime.MediaTypeNames;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace YTExtractor
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        YTAudioExtractor extractor;

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 225));
            ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            extractor = new YTAudioExtractor();
        }

        private void OnUrlChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlBox.Text))
            {
                Download.IsEnabled = false;
                UrlBox.Text = string.Empty;
                return;
            }
            Download.IsEnabled = true;
        }

        private async void OnUrlPasted(object sender, TextControlPasteEventArgs e)
        {
            TextBox urlBox = sender as TextBox;
            var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                try
                {
                    var text = await dataPackageView.GetTextAsync();

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Download.IsEnabled = false;
                        urlBox.Text = string.Empty;
                        return;
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
            Download.IsEnabled = true;
            await OnDownloadPressed(null, null);
        }

        private void OnUrlKeyDown(object sender, KeyRoutedEventArgs e)
        {
            
        }

        private async void OnDownloadPressed(object sender, RoutedEventArgs e)
        {
            string url = UrlBox.Text;
            
            // это нормальная ссылка?
            if (!extractor.IsUrl(url))
            {
                // чото делать если это не нормальная ссылка
            }

            // это (почти) ссылка на ютуб?
            if (!url.Contains("youtu"))
            {
                // это явно не ссылка на ютуб
            }

            // это ссылка на плейлист?
            if (url.Contains("playlist"))
            {
                // это ссылка на плейлист
            }

            // это видео состоит в плейлисте?
            if (url.Contains("&list="))
            {
                // это видео состоит в плейлисте
            }

            // а иначе это просто ссылка на видео
            // предложить скачать видео
        }
    }
}
