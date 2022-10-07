using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Microsoft.Win32;


namespace ScannerClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isMaximized = false;

        private Uri folderUri, textFileUri, normalFileUri;
        private Image folderImage, textFileImage, normalFileImage;

        public MainWindow()
        {
            string str = Environment.CurrentDirectory;
            InitializeComponent();

            folderUri = new Uri($@"{Environment.CurrentDirectory}\..\..\..\Images\folder.png");
            normalFileUri = new Uri($@"{Environment.CurrentDirectory}\..\..\..\Images\file.png");
            textFileUri = new Uri($@"{Environment.CurrentDirectory}\..\..\..\Images\textFile.png");

            folderImage = new Image() { Source = new BitmapImage(folderUri) };
            textFileImage = new Image() { Source = new BitmapImage(textFileUri) };
            normalFileImage = new Image() { Source = new BitmapImage(normalFileUri) };
        }

        // Form scrin methods

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Click on the form 2 times

            if (e.ClickCount == 2)
            {
                // Set normal scrin size
                if (isMaximized)
                {
                    this.WindowState = WindowState.Normal;
                    this.Width = 1080;
                    this.Height = 720;

                    isMaximized = false;
                }
                // Set maximum scrin size
                else
                {
                    this.WindowState = WindowState.Maximized;

                    isMaximized = true;
                }
            }
        }

        private void StopGauging_Btn_Click(object sender, RoutedEventArgs e)
        {
            MyScannerLibrary.DirScanner.StopProcessing();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private async void GaugeDir_Btn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog folderBrowser = new OpenFileDialog();
            // Set validate names and check file exists to false otherwise windows will
            // not let you select "Folder Selection."
            folderBrowser.ValidateNames = false;
            folderBrowser.CheckFileExists = false;
            folderBrowser.CheckPathExists = true;
            // Always default to Folder Selection.
            folderBrowser.FileName = "Folder Selection.";

            string? folderPath = string.Empty;
            if (folderBrowser.ShowDialog() == true)
            {
                folderPath = Path.GetDirectoryName(folderBrowser.FileName);

                if (folderPath == null)
                {
                    MessageBox.Show("Error. Folder path is null!");
                    return;
                }
            }

            var task = Task.Run(() => TaskForAnAsyncOperation(folderPath));
            var entities = await task;

            TreeView treeView = GenerateTreeViewFromTheEntities(null, entities, 0, null);

            if (DirectoryTreeView.Children.Count > 0)
                DirectoryTreeView.Children.RemoveAt(0);

            DirectoryTreeView.Children.Add(treeView);
        }

        private void Logout_Btn_Click(object sender, RoutedEventArgs e)
        {
            //Exit the application
            Application.Current.Shutdown();
        }

        // Methods for usage (Not connected with xaml items)

        private TreeView GenerateTreeViewFromTheEntities(TreeViewItem treeItem, List<MyScannerLibrary.Entity> entities, int index, DirectoryInfo subDir)
        {
            TreeView treeView = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                treeView = new TreeView();
                TreeViewItem tempItem = new TreeViewItem();

                // Proceed through all list of entities
                while (index <= entities.Count - 1)
                {
                    if (index == 0 || entities[index].SubDirecory.FullName == subDir?.FullName)
                    {
                        string extension = entities[index].Type == MyScannerLibrary.EntityType.File ? "(file)" : entities[index].Type == MyScannerLibrary.EntityType.Directory ? "(dir)" : "(txt)";
                        string persantage = entities[index].Persantage == String.Empty ? "" : $", {entities[index].Persantage}";

                        var newTreeItem = new TreeViewItem();

                        StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
                        TextBlock textBlock = new TextBlock() { Text = extension + $" {entities[index].Name} ({entities[index].Size} байт{persantage})" };

                        string path = entities[index].Type == MyScannerLibrary.EntityType.File ? normalFileUri.ToString() : entities[index].Type == MyScannerLibrary.EntityType.Directory ? folderUri.ToString() : textFileUri.ToString();
                        Uri uri = new Uri(path);
                        var image = new Image() { Source = new BitmapImage(uri) };

                        stackPanel.Children.Add(image);
                        stackPanel.Children.Add(textBlock);

                        newTreeItem.Header = stackPanel;

                        if (treeItem == null)
                        {
                            treeView.Items.Add(newTreeItem);
                            treeItem = newTreeItem;
                        }
                        else
                        {
                            treeItem.Items.Add(newTreeItem);
                        }

                        tempItem = newTreeItem;

                        index += 1;

                        continue;
                    }
                    else
                    {
                        if (subDir == null || entities[index].SubDirecory.FullName.Contains(entities[index - 1].SubDirecory.FullName))
                        {
                            treeItem = tempItem;
                            subDir = entities[index].SubDirecory;
                            continue;
                        }
                        else
                        {
                            subDir = subDir.Parent;
                            treeItem = (TreeViewItem)treeItem.Parent;
                            continue;
                        }
                    }
                }

                return treeView;
            });
            return treeView;
        }

        private Task<List<MyScannerLibrary.Entity>> TaskForAnAsyncOperation(string folderPath)
        {
            var entities = MyScannerLibrary.DirScanner.Scan(folderPath);

            return Task.FromResult<List<MyScannerLibrary.Entity>>(entities);
        }
    }
}
