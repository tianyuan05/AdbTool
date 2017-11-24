using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using System.Windows.Forms;
using System.Configuration;
using Hazmat.Common;
using Managed.Adb;
using AdbDevice = Managed.Adb.Device;
using Microsoft.Practices.Unity;
using Managed.Adb.IO;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace Hazmat.Device
{
    public class DeviceService : NativeWindow, IDeviceService
    {
        [Dependency]
        public IEventAggregator EventAggregator { get; set; }



        public DeviceService(IEventAggregator eventAggregator)
        {
            this.CreateHandle(new CreateParams());

            EventAggregator = eventAggregator;



            GetDeviceList();
        }

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_CONFIGCHANGECANCELED = 0x0019;
        private const int DBT_CONFIGCHANGED = 0x0018;
        private const int DBT_CUSTOMEVENT = 0x8006;
        private const int DBT_DEVICEQUERYREMOVE = 0x8001;
        private const int DBT_DEVICEQUERYREMOVEFAILED = 0x8002;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVICEREMOVEPENDING = 0x8003;
        private const int DBT_DEVICETYPESPECIFIC = 0x8005;
        private const int DBT_DEVNODES_CHANGED = 0x0007;
        private const int DBT_QUERYCHANGECONFIG = 0x0017;
        private const int DBT_USERDEFINED = 0xFFFF;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DEVICECHANGE)
            {
                switch (m.WParam.ToInt32())
                {
                    case DBT_DEVNODES_CHANGED:// 设备树变化时 A device has been added to or removed from the system.
                        OnDeviceNodesChanged();
                        break;
                    default:
                        break;
                }
            }

            base.WndProc(ref m);
        }

        System.Timers.Timer timer = null;
        DateTime deviceNodeChangedTime;

        // 当有USB设备插入或移除时
        private void OnDeviceNodesChanged()
        {
            deviceNodeChangedTime = DateTime.Now;

            if (timer == null)
            {
                timer = new System.Timers.Timer();
                timer.Interval = 100;
                timer.Elapsed += Timer_Tick;
            }

            if (!timer.Enabled)
                timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - deviceNodeChangedTime).TotalMilliseconds >= 1000)
            {
                timer.Stop();
                GetDeviceList();
            }
        }


        List<UsbDeviceInfo> UsbDeviceInfoList { get; set; } = new List<UsbDeviceInfo>();
        List<AdbDevice> AdbDeviceList { get; set; } = new List<AdbDevice>();

        // adb 获取设备列表
        private List<DeviceInfo> AdbGetDeviceList()
        {
            ProcessStartInfo psi = new ProcessStartInfo("./tadb.exe", "devices");
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            List<String> errorOutput = new List<String>();
            List<String> stdOutput = new List<String>();
            using (Process proc = Process.Start(psi))
            {
                int status = AdbGrabProcessOutput(proc, errorOutput, stdOutput, true /* waitForReaders */);
                if (status != 0)
                {
                    StringBuilder builder = new StringBuilder("'adb devices' failed!");
                    builder.AppendLine(string.Empty);
                    foreach (String error in errorOutput)
                    {
                        builder.AppendLine(error);
                    }
                    //Log.LogAndDisplay(LogLevel.Error, TAG, builder.ToString());
                    Console.WriteLine(builder);
                }
            }

            bool start = false;
            List<DeviceInfo> deviceList = new List<DeviceInfo>();
            foreach (var line in stdOutput)
            {
                Console.WriteLine(line);
                if (!start)
                {
                    if (line == "List of devices attached ")
                    {
                        start = true;
                        continue;
                    }
                }
                else
                {
                    string[] strs = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    //foreach (var str in strs)
                    //    Console.WriteLine(str);
                    if (strs.Length == 2)
                    {
                        DeviceInfo deviceInfo = new DeviceInfo { Serial = strs[0] };
                        deviceInfo.Model = AdbGetDeviceProp(deviceInfo.Serial, "ro.product.model");
                        deviceList.Add(deviceInfo);
                    }
                }
            }
            return deviceList;
        }

        // adb 获取设备属性
        private string AdbGetDeviceProp(string serial, string property)
        {
            ProcessStartInfo psi = new ProcessStartInfo("./tadb.exe", string.Format("-s {0} shell getprop {1}", serial, property));
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            List<String> errorOutput = new List<String>();
            List<String> stdOutput = new List<String>();
            using (Process proc = Process.Start(psi))
            {
                int status = AdbGrabProcessOutput(proc, errorOutput, stdOutput, true /* waitForReaders */);
                if (status != 0)
                {
                    StringBuilder builder = new StringBuilder("'adb getprop' failed!");
                    builder.AppendLine(string.Empty);
                    foreach (String error in errorOutput)
                    {
                        builder.AppendLine(error);
                    }
                    //Log.LogAndDisplay(LogLevel.Error, TAG, builder.ToString());
                    Console.WriteLine(builder);
                }

                return stdOutput.LastOrDefault();
            }
        }

        // adb 安装apk
        private bool AdbInstallApk(string serial, string package)
        {
            ProcessStartInfo psi = new ProcessStartInfo("./tadb.exe", string.Format("-s {0} install -r {1}", serial, package));
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            List<String> errorOutput = new List<String>();
            List<String> stdOutput = new List<String>();
            using (Process proc = Process.Start(psi))
            {
                int status = AdbGrabProcessOutput(proc, errorOutput, stdOutput, true /* waitForReaders */);
                if (status != 0)
                {
                    StringBuilder builder = new StringBuilder("'adb getprop' failed!");
                    builder.AppendLine(string.Empty);
                    foreach (String error in errorOutput)
                    {
                        builder.AppendLine(error);
                    }
                    //Log.LogAndDisplay(LogLevel.Error, TAG, builder.ToString());
                    Console.WriteLine(builder);
                    return false;
                }
            }

            foreach (var line in stdOutput)
            {
                Console.WriteLine(line);
            }
            return true;
        }

        // adb Push File
        private bool AdbPushFile(string serial, string lfile, string rfile)
        {
            ProcessStartInfo psi = new ProcessStartInfo("./tadb.exe", string.Format("-s {0} push {1} {2}", serial, lfile, rfile));
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            List<String> errorOutput = new List<String>();
            List<String> stdOutput = new List<String>();
            using (Process proc = Process.Start(psi))
            {
                int status = AdbGrabProcessOutput(proc, errorOutput, stdOutput, true /* waitForReaders */);
                if (status != 0)
                {
                    StringBuilder builder = new StringBuilder("'adb push' failed!");
                    builder.AppendLine(string.Empty);
                    foreach (String error in errorOutput)
                    {
                        builder.AppendLine(error);
                    }
                    //Log.LogAndDisplay(LogLevel.Error, TAG, builder.ToString());
                    Console.WriteLine(builder);
                    return false;
                }
            }

            foreach (var line in stdOutput)
            {
                Console.WriteLine(line);
            }

            return true;
        }

        private bool AdbPullFile(string serial, string rfile, string lfile)
        {
            ProcessStartInfo psi = new ProcessStartInfo("./tadb.exe", string.Format("-s {0} pull {1} {2}", serial, rfile, lfile));
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            List<String> errorOutput = new List<String>();
            List<String> stdOutput = new List<String>();
            using (Process proc = Process.Start(psi))
            {
                int status = AdbGrabProcessOutput(proc, errorOutput, stdOutput, true /* waitForReaders */);
                if (status != 0)
                {
                    StringBuilder builder = new StringBuilder("'adb pull' failed!");
                    builder.AppendLine(string.Empty);
                    foreach (String error in errorOutput)
                    {
                        builder.AppendLine(error);
                    }
                    //Log.LogAndDisplay(LogLevel.Error, TAG, builder.ToString());
                    Console.WriteLine(builder);
                    return false;
                }
            }

            foreach (var line in stdOutput)
            {
                Console.WriteLine(line);
            }
            return true;
        }

        // adb 文件是否存在
        private bool AdbIsFileExist(string serial, string rfile)
        {
            ProcessStartInfo psi = new ProcessStartInfo("./tadb.exe", string.Format("-s {0} shell ls {1}", serial, rfile));
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            List<String> errorOutput = new List<String>();
            List<String> stdOutput = new List<String>();
            using (Process proc = Process.Start(psi))
            {
                int status = AdbGrabProcessOutput(proc, errorOutput, stdOutput, true /* waitForReaders */);
                if (status != 0)
                {
                    StringBuilder builder = new StringBuilder("'adb shell check file' failed!");
                    builder.AppendLine(string.Empty);
                    foreach (String error in errorOutput)
                    {
                        builder.AppendLine(error);
                    }
                    //Log.LogAndDisplay(LogLevel.Error, TAG, builder.ToString());
                    Console.WriteLine(builder);
                    return false;
                }
            }

            foreach (var line in stdOutput)
            {
                if (line.Contains(rfile) && !line.Contains("No such file or directory"))
                    return true;
            }
            return false;
        }


        private int AdbGrabProcessOutput(Process process, List<String> errorOutput, List<String> stdOutput, bool waitforReaders)
        {
            if (errorOutput == null)
            {
                throw new ArgumentNullException("errorOutput");
            }
            if (stdOutput == null)
            {
                throw new ArgumentNullException("stdOutput");
            }
            // read the lines as they come. if null is returned, it's
            // because the process finished
            Thread t1 = new Thread(new ThreadStart(delegate
            {
                // create a buffer to read the stdoutput
                try
                {
                    using (StreamReader sr = process.StandardError)
                    {
                        while (!sr.EndOfStream)
                        {
                            String line = sr.ReadLine();
                            if (!String.IsNullOrEmpty(line))
                            {
                                //Log.e(ADB, line);
                                errorOutput.Add(line);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // do nothing.
                }
            }));

            Thread t2 = new Thread(new ThreadStart(delegate
            {
                // create a buffer to read the std output
                try
                {
                    using (StreamReader sr = process.StandardOutput)
                    {
                        while (!sr.EndOfStream)
                        {
                            String line = sr.ReadLine();
                            if (!String.IsNullOrEmpty(line))
                            {
                                stdOutput.Add(line);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // do nothing.
                }
            }));

            t1.Start();
            t2.Start();

            // it looks like on windows process#waitFor() can return
            // before the thread have filled the arrays, so we wait for both threads and the
            // process itself.
            if (waitforReaders)
            {
                try
                {
                    t1.Join();
                }
                catch (ThreadInterruptedException)
                {
                }
                try
                {
                    t2.Join();
                }
                catch (ThreadInterruptedException)
                {
                }
            }

            // get the return code from the process
            process.WaitForExit();
            return process.ExitCode;
        }

        //async private void GetDeviceList()
        //{
        //    await Task.Run(() =>
        //    {
        //        // 启动ADB Server
        //        AndroidDebugBridge mADB;
        //        String mAdbPath = ".\\tadb.exe";

        //        mADB = AndroidDebugBridge.CreateBridge(mAdbPath, false);
        //        //mADB.Start();

        //        // 读取配置文件
        //        string deviceModel = ConfigurationManager.AppSettings["device_model"];
        //        string[] deviceModelArray = deviceModel.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        //        List<UsbDeviceInfo> usbDeviceList = SetupApi.GetUsbDevInfoList().Where(d => deviceModelArray.Contains(d.Desc)).ToList();

        //        UsbDeviceInfoList = usbDeviceList;

        //        if (UsbDeviceInfoList.Count > 0)
        //            AdbDeviceList = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress)/*.Where(d => deviceModelArray.Contains(d.Model))*/.ToList();
        //        else
        //            AdbDeviceList.Clear();

        //        // 根据USB设备的数量来创建设备列表
        //        List<DeviceInfo> devList = UsbDeviceInfoList.Select(d => new DeviceInfo { Model = d.Desc }).ToList();
        //        List<AdbDevice> adbList = AdbDeviceList.ToList();

        //        // 按照顺序对应ADB设备
        //        foreach (DeviceInfo dev in devList)
        //        {
        //            AdbDevice adbDevice = adbList.FirstOrDefault(d => d.Model == dev.Model || d.GetProperty("ro.product.model") == dev.Model);
        //            if (adbDevice != null)
        //            {
        //                dev.Serial = adbDevice.SerialNumber;
        //                adbList.Remove(adbDevice);
        //            }
        //        }

        //        EventAggregator.GetEvent<DeviceChangedEvent>().Publish(devList);
        //    });

        //}

        async private void GetDeviceList()
        {
            await Task.Run(() =>
            {
                // 读取配置文件
                string deviceModel = ConfigurationManager.AppSettings["device_model"];
                string[] deviceModelArray = deviceModel.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                List<UsbDeviceInfo> usbDeviceList = SetupApi.GetUsbDevInfoList().Where(d => deviceModelArray.Contains(d.Desc)).ToList();

                UsbDeviceInfoList = usbDeviceList;

                if (UsbDeviceInfoList.Count > 0)
                {
                    // 根据USB设备的数量来创建设备列表
                    List<DeviceInfo> devList = UsbDeviceInfoList.Select(d => new DeviceInfo { Model = d.Desc }).ToList();
                    List<DeviceInfo> adbList = AdbGetDeviceList();

                    // 按照顺序对应ADB设备
                    foreach (DeviceInfo dev in devList)
                    {
                        DeviceInfo adbDevice = adbList.FirstOrDefault(d => d.Model == dev.Model);
                        if (adbDevice != null)
                        {
                            dev.Serial = adbDevice.Serial;
                            adbList.Remove(adbDevice);
                        }
                    }
                    EventAggregator.GetEvent<DeviceChangedEvent>().Publish(devList);
                }
                else
                {
                    AdbDeviceList.Clear();
                }
            });

        }


        //public bool InstallApk(string serial)
        //{
        //    try
        //    {
        //        AdbDevice device = AdbDeviceList.FirstOrDefault(d => d.SerialNumber == serial);
        //        if (device == null)
        //            return false;

        //        device.InstallPackage(Environment.CurrentDirectory + "\\app-debug.apk", true);
        //        return true;
        //    }
        //    catch (Exception exc)
        //    {
        //        Console.WriteLine(exc.Message);
        //        return false;
        //    }
        //}

        public bool InstallApk(string serial)
        {
            return AdbInstallApk(serial, Environment.CurrentDirectory + "\\app-debug.apk");
        }



        // local Hazmat_tmp.db -> remote Hazmat.db
        //public bool PushDbFile(string serial)
        //{
        //    AdbDevice device = AdbDeviceList.FirstOrDefault(d => d.SerialNumber == serial);
        //    if (device == null)
        //        return false;

        //    try
        //    {
        //        using (SyncService sync = device.SyncService)
        //        {
        //            String lfile = System.IO.Path.Combine(Environment.CurrentDirectory, FileNames.localTempDbFileName);
        //            String lfileCopy = System.IO.Path.Combine(Environment.CurrentDirectory, FileNames.localTempCopyDbFileName);
        //            // 数据库访问后会出现文件被占用的情况，暂时解决办法是拷贝一份再push
        //            File.Copy(lfile, lfileCopy, true);
        //            SyncResult result = sync.PushFile(lfileCopy, FileNames.remoteDbFile, new FileSyncProgressMonitor());
        //            return true;
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        Console.WriteLine(exc.Message);
        //        return false;
        //    }
        //}

        public bool PushDbFile(string serial)
        {
            String lfile = System.IO.Path.Combine(Environment.CurrentDirectory, FileNames.localTempDbFileName);
            String lfileCopy = System.IO.Path.Combine(Environment.CurrentDirectory, FileNames.localTempCopyDbFileName);
            // 数据库访问后会出现文件被占用的情况，暂时解决办法是拷贝一份再push
            File.Copy(lfile, lfileCopy, true);
            return AdbPushFile(serial, lfileCopy, FileNames.remoteDbFile);
        }



        //public bool PullDbFile(string serial)
        //{
        //    AdbDevice device = AdbDeviceList.FirstOrDefault(d => d.SerialNumber == serial);
        //    if (device == null)
        //        return false;

        //    return PullDbFile(device);
        //}

        public bool PullDbFile(string serial)
        {
            string lfile = System.IO.Path.Combine(Environment.CurrentDirectory, FileNames.localTempDbFileName);
            string rfile = FileNames.remoteDbFile;
            return AdbPullFile(serial, rfile, lfile);
        }



        // remote Hazmat.db -> local Hazmat_tmp.db
        //private bool PullDbFile(AdbDevice device)
        //{
        //    try
        //    {
        //        using (SyncService sync = device.SyncService)
        //        {
        //            String lfile = System.IO.Path.Combine(Environment.CurrentDirectory, FileNames.localTempDbFileName);

        //            SyncResult result = sync.PullFile(FileNames.remoteDbFile, lfile, new FileSyncProgressMonitor());
        //            return true;
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        Console.WriteLine(exc.Message);
        //        return false;
        //    }
        //}


        // 查看设备上的数据库文件是否存在
        //public bool IsRemoteDbFileExits(string serial)
        //{
        //    AdbDevice device = AdbDeviceList.FirstOrDefault(d => d.SerialNumber == serial);
        //    if (device == null)
        //        return false;

        //    try
        //    {
        //        FileEntry fileEntry = FileEntry.Find(device, FileNames.remoteDbFile);
        //        if (fileEntry.Exists)
        //            return true;
        //        else
        //            return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        public bool IsRemoteDbFileExits(string serial)
        {
            return AdbIsFileExist(serial, FileNames.remoteDbFile);
        }



        //public string PullResuorceFile(string serial, string rfile, string lpath)
        //{
        //    AdbDevice device = AdbDeviceList.FirstOrDefault(d => d.SerialNumber == serial);
        //    rfile = rfile.Replace("/storage/emulated/0", "/sdcard");
        //    if (device == null)
        //        return null;

        //    try
        //    {
        //        FileEntry fileEntry = FileEntry.Find(device, rfile);
        //        if (!fileEntry.Exists)
        //            return null;

        //        if (!Directory.Exists(lpath))
        //            Directory.CreateDirectory(lpath);

        //        string lfile = Path.Combine(lpath, LinuxPath.GetFileName(rfile));
        //        using (SyncService sync = device.SyncService)
        //        {
        //            SyncResult result = sync.PullFile(fileEntry, lfile, new FileSyncProgressMonitor());
        //            if (result.Code != 0)
        //            {
        //                Console.Write("Pull file failed：{0}", result.Message);
        //                return null;
        //            }
        //        }

        //        return lfile;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        public string PullResuorceFile(string serial, string rfile, string lpath)
        {
            rfile = rfile.Replace("/storage/emulated/0", "/sdcard");
            if (!AdbIsFileExist(serial, rfile))
                return null;

            if (!Directory.Exists(lpath))
                Directory.CreateDirectory(lpath);

            string lfile = Path.Combine(lpath, LinuxPath.GetFileName(rfile));

            if (AdbPullFile(serial, rfile, lfile))
                return lfile;

            return null;
        }

        //    public class FileSyncProgressMonitor : ISyncProgressMonitor
        //    {

        //        public void Start(long totalWork)
        //        {
        //            Console.WriteLine("Starting Transfer");
        //            this.TotalWork = this.Remaining = totalWork;
        //            Transfered = 0;
        //        }

        //        public void Stop()
        //        {
        //            IsCanceled = true;
        //        }

        //        public bool IsCanceled { get; private set; }

        //        public void StartSubTask(String source, String destination)
        //        {
        //            Console.WriteLine("Syncing {0} -> {1}", source, destination);
        //        }

        //        public void Advance(long work)
        //        {
        //            Transfered += work;
        //            Remaining -= work;
        //            Console.WriteLine("Transfered {0} of {1} - {2} remaining", Transfered, TotalWork, Remaining);
        //        }

        //        public long TotalWork { get; set; }
        //        public long Remaining { get; set; }
        //        public long Transfered { get; set; }
        //    }
    }
}
