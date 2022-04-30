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

namespace VidCropper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            controlPanel.FilePicked += ControlPanel_FilePicked;

            mediaPlayer.RectPicked += MediaPlayer_RectPicked;
            mediaPlayer.Loaded += MediaPlayer_Loaded;
        }

        private void MediaPlayer_Loaded(object sender, TimeSpan length, int w, int h)
        {
            controlPanel.SetLengthField(length);
            controlPanel.SetRect(0, 0, w, h);
        }

        private void MediaPlayer_RectPicked(object sender, int x, int y, int w, int h)
        {
            controlPanel.SetRect(x, y, w, h);
        }

        private void ControlPanel_FilePicked(object sender, string path)
        {
            mediaPlayer.Init(path);
        }
    }
}
