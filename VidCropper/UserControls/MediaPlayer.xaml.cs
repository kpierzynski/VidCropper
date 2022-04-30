using System;
using System.Collections.Generic;
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

namespace VidCropper.UserControls
{
    /// <summary>
    /// Interaction logic for MediaPlayer.xaml
    /// </summary>
    public partial class MediaPlayer : UserControl
    {
        String mediaPath;
        bool playing = false;

        Point p0, p1;
        Rectangle rect;

        public delegate void MediaPlayerRectPickEventHandler(object sender, int x, int y, int w, int h);
        public event MediaPlayerRectPickEventHandler RectPicked;

        public delegate void MediaPlayerMetadataEventHandler(object sender, TimeSpan length, int w, int h );
        public event MediaPlayerMetadataEventHandler Loaded;

        public bool IsPlaying
        {
            get
            {
                return playing;
            }
        }

        public MediaPlayer()
        {
            InitializeComponent();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_event;
            timer.Start();

            player.MediaOpened += player_mediaOpened;
            player.MediaEnded += (s, e) => { playing = false; };

            player.MouseLeftButtonDown += player_mouseLeftButtonDown;
            player.MouseLeftButtonUp += player_mouseLeftButtonUp;

            slider.PreviewMouseLeftButtonDown += Slider_PreviewMouseLeftButtonDown;
            slider.ValueChanged += Slider_ValueChanged; ;
            slider.PreviewMouseLeftButtonUp += Slider_PreviewMouseLeftButtonUp;

            this.SizeChanged += MediaPlayer_SizeChanged;
        }

        private void MediaPlayer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvas.Children.Clear();
            if(RectPicked != null) RectPicked.Invoke(this, 0, 0, (int)player.NaturalVideoWidth, (int)player.NaturalVideoHeight);
        }

        public void Init(string path)
        {
            InitializeComponent();
            SetMediaPath(path);
            EnableControls();
            Play();
            canvas.Children.Clear();
        }

        private void timer_event(object sender, EventArgs e)
        {
            duration.Text = player.Position.ToString(@"hh\:mm\:ss");
            slider.Value = player.Position.TotalSeconds;
        }

        private void player_mouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            canvas.Children.Clear();
            p0 = e.GetPosition(player);
        }

        private void player_mouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            p1 = e.GetPosition(player);
            int x, y, w, h;
            h = (int)((p1.Y - p0.Y < 0) ? p0.Y - p1.Y : p1.Y - p0.Y);
            w = (int)((p1.X - p0.X < 0) ? p0.X - p1.X : p1.X - p0.X);
            x = (int)(p0.X < p1.X ? p0.X : p1.X);
            y = (int)(p0.Y < p1.Y ? p0.Y : p1.Y);

            rect = new Rectangle
            {
                Height = h,
                Width = w,
                Fill = new SolidColorBrush( Color.FromArgb(63,127,0,0) ),
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Colors.Red),
                RenderTransform = new TranslateTransform(x, y),
                IsHitTestVisible = false,
                
            };

            double ratioX = player.NaturalVideoWidth / player.ActualWidth;
            double ratioY = player.NaturalVideoHeight / player.ActualHeight;

            canvas.Children.Add(rect);
            RectPicked.Invoke(this, (int)(x*ratioX), (int)(y*ratioY), (int)(w*ratioX), (int)(h*ratioY));
        }

        public void Play()
        {
            player.Play();
            playing = true;
        }

        public void Pause()
        {
            player.Pause();
            playing = false;
        }

        private void Slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Play();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double position = e.NewValue;
            player.Position = TimeSpan.FromSeconds(position);
        }

        private void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Pause();
        }

        private void player_mediaOpened(object sender, RoutedEventArgs e)
        {
            length.Text = player.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
            slider.Minimum = 0;
            slider.Maximum = player.NaturalDuration.TimeSpan.TotalSeconds;

            Loaded.Invoke(this, player.NaturalDuration.TimeSpan, player.NaturalVideoWidth, player.NaturalVideoHeight);
        }

        private void SetMediaPath(string path)
        {
            mediaPath = path;
            player.Source = new Uri(mediaPath);
        }

        private void EnableControls()
        {
            pause.IsEnabled = true;
            play.IsEnabled = true;
            minus5s.IsEnabled = true;
            plus5s.IsEnabled = true;
            slider.IsEnabled = true;
        }

        private void minus5s_Click(object sender, RoutedEventArgs e)
        {
            player.Position -= TimeSpan.FromSeconds(4.5);
        }

        private void play_Click(object sender, RoutedEventArgs e)
        {
            player.Play();
            playing = true;
        }

        private void pause_Click(object sender, RoutedEventArgs e)
        {
            player.Pause();
            playing = false;
        }

        private void plus5s_Click(object sender, RoutedEventArgs e)
        {
            player.Position += TimeSpan.FromSeconds(4.5);
        }
    }
}
