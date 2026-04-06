
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
//using static GMap.NET.Entity.OpenStreetMapGraphHopperRouteEntity;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DevExpress.Dialogs.Core.View;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors.Helpers;
using DevExpress.Xpf.Scheduling.VisualData;
using Google.Protobuf;
using MLAH_Controller;
using MLAHInterop;
using REALTIMEVISUAL.Native.FederateInterface;
using static MLAH_Controller.CommonEvent;


namespace MLAH_Controller
{
    public partial class ViewModel_MainView : CommonBase
    {

        //public event Action<ViewName> OnRequestFadeOut;
        // [확인 후 삭제] 미사용 필드 - 주석처리된 타이머 코드에서만 참조됨
        //private DateTime _lastUAVSw1StatusReceived = DateTime.MinValue;
        //private DateTime _lastUAVSw2StatusReceived = DateTime.MinValue;
        //private DateTime _lastUAVSw3StatusReceived = DateTime.MinValue;
        //private DateTime _lastBattleSw1StatusReceived = DateTime.MinValue;
        //private DateTime _lastDisplaySwStatusReceived = DateTime.MinValue;
        //private DateTime _lastSCSwStatusReceived = DateTime.MinValue;
        //private DateTime _lastControlOperSwStatusReceived = DateTime.MinValue;
        //private readonly TimeSpan _swStatusTimeout = TimeSpan.FromSeconds(2);

        //개발용 메인화면 단위종합 체크
        private string _Controller_Platform_String = "설정";
        public string Controller_Platform_String
        {
            get
            {
                return _Controller_Platform_String;
            }
            set
            {
                _Controller_Platform_String = value;
                OnPropertyChanged("Controller_Platform_String");
            }
        }

        //Temp 상태 체크
        private int _Status = 0;
        public int Status
        {
            get
            {
                return _Status;
            }
            set
            {
                _Status = value;
                OnPropertyChanged("Status");
            }
        }

        //Temp 상태 체크
        private int _Status1 = 1;
        public int Status1
        {
            get
            {
                return _Status1;
            }
            set
            {
                _Status1 = value;
                OnPropertyChanged("Status1");
            }
        }

        private int _CTRLHwStatus = 1;
        //통제기 HW 상태
        public int CTRLHwStatus
        {
            get
            {
                return _CTRLHwStatus;
            }
            set
            {
                _CTRLHwStatus = value;
                OnPropertyChanged("CTRLHwStatus");
            }
        }


        private int _CTRLSwStatus = 1;
        //통제기 SW 상태
        public int CTRLSwStatus
        {
            get
            {
                return _CTRLSwStatus;
            }
            set
            {
                _CTRLSwStatus = value;
                OnPropertyChanged("CTRLSwStatus");
            }
        }

        private int _BattleSimHwStatus1 = 0;
        //전장상황 모의기1 Hw 상태
        public int BattleSimHwStatus1
        {
            get
            {
                return _BattleSimHwStatus1;
            }
            set
            {
                _BattleSimHwStatus1 = value;
                OnPropertyChanged("BattleSimHwStatus1");
            }
        }

        private int _BattleSimSwStatus1 = 0;
        //전장상황 모의기1 Sw 상태
        public int BattleSimSwStatus1
        {
            get
            {
                return _BattleSimSwStatus1;
            }
            set
            {
                _BattleSimSwStatus1 = value;
                OnPropertyChanged("BattleSimSwStatus1");
            }
        }

        private int _BattleSimHwStatus2 = 0;
        //전장상황 모의기2 Hw 상태
        public int BattleSimHwStatus2
        {
            get
            {
                return _BattleSimHwStatus2;
            }
            set
            {
                _BattleSimHwStatus2 = value;
                OnPropertyChanged("BattleSimHwStatus2");
            }
        }

        private int _BattleSimSwStatus2 = 0;
        //전장상황 모의기2 Sw 상태
        public int BattleSimSwStatus2
        {
            get
            {
                return _BattleSimSwStatus2;
            }
            set
            {
                _BattleSimSwStatus2 = value;
                OnPropertyChanged("BattleSimSwStatus2");
            }
        }

        private int _BattleSimHwStatus3 = 0;
        //전장상황 모의기3 Hw 상태
        public int BattleSimHwStatus3
        {
            get
            {
                return _BattleSimHwStatus3;
            }
            set
            {
                _BattleSimHwStatus3 = value;
                OnPropertyChanged("BattleSimHwStatus3");
            }
        }

        private int _BattleSimSwStatus3 = 0;
        //전장상황 모의기3 Sw 상태
        public int BattleSimSwStatus3
        {
            get
            {
                return _BattleSimSwStatus3;
            }
            set
            {
                _BattleSimSwStatus3 = value;
                OnPropertyChanged("BattleSimSwStatus3");
            }
        }

        private int _SCSimSwStatus = 0;
        //전장정보 상황인지 SW
        public int SCSimSwStatus
        {
            get
            {
                return _SCSimSwStatus;
            }
            set
            {
                _SCSimSwStatus = value;
                OnPropertyChanged("SCSimSwStatus");
            }
        }

