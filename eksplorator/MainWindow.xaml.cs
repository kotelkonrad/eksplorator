using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
namespace Eksplorator
{
    public partial class MainWindow : Window
    {
        private DirectoryInfo? _leftCurrentDirectory;
        private DirectoryInfo? _rightCurrentDirectory;

        public MainWindow()
        {
            InitializeComponent();
            LoadDrives(leftDriveComboBox);
            LoadDrives(rightDriveComboBox);
        }

        private void LoadDrives(ComboBox driveComboBox)
        {
            driveComboBox.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                driveComboBox.Items.Add(drive.Name);
            }
            if (driveComboBox.Items.Count > 0)
                driveComboBox.SelectedIndex = 0;
        }

        private void LoadDirectory(string path, bool isLeft)
        {
            try
            {
                var currentDirectory = new DirectoryInfo(path);

                if (isLeft)
                {
                    _leftCurrentDirectory = currentDirectory;
                    leftPath.Text = _leftCurrentDirectory.FullName;
                }
                else
                {
                    _rightCurrentDirectory = currentDirectory;
                    rightPath.Text = _rightCurrentDirectory.FullName;
                }

                var items = currentDirectory.GetFileSystemInfos()
                    .Select(f => new FileSystemItem
                    {
                        Name = f.Name,
                        Type = f is DirectoryInfo ? "Folder" : "Plik",
                        Size = f is FileInfo fileInfo ? $"{fileInfo.Length / 1024} KB" : "",
                        DateModified = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList();

                if (isLeft)
                {
                    leftFileListView.ItemsSource = items;
                }
                else
                {
                    rightFileListView.ItemsSource = items;
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Nie masz uprawnień do uzyskania dostępu do tego folderu.", "Błąd dostępu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LeftDriveComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (leftDriveComboBox.SelectedItem != null)
            {
                string selectedDrive = leftDriveComboBox.SelectedItem.ToString();
                LoadDirectory(selectedDrive, true);
            }
        }

        private void RightDriveComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rightDriveComboBox.SelectedItem != null)
            {
                string selectedDrive = rightDriveComboBox.SelectedItem.ToString();
                LoadDirectory(selectedDrive, false);
            }
        }

        private void LeftBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_leftCurrentDirectory?.Parent != null)
            {
                LoadDirectory(_leftCurrentDirectory.Parent.FullName, true);
            }
        }

