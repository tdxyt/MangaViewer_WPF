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

namespace MangaViewer_WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage m_currentImg;
        MainViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();
            m_currentImg = new BitmapImage();
            m_currentImg.BeginInit();
            m_currentImg.CacheOption = BitmapCacheOption.OnLoad;
            m_currentImg.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            m_currentImg.UriSource = new Uri("d:/e.png");
            m_currentImg.EndInit();

            viewModel = new MainViewModel();
            DataContext = viewModel;
            viewModel.Img_src = m_currentImg;
            viewModel.Img_rect = new Rect(0, 0, 800, 800);
            viewModel.Img_scale = new ScaleTransform(1, 1);

            viewModel.Tips_text = "SOMETHING BULABULA...";
            viewModel.Tips_visable = Visibility.Visible;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            viewModel.Img_margin = new Thickness((ActualWidth - 800) / 2, (ActualHeight - 800) / 2, 0, 0);
        }
    }
}
