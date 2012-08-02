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
using TronLC.Framework;
using System.ComponentModel;

namespace TronLCSim
{
    /// <summary>
    /// Interaction logic for AIBotSelectionWindow.xaml
    /// </summary>
    public partial class AIBotSelectionWindow : Window, INotifyPropertyChanged
    {
        private bool canClose = false;

        private List<IAIBot> instances;
        public List<IAIBot> Instances 
        { 
            get
            {
                return this.instances;
            }
            private set
            {
                if (this.instances != value)
                {
                    this.instances = value;
                    PropertyChange("Instances");
                }
            }
        }

        public AIBotSelectionWindow(List<IAIBot> instances)
        {
            InitializeComponent();
            this.DataContext = this;
            this.Instances = instances;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!canClose)
            {
                e.Cancel = true;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.canClose = true;
            this.DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            canClose = true;
            this.DialogResult = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void PropertyChange(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbInstances.SelectedIndex = 0;
        }
    }
}