        private int _SCSimHwStatus = 0;
        //전장정보 상황인지 HW
        public int SCSimHwStatus
        {
            get
            {
                return _SCSimHwStatus;
            }
            set
            {
                _SCSimHwStatus = value;
                OnPropertyChanged("SCSimHwStatus");
            }
        }

        private int _UAVSimHwStatus1 = 0;
        //무인기 모의기 상태
        public int UAVSimHwStatus1
        {
            get
            {
                return _UAVSimHwStatus1;
            }
            set
            {
                _UAVSimHwStatus1 = value;
                OnPropertyChanged("UAVSimHwStatus1");
            }
        }

        private int _UAVSimHwStatus2 = 0;
        //무인기 모의기 상태
        public int UAVSimHwStatus2
        {
            get
            {
                return _UAVSimHwStatus2;
            }
            set
            {
                _UAVSimHwStatus2 = value;
                OnPropertyChanged("UAVSimHwStatus2");
            }
        }

        private int _UAVSimHwStatus3 = 0;
        //무인기 모의기 3 Hw 상태
        public int UAVSimHwStatus3
        {
            get
            {
                return _UAVSimHwStatus3;
            }
            set
            {
                _UAVSimHwStatus3 = value;
                OnPropertyChanged("UAVSimHwStatus3");
            }
        }


        private int _UAVSimSwStatus1 = 0;
        //무인기 모의기 상태
        public int UAVSimSwStatus1
        {
            get
            {
                return _UAVSimSwStatus1;
            }
            set
            {
                _UAVSimSwStatus1 = value;
                OnPropertyChanged("UAVSimSwStatus1");
            }
        }

        private int _UAVSimSwStatus2 = 0;
        //무인기 모의기 상태
        public int UAVSimSwStatus2
        {
            get
            {
                return _UAVSimSwStatus2;
            }
            set
            {
                _UAVSimSwStatus2 = value;
                OnPropertyChanged("UAVSimSwStatus2");
            }
        }

        private int _UAVSimSwStatus3 = 0;
        //무인기 모의기 상태
        public int UAVSimSwStatus3
        {
            get
            {
                return _UAVSimSwStatus3;
            }
            set
            {
                _UAVSimSwStatus3 = value;
                OnPropertyChanged("UAVSimSwStatus3");
            }
        }

        private int _ControlOperSimHwStatus = 0;
        //임무통제 및 운용모의 HW 상태
        public int ControlOperSimHwStatus
        {
            get
            {
                return _ControlOperSimHwStatus;
            }
            set
            {
                _ControlOperSimHwStatus = value;
                OnPropertyChanged("ControlOperSimHwStatus");
            }
        }


        private int _ControlOperSimSwStatus = 0;
        //임무통제 및 운용모의 SW 상태
        public int ControlOperSimSwStatus
        {
            get
            {
                return _ControlOperSimSwStatus;
            }
            set
            {
                _ControlOperSimSwStatus = value;
                OnPropertyChanged("ControlOperSimSwStatus");
            }
        }

        private int _DisplaySimHwStatus = 0;
        //시현화면모의 HW 상태
        public int DisplaySimHwStatus
        {
            get
            {
                return _DisplaySimHwStatus;
            }
            set
            {
                _DisplaySimHwStatus = value;
                OnPropertyChanged("DisplaySimHwStatus");
            }
        }


        private int _DisplaySimSwStatus = 0;
        //시현화면모의 SW 상태
        public int DisplaySimSwStatus
        {
            get
            {
                return _DisplaySimSwStatus;
            }
            set
            {
                _DisplaySimSwStatus = value;
                OnPropertyChanged("DisplaySimSwStatus");
            }
        }

        private Model_SystemInfoView _SystemInfoView = new Model_SystemInfoView();
        public Model_SystemInfoView SystemInfoView
        {
            get
            {
                return _SystemInfoView;
            }
            set
            {
                _SystemInfoView = value;
                OnPropertyChanged("SystemInfoView");
            }
        }

        private double _BFSS_CpuUsage = 0;
        public double BFSS_CpuUsage
        {
            get
            {
                return _BFSS_CpuUsage;
            }
            set
            {
                _BFSS_CpuUsage = value;
                OnPropertyChanged("BFSS_CpuUsage");
            }
        }

        private double _BFSS_MemoryUsage = 0;
        public double BFSS_MemoryUsage
        {
            get
            {
                return _BFSS_MemoryUsage;
            }
            set
            {
                _BFSS_MemoryUsage = value;
                OnPropertyChanged("BFSS_MemoryUsage");
            }
        }

        private long _BFSS_Disk1Usage = 0;
        public long BFSS_Disk1Usage
        {
            get
            {
                return _BFSS_Disk1Usage;
            }
            set
            {
                _BFSS_Disk1Usage = value;
                OnPropertyChanged("BFSS_Disk1Usage");
            }
        }

        private long _BFSS_Disk1Total = 0;
        public long BFSS_Disk1Total
        {
            get
            {
                return _BFSS_Disk1Total;
            }
            set
            {
                _BFSS_Disk1Total = value;
                OnPropertyChanged("BFSS_Disk1Total");
            }
        }

        // [확인 후 삭제] 미사용 필드 - 어디서도 참조되지 않음
        //private PerformanceMetrics metrics = new PerformanceMetrics();

    }
}
