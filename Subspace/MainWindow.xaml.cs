﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Subspace
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Dictionary<string, string> Languages
        {
            get { return (Dictionary<string, string>)GetValue(LanguagesProperty); }
            set { SetValue(LanguagesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Languages.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LanguagesProperty =
            DependencyProperty.Register("Languages", typeof(Dictionary<string, string>), typeof(MainWindow), new PropertyMetadata(null));


        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(MainWindow), new PropertyMetadata("Hello world"));



        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public OpenSubtitlesClient Client { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            Languages = new Dictionary<string, string>
            {
                { "en", "English" },
                { "fr", "French" },
                { "de", "German" },
                { "es", "Spanish" },
                
                { "fi", "Finnish" },
                { "pl", "Polish" },
                { "sv", "Swedish" },
                { "ru", "Russian" },
                { "el", "Greek" },
                { "it", "Italian" },
                { "da", "Danish" },
                { "tr", "Turkish" },

                { "jp", "Japanese" },
                { "ch", "Chinese" }
            };

            Message = "Drag and Drop files here.";

            this.DataContext = this;
        }

        private async Task<bool> EnsureClientExists()
        {
            if (Client == null)
            {
                try
                {
                    Client = await OpenSubtitlesClient.LogIn();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
                return true;
        }

        private void Error(string msg)
        {
            Message = msg;
            TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
            IsLoading = false;
        }

        private void Success(string msg)
        {
            Message = msg;
            TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            IsLoading = false;
        }

        private async void Border_Drop(object sender, DragEventArgs e)
        {
            IsLoading = true;
            TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;

            if (!await EnsureClientExists())
            {
                Error("Couldn't connect to OpenSubtitles server.");
                return;
            }
            
            try
            {
                string lang = ((KeyValuePair<string, string>)LanguageBox.SelectedItem).Key;
                int errors = 0;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    string friendlyName = Path.GetFileNameWithoutExtension(file);

                    Message = $"Searching subtitles for {friendlyName}...";
                    var subs = await Client.GetSubtitlesFromFilePlus(file, lang, false);

                    if (subs.FirstOrDefault() == null)
                    {
                        errors++;
                        continue;
                    }

                    Message = $"Downloading subtitles for {friendlyName}...";
                    File.WriteAllBytes(Path.ChangeExtension(file, "srt"), await Client.RetrieveSubtitle(subs.First()));
                }

                if (errors > 0)
                    Error($"Couldn't find subtitles for {errors} files.");
                else
                    Success(files.Length == 1 ? $"One subtitle loaded." : $"{files.Length} subtitles loaded.");
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        private bool Correct(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string file in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext != ".mp4" && ext != ".mkv" && ext != ".mov" && ext != ".avi")
                    {
                        return false;
                    }
                }

                e.Effects = e.AllowedEffects;
                return true;
            }
            else
                return false;
        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {
            UIElement el = sender as UIElement;
            
            el.AllowDrop = Correct(e);
        }

        private async void Border_DragLeave(object sender, DragEventArgs e)
        {
            await Task.Delay(500);
            Dispatcher.Invoke(() =>
            {
                (sender as UIElement).AllowDrop = true;
            });
            
            // TODO: visual feedback
        }
    }
}