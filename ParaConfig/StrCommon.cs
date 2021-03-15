using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ParaConfig
{
    public class StrCommon
    {
        /* StrToHex */
        /// <summary>
        /// //返回处理后的十六进制字符串
        /// </summary>
        /// <param name="mStr"></param>
        /// <returns></returns>
        public static string StrToHex(string mStr)
        {
            return BitConverter.ToString(ASCIIEncoding.Default.GetBytes(mStr)).Replace("-", "");
        }

        /// <summary>
        /// // 返回十六进制代表的字符串
        /// </summary>
        /// <param name="mHex"></param>
        /// <returns></returns>
        public static string HexToStr(string mHex)
        {
            mHex = mHex.Replace(" ", "");
            if (mHex.Length <= 0) return "";
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            return ASCIIEncoding.Default.GetString(vBytes).Replace(" ", "").Replace("\r\n", "");
        }

        /// <summary>
        /// 返回十六进制代表的字符串  反序
        /// </summary>
        /// <param name="mHex"></param>
        /// <returns></returns>
        public static string HexToStrReverseOrder(string mHex)
        {
            mHex = mHex.Replace(" ", "");
            if (mHex.Length <= 0) return "";
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            Array.Reverse(vBytes);//翻转排序
            return ASCIIEncoding.Default.GetString(vBytes).Replace(" ", "").Replace("\n\r", "");
        }

        /// <summary>
        /// 根据十六进制的字符串转换成字节数组
        /// </summary>
        /// <param name="shex"></param>
        /// <returns></returns>
        public static byte[] GetByteArrayByHexStr(string shex)
        {
            shex = shex.Replace(" ", "");
            shex = shex.ToCharArray().Aggregate("", (result, c) => result += ((!string.IsNullOrEmpty(result) && (result.Length + 1) % 3 == 0) ? " " : "") + c.ToString());
            string[] ssArray = shex.Split(' ');
            List<byte> bytList = new List<byte>();
            foreach (var s in ssArray)//将十六进制的字符串转换成数值
            {
                bytList.Add(Convert.ToByte(s, 16));
            }
            return bytList.ToArray();//返回字节数组  
        }
        /// <summary>
        /// 根据字节数组转换成十六进制的字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GetHexStrByByteArray(byte[] bytes)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();//清除字符串构造器的内容
            foreach (byte b in bytes)
            {
                stringBuilder.Append(b.ToString("X2") + " ");//一个字节一个字节的处理
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 正则截取起止位中间的数据
        /// </summary>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static string MidStrMain(string source, string start, string end)
        {
            if (start.Contains('-'))
            {
                var ret = "";
                foreach (string item in start.Split('-'))
                {
                    if (source.Contains(item))
                    {
                        ret = MidStrEx_New(source, item, end);
                    }
                }

                return !string.IsNullOrEmpty(ret) ? ret : source;
            }
            else
            {
                return MidStrEx_New(source, start, end);
            }
        }
        /// <summary>
        /// 正则截取起止位中间的数据
        /// </summary>
        /// <param name="sourse"></param>
        /// <param name="startstr"></param>
        /// <param name="endstr"></param>
        /// <returns></returns>
        public static string MidStrEx_New(string sourse, string startstr, string endstr)
        {
            Regex rg = new Regex("(?<=(" + startstr + "))[.\\s\\S]*?(?=(" + endstr + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            return rg.Match(sourse).Value;
        }

        public static Dictionary<int, string> GetDictByHexStr(string shex)
        {
            Dictionary<int, string> dicRet = new Dictionary<int, string>();
            shex = shex.Replace(" ", "");
            shex = shex.ToCharArray().Aggregate("", (result, c) => result += ((!string.IsNullOrEmpty(result) && (result.Length + 1) % 3 == 0) ? " " : "") + c.ToString());
            string[] ssArray = shex.Split(' ');
            for (int i = 0; i < ssArray.Length; i++)
            {
                dicRet.Add(i, ssArray[i]);
            }
            return dicRet;
        }
    }
}
