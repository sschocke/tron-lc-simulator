using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;

namespace TronLCSim
{
    /// <summary>
    /// Interaction logic for StartBatConfigWindow.xaml
    /// </summary>
    public partial class StartBatConfigWindow : Window
    {
        private bool canClose = false;

        public StartBatConfigWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!canClose)
            {
                e.Cancel = true;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            canClose = true;
            this.DialogResult = false;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.AddExtension = true;
            dialog.CheckFileExists = true;
            dialog.DefaultExt = "bat";
            dialog.Filter = "Batch Files|*.bat|All Files|*.*";
            dialog.FilterIndex = 0;
            dialog.Title ="Select start.bat file of bot...";
            bool? result = dialog.ShowDialog();
            if (result.HasValue && (result.Value == true))
            {
                txtStartPath.Text = dialog.FileName;
                txtWorkDir.Text = System.IO.Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(txtStartPath.Text) == false)
            {
                MessageBox.Show("Path to start.bat is not valid! File does not exist!", "Start.bat Bot Configuration...");
                return;
            }
            if (Directory.Exists(txtWorkDir.Text) == false)
            {
                MessageBox.Show("Working Directory is not valid! Folder does not exist!", "Start.bat Bot Configuration...");
                return;
            }
            if (String.IsNullOrEmpty(txtPlayerName.Text) == true)
            {
                MessageBox.Show("Player must have a valid name!", "Start.bat Bot Configuration...");
                return;
            }

            this.canClose = true;
            this.DialogResult = true;
        }
    }
}
