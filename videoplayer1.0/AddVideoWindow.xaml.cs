using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using videoplayer1._0.Database;
using videoplayer1._0.Models;
using videoplayer1._0.Utils;
using Npgsql;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Navigation;
using System.Reflection;

namespace videoplayer1._0
{
    public partial class AddVideoWindow : Window
    {
        private List<Category> categories;
        private string currentIframe;

        public AddVideoWindow()
        {
            InitializeComponent();
            LoadCategories();
            DisableScriptErrors();
        }

        private async void LoadCategories()
        {
            categories = new List<Category>
            {
                new Category { Id = 1, Name = "футбол" },
                new Category { Id = 2, Name = "баскетбол" },
                new Category { Id = 3, Name = "хоббихорсинг" }
            };

            CategoryComboBox.ItemsSource = categories;
            CategoryComboBox.DisplayMemberPath = "Name";
            CategoryComboBox.SelectedValuePath = "Id";
        }

        private void DisableScriptErrors()
        {
            try
            {
                // Отключаем окна с ошибками скриптов
                dynamic activeX = PreviewBrowser.GetType().InvokeMember("ActiveXInstance",
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                    null, PreviewBrowser, new object[] { });

                activeX.Silent = true;
            }
            catch { }
        }

        private void PreviewBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            DisableScriptErrors();
        }

        private void PreviewBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            DisableScriptErrors();
        }

        private async void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string url = UrlTextBox.Text.Trim();
                if (string.IsNullOrEmpty(url)) return;

                // Get video info
                var (title, description) = await VideoUtils.GetVideoInfo(url);
                TitleTextBox.Text = title;
                DescriptionTextBox.Text = description;

                // Create and show iframe preview
                currentIframe = VideoUtils.CreateIframe(url);
                PreviewBrowser.NavigateToString(currentIframe);
            }
            catch (Exception ex)
            {
                // Ignore errors while typing
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(UrlTextBox.Text) || 
                    string.IsNullOrEmpty(TitleTextBox.Text) || 
                    CategoryComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please fill in all required fields");
                    return;
                }

                // Извлекаем iframe из HTML документа
                var match = Regex.Match(currentIframe, @"<iframe[^>]*>[^<]*</iframe>");
                if (!match.Success)
                {
                    MessageBox.Show("Failed to process video iframe");
                    return;
                }

                string description = DescriptionTextBox.Text;
                if (string.IsNullOrWhiteSpace(description))
                {
                    MessageBox.Show("Description is empty. Please add a description.");
                    return;
                }

                var video = new Video
                {
                    Title = TitleTextBox.Text,
                    Description = description,
                    Iframe = match.Value, // Сохраняем только сам iframe
                    Date = DateTime.UtcNow,
                    CategoryId = (int)CategoryComboBox.SelectedValue
                };

                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO videos (title, description, iframe, date, \"createdAt\", \"updatedAt\", \"categoryId\") " +
                        "VALUES (@title, @description, @iframe, @date, @createdAt, @updatedAt, @categoryId) RETURNING id", conn))
                    {
                        cmd.Parameters.AddWithValue("title", video.Title);
                        cmd.Parameters.AddWithValue("description", video.Description);
                        cmd.Parameters.AddWithValue("iframe", video.Iframe);
                        cmd.Parameters.AddWithValue("date", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("categoryId", video.CategoryId);

                        var id = await cmd.ExecuteScalarAsync();
                        MessageBox.Show($"Video successfully added with ID: {id}!\nTitle: {video.Title}\nDescription: {video.Description}");
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving video: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 