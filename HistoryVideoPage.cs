using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YTExtractor
{
    public sealed class HistoryVideoPage : Control
    {
        public HistoryVideoPage()
        {
            this.DefaultStyleKey = typeof(HistoryVideoPage);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(HistoryVideoPage),
            new PropertyMetadata(default(string), new PropertyChangedCallback(OnLabelChanged)));

        public bool HasLabelValue { get; set; }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HistoryVideoPage labelControl = d as HistoryVideoPage; //null checks omitted
            String s = e.NewValue as String; //null checks omitted
            if (s == String.Empty)
            {
                labelControl.HasLabelValue = false;
            }
            else
            {
                labelControl.HasLabelValue = true;
            }
        }
    }
}
