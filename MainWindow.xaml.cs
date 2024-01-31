using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Xml.Linq;
using WpfScreenHelper;
using static System.Net.Mime.MediaTypeNames;

namespace MangaViewer_WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage m_currentImg;
        private List<string> m_fileList = new List<string>();
        private Int32Rect m_crop_rect;
        private double m_scale = 1;
        private int m_index;
        private string m_folderPath;
        private bool m_isReposMode = true;
        private int m_zoomRatio = 100;
        private readonly List<string> EXT_NAMES = new List<string> { ".gif", ".jpg", ".bmp", ".png", ".jpeg", ".tif" , ".webp"};
        private readonly List<int> ZOOM_L = new List<int>{ 5,6,7,8,10,12,14,17,20,24,29,35,42,50,60,72,85,100,120,145,175,210,250
            ,300,360,430,520,620,750,900,1100,1300,1600};
        private readonly int HORIZONTAL_DELTA = 120;
        private readonly int VERTICAL_DELTA = 120;
        private DispatcherTimer m_timer;        
        private bool m_isJumpingTo = false;        
        private int m_jumpNum;
        private double m_showingTime = 0;
        private List<string> m_badFileNames = new List<string>();
        private bool m_dragFlag = false;
        private bool m_dragging = false;
        private Point m_dragFrom;
        private Rect m_screenRect;
        private bool m_isGif =false;
        public MainWindow(string[] args)
        {
            InitializeComponent();
            string sFile;
            if (args.Length == 0)
            {
                OpenFileDialog openPicDialog = new OpenFileDialog();
                openPicDialog.Filter = "Image files|*"+EXT_NAMES.Aggregate((a,b) => a +";*" + b);
                if (openPicDialog.ShowDialog() != true)
                {
                    System.Windows.Application.Current.Shutdown();
                    return;
                }
                else
                {
                    sFile = openPicDialog.FileName;
                }
            }
            else
            {
                sFile = args[0];
            }
            Load_img_cache(sFile, true);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            m_timer = new DispatcherTimer();
            m_timer.Tick += DispatcherTimer_Tick;
            m_timer.Interval = TimeSpan.FromMilliseconds(100);

            Screen_refresh();
            Img_refresh(true);
        }

        private void Show_tips(string tips, double last_sec = 1)
        {
            Tips.Text = tips;
            Tips.Visibility = Visibility.Visible;
            m_showingTime = last_sec;
            m_timer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (m_showingTime > 0)
            {
                m_showingTime -= 0.1;                                    
            }
            if (m_showingTime <= 0)
            {
                Tips.Visibility = Visibility.Hidden;
                m_timer.Stop();
            }
        }

        // from https://stackoverflow.com/questions/20848861/wpf-some-images-are-rotated-when-loaded
        private readonly string _orientationQuery = "System.Photo.Orientation";
        private BitmapImage LoadImageFile(String path)
        {
            Rotation rotation = Rotation.Rotate0;
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                BitmapFrame bitmapFrame = BitmapFrame.Create(fileStream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                BitmapMetadata bitmapMetadata = bitmapFrame.Metadata as BitmapMetadata;

                if ((bitmapMetadata != null) && (bitmapMetadata.ContainsQuery(_orientationQuery)))
                {
                    object o = bitmapMetadata.GetQuery(_orientationQuery);

                    if (o != null)
                    {
                        switch ((ushort)o)
                        {
                            case 6:
                                {
                                    rotation = Rotation.Rotate90;
                                }
                                break;
                            case 3:
                                {
                                    rotation = Rotation.Rotate180;
                                }
                                break;
                            case 8:
                                {
                                    rotation = Rotation.Rotate270;
                                }
                                break;
                        }
                    }
                }
            }

            BitmapImage _image = new BitmapImage();
            _image.BeginInit();
            _image.CacheOption = BitmapCacheOption.OnLoad;
            _image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            _image.UriSource = new Uri(path);
            _image.Rotation = rotation;
            _image.EndInit();
            _image.Freeze();

            return _image;
        }

        private bool Load_img_cache(string path = null, bool first_run = false)
        {
            if (path == null) path = m_folderPath + m_fileList[m_index];

            try
            {
                if (path.ToLower().EndsWith("gif"))
                {
                    m_isGif = true;
                    Img.Visibility = Visibility.Hidden;
                    Gif.Visibility = Visibility.Visible;
                    Gif.Source = new Uri(path);
                }
                else
                {
                    Img.Visibility = Visibility.Visible;
                    Gif.Stop();
                    Gif.Source = null;
                    Gif.Visibility = Visibility.Hidden;                    
                    m_isGif = false;
                    m_currentImg = LoadImageFile(path);
                }
                if (first_run) m_folderPath = path.Substring(0, path.LastIndexOf('\\') + 1);
                m_fileList = new List<string>();
                DirectoryInfo curFolder = new DirectoryInfo(m_folderPath);
                foreach (FileInfo f in curFolder.GetFiles())
                {
                    string lowerCaseExt = f.Extension.ToLower();
                    if (EXT_NAMES.Contains(lowerCaseExt) && !m_badFileNames.Contains(f.Name))
                    {
                        m_fileList.Add(f.Name);
                    }
                }
                m_fileList.Sort();
                m_index = m_fileList.IndexOf(path.Substring(path.LastIndexOf('\\') + 1));             
                
            }
            catch (Exception)
            {
                
                if (first_run)
                {   
                    MessageBox.Show("Not supported file type.", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    System.Windows.Application.Current.Shutdown();                    
                }
                else if (m_fileList.Count <= 1)
                {
                    MessageBox.Show("No valid file.", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    System.Windows.Application.Current.Shutdown();
                }
                else
                {
                    Show_tips(string.Format("Fail to load \"{0}\"", m_fileList[m_index]),2);
                    m_badFileNames.Add(m_fileList[m_index]);
                }
                return false;
            }
            return true;
        }
        private Int32Rect New_legal_clip(Int32Rect new_rect)
        {
            return New_legal_clip(new_rect.X,new_rect.Y,new_rect.Width,new_rect.Height);
        }
        private Int32Rect New_legal_clip(int left, int top, int width, int height)
        {   if (width > m_currentImg.PixelWidth)
            {
                left = 0;
                width = m_currentImg.PixelWidth;
            }
            else if (left < 0) left = 0;
            else if (left + width > m_currentImg.PixelWidth) left = (int)m_currentImg.PixelWidth - width;
            if (height > m_currentImg.PixelHeight)
            {
                top = 0;
                height = m_currentImg.PixelHeight;
            }
            else if (top < 0) top = 0;
            else if (top + height > m_currentImg.PixelHeight) top = (int)m_currentImg.PixelHeight - height ;
            return new Int32Rect(left, top, width, height);
        }
        private void Img_refresh(bool replace)
        {
            if(m_isGif) return;
            PresentationSource source = PresentationSource.FromVisual(this);
            double sys_dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            double sys_dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            double dpi_fixX = m_currentImg.DpiX / sys_dpiX;
            double dpi_fixY = m_currentImg.DpiY / sys_dpiY;
            if (Properties.Settings.Default.view_model != 2)
            {
                m_scale = 1;
                if (m_screenRect.Width >= m_currentImg.PixelWidth && m_screenRect.Height >= m_currentImg.PixelHeight)
                {
                    m_crop_rect = new Int32Rect(0, 0, m_currentImg.PixelWidth, m_currentImg.PixelHeight);    
                }
                else if (Properties.Settings.Default.view_model == 0) // width first
                {
                    if (m_screenRect.Width < m_currentImg.PixelWidth)
                    {
                        m_scale = m_screenRect.Width / m_currentImg.PixelWidth;
                    }
                    if (replace)
                    {
                        m_crop_rect = New_legal_clip(0, 0, m_currentImg.PixelWidth, (int)(m_screenRect.Height / m_scale));
                    }
                    else
                    {
                        m_crop_rect = New_legal_clip(0, m_crop_rect.Y + m_crop_rect.Height / 2 - (int)(m_screenRect.Height / 2 / m_scale), m_currentImg.PixelWidth, (int)(m_screenRect.Height / m_scale));
                    }
                }                
                else // height first
                {
                    if (m_screenRect.Height < m_currentImg.PixelHeight)
                    {
                        m_scale = m_screenRect.Height / m_currentImg.PixelHeight;

                    }
                    if (replace)
                    {
                        m_crop_rect = New_legal_clip(0, 0, (int)(m_screenRect.Width / m_scale), m_currentImg.PixelHeight);
                    }
                    else
                    {
                        m_crop_rect = New_legal_clip(m_crop_rect.X + m_crop_rect.Width / 2 - (int)(m_screenRect.Width / 2 / m_scale), 0, (int)(m_screenRect.Width / m_scale), m_currentImg.PixelHeight);
                    }                    
                }
                Img.LayoutTransform = new ScaleTransform(m_scale * dpi_fixX, m_scale * dpi_fixY);
                Img.Source = new CroppedBitmap(m_currentImg, m_crop_rect);                
            }
            else // TODO:free-zoom
            {
                int centre_x,centre_y;
                if (replace)
                {
                    m_zoomRatio = 100;
                    centre_x = m_currentImg.PixelWidth / 2;
                    centre_y = m_currentImg.PixelHeight / 2;
                }
                else
                {
                    centre_x = m_crop_rect.X + m_crop_rect.Width / 2;
                    centre_y = m_crop_rect.Y + m_crop_rect.Height / 2;
                }
                m_scale = (double)m_zoomRatio / 100;
                m_crop_rect = New_legal_clip(centre_x-(int)(m_screenRect.Width / 2 / m_scale),
                                            centre_y - (int)(m_screenRect.Height / 2 / m_scale),
                                            (int)(m_screenRect.Width / m_scale),
                                            (int)(m_screenRect.Height / m_scale));
                Img.LayoutTransform = new ScaleTransform(m_scale * dpi_fixX, m_scale * dpi_fixY);                
                Img.Source = new CroppedBitmap(m_currentImg, m_crop_rect);
            }
        }

        private void Screen_refresh()
        {
            int max_s = Screen.AllScreens.Count();
            Properties.Settings.Default.screen_index = Properties.Settings.Default.screen_index % max_s;
            
            m_screenRect = Screen.AllScreens.ToList()[Properties.Settings.Default.screen_index].Bounds;
            this.Left = m_screenRect.Left;
            this.Top = m_screenRect.Top;
            this.Width = m_screenRect.Width;
            this.Height = m_screenRect.Height;
        }

        private void Model_switch(bool isFirstMode)
        {
            if (m_isGif) return;
            if (isFirstMode)
            {
                Properties.Settings.Default.view_model = (Properties.Settings.Default.view_model + 1) % 3;
                m_zoomRatio = 100;
                Img_refresh(false);
                if (Properties.Settings.Default.view_model == 2)
                {                    
                    Show_tips("Free-Zoom");
                }
                else
                {
                    if (Properties.Settings.Default.view_model == 0)
                        Show_tips("Width-Prior");
                    else
                        Show_tips("Height-Prior");
                }
            }
            else
            {
                m_isReposMode = !m_isReposMode;
                if (m_isReposMode)
                    Show_tips("Switch-Replace");
                else
                    Show_tips("Non-Replace");
            }
        }
        private void Show_info()
        {
            Show_tips(m_fileList[m_index] + "  (" + (m_index + 1).ToString() + "/" + m_fileList.Count.ToString() + " " + m_zoomRatio.ToString() + "%" + ") ");
        }

        private bool Try_set_new_img_index(int new_index)
        {
            int old_index = m_index;
            m_index = new_index;
            if (!Load_img_cache())
            {
                m_fileList.RemoveAt(m_index);
                if (m_index > old_index)
                    m_index = old_index;
                else
                    m_index = old_index - 1;
                return false;
            }
            Img_refresh(m_isReposMode);
            return true;
        }
        private void Img_switch(int direction)
        {
            // for jumping and mouse click
            if (m_isJumpingTo) m_isJumpingTo = false;

            int res = m_index + direction;
            if (Try_set_new_img_index((m_index + direction + m_fileList.Count) % m_fileList.Count))
            {
                if (res < 0)
                {
                    Show_tips("Cross the front");
                }
                else if (res >= m_fileList.Count)
                {
                    Show_tips("Cross the end");
                }
            }
            
        }
        private void debug_print()
        {
            Trace.WriteLine(string.Format("Img: x:{0}, y:{1}, width:{2}, height:{3}", Img.Margin.Left, Img.Margin.Top,Img.Width,Img.Height));
            Trace.WriteLine(string.Format("Img_Clip: x:{0}, y:{1}, width:{2}, height:{3}", m_crop_rect.X, m_crop_rect.Y, m_crop_rect.Width, m_crop_rect.Height));
            //Trace.WriteLine(string.Format("Sys: {0},{1}; Img: {2},{3}", sys_dpiX, sys_dpiY, m_currentImg.DpiX, m_currentImg.DpiY));
        }

        private void Vertical_move(double distance)
        {
            if (!m_isGif && m_currentImg.PixelHeight >= m_crop_rect.Height)
            {
                m_crop_rect.Y += (int)(Math.Max(1.0, m_scale) * distance);
                m_crop_rect = New_legal_clip(m_crop_rect);
                Img.Source = new CroppedBitmap(m_currentImg, m_crop_rect);
            }
        }
        private void Horizontal_move(double distance)
        {
            if (!m_isGif && m_currentImg.PixelWidth >= m_crop_rect.Width)
            {
                m_crop_rect.X += (int)(Math.Max(1.0, m_scale) * distance);
                m_crop_rect = New_legal_clip(m_crop_rect);
                Img.Source = new CroppedBitmap(m_currentImg, m_crop_rect);
            }
        }


        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_dragFlag)
            {
                Vector offSet = m_dragFrom - e.GetPosition(this);
                m_dragFrom = e.GetPosition(this);
                m_crop_rect.X += (int)(offSet.X / m_scale);
                m_crop_rect.Y += (int)(offSet.Y / m_scale);
                m_crop_rect = New_legal_clip(m_crop_rect);
                Img.Source = new CroppedBitmap(m_currentImg, m_crop_rect);
                m_dragging = true;
                Cursor = Cursors.ScrollAll;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            m_showingTime = 0;
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
            {
                System.Windows.Application.Current.Shutdown();
                return;
            }
            if (!m_isGif && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Pressed
                 && (m_currentImg.Width * m_scale > m_screenRect.Width || m_currentImg.Height * m_scale > m_screenRect.Height))
            {
                m_dragFrom = e.GetPosition(this);
                m_dragFlag = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            m_showingTime = 0;
        }

        private void Tips_MouseDown(object sender, MouseButtonEventArgs e)
        {
            m_showingTime = 0;
        }

        private bool Set_app_path()
        {
            OpenFileDialog openPicDialog = new OpenFileDialog();
            openPicDialog.Filter = "Exacutable|*.exe";
            if (openPicDialog.ShowDialog() != true) return false;
            else Properties.Settings.Default.app_path = openPicDialog.FileName;
            return true;
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (m_isJumpingTo)
            {
                //(int)Keys.D0 = 48, D9 = 57 
                int key = (int)e.Key - (int)Key.D0;
                if (0 <= key && key <= 9)
                {
                    m_jumpNum = m_jumpNum * 10 + key;
                    if (m_jumpNum > m_fileList.Count)
                    {
                        m_isJumpingTo = false;
                        m_jumpNum = m_fileList.Count;
                    }
                }
                else m_isJumpingTo = false;
                if (!m_isJumpingTo)
                {
                    if (m_jumpNum > 0 && Try_set_new_img_index(m_jumpNum - 1))
                    {
                        Show_info();
                    }
                    return;
                }
                else Show_tips("To: " + m_jumpNum.ToString() + "/" + m_fileList.Count.ToString(),3);
                return;
            }
            switch (e.Key)
            {
                case Key.Escape:
                    System.Windows.Application.Current.Shutdown();
                    return;
                case Key.F1:       
                    Show_tips(

@"   HELP:
Hold Mouse_Right to drag.
Key_Right = Mouse_Left => Next image
Key_Left = Mouse_Right => Previous image
Key_E => Show image info
Key_H => Minimize window
Key_J + Number => Jumping to image
Key_A = Mouse_X1 => Switch Width-Prior/Height-Prior/Free-Zoom mode
Key_S = Mouse_X2 => Switch Switch-Replace/Non-Replace mode
Key_N => Switch Screen
Key_Esc = Mouse_MiddleDoubleclick => Quit
Key_Up/Down = Mouse_Wheel => Move image from mode
  On Free-Zoom mode:
Key_Q/W => Move image left/right
Key_Z/X = Mouse_Wheel => Zoom in/out", 10);
                    break;                    
                case Key.H:
                    WindowState = WindowState.Minimized;
                    break;
                case Key.E:
                    Show_info();
                    break;
                case Key.A:
                    Model_switch(true);
                    break;
                case Key.S:
                    Model_switch(false);
                    break;
                case Key.J:
                    Show_tips("Jumping...");
                    m_jumpNum = 0;
                    m_isJumpingTo = true;
                    break;
                case Key.N:
                    // Switch screen
                    int max_s = Screen.AllScreens.Count();
                    Properties.Settings.Default.screen_index = (Properties.Settings.Default.screen_index + 1) % max_s;
                    Screen_refresh();
                    Img_refresh(false);
                    break;
                case Key.Up:
                    if (Properties.Settings.Default.view_model == 1)
                        Horizontal_move(-HORIZONTAL_DELTA);
                    else Vertical_move(-VERTICAL_DELTA);
                    break;
                case Key.Down:
                    if (Properties.Settings.Default.view_model == 1)
                        Horizontal_move(HORIZONTAL_DELTA);
                    else Vertical_move(VERTICAL_DELTA);
                    break;
                case Key.Left:
                    Img_switch(-1);
                    break;
                case Key.Right:
                    Img_switch(1);
                    break;
                case Key.F3:
                    if (Properties.Settings.Default.app_path != string.Empty) Set_app_path();
                    break;
                case Key.F4:
                    if (Properties.Settings.Default.app_path == string.Empty && !Set_app_path())
                        return;                        
                    else
                    {
                        // open with another app
                        Process ps = new Process();
                        ps.StartInfo.FileName = Properties.Settings.Default.app_path;
                        ps.StartInfo.Arguments = m_folderPath + m_fileList[m_index];
                        ps.StartInfo.CreateNoWindow = false;
                        ps.StartInfo.UseShellExecute = false;
                        ps.Start();
                    }                        
                    break;
                default:                   
                    if (Properties.Settings.Default.view_model == 2)
                    {
                        if (e.Key == Key.Q)
                        {
                            Horizontal_move(-HORIZONTAL_DELTA);
                        }
                        else if (e.Key == Key.W)
                        {
                            Horizontal_move(HORIZONTAL_DELTA);
                        }
                        else if (e.Key == Key.Z)
                        {
                            Zoom(true);
                        }
                        else if (e.Key == Key.X)
                        {
                            Zoom(false);
                        }
                    }                        
                    break;
            }
        }


        private void Zoom(bool zoomIn = true)
        {
            int i = ZOOM_L.IndexOf(m_zoomRatio);
            if (zoomIn)
            {
                i++;
                if (i >= ZOOM_L.Count) i = ZOOM_L.Count - 1;
            }
            else
            {
                i--;
                if (i < 0) i = 0;
            }
            m_zoomRatio = ZOOM_L[i];
            Img_refresh(false);
            Show_tips(m_zoomRatio.ToString() + "%");
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Released)
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        Img_switch(1);
                        break;
                    case MouseButton.Right:
                        if (!m_dragging) Img_switch(-1);
                        break;
                    case MouseButton.XButton1:
                        Model_switch(true);
                        break;
                    case MouseButton.XButton2:
                        Model_switch(false);
                        break;
                    default: break;
                }
            }
            if (m_dragFlag) 
            {
                Cursor = Cursors.Arrow;
                m_dragFlag = false; 
                m_dragging = false;
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (m_isGif) return;
            switch (Properties.Settings.Default.view_model)
            {
                case 0:
                    Vertical_move(-e.Delta);
                    break;
                case 1:
                    Horizontal_move(-e.Delta);
                    break;
                case 2:
                    if (e.Delta > 0) Zoom(true);
                    else Zoom(false);
                    break;
                default : break;
            }
        }

        private void Gif_MediaEnded(object sender, RoutedEventArgs e)
        {
            Gif.Position = new TimeSpan(0, 0, 1);
            Gif.Play();
        }
    }
}
