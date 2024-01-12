using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
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
                        if(isPaused)
                        {
                            btnPlay_Click(null, null);
                        }
                        else
                        {
                            btnPause_Click(null, null);
                        }
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
        private const int HOTKEY_ID_PAUSE_PLAY = 9000;
        private const int HOTKEY_ID_SKIP = 9001;
        

        //hotkey
        private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const int VK_MEDIA_NEXT_TRACK = 0xB0;
       


        DispatcherTimer timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            List<PlayList> playLists = DataManager.Instance.GetAllPlayList();
            listBox_PlayList.ItemsSource = playLists;
            listBox_Songs.ItemsSource = mediaFileNames;
            if (playLists.Count > 0)
            {
                listBox_PlayList.SelectedIndex = 0;
            }
            
            string lastPlayListID = DataManager.Instance.LastPlayListID();
            string lastSongFileName = DataManager.Instance.LastSongFileName();
            double lastSongPosition = DataManager.Instance.LastSongPosition();

            PlayList pl =  playLists.Find(x => x.ID == lastPlayListID);
            if(pl != null)
            {
                listBox_PlayList.SelectedItem = pl;
                mediaFileNames = pl.ListFileName;
                listBox_Songs.ItemsSource = mediaFileNames;
                if (mediaFileNames.Count > 0)
                {
                    listBox_Songs.SelectedIndex = mediaFileNames.FindIndex(x => x == lastSongFileName);
                    currentMediaIndex = listBox_Songs.SelectedIndex;
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
                        mediaElement.IsMuted = true;
                    }
                    else
                    {
                        mediaElement.IsMuted = false;
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
            slider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            slider.IsEnabled = true;
            timer.Start();
        }
        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            slider.IsEnabled = false;
            slider.Value = 0;
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

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if(mediaFileNames.Count == 0)
            {
                MessageBox.Show("Chưa chọn file");
                return;
            }
            
            if(isPaused)
            {
                mediaElement.Play();
                timer.Start();
                isPaused = false;
                return;
            }

            mediaElement.Play();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Pause();
            timer.Stop();
            isPaused = true;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Stop();
            timer.Stop();
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isChangeByAuto)
            {
                isChangeByAuto = false;
                return;
            }
            
            mediaElement.Position = TimeSpan.FromSeconds(slider.Value);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        { 
            NextMediaFile();
        }

        private void NextMediaFile()
        {
            if(mediaFileNames.Count == 0)
            {
                MessageBox.Show("Chưa chọn file");
                return;
            }
            if (isShuffle)
            {
                currentMediaIndex = GetNextRandomIndex();
            }
            else
            {
                if (currentMediaIndex == mediaFileNames.Count - 1)
                {
                    currentMediaIndex = 0;
                }
                else
                {
                    currentMediaIndex++;
                }
            }
            listBox_Songs.SelectedIndex = currentMediaIndex;
            mediaElement.Play();
            isPaused = false;
            slider.Value = 0;
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (mediaFileNames.Count == 0)
            {
                MessageBox.Show("Chưa chọn file");
                return;
            }

            if (currentMediaIndex == 0)
            {
                currentMediaIndex = mediaFileNames.Count - 1;
            }
            else
            {
                currentMediaIndex--;
            }
            listBox_Songs.SelectedIndex = currentMediaIndex;
            mediaElement.Play();
            isPaused = false;
        }

        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            isShuffle = !isShuffle;
            if(isShuffle)
            {
                btnShuffle.Background = Brushes.LightBlue;
            }
            else
            {
                btnShuffle.Background = Brushes.LightGray;
            }
        }

        private int GetNextRandomIndex()
        {
            Random random = new Random();
            int index = random.Next(0, mediaFileNames.Count);
            return index;
        }

        private void listBoxSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(listBox_Songs.SelectedIndex != -1)
            {
                currentMediaIndex = listBox_Songs.SelectedIndex;
                UpdateUI(mediaFileNames[currentMediaIndex]);
            }
        }

        private void listBoxPlayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayList playList = listBox_PlayList.SelectedItem as PlayList;
            if (playList != null)
            {
                mediaFileNames = playList.ListFileName;
                currentMediaIndex = 0;
                listBox_Songs.ItemsSource = mediaFileNames;
                listBox_Songs.SelectedIndex = 0;
            }
        }

        private void btnAddPlayList_Click(object sender, RoutedEventArgs e)
        {
            mediaFileNames = SelectMediaFiles(); // Chọn file
            if (mediaFileNames.Count > 0)
            {
                UpdateUI(mediaFileNames[0]); // Cập nhật giao diện
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
            PlayList playList = listBox_PlayList.SelectedItem as PlayList;
            int index = listBox_PlayList.SelectedIndex;
            if (playList != null)
            {
                List<string> newFiles = SelectMediaFiles();
                if (newFiles.Count > 0)
                {
                    foreach (string fileName in newFiles)
                    {
                        if(!playList.ListFileName.Contains(fileName))
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
            PlayList playList = listBox_PlayList.SelectedItem as PlayList;
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
                        listBox_Songs.SelectedIndex = 0;

                    mediaFileNames = playLists[0].ListFileName;
                }
                else
                {
                    listBox_Songs.ItemsSource = null;
                    mediaFileNames = new List<string>();
                }

                currentMediaIndex = 0;
            }
        }

        private void btnRemoveSong_Click(object sender, RoutedEventArgs e)
        {
            PlayList playList = listBox_PlayList.SelectedItem as PlayList;
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

                    if(mediaFileNames.Count > 0)
                    {
                        listBox_Songs.SelectedIndex = 0;
                    }
                }
                
                currentMediaIndex = 0;
            }
        }

        
    }
}