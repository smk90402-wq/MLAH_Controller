
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DevExpress.Map;
using DevExpress.Map.Kml.Model;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.CodeView.Margins;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Map;
using DevExpress.XtraPrinting.Native;
using Windows.Devices.Geolocation;
using Windows.Storage.Provider;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using static DevExpress.Utils.Drawing.Helpers.NativeMethods;



namespace MLAH_LogAnalyzer
{
    public partial class UnitObjectInfo : CommonBase
    {
        public void Clear()
        {
            ID = 0;
            Name = string.Empty;
            Type = 0;
            PlatformType = 0;
            Health = 100;
            Status = 1;
            PayLoadHealth = 1;
            FuelWarning = 1;
            entityAbnormalCause?.Clear();
            Identification = 0;
            IsLeader = 0;
            LOC?.Clear();
            velocity?.Clear();
            weapons?.Clear();
            Fuel = 15f;
            FuelConsumption = 0.00000323f;
            HorizontalFov = 0f;
            VerticalFov = 0f;
            DiagonalFov = 0f;
            SensorCenterLat = 0f;
            SensorCenterLon = 0f;
            SensorCenterAlt = 0;
            SlantRange = 0;
            FootPrintLeftTopLat = 0f;
            FootPrintLeftTopLon = 0f;
            FootPrintLeftTopAlt = 0;
            FootPrintRightTopLat = 0f;
            FootPrintRightTopLon = 0f;
            FootPrintRightTopAlt = 0;
            FootPrintRightBottomLat = 0f;
            FootPrintRightBottomLon = 0f;
            FootPrintRightBottomAlt = 0;
            FootPrintLeftBottomLat = 0f;
            FootPrintLeftBottomLon = 0f;
            FootPrintLeftBottomAlt = 0;
            DetectRange = 1500;
            AttackRange = 1000;
            AttackAccuracy = 50;
            AttackDelay = 0.1;
            AttackDamage = 1;
            DetectPixel = 20;
            RecogPixel = 10;
            AttackFlag = 1;
        }

        private int _ID;
        /// <summary>
        /// ID
        /// </summary>
        public int ID
        {
            get
            {
                return _ID;
            }
            set
            {
                _ID = value;
                OnPropertyChanged("ID");
            }
        }

