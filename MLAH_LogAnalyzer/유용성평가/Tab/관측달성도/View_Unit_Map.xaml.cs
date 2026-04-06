using System.IO;
using System.Net;
using System.Windows.Controls;
using DevExpress.Xpf.Map;
using System;
using System.Windows;
using DevExpress.Xpf.Bars;
using DevExpress.Map;
using DevExpress.Mvvm.Native;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Input;
using DevExpress.XtraGauges.Core.Model;
using Windows.Devices.Printers;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using System.Globalization;
using System.Windows.Data;
using DevExpress.Xpf.CodeView;


namespace MLAH_LogAnalyzer
{

    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class View_Unit_Map : UserControl
    {
        private static View_Unit_Map _instance;

        public static View_Unit_Map SingletonInstance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("View_Unit_Map가 아직 생성되지 않았습니다.");
                return _instance;
            }
        }
        public View_Unit_Map()
        {
            //ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            InitializeComponent();
            var _ = new DevExpress.Xpf.Map.MapControl();
            this.DataContext = ViewModel_Unit_Map.SingletonInstance;
            // XAML에서 생성된 첫 번째 인스턴스를 싱글톤으로 설정
            if (_instance == null)
            {
                _instance = this;
            }
            InitializeFocusEffect();

        }

        // [성능 최적화용] MouseMove 이벤트 조절을 위한 변수
        private DateTime _lastMouseMoveTime = DateTime.MinValue;
        private readonly TimeSpan _mouseMoveInterval = TimeSpan.FromMilliseconds(33); // 약 60 FPS

        /// <summary>
        /// 포커스(비네트) 효과에 사용될 브러시를 생성하고 사각형에 적용합니다.
        /// </summary>
        private void InitializeFocusEffect()
        {
            _focusBrush = new RadialGradientBrush
            {
                // 그라데이션 중심은 투명, 바깥쪽은 반투명 검정색
                GradientStops = new GradientStopCollection
            {
             // 1. [영역 확장] 완전히 밝은 원(1단계)을 반지름의 60%까지 크게 확장한다.
            new GradientStop(Colors.Transparent, 0.0),
            new GradientStop(Colors.Transparent, 0.6), // 기존 0.4에서 0.6으로 대폭 상향

            // 2. [영역 확장] 부드럽게 어두워지는 중간 영역(2단계)을 60% ~ 90% 구간으로 넓게 설정한다.
            //new GradientStop(Color.FromArgb(0x44, 0, 0, 0), 0.9), // 기존 0.7에서 0.9로 상향
            new GradientStop(Colors.Transparent, 0.9), // 기존 0.7에서 0.9로 상향

            // 3. 가장자리는 어둡게 유지한다.
            //new GradientStop(Color.FromArgb(0x44, 0, 0, 0), 1.0)
            //new GradientStop(Color.FromArgb(0xDD, 0, 0, 0), 1.0)
            new GradientStop(Colors.Transparent, 1.0)
            },
                // 처음에는 화면 중앙에 위치
                Center = new Point(0.5, 0.5),
                GradientOrigin = new Point(0.5, 0.5),
                RadiusX = 0.5, // 포커스 원의 가로 반지름
                RadiusY = 0.5  // 포커스 원의 세로 반지름
            };

            //FocusRectangle.Fill = _focusBrush;
        }


        // 포커스 효과를 위한 브러쉬를 멤버 변수로 선언
        private RadialGradientBrush _focusBrush;

   


        #region INIT Mission Line 필드
        // ────────── LINEAR‑TYPE 전용 새 필드 ──────────
        public List<GeoPoint> _linePoints = new();   // 사용자가 찍은 점 목록
        public List<MapLine> _previewSegments = new();   // 중앙선 미리보기
        public List<CustomMapPolygon> _previewRects = new(); // 폭 미리보기(사각형)

        public enum DrawState { None, AddingPoints, WidthAdjust }
        public DrawState _state = DrawState.None;
        public MapLine _ghostSegment = null;   // “다음 점” 미리보기 선
        //private double _prevHalf = -1;     // 폭 변화 체크용

        
        #endregion INIT Mission Line 필드


        //Polygon 그리기
        // 폴리곤 그리기 상태
        private enum PolygonState
        {
            None,       // 폴리곤 안 그리는 중
            Drawing     // 폴리곤 그리고 있는 중
        }

        // 폴리곤 관련
        private PolygonState _polygonState = PolygonState.None;
        private List<GeoPoint> _polyPoints = new List<GeoPoint>(); // 클릭으로 찍은 좌표들
        private MapLine _currentLine = null;  // "마우스 무브"에 따라 바뀌는 임시 선


        //PathPlan 그리기
        // 경로 그리기 상태
        private enum PathState
        {
            None,      // 아무 것도 안 하는 중
            Drawing    // 경로를 그리고 있는 중
        }

        // 필드
        private PathState _pathState = PathState.None;
        private MapPolyline _currentPolyline = null;     // 하나의 Polyline에서 점이 계속 늘어남

        //DevelopLAHPathPlan 그리기
        // 경로 그리기 상태
        private enum DevelopPathState
        {
            None,      // 아무 것도 안 하는 중
            Drawing    // 경로를 그리고 있는 중
        }

        // 필드
        private DevelopPathState _developpathState = DevelopPathState.None;
        private MapPolyline _developcurrentPolyline = null;     // 하나의 Polyline에서 점이 계속 늘어남




        private double ZoomLevelToResolution(double zoomLevel)
        {
            const double baseZoom = 14.0;
            const double baseResolution = 7.525; // 14레벨에서 약 7.525 m/px
            double diff = zoomLevel - baseZoom;
            return baseResolution / Math.Pow(2, diff);
        }

        private void MapControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // [최적화 1] 이벤트 실행 횟수 제한 (Throttling)
            // 마지막 실행 후 33ms가 지나지 않았으면 아무것도 하지 않고 종료한다.
            if (DateTime.Now - _lastMouseMoveTime < _mouseMoveInterval)
                return;
            _lastMouseMoveTime = DateTime.Now;

            Point mousePos = e.GetPosition(mapControl);


            bool isFocusModeActive = false;

           
            if (!isFocusModeActive)
            {
                // 커서 좌표는 계속 업데이트 해줄 수 있음 (선택적)
                //Point mousePos = e.GetPosition(mapControl);
                CoordPoint cp = mapControl.ScreenPointToCoordPoint(mousePos);
                if (cp == null) return;
                ViewModel_Unit_Map.SingletonInstance.MapCursorLat = cp.GetY();
                ViewModel_Unit_Map.SingletonInstance.MapCursorLon = cp.GetX();
                return;
            }

            // [최적화 3] 지리 좌표 변환은 정말 필요할 때 딱 한 번만 실행한다.
            //Point finalMousePos = e.GetPosition(mapControl);
            CoordPoint finalCp = mapControl.ScreenPointToCoordPoint(mousePos);
            if (finalCp == null) return;

            GeoPoint cursor = new GeoPoint(finalCp.GetY(), finalCp.GetX());
            ViewModel_Unit_Map.SingletonInstance.MapCursorLat = cursor.Latitude;
            ViewModel_Unit_Map.SingletonInstance.MapCursorLon = cursor.Longitude;

        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
    
        }


        private ToolTip _currentToolTip;
        private void Element_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is UnitMapObjectInfo unit)
            {
                _currentToolTip = new ToolTip
                {
                    Content = $"ID: {unit.ID}\nType: {unit.TypeString}\nPlatform: {unit.PlatformString}",
                    PlacementTarget = border,
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
                    Background = Application.Current.Resources["MLAH_COLOR_BG_Brush"] as Brush,
                    Foreground = Brushes.White,
                    IsOpen = true
                };
            }
        }

        private void Element_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_currentToolTip != null)
            {
                _currentToolTip.IsOpen = false;
                _currentToolTip = null;
            }
        }

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is UnitMapObjectInfo unit)
            {
                CommonEvent.OnMapUnitObjectClicked?.Invoke(unit.ID);
            }
        }


        private void MapControl_MouseButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //var map = param as MapControl;
            //if (map == null) return;

            Point mousePos = e.GetPosition(mapControl);
            CoordPoint cp = mapControl.ScreenPointToCoordPoint(mousePos);
            if (cp == null) return;

            double lat = cp.GetY();
            double lon = cp.GetX();
            GeoPoint clickGeo = new GeoPoint(lat, lon);

            bool isRightClick = (Mouse.RightButton == MouseButtonState.Pressed);

  
        }



        #region 직선 보조 함수
        /* 반폭(미터) 계산 */
        private int PerpDistMeters(GeoPoint A, GeoPoint B, GeoPoint P)
        {
            double mPerDegLat = 111000.0;
            double mPerDegLon = Math.Cos(((A.Latitude + B.Latitude) / 2) * Math.PI / 180.0) * 111000.0;

            double Ax = (B.Longitude - A.Longitude) * mPerDegLon;
            double Ay = (B.Latitude - A.Latitude) * mPerDegLat;
            double Px = (P.Longitude - A.Longitude) * mPerDegLon;
            double Py = (P.Latitude - A.Latitude) * mPerDegLat;

            double segLen = Math.Sqrt(Ax * Ax + Ay * Ay);
            if (segLen < 1e-6) return 0;
            return (int)Math.Abs(Px * (Ay / segLen) - Py * (Ax / segLen));
        }

        /* 선분 AB에 직교하는 단위 벡터(m 단위) */
        private (double x, double y) UnitPerp(GeoPoint A, GeoPoint B)
        {
            double mPerDegLat = 111000.0;
            double mPerDegLon = Math.Cos(((A.Latitude + B.Latitude) / 2) * Math.PI / 180.0) * 111000.0;

            double vx = (B.Longitude - A.Longitude) * mPerDegLon;
            double vy = (B.Latitude - A.Latitude) * mPerDegLat;
            double len = Math.Sqrt(vx * vx + vy * vy);
            if (len < 1e-6) return (0, 0);
            return (-vy / len, vx / len);       // 오른쪽 90°
        }


        // (lat, lon)을 meter 단위 (dx, dy)만큼 이동
        private GeoPoint Offset(GeoPoint src, double ux, double uy, double dist)
        {
            double latDeg = (uy * dist) / 111000.0;
            double lonDeg = (ux * dist) / (Math.Cos(src.Latitude * Math.PI / 180.0) * 111000.0);
            return new GeoPoint(src.Latitude + latDeg, src.Longitude + lonDeg);
        }

        /* 모든 미리보기 객체/상태 초기화 */
        private void ResetLinearDrawing()
        {
            _state = DrawState.None;
            _linePoints.Clear();
            //_prevHalf = -1;

            //foreach (var l in _previewSegments)
            //    ViewModel_Unit_Map.SingletonInstance.TempINITMissionLineList.Remove(l);
            //foreach (var p in _previewRects)
            //    ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonList.Remove(p);

            _previewSegments.Clear();
            _previewRects.Clear();

            // ▷▶ 추가
            if (_ghostSegment != null)
            {
                //ViewModel_Unit_Map.SingletonInstance.TempINITMissionLineList.Remove(_ghostSegment);
                _ghostSegment = null;
            }
        }

        #endregion 직선 보조 함수

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //ViewModel_Unit_Map.SingletonInstance.InitializeFixedPolygons();
            Dispatcher.BeginInvoke(new Action(() =>
            {
            }), System.Windows.Threading.DispatcherPriority.Background);

            // 이벤트 핸들러는 한 번만 필요하므로, 실행 후 제거하는 것이 좋습니다.
            this.Loaded -= UserControl_Loaded;
        }

        private void mapControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 컨트롤의 새 크기를 가져옵니다.
            double actualWidth = e.NewSize.Width;
            double actualHeight = e.NewSize.Height;

            if (actualWidth > 0 && actualHeight > 0 && _focusBrush != null)
            {
                // 우리가 원하는 원의 '고정 픽셀 반지름'을 정합니다.
                const double focusPixelRadius = 500.0;

                // 고정된 픽셀 크기를 기준으로 상대적인 RadiusX와 RadiusY를 다시 계산합니다.
                _focusBrush.RadiusX = focusPixelRadius / actualWidth;
                _focusBrush.RadiusY = focusPixelRadius / actualHeight;
            }
        }
    }

}

