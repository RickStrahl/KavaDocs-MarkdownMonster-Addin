﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocHound.Configuration;
using DocHound.Utilities;
using FontAwesome.WPF;
using KavaDocsAddin;
using KavaDocsAddin.Core.Configuration;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MarkdownMonster;
using MarkdownMonster.Windows;
using Microsoft.Win32;
using Westwind.Utilities;
using Color = System.Windows.Media.Color;

namespace DocHound.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for PasteHref.xaml
    /// </summary>
    public partial class PasteImageDialog : MetroWindow, INotifyPropertyChanged
    {
        private string _image;
        private string _imageText;

        public string Image
        {
            get { return _image; }
            set
            {
                if (value == _image) return;
                _image = value;
                OnPropertyChanged(nameof(Image));
               
            }
        }

        public string ImageText
        {
            get { return _imageText; }
            set
            {
                if (value == _imageText) return;
                _imageText = value;
                OnPropertyChanged(nameof(ImageText));
            }
        }



        public bool PasteAsBase64Content
        {
            get { return _PasteAsBase64Content; }
            set
            {
                if (value == _PasteAsBase64Content) return;
                _PasteAsBase64Content = value;
                OnPropertyChanged(nameof(PasteAsBase64Content));
            }
        }
        private bool _PasteAsBase64Content = false;

        public string MarkdownFile { get; set; }

        

        public AppModel AppModel
        {
            get { return _appModel; }
            set
            {
                if (_appModel == value) return;
                _appModel = value;
                OnPropertyChanged(nameof(AppModel));
            }
        }
        private AppModel _appModel = mmApp.Model;

        public CommandBase PasteCommand
        {
            get { return _pasteCommand; }
            set
            {
                _pasteCommand = value;
                OnPropertyChanged(nameof(PasteCommand));
            }
        }

        private CommandBase _pasteCommand;

        public bool IsFileImage
        {
            get { return !_isMemoryImage; }
        }

        public bool IsMemoryImage
        {
            get { return _isMemoryImage; }
            set
            {
                if (value == _isMemoryImage) return;
                _isMemoryImage = value;
                OnPropertyChanged(nameof(IsMemoryImage));
                OnPropertyChanged(nameof(IsFileImage));
            }
        }

        private bool _isMemoryImage;

        AppModel Model { get; set; } 

        MarkdownDocumentEditor Editor { get; set; }


        #region Initialization


        public PasteImageDialog(MainWindow window)
        {
            InitializeComponent();
            
            Owner = window;
            DataContext = this;            

            Loaded += PasteImage_Loaded;
            SizeChanged += PasteImage_SizeChanged;
            Activated += PasteImage_Activated;
            PreviewKeyDown += PasteImage_PreviewKeyDown;

            
            Model = window.Model;                       
        }

        private void PasteImage_Loaded(object sender, RoutedEventArgs e)
        {

            PasteCommand = new CommandBase((s, args) =>
            {
                MessageBox.Show("PasteCommand");
            });

            TextImage.Focus();
            if (string.IsNullOrEmpty(Image) && Clipboard.ContainsImage())
            {
                PasteImageFromClipboard();
            }
            else if (string.IsNullOrEmpty(Image) && Clipboard.ContainsText())
            {
                string clip = Clipboard.GetText().ToLower();
                if ((clip.StartsWith("http://") || clip.StartsWith("https://")) &&
                    (clip.Contains(".png") || clip.Contains("jpg")))
                {
                    TextImage.Text = clip;
                    SetImagePreview(clip);
                }
            }
        }


        private void PasteImage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool isControlKey = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            if (isControlKey && e.Key == Key.V && Clipboard.ContainsImage())            
                PasteImageFromClipboard();
            else if (isControlKey && e.Key == Key.C)
                Button_CopyImage(null, null);
        }

        private void PasteImage_Activated(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Image) && Clipboard.ContainsImage())            
                PasteImageFromClipboard();            
        }

        #endregion

        private void SelectLocalImageFile_Click(object sender, RoutedEventArgs e)
        {
            var fd = new OpenFileDialog
            {
                DefaultExt = ".png",
                Filter = "Image files (*.png;*.jpg;*.gif;)|*.png;*.jpg;*.jpeg;*.gif|All Files (*.*)|*.*",
                CheckFileExists = true,
                RestoreDirectory = true,
                Multiselect = false,
                Title = "Embed Image"
            };

            if (!string.IsNullOrEmpty(MarkdownFile))
                fd.InitialDirectory = Path.GetDirectoryName(MarkdownFile);
            else
            {
                if (!string.IsNullOrEmpty(KavaApp.Configuration.LastImageFolder))
                    fd.InitialDirectory = KavaApp.Configuration.LastImageFolder;
                else
                    fd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }

            var res = fd.ShowDialog();
            if (res == null || !res.Value)
                return;

            Image = fd.FileName;

            if (PasteAsBase64Content)
            {
	            var bmi = new BitmapImage();
	            bmi.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // don't lock file
	            bmi.UriSource = new Uri(fd.FileName);

				Base64EncodeImage(fd.FileName);
	            ImagePreview.Source = bmi;
                return;
            }


            // Normalize the path relative to the Markdown file
            if (!string.IsNullOrEmpty(MarkdownFile) && MarkdownFile != "untitled")
            {
                string imgUrl = fd.FileName;
                if (!string.IsNullOrEmpty(imgUrl))
                {
                    Image = imgUrl;
                    TextImageText.Focus();
                    return;
                }

                string mdPath = Path.GetDirectoryName(MarkdownFile);
                string relPath = fd.FileName;
                try
                {
                    relPath = FileUtils.GetRelativePath(fd.FileName, mdPath);
                }
                catch (Exception ex)
                {
                    mmApp.Log($"Failed to get relative path.\r\nFile: {fd.FileName}, Path: {mdPath}", ex);
                }


                if (!relPath.StartsWith("..\\"))
                    Image = relPath;
                else
                {
                    // not relative 
                    var mbres = MessageBox.Show(
                        "The image you are linking, is not in a relative path.\r\n" +
                        "Do you want to copy it to a local path?",
                        "Non-relative Image",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (mbres.Equals(MessageBoxResult.Yes))
                    {
                        string newImageFileName = System.IO.Path.Combine(mdPath, System.IO.Path.GetFileName(fd.FileName));
                        var sd = new SaveFileDialog
                        {
                            Filter = "Image files (*.png;*.jpg;*.gif;)|*.png;*.jpg;*.jpeg;*.gif|All Files (*.*)|*.*",
                            FilterIndex = 1,
                            FileName = newImageFileName,
                            InitialDirectory = mdPath,
                            CheckFileExists = false,
                            OverwritePrompt = true,
                            CheckPathExists = true,
                            RestoreDirectory = true
                        };
                        var result = sd.ShowDialog();
                        if (result != null && result.Value)
                        {
                            try
                            {
                                File.Copy(fd.FileName, sd.FileName, true);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Couldn't copy file to new location: \r\n" + ex.Message,
                                    KavaApp.ApplicationName);
                                return;
                            }
                            try
                            {
                                relPath = FileUtils.GetRelativePath(sd.FileName, mdPath);
                            }
                            catch (Exception ex)
                            {
                                mmApp.Log($"Failed to get relative path.\r\nFile: {sd.FileName}, Path: {mdPath}", ex);
                            }
                            Image = relPath;
                        }
                    }
                    else
                        Image = relPath;
                }
            }


            
            if (Image.Contains(":\\"))
                Image = "file:///" + Image;
            else
                Image = Image.Replace("\\", "/");

            SetImagePreview("file:///" + fd.FileName);

            IsMemoryImage = false;

            mmApp.Configuration.LastImageFolder = Path.GetDirectoryName(fd.FileName);
            TextImageText.Focus();
        }

        private void TextImage_LostFocus(object sender, RoutedEventArgs e)
        {
            
            if (!IsMemoryImage && string.IsNullOrEmpty(TextImage.Text))
            {
                ImagePreview.Source = null;                
                return;
            }

            string href = TextImage.Text.ToLower();
            if (href.StartsWith("http://") || href.StartsWith("https://"))
            {
                SetImagePreview(href);
            }            
        }

        #region Main Buttons

        /// <summary>
        /// Saves an image loaded from clipboard to disk OR if base64 is checked
        /// creates the base64 content.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SaveImage(object sender, RoutedEventArgs e)
        {
            string imagePath = null;

            var bitmapSource = ImagePreview.Source as BitmapSource;
            if (bitmapSource == null)
            {
                MessageBox.Show("Unable to convert bitmap source.", "Bitmap conversion error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                    return;
            }

            using (var bitMap = WindowUtilities.BitmapSourceToBitmap(bitmapSource))
            {                
                if (PasteAsBase64Content)
                {
                    Base64EncodeImage(bitMap);
                    IsMemoryImage = false;
                    return;
                }
            }


            string initialFolder = Path.Combine(kavaUi.AddinModel.ActiveProject.ProjectDirectory, "wwwroot", "images");

            var sd = new SaveFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.gif;)|*.png;*.jpg;*.jpeg;*.gif|All Files (*.*)|*.*",
                FilterIndex = 1,
                Title = "Save Image from Clipboard as",
                InitialDirectory = initialFolder,
                CheckFileExists = false,
                OverwritePrompt = true,
                CheckPathExists = true,
                RestoreDirectory = true
            };
            var result = sd.ShowDialog();
            if (result != null && result.Value)
            {
                imagePath = sd.FileName;

                try
                {
                    var ext = Path.GetExtension(imagePath)?.ToLower();

                    using (var fileStream = new FileStream(imagePath, FileMode.Create))
                    {
                        BitmapEncoder encoder = null;
                        if (ext == ".png")
                            encoder = new PngBitmapEncoder();
                        else if (ext == ".jpg")
                            encoder = new JpegBitmapEncoder();
                        else if (ext == ".gif")
                            encoder = new GifBitmapEncoder();

                        encoder.Frames.Add(BitmapFrame.Create(ImagePreview.Source as BitmapSource));
                        encoder.Save(fileStream);

                        if (ext == ".png")
                            mmFileUtils.OptimizePngImage(sd.FileName, 5); // async
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Couldn't copy file to new location: \r\n" + ex.Message, KavaApp.ApplicationName);
                    return;
                }

                string relPath = Path.GetDirectoryName(sd.FileName);
                if (initialFolder != null)
                {
                    try
                    {
                        relPath = FileUtils.GetRelativePath(sd.FileName, initialFolder);
                    }
                    catch (Exception ex)
                    {
                        mmApp.Log($"Failed to get relative path.\r\nFile: {sd.FileName}, Path: {imagePath}", ex);
                    }
                    imagePath = relPath;
                }

                if (imagePath.Contains(":\\"))
                    imagePath = "file:///" + imagePath;

                imagePath = imagePath.Replace("\\", "/");

                this.Image = imagePath;
                IsMemoryImage = false;

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonCancel)
                DialogResult = false;
            else
                DialogResult = true;

            Close();
        }

        private void Button_PasteImage(object sender, RoutedEventArgs e)
        {
            PasteImageFromClipboard();
        }

        private void CheckPasteAsBase64Content_Checked(object sender, RoutedEventArgs e)
        {             
            if (PasteAsBase64Content)
                if (IsMemoryImage)
                {
                    Base64EncodeImage(WindowUtilities.BitmapSourceToBitmap(ImagePreview.Source as BitmapSource));
                    IsMemoryImage = false;
                }
                else
                    Base64EncodeImage(Image);
            else
                Image = null;
            
        }

        private void Button_EditImage(object sender, RoutedEventArgs e)
        {
            string exe = mmApp.Configuration.ImageEditor;
            

            string imageFile = Image;
            if (!imageFile.Contains(":\\") && kavaUi.AddinModel.ActiveProject != null)
            {
                imageFile = Path.Combine(kavaUi.AddinModel.ActiveProject.ProjectDirectory,Image);
            }

			if (!mmFileUtils.OpenImageInImageEditor(imageFile))
			{
				MessageBox.Show("Unable to launch image editor " + Path.GetFileName(mmApp.Configuration.ImageEditor) +
				                "\r\n\r\n" +
				                "Most likely the image editor configured in settings is not a valid executable. Please check the 'ImageEditor' key in the Markdown Monster Settings.\r\n\r\n" +
				                "We're opening the settings file for you in the editor now.",
					"Image Launching Error",
					MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			else
				ShowStatus("Launching editor " + exe + " with " + imageFile, 5000);
        }

        private void Button_ClearImage(object sender, RoutedEventArgs e)
        {
            Image = null;
            ImageText = null;
            ImagePreview.Source = null;
            ShowStatus("Image has been cleared.", 6000);
        }

        private void Button_CopyImage(object sender, RoutedEventArgs e)
        {
            if (ImagePreview.Source != null)
            {
                var src = ImagePreview.Source as BitmapSource;
                if (src != null)
                {
                    Clipboard.SetImage(src);
                    ShowStatus("Image copied to the Clipboard.", 6000);
                }
            }
        }

        #region StatusBar Display

        public void ShowStatus(string message = null, int milliSeconds = 0)
        {
            if (message == null)
            {
                message = "Ready";
                SetStatusIcon();
            }

            StatusText.Text = message;

            if (milliSeconds > 0)
            {
                Dispatcher.DelayWithPriority(milliSeconds, (win) =>
                {                    
                    ShowStatus(null, 0);
                    SetStatusIcon();
                }, this);
            }
            WindowUtilities.DoEvents();
        }

        /// <summary>
        /// Status the statusbar icon on the left bottom to some indicator
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="color"></param>
        /// <param name="spin"></param>
        public void SetStatusIcon(FontAwesomeIcon icon, Color color, bool spin = false)
        {
            StatusIcon.Icon = icon;
            StatusIcon.Foreground = new SolidColorBrush(color);
            if (spin)
                StatusIcon.SpinDuration = 30;

            StatusIcon.Spin = spin;
        }

        /// <summary>
        /// Resets the Status bar icon on the left to its default green circle
        /// </summary>
        public void SetStatusIcon()
        {
            StatusIcon.Icon = FontAwesomeIcon.Circle;
            StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
            StatusIcon.Spin = false;
            StatusIcon.SpinDuration = 0;
            StatusIcon.StopSpin();
        }

        /// <summary>
        /// Helper routine to show a Metro Dialog. Note this dialog popup is fully async!
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="style"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public async Task<MessageDialogResult> ShowMessageOverlayAsync(string title, string message,
            MessageDialogStyle style = MessageDialogStyle.Affirmative,
            MetroDialogSettings settings = null)
        {
            return await this.ShowMessageAsync(title, message, style, settings);
        }

        #endregion

        #endregion

        #region Image Display

        private void PasteImageFromClipboard()
        {
            SetImagePreview(Clipboard.GetImage());
            Image = null;
            IsMemoryImage = true;
            
            ShowStatus("Image pasted from clipboard...", 5000);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        public void Base64EncodeImage(string file)
        {
            try
            {
                if (!PasteAsBase64Content ||
                    Image == null ||
                    Image.StartsWith("data:image/") ||
                    file == "Untitled")
                    return;

                file = file.Replace("file:///", "");

                var fullPath = file;
                if (!File.Exists(file))
                    fullPath = Path.Combine(kavaUi.AddinModel.ActiveProject.ProjectDirectory, file);

                if (File.Exists(fullPath))
                {
                    var bytes = File.ReadAllBytes(fullPath);
                    var bytestring = Convert.ToBase64String(bytes);
                    var mediaFormat = mmFileUtils.GetImageMediaTypeFromFilename(fullPath);
                    Image = $"data:{mediaFormat};base64," + bytestring;
                }
            }
            catch (Exception ex)
            {
                ShowStatus("Image base64 encoding failed: " + ex.GetBaseException().Message, 5000);
                this.SetStatusIcon(FontAwesomeIcon.Warning, Colors.Firebrick);
            }
        }

        public void Base64EncodeImage(Bitmap bmp)
        {
            try
            {
                using (var ms = new MemoryStream(10000))
                {
                    bmp.Save(ms, ImageFormat.Jpeg);
                    ms.Flush();
                    Image = $"data:image/jpeg;base64,{Convert.ToBase64String(ms.ToArray())}";
                }                
            }
            catch (Exception ex)
            {
                ShowStatus("Image base64 encoding failed: " + ex.GetBaseException().Message, 5000);
                this.SetStatusIcon(FontAwesomeIcon.Warning, Colors.Firebrick);
            }
        }

        private void PasteImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var image = ImagePreview.Source as BitmapSource;
            if (image == null)
                return;

            if (image.Width < Width - 20 && image.Height < PageGrid.RowDefinitions[1].ActualHeight)
                ImagePreview.Stretch = Stretch.None;
            else
                ImagePreview.Stretch = Stretch.Uniform;
        }


        /// <summary>
        /// Attempts to resolve the full image filename from the active image
        /// if the image is a file based image with a relative or physical
        /// path but not a URL based image.
        /// </summary>
        /// <returns></returns>
        public string GetFullImageFilename(string filename = null)
        {
            string imageFile = filename ?? Image;

            if (string.IsNullOrEmpty(imageFile))
                return null;

             if (!File.Exists(imageFile))
                    imageFile = Path.Combine(kavaUi.AddinModel.ActiveProject.ProjectDirectory, imageFile);

            return File.Exists(imageFile) ? imageFile : null;
        }

        public void SetImagePreview(string url = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                url = GetFullImageFilename();
                if (url == null)
                    url = Image;
            }

            try
            {
	            var bmi = new BitmapImage();
	            bmi.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // no locking
	            bmi.UriSource = new Uri(url);

	            ImagePreview.Source = bmi;
                if (Height < 400)
                {
                    Top -= 300;
                    Left -= 100;
                    Width = 800;
                    Height = 800;
                }

                WindowUtilities.DoEvents();
                PasteImage_SizeChanged(this, null);
            }
            catch
            {
            }
        }		

        private void SetImagePreview(BitmapSource source)
        {
            try
            {
                ImagePreview.Source = source;
                if (Height < 400)
                {
                    Top -= 300;
                    Left -= 100;
                    Width = 800;
                    Height = 800;
                }

                WindowUtilities.DoEvents();
                PasteImage_SizeChanged(this, null);
            }
            catch
            {
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
