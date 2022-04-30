using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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

namespace VidCropper.UserControls
{
    /// <summary>
    /// Interaction logic for ControlPanel.xaml
    /// </summary>
    public partial class ControlPanel : UserControl
    {
        String filePath;
        int x, y, w, h;
        TimeSpan length;

        Thread th;
        Process ffmpeg;

        public delegate void ControlPanelFilePickEventHandler(object sender, String path);
        public event ControlPanelFilePickEventHandler FilePicked;

        public ControlPanel()
        {
            InitializeComponent();
        }


        public void Kill()
        {
            if( ffmpeg != null ) ffmpeg.Kill();
            if( th != null ) th.Abort();
        }

        public void SetLengthField(TimeSpan length)
        {
            this.length = length;
            cutTo.Text = length.ToString(@"hh\:mm\:ss");
            cutFrom.Text = "00:00:00";
        }

        public void SetRect(int x, int y, int w, int h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
            resizingInfo.Text = w.ToString() + " width: " + h.ToString() + " height (x: " + x.ToString() + ", y: " + y.ToString() + ")";

        }

        private void open_Click(object sender, RoutedEventArgs e)
        {
            String _filePath;
            bool result = OpenFileDialog(out _filePath);
            if (result == true)
            {
                filePath = _filePath;
                progress.Value = 0;
                if( FilePicked != null ) FilePicked.Invoke(this, filePath);
                save.IsEnabled = true;
            }
        }
        private bool checkTimestamps(out DateTime fromSpan, out DateTime toSpan)
        {
            String from = cutFrom.Text;
            String to = cutTo.Text;
            DateTime fromTime;
            DateTime toTime;
            String timeStampFormat = "hh:mm:ss";

            fromSpan = default;
            toSpan = default;

            if (String.IsNullOrEmpty(from) || String.IsNullOrEmpty(to)) return false;

            bool resFrom = DateTime.TryParseExact(from, timeStampFormat, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out fromTime);
            bool resTo = DateTime.TryParseExact(to, timeStampFormat, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out toTime);

            fromSpan = fromTime;
            toSpan = toTime;

            if (!resFrom || !resTo) return false;
            if (fromTime > toTime) return false;

            return true;
        }
        private bool checkFilePath()
        {
            if (String.IsNullOrEmpty(filePath)) return false;
            if (!File.Exists(filePath)) return false;
            return true;
        }

        private bool checkFFmpeg(out String path)
        {
            String currentPath = System.IO.Directory.GetCurrentDirectory();
            path = currentPath;
            if (!File.Exists(currentPath + "\\ffmpeg.exe")) return false;

            return true;
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            DateTime toTime, fromTime;
            if (!checkTimestamps(out fromTime, out toTime))
            {
                MessageBox.Show("Missing or incorrect from or to timestamps! Pass in hh:mm:ss format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!checkFilePath())
            {
                MessageBox.Show("Open file first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            String currentPath;
            if (!checkFFmpeg(out currentPath))
            {
                MessageBox.Show("Cannot find ffmpeg.exe in current directory!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            String savePath;
            if (!SaveFileDialog(out savePath))
            {
                MessageBox.Show("Save path is incorrent!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            progress.Minimum = 0;
            progress.Maximum = (toTime - fromTime).TotalSeconds;

            FormattableString[] cmds = new FormattableString[]
            {
                $"-hwaccel auto",
                $"-ss {cutFrom.Text}.0",
                $"-i \"{filePath}\"",
                $"-t { (toTime - fromTime).ToString(@"hh\:mm\:ss") }.0",
                $"-filter:v \"crop={w}:{h}:{x}:{y}\"",
                $"-c:v libx265",
                $"-c:a copy",
                $"\"{savePath}\""
            };

            String cmd = default;
            foreach (var arg in cmds)
            {
                cmd += arg.ToString();
                cmd += " ";
            }

            th = new Thread(() =>
            {
                using (ffmpeg = new Process()
                {
                    StartInfo =
                {
                    FileName = currentPath + @"\ffmpeg.exe",
                    Arguments = cmd,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true

                },
                    EnableRaisingEvents = true,
                })
                {
                    ffmpeg.ErrorDataReceived += ffmpeg_process_bar;
                    EventHandler exitHandler = null;
                    exitHandler = (s, ev) =>
                    {
                        if (Application.Current != null) Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            if (showDone.IsChecked == true) MessageBox.Show("Conversion done!");
                        }));
                        //progress.Value = 0;
                        ffmpeg.Exited -= exitHandler;
                    };
                    ffmpeg.Exited += exitHandler;
                    ffmpeg.Start();
                    ffmpeg.BeginOutputReadLine();
                    ffmpeg.BeginErrorReadLine();
                    ffmpeg.WaitForExit();
                }
            });
            th.Start();
        }

        private void ffmpeg_process_bar(object sender, DataReceivedEventArgs e)
        {
            String data = e.Data;
            if (String.IsNullOrEmpty(data)) return;

            if (data.Contains("time=") && data.Contains(" bitrate="))
            {
                int start, end;
                start = data.IndexOf("time=") + 5;
                end = data.IndexOf(" bitrate=", start);
                string time = data.Substring(start, end - start);
                Console.WriteLine(data);

                start = data.IndexOf("speed=") + 6;
                end = data.IndexOf("x", start);
                string speed = data.Substring(start, end - start);
                double speedValue = double.Parse(speed.Trim(), CultureInfo.InvariantCulture);
                if (speedValue == 0) speedValue = 1;
                TimeSpan progress_value;
                progress_value = TimeSpan.Parse(time);

                if( Application.Current != null ) Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    progress.Value = progress_value.TotalSeconds;
                    eta.Text = TimeSpan.FromSeconds((progress.Maximum - progress.Value) / speedValue).ToString(@"hh\:mm\:ss");
                }));


            }
        }

        private bool OpenFileDialog(out String path)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == true)
            {
                path = open.FileName;
                return true;
            }
            path = "";
            return false;
        }

        private bool SaveFileDialog(out String path)
        {
            SaveFileDialog save = new SaveFileDialog()
            {
                FileName = System.IO.Path.GetFileNameWithoutExtension(filePath) + "_cropped",
                Filter = "Video (*.mp4)|*.mp4"
            };

            if (save.ShowDialog() != true)
            {
                path = "";
                return false;
            }

            path = save.FileName;
            return true;
        }
    }
}