        private string _Name;
        /// <summary>
        /// 이름
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                OnPropertyChanged("Name");
            }
        }

        private short _Type;
        /// <summary>
        /// 객체유형 - 1: UAV 2:KUH 3:LAH(Light Armed Helicopter) 4:장갑차 5:탱크 6:방사포
        /// 7:곡사포 8:고정고사포 9:특작군인 10:자주포 11:트럭 12:작전차량 13:군인
        /// </summary>
        public short Type
        {
            get
            {
                return _Type;
            }
            set
            {
                _Type = value;
                OnPropertyChanged("Type");
            }
        }

        private short _PlatformType = 0;
        /// <summary>
        /// 기종
        /// </summary>
        public short PlatformType
        {
            get
            {
                return _PlatformType;
            }
            set
            {
                _PlatformType = value;
                OnPropertyChanged("PlatformType");
            }
        }

        /// <summary>
        /// 상태
        /// 체력
        /// </summary>
        private double _Health = 100;
        public double Health
        {
            get
            {
                return _Health;
            }
            set
            {
                _Health = value;
                OnPropertyChanged("Health");
            }
        }

        /// <summary>
        /// 상태
        /// 0: 알수없음,1: 정상,2: 비정상
        /// </summary>
        private uint _Status = 1;
        public uint Status
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

        /// <summary>
        /// 상태
        /// 0: 알수없음,1: 정상,2: 비정상
        /// </summary>
        private uint _PayLoadHealth = 1;
        public uint PayLoadHealth
        {
            get
            {
                return _PayLoadHealth;
            }
            set
            {
                _PayLoadHealth = value;
                OnPropertyChanged("PayLoadHealth");
            }
        }

        /// <summary>
        /// 상태
        /// 0: 알수없음,1: 정상,2: 비정상
        /// </summary>
        private uint _FuelWarning = 1;
        public uint FuelWarning
        {
            get
            {
                return _FuelWarning;
            }
            set
            {
                _FuelWarning = value;
                OnPropertyChanged("FuelWarning");
            }
        }

        /// <summary>
        /// 상태
        /// 0: 알수없음,1: 정상,2: 비정상
        /// </summary>
        private EntityAbnormalCause _entityAbnormalCause = new EntityAbnormalCause();
        public EntityAbnormalCause entityAbnormalCause
        {
            get
            {
                return _entityAbnormalCause;
            }
            set
            {
                _entityAbnormalCause = value;
                OnPropertyChanged("entityAbnormalCause");
            }
        }

        /// <summary>
        /// 피아식별 유형
        /// 1 : 아군 / 2 : 적군
        /// </summary>
        private short _Identification;
        public short Identification
        {
            get
            {
                return _Identification;
            }
            set
            {
                _Identification = value;
                OnPropertyChanged("Identification");
            }
        }


        private short _IsLeader;
        /// <summary>
        /// 지휘 권한 - ID 1번이 지휘기라고 가정
        /// </summary>
        public short IsLeader
        {
            get
            {
                return _IsLeader;
            }
            set
            {
                _IsLeader = value;
                OnPropertyChanged("IsLeader");
            }
        }

        private CoordinateInfo _LOC = new CoordinateInfo();
        /// <summary>
        /// 위치 - 초기위치
        /// </summary>
        public CoordinateInfo LOC
        {
            get
            {
                return _LOC;
            }
            set
            {
                _LOC = value;
                OnPropertyChanged("LOC");
            }
        }

        private Velocity _velocity = new Velocity();
        /// <summary>
        /// 위치 - 초기위치
        /// </summary>
        public Velocity velocity
        {
            get
            {
                return _velocity;
            }
            set
            {
                _velocity = value;
                OnPropertyChanged("velocity");
            }
        }

        private Weapons _weapons = new Weapons();
        /// <summary>
        /// 무장 정보
        /// </summary>
        public Weapons weapons
        {
            get
            {
                return _weapons;
            }
            set
            {
                _weapons = value;
                OnPropertyChanged("weapons");
            }
        }

        private float _Fuel;
        /// <summary>
        /// 연료량(L?)
        /// </summary>
        public float Fuel
        {
            get
            {
                return _Fuel;
            }
            set
            {
                _Fuel = value;
                OnPropertyChanged("Fuel");
            }
        }

        private float _FuelConsumption;
        /// <summary>
        /// 연료량(L?)
        /// </summary>
        public float FuelConsumption
        {
            get
            {
                return _FuelConsumption;
            }
            set
            {
                _FuelConsumption = value;
                OnPropertyChanged("FuelConsumption");
            }
        }

        public float HorizontalFov;
        public float VerticalFov;
        public float DiagonalFov;
        public float SensorCenterLat;
        public float SensorCenterLon;
        public int SensorCenterAlt;
        public int SlantRange;
        public float FootPrintLeftTopLat;
        public float FootPrintLeftTopLon;
        public int FootPrintLeftTopAlt;
        public float FootPrintRightTopLat;
        public float FootPrintRightTopLon;
        public int FootPrintRightTopAlt;
        public float FootPrintRightBottomLat;
        public float FootPrintRightBottomLon;
        public int FootPrintRightBottomAlt;
        public float FootPrintLeftBottomLat;
        public float FootPrintLeftBottomLon;
        public int FootPrintLeftBottomAlt;

        private double _DetectRange = 1500;
        public double DetectRange
        {

            get { return _DetectRange; }
            set
            {
                _DetectRange = value;
                OnPropertyChanged("DetectRange");
            }
        }

        private double _AttackRange = 1000;
        public double AttackRange
        {

            get { return _AttackRange; }
            set
            {
                _AttackRange = value;
                OnPropertyChanged("AttackRange");
            }
        }

        private double _AttackAccuracy = 50;
        public double AttackAccuracy
        {

            get { return _AttackAccuracy; }
            set
            {
                _AttackAccuracy = value;
                OnPropertyChanged("AttackAccuracy");
            }
        }

        private double _AttackDelay = 0.1;
        public double AttackDelay
        {

            get { return _AttackDelay; }
            set
            {
                _AttackDelay = value;
                OnPropertyChanged("AttackDelay");
            }
        }

        private double _AttackDamage = 1;
        public double AttackDamage
        {

            get { return _AttackDamage; }
            set
            {
                _AttackDamage = value;
                OnPropertyChanged("AttackDamage");
            }
        }

        private int _UAVTestMode = 0;
        public int UAVTestMode
        {

            get { return _UAVTestMode; }
            set
            {
                _UAVTestMode = value;
                OnPropertyChanged("UAVTestMode");
            }
        }

        private double _DetectPixel = 20;
        public double DetectPixel
        {

            get { return _DetectPixel; }
            set
            {
                _DetectPixel = value;
                OnPropertyChanged("DetectPixel");
            }
        }

        private double _RecogPixel = 10;
        public double RecogPixel
        {

            get { return _RecogPixel; }
            set
            {
                _RecogPixel = value;
                OnPropertyChanged("RecogPixel");
            }
        }

        private int _AttackFlag = 1;
        public int AttackFlag
        {

            get { return _AttackFlag; }
            set
            {
                _AttackFlag = value;
                OnPropertyChanged("AttackFlag");
            }
        }

        //enum enumEntityStatus
        //{
        //    STATUS_FLIGHT = 0;
        //    STATUS_WAIT = 1;
        //    STATUS_ATTACK = 2;
        //    STATUS_DEATH = 3;
        //}

        private enumEntityStatus _LAHStatus = enumEntityStatus.StatusWait;
        public enumEntityStatus LAHStatus
        {

            get { return _LAHStatus; }
            set
            {
                _LAHStatus = value;
                OnPropertyChanged("LAHStatus");
            }
        }

        //private ObservableCollection<CoordinateInfo> _PathPlanList = new ObservableCollection<CoordinateInfo>();
        ///// <summary>
        ///// 이동경로 리스트
        ///// </summary>
        //public ObservableCollection<CoordinateInfo> PathPlanList
        //{
        //    get
        //    {
        //        return _PathPlanList;
        //    }
        //    set
        //    {
        //        _PathPlanList = value;
        //        OnPropertyChanged("PathPlanList");
        //    }
        //}


    }

    public enum enumEntityStatus
    {
        /// <summary>
        ///
        ///STATUS_MISSION = 0;
        ///STATUS_FLIGHT = 1;
        ///STATUS_LOITER = 2;
        ///STATUS_HOLDING = 3;
        ///STATUS_ATTACK = 4;
        ///STATUS_DEATH = 5;
        ///STATUS_TRACKING = 6;
        ///STATUS_CONTROL = 7;
        /// </summary>
         StatusFlight = 0,
        StatusWait = 1,
        StatusAttack = 2,
        StatusDeath = 3,
    }

    public partial class EntityAbnormalCause : CommonBase
    {
        public void Clear()
        {
            Hit = 0;
            Loss1 = 0;
            Loss2 = 0;
            Loss3 = 0;
            FuelWarning = 0;
            FuelDanger = 0;
            FuelZero = 0;
            Crash = 0;
            Sensor = 0;
        }

        private int _Hit;
        public int Hit
        {
            get
            {
                return _Hit;
            }
            set
            {
                _Hit = value;
                OnPropertyChanged("Hit");
            }
        }

        private int _Loss1;
        public int Loss1
        {
            get
            {
                return _Loss1;
            }
            set
            {
                _Loss1 = value;
                OnPropertyChanged("Loss1");
            }
        }

        private int _Loss2;
        public int Loss2
        {
            get
            {
                return _Loss2;
            }
            set
            {
                _Loss2 = value;
                OnPropertyChanged("Loss2");
            }
        }

        private int _Loss3;
        public int Loss3
        {
            get
            {
                return _Loss3;
            }
            set
            {
                _Loss3 = value;
                OnPropertyChanged("Loss3");
            }
        }

        private int _FuelWarning;
        public int FuelWarning
        {
            get
            {
                return _FuelWarning;
            }
            set
            {
                _FuelWarning = value;
                OnPropertyChanged("FuelWarning");
            }
        }

        private int _FuelDanger;
        public int FuelDanger
        {
            get
            {
                return _FuelDanger;
            }
            set
            {
                _FuelDanger = value;
                OnPropertyChanged("FuelDanger");
            }
        }

        private int _FuelZero;
        public int FuelZero
        {
            get
            {
                return _FuelZero;
            }
            set
            {
                _FuelZero = value;
                OnPropertyChanged("FuelZero");
            }
        }

        private int _Crash;
        public int Crash
        {
            get
            {
                return _Crash;
            }
            set
            {
                _Crash = value;
                OnPropertyChanged("Crash");
            }
        }

        private int _Sensor;
        public int Sensor
        {
            get
            {
                return _Sensor;
            }
            set
            {
                _Sensor = value;
                OnPropertyChanged("Sensor");
            }
        }
    }

    public partial class CoordinateInfo : CommonBase
    {
        public void Clear()
        {
            Latitude = 0f;
            Longitude = 0f;
            Altitude = 700; // 기본값 유지
        }
        private float _Latitude;
        /// <summary>
        /// 좌표의 위도 (deg) (필요 시 여러 좌표)
        /// </summary>
        public float Latitude
        {
            get
            {
                return _Latitude;
            }
            set
            {
                _Latitude = value;
                OnPropertyChanged("Latitude");
            }
        }

        private float _Longitude;
        /// <summary>
        /// 좌표의 경도 (deg) (필요 시 여러 좌표)
        /// </summary>
        public float Longitude
        {
            get
            {
                return _Longitude;
            }
            set
            {
                _Longitude = value;
                OnPropertyChanged("Longitude");
            }
        }

        private int _Altitude = 700;
        /// <summary>
        /// 좌표의 고도 (m) (필요 시 여러 좌표)
        /// </summary>
        public int Altitude
        {
            get
            {
                return _Altitude;
            }
            set
            {
                _Altitude = value;
                OnPropertyChanged("Altitude");
            }
        }
    }

    public partial class Velocity : CommonBase
    {
        public void Clear()
        {
            Speed = 0f;
            Heading = 0f;
        }

        private float _Speed;
        /// <summary>
        /// 속도 - Km/h
        /// </summary>
        public float Speed
        {
            get
            {
                return _Speed;
            }
            set
            {
                _Speed = value;
                OnPropertyChanged("Speed");
            }
        }

        private float _Heading;
        /// <summary>
        /// 헤딩
        /// </summary>
        public float Heading
        {
            get
            {
                return _Heading;
            }
            set
            {
                _Heading = value;
                OnPropertyChanged("Heading");
            }
        }
    }

    public partial class Weapons : CommonBase
    {
        public void Clear()
        {
            Type1 = 0;
            Type2 = 0;
            Type3 = 0;
        }

        private uint _Type1;
        /// <summary>
        /// 무장타입1 - 기관총?(ea)
        /// </summary>
        public uint Type1
        {
            get
            {
                return _Type1;
            }
            set
            {
                _Type1 = value;
                OnPropertyChanged("Type1");
            }
        }

        private uint _Type2;
        /// <summary>
        /// 무장타입2 - 로켓?(ea)
        /// </summary>
        public uint Type2
        {
            get
            {
                return _Type2;
            }
            set
            {
                _Type2 = value;
                OnPropertyChanged("Type2");
            }
        }

        private uint _Type3;
        /// <summary>
        /// 무장타입3 - 미사일?(ea)
        /// </summary>
        public uint Type3
        {
            get
            {
                return _Type3;
            }
            set
            {
                _Type3 = value;
                OnPropertyChanged("Type3");
            }
        }
    }

    public class MapCoveragePolygon : INotifyPropertyChanged // INotifyPropertyChanged 추가 (스타일 변경 시 업데이트 위해)
    {
        private ObservableCollection<GeoPoint> _coordinates = new ObservableCollection<GeoPoint>();
        public ObservableCollection<GeoPoint> Coordinates
        {
            get => _coordinates;
            set { _coordinates = value; OnPropertyChanged(nameof(Coordinates)); }
        }

        // IsHole 대신 IsMissing 사용 (의미 명확화)
        private bool _isMissing;
        public bool IsMissing
        {
            get => _isMissing;
            set { _isMissing = value; OnPropertyChanged(nameof(IsMissing)); }
        }

        // 스타일 속성들 추가
        private Brush _fill = Brushes.Transparent;
        public Brush Fill
        {
            get => _fill;
            set { _fill = value; OnPropertyChanged(nameof(Fill)); }
        }

        private Brush _stroke = Brushes.Black;
        public Brush Stroke
        {
            get => _stroke;
            set { _stroke = value; OnPropertyChanged(nameof(Stroke)); }
        }

        private StrokeStyle _strokeStyle = new StrokeStyle { Thickness = 1 };
        public StrokeStyle StrokeStyle
        {
            get => _strokeStyle;
            set { _strokeStyle = value; OnPropertyChanged(nameof(StrokeStyle)); }
        }

        // INotifyPropertyChanged 구현
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UnitMapObjectInfo : CommonBase
    {
        public UnitMapObjectInfo()
        {
            // 기본 좌표(예: 0,0)로 초기화하여 null을 방지합니다.
            Location = new GeoPoint(0, 0);
        }
        public uint ID { get; set; }

        private uint _Status = 0;
        public uint Status
        { 
            get
            {
                return _Status;
            }
            set
            {
                _Status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private GeoPoint _location;
        public GeoPoint Location
        {
            get => _location;
            set
            {
                _location = value;
                OnPropertyChanged(nameof(Location));
            }
        }
        public int Type { get; set; }

        public string TypeString { get; set; }
        public string PlatformString { get; set; }

        public string StatusString { get; set; }

        private double _heading;
        public double Heading
        {
            get => _heading;
            set
            {
                _heading = value;
                OnPropertyChanged(nameof(Heading));
            }
        }
        public string Name { get; set; }
        public string Description { get; set; }
        private ImageSource _imagesource;
        public ImageSource imagesource
        {
            get => _imagesource;
            set
            {
                _imagesource = value;
                OnPropertyChanged("imagesource");
            }
        }
    }

    public class LinearMissionResultSet
    {
        public List<GeoPoint> CenterPoints { get; set; }

        // List<GeoPoint> CorridorVertices -> List<List<GeoPoint>> SegmentRectangles
        // 이제 '사각형 목록'을 전달한다. 각 사각형은 GeoPoint 리스트(꼭짓점 4개)이다.
        public List<List<GeoPoint>> SegmentRectangles { get; set; }
        public int WidthMeters { get; set; }
    }
    public class CustomMapPolygon : MapPolygon
    {
        public int MissionID { get; set; }
        public int PolygonIndex { get; set; }

        //public bool IsShow { get; set; }

        //public int IsShow { get; set; }

        //private PolygonCoordCollection _PolygonCoordItems = new PolygonCoordCollection();
        //public PolygonCoordCollection PolygonCoordItems
        //{
        //    get
        //    {
        //        return _PolygonCoordItems;
        //    }
        //    set
        //    {
        //        _PolygonCoordItems = value;
        //        OnPropertyChanged("PolygonCoordItems");
        //    }
        //}

        #region 인터페이스 고정 구현부
        public event PropertyChangedEventHandler PropertyChangedEvent;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEvent?.Invoke(this, new PropertyChangedEventArgs(name));

        }
        #endregion 인터페이스 고정 구현부    
    }

    public class CustomMapPoint : GeoPoint
    {
        public int MissionID { get; set; }
        //public int PolygonIndex { get; set; }

        //private bool _IsShow = true;
        //public bool IsShow
        //{
        //    get
        //    {
        //        return _IsShow;
        //    }
        //    set
        //    {
        //        _IsShow = value;
        //        OnPropertyChanged("IsShow");
        //    }
        //}

        private string _TagString = "";
        public string TagString
        {
            get
            {
                return _TagString;
            }
            set
            {
                _TagString = value;
                OnPropertyChanged("TagString");
            }
        }



        #region 인터페이스 고정 구현부
        public event PropertyChangedEventHandler PropertyChangedEvent;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEvent?.Invoke(this, new PropertyChangedEventArgs(name));

        }
        #endregion 인터페이스 고정 구현부    
    }

    //public class PolygonCoordCollection : CoordPointCollection
    //{
    //    public int CoordIndex { get; set; }
    //}


    public class CustomMapLine : MapPolyline
    {
        //public CustomMapLine()
        //{
        //    Points = new Points();
        //}
        //나중에 enum으로
        //public int MissionType { get; set; }
        public int MissionId { get; set; }
        //public int ShapeId { get; set; }
        public int Width { get; set; }
    }

}

