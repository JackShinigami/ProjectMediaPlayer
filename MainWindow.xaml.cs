﻿using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ProjectMediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Import các hàm từ user32.dll
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainWindow()
        {
            InitializeComponent();
            // Đăng ký các phím tắt
            RegisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID_PAUSE_PLAY, 0, VK_MEDIA_PLAY_PAUSE);
            RegisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID_SKIP, 0, VK_MEDIA_NEXT_TRACK);


            // Thêm hook để xử lý sự kiện WM_HOTKEY
            ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
        }

        private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message == 0x0312) // WM_HOTKEY
            {
                int hotkeyId = msg.wParam.ToInt32();
                switch (hotkeyId)
                {
                    case HOTKEY_ID_PAUSE_PLAY:
                        PlayPauseToggleButton.IsChecked = !PlayPauseToggleButton.IsChecked;
                        break;
                    case HOTKEY_ID_SKIP:
                        NextMediaFile();
                        break;
                }
            }
        }

        List<string> mediaFileNames = new List<string>(); // Danh sách các file được chọn
        int currentMediaIndex = 0; // Chỉ số của file đang được chọn
        bool isPaused = false;
        bool isShuffle = false;
        bool isChangeByAuto = false;
        bool isRepeatOne = false;
        bool isMuted = false;
        bool isLoading = false;
        private const int HOTKEY_ID_PAUSE_PLAY = 9000;
        private const int HOTKEY_ID_SKIP = 9001;


        //hotkey
        private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const int VK_MEDIA_NEXT_TRACK = 0xB0;



        DispatcherTimer timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(0.1)
        };

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            List<PlayList> playLists = DataManager.Instance.GetAllPlayList();
            listBox_PlayList.ItemsSource = playLists;
            listBox_Songs.ItemsSource = mediaFileNames;
            // Remove this code
            //if (playLists.Count > 0)
            //{
            //    listBox_PlayList.SelectedIndex = 0;
            //}
            volumeSlider.Value = 0.5;
            isPaused = false;
            string lastPlayListID = DataManager.Instance.LastPlayListID();
            string lastSongFileName = DataManager.Instance.LastSongFileName();
            double lastSongPosition = DataManager.Instance.LastSongPosition();

            PlayList pl = playLists.Find(x => x.ID == lastPlayListID);
            // Remove this code
            if (pl != null)
            {
                listBox_PlayList.SelectedItem = pl;
                mediaFileNames = pl.ListFileName;
                listBox_Songs.ItemsSource = mediaFileNames;

                if (mediaFileNames.Count > 0)
                {
                    listBox_Songs.SelectedIndex = mediaFileNames.FindIndex(x => x == lastSongFileName);
                    currentMediaIndex = listBox_Songs.SelectedIndex;
                    if (currentMediaIndex == -1)
                    {
                        currentMediaIndex = 0;
                        listBox_Songs.SelectedIndex = 0;
                    }

                    UpdateUI(mediaFileNames[currentMediaIndex]);
                    slider.Value = lastSongPosition;
                }
            }

            timer.Tick += (s, e) =>
            {
                if (mediaElement.NaturalDuration.HasTimeSpan)
                {
                    // Update the slider value
                    isChangeByAuto = true;
                    if (slider.Value == mediaElement.Position.TotalSeconds)
                    {
                        loadingProgressBar.Visibility = Visibility.Visible;
                        isLoading = true;
                        mediaElement.IsMuted = true;
                    }
                    else
                    {
                        if(isLoading)
                        {
                            mediaElement.IsMuted = false;
                            isLoading = false;
                        }
                        loadingProgressBar.Visibility = Visibility.Hidden;
                    }
                    slider.Value = mediaElement.Position.TotalSeconds;
                }
            };
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            PlayList playList = listBox_PlayList.SelectedItem as PlayList;
            string fileName = listBox_Songs.SelectedItem as string;

            if (playList != null && fileName != null)
            {
                DataManager dataManager = DataManager.Instance;
                dataManager.UpdateLastPlayListID(playList.ID);
                dataManager.UpdateLastSongFileName(fileName);
                dataManager.UpdateLastSongPosition(mediaElement.Position.TotalSeconds);
            }

            // Hủy đăng ký các phím tắt
            UnregisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID_PAUSE_PLAY);
            UnregisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID_SKIP);
        }

        public BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        private void UpdateUI(String fileName)
        {
            try
            {
                mediaElement.Source = new Uri(fileName);
                var file = TagLib.File.Create(fileName);
                txtName.Text = file.Tag.Title;
                txtSinger.Text = file.Tag.FirstPerformer;
                if (file.Tag.Pictures.Length >= 1)
                {
                    var bin = (byte[])(file.Tag.Pictures[0].Data.Data);
                    imgBg_Media.Source = LoadImage(bin);
                }
                else
                {
                    imgBg_Media.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Music.png"));
                } 
                    
                mediaElement.Play();
                timer.Start();
                slider.Value = 0;
            }
            catch (Exception e)
            {
                MessageBox.Show("File không hợp lệ");
                btnRemoveSong_Click(null, null);
            }

 
        }

        private List<string> SelectMediaFiles()
        {
            var fileDialog = new OpenFileDialog
            {
                Multiselect = true, // Cho phép chọn nhiều file
                Filter = "Media files (*.mp3, *.mp4)|*.mp3;*.mp4|All files (*.*)|*.*" // Chỉ cho phép chọn file mp3 và mp4
            };

            if (fileDialog.ShowDialog() == true)
            {
                return fileDialog.FileNames.ToList(); // Trả về danh sách các file được chọn
            }

            return new List<string>(); // Trả về danh sách rỗng nếu không có file nào được chọn
        }


        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
           
            double toltalTime = slider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            SecondToTimeConverter secondToTimeConverter = new SecondToTimeConverter();
            txtTotalTime.Text = secondToTimeConverter.Convert(toltalTime, null, null, CultureInfo.CurrentCulture).ToString();
            slider.IsEnabled = true;
            timer.Start();
        }
        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            slider.Value = 0;
            if (isRepeatOne)
            {
                mediaElement.Position = TimeSpan.FromSeconds(0);
                mediaElement.Play();
                return;
            }
            NextMediaFile();
        }

        private void mediaElement_BufferingStarted(object sender, RoutedEventArgs e)
        {
            loadingProgressBar.Visibility = Visibility.Visible;
        }

        private void mediaElement_BufferingEnded(object sender, RoutedEventArgs e)
        {
            loadingProgressBar.Visibility = Visibility.Hidden;
        }


        //private void btnStop_Click(object sender, RoutedEventArgs e)
        //{
        //    mediaElement.Stop();
        //    timer.Stop();
        //}

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isChangeByAuto)
            {
                isChangeByAuto = false;
                return;
            }
            
            mediaElement.Position = TimeSpan.FromSeconds(slider.Value);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NextMediaFile();
        }

        private void NextMediaFile()
        {
            if (mediaFileNames.Count == 0)
            {
                MessageBox.Show("Chưa chọn file");
                return;
            }
            if (isShuffle)
            {
                currentMediaIndex = GetNextRandomIndex();
            } else
            {
                if (currentMediaIndex == mediaFileNames.Count - 1)
                {
                    currentMediaIndex = 0;
                } else
                {
                    currentMediaIndex++;
                }
            }
            listBox_Songs.SelectedIndex = currentMediaIndex;
            mediaElement.Play();
            slider.Value = 0;
        }
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {

            if (mediaFileNames.Count == 0)
            {
                MessageBox.Show("Chưa chọn file");
                return;
            }

            if (currentMediaIndex == 0)
            {
                currentMediaIndex = mediaFileNames.Count - 1;
            } else
            {
                currentMediaIndex--;
            }
            listBox_Songs.SelectedIndex = currentMediaIndex;
            mediaElement.Play();
        }

        private int GetNextRandomIndex()
        {
            Random random = new Random();
            int index = random.Next(0, mediaFileNames.Count);
            return index;
        }

        private void listBoxSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox_Songs.SelectedIndex != -1)
            {
                currentMediaIndex = listBox_Songs.SelectedIndex;
                UpdateUI(mediaFileNames[currentMediaIndex]);
            }
        }

        private void btnAddPlayList_Click(object sender, RoutedEventArgs e)
        {
            mediaFileNames = SelectMediaFiles(); // Chọn file
            if (mediaFileNames.Count > 0)
            {
                //UpdateUI(mediaFileNames[0]); // Cập nhật giao diện
                PlayList playList = new PlayList(mediaFileNames, "New PlayList " + (DataManager.Instance.GetMaxID() + 1).ToString()); ;
                DataManager.Instance.AddPlayList(playList);
            }

            List<PlayList> playLists = DataManager.Instance.GetAllPlayList();
            listBox_PlayList.ItemsSource = playLists;
            listBox_PlayList.SelectedIndex = playLists.Count - 1;
            currentMediaIndex = 0;
            listBox_Songs.SelectedIndex = 0;
        }


        private void btnAddSong_Click(object sender, RoutedEventArgs e)
        {
            PlayList? playList = listBox_PlayList.SelectedItem as PlayList;
            int index = listBox_PlayList.SelectedIndex;
            if (playList != null)
            {
                List<string> newFiles = SelectMediaFiles();
                if (newFiles.Count > 0)
                {
                    foreach (string fileName in newFiles)
                    {
                        if (!playList.ListFileName.Contains(fileName))
                            playList.Add(fileName);
                    }
                    DataManager.Instance.UpdatePlayList(playList);
                    List<PlayList> playLists = DataManager.Instance.GetAllPlayList();
                    mediaFileNames = playLists[index].ListFileName;
                    listBox_PlayList.ItemsSource = playLists;
                    listBox_PlayList.SelectedIndex = index;

                    listBox_Songs.ItemsSource = playLists[index].ListFileName;
                    listBox_Songs.SelectedIndex = 0;

                }
            }
        }


        private void btnRemovePlayList_Click(object sender, RoutedEventArgs e)
        {
            PlayList? playList = listBox_PlayList.SelectedItem as PlayList;
            if (playList != null)
            {
                DataManager.Instance.RemovePlayList(playList);
                List<PlayList> playLists = DataManager.Instance.GetAllPlayList();
                listBox_PlayList.ItemsSource = playLists;
                if (playLists.Count > 0)
                {
                    listBox_PlayList.SelectedIndex = 0;

                    listBox_Songs.ItemsSource = playLists[0].ListFileName;

                    if (playLists[0].ListFileName.Count > 0)
                        listBox_Songs.SelectedIndex = -1;

                    mediaFileNames = playLists[0].ListFileName;
                } else
                {
                    listBox_Songs.ItemsSource = null;
                    mediaFileNames = new List<string>();
                }

                currentMediaIndex = 0;
            }
        }

        private void btnRemoveSong_Click(object sender, RoutedEventArgs e)
        {
            PlayList? playList = listBox_PlayList.SelectedItem as PlayList;
            int index = listBox_PlayList.SelectedIndex;
            if (playList != null)
            {
                if (listBox_Songs.SelectedIndex != -1)
                {
                    playList.RemoveAt(listBox_Songs.SelectedIndex);
                    DataManager.Instance.UpdatePlayList(playList);
                    List<PlayList> playLists = DataManager.Instance.GetAllPlayList();
                    mediaFileNames = playLists[index].ListFileName;
                    listBox_PlayList.ItemsSource = playLists;
                    listBox_PlayList.SelectedIndex = index;

                    listBox_Songs.ItemsSource = playLists[index].ListFileName;

                    if (mediaFileNames.Count > 0)
                    {
                        listBox_Songs.SelectedIndex = 0;
                    }
                }

                currentMediaIndex = -1;
            }
        }

        #region Play/pause 
        /// <summary>
        /// Checked = Pause
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayPauseToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            mediaElement.Pause();
            timer.Stop();
            isPaused = true;
        }

        /// <summary>
        /// Unchecked = Play
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayPauseToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {

            if (mediaFileNames.Count == 0)
            {
                MessageBox.Show("Chưa chọn file");
                return;
            }

            if (isPaused)
            {
                mediaElement.Play();
                timer.Start();
                isPaused = false;
                return;
            }

            mediaElement.Play();
        }
        #endregion

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            listBox_Songs.ItemsSource = null;

            listBox_PlayList.Visibility = Visibility.Visible;
            stackpanel_PlaylistsTitle.Visibility = Visibility.Visible;
            stackpanel_PlaylistButtons.Visibility = Visibility.Visible;

            listBox_Songs.Visibility = Visibility.Collapsed;
            stackpanel_SongsTitle.Visibility = Visibility.Collapsed;
            stackpanel_SongButtons.Visibility = Visibility.Collapsed;
        }

        private void btnPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            PlayList? playList = clickedButton.DataContext as PlayList;

            if (playList != null)
            {
                listBox_PlayList.SelectedItem = playList;
            }
        }

        private void btnPlaylist_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Button clickedButton = (Button)sender;
                PlayList playList = clickedButton.DataContext as PlayList;

                if (playList != null)
                {
                    mediaFileNames = playList.ListFileName;

                    listBox_Songs.ItemsSource = mediaFileNames;
                    listBox_Songs.SelectedIndex = 0;

                    listBox_PlayList.Visibility = Visibility.Collapsed;
                    stackpanel_PlaylistsTitle.Visibility = Visibility.Collapsed;
                    stackpanel_PlaylistButtons.Visibility = Visibility.Collapsed;

                    label_PlaylistName.Content = playList.Name;
                    listBox_Songs.Visibility = Visibility.Visible;
                    stackpanel_SongsTitle.Visibility = Visibility.Visible;
                    stackpanel_SongButtons.Visibility = Visibility.Visible;
                }
            }
            e.Handled = true;
        }

        private void RepeatToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            isRepeatOne = true;
        }

        private void RepeatToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            isRepeatOne = false;
        }

        private void ShuffleToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            isShuffle = ShuffleToggleButton.IsChecked!.Value;
        }

        private void ShuffleToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            isShuffle = ShuffleToggleButton.IsChecked!.Value;
        }

        /// <summary>
        /// Checked = Mute
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MuteToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            isMuted = mediaElement.IsMuted = MuteToggleButton.IsChecked!.Value;
        }

        /// <summary>
        /// Unchecked = Unmute
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MuteToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            isMuted = mediaElement.IsMuted = MuteToggleButton.IsChecked!.Value;
        }

        private void slider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // ghi nhận giá trị tương đối của chuột so với slider
            double mousePosition = e.GetPosition(slider).X;
            double ratio = mousePosition / slider.ActualWidth;

            // tính giá trị tương đối của slider
            double relativeValue = ratio * slider.Maximum;
            slider.Value = relativeValue;

        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // set volume
            mediaElement.Volume = volumeSlider.Value;

            // set mute
            if (volumeSlider.Value == 0)
            {
                MuteToggleButton.IsChecked = isMuted = true;
            } else
            {
                MuteToggleButton.IsChecked = isMuted = false;
            }
        }
        
        private void txtBoxRename_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox txtBoxRename = sender as TextBox;
                txtBoxRename.Visibility = Visibility.Collapsed;
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T)
                {
                    return (T)child;
                }

                T childItem = FindVisualChild<T>(child);
                if (childItem != null)
                {
                    return childItem;
                }
            }

            return null;
        }

        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while ((child != null) && !(child is T))
            {
                child = VisualTreeHelper.GetParent(child);
            }
            return child as T;
        }

        private void MenuItemRename_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu contextMenu = (ContextMenu)((MenuItem)sender).Parent;
            Button button = (Button)contextMenu.PlacementTarget;

            TextBox txtBoxRename = FindVisualChild<TextBox>(button.Parent as StackPanel);

            if (txtBoxRename != null)
            {
                txtBoxRename.Visibility = Visibility.Visible;
                button.Visibility = Visibility.Collapsed;
                txtBoxRename.Focus();
            }
        }

        private void txtBoxRename_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox? txtBoxRename = sender as TextBox;
            txtBoxRename.Visibility = Visibility.Collapsed;

            StackPanel stackPanel = FindVisualParent<StackPanel>(txtBoxRename);

            Button button = FindVisualChild<Button>(stackPanel);
            if (button != null)
            {
                button.Visibility = Visibility.Visible;
                PlayList? playlist = button.DataContext as PlayList;
                if (playlist != null)
                {
                    playlist.Name = txtBoxRename.Text;
                    DataManager.Instance.RemovePlayList(playlist);
                    DataManager.Instance.AddPlayList(playlist);
                }
            }
        }
    }
}