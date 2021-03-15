using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParaConfig
{
    /// <summary>
    /// 连接状态改变事件
    /// </summary>
    public class ConnectionStatusChangedEventArgs
    {
        /// <summary>
        /// 连接状态
        /// </summary>
        public readonly bool Connected;

        /// <summary>
        /// 初始化<see cref=”的新实例SerialPortLib.ConnectionStatusChangedEventArgs“/>类。
        /// </summary>
        /// <param name="state">连接的状态（true=connected，false=not connected）。</param>
        public ConnectionStatusChangedEventArgs(bool state)
        {
            Connected = state;
        }
    }

    /// <summary>
    /// 消息接收到事件参数。
    /// </summary>
    public class MessageReceivedEventArgs
    {
        /// <summary>
        /// 数据
        /// </summary>
        public readonly byte[] Data;

        /// <summary>
        /// 初始化的新实例 <see cref="SerialPortLib.MessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="data">数据.</param>
        public MessageReceivedEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}
