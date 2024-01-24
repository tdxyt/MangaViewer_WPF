using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace MangaViewer_WPF
{
    internal class MainViewModel
    {
        public BitmapImage Img_src { get; set; }
        public Rect Img_rect { get; set; }
        public ScaleTransform Img_scale { get; set; }
        public Thickness Img_margin { get; set; }
        public string Tips_text { get; set; }
        public Visibility Tips_visable { get; set; }
        public MainViewModel()
        {
            Tips_visable = Visibility.Hidden;
        }
    }
}
