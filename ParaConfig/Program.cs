using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParaConfig
{
    class Program
    {
        static void Main(string[] args)
        {
            TransHelper trans = new TransHelper();
            trans.setWeightResultValue += new setResultValue(GetValue);
            Console.ReadLine();
        }
        static void GetValue(double retValue)
        {
            bool isOk = TransHelper.GetIsOk(retValue);
            Console.WriteLine(isOk);
            if (isOk)
            {
                Console.WriteLine(retValue);
            }
        }
    }
}
