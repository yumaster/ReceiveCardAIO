using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace ParaConfig
{
    public class SerialPortClient
    {

        #region Private Fields

        private SerialPort _serialPort;
        private string _portName = "";
        private int _baudRate = 115200;
        private StopBits _stopBits = StopBits.One;
        private Parity _parity = Parity.None;
        private DataBits _dataBits = DataBits.Eight;

        // 读/写错误状态变量
        private bool gotReadWriteError = true;

        // 串行端口读取器任务
        private Thread reader;
        private CancellationTokenSource readerCts;
        // 串行端口连接监视程序
        private Thread connectionWatcher;
        private CancellationTokenSource connectionWatcherCts;

        private object accessLock = new object();
        private bool disconnectRequested = false;

        #endregion

        #region Public Events

        /// <summary>
        /// 连接状态改变事件
        /// </summary>
        public delegate void ConnectionStatusChangedEventHandler(object sender, ConnectionStatusChangedEventArgs args);

        /// <summary>
        /// 连接状态更改时发生
        /// </summary>
        public event ConnectionStatusChangedEventHandler ConnectionStatusChanged;

        /// <summary>
        /// 消息接收事件
        /// </summary>
        //public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs args);

        /// <summary>
        /// 收到消息时发生
        /// </summary>
        //public event MessageReceivedEventHandler MessageReceived;

        public event Action<List<byte>> MessageReceived;

        #endregion

        #region Public Members
        public SerialPortClient()
        {
            connectionWatcherCts = new CancellationTokenSource();
            readerCts = new CancellationTokenSource();
        }

        /// <summary>
        /// 连接到串口
        /// </summary>
        public bool Connect()
        {
            if (disconnectRequested)
                return false;
            lock (accessLock)
            {
                Disconnect();
                Open();
                connectionWatcherCts = new CancellationTokenSource();
                connectionWatcher = new Thread(ConnectionWatcherTask);
                connectionWatcher.Start(connectionWatcherCts.Token);
            }
            return IsConnected;
        }

        /// <summary>
        /// 断开串口
        /// </summary>
        public void Disconnect()
        {
            if (disconnectRequested)
                return;
            disconnectRequested = true;
            Close();
            lock (accessLock)
            {
                if (connectionWatcher != null)
                {
                    if (!connectionWatcher.Join(5000))
                        connectionWatcherCts.Cancel();
                    connectionWatcher = null;
                }
                disconnectRequested = false;
            }
        }

        /// <summary>
        /// 获取一个值，该值指示串行端口是否已连接。
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        public bool IsConnected
        {
            get { return _serialPort != null && !gotReadWriteError && !disconnectRequested; }
        }

        /// <summary>
        /// 设置串行端口选项。
        /// </summary>
        /// <param name="portName">Portname.</param>
        /// <param name="baudRate">Baudrate.</param>
        /// <param name="stopBits">Stopbits.</param>
        /// <param name="parity">Parity.</param>
        /// <param name="dataBits">Databits.</param>
        public void SetPort(string portName, string baudRate, string parity, string dataBits, string stopBits)
        {
            // 更改参数请求
            // 立即考虑新的连接参数
            // (不要使用ConnectionWatcher，否则会发生奇怪的事情!)
            _portName = portName;
            _baudRate = int.Parse(baudRate);
            _parity = (Parity)Enum.Parse(typeof(Parity), parity);
            _dataBits = (DataBits)Enum.Parse(typeof(DataBits), dataBits);
            _stopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits);


            if (IsConnected)
            {
                Connect();      // 立即考虑新的连接参数
            }
            LogDebug(string.Format("Port parameters changed (port name {0} / baudrate {1} / stopbits {2} / parity {3} / databits {4})", portName, baudRate, stopBits, parity, dataBits));
        }

        /// <summary>
        /// 发送消息。
        /// </summary>
        /// <returns><c>true</c>, 如果消息已发送, <c>false</c> 否则.</returns>
        /// <param name="message">Message.</param>
        public bool SendMessage(byte[] message)
        {
            bool success = false;
            if (IsConnected)
            {
                try
                {
                    _serialPort.Write(message, 0, message.Length);
                    success = true;
                    LogDebug(BitConverter.ToString(message));
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
            return success;
        }

        #endregion

        #region 私有成员

        #region 串行端口处理

        private bool Open()
        {
            bool success = false;
            lock (accessLock)
            {
                Close();
                try
                {
                    bool tryOpen = true;
                    var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
                    if (!isWindows)
                    {
                        tryOpen = (tryOpen && System.IO.File.Exists(_portName));
                    }
                    if (tryOpen)
                    {
                        _serialPort = new SerialPort();
                        _serialPort.ErrorReceived += HandleErrorReceived;
                        _serialPort.PortName = _portName;
                        _serialPort.BaudRate = _baudRate;
                        _serialPort.StopBits = _stopBits;
                        _serialPort.Parity = _parity;
                        _serialPort.DataBits = (int)_dataBits;

                        // 我们没有使用接收到serialPort.DataReceived接收数据的事件，因为这在Linux/Mono下不起作用.
                        // 我们改用readerTask（见下文）。
                        _serialPort.Open();
                        success = true;
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                    Close();
                }
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    gotReadWriteError = false;
                    // 启动读任务
                    readerCts = new CancellationTokenSource();
                    reader = new Thread(ReaderTask);
                    reader.Start(readerCts.Token);
                    OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs(true));
                }
            }
            return success;
        }

        private void Close()
        {
            lock (accessLock)
            {
                // 停止读任务
                if (reader != null)
                {
                    if (!reader.Join(5000))
                        readerCts.Cancel();
                    reader = null;
                }
                if (_serialPort != null)
                {
                    _serialPort.ErrorReceived -= HandleErrorReceived;
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs(false));
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                }
                gotReadWriteError = true;
            }
        }

        private void HandleErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            LogError(e.EventType);
        }

        #endregion

        #region 后台任务
        /// <summary>
        /// 数据仓库
        /// </summary>
        List<byte> datapool = new List<byte>();//存放接收的所有字节
        private void ReaderTask(object data)
        {
            var ct = (CancellationToken)data;
            while (IsConnected && !ct.IsCancellationRequested)
            {
                int msglen = 0;
                //
                try
                {
                    msglen = _serialPort.BytesToRead;
                    if (msglen > 0)
                    {
                        byte[] message = new byte[msglen];
                        //
                        int readbytes = 0;
                        while (_serialPort.Read(message, readbytes, msglen - readbytes) <= 0)
                            ; // noop
                        if (MessageReceived != null)
                        {
                            //OnMessageReceived(new MessageReceivedEventArgs(message));
                            datapool.AddRange(message);
                            OnMessageReceived(datapool);
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                    gotReadWriteError = true;
                    Thread.Sleep(1000);
                }
            }
        }

        private void ConnectionWatcherTask(object data)
        {
            var ct = (CancellationToken)data;
            // 此任务负责自动重新连接接口
            // 当连接断开或发生I/O错误时
            while (!disconnectRequested && !ct.IsCancellationRequested)
            {
                if (gotReadWriteError)
                {
                    try
                    {
                        Close();
                        // 请等待1秒钟，然后重新连接
                        Thread.Sleep(1000);
                        if (!disconnectRequested)
                        {
                            try
                            {
                                Open();
                            }
                            catch (Exception e)
                            {
                                LogError(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e);
                    }
                }
                if (!disconnectRequested)
                    Thread.Sleep(1000);
            }
        }

        private void LogDebug(string message)
        {
            Debug.WriteLine(message);
        }

        private void LogError(Exception ex)
        {
            Debug.WriteLine(ex);
        }

        private void LogError(SerialError error)
        {
            Debug.WriteLine("SerialPort ErrorReceived: {0}", error);
        }

        #endregion

        #region 事件引发

        /// <summary>
        /// 引发已连接状态更改事件。
        /// </summary>
        /// <param name="args">参数.</param>
        protected virtual void OnConnectionStatusChanged(ConnectionStatusChangedEventArgs args)
        {
            LogDebug(args.Connected.ToString());
            if (ConnectionStatusChanged != null)
                ConnectionStatusChanged(this, args);
        }

        /// <summary>
        /// 引发消息接收事件。
        /// </summary>
        /// <param name="args">参数.</param>
        protected virtual void OnMessageReceived(List<byte> data)
        {
            //LogDebug(BitConverter.ToString(args.Data));
            if (MessageReceived != null)
                //MessageReceived(this, args);
                MessageReceived(data);
        }
        #endregion

        #endregion

    }
    #region 波特率、数据位的枚举
    /// <summary>
    /// 串口数据位列表（5,6,7,8）
    /// </summary>
    public enum DataBits : int
    {
        Five = 5,
        Six = 6,
        Sevent = 7,
        Eight = 8
    }

    /// <summary>
    /// 串口波特率列表。
    /// 75,110,150,300,600,1200,2400,4800,9600,14400,19200,28800,38400,56000,57600,
    /// 115200,128000,230400,256000
    /// </summary>
    public enum BaudRates : int
    {
        BR_75 = 75,
        BR_110 = 110,
        BR_150 = 150,
        BR_300 = 300,
        BR_600 = 600,
        BR_1200 = 1200,
        BR_2400 = 2400,
        BR_4800 = 4800,
        BR_9600 = 9600,
        BR_14400 = 14400,
        BR_19200 = 19200,
        BR_28800 = 28800,
        BR_38400 = 38400,
        BR_56000 = 56000,
        BR_57600 = 57600,
        BR_115200 = 115200,
        BR_128000 = 128000,
        BR_230400 = 230400,
        BR_256000 = 256000
    }
    #endregion
}
