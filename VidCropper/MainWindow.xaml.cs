using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Threading;

namespace VidCropper
{
    public partial class MainWindow : Window
    {
        Point p0, p1;
        Rectangle rect;

        String filePath;
        String saveFilePath;

        public MainWindow()
        {
            InitializeComponent();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_event;
            timer.Start();

            player.MouseLeftButtonDown += player_mouseLeftButtonDown;
            player.MouseLeftButtonUp += player_mouseLeftButtonUp;
        }


        private void player_mouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            canvas.Children.Clear();
            p0 = e.GetPosition(player);
        }

        private void player_mouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            p1 = e.GetPosition(player);
            canvas.Children.Clear();

            rect = new Rectangle
            {
                Height = (p1.Y - p0.Y < 0) ? p0.Y - p1.Y : p1.Y - p0.Y,
                Width = (p1.X - p0.X < 0) ? p0.X - p1.X : p1.X - p0.X,
                Fill = System.Windows.Media.Brushes.Transparent,
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Colors.Red),
                RenderTransform = new TranslateTransform(p0.X < p1.X ? p0.X : p1.X, p0.Y < p1.Y ? p0.Y : p1.Y)
            };

            canvas.Children.Add(rect);
        }

        private void timer_event(object sender, EventArgs e)
        {
            duration.Text = "Duration: " + player.Position.ToString(@"hh\:mm\:ss") + " / " + ((player.NaturalDuration.HasTimeSpan == true) ? player.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss") : "00:00:00");

        }

        private void generate_click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Open file first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (p0 == default(Point) || p1 == default(Point))
            {
                MessageBox.Show("Select rectangle first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            String currentPath = System.IO.Directory.GetCurrentDirectory();
            if ( System.IO.File.Exists(currentPath + "\\ffmpeg.exe") != true )
            {
                MessageBox.Show("Cannot find ffmpeg.exe in current directory!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Video (*.mp4)|*.mp4";
            if (save.ShowDialog() != true) return;
            saveFilePath = save.FileName;

            double ratioX = player.NaturalVideoWidth / player.ActualWidth;
            double ratioY = player.NaturalVideoHeight / player.ActualHeight;

            int p0x = (int)(p0.X * ratioX);
            int p0y = (int)(p0.Y * ratioY);
            int p1x = (int)(p1.X * ratioX);
            int p1y = (int)(p1.Y * ratioY);

            int x = p0x < p1x ? p0x : p1x;
            int y = p0y < p1y ? p0y : p1y;

            int w = (p1x - p0x) < 0 ? p0x - p1x : p1x - p0x;
            int h = (p1y - p0y) < 0 ? p0y - p1y : p1y - p0y;

            FormattableString cmd = $"-i \"{filePath}\" -filter:v \"crop={w}:{h}:{x}:{y}\" -c:v {(codec.SelectedItem as ComboBoxItem).Content.ToString()} \"{saveFilePath}\"";
            command.Text = "./ffmpeg.exe " + cmd.ToString();

            using (Process p = new Process())
            {
                p.StartInfo.FileName = currentPath + "\\ffmpeg.exe";
                p.StartInfo.Arguments = cmd.ToString();
                p.StartInfo.CreateNoWindow = false;
                p.Start();
                p.WaitForExit();
            }
            canvas.Children.Clear();
        }

        private void plus10s_Click(object sender, RoutedEventArgs e)
        {
            if (player.Source != default(Uri)) player.Position += TimeSpan.FromSeconds(9);
        }

        private void pause_Click(object sender, RoutedEventArgs e)
        {
            if (player.Source != default(Uri)) player.Pause();
        }

        private void play_Click(object sender, RoutedEventArgs e)
        {
            if (player.Source != default(Uri)) player.Play();
        }

        private void minus10s_Click(object sender, RoutedEventArgs e)
        {
            if (player.Source != default(Uri)) player.Position -= TimeSpan.FromSeconds(9);
        }

        private void file_click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == true)
            {
                filePath = open.FileName;
                player.Source = new Uri(filePath);
                player.Play();
            }
        }
    }


}
