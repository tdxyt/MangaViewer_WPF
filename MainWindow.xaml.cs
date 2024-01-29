﻿using Microsoft.Win32;
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

namespace MangaViewer_WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage m_currentImg;
        private List<string> m_fileList = new List<string>();
        private Rect clip_rect = new Rect(0, 0, 0, 0);
        private int m_index;
        private string m_folderPath;
        private bool m_isReposMode = true;
        private int m_zoomRatio = 100;
        private readonly List<string> EXT_NAMES = new List<string> { ".gif", ".jpg", ".bmp", ".png", ".jpeg", ".tif" };
        private const int HORIZONTAL_DELTA = 100;
        private const int VERTICAL_DELTA = 100;
        private DispatcherTimer timer;        
        private bool m_isJumpingTo = false;        
        private int m_jumpNum;
        private double m_showing_time = 0;
        private List<string> bad_file_names = new List<string>();
        private bool m_dragMidFlag = false;

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
                    Application.Current.Shutdown();
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

        
        private void show_tips(string tips, double last_sec = 1)
        {
            Tips.Text = tips;
            Tips.Visibility = Visibility.Visible;
            m_showing_time = last_sec;
            timer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (m_showing_time > 0)
            {
                m_showing_time -= 0.1;                                    
            }
            if (m_showing_time <= 0)
            {
                Tips.Visibility = Visibility.Hidden;
                timer.Stop();
            }
        }



        private bool Load_img_cache(string path = null, bool first_run = false)
        {
            if (path == null) path = m_folderPath + m_fileList[m_index];
            try
            {
                m_currentImg = new BitmapImage();
                m_currentImg.BeginInit();
                m_currentImg.CacheOption = BitmapCacheOption.OnLoad;
                m_currentImg.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                m_currentImg.UriSource = new Uri(path);
                m_currentImg.EndInit();
                if (first_run) m_folderPath = path.Substring(0, path.LastIndexOf('\\') + 1);
                m_fileList = new List<string>();
                DirectoryInfo curFolder = new DirectoryInfo(m_folderPath);
                foreach (FileInfo f in curFolder.GetFiles())
                {
                    string lowerCaseExt = f.Extension.ToLower();
                    if (EXT_NAMES.Contains(lowerCaseExt) && !bad_file_names.Contains(f.Name))
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
                    Application.Current.Shutdown();                    
                }
                else if (m_fileList.Count <= 1)
                {
                    MessageBox.Show("No valid file.", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    Application.Current.Shutdown();
                }
                else
                {
                    show_tips(string.Format("Fail to load \"{0}\"", m_fileList[m_index]),2);
                    bad_file_names.Add(m_fileList[m_index]);
                }
                return false;
            }
            Img.Source = m_currentImg;
            return true;
        }

        private void screen_refresh()
        {
            int max_s = Screen.AllScreens.Count();
            Properties.Settings.Default.screen_index = Properties.Settings.Default.screen_index % max_s;
            
            Rect scr_rect = Screen.AllScreens.ToList()[Properties.Settings.Default.screen_index].Bounds;
            this.Left = scr_rect.Left;
            this.Top = scr_rect.Top;
            this.Width = scr_rect.Width;
            this.Height = scr_rect.Height;
        }
        private void img_refresh()
        {
            Img.Source = m_currentImg;
            Img.Clip = new RectangleGeometry(new Rect(0, 0, 800, 800));
            Img.RenderTransform = new ScaleTransform(1, 1);
            Img.Margin = new Thickness((Width - 800) / 2, (Height - 800) / 2, 0, 0);
        }
        private void modelSwitch(bool isFirstMode)
        {
            if (isFirstMode)
            {
                Properties.Settings.Default.view_model = (Properties.Settings.Default.view_model + 1) % 3;
                if (Properties.Settings.Default.view_model == 0)
                    clip_rect.Offset(new Vector(-clip_rect.Left,0));
                else if (Properties.Settings.Default.view_model == 1)
                    clip_rect.Offset(new Vector(0, -clip_rect.Top));
                img_refresh();
                if (Properties.Settings.Default.view_model == 2)
                    show_tips("Free-Zoom");
                else if (Properties.Settings.Default.view_model == 0)
                    show_tips("Width-Prior");
                else
                    show_tips("Height-Prior");
            }
            else
            {
                m_isReposMode = !m_isReposMode;
                if (m_isReposMode)
                    show_tips("Switch-Replace");
                else
                    show_tips("Non-Replace");
            }
        }
        private void show_info()
        {
            show_tips(m_fileList[m_index] + "  (" + (m_index + 1).ToString() + "/" + m_fileList.Count.ToString() + " " + m_zoomRatio.ToString() + "%" + ") ");
        }

        private bool try_set_new_img_index(int new_index)
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
            img_refresh();
            return true;
        }
        private void imgSwitch(int direction)
        {
            // for jumping and mouse click
            if (m_isJumpingTo) m_isJumpingTo = false;

            int res = m_index + direction;
            if (try_set_new_img_index((m_index + direction + m_fileList.Count) % m_fileList.Count))
            {
                if (res < 0)
                {
                    show_tips("Cross the front");
                }
                else if (res >= m_fileList.Count)
                {
                    show_tips("Cross the end");
                }
            }
            
        }
        private void verticalMove(double distance)
        {
            // TODO
            double newTop = clip_rect.Top + distance;
            if (newTop > 0) newTop = 0;
            else if (newTop + clip_rect.Height < ActualHeight) newTop = Height - clip_rect.Height;
                return;
                //mainViewPicBox.Top = newTop;
        }
        private void horizontalMove(double distance)
        {
            // TODO
            double newLeft = clip_rect.Left + distance;
            if (newLeft > 0) newLeft = 0;
            else if (newLeft + clip_rect.Width < Width) newLeft = Width - clip_rect.Width;
                return; // mainViewPicBox.Left = newLeft;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Tick += dispatcherTimer_Tick;
            timer.Interval = TimeSpan.FromMilliseconds(100);

            screen_refresh();
            img_refresh();            
            //Trace.WriteLine(string.Empty == "");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
            {
                Application.Current.Shutdown();
                return;
            }
            // drag event
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            m_showing_time = 0;
        }

        private void Tips_MouseDown(object sender, MouseButtonEventArgs e)
        {
            m_showing_time = 0;
        }

        private bool set_app_path()
        {
            OpenFileDialog openPicDialog = new OpenFileDialog();
            openPicDialog.Filter = "Exacutable|*.exe";
            if (openPicDialog.ShowDialog() != true)
            {
                return false;
            }
            else
            {
                Properties.Settings.Default.app_path = openPicDialog.FileName;
            }
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
                    if (m_jumpNum > 0 && try_set_new_img_index(m_jumpNum - 1))
                    {
                        show_info();
                    }
                    return;
                }
                else show_tips(m_jumpNum.ToString() + "/" + m_fileList.Count.ToString(),3);
                return;
            }
            switch (e.Key)
            {
                case Key.Escape:
                    Application.Current.Shutdown();
                    return;
                case Key.F1:       
                    show_tips(

@"   HELP:
Hold Mouse_Middle to drag.
Key_Right = Mouse_Left => Next image
Key_Left = Mouse_Right => Previous image
Key_E => Show image info
Key_H => Minimize window
Key_J + Number => Jumping to image
Key_A = Mouse_X1 => Switch Width-Prior/Height-Prior/Free-Zoom mode
Key_S = Mouse_X2 => Switch Switch-Replace/Non-Replace mode
Key_N => Switch Screen
Key_Esc = Mouse_MiddleDoubleclick => Quit
Key_Up/Down = Mouse_wheel(Width-Prior) => Move image up/down
On Free-Zoom mode:
Key_Q/W => Move image left/right
Key_Z/X = Mouse_Wheel => Zoom in/out", 10);
                    break;                    
                case Key.H:
                    WindowState = WindowState.Minimized;
                    break;
                case Key.E:
                    show_info();
                    break;
                case Key.A:
                    modelSwitch(true);
                    break;
                case Key.S:
                    modelSwitch(false);
                    break;
                case Key.J:
                    show_tips("Jumping...");
                    m_jumpNum = 0;
                    m_isJumpingTo = true;
                    break;
                case Key.N:
                    // Switch screen
                    int max_s = Screen.AllScreens.Count();
                    Properties.Settings.Default.screen_index = (Properties.Settings.Default.screen_index + 1) % max_s;
                    screen_refresh();
                    img_refresh();
                    break;
                case Key.Up:
                    verticalMove(VERTICAL_DELTA);
                    break;
                case Key.Down:
                    verticalMove(-VERTICAL_DELTA);
                    break;
                case Key.Left:
                    imgSwitch(-1);
                    break;
                case Key.Right:
                    imgSwitch(1);
                    break;
                case Key.F3:
                    if (Properties.Settings.Default.app_path != string.Empty) set_app_path();
                    break;
                case Key.F4:
                    if (Properties.Settings.Default.app_path == string.Empty && !set_app_path())
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
                            horizontalMove(HORIZONTAL_DELTA);
                        }
                        else if (e.Key == Key.W)
                        {
                            horizontalMove(-HORIZONTAL_DELTA);
                        }
                        else if (e.Key == Key.Z)
                        {
                            zoom(true);
                        }
                        else if (e.Key == Key.X)
                        {
                            zoom(false);
                        }
                    }                        
                    break;
            }
        }

        private readonly List<int> ZOOM_L = new List<int>{ 5,6,7,8,10,12,14,17,20,24,29,35,42,50,60,72,85,100,120,145,175,210,250
        ,300,360,430,520,620,750,900,1100,1300,1600};
        private void zoom(bool zoomIn = true)
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
            img_refresh();
            show_tips(m_zoomRatio.ToString() + "%");
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        imgSwitch(1);
                        break;
                    case MouseButton.Right:
                        imgSwitch(-1);
                        break;
                    case MouseButton.XButton1:
                        modelSwitch(true);
                        break;
                    case MouseButton.XButton2:
                        modelSwitch(false);
                        break;
                    default: break;
                }
            }
            if (m_dragMidFlag)
            {
                m_dragMidFlag = false;
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            switch (Properties.Settings.Default.view_model)
            {
                case 0:
                    verticalMove(e.Delta);
                    break;
                case 1:
                    horizontalMove(e.Delta);
                    break;
                case 2:
                    if (e.Delta > 0) zoom(true);
                    else zoom(false);
                    break;
                default : break;
            }
        }
    }
}
