using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AnwConnector.Util;
using SAPbobsCOM;

namespace AnwConnector.Common
{
    class Globle
    {
        public static readonly int SleepTime = Config.SleepTime * 1000;
        //public static readonly Company DiCompany = null;
        public static readonly Company DiCompany = SapDiHelper.GetCompany();
    }
}
