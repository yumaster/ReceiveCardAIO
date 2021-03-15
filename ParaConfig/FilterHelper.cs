using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParaConfig
{
    public class FilterHelper
    {
        /// <summary>
        /// 限幅防抖滤波法
        /// </summary>
        /// <param name="ADNum"采样值></param>
        /// RANG：幅度  LPNUM :测试计数
        /// <returns></returns>
        private const double RANGE = 0.002;
        private const int LPNUM = 5;
        private static double currentValue = 0;
        private static int tmpCount = 0;
        private static bool isFirstLF = true;

        private static int okCount = 0;//稳定了多少次

        public static bool LimitFilterAD(double ADNum)
        {
            if (isFirstLF)
            {
                isFirstLF = false;
                currentValue = ADNum;
                //Console.WriteLine("第一次计数：" + currentValue);
            }
            if (Math.Abs(ADNum - currentValue) > RANGE)//如果平均值大于幅度
            {
                if (tmpCount++ > LPNUM)//临时数是否大于测试数，如果大于测试数，进行下一次过滤
                {
                    isFirstLF = true;    // 初始化滑动平均值
                    tmpCount = 0;
                    currentValue = ADNum;
                    //Console.WriteLine("又一次重新计数，当前值：" + currentValue);
                }
                //Console.WriteLine("不稳定,当前值：" + currentValue + "," + "差距：" + Math.Abs(ADNum - currentValue));
                okCount = 0;
                return false;
            }
            else//如果小于幅度，说明稳定
            {
                //Console.WriteLine("稳定：" + currentValue);
                if (okCount++ > 150)
                {
                    return true;
                }
                else
                {
                    return false;
                }
                //return true;
            }
        }

        /// <summary>
        ///滑动平均滤波算法（递推平均滤波法）
        /// </summary>
        /// <param name="ADNum"为获得的AD数></param>
        /// GN为数组value_buf[]的元素个数,该函数主要被调用，利用参数的数组传值
        /// <returns></returns>   
        private const int GN = 12;
        private static int filterPtr = 0;
        private static bool isFirstGF = true;
        public static double gSum = 0;
        static double[] gbuf = new double[GN];

        public static double GlideFilterAD(double ADNum)
        {
            if (isFirstGF)
            {
                isFirstGF = false;
                for (int i = 0; i < GN; i++)
                    gbuf[i] = ADNum;
                gSum = ADNum * GN;
                return ADNum;
            }
            else
            {
                gSum += ADNum - gbuf[filterPtr];
                gbuf[filterPtr++] = ADNum;
                if (filterPtr == GN)
                    filterPtr = 0;    //先进先出，再求平均值
                return (gSum / GN);
            }
        }
    }
}