using System.Windows;
using TronLC.Framework;

namespace TronLCSim
{
    /// <summary>
    /// Interaction logic for BotTypeWindow.xaml
    /// </summary>
    public partial class BotTypeWindow : Window
    {
        public MainWindow.Player Player { get; private set; }
        public string WhichPlayer { get; set; }

        private bool canClose = false;

        public BotTypeWindow()
        {
            InitializeComponent();
        }

        private void btnInternalRandomBot_Click(object sender, RoutedEventArgs e)
        {
            this.Player = new MainWindow.Player(MainWindow.PlayerType.InternalRandom, this.WhichPlayer);
            canClose = true;
            Close();
        }

        private void btnStartBatBot_Click(object sender, RoutedEventArgs e)
        {
            StartBatConfigWindow configWindow = new StartBatConfigWindow();
            configWindow.txtPlayerName.Text = this.WhichPlayer;
            bool? result = configWindow.ShowDialog();
            if (result.HasValue && (result.Value == true))
            {
                MainWindow.StartBatPlayer temp = new MainWindow.StartBatPlayer(configWindow.txtPlayerName.Text, 
                    configWindow.txtStartPath.Text, configWindow.txtWorkDir.Text);

                this.Player = temp;
                canClose = true;
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!canClose)
            {
                e.Cancel = true;
            }
        }

        private void btnAIBot_Click(object sender, RoutedEventArgs e)
        {
            AIBotSelectionWindow selectWindow = new AIBotSelectionWindow(MainWindow.bots);
            bool? result = selectWindow.ShowDialog();
            if (result.HasValue && (result.Value == true))
            {
                MainWindow.AIBotPlayer temp = new MainWindow.AIBotPlayer(selectWindow.cmbInstances.SelectedItem as IAIBot);
                MainWindow.bots.Remove(temp.instance);

                this.Player = temp;
                canClose = true;
                Close();
            }
        }
    }
}
