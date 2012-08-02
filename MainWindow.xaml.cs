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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading;
using System.IO;
using System.Diagnostics;
using TronLC.Framework;
using System.Reflection;

namespace TronLCSim
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum PointState
        {
            Clear,
            Player1,
            Player2,
            Player1Wall,
            Player2Wall,
            You = 100,
            YourWall,
            Opponent,
            OpponentWall
        }

        public enum PlayerType
        {
            InternalRandom,
            ExternalStartBat,
            AIBotInterface
        }

        public class Player
        {
            public PlayerType type;
            public string name;

            public Player(PlayerType type, string name)
            {
                this.type = type;
                this.name = name;
            }

            public override string ToString()
            {
                return type.ToString();
            }
        }
        public class StartBatPlayer : Player
        {
            public string path;
            public string workdir;

            public StartBatPlayer(string name, string path, string workdir)
                : base(PlayerType.ExternalStartBat, name)
            {
                this.path = path;
                this.workdir = workdir;
            }

            public override string ToString()
            {
                return name;
            }
        }
        public class AIBotPlayer : Player
        {
            public IAIBot instance;

            public AIBotPlayer(IAIBot bot)
                : base(PlayerType.AIBotInterface, bot.ToString())
            {
                this.instance = bot;
            }

            public override string ToString()
            {
                return name;
            }
        }

        public static List<IAIBot> bots = new List<IAIBot>();

        public Ellipse[,] points = new Ellipse[30, 30];
        public Line[,] Hlines = new Line[30, 30];
        public Line[,] Vlines = new Line[30, 30];
        public PointState[,] map = new PointState[30, 30];
        public int currentPlayer;
        public Player[] players;

        private Random rng = new Random();
        private BackgroundWorker worker;

        public MainWindow()
        {
            InitializeComponent();

            createHLines();
            createVLines();
            createVectorDots();
            findAIBots();

            resetMap();
            repaintMap();

            players = new Player[2];
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;

            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }

        private void findAIBots()
        {
            Type typeInterface = typeof(IAIBot);

            string[] exeFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.exe", SearchOption.TopDirectoryOnly);
            foreach (string file in exeFiles)
            {
                Assembly pluginAssembly = Assembly.LoadFrom(file);

                foreach (Type pluginType in pluginAssembly.GetTypes())
                {
                    Type testType = pluginType.GetInterface(typeInterface.FullName);

                    if ((pluginType.IsSubclassOf(typeInterface) == true) || (testType != null))
                    {
                        IAIBot instance = (IAIBot)Activator.CreateInstance(pluginType);

                        bots.Add(instance);
                    }
                }
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnReset.IsEnabled = true;
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 50)
            {
                repaintMap();
            }
            else if (e.ProgressPercentage == 99)
            {
                Ellipse targetEllipse = (Ellipse)e.UserState;
                RecolorLines(Brushes.Red, Brushes.Black);
                targetEllipse.Stroke = Brushes.DarkGray;
                targetEllipse.Fill = Brushes.Black;
                MessageBox.Show("Player 1 loses!");
            }
            else if (e.ProgressPercentage == 100)
            {
                Ellipse targetEllipse = (Ellipse)e.UserState;
                RecolorLines(Brushes.Blue, Brushes.Black);
                targetEllipse.Stroke = Brushes.DarkGray;
                targetEllipse.Fill = Brushes.Black;
                MessageBox.Show("Player 2 loses!");
            }
            else
            {
                int player = e.ProgressPercentage;
                Line targetLine = (Line)e.UserState;
                targetLine.Stroke = (player == 0) ? Brushes.Red : Brushes.Blue;
                targetLine.StrokeThickness = 3;
            }
        }

        private void RecolorLines(SolidColorBrush from, SolidColorBrush to)
        {
            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    if (Vlines[x, y].Stroke.Equals(from) == true)
                    {
                        Vlines[x, y].Stroke = to;
                    }
                    if (Hlines[x, y].Stroke.Equals(from) == true)
                    {
                        Hlines[x, y].Stroke = to;
                    }
                    if (points[x, y].Stroke.Equals(from) == true)
                    {
                        points[x, y].Stroke = to;
                        points[x, y].Fill = to;
                    }
                }
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep(100);
                if (worker.CancellationPending == true)
                {
                    break;
                }
                int px = 0, py = 0;
                int ox = 0, oy = 0;
                switch (currentPlayer)
                {
                    case 0:
                        Locate(map, PointState.Player1, out px, out py);
                        Locate(map, PointState.Player2, out ox, out oy);
                        break;
                    case 1:
                        Locate(map, PointState.Player2, out px, out py);
                        Locate(map, PointState.Player1, out ox, out oy);
                        break;
                }
                bool hasMove = CheckLegalMoveAvailable(px, py, ox, oy);
                if (hasMove == false)
                {
                    worker.ReportProgress((currentPlayer == 0) ? 99 : 100, points[px, py]);
                    break;
                }
                PointState[,] curPlayerState = GenerateCurrentMapState();
                WriteGameState(curPlayerState);

                CallPlayerProgram();

                PointState[,] newPlayerState = ReadGameState();
                UpdateCurrentMapState(newPlayerState);

                int newpx = 0, newpy = 0;
                switch (currentPlayer)
                {
                    case 0:
                        Locate(map, PointState.Player1, out newpx, out newpy);
                        break;
                    case 1:
                        Locate(map, PointState.Player2, out newpx, out newpy);
                        break;
                }

                if (py == 0)
                {
                    for (int xi = 0; xi < 30; xi++)
                    {
                        worker.ReportProgress(currentPlayer, Hlines[xi, py]);
                    }
                    worker.ReportProgress(currentPlayer, Vlines[newpx, py]);
                }
                else if (py == 29)
                {
                    for (int xi = 0; xi < 30; xi++)
                    {
                        worker.ReportProgress(currentPlayer, Hlines[xi, py]);
                    }
                    worker.ReportProgress(currentPlayer, Vlines[newpx, newpy]);
                }
                else if (newpx != px)
                {
                    // Horizontal movement
                    if ((newpx - px == 1) || ((px == 29) && (newpx == 0)))
                    {
                        // Right movement
                        worker.ReportProgress(currentPlayer, Hlines[px, py]);
                    }
                    else
                    {
                        // Left movement
                        worker.ReportProgress(currentPlayer, Hlines[newpx, py]);
                    }
                }
                else if (newpy != py)
                {
                    // Vertical movement
                    if ((newpy - py == 1) || ((py == 29) && (newpy == 0)))
                    {
                        // Downwards movement
                        worker.ReportProgress(currentPlayer, Vlines[px, py]);
                    }
                    else
                    {
                        // Upwards movement
                        worker.ReportProgress(currentPlayer, Vlines[px, newpy]);
                    }
                }

                currentPlayer = ((currentPlayer + 1) % 2);
                worker.ReportProgress(50);
            }
        }

        private bool CheckLegalMoveAvailable(int px, int py, int ox, int oy)
        {
            bool goUp = (py > 0) &&  map[px, py - 1] == PointState.Clear;
            bool goDown = (py < 29) && map[px, py + 1] == PointState.Clear;
            bool goLeft = map[(px - 1) >= 0 ? (px - 1) : 29, py] == PointState.Clear;
            bool goRight = map[(px + 1) <= 29 ? (px + 1) : 0, py] == PointState.Clear;
            if (py == 0)
            {
                goLeft = goRight = goUp = goDown = false;
                for (int xi = 0; xi < 30; xi++)
                {
                    if (map[xi, 1] == PointState.Clear)
                    {
                        goDown = true;
                        break;
                    }
                }
            }
            else if (py == 29)
            {
                goLeft = goRight = goUp = goDown = false;
                for (int xi = 0; xi < 30; xi++)
                {
                    if (map[xi, 28] == PointState.Clear)
                    {
                        goUp = true;
                        break;
                    }
                }
            }
            else if (py == 1)
            {
                if (oy == 0)
                {
                    goUp = false;
                }
            }
            else if (py == 28)
            {
                if (oy == 29)
                {
                    goDown = false;
                }
            }

            return (goUp || goDown || goLeft || goRight);
        }

        private void UpdateCurrentMapState(PointState[,] newState)
        {
            for (int x = 0; x < 30; x++)
            {
                if (newState[x, 0] == PointState.YourWall)
                {
                    for (int xi = 0; xi < 30; xi++)
                    {
                        newState[xi, 0] = PointState.YourWall;
                    }
                    break;
                }
            }
            for (int x = 0; x < 30; x++)
            {
                if (newState[x, 29] == PointState.YourWall)
                {
                    for (int xi = 0; xi < 30; xi++)
                    {
                        newState[xi, 29] = PointState.YourWall;
                    }
                    break;
                }
            }
            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    switch (newState[x, y])
                    {
                        case PointState.You:
                            map[x, y] = (currentPlayer == 0) ? PointState.Player1 : PointState.Player2;
                            break;
                        case PointState.YourWall:
                            map[x, y] = (currentPlayer == 0) ? PointState.Player1Wall : PointState.Player2Wall;
                            break;
                        case PointState.Opponent:
                            map[x, y] = (currentPlayer == 0) ? PointState.Player2 : PointState.Player1;
                            break;
                        case PointState.OpponentWall:
                            map[x, y] = (currentPlayer == 0) ? PointState.Player2Wall : PointState.Player1Wall;
                            break;
                        default:
                            map[x, y] = PointState.Clear;
                            break;
                    }
                }
            }
        }

        private PointState[,] ReadGameState()
        {
            int x, y;
            PointState state;

            PointState[,] newState = new PointState[30, 30];
            string[] lines = File.ReadAllLines("game.state", Encoding.UTF8);
            foreach (string line in lines)
            {
                string[] tokens = line.Split(' ');
                if (tokens.Length != 3)
                {
                    continue;
                }

                if (int.TryParse(tokens[0], out x) == false)
                {
                    continue;
                }
                if (int.TryParse(tokens[1], out y) == false)
                {
                    continue;
                }
                if (Enum.TryParse<PointState>(tokens[2], out state) == false )
                {
                    continue;
                }

                newState[x, y] = state;
            }

            return newState;
        }

        private void CallPlayerProgram()
        {
            switch (players[currentPlayer].type)
            {
                case PlayerType.InternalRandom:
                    InternalRandomWalker();
                    break;
                case PlayerType.ExternalStartBat:
                    {
                        StartBatPlayer player = (StartBatPlayer)players[currentPlayer];
                        File.Copy("game.state", player.workdir + "\\game.state", true);

                        ProcessStartInfo botProcessInfo = new ProcessStartInfo();
                        botProcessInfo.FileName = player.path;
                        botProcessInfo.WorkingDirectory = player.workdir;
                        botProcessInfo.UseShellExecute = true;
                        botProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        botProcessInfo.Arguments = '"' + player.workdir + "\\game.state\"";
                        Process botProcess = Process.Start(botProcessInfo);
                        while (botProcess.HasExited == false)
                        {
                            Thread.Sleep(100);
                        }

                        File.Copy(player.workdir + "\\game.state", "game.state", true);
                    }
                    break;
                case PlayerType.AIBotInterface:
                    {
                        AIBotPlayer player = (AIBotPlayer)players[currentPlayer];

                        player.instance.ExecuteMove("game.state");
                    }
                    break;

            }
        }

        private void InternalRandomWalker()
        {
            PointState[,] inputState = ReadGameState();
            int px = 0, py = 0;
            Locate(inputState, PointState.You, out px, out py);
            int newpx = px, newpy = py;
            bool validMove = true;
            do
            {
                validMove = true;
                newpx = px; newpy = py;
                if (py == 0)
                {
                    newpx = rng.Next(30);
                    newpy = 1;
                }
                else if (py == 29)
                {
                    newpx = rng.Next(30);
                    newpy = 28;
                }
                else
                {
                    int move = rng.Next(4);
                    switch (move)
                    {
                        case 0:
                            newpx++;
                            newpx = newpx % 30;
                            break;
                        case 1:
                            newpy++;
                            newpy = newpy % 30;
                            break;
                        case 2:
                            newpx--;
                            if (newpx < 0) newpx = 29;
                            break;
                        case 3:
                            newpy--;
                            if (newpy < 0) newpy = 29;
                            break;
                    }
                }
                if (inputState[newpx, newpy] != PointState.Clear)
                {
                    validMove = false;
                }
            } while (!validMove);

            inputState[px, py] = PointState.YourWall;
            inputState[newpx, newpy] = PointState.You;

            WriteGameState(inputState);
        }

        private void Locate(PointState[,] state, PointState what, out int px, out int py)
        {
            px = py = -1;

            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    if (state[x, y] == what)
                    {
                        px = x;
                        py = y;
                    }
                }
            }
        }

        private void WriteGameState(PointState[,] curState)
        {
            using (TextWriter writer = File.CreateText("game.state"))
            {
                for (int x = 0; x < 30; x++)
                {
                    for (int y = 0; y < 30; y++)
                    {
                        writer.WriteLine("{0} {1} {2}", x, y, curState[x, y].ToString());
                    }
                }
                writer.Close();
            }
        }

        private PointState[,] GenerateCurrentMapState()
        {
            PointState[,] mapState = new PointState[30, 30];
            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    switch(map[x,y])
                    {
                        case PointState.Player1:
                            mapState[x, y] = (currentPlayer == 0) ? PointState.You : PointState.Opponent;
                            break;
                        case PointState.Player1Wall:
                            mapState[x, y] = (currentPlayer == 0) ? PointState.YourWall : PointState.OpponentWall;
                            break;
                        case PointState.Player2:
                            mapState[x, y] = (currentPlayer == 1) ? PointState.You : PointState.Opponent;
                            break;
                        case PointState.Player2Wall:
                            mapState[x, y] = (currentPlayer == 1) ? PointState.YourWall : PointState.OpponentWall;
                            break;
                        default:
                            mapState[x, y] = PointState.Clear;
                            break;
                    }
                }
            }

            return mapState;
        }

        private void repaintMap()
        {
            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    points[x, y].Width = 8;
                    points[x, y].Height = 8;
                    Canvas.SetTop(points[x, y], (y * 20) + 3);
                    Canvas.SetLeft(points[x, y], (x * 20) + 3);
                    switch (map[x, y])
                    {
                        case PointState.Player1:
                            points[x, y].Fill = Brushes.Red;
                            points[x, y].Stroke = Brushes.Red;
                            points[x, y].Width = 16;
                            points[x, y].Height = 16;
                            Canvas.SetTop(points[x, y], (y * 20) - 1);
                            Canvas.SetLeft(points[x, y], (x * 20) - 1);
                            break;
                        case PointState.Player2:
                            points[x, y].Fill = Brushes.Blue;
                            points[x, y].Stroke = Brushes.Blue;
                            points[x, y].Width = 16;
                            points[x, y].Height = 16;
                            Canvas.SetTop(points[x, y], (y * 20) - 1);
                            Canvas.SetLeft(points[x, y], (x * 20) - 1);
                            break;
                        case PointState.Player1Wall:
                            points[x, y].Fill = Brushes.Red;
                            points[x, y].Stroke = Brushes.Red;
                            break;
                        case PointState.Player2Wall:
                            points[x, y].Fill = Brushes.Blue;
                            points[x, y].Stroke = Brushes.Blue;
                            break;
                    }

                    //switch (map[x, y])
                    //{
                    //    case PointState.Player1:
                    //    case PointState.Player1Wall:
                    //        if ((map[x, (y + 1) % 30] == PointState.Player1) || (map[x, (y + 1) % 30] == PointState.Player1Wall))
                    //        {
                    //            Vlines[x, y].Stroke = Brushes.Red;
                    //            Vlines[x, y].StrokeThickness = 3;
                    //        }
                    //        if ((map[(x + 1) % 30, y] == PointState.Player1) || (map[(x + 1) % 30, y] == PointState.Player1Wall))
                    //        {
                    //            Hlines[x, y].Stroke = Brushes.Red;
                    //            Hlines[x, y].StrokeThickness = 3;
                    //        }
                    //        break;
                    //    case PointState.Player2:
                    //    case PointState.Player2Wall:
                    //        if ((map[x, (y + 1) % 30] == PointState.Player2) || (map[x, (y + 1) % 30] == PointState.Player2Wall))
                    //        {
                    //            Vlines[x, y].Stroke = Brushes.Aqua;
                    //            Vlines[x, y].StrokeThickness = 3;
                    //        }
                    //        if ((map[(x + 1) % 30, y] == PointState.Player2) || (map[(x + 1) % 30, y] == PointState.Player2Wall))
                    //        {
                    //            Hlines[x, y].Stroke = Brushes.Aqua;
                    //            Hlines[x, y].StrokeThickness = 3;
                    //        }
                    //        break;
                    //    default:
                    //        Vlines[x, y].Stroke = Brushes.Black;
                    //        Vlines[x, y].StrokeThickness = 1;
                    //        Hlines[x, y].Stroke = Brushes.Black;
                    //        Hlines[x, y].StrokeThickness = 1;
                    //        break;

                    //}
                }
            }

            lblCurrentPlayer.Content = "Current Player: " + (currentPlayer + 1).ToString();
        }

        private void resetMap()
        {
            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    map[x, y] = PointState.Clear;
                    points[x, y].Fill = Brushes.DarkGray;
                    points[x, y].Stroke = Brushes.Black;
                    Vlines[x, y].Stroke = Brushes.Black;
                    Vlines[x, y].StrokeThickness = 1;
                    Hlines[x, y].Stroke = Brushes.Black;
                    Hlines[x, y].StrokeThickness = 1;
                }
            }

            int startX = rng.Next(30);
            int startY = rng.Next(30);
            map[startX, startY] = PointState.Player1;

            startX = rng.Next(30);
            startY = rng.Next(30);
            map[startX, startY] = PointState.Player2;

            currentPlayer = rng.Next(2);
        }

        private void createVLines()
        {
            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    Line temp = new Line();
                    temp.Stroke = Brushes.Black;
                    temp.StrokeThickness = 1;
                    temp.X1 = (x * 20) + 7;
                    temp.X2 = temp.X1;
                    temp.Y1 = (y * 20) + 7;
                    temp.Y2 = temp.Y1 + 20;
                    Vlines[x, y] = temp;

                    gridSpace.Children.Add(temp);
                }
            }
        }

        private void createHLines()
        {
            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    Line temp = new Line();
                    temp.Stroke = Brushes.Black;
                    temp.StrokeThickness = 1;
                    temp.X1 = (x * 20) + 7;
                    temp.X2 = temp.X1 + 20;
                    temp.Y1 = (y * 20) + 7;
                    temp.Y2 = temp.Y1;
                    Hlines[x, y] = temp;

                    gridSpace.Children.Add(temp);
                }
            }
        }

        private void createVectorDots()
        {
            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    Ellipse temp = new Ellipse();
                    temp.Stroke = Brushes.Black;
                    temp.StrokeThickness = 1;
                    temp.Fill = Brushes.DarkGray;
                    temp.Width = 8;
                    temp.Height = 8;
                    Canvas.SetTop(temp, (y * 20) + 3);
                    Canvas.SetLeft(temp, (x * 20) + 3);
                    points[x, y] = temp;

                    gridSpace.Children.Add(points[x, y]);
                }
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            btnReset.IsEnabled = false;

            worker.RunWorkerAsync();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            btnStop.IsEnabled = false;
            worker.CancelAsync();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            resetMap();
            repaintMap();
        }

        private void btnPlayer1_Click(object sender, RoutedEventArgs e)
        {
            players[0] = SelectBot("Player 1");
            btnPlayer1.IsEnabled = false;
            btnPlayer2.IsEnabled = true;
            lblPlayer1Name.Content = "Player 1: " + players[0].ToString();
        }

        private Player SelectBot(string player)
        {
            BotTypeWindow window = new BotTypeWindow();
            window.WhichPlayer = player;
            window.ShowDialog();

            return window.Player;
        }

        private void btnPlayer2_Click(object sender, RoutedEventArgs e)
        {
            players[1] = SelectBot("Player 2");
            btnPlayer2.IsEnabled = false;
            btnStart.IsEnabled = true;
            btnReset.IsEnabled = true;
            lblPlayer2Name.Content = "Player 2: " + players[1].ToString();
        }
    }
}
