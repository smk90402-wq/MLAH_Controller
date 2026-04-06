using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Windows.Threading;
using MLAH_Controller;
using System.Collections.ObjectModel;
using System.IO;

namespace MLAH_Controller
{
    class Model_MainView : CommonBase
    {
        #region Singleton
        //private static Model_MainView _Model_MainView = null;
        private static readonly Lazy<Model_MainView> _lazyInstance = new Lazy<Model_MainView>(() => new Model_MainView(), true);

        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static Model_MainView SingletonInstance
        {
            get { return _lazyInstance.Value; }
        }

        #endregion Singleton

        public Model_MainView()
        {

        }

        
    }



    public class DiskInfoView
    {
        public string Name { get; set; }
        public long TotalSize { get; set; }
        public long UsedSize { get; set; }
        public long FreeSpace { get; set; }
    }

    public class Model_SystemInfoView : CommonBase
    {
        private float _CpuUsage = 0;
        public float CpuUsage
        {
            get
            {
                return _CpuUsage;
            }
            set
            {
                _CpuUsage = value;
                OnPropertyChanged("CpuUsage");
            }
        }

        private float _MemoryUsage = 0;
        public float MemoryUsage
        {
            get
            {
                return _MemoryUsage;
            }
            set
            {
                _MemoryUsage = value;
                OnPropertyChanged("MemoryUsage");
            }
        }

        private List<DiskInfoView> _Disks = new List<DiskInfoView>();
        public List<DiskInfoView> Disks
        {
            get
            {
                return _Disks;
            }
            set
            {
                _Disks = value;
                OnPropertyChanged("Disks");
            }
        }

        //private float _DiskUsage = 0;
        //public float DiskUsage
        //{
        //    get
        //    {
        //        return _DiskUsage;
        //    }
        //    set
        //    {
        //        _DiskUsage = value;
        //        OnPropertyChanged("DiskUsage");
        //    }
        //}
    }

    public class PerformanceMetrics
    {
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private PerformanceCounter diskCounter;

        public PerformanceMetrics()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
            diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        }

        public float GetCurrentCpuUsage()
        {
            return cpuCounter.NextValue();
        }

        public float GetCurrentMemoryUsage()
        {
            return ramCounter.NextValue();
        }

        public float GetCurrentDiskUsage()
        {
            return diskCounter.NextValue();
        }
    }


//    using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", "192.168.0.10"))
//{
//    // PerformanceCounter는 1~2회 먼저 NextValue()를 호출해야 정확한 값이 나옴
//    _ = cpuCounter.NextValue();
//    System.Threading.Thread.Sleep(500);

//    float cpuUsage = cpuCounter.NextValue();
//    Console.WriteLine($"CPU 사용률: {cpuUsage}%");
//}
}
