using System;
using System.Windows.Controls;
using System.IO.Ports;
using System.Windows.Threading;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using System.Windows;

using System.Windows.Media;

using digitalOscilloscope;

delegate void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e);
static class COMPortSelecetor
{
    public static SerialPort port;

    private static MainWindow mainWindow;
    private static ComboBox SerialPortComboBox;
    private static Button ConnectButton;
    private static Button StartButton;
    private static DispatcherTimer _timer;

    private static bool isConnected = false;

    // ***************************************************************************************************************** //

    private static int BAUDRATE;

    private static ComboBox BaudRateComboBox;

    // ***************************************************************************************************************** //

    private static DataReceivedHandler data_received_handle_;

    public static void Init()
    {
        mainWindow = (MainWindow)App.Current.MainWindow;
        SerialPortComboBox = mainWindow.SerialPortComboBox;
        ConnectButton = mainWindow.ConnectButton;
        StartButton = mainWindow.StartButton;
        SerialPortComboBox.SelectedIndex = 0;

        // ***************************************************************************************************************** //

        BaudRateComboBox = mainWindow.BaudRateComboBox;

        // ***************************************************************************************************************** //

        SetTimer();
    }

    public static void SetBaudrate(int baudrate)
    {
        BAUDRATE = baudrate;
    }

    public static void PushConnectButton()
    {
        if (IsConnected())
        {
            DisconnectPort();
        }

        else
        {
            ConnectPort();
        }
    }

    public static void ConnectPort()
    {
        if (isConnected) return;

        BAUDRATE = Convert.ToInt32(BaudRateComboBox.Text);

        UpdateSerialPortComboBox();
        string port_name = SerialPortComboBox.Text;
        if (String.IsNullOrEmpty(port_name)) return;
        port = new SerialPort(port_name, BAUDRATE, Parity.None, 8, StopBits.One);

        try
        {
            port.Open();
            port.DtrEnable = true;
            port.RtsEnable = true;
            isConnected = true;
            ConnectButton.Content = "Disconnect";
            ConnectButton.Foreground = Brushes.Red;
            MessageBox.Show("Connected", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            port.DataReceived += new SerialDataReceivedEventHandler(data_received_handle_);

            StartButton.IsEnabled = true;
        }
        catch (global::System.Exception)
        {
            MessageBox.Show("Please Choose Different Com Port", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static void DisconnectPort()
    {
        if (isConnected)
        {
            port.Close();
            port.Dispose();
            isConnected = false;
            ConnectButton.Content = "Connect";
            ConnectButton.Foreground = Brushes.Green;
            MessageBox.Show("Disconnected", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

            StartButton.IsEnabled = false;
        }
    }

    public static bool IsConnected()
    {
        return isConnected;
    }

    public static void SetDataReceivedHandle(DataReceivedHandler data_received_handle)
    {
        data_received_handle_ = data_received_handle;
    }

    private static void UpdateSerialPortComboBox()
    {
        string prev_selected_port = "";
        if (SerialPortComboBox.SelectedItem != null) prev_selected_port = SerialPortComboBox.SelectedItem.ToString();

        string[] port_list = SerialPort.GetPortNames();
        SerialPortComboBox.Items.Clear();
        foreach (var i in port_list) SerialPortComboBox.Items.Add(i);

        for (int i = 0; i < SerialPortComboBox.Items.Count; i++)
        {
            if (SerialPortComboBox.Items[i].ToString() == prev_selected_port) SerialPortComboBox.SelectedIndex = i;
        }

        if (SerialPortComboBox.Items.Count <= 1) SerialPortComboBox.SelectedIndex = 0;
    }

    private static void SetTimer()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = new TimeSpan(0, 0, 1);
        _timer.Tick += new EventHandler(OnTimeEvent);
        _timer.Start();
        mainWindow.Closing += new CancelEventHandler(StopTimer);
    }

    private static void OnTimeEvent(Object source, EventArgs e)
    {
        UpdateSerialPortComboBox();
    }

    private static void StopTimer(object sender, CancelEventArgs e)
    {
        _timer.Stop();
    }
}