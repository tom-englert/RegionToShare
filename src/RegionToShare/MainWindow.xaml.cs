using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using TomsToolbox.Wpf;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RecordingWindow? _recordingWindow;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_recordingWindow == null)
            {
                // WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                RenderTargetHost.Visibility = Visibility.Visible;

                _recordingWindow = new RecordingWindow(this, RenderTargetWindow.Handle)
                {
                    Left = Left - RecordingWindow.BorderSize.Left,
                    Top = Top - RecordingWindow.BorderSize.Top,
                    Width = ClientArea.ActualWidth + RecordingWindow.BorderSize.Left + RecordingWindow.BorderSize.Right,
                    Height = ClientArea.ActualHeight + RecordingWindow.BorderSize.Top + RecordingWindow.BorderSize.Bottom,
                };

                var left = Left;
                var top = Top;

                Left += _recordingWindow.Width;

                _recordingWindow.Closed += (_, _) =>
                {
                    Left = left;
                    Top = top;
                    RenderTargetHost.Visibility = Visibility.Collapsed;
                    WindowStyle = WindowStyle.ThreeDBorderWindow;
                    ResizeMode = ResizeMode.CanResize;

                    _recordingWindow = null;
                };

                _recordingWindow.Show();
            }
        }
    }
}
