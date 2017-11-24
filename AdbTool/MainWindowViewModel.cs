using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AdbTool
{
    public class MainWindowViewModel : BindableBase
    {

        #region 字段

        private string command/*="adb shell"*/;

        private string result;

        #endregion

        #region 属性

        public string Command { get => command; set => SetProperty(ref command, value); }

        public string Result { get => result; set => SetProperty(ref result, value); }

        #endregion


        #region 构造器

        public MainWindowViewModel()
        {
            ExcuteCommand = new DelegateCommand(OnExcute);
        }


        #endregion

        #region DelegateCommand

        public DelegateCommand ExcuteCommand { get; private set; }


        #endregion
        void OnExcute()
        {
            if (!System.IO.File.Exists(Util.AdbPath))
            {
                MessageBox.Show("未找到ADB程序");
                return;
            }
            Process p = new Process();
            p.StartInfo.FileName = Util.AdbPath;           //设定程序名   
            p.StartInfo.Arguments = $"{command.Trim()}";    //设定程式执行參數   
            p.StartInfo.UseShellExecute = false;        //关闭Shell的使用   
            p.StartInfo.RedirectStandardInput = true;   //重定向标准输入   
            p.StartInfo.RedirectStandardOutput = true;  //重定向标准输出   
            p.StartInfo.RedirectStandardError = true;   //重定向错误输出   
            p.StartInfo.CreateNoWindow = true;          //设置不显示窗口   
            p.Start();

            StringBuilder builder = new StringBuilder();

            var rs = GetAdbCommnandResult(p);

            if (!string.IsNullOrWhiteSpace(rs))
                Result = rs;


            p.Close();
        }


        string GetAdbCommnandResult(Process process)
        {
            StringBuilder sb = new StringBuilder();

            Thread rsThread = new Thread(new ThreadStart(delegate
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    while (!reader.EndOfStream)
                    {
                        sb.AppendLine(reader.ReadLine());
                    }
                }
            }));
            rsThread.Start();

            //try
            //{
            //    rsThread.Join();
            //}
            //catch (Exception)
            //{

            //}

            process.WaitForExit();

            return sb.ToString();
        }



        #region 事件



        #endregion

    }
}
