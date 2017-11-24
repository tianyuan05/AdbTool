using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdbTool
{
    public class ADBToolKit
    {
        /// <summary>
        /// 创建一个 Process 实例
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static Process CreateInstance(string filePath, string parameter)
        {
            Process p = new Process();
            p.StartInfo.FileName = filePath;           //设定程序名   
            p.StartInfo.Arguments = $"{parameter.Trim()}";  //设定程式执行參數   
            p.StartInfo.UseShellExecute = false;        //关闭Shell的使用   
            p.StartInfo.RedirectStandardInput = true;   //重定向标准输入   
            p.StartInfo.RedirectStandardOutput = true;  //重定向标准输出   
            p.StartInfo.RedirectStandardError = true;   //重定向错误输出   
            p.StartInfo.CreateNoWindow = false;          //设置不显示窗口  

            return p;
        }


        public static string ADBCommandOutput(Process process)
        {
            StringBuilder sb = new StringBuilder();
            Thread outputThread = new Thread(new ThreadStart(delegate
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    while (!reader.EndOfStream)
                    {
                        sb.AppendLine(reader.ReadLine());
                    }
                }
            }));

            Thread errorThread = new Thread(new ThreadStart(delegate
            {
                using (StreamReader reader = process.StandardError)
                {
                    while (!reader.EndOfStream)
                    {
                        sb.AppendLine(reader.ReadLine());
                    }
                }
            }));

            outputThread.Start();
            errorThread.Start();

            try
            {
                outputThread.Join();
            }
            catch (Exception)
            {

            }

            try
            {
                errorThread.Join();
            }
            catch (Exception)
            {

            }
            process.WaitForExit();

            return sb.ToString();
        }

    }
}
