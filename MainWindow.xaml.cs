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
using WpfScreenHelper;

namespace MangaViewer_WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage m_currentImg;
        List<string> m_fileList;
        string m_sFolderPath;
        int m_index;
        int isFreeMode;
        bool isReposMode;
        int m_zoomRatio;
        string sFile;
        readonly List<string> EXT_NAMES = new List<string> { "gif", "jpg", "bmp", "png", "jpeg", "tif" };
        public MainWindow(string[] args)
        {
            InitializeComponent();

            if (args.Length == 0)
            {
                OpenFileDialog openPicDialog = new OpenFileDialog();
                openPicDialog.Filter = "Image files|*."+EXT_NAMES.Aggregate((a,b) => a +";*." + b);
                if (openPicDialog.ShowDialog() != true)
                    Application.Current.Shutdown();
                else
                {
                    sFile = openPicDialog.FileName;
                    m_sFolderPath = sFile.Substring(0,sFile.LastIndexOf('\\') + 1);
                }
            }
            else
            {
                sFile = args[0];
            }

            m_currentImg = new BitmapImage();
            m_currentImg.BeginInit();
            m_currentImg.CacheOption = BitmapCacheOption.OnLoad;
            m_currentImg.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            m_currentImg.UriSource = new Uri("d:/e.png");
            m_currentImg.EndInit();
            Screen screen = Screen.AllScreens.ToList()[0]; ;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Img.Source = m_currentImg;
            Img.Clip = new RectangleGeometry(new Rect(0, 0, 800, 800));
            Img.RenderTransform = new ScaleTransform(1, 1);

            Tips.Text = "SOMETHING BULABULA...";
            Tips.Visibility = Visibility.Visible;
            Img.Margin = new Thickness((Width - 800) / 2, (Height - 800) / 2, 0, 0);
            //Trace.WriteLine(string.Format("Width: W{0}, I{1}; Height: W{2}, I{3}; M:{4}",Width,Img.ActualWidth,Height,Img.ActualHeight, Img.Margin.ToString()));
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
        }
    }
}