        private void RightBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rightCurrentDirectory?.Parent != null)
            {
                LoadDirectory(_rightCurrentDirectory.Parent.FullName, false);
            }
        }

        private void LeftFileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (leftFileListView.SelectedItem == null) return;

            var selectedItem = leftFileListView.SelectedItem as FileSystemItem;
            if (selectedItem.Type == "Folder")
            {
                LoadDirectory(Path.Combine(_leftCurrentDirectory.FullName, selectedItem.Name), true);
            }
            else
            {
                string filePath = Path.Combine(_leftCurrentDirectory.FullName, selectedItem.Name);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }

        private void RightFileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (rightFileListView.SelectedItem == null) return;

            var selectedItem = rightFileListView.SelectedItem as FileSystemItem;
            if (selectedItem.Type == "Folder")
            {
                LoadDirectory(Path.Combine(_rightCurrentDirectory.FullName, selectedItem.Name), false);
            }
            else
            {
                string filePath = Path.Combine(_rightCurrentDirectory.FullName, selectedItem.Name);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }

        private void LeftFileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowFilePreview(leftFileListView, leftFilePreview);
        }

        private void RightFileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowFilePreview(rightFileListView, rightFilePreview);
        }

        private void ShowFilePreview(ListView listView, ContentControl previewControl)
        {
            if (listView.SelectedItem == null) return;

            var selectedItem = listView.SelectedItem as FileSystemItem;
            string filePath = Path.Combine((listView == leftFileListView ? _leftCurrentDirectory : _rightCurrentDirectory).FullName, selectedItem.Name);

            if (selectedItem.Type == "Plik")
            {
                if (filePath.EndsWith(".txt") || filePath.EndsWith(".csv"))
                {
                    TextBox textBox = new TextBox
                    {
                        Text = File.ReadAllText(filePath),
                        IsReadOnly = true,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };
                    previewControl.Content = textBox;
                }
                else if (filePath.EndsWith(".jpg") || filePath.EndsWith(".png"))
                {
                    Image image = new Image();
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.EndInit();
                    image.Source = bitmap;
                    previewControl.Content = image;
                }
                else
                {
                    previewControl.Content = null;
                }
            }
            else
            {
                previewControl.Content = null;
            }
        }

        private void CopyToRightButton_Click(object sender, RoutedEventArgs e)
        {
            PerformFileOperation(FileOperation.Copy, true);
        }

        private void CopyToLeftButton_Click(object sender, RoutedEventArgs e)
        {
            PerformFileOperation(FileOperation.Copy, false);
        }

        private void MoveToRightButton_Click(object sender, RoutedEventArgs e)
        {
            PerformFileOperation(FileOperation.Move, true);
        }

        private void MoveToLeftButton_Click(object sender, RoutedEventArgs e)
        {
            PerformFileOperation(FileOperation.Move, false);
        }
        
        private void LeftDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            PerformFileOperation(FileOperation.Delete, true);
        }

        private void RightDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            PerformFileOperation(FileOperation.Delete, false);
        }

        private enum FileOperation
        {
            Copy,
            Move,
            Delete
        }

        private void PerformFileOperation(FileOperation operation, bool toRight)
        {
            var sourceListView = toRight ? leftFileListView : rightFileListView;
            var destinationDirectory = toRight ? _rightCurrentDirectory : _leftCurrentDirectory;

            if (destinationDirectory == null) return;

            foreach (FileSystemItem selectedItem in sourceListView.SelectedItems)
            {
                var sourcePath = Path.Combine(toRight ? _leftCurrentDirectory.FullName : _rightCurrentDirectory.FullName, selectedItem.Name);
                var destinationPath = Path.Combine(destinationDirectory.FullName, selectedItem.Name);

                try
                {
                    if (operation == FileOperation.Copy)
                    {
                        if (File.Exists(sourcePath))
                        {
                            File.Copy(sourcePath, destinationPath);
                        }
                        else if (Directory.Exists(sourcePath))
                        {
                            CopyDirectory(sourcePath, destinationPath);
                        }
                    }
                    else if (operation == FileOperation.Move)
                    {
                        if (File.Exists(sourcePath))
                        {
                            File.Move(sourcePath, destinationPath);
                        }
                        else if (Directory.Exists(sourcePath))
                        {
                            Directory.Move(sourcePath, destinationPath);
                        }
                    }
                    else if (operation == FileOperation.Delete)
                    {
                        if (File.Exists(sourcePath))
                        {
                            File.Delete(sourcePath);
                        }
                        else if (Directory.Exists(sourcePath))
                        {
                            if (MessageBox.Show($"Czy na pewno chcesz usunąć folder {sourcePath}?", "Potwierdzenie usunięcia", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                            {
                                if (Directory.EnumerateFileSystemEntries(sourcePath).Any())
                                {
                                    if(MessageBox.Show($"Folder {sourcePath} nie jest pusty. Czy na pewno chcesz usunąć wszystkie pliki i foldery w nim zawarte?", "Potwierdzenie usunięcia", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                                    {
                                        Directory.Delete(sourcePath, true);
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    Directory.Delete(sourcePath);
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Operacja nie powiodła się: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            LoadDirectory(_leftCurrentDirectory.FullName, true);
            LoadDirectory(_rightCurrentDirectory.FullName, false);
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            var dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
            {
                string tempPath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(tempPath, false);
            }

            foreach (var subdir in dirs)
            {
                string tempPath = Path.Combine(destinationDir, subdir.Name);
                CopyDirectory(subdir.FullName, tempPath);
            }
        }

        public class FileSystemItem
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Size { get; set; }
            public string DateModified { get; set; }
        }
    }
}
