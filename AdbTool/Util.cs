using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AdbTool
{
    public class Util
    {
        /// <summary>
        /// ADB 路径
        /// </summary>
        public static string AdbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb", "adb.exe");


    }
}
