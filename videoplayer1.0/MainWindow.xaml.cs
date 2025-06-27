using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TagLib;

namespace videoplayer1._0
{
    public partial class MainWindow : Window
    {
        private string videoFilePath = string.Empty;
        private string[] videoFiles; // Список видеофайлов в директории
        private int currentVideoIndex; // Индекс текущего видео
        private DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            this.KeyDown += MainWindow_KeyDown; 

            Utils.ApiConfig.Initialize(
                youtubeApiKey: "AIzaSyDk4tBCxsIci3YZwo7wVflwW7LE49bowvw"
            );
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (videoPlayer.Position.TotalMilliseconds > 0)
            {
                timeSlider.Value = videoPlayer.Position.TotalMilliseconds;
            }
        }

        private void OpenVideo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Видео Файлы|*.mp4;*.avi;*.mov",
                Multiselect = false // Позволяем выбрать только один файл
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadVideo(openFileDialog.FileName);
                LoadVideoFiles(Path.GetDirectoryName(openFileDialog.FileName)); // Загружаем видеофайлы из директории
            }
        }

        // Загрузка видео по пути
        private void LoadVideo(string filePath)
        {
            videoPlayer.Source = new Uri(filePath);
            videoFilePath = filePath;

            titleTextBlock.Text = GetFileName(filePath);
            commentTextBlock.Text = GetFileComment(filePath) ?? "Комментарий отсутствует";

            UpdateMediaControl();
            videoPlayer.Play();
        }

        // Загружает все видеофайлы для переходов
        private void LoadVideoFiles(string directoryPath)
        {
            videoFiles = Directory.GetFiles(directoryPath, "*.mp4")
                .Concat(Directory.GetFiles(directoryPath, "*.avi"))
                .Concat(Directory.GetFiles(directoryPath, "*.mov"))
                .ToArray();

            currentVideoIndex = Array.IndexOf(videoFiles, videoFilePath);
        }

        // Пауза
        private void PauseVideo_Click(object sender, RoutedEventArgs e)
        {
            videoPlayer.Pause();
        }

        // Воспроизведение
        private void PlayVideo_Click(object sender, RoutedEventArgs e)
        {
            UpdateMediaControl();
            videoPlayer.Play();
        }

        // Переход к предыдущему видео
        private void LastVideo_Click(object sender, RoutedEventArgs e)
        {
            if (videoFiles != null && videoFiles.Length > 0)
            {
                currentVideoIndex = (currentVideoIndex - 1 + videoFiles.Length) % videoFiles.Length; // Переход к предыдущему видео
                LoadVideo(videoFiles[currentVideoIndex]);
            }
        }

        // Переход к следующему видео
        private void NextVideo_Click(object sender, RoutedEventArgs e)
        {
            if (videoFiles != null && videoFiles.Length > 0)
            {
                currentVideoIndex = (currentVideoIndex + 1) % videoFiles.Length; // Переход к следующему видео
                LoadVideo(videoFiles[currentVideoIndex]);
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S)
            {
                if (videoPlayer.SpeedRatio > 0)
                {
                    PauseVideo_Click(sender, e);
                }
                else
                {
                    PlayVideo_Click(sender, e);
                }

                e.Handled = true;
            }

            if (e.Key == Key.D)
            {
                videoPlayer.Position += TimeSpan.FromSeconds(3); // Перемотка вперед на 3 секунды
                
            }
            else if (e.Key == Key.A)
            {
                videoPlayer.Position -= TimeSpan.FromSeconds(3); // Перемотка назад на 3 секунды
            }
        }

        #region Получение информации о файле

        private static string GetFileComment(string filePath)
        {
            try
            {
                var videoFile = TagLib.File.Create(filePath);
                return videoFile.Tag.Comment;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении комментария: {ex.Message}");
                return null;
            }
        }

        private string GetFileName(string filePath)
        {
            return System.IO.Path.GetFileName(filePath);
        }

        #endregion

        #region Управление медиа

        private void UpdateMediaControl()
        {
            videoPlayer.Volume = volumeSlider.Value;
            videoPlayer.SpeedRatio = speedSlider.Value;
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            videoPlayer.Volume = e.NewValue;
        }

        private void speedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            videoPlayer.SpeedRatio = e.NewValue;
        }

        private void timeSlider_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            videoPlayer.Position = TimeSpan.FromMilliseconds(timeSlider.Value);
            videoPlayer.Play();
            timer.Start();
        }

        private void timeSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            videoPlayer.Pause();
            timer.Stop();
        }

        #endregion

        #region Обновление метаданных

        private string RenameFile(string newFileName)
        {
            if (string.IsNullOrWhiteSpace(newFileName))
            {
                MessageBox.Show("Введите новое имя файла.");
                return null;
            }

            try
            {
                string newFilePath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(videoFilePath),
                    $"{newFileName}{System.IO.Path.GetExtension(videoFilePath)}"
                );

                if (System.IO.File.Exists(newFilePath))
                {
                    MessageBox.Show("Файл с таким именем уже существует. Пожалуйста, выберите другое имя.");
                    return null;
                }

                System.IO.File.Move(videoFilePath, newFilePath);
                return newFilePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переименовании файла: {ex.Message}");
                return null;
            }
        }

        private void ReWriteFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(videoFilePath) || !System.IO.File.Exists(videoFilePath))
            {
                MessageBox.Show("Файл не выбран или не существует.");
                return;
            }

            try
            {
                videoPlayer.Source = null;

                UpdateFileMetadata(videoFilePath, titleTextBox.Text, commentTextBox.Text);

                string newFilePath = RenameFile(titleTextBox.Text);

                if (newFilePath != null)
                {
                    videoFilePath = newFilePath;
                    MessageBox.Show("Метаданные успешно обновлены!");
                    LoadVideo(videoFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при записи метаданных: {ex.Message}");
            }
        }

        private void UpdateFileMetadata(string filePath, string title, string comment)
        {
            var videoFile = TagLib.File.Create(filePath);
            videoFile.Tag.Title = title;
            videoFile.Tag.Comment = comment;
            videoFile.Save();
        }

        #endregion

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            UpdateMediaControl();
            timeSlider.Maximum = videoPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            UpdateWindowSize();
        }

        private void UpdateWindowSize()
        {
            var videoWidth = videoPlayer.NaturalVideoWidth;
            var videoHeight = videoPlayer.NaturalVideoHeight;

            this.Width = Math.Clamp(videoWidth + 50, 320, 1920);
            this.Height = Math.Clamp(videoHeight + 100, 240, 1080);
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            videoPlayer.Stop();
        }

        private void AddVideoToDatabase_Click(object sender, RoutedEventArgs e)
        {
            var addVideoWindow = new AddVideoWindow();
            addVideoWindow.Owner = this;
            addVideoWindow.ShowDialog();
        }
    }
}
