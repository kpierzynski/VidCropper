using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
            player.MediaOpened += player_loaded;

            progressBar.PreviewMouseLeftButtonDown += progressBar_start_dragging_event;
            progressBar.ValueChanged += progressBar_value_event;
            progressBar.PreviewMouseLeftButtonUp += progressBar_end_dragging_event;
        }

        private void progressBar_start_dragging_event(object sender, MouseButtonEventArgs e)
        {
            player.Pause();
        }

        private void progressBar_end_dragging_event(object sender, MouseButtonEventArgs e)
        {
            player.Play();
        }

        private void progressBar_value_event(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double position = e.NewValue;
            player.Position = TimeSpan.FromSeconds(position);
        }

        private void player_loaded(object sender, RoutedEventArgs e)
        {
            cutFrom.Text = "00:00:00";
            if (String.IsNullOrEmpty(cutTo.Text)) cutTo.Text = player.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
            length.Text = cutTo.Text;

            play.IsEnabled = true;
            pause.IsEnabled = true;
            plus10s.IsEnabled = true;
            minus10s.IsEnabled = true;
            generate.IsEnabled = true;
            progressBar.IsEnabled = true;
            progressBar.Minimum = 0;
            progressBar.Maximum = player.NaturalDuration.TimeSpan.TotalSeconds;
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
            duration.Text = player.Position.ToString(@"hh\:mm\:ss");
            progressBar.Value = player.Position.TotalSeconds;
        }

        private void generate_click(object sender, RoutedEventArgs e)
        {
            String from = cutFrom.Text;
            String to = cutTo.Text;
            DateTime fromTime;
            DateTime toTime;
            String timeStampFormat = "hh:mm:ss";

            bool resFrom = DateTime.TryParseExact(from, timeStampFormat, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out fromTime);
            bool resTo = DateTime.TryParseExact(to, timeStampFormat, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out toTime);

            if (fromTime > toTime)
            {
                MessageBox.Show("From timestamp should be before to timestamp.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (from.Length == 0 || to.Length == 0 || !resFrom || !resTo)
            {
                MessageBox.Show("Missing or incorrect from or to timestamps! Pass in hh:mm:ss format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (String.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Open file first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            String currentPath = System.IO.Directory.GetCurrentDirectory();
            if (System.IO.File.Exists(currentPath + "\\ffmpeg.exe") != true)
            {
                MessageBox.Show("Cannot find ffmpeg.exe in current directory!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.FileName = System.IO.Path.GetFileNameWithoutExtension(filePath) + "_cropped";
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

            if (p0 == default(Point) || p1 == default(Point))
            {
                x = 0;
                y = 0;
                w = player.NaturalVideoWidth;
                h = player.NaturalVideoHeight;
            }

            FormattableString cmd = $"-ss {from}.0 -i \"{filePath}\" -t { (toTime - fromTime).ToString(@"hh\:mm\:ss") }.0 -filter:v \"crop={w}:{h}:{x}:{y}\" -c:v {(codec.SelectedItem as ComboBoxItem).Content.ToString()} \"{saveFilePath}\"";
            command.Text = "./ffmpeg.exe " + cmd.ToString();

            using (Process p = new Process())
            {
                p.StartInfo.FileName = currentPath + "\\ffmpeg.exe";
                p.StartInfo.Arguments = cmd.ToString();
                p.StartInfo.CreateNoWindow = false;
                p.Start();
            }
            canvas.Children.Clear();
        }

        private void plus10s_Click(object sender, RoutedEventArgs e)
        {
            player.Position += TimeSpan.FromSeconds(4);
        }

        private void pause_Click(object sender, RoutedEventArgs e)
        {
            player.Pause();
        }

        private void play_Click(object sender, RoutedEventArgs e)
        {
            player.Play();
        }

        private void minus10s_Click(object sender, RoutedEventArgs e)
        {
            player.Position -= TimeSpan.FromSeconds(5);
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
