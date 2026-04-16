using DevExpress.Map;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Map;
using DevExpress.Xpf.Map.Native;
using Microsoft.CodeAnalysis;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;



namespace MLAH_Controller
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
            InitializeComponent();
            //mapControl.MapEditor.EditorPanelOptions.Visible = false;
            // 1. [중요] NRE 방지를 위해 Visible은 건드리지 않거나 True로 둡니다.
            // (이 코드가 없으면 기본값이 True이므로 굳이 안 써도 되지만, 명시적으로 초기화합니다)
            //if (mapControl.MapEditor != null)
            //{
            //    mapControl.MapEditor.EditorPanelOptions = new MapEditorPanelOptions()
            //    {
            //        Visible = true // ★ 켜둬야 내부 로직이 안 죽습니다.
            //    };
            //}


            var _ = new DevExpress.Xpf.Map.MapControl();
            this.DataContext = ViewModel_Unit_Map.SingletonInstance;
        
            if (_instance == null)
            {
                _instance = this;
            }

            this.Loaded += OnViewLoaded;
            this.Unloaded += OnViewUnloaded;

            if (mapControl.Layers.Count > 0 && mapControl.Layers[0] is ImageLayer baseLayer)
            {
                // LocalTileSource 연결
                var localSource = new ImageTileDataProvider()
                {
                    TileSource = new LocalTileSource()
                };
                baseLayer.DataProvider = localSource;
            }

            InitializeFocusEffect();

            // 버튼 텍스트 초기화
            btnToggleMap.Content = "Switch to AzureWeb";

            string srtmPath = System.IO.Path.Combine(CommonUtil.ExecutableDirectory, "srtm_62_05.tif");
            ViewModel_Unit_Map.SingletonInstance.InitializeSrtm(srtmPath);

        }

        #region [2] LifeCycle (Loaded / Unloaded)
        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel_Unit_Map.SingletonInstance;

            // 1. 이벤트 구독 (중복 방지를 위해 -= 후 += 하거나, Loaded가 한 번만 불린다는 보장이 있으면 +=만)
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.PropertyChanged += OnViewModelPropertyChanged;

            // 2. 맵 타일 소스 초기화
            if (mapControl.Layers.Count > 0 && mapControl.Layers[0] is ImageLayer baseLayer)
            {
                if (baseLayer.DataProvider == null || !(baseLayer.DataProvider is ImageTileDataProvider))
                {
                    baseLayer.DataProvider = new ImageTileDataProvider { TileSource = new LocalTileSource() };
                }
            }

            // 3. SRTM 초기화
            string srtmPath = System.IO.Path.Combine(CommonUtil.ExecutableDirectory, "srtm_62_05.tif");
            vm.InitializeSrtm(srtmPath);

            // 4. 고정 폴리곤 초기화 (백그라운드)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                vm.InitializeFixedPolygons();}), DispatcherPriority.Background);

            // NEX1 버전이면 Web Map 버튼 숨김
            if (CommonUtil.IPConfig.IsNex1)
            {
                btnToggleMap.Visibility = System.Windows.Visibility.Collapsed;
            }

        
        }

       

        private void OnViewUnloaded(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel_Unit_Map.SingletonInstance;
            // 이벤트 구독 해제 (메모리 누수 방지)
            vm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        // ViewModel 속성 변경 감지
        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vm = this.DataContext as ViewModel_Unit_Map;
            if (vm == null) return;

            // 1. 비행 가능 구역 편집 모드 변경 감지
            if (e.PropertyName == nameof(ViewModel_Unit_Map.IsFlightAreaEditMode))
            {
                if (vm.IsFlightAreaEditMode) StartEditing(ViewModel_Unit_Map.EditLayerType.FlightArea);
                else EndEditing(ViewModel_Unit_Map.EditLayerType.FlightArea);
            }
            // 2. 비행 금지 구역 편집 모드 변경 감지
            else if (e.PropertyName == nameof(ViewModel_Unit_Map.IsProhibitedAreaEditMode))
            {
                if (vm.IsProhibitedAreaEditMode) StartEditing(ViewModel_Unit_Map.EditLayerType.ProhibitedArea);
                else EndEditing(ViewModel_Unit_Map.EditLayerType.ProhibitedArea);
            }
            // 3. 초기 임무 폴리곤 편집 모드(신규 임무 생성) 변경 감지
            else if (e.PropertyName == nameof(ViewModel_Unit_Map.IsINITMissionPolygonEditMode))
            {
                if (vm.IsINITMissionPolygonEditMode) StartEditing(ViewModel_Unit_Map.EditLayerType.InitMission);
                else EndEditing(ViewModel_Unit_Map.EditLayerType.InitMission);
            }
            // 3. 초기 임무 폴리곤 편집 모드(기존 임무 수정) 변경 감지
            else if (e.PropertyName == nameof(ViewModel_Unit_Map.IsTempINITMissionPolygonEditMode))
            {
                if (vm.IsTempINITMissionPolygonEditMode) StartEditing(ViewModel_Unit_Map.EditLayerType.TempInitMission);
                else EndEditing(ViewModel_Unit_Map.EditLayerType.TempInitMission);
            }
            else if (e.PropertyName == nameof(ViewModel_Unit_Map.FilterFlightRefInfo))
            {
                FlightAreaPolygonLayer.Visibility = vm.FilterFlightRefInfo ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                ProhibitedAreaPolygonLayer.Visibility = vm.FilterFlightRefInfo ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                TakeOverPointLayer.Visibility = vm.FilterFlightRefInfo ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                HandOverPointLayer.Visibility = vm.FilterFlightRefInfo ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            else if (e.PropertyName == nameof(ViewModel_Unit_Map.FilterLAHPlan))
            {
                LAHStaticLayer.Visibility = vm.FilterLAHPlan ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                LAHWaypointMarkerLayer.Visibility = vm.FilterLAHPlan ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            else if (e.PropertyName == nameof(ViewModel_Unit_Map.FilterUAVPlan))
            {
                UAVWaypointLayer.Visibility = vm.FilterUAVPlan ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                UAVWaypointMarkerLayer.Visibility = vm.FilterUAVPlan ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }

        private void MapEditor_MapItemEdited(object sender, MapItemEditedEventArgs e)
        {
            var vm = this.DataContext as ViewModel_Unit_Map;
            if (vm == null) return;

            // 편집된 아이템 가져오기
            MapPolygon editedPoly = null;
            if (e.Items != null && e.Items.Any())
            {
                editedPoly = e.Items.FirstOrDefault() as MapPolygon;
            }

            // 원본 객체(Tag)가 있고, 좌표가 변경되었을 때
            if (editedPoly != null && editedPoly.Tag is CustomMapPolygon originalPoly)
            {
                // 1. 원본 폴리곤(UI 표시용) 좌표 갱신
                originalPoly.Points.Clear();
                foreach (var p in editedPoly.Points)
                {
                    originalPoly.Points.Add(new GeoPoint(p.GetY(), p.GetX()));
                }

                // 2. 변경된 좌표 리스트 준비 (LINQ Cast 사용)
                List<GeoPoint> currentPoints = originalPoly.Points.Cast<GeoPoint>().ToList();

                // 3. ★ [수정됨] 현재 편집 모드에 따라 알맞은 이벤트 발생
                // (ViewModel_UC_Unit_MissionPackage의 그리드를 갱신하기 위함)

                switch (vm.CurrentEditLayer)
                {
                    case ViewModel_Unit_Map.EditLayerType.FlightArea:
                        CommonEvent.OnFlightAreaPolygonUpdated?.Invoke(currentPoints);
                        break;

                    case ViewModel_Unit_Map.EditLayerType.ProhibitedArea:
                        CommonEvent.OnProhibitedAreaPolygonUpdated?.Invoke(currentPoints);
                        break;

                    case ViewModel_Unit_Map.EditLayerType.InitMission:
                        // 기존 임무 수정 모드
                        CommonEvent.OnINITMissionPolygonUpdated?.Invoke(currentPoints);
                        break;

                    case ViewModel_Unit_Map.EditLayerType.TempInitMission:
                        // ★ [핵심] 임무 생성 중 폴리곤 수정 모드 (Temp 레이어)
                        // 여기서도 동일하게 업데이트 이벤트를 호출해줘야 ViewModel이 변경된 좌표를 받습니다.
                        CommonEvent.OnINITMissionPolygonUpdated?.Invoke(currentPoints);
                        break;
                }
            }

        }
        #endregion
        const string Azurekey = "CDOhtwNBSsbmBiN3rUEjmBJGHW2tRMbp5XVwu4J55VBZg8PdRe9MJQQJ99BEACYeBjFllM6LAAAgAZMP1cA4";

        // 현재 지도 모드 상태 (true: Web, false: Local)
        private bool isWebMapMode = false;

        // [성능 최적화용] MouseMove 이벤트 조절을 위한 변수
        private DateTime _lastMouseMoveTime = DateTime.MinValue;
        //private readonly TimeSpan _mouseMoveInterval = TimeSpan.FromMilliseconds(33); // 약 33 FPS
        private readonly TimeSpan _mouseMoveInterval = TimeSpan.FromMilliseconds(16); // 약 60 FPS

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

            FocusRectangle.Fill = _focusBrush;
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


      

        // 폴리곤 관련
        public PolygonState _polygonState = PolygonState.None;
        public List<GeoPoint> _polyPoints = new List<GeoPoint>(); // 클릭으로 찍은 좌표들
        public MapLine _currentLine = null;  // "마우스 무브"에 따라 바뀌는 임시 선


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
            //if (DateTime.Now - _lastMouseMoveTime < _mouseMoveInterval)
            //    return;
            //_lastMouseMoveTime = DateTime.Now;

            Point mousePos = e.GetPosition(mapControl);
            var vm = ViewModel_Unit_Map.SingletonInstance;

            // --- 1. 포커스 모드 활성화 여부 체크 ---
            // 점, 선, 다각형 등 그리기 모드가 하나라도 켜져 있는지 확인
            bool isFocusModeActive =
                ViewModel_ScenarioObject_PopUp.SingletonInstance.POSSelectChecked ||
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.PointTypeChecked ||
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.LinearTypeChecked ||
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.PolygonTypeChecked ||
                ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaChecked ||
                ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaChecked ||
                ViewModel_UC_Unit_MissionPackage.SingletonInstance.TakeOverChecked ||
                ViewModel_UC_Unit_MissionPackage.SingletonInstance.HandOverChecked ||
                ViewModel_UC_Unit_MissionPackage.SingletonInstance.RTBChecked ||
                ViewModel_UC_Unit_Developer.SingletonInstance.DevelopPathChecked;

            // --- 2. 포커스 효과 업데이트 또는 숨기기 ---
            if (isFocusModeActive)
            {
                // 효과가 숨겨져 있었다면 다시 보이게 함
                if (FocusOverlay.Visibility == Visibility.Collapsed)
                {
                    FocusOverlay.Visibility = Visibility.Visible;
                }

                // 현재 마우스 위치 가져오기 (mapControl 기준)
                //var mousePos = e.GetPosition(mapControl);

                // 십자선 위치 업데이트
                CrosshairX.X1 = 0;
                CrosshairX.Y1 = mousePos.Y;
                CrosshairX.X2 = mapControl.ActualWidth;
                CrosshairX.Y2 = mousePos.Y;

                CrosshairY.X1 = mousePos.X;
                CrosshairY.Y1 = 0;
                CrosshairY.X2 = mousePos.X;
                CrosshairY.Y2 = mapControl.ActualHeight;

                // 포커스(비네트) 효과 중심점 업데이트
                //Point relativePos = new Point(mousePos.X / mapControl.ActualWidth, mousePos.Y / mapControl.ActualHeight);
                //_focusBrush.Center = relativePos;
                //_focusBrush.GradientOrigin = relativePos;

                // 포커스(비네트) 효과 중심점 업데이트
                if (_focusBrush != null)
                {
                    // 좌표 정규화 (0.0 ~ 1.0)
                    double rx = mousePos.X / mapControl.ActualWidth;
                    double ry = mousePos.Y / mapControl.ActualHeight;

                    // 값 유효성 체크 (0~1 사이 안전장치)
                    if (rx < 0) rx = 0; if (rx > 1) rx = 1;
                    if (ry < 0) ry = 0; if (ry > 1) ry = 1;

                    Point relativePos = new Point(rx, ry);
                    _focusBrush.Center = relativePos;
                    _focusBrush.GradientOrigin = relativePos;
                }

            }
            else
            {
                // 어떤 모드도 켜져 있지 않다면 효과를 숨김
                if (FocusOverlay.Visibility == Visibility.Visible)
                {
                    FocusOverlay.Visibility = Visibility.Collapsed;
                }
            }

            // 마지막 실행 후 33ms가 지나지 않았으면 "무거운 로직"은 건너뛴다.
            // 하지만 위의 "시각적 업데이트"는 이미 수행되었으므로 마우스는 부드럽게 보임.
            if ((DateTime.Now - _lastMouseMoveTime) < _mouseMoveInterval)
                return;

            _lastMouseMoveTime = DateTime.Now;

            // [최적화 2] 빠른 탈출 (Early Exit)
            // 그리기 모드가 아니라면, 더 이상 비싼 계산을 할 필요가 없으므로 여기서 함수 종료!
            if (!isFocusModeActive)
            {
                //커서 좌표 변환
                CoordPoint Cp = mapControl.ScreenPointToCoordPoint(mousePos);
                if (Cp == null) return;

                double lat = Cp.GetY();
                double lon = Cp.GetX();

                // 1. 위도/경도 업데이트
                vm.MapCursorLat = lat;
                vm.MapCursorLon = lon;

                // 2. [추가] 고도 추출 및 업데이트
                if (vm.SrtmReaderInstance != null)
                {
                    short elevation = vm.SrtmReaderInstance.GetElevation(lat, lon);

                    // 데이터가 없는 곳(-32768)은 0이나 NaN으로 처리
                    vm.MapCursorAlt = (elevation == -32768) ? 0 : elevation;
                }
                else
                {
                    vm.MapCursorAlt = -32768; // SRTM 로드 실패 시
                }


                return;
            }

            // [최적화 3] 지리 좌표 변환은 정말 필요할 때 딱 한 번만 실행한다.
            //Point finalMousePos = e.GetPosition(mapControl);
            CoordPoint finalCp = mapControl.ScreenPointToCoordPoint(mousePos);
            if (finalCp == null) return;

            GeoPoint cursor = new GeoPoint(finalCp.GetY(), finalCp.GetX());
            vm.MapCursorLat = cursor.Latitude;
            vm.MapCursorLon = cursor.Longitude;
        
            // 2. [추가] 고도 추출 및 업데이트
            if (vm.SrtmReaderInstance != null)
            {
                short elevation = vm.SrtmReaderInstance.GetElevation(cursor.Latitude, cursor.Longitude);

                // 데이터가 없는 곳(-32768)은 0이나 NaN으로 처리
                vm.MapCursorAlt = (elevation == -32768) ? 0 : elevation;
            }
            else
            {
                vm.MapCursorAlt = -32768; // SRTM 로드 실패 시
            }

            /*──── 선형 폭 미리보기 ────*/
            // (1) 점 추가 단계의 유령선  ─────────────────────────
            if (ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.LinearTypeChecked &&
                _state == DrawState.AddingPoints &&
                _linePoints.Count > 0)
            {
                GeoPoint last = _linePoints[^1];   // 마지막 확정 점

                if (_ghostSegment == null)
                {
                    _ghostSegment = new MapLine { Stroke = Brushes.Red };
                    ViewModel_Unit_Map.SingletonInstance.TempINITMissionLineList.Add(_ghostSegment);
                }
                _ghostSegment.Point1 = last;
                _ghostSegment.Point2 = cursor;     // 커서 위치
                                                   // 여기서 return 하지 마세요 ‑‑ 폭 모드가 아닐 때만 실행되고 아래로 계속 내려갑니다.
            }

            // (2) 폭 조절 단계의 사각형  ─────────────────────────
            if (ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.LinearTypeChecked &&
                _state == DrawState.WidthAdjust && _linePoints.Count >= 2)
            {
                double half = PerpDistMeters(_linePoints[^2], _linePoints[^1], cursor);

                // 모든 선분에 동일 폭 사각형 업데이트
                for (int i = 0; i < _linePoints.Count - 1; i++)
                {
                    GeoPoint A = _linePoints[i];
                    GeoPoint B = _linePoints[i + 1];

                    // 직교 단위 벡터
                    var (nx, ny) = UnitPerp(A, B);

                    // 왼쪽/오른쪽으로 offset
                    GeoPoint A1 = Offset(A, nx, ny, half);   // 한쪽
                    GeoPoint A2 = Offset(A, -nx, -ny, half);   // 반대쪽
                    GeoPoint B1 = Offset(B, nx, ny, half);
                    GeoPoint B2 = Offset(B, -nx, -ny, half);

                    var rect = _previewRects[i];
                    rect.Points.Clear();
                    rect.Points.Add(A1); rect.Points.Add(B1); rect.Points.Add(B2); rect.Points.Add(A2);
                }
                return;

            }

            else if (ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.PolygonTypeChecked)
            {
                if (_polygonState == PolygonState.Drawing && _currentLine != null)
                {
                    _currentLine.Point2 = cursor;
                }
            }
            else if (ViewModel_UC_Unit_Developer.SingletonInstance.DevelopPathChecked)
            {
                if (_developpathState == DevelopPathState.Drawing && _developcurrentPolyline != null)
                {
                    // 마지막 점 = 마우스 위치
                    var points = _developcurrentPolyline.Points;
                    if (points.Count > 0)
                    {
                        int lastIndex = points.Count - 1;
                        points[lastIndex] = cursor;
                    }
                }
            }
            else if (ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaChecked)
            {
                if (_polygonState == PolygonState.Drawing && _currentLine != null)
                {
                    _currentLine.Point2 = cursor;
                }
            }

            else if (ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaChecked)
            {
                if (_polygonState == PolygonState.Drawing && _currentLine != null)
                {
                    _currentLine.Point2 = cursor;
                }
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (View_ScenarioObject_PopUp.SingletonInstance.IsVisible)
            {
                var vm = ViewModel_ScenarioObject_PopUp.SingletonInstance;
                if (vm == null)
                    return;

                if (e.Key == Key.Enter)
                {
                    if (vm.ConfirmCommand.CanExecute(this))
                        vm.ConfirmCommand.Execute(this);

                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    if (vm.CancelCommand.CanExecute(this))
                        vm.CancelCommand.Execute(this);

                    e.Handled = true;
                }
            }
        }


        private ToolTip _currentToolTip;
        //private void Element_MouseEnter(object sender, MouseEventArgs e)
        //{
        //    if (sender is Border border && border.DataContext is UnitMapObjectInfo unit)
        //    {
        //        _currentToolTip = new ToolTip
        //        {
        //            Content = $"ID: {unit.ID}\nType: {unit.TypeString}\nPlatform: {unit.PlatformString}",
        //            PlacementTarget = border,
        //            Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
        //            Background = Application.Current.Resources["MLAH_COLOR_BG_Brush"] as Brush,
        //            Foreground = Brushes.White,
        //            IsOpen = true
        //        };
        //    }
        //}
        private void Element_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is UnitMapObjectInfo unit)
            {
                _currentToolTip = new ToolTip
                {
                    // 1. 툴팁의 내용물(데이터)로 Unit 객체 자체를 넘깁니다.
                    Content = unit,

                    // 2. 아까 만든 XAML 디자인(DataTemplate)을 적용합니다.
                    ContentTemplate = (DataTemplate)this.FindResource("UnitToolTipTemplate"),

                    // 3. 툴팁 자체의 배경/테두리는 없애고 우리가 만든 Border만 보이게 합니다.
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),

                    PlacementTarget = border,
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
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

            /*─────────────────── ① 선형 모드 ───────────────────*/
            if (ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.LinearTypeChecked)
            {
                /* ①‑A : 우클릭 → 점 입력 종료하고 폭 조절 모드로 */
                if (isRightClick && _state == DrawState.AddingPoints && _linePoints.Count >= 2)
                {
                    _state = DrawState.WidthAdjust;

                    // 유령선 제거 (우클릭 시점에)
                    if (_ghostSegment != null)
                    {
                        ViewModel_Unit_Map.SingletonInstance.TempINITMissionLineList.Remove(_ghostSegment);
                        _ghostSegment = null;
                    }

                    // 사각형 미리보기 객체를 선분 개수만큼 준비
                    _previewRects.Clear();
                    for (int i = 0; i < _linePoints.Count - 1; i++)
                    {
                        var rect = new CustomMapPolygon
                        {
                            IsHitTestVisible = false,
                            Fill = null,
                            Stroke = Brushes.Red
                        };
                        _previewRects.Add(rect);
                        ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonList.Add(rect);
                    }
                    return;
                }

                /* ①‑B : 좌클릭 */
                if (!isRightClick)
                {
                    switch (_state)
                    {
                        /* 점 추가 단계 */
                        case DrawState.None:
                        case DrawState.AddingPoints:
                            _state = DrawState.AddingPoints;
                            _linePoints.Add(clickGeo);

                            if (_ghostSegment != null)
                            {
                                ViewModel_Unit_Map.SingletonInstance.TempINITMissionLineList.Remove(_ghostSegment);
                                _ghostSegment = null;
                            }

                            // 두 번째 점부터 선분 미리보기
                            if (_linePoints.Count >= 2)
                            {
                                var seg = new MapLine
                                {
                                    Point1 = _linePoints[^2],
                                    Point2 = _linePoints[^1],
                                    Stroke = Brushes.Red
                                };
                                _previewSegments.Add(seg);
                                ViewModel_Unit_Map.SingletonInstance.TempINITMissionLineList.Add(seg);
                            }
                            return;

                        /* 폭 확정 단계 */
                        case DrawState.WidthAdjust:
                            if (_linePoints.Count < 2) { ResetLinearDrawing(); return; }

                            int half = PerpDistMeters(_linePoints[^2], _linePoints[^1], clickGeo);
                            int widthMeters = half * 2;


                            // 1. '여러 사각형들의 목록'을 담을 리스트를 생성한다.
                            var segmentRectangleList = new List<List<GeoPoint>>();

                            // 2. 각 선분(segment)을 순회하며 독립적인 사각형을 계산한다.
                            //    (MouseMove의 미리보기 로직과 완전히 동일)
                            for (int i = 0; i < _linePoints.Count - 1; i++)
                            {
                                GeoPoint A = _linePoints[i];
                                GeoPoint B = _linePoints[i + 1];

                                var (nx, ny) = UnitPerp(A, B);

                                GeoPoint A1 = Offset(A, nx, ny, half);
                                GeoPoint A2 = Offset(A, -nx, -ny, half);
                                GeoPoint B1 = Offset(B, nx, ny, half);
                                GeoPoint B2 = Offset(B, -nx, -ny, half);

                                // 3. 계산된 사각형 꼭짓점 4개를 새로운 리스트에 담는다.
                                var rectPoints = new List<GeoPoint> { A1, B1, B2, A2 };

                                // 4. 이 사각형(점 4개 리스트)을 전체 목록에 추가한다.
                                segmentRectangleList.Add(rectPoints);
                            }

                            // 5. DTO 객체를 생성하고, 계산된 데이터들을 채워 넣는다.
                            var resultSet = new LinearMissionResultSet
                            {
                                CenterPoints = new List<GeoPoint>(_linePoints),
                                SegmentRectangles = segmentRectangleList, // 새로 계산한 사각형 목록
                                WidthMeters = widthMeters
                            };

                            // 6. DTO를 이벤트로 전달한다.
                            CommonEvent.OnINITMissionLineSet?.Invoke(resultSet);

                            ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.LinearTypeChecked = false;
                            _state = DrawState.None;
                            _ghostSegment = null;
                            return;
                    }
                }
            }
            else if (ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.PointTypeChecked)
            {
                GeoPoint clickedPoint = clickGeo;
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPointList.Clear();
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPointList.Add(clickedPoint);
                CommonEvent.OnINITMissionPointSet?.Invoke(clickedPoint.Latitude, clickedPoint.Longitude);
            }
            else if (ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.PolygonTypeChecked)
            {
                if (isRightClick)
                {
                    // (1) 오른쪽 클릭 -> 폴리곤 완성
                    if (_polygonState == PolygonState.Drawing && _polyPoints.Count >= 3)
                    {
                        // 마지막 점과 첫 점 연결 (선택적으로 '확정 선'을 추가할 수도 있음)

                        // 실제 MapPolygon 만들기
                        var mapPoly = new CustomMapPolygon();
                        foreach (var gp in _polyPoints)
                            mapPoly.Points.Add(gp);

                        // 첫 점과 마지막 점은 자동으로 닫힘
                        // or 만약 자동 닫힘이 안 된다면 "mapPoly.Points.Add(_polyPoints[0])" 을 할 수도 있음

                        // 채움색(빨간색), 투명도 0.5
                        mapPoly.Fill = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                        //   └ 128 = 50% 투명(0~255),  (255,0,0)은 R,G,B 순서

                        // 폴리곤 테두리(선) 색상·두께도 설정 가능
                        mapPoly.Stroke = Brushes.Red;
                        //mapPoly.StrokeThickness = 2;

                        // List에 추가 => 지도에 표시
                        ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonList.Add(mapPoly);

                        // Delegate로 polygon 좌표 넘기기 (다각형 완성 콜백)
                        // Example:
                        CommonEvent.OnINITMissionPolygonSet?.Invoke(_polyPoints);

                        // 상태 리셋
                        _polygonState = PolygonState.None;
                        _polyPoints.Clear();
                        _currentLine = null;

                        // PolygonTypeChecked 해제
                        ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.PolygonTypeChecked = false;

                        ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Clear();

                        //저장을 해야 Clear를 하는게 맞음
                        //ViewModel_Map_Dev_OSM.SingletonInstance.TempListsClear();
                        //isRightClick = false;
                    }
                }
                else
                {
                    // (2) 왼쪽 클릭 -> 점 추가
                    switch (_polygonState)
                    {
                        case PolygonState.None:
                            // 처음 시작
                            _polygonState = PolygonState.Drawing;
                            _polyPoints.Clear();
                            _polyPoints.Add(clickGeo);

                            // currentLine: "마우스 무브" 임시선 => last point -> mouse
                            _currentLine = new MapLine
                            {
                                Point1 = clickGeo,
                                Point2 = clickGeo,
                                Stroke = new SolidColorBrush(Colors.Red)
                            };
                            ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Add(_currentLine);
                            break;

                        case PolygonState.Drawing:
                            // 이미 그리는 중
                            // 1) 직전 점 -> 새 점 확정 선 만들기 (원하면)
                            //    예: MapLine line = new MapLine { ... };
                            //    TempCompINITMissionLineList.Add(line);

                            // 2) _polyPoints.Add(new point)
                            if (_polyPoints.Count > 0)
                            {
                                // 직전 점
                                GeoPoint prevPoint = _polyPoints[_polyPoints.Count - 1];
                                // 확정 선
                                var confirmLine = new MapLine
                                {
                                    Point1 = prevPoint,
                                    Point2 = clickGeo,
                                    Stroke = new SolidColorBrush(Colors.Red)
                                };
                                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Add(confirmLine);
                            }
                            _polyPoints.Add(clickGeo);

                            // currentLine 다시 갱신 (새로 찍은 점 -> mouse)
                            _currentLine = new MapLine
                            {
                                Point1 = clickGeo,
                                Point2 = clickGeo,
                                Stroke = new SolidColorBrush(Colors.Red)

                            };
                            ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Add(_currentLine);
                            break;
                    }
                }
            }

            else if (ViewModel_UC_Unit_Developer.SingletonInstance.DevelopPathChecked)
            {
                // 경로(Polyline) 모드라고 가정
                if (isRightClick)
                {
                    // (A) 만약 사용자 “우클릭”으로 경로 그리기 끝내기
                    if (_developpathState == DevelopPathState.Drawing && _developcurrentPolyline != null)
                    {
                        // 임시 점(마지막 점)이 추가되어 있다면 제거 (확정된 점까지만 남도록)
                        if (_developcurrentPolyline.Points.Count > 1)
                            _developcurrentPolyline.Points.RemoveAt(_developcurrentPolyline.Points.Count - 1);
                        // 여기서 경로 완성 처리
                        // 예: Delegate Invoke해서 _currentPolyline.Points를 넘기거나, 상태 초기화
                        CommonEvent.OnDevelopPathPlanSet?.Invoke(_developcurrentPolyline.Points.ToList());

                        // 상태 초기화
                        _developpathState = DevelopPathState.None;
                        _developcurrentPolyline = null;

                        // Mode off
                        ViewModel_UC_Unit_Developer.SingletonInstance.DevelopPathChecked = false;
                        //isRightClick = false;
                    }
                }
                else
                {
                    // (B) 왼쪽 클릭 => 점 추가
                    switch (_developpathState)
                    {
                        case DevelopPathState.None:
                            // 첫 클릭 => 새 Polyline 생성
                            _developpathState = DevelopPathState.Drawing;

                            _developcurrentPolyline = new MapPolyline
                            {
                                Stroke = Brushes.Red,  // 기본 스타일
                                //StrokeThickness = 2
                            };
                            // 초기 Points = [clickGeo, clickGeo]
                            //  → 맨 끝 점을 마우스 무브로 실시간 갱신
                            _developcurrentPolyline.Points.Add(clickGeo);
                            _developcurrentPolyline.Points.Add(clickGeo);

                            // 지도에 추가
                            ViewModel_Unit_Map.SingletonInstance.TempUnitDevelopPathPlanList.Add(_developcurrentPolyline);
                            break;

                        case DevelopPathState.Drawing:
                            // 이미 그리는 중 => 
                            // 1) “마지막 점”을 현재 클릭 점으로 고정
                            var points = _developcurrentPolyline.Points;
                            if (points.Count > 0)
                            {
                                int lastIndex = points.Count - 1;
                                points[lastIndex] = clickGeo;  // 고정
                            }
                            // 2) 새로 “중복 점” 추가 => 다음 마우스 무브에 대해 임시 선분
                            _developcurrentPolyline.Points.Add(clickGeo);
                            break;
                    }
                }
            }
            //else if(ViewModel_Comp_ScenarioObjectSet_PopUp.SingletonInstance.POSSelectChecked)
            //{
            //    GeoPoint clickedPoint = clickGeo;
            //    CommonEvent.OnMapPOSSelect?.Invoke(clickedPoint.Latitude, clickedPoint.Longitude,0);
            //}
            else if (ViewModel_ScenarioObject_PopUp.SingletonInstance.POSSelectChecked)
            {
                GeoPoint clickedPoint = clickGeo;
                CommonEvent.OnMapPOSSelect?.Invoke(clickedPoint.Latitude, clickedPoint.Longitude, 0);
            }
            else if (ViewModel_UC_Unit_MissionPackage.SingletonInstance.TakeOverChecked)
            {
                GeoPoint clickedPoint = clickGeo;
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPointList.Clear();
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPointList.Add(clickedPoint);
                CommonEvent.OnTakeOverPointSet?.Invoke(clickedPoint.Latitude, clickedPoint.Longitude);
            }

            else if (ViewModel_UC_Unit_MissionPackage.SingletonInstance.HandOverChecked)
            {
                GeoPoint clickedPoint = clickGeo;
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPointList.Clear();
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPointList.Add(clickedPoint);
                CommonEvent.OnHandOverPointSet?.Invoke(clickedPoint.Latitude, clickedPoint.Longitude);
            }

            else if (ViewModel_UC_Unit_MissionPackage.SingletonInstance.RTBChecked)
            {
                GeoPoint clickedPoint = clickGeo;
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPointList.Clear();
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPointList.Add(clickedPoint);
                CommonEvent.OnRTBPointSet?.Invoke(clickedPoint.Latitude, clickedPoint.Longitude);
            }
            else if (ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaChecked)
            {
                if (isRightClick)
                {
                    // (1) 오른쪽 클릭 -> 폴리곤 완성
                    //if (_polygonState == PolygonState.Drawing && _polyPoints.Count >= 3)
                    {
                        // 마지막 점과 첫 점 연결 (선택적으로 '확정 선'을 추가할 수도 있음)

                        // 실제 MapPolygon 만들기
                        var mapPoly = new CustomMapPolygon();
                        foreach (var gp in _polyPoints)
                            mapPoly.Points.Add(gp);

                        // 첫 점과 마지막 점은 자동으로 닫힘
                        // or 만약 자동 닫힘이 안 된다면 "mapPoly.Points.Add(_polyPoints[0])" 을 할 수도 있음

                        // 채움색(빨간색), 투명도 0.5
                        mapPoly.Fill = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                        //   └ 128 = 50% 투명(0~255),  (255,0,0)은 R,G,B 순서

                        // 폴리곤 테두리(선) 색상·두께도 설정 가능
                        mapPoly.Stroke = Brushes.Red;
                        //mapPoly.StrokeThickness = 2;

                        // List에 추가 => 지도에 표시
                        ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonList.Add(mapPoly);

                        // Delegate로 polygon 좌표 넘기기 (다각형 완성 콜백)
                        // Example:
                        CommonEvent.OnFlightAreaPolygonSet?.Invoke(_polyPoints);

                        // 상태 리셋
                        _polygonState = PolygonState.None;
                        _polyPoints.Clear();
                        _currentLine = null;

                        // PolygonTypeChecked 해제
                        ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaChecked = false;

                        ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Clear();

                        //저장을 해야 Clear를 하는게 맞음
                        //ViewModel_Map_Dev_OSM.SingletonInstance.TempListsClear();
                        //isRightClick = false;
                    }
                    ForceFinishDrawing();
                }
                else
                {
                    // (2) 왼쪽 클릭 -> 점 추가
                    switch (_polygonState)
                    {
                        case PolygonState.None:
                            // 처음 시작
                            _polygonState = PolygonState.Drawing;
                            _polyPoints.Clear();
                            _polyPoints.Add(clickGeo);

                            // currentLine: "마우스 무브" 임시선 => last point -> mouse
                            _currentLine = new MapLine
                            {
                                Point1 = clickGeo,
                                Point2 = clickGeo,
                                Stroke = new SolidColorBrush(Colors.Red)
                            };
                            ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Add(_currentLine);
                            break;

                        case PolygonState.Drawing:
                            // 이미 그리는 중
                            // 1) 직전 점 -> 새 점 확정 선 만들기 (원하면)
                            //    예: MapLine line = new MapLine { ... };
                            //    TempCompINITMissionLineList.Add(line);

                            // 2) _polyPoints.Add(new point)
                            if (_polyPoints.Count > 0)
                            {
                                // 직전 점
                                GeoPoint prevPoint = _polyPoints[_polyPoints.Count - 1];
                                // 확정 선
                                var confirmLine = new MapLine
                                {
                                    Point1 = prevPoint,
                                    Point2 = clickGeo,
                                    Stroke = new SolidColorBrush(Colors.Red)
                                };
                                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Add(confirmLine);
                            }
                            _polyPoints.Add(clickGeo);

                            // currentLine 다시 갱신 (새로 찍은 점 -> mouse)
                            _currentLine = new MapLine
                            {
                                Point1 = clickGeo,
                                Point2 = clickGeo,
                                Stroke = new SolidColorBrush(Colors.Red)

                            };
                            ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Add(_currentLine);
                            break;
                    }
                }
            }

            else if (ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaChecked)
            {
                if (isRightClick)
                {
                    ForceFinishDrawing();
                }
                else
                {
                    // (2) 왼쪽 클릭 -> 점 추가
                    switch (_polygonState)
                    {
                        case PolygonState.None:
                            // 처음 시작
                            _polygonState = PolygonState.Drawing;
                            _polyPoints.Clear();
                            _polyPoints.Add(clickGeo);

                            // currentLine: "마우스 무브" 임시선 => last point -> mouse
                            _currentLine = new MapLine
                            {
                                Point1 = clickGeo,
                                Point2 = clickGeo,
                                Stroke = new SolidColorBrush(Colors.Red)
                            };
                            ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Add(_currentLine);
                            break;

                        case PolygonState.Drawing:
                            // 이미 그리는 중
                            // 1) 직전 점 -> 새 점 확정 선 만들기 (원하면)
                            //    예: MapLine line = new MapLine { ... };
                            //    TempCompINITMissionLineList.Add(line);

                            // 2) _polyPoints.Add(new point)
                            if (_polyPoints.Count > 0)
                            {
                                // 직전 점
                                GeoPoint prevPoint = _polyPoints[_polyPoints.Count - 1];
                                // 확정 선
                                var confirmLine = new MapLine
                                {
                                    Point1 = prevPoint,
                                    Point2 = clickGeo,
                                    Stroke = new SolidColorBrush(Colors.Red)
                                };
                                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Add(confirmLine);
                            }
                            _polyPoints.Add(clickGeo);

                            // currentLine 다시 갱신 (새로 찍은 점 -> mouse)
                            _currentLine = new MapLine
                            {
                                Point1 = clickGeo,
                                Point2 = clickGeo,
                                Stroke = new SolidColorBrush(Colors.Red)

                            };
                            ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Add(_currentLine);
                            break;
                    }
                }
            }
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

            foreach (var l in _previewSegments)
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionLineList.Remove(l);
            foreach (var p in _previewRects)
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonList.Remove(p);

            _previewSegments.Clear();
            _previewRects.Clear();

            // ▷▶ 추가
            if (_ghostSegment != null)
            {
                ViewModel_Unit_Map.SingletonInstance.TempINITMissionLineList.Remove(_ghostSegment);
                _ghostSegment = null;
            }
        }

        #endregion 직선 보조 함수

        // [4] 편집 시작 (데이터 복사 & 핸들 활성화)
        private void StartEditing(ViewModel_Unit_Map.EditLayerType layerType)
        {
            var vm = this.DataContext as ViewModel_Unit_Map;
            if (vm == null) return;

            // 1. [안전장치] 에디터 초기화
            if (mapControl.MapEditor != null)
            {
                mapControl.MapEditor.Mode = null;
                mapControl.MapEditor.ActiveLayer = null;
            }

            // 변수 준비 (Switch 문을 통해 할당)
            ObservableCollection<CustomMapPolygon> sourceList = null;
            MapItemStorage targetStorage = null;
            VectorLayer targetLayer = null;
            VectorLayer viewLayer = null; // 편집 중 흐릿하게 만들 뷰 레이어

            switch (layerType)
            {
                case ViewModel_Unit_Map.EditLayerType.FlightArea:
                    sourceList = vm.FlightAreaPolygonList;
                    targetStorage = FlightAreaEditStorage;
                    targetLayer = FlightAreaPolygonEditLayer;
                    viewLayer = FlightAreaPolygonLayer;
                    break;

                case ViewModel_Unit_Map.EditLayerType.ProhibitedArea:
                    sourceList = vm.ProhibitedAreaPolygonList;
                    targetStorage = ProhibitedAreaEditStorage;
                    targetLayer = ProhibitedAreaPolygonEditLayer;
                    viewLayer = ProhibitedAreaPolygonLayer;
                    break;

                case ViewModel_Unit_Map.EditLayerType.InitMission:
                    sourceList = vm.INITMissionPolygonList;
                    targetStorage = INITMissionPolygonEditStorage;
                    targetLayer = INITMissionPolygonEditLayer;
                    viewLayer = INITMissionPolygonLayer;
                    break;

                case ViewModel_Unit_Map.EditLayerType.TempInitMission:
                    sourceList = vm.TempINITMissionPolygonList;
                    targetStorage = TempINITMissionPolygonEditStorage;
                    targetLayer = TempINITMissionPolygonEditLayer;
                    viewLayer = TempINITMissionPolygonLayer;
                    break;
            }

            if (sourceList == null || targetStorage == null || targetLayer == null) return;

            // 2. 뷰 레이어 흐리게 처리
            if (viewLayer != null) viewLayer.Opacity = 0.2;

            // 3. 스토리지 초기화
            targetStorage.Items.Clear();

            // 4. 편집 대상 찾기 (패키지 VM에서 선택된 항목을 찾는 것이 가장 정확하나, 여기서는 리스트의 항목을 가져옴)
            // * 주의: 여러 개일 경우 선택된 것을 가져오는 로직으로 고도화 가능. 현재는 리스트 전체를 편집 대상으로 올리거나 첫 번째를 잡는 예시.
            // 여기서는 "리스트에 있는 모든 폴리곤"을 편집 가능하게 올리는 방식으로 구현
            foreach (var targetPoly in sourceList)
            {
                // 편집용 폴리곤 생성 (Deep Copy)
                MapPolygon editPoly = new MapPolygon();
                editPoly.Fill = targetPoly.Fill;
                editPoly.Stroke = Brushes.Cyan;
                editPoly.StrokeStyle = new StrokeStyle() { Thickness = 3 };

                foreach (var p in targetPoly.Points)
                {
                    editPoly.Points.Add(new GeoPoint(p.GetY(), p.GetX()));
                }
                editPoly.Tag = targetPoly; // ★ 원본 객체 연결 (매우 중요)

                targetStorage.Items.Add(editPoly);
            }

            // 5. 비동기로 에디터 활성화 (렌더링 타이밍 이슈 방지)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (mapControl.MapEditor != null)
                {
                    // 활성 레이어 설정
                    mapControl.MapEditor.ActiveLayer = targetLayer;
                    // 편집 모드 켜기
                    mapControl.MapEditor.Mode = new MapEditorEditMode();
                }

                // 필요 시 첫 번째 아이템 선택
                if (targetStorage.Items.Count > 0)
                {
                    targetLayer.SelectedItem = targetStorage.Items[0];
                }

            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // [5] 편집 종료 (정리)
        private void EndEditing(ViewModel_Unit_Map.EditLayerType layerType)
        {
            // 1. 에디터 끄기
            if (mapControl.MapEditor != null)
            {
                mapControl.MapEditor.Mode = null;
                mapControl.MapEditor.ActiveLayer = null;
            }

            // 2. 변수 준비
            MapItemStorage targetStorage = null;
            VectorLayer viewLayer = null;

            switch (layerType)
            {
                case ViewModel_Unit_Map.EditLayerType.FlightArea:
                    targetStorage = FlightAreaEditStorage;
                    viewLayer = FlightAreaPolygonLayer;
                    break;

                case ViewModel_Unit_Map.EditLayerType.ProhibitedArea:
                    targetStorage = ProhibitedAreaEditStorage;
                    viewLayer = ProhibitedAreaPolygonLayer;
                    break;

                case ViewModel_Unit_Map.EditLayerType.InitMission:
                    targetStorage = INITMissionPolygonEditStorage;
                    viewLayer = INITMissionPolygonLayer;
                    break;

                case ViewModel_Unit_Map.EditLayerType.TempInitMission:
                    targetStorage = TempINITMissionPolygonEditStorage;
                    viewLayer = TempINITMissionPolygonLayer;
                    break;
            }

            // 3. 데이터 클리어
            if (targetStorage != null) targetStorage.Items.Clear();

            // 4. 뷰 복구 (투명도 원복)
            if (viewLayer != null) viewLayer.Opacity = 1.0;
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

        //RTV맵
        private void OnMapWebRequest(object sender, MapWebRequestEventArgs e)
        {
            //e.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        }

        private void btnToggleMap_Click(object sender, RoutedEventArgs e)
        {
            // 맵 레이어 중 첫 번째(ImageLayer)를 찾습니다.
            // (보통 맨 밑바닥 레이어가 지도 타일이므로 index 0입니다.)
            if (mapControl.Layers.Count > 0 && mapControl.Layers[0] is ImageLayer baseLayer)
            {
                if (isWebMapMode)
                {
                    // Web -> Local 전환
                    var localSource = new ImageTileDataProvider()
                    {
                        TileSource = new LocalTileSource()
                    };

                    baseLayer.DataProvider = localSource;

                    btnToggleMap.Content = "AzureWeb Map"; // 다음 클릭 시 Web으로 가도록 텍스트 변경
                    isWebMapMode = false;
                }
                else
                {
                    // Local -> Web (Azure) 전환
                    var azureSource = new AzureMapDataProvider()
                    {
                        AzureKey = Azurekey,
                        Tileset = AzureTileset.Imagery // 위성 지도
                    };

                    baseLayer.DataProvider = azureSource;

                    btnToggleMap.Content = "Local Map"; // 다음 클릭 시 Local로 가도록 텍스트 변경
                    isWebMapMode = true;
                }
            }
        }

        /// <summary>
        /// [신규] 외부(엔터키 등)에서 호출 가능한 그리기 강제 종료 메서드
        /// </summary>
        public void ForceFinishDrawing()
        {
            // 1. 비행가능구역 그리기 중이라면?
            if (ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaChecked)
            {
                if (_polygonState == PolygonState.Drawing && _polyPoints.Count >= 3)
                {
                    // [수정] 람다식 'points => ...' 를 사용하여 Action<List<GeoPoint>> 타입으로 감싸줍니다.
                    FinishPolygonDrawing((points) => CommonEvent.OnFlightAreaPolygonSet?.Invoke(points));

                    // 체크 해제 및 초기화
                    ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaChecked = false;
                    ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Clear();
                }
            }
            // 2. 비행금지구역 그리기 중이라면?
            else if (ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaChecked)
            {
                if (_polygonState == PolygonState.Drawing && _polyPoints.Count >= 3)
                {
                    // [수정] 동일하게 람다식 적용
                    FinishPolygonDrawing((points) => CommonEvent.OnProhibitedAreaPolygonSet?.Invoke(points));

                    ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaChecked = false;
                    ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Clear();
                }
            }
            // ★ [추가] 3. 초기임무 폴리곤 (단축키 E)
            else if (ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.PolygonTypeChecked)
            {
                if (_polygonState == PolygonState.Drawing && _polyPoints.Count >= 3)
                {
                    // 우클릭 로직과 동일하게 공통 헬퍼 사용
                    FinishPolygonDrawing((points) => CommonEvent.OnINITMissionPolygonSet?.Invoke(points));

                    // 초기화 (토글 끄기, 임시 선 지우기)
                    ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.PolygonTypeChecked = false;
                    ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonLineList.Clear();
                }
            }

            // (필요 시 Linear 모드 등의 강제 종료 로직도 여기에 추가)
        }

        // [헬퍼] 폴리곤 완성 공통 로직 (중복 코드 제거용)
        private void FinishPolygonDrawing(Action<List<GeoPoint>> onSetEvent)
        {
            // 실제 MapPolygon 만들기 (화면 표시용)
            var mapPoly = new CustomMapPolygon();
            foreach (var gp in _polyPoints)
                mapPoly.Points.Add(gp);

            // 스타일 설정
            mapPoly.Fill = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
            mapPoly.Stroke = Brushes.Red;

            // 임시 리스트에 추가
            ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonList.Add(mapPoly);

            // ★ 핵심: 좌표 세팅 이벤트 발생 (ViewModel로 데이터 전달)
            onSetEvent?.Invoke(new List<GeoPoint>(_polyPoints));

            // 상태 리셋
            _polygonState = PolygonState.None;
            _polyPoints.Clear();
            _currentLine = null;
        }

      
    }

}

