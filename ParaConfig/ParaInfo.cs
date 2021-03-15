using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParaConfig
{
    /// <summary>
    /// 配置参数信息
    /// </summary>
    public class ParaInfo
    {
        /// <summary>
        /// 参数配置Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// COM端口
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 波特率 9600、14400、19200...
        /// </summary>
        public int Baudrate { get; set; }
        /// <summary>
        /// 数据位-5、6、7、8...
        /// Five = 5,Six = 6,Sevent = 7,Eight = 8
        /// </summary>
        public int DataBit { get; set; }
        /// <summary>
        /// 停止位 1、2、1.5
        /// None=0,One=1,Two=2,OnePointFive=3
        /// </summary>
        public int StopBit { get; set; }
        /// <summary>
        /// 校验位 None-0,Odd-1,Even-2,Mark-3,Space-4
        /// </summary>
        public int CheckBit { get; set; }

        /// <summary>
        /// 数据接收方式-  0：十六进制、1：字符方式
        /// </summary>
        public int DataReceiveMode { get; set; }
        /// <summary>
        /// 接收数据总长度
        /// </summary>
        public int ReceiveDataLength { get; set; }

        /// <summary>
        /// 重量数据长度
        /// </summary>
        public int WeightDataLength { get; set; }
        /// <summary>
        /// 重量数据起始位置
        /// </summary>
        public int WeightDataStartBit { get; set; }
        /// <summary>
        /// 重量数据排列方式-除去起止位之外的重量数据
        /// 0 正序，1 反序
        /// </summary>
        public int WeightDataOrderMode { get; set; }
        /// <summary>
        /// 正负单位起始位
        /// </summary>
        public int PlusMinusUnitStartBit { get; set; }
        /// <summary>
        /// 起始标志
        /// </summary>
        public string StartMark { get; set; }
        /// <summary>
        /// 结束标志
        /// </summary>
        public string EndMark { get; set; }

        /// <summary>
        /// 单位转换 * 1000、*000.1
        /// </summary>
        public double ConvertRatio { get; set; }

    }
}
