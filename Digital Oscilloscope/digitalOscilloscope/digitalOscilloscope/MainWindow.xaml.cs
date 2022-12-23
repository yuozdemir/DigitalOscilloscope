using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;
using System.Windows.Threading;
using System.IO;
using System.Runtime.CompilerServices;
using System.ComponentModel;

using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Configurations;
using LiveCharts.Wpf;


namespace digitalOscilloscope
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool RequestStatus = false;
        public bool flag = false;
        public int[] data = { 0, 0, 0, 0 };
        public int data_l = 0;
        public byte[] dataBytes;

        delegate void serialCalback(string val);

        CancellationTokenSource _tokenSource = null;

        DataModel.DataModel dataModel = new DataModel.DataModel();

        private static BlockingCollection<byte[]> data_s = new BlockingCollection<byte[]>(boundedCapacity: 3);

        public MainWindow()
        {
            InitializeComponent();

            dataModel.ChartValues1 = new ChartValues<ObservablePoint>();
            dataModel.ChartValues2 = new ChartValues<ObservablePoint>();
            dataModel.ChartValues3 = new ChartValues<ObservablePoint>();
            dataModel.ChartValues4 = new ChartValues<ObservablePoint>();

            ChartValues<ObservablePoint>[] observablePoints = { dataModel.ChartValues1, dataModel.ChartValues2, dataModel.ChartValues3, dataModel.ChartValues4 };

            dataModel.XMax = 360;
            dataModel.XMin = 0;

            foreach (var item in observablePoints)
            {
                for (double x = dataModel.XMin; x <= dataModel.XMax; x++)
                {
                    var point = new ObservablePoint()
                    {
                        X = x,
                        Y = 0.0
                    };
                    item.Add(point);
                }
            }

            dataModel.DataMapper = new CartesianMapper<ObservablePoint>()
                .X(point => point.X)
                .Y(point => point.Y)
                .Stroke(point => point.Y > 0.3 ? Brushes.Red : Brushes.LightGreen)
                .Fill(point => point.Y > 0.3 ? Brushes.Red : Brushes.LightGreen);

            var progressReporter = new Progress<double>(newValue => ShiftValuesToTheLeft(newValue, observablePoints, CancellationToken.None));

            Task.Run(async () => await StartGraphGenerator(progressReporter, observablePoints, CancellationToken.None));

            COMPortSelecetor.Init();
            COMPortSelecetor.SetDataReceivedHandle(aDataReceivedHandler);

            /***********************************************************/

            this.DataContext = dataModel;
        }

        private void ShiftValuesToTheLeft(double newValue, ChartValues<ObservablePoint>[] chartValues, CancellationToken cancellationToken)
        {
            int i = 0;
            foreach (var item in chartValues)
            {
                for (var index = 0; index < item.Count - 1; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ObservablePoint currentPoint = item[index];
                    ObservablePoint nextPoint = item[index + 1];
                    currentPoint.X = nextPoint.X;
                    currentPoint.Y = nextPoint.Y;
                }

                ObservablePoint newPoint = item[item.Count - 1];
                newPoint.X = newValue;
                newPoint.Y = data[i];
                i++;

                dataModel.XMax = newValue;
                dataModel.XMin = item[0].X;
            }
        }

        public async Task StartGraphGenerator(IProgress<double> progressReporter, ChartValues<ObservablePoint>[] chartValues, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (flag)
                {
                    foreach (var item in chartValues)
                    {
                        ObservablePoint newPoint = item[item.Count - 1];
                        double newXValue = newPoint.X + 1;
                        progressReporter.Report(newXValue);
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
                    }
                }
            }
        }

        public void GetPackage(int milliseconds, CancellationToken token)
        {
            while (RequestStatus)
            {
                SendPackage(10);

                data_l = COMPortSelecetor.port.ReadByte();

                dataBytes = new byte[data_l];

                for (int i = 0; i < data_l; i++)
                {
                    dataBytes[i] = Convert.ToByte(COMPortSelecetor.port.ReadByte());
                }

                data_s.Add(dataBytes);

                Task.Delay(milliseconds).Wait();
            }
        }

        public void SolitionPackage(int millisecond, CancellationToken token)
        {
            foreach (var item in data_s.GetConsumingEnumerable())
            {
                data[0] = item[0];
                dataModel.TextData1 = data[0];

                data[1] = item[1];
                dataModel.TextData2 = data[1];

                data[2] = item[2];
                dataModel.TextData3 = data[2];

                data[3] = item[3];
                dataModel.TextData4 = data[3];
            }
        }

        private void SendPackage(int data)
        {
            dataBytes = new byte[1];
            dataBytes = BitConverter.GetBytes(data);
            COMPortSelecetor.port.Write(dataBytes, 0, 1);
        }

        // ***************************************************************************************************************** //
        // ***************************************************************************************************************** //
        // ***************************************************************************************************************** //

        public void EnableButton()
        {
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = true;
            DataRequestStartButton.IsEnabled = true;
            DataRequestStopButton.IsEnabled = true;
            DataSendButton.IsEnabled = true;
        }

        public void DisableButton()
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            DataRequestStartButton.IsEnabled = false;
            DataRequestStopButton.IsEnabled = false;
            DataSendButton.IsEnabled = false;
        }

        // ***************************************************************************************************************** //
        // ***************************************************************************************************************** //
        // ***************************************************************************************************************** //

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // ***************************************************************************************************************** //
        // ***************************************************************************************************************** //
        // ***************************************************************************************************************** //

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (StopButton.IsEnabled == true)
            {
                MessageBox.Show("Please Press The Stop Button First", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            else if (DataRequestStopButton.IsEnabled == true)
            {
                MessageBox.Show("Please Press The Data Request Stop Button First", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            else
            {
                COMPortSelecetor.PushConnectButton();

                if (COMPortSelecetor.IsConnected())
                {
                    StartButton.IsEnabled = true;
                    StartButton.Foreground = Brushes.Green;

                    SettingsButton.IsEnabled = false;
                    SettingsButton.Foreground = Brushes.Gray;

                    LiveGraphButton.IsEnabled = true;
                    LiveGraphButton.Foreground = Brushes.Orange;
                }

                else
                {
                    StartButton.IsEnabled = false;
                    StartButton.Foreground = Brushes.Gray;

                    SettingsButton.IsEnabled = true;
                    SettingsButton.Foreground = Brushes.Green;

                    LiveGraphButton.IsEnabled = false;
                    LiveGraphButton.Foreground = Brushes.LightBlue;
                }
            }
        }

        private static void aDataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {

        }

        private void LiveGraphButton_Click(object sender, RoutedEventArgs e)
        {
            Panel.SetZIndex(MainPanel, 0);
            Panel.SetZIndex(GraphPanel, 1);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            StartButton.Foreground = Brushes.Gray;

            StopButton.IsEnabled = true;
            StopButton.Foreground = Brushes.Green;

            DataRequestStartButton.IsEnabled = true;
            DataRequestStartButton.Foreground = Brushes.Orange;

            DataSendButton.IsEnabled = true;
            DataSendButton.Foreground = Brushes.Orange;

            LiveGraphButton.IsEnabled = false;
            LiveGraphButton.Foreground = Brushes.LightBlue;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = true;
            StartButton.Foreground = Brushes.Green;

            StopButton.IsEnabled = false;
            StopButton.Foreground = Brushes.Gray;

            DataRequestStartButton.IsEnabled = false;
            DataRequestStartButton.Foreground = Brushes.LightBlue;

            DataRequestStopButton.IsEnabled = false;
            DataRequestStopButton.Foreground = Brushes.LightBlue;

            DataSendButton.IsEnabled = false;
            DataSendButton.Foreground = Brushes.LightBlue;

            LiveGraphButton.IsEnabled = true;
            LiveGraphButton.Foreground = Brushes.Orange;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Panel.SetZIndex(Settings_Menu, 10);
        }

        private void SettingsMenuSaveButton_Click(object sender, RoutedEventArgs e)
        {
            BaudRateComboBox.SelectedItem = BaudRateComboBox.SelectedIndex;
        }

        private void SettingsMenuCloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Settings_Menu.Height == 0)
            {
                Panel.SetZIndex(Settings_Menu, 0);
            }
        }

        private void DataSendButton_Click(object sender, RoutedEventArgs e)
        {
            COMPortSelecetor.port.WriteLine(DataText.Text);
        }

        private async void DataRequestStartButton_Click(object sender, RoutedEventArgs e)
        {
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            int dataTime = Convert.ToInt32(this.DataTimeComboBox.Text);

            StopButton.IsEnabled = false;
            StopButton.Foreground = Brushes.Gray;

            DataRequestStartButton.IsEnabled = false;
            DataRequestStartButton.Foreground = Brushes.LightBlue;

            DataRequestStopButton.IsEnabled = true;
            DataRequestStopButton.Foreground = Brushes.Orange;

            RequestStatus = true;

            var getPackage = Task.Factory.StartNew(() => GetPackage(dataTime, token));
            var solitionPackage = Task.Factory.StartNew(() => SolitionPackage(dataTime, token));
        }

        private async void DataRequestStopButton_Click(object sender, RoutedEventArgs e)
        {
            StopButton.IsEnabled = true;
            StopButton.Foreground = Brushes.Green;

            DataRequestStartButton.IsEnabled = true;
            DataRequestStartButton.Foreground = Brushes.Orange;

            DataRequestStopButton.IsEnabled = false;
            DataRequestStopButton.Foreground = Brushes.LightBlue;

            RequestStatus = false;
        }

        // ***************************************************************************************************************** //
        // ***************************************************************************************************************** //
        // ***************************************************************************************************************** //

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            flag = true;

            Start_Button.IsEnabled = false;
            Start_Button.Foreground = Brushes.DarkCyan;

            Stop_Button.IsEnabled = true;
            Stop_Button.Foreground = Brushes.LightCyan;

            Send_Button.IsEnabled = true;
            Send_Button.Foreground = Brushes.LightCyan;

            MainMenu_Button.IsEnabled = false;
            MainMenu_Button.Foreground = Brushes.DarkCyan;
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            flag = false;

            Start_Button.IsEnabled = true;
            Start_Button.Foreground = Brushes.LightCyan;

            Stop_Button.IsEnabled = false;
            Stop_Button.Foreground = Brushes.DarkCyan;

            Send_Button.IsEnabled = false;
            Send_Button.Foreground = Brushes.DarkCyan;

            GraphReset_Button.IsEnabled = true;
            GraphReset_Button.Foreground = Brushes.LightCyan;

            MainMenu_Button.IsEnabled = true;
            MainMenu_Button.Foreground = Brushes.LightCyan;
        }

        private void Send_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void RequestStart_Button_Click(object sender, RoutedEventArgs e)
        {
            RequestStatus = true;

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            int dataTime = Convert.ToInt32(this.DataTimeComboBox.Text);

            RequestStart_Button.IsEnabled = false;
            RequestStart_Button.Foreground = Brushes.DarkCyan;

            RequestStop_Button.IsEnabled = true;
            RequestStop_Button.Foreground = Brushes.LightCyan;

            var getPackage = Task.Factory.StartNew(() => GetPackage(dataTime, token));
            var solitionPackage = Task.Factory.StartNew(() => SolitionPackage(dataTime, token));
        }

        private void RequestStop_Button_Click(object sender, RoutedEventArgs e)
        {
            RequestStart_Button.IsEnabled = true;
            RequestStart_Button.Foreground = Brushes.LightCyan;

            RequestStop_Button.IsEnabled = false;
            RequestStop_Button.Foreground = Brushes.DarkCyan;

            RequestStatus = false;
        }

        private void GraphReset_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            Panel.SetZIndex(MainPanel, 1);
            Panel.SetZIndex(GraphPanel, 0);
        }

        // ***************************************************************************************************************** //
        // ***************************************************************************************************************** //
        // ***************************************************************************************************************** //
    }
}
