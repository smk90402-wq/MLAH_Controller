
using DevExpress.Map;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Map;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;



namespace MLAH_Controller
{
    public partial class ViewModel_Unit_Map : CommonBase
    {
        private static readonly Lazy<ViewModel_Unit_Map> _lazy =
        new Lazy<ViewModel_Unit_Map>(() => new ViewModel_Unit_Map());

        public static ViewModel_Unit_Map SingletonInstance => _lazy.Value;

        private Guid _instanceId = Guid.NewGuid(); // 인스턴스 식별용

        #region 생성자 & 콜백
        public ViewModel_Unit_Map()
        {
            // ✅ 디자이너에서 이벤트 구독을 막는 보호 코드를 추가합니다.
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            CommonEvent.OnPathPlanSave += Callback_OnPathPlanSave;
            CommonEvent.OnDevelopPathPlanAdd += Callback_OnDevelopPathPlanAdd;


            try { System.IO.File.AppendAllText("debug_mission.txt", $"[DEBUG] ViewModel_Unit_Map Constructor Called. InstanceID={_instanceId}\n"); } catch { }

            //협업기저임무
            CommonEvent.OnINITMissionPointAdd += Callback_OnINITMissionPointAdd;

            CommonEvent.OnINITMissionPolyLineAdd += Callback_OnINITMissionPolyLineAdd;
            CommonEvent.OnINITMissionLinePolygonAdd += Callback_OnINITMissionLinePolygonAdd;
            CommonEvent.OnINITMissionLineLabelAdd += Callback_OnINITMissionLineLabelAdd;

            CommonEvent.OnINITMissionPolygonAdd += Callback_OnINITMissionPolygonAdd;


            //비행참조정보
            CommonEvent.OnTakeOverPointAdd += Callback_OnTakeOverPointAdd;
            CommonEvent.OnTakeOverPointRemove += Callback_OnTakeOverPointRemove;

            CommonEvent.OnHandOverPointAdd += Callback_OnHandOverPointAdd;
            CommonEvent.OnHandOverPointRemove += Callback_OnHandOverPointRemove;

            CommonEvent.OnRTBPointAdd += Callback_OnRTBPointAdd;
            CommonEvent.OnRTBPointRemove += Callback_OnRTBPointRemove;



            CommonEvent.OnFlightAreaPolygonAdd += Callback_OnFlightAreaPolygonAdd;
            CommonEvent.OnFlightAreaPolygonRemove += Callback_OnFlightAreaPolygonRemove;

            CommonEvent.OnProhibitedAreaPolygonAdd += Callback_OnProhibitedAreaPolygonAdd;
            CommonEvent.OnProhibitedAreaPolygonRemove += Callback_OnProhibitedAreaPolygonRemove;

            CommonEvent.OnINITMissionSave += Callback_OnINITMissionSave;


            CommonEvent.OnINITMissionDisplayChanged += Callback_OnINITMissionDisplayChanged;

            //InitializeFixedPolygons();

            FocusSquareItems = new ObservableCollection<MapPolygon>();
            FourCornerItems = new ObservableCollection<MapPolygon>();

            // 1. [배경선 스타일] 묵직하게 깔리는 고정된 선
            _baseStrokeStyle = new StrokeStyle
            {
                Thickness = 4, // 두께감 있게
                               // DashArray를 설정하지 않으면 실선(Solid)이 됩니다.
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };

            // 2. [펄스선 스타일] 슝~ 지나가는 빛줄기
            // 핵심: 선은 짧고(8), 공백은 아주 길게(100) 주어 '드문드문' 지나가게 함
            _pulseStrokeStyle = new StrokeStyle
            {
                Thickness = 4, // 배경선과 같은 두께 or 살짝 얇게
                DashArray = new DoubleCollection { 8, 100 }, // {길이, 공백} 비율 조절로 속도감 연출
                DashOffset = 0,
                StartLineCap = PenLineCap.Round, // 펄스 앞뒤를 둥글게
                EndLineCap = PenLineCap.Round
            };

            // 3. 타이머 설정 (매우 부드럽고 빠른 속도)
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(40); // 50fps급 부드러움
            _animationTimer.Tick += (s, e) =>
            {
                // 값을 계속 감소시켜 '정방향'으로 흐르게 함
                // 감소폭을 키울수록(예: -3.0) 슝슝 빠르게 지나갑니다.
                //_currentDashOffset -= 3.0;
                _currentDashOffset -= 6.0;

                if (_currentDashOffset < -10000) _currentDashOffset = 0;

                // ★ 펄스선만 오프셋 변경 (배경선은 가만히 있음)
                _pulseStrokeStyle.DashOffset = _currentDashOffset;
            };
            _animationTimer.Start();
        }

        #endregion 생성자 & 콜백



        /// <summary>
        /// 선택된 객체 및 특정 UAV들의 시각적 효과를 업데이트하는 루프
        /// </summary>
        public void StartSelectedObjectVisualsLoop()
        {
            _visualsUpdateCts?.Cancel();
            _visualsUpdateCts = new CancellationTokenSource();
            var token = _visualsUpdateCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        //await Task.Delay(41, token); // 약 24 FPS
                        await Task.Delay(66, token); // 약 15 FPS

                        // UI 스레드 접근
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            // 시나리오 데이터가 없으면 스킵
                            if (ViewModel_ScenarioView.SingletonInstance == null ||
                                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario == null)
                                return;

                            var selected = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject;

                            // 1. 포커스 사각형 (선택된 객체용) - 기존 로직 유지
                            if (selected != null && selected.ID != 0)
                            {
                                UpdateFocusSquare(selected);
                            }
                            else
                            {
                                if (FocusSquareItems.Count > 0)
                                {
                                    FocusSquareItems.Clear();
                                    _focusPolygon = null;
                                }
                            }

                            // 2. UAV Footprint 다중 업데이트 로직
                            UpdateMultiUavFootprints(selected);

                        });
                    }
                    catch (TaskCanceledException) { break; }
                    catch (Exception) { /* 로그 처리 */ }
                }
            }, token);
        }

        /// <summary>
        /// [신규] 여러 UAV의 Footprint를 동시에 처리하는 메인 로직
        /// </summary>
        private void UpdateMultiUavFootprints(UnitObjectInfo selected)
        {
            // A. 이번 프레임에서 그려야 할 대상 ID 수집 (Set으로 중복 제거)
            // 기본적으로 4, 5, 6번은 항상 포함
            var targetIds = new HashSet<int>(_alwaysShowIds);

            // 현재 선택된 객체가 UAV(Type==1)라면 그 ID도 포함
            if (selected != null && selected.ID != 0 && selected.Type == 1)
            {
                targetIds.Add(selected.ID);
            }

            // B. 더 이상 그릴 필요가 없는 객체 제거 (딕셔너리 키와 비교)
            // (기존에 그렸는데 지금은 targetIds에 없는 녀석들을 삭제)
            var idsToRemove = _activeUavVisuals.Keys.Where(id => !targetIds.Contains(id)).ToList();
            foreach (var id in idsToRemove)
            {
                RemoveUavVisual(id);
            }

            // C. 대상 ID들에 대해 생성 또는 업데이트 수행
            var unitList = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList;

            foreach (var id in targetIds)
            {
                // UnitObjectList에서 해당 ID를 가진 실제 데이터 찾기
                var unitInfo = unitList.FirstOrDefault(u => u.ID == id);

                // 데이터가 있고, UAV이며, 좌표가 유효한 경우에만 그림
                if (unitInfo != null && unitInfo.Type == 1 && IsValidFootprintData(unitInfo))
                {
                    UpdateSingleUavVisual(id, unitInfo);
                }
                else
                {
                    // 데이터가 유효하지 않으면(초기값이거나 0인 경우) 화면에서 제거
                    RemoveUavVisual(id);
                }
            }
        }

        /// <summary>
        /// 특정 ID의 시각적 요소(Polygon, Lines)를 생성하거나 업데이트
        /// </summary>
        private void UpdateSingleUavVisual(int id, UnitObjectInfo unit)
        {
            // 1. 좌표 데이터 준비
            var topPoint = new GeoPoint(unit.LOC.Latitude, unit.LOC.Longitude);
            var baseCorners = new CoordPointCollection
            {
                new GeoPoint(unit.FootPrintLeftBottomLat, unit.FootPrintLeftBottomLon),
                new GeoPoint(unit.FootPrintRightBottomLat, unit.FootPrintRightBottomLon),
                new GeoPoint(unit.FootPrintRightTopLat, unit.FootPrintRightTopLon),
                new GeoPoint(unit.FootPrintLeftTopLat, unit.FootPrintLeftTopLon)
            };

            // 2. 딕셔너리에 없으면 새로 생성 (최초 1회)
            if (!_activeUavVisuals.TryGetValue(id, out var visualSet))
            {
                visualSet = new UavVisualSet();

                // 2-1. 바닥면 폴리곤 생성
                visualSet.FootprintPoly = new MapPolygon
                {
                    Points = baseCorners,
                    Fill = new SolidColorBrush(Colors.RosyBrown) { Opacity = 0.2 },
                    Stroke = Brushes.RosyBrown,
                    StrokeStyle = new StrokeStyle { Thickness = 2 }
                };
                FourCornerItems.Add(visualSet.FootprintPoly); // ViewModel의 컬렉션에 추가

                // 2-2. 옆면 라인 4개 생성
                for (int i = 0; i < 4; i++)
                {
                    var line = new MapLine
                    {
                        Point1 = topPoint,
                        Point2 = baseCorners[i],
                        Stroke = Brushes.RosyBrown,
                        StrokeStyle = new StrokeStyle { Thickness = 1.5 }
                    };
                    visualSet.SideLines.Add(line);
                    FootprintSideLines.Add(line); // ViewModel의 컬렉션에 추가
                }

                // 딕셔너리에 등록
                _activeUavVisuals.Add(id, visualSet);
            }
            else
            {
                // 3. 이미 존재하면 좌표만 업데이트 (가장 빈번하게 호출됨)
                visualSet.FootprintPoly.Points = baseCorners;

                for (int i = 0; i < 4; i++)
                {
                    if (i < visualSet.SideLines.Count)
                    {
                        visualSet.SideLines[i].Point1 = topPoint;
                        visualSet.SideLines[i].Point2 = baseCorners[i];
                    }
                }
            }
        }

        /// <summary>
        /// 특정 ID의 시각적 요소를 화면과 딕셔너리에서 제거
        /// </summary>
        private void RemoveUavVisual(int id)
        {
            if (_activeUavVisuals.TryGetValue(id, out var visualSet))
            {
                // ViewModel의 ObservableCollection에서 제거 -> UI 자동 갱신
                if (visualSet.FootprintPoly != null) FourCornerItems.Remove(visualSet.FootprintPoly);

                if (visualSet.SideLines != null)
                {
                    foreach (var line in visualSet.SideLines)
                    {
                        FootprintSideLines.Remove(line);
                    }
                }
                _activeUavVisuals.Remove(id);
            }
        }

        /// <summary>
        /// Footprint 좌표 유효성 검사
        /// </summary>
        private bool IsValidFootprintData(UnitObjectInfo unit)
        {
            return unit.FootPrintLeftTopLat > 0 && unit.FootPrintLeftTopLon > 0 &&
                   unit.FootPrintRightTopLat > 0 && unit.FootPrintRightTopLon > 0 &&
                   unit.FootPrintRightBottomLat > 0 && unit.FootPrintRightBottomLon > 0 &&
                   unit.FootPrintLeftBottomLat > 0 && unit.FootPrintLeftBottomLon > 0;
        }

        public void StopSelectedObjectVisualsLoop()
        {
            _visualsUpdateCts?.Cancel();
            _visualsUpdateCts?.Dispose();
            _visualsUpdateCts = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                FocusSquareItems.Clear();
                FourCornerItems.Clear();
                FootprintSideLines.Clear();

                _focusPolygon = null;
                _focusHeadingLine = null;
                _activeUavVisuals.Clear(); // 딕셔너리 초기화
            });
        }

        /// <summary>
        /// 포커스 사각형을 생성하거나 업데이트합니다.
        /// </summary>
        private void UpdateFocusSquare(UnitObjectInfo selected)
        {
            if (_focusPolygon == null)
            {
                _focusPolygon = new MapPolygon
                {
                    Fill = Brushes.Transparent,
                    StrokeStyle = new StrokeStyle { Thickness = 2 }
                };
                FocusSquareItems.Add(_focusPolygon);
            }

            const double baseZoomLevel = 12.0;
            const double baseHalfMeter = 1000.0;
            double zoomFactor = Math.Pow(2, baseZoomLevel - this.CurrentZoomLevel);
            double halfMeter = baseHalfMeter * zoomFactor;

            double centerLat = selected.LOC.Latitude;
            double centerLon = selected.LOC.Longitude;
            double earthRadius = 6378137.0;
            double dLat = (halfMeter / earthRadius) * (180.0 / Math.PI);
            double dLon = (halfMeter / (earthRadius * Math.Cos(centerLat * Math.PI / 180.0))) * (180.0 / Math.PI);

            var newCorners = new CoordPointCollection
            {
                new GeoPoint(centerLat + dLat, centerLon - dLon),
                new GeoPoint(centerLat + dLat, centerLon + dLon),
                new GeoPoint(centerLat - dLat, centerLon + dLon),
                new GeoPoint(centerLat - dLat, centerLon - dLon)
            };

            var strokeBrush = selected.Identification switch
            {
                1 => Brushes.Blue,
                2 => Brushes.Red,
                _ => Brushes.Purple
            };

            _focusPolygon.Stroke = strokeBrush;
            _focusPolygon.Points = newCorners;

            // ---------------------------------------------------------
            // B. [신규] 헤딩 선 (Heading Line) 그리기
            // ---------------------------------------------------------
            if (_focusHeadingLine == null)
            {
                _focusHeadingLine = new MapPolyline
                {
                    Stroke = strokeBrush, // 사각형과 같은 색
                    StrokeStyle = new StrokeStyle
                    {
                        Thickness = 2,
                        DashArray = new DoubleCollection { 4, 2 } // 점선 스타일 추천
                    },
                    IsGeodesic = true // 지구 곡률 반영 (직선)
                };
                FocusHeadingLineList.Add(_focusHeadingLine);
            }

            // 색상 동기화 (선택된 객체 피아식별에 맞춤)
            _focusHeadingLine.Stroke = strokeBrush;

            // 시작점 (유닛 위치)
            GeoPoint startPoint = new GeoPoint(centerLat, centerLon);

            // 끝점 계산 (Heading 방향으로 일정 거리 앞)
            // 화면상에서 적절한 길이로 보이기 위해 ZoomFactor를 활용하거나 고정 미터 사용
            // 여기서는 '사각형 크기'와 비례하게 2배 길이로 설정해 봅니다.
            double lineLengthMeters = halfMeter * 2.0;

            // Heading은 도(Degree) 단위, 0=북, 90=동 (시계방향)
            double headingRad = (selected.velocity.Heading) * (Math.PI / 180.0);

            // 위도/경도 델타 계산 (간이 계산)
            // dLat = distance * cos(heading) / R
            // dLon = distance * sin(heading) / (R * cos(lat))
            double dLatLine = (lineLengthMeters * Math.Cos(headingRad)) / earthRadius * (180.0 / Math.PI);
            double dLonLine = (lineLengthMeters * Math.Sin(headingRad)) / (earthRadius * Math.Cos(centerLat * Math.PI / 180.0)) * (180.0 / Math.PI);

            GeoPoint endPoint = new GeoPoint(centerLat + dLatLine, centerLon + dLonLine);

            // 좌표 업데이트
            _focusHeadingLine.Points.Clear();
            _focusHeadingLine.Points.Add(startPoint);
            _focusHeadingLine.Points.Add(endPoint);
        }

        public void InitializeFixedPolygons()
        {
            //combo

            //DataTemplate titleTemplate = XamlHelper.GetTemplate("<Grid> <TextBlock Text=\"{Binding Path=Text}\" Foreground=\"Blue\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/> </Grid>");

            //        DataTemplate titleTemplate = XamlHelper.GetTemplate(
            //"<Grid>" +
            //"   <TextBlock Text=\"{Binding Path=Text}\" " +
            //"              FontFamily=\"Pretendard Medium\" " +
            //"              FontSize=\"15\" " +
            //"              FontWeight=\"SemiBold\" " +
            //"              Foreground=\"Blue\" " +
            //"              HorizontalAlignment=\"Center\" " +
            //"              VerticalAlignment=\"Center\"/> " +
            //"</Grid>");

            //        DataTemplate titleTemplate = XamlHelper.GetTemplate(
            //"<Grid>" +
            //"   <TextBlock Text=\"{Binding Path=Text}\" " +
            //"              FontFamily=\"Malgun Gothic\" " +
            //"              FontSize=\"14\" " +
            //"              FontWeight=\"Bold\" " +
            //"              Foreground=\"DarkSlateGray\" " +
            //"              HorizontalAlignment=\"Center\" " +
            //"              VerticalAlignment=\"Center\"/> " +
            //"</Grid>");

            DataTemplate titleTemplate = XamlHelper.GetTemplate(
            "<Grid>" +
            "   <TextBlock Text=\"{Binding Path=Text}\" " +
            "              FontFamily=\"Malgun Gothic\" " +
            "              FontSize=\"12\" " +
            "              FontWeight=\"ExtraBold\" " + // 굵기 강화
            "              Foreground=\"DarkSlateGray\" " +
            "              HorizontalAlignment=\"Center\" " +
            "              Panel.ZIndex=\"0\"" +
            "              VerticalAlignment=\"Top\">" +
            //"       <TextBlock.Effect>" +
            //"           <DropShadowEffect BlurRadius=\"2\" ShadowDepth=\"0\" Color=\"White\" Opacity=\"1\"/>" + // [팁] 흰색 테두리(Halo) 효과 추가로 가독성 극대화
            //"       </TextBlock.Effect>" +
            "   </TextBlock>" +
            "</Grid>");

            // 지포리 박스
            var unrealmapPoly = new MapPolygon();

            //좌상
            unrealmapPoly.Points.Add(new GeoPoint(38.146458, 127.294782));

            //우상
            unrealmapPoly.Points.Add(new GeoPoint(38.147111, 127.340401));

            //좌하
            unrealmapPoly.Points.Add(new GeoPoint(38.111084, 127.341217));

            //우하
            unrealmapPoly.Points.Add(new GeoPoint(38.110432, 127.295620));
            unrealmapPoly.Fill = Brushes.Transparent;
            unrealmapPoly.Stroke = new SolidColorBrush(Colors.Teal);
            unrealmapPoly.StrokeStyle = new StrokeStyle { Thickness = 3 };
            unrealmapPoly.TitleOptions = new ShapeTitleOptions();
            unrealmapPoly.TitleOptions.Pattern = "지포리";
            unrealmapPoly.TitleOptions.Template = titleTemplate;
            UnrealLandScapeList.Add(unrealmapPoly);

            // 외부 박스
            var unrealmapPoly1 = new MapPolygon();
            unrealmapPoly1.Points.Add(new GeoPoint(38.270646, 127.130588));
            unrealmapPoly1.Points.Add(new GeoPoint(38.278147, 127.682911));
            unrealmapPoly1.Points.Add(new GeoPoint(37.843969, 127.687338));
            unrealmapPoly1.Points.Add(new GeoPoint(37.836468, 127.135015));
            unrealmapPoly1.Fill = Brushes.Transparent;
            unrealmapPoly1.Stroke = new SolidColorBrush(Colors.Teal);
            unrealmapPoly1.StrokeStyle = new StrokeStyle { Thickness = 3 };
            unrealmapPoly1.TitleOptions = new ShapeTitleOptions();
            unrealmapPoly1.TitleOptions.Template = titleTemplate;
            unrealmapPoly1.TitleOptions.Pattern = "작전 구역";
            UnrealLandScapeList.Add(unrealmapPoly1);

            // 화성 홍익
            var unrealmapPoly2 = new MapPolygon();
            unrealmapPoly2.Points.Add(new GeoPoint(37.226816, 126.970298));
            unrealmapPoly2.Points.Add(new GeoPoint(37.227199, 126.992805));

            unrealmapPoly2.Points.Add(new GeoPoint(37.209201, 126.993282));

            unrealmapPoly2.Points.Add(new GeoPoint(37.208818, 126.970780));

            unrealmapPoly2.Fill = Brushes.Transparent;
            unrealmapPoly2.Stroke = new SolidColorBrush(Colors.Teal);
            unrealmapPoly2.StrokeStyle = new StrokeStyle { Thickness = 3 };
            unrealmapPoly2.TitleOptions = new ShapeTitleOptions();
            unrealmapPoly2.TitleOptions.Pattern = "화성 홍익";
            unrealmapPoly2.TitleOptions.Template = titleTemplate;
            UnrealLandScapeList.Add(unrealmapPoly2);


            // 인제 
            var unrealmapPoly3 = new MapPolygon();
            unrealmapPoly3.Points.Add(new GeoPoint(37.951046, 128.135643));
            unrealmapPoly3.Points.Add(new GeoPoint(37.951046, 128.228074));

            unrealmapPoly3.Points.Add(new GeoPoint(37.878051, 128.228074));

            unrealmapPoly3.Points.Add(new GeoPoint(37.878051, 128.135643));
            unrealmapPoly3.Fill = Brushes.Transparent;
            unrealmapPoly3.Stroke = new SolidColorBrush(Colors.Teal);
            unrealmapPoly3.StrokeStyle = new StrokeStyle { Thickness = 3 };
            unrealmapPoly3.TitleOptions = new ShapeTitleOptions();
            unrealmapPoly3.TitleOptions.Pattern = "인제";
            unrealmapPoly3.TitleOptions.Template = titleTemplate;
            UnrealLandScapeList.Add(unrealmapPoly3);
        }

        //public class CustomMapPoint : GeoPoint
        //{
        //    public int ShapeId { get; set; }
        //}

        public void MapClear()
        {
            ObjectDisplayList.Clear();
            FocusSquareList.Clear();

            INITMissionPointList.Clear();
            INITMissionLineList.Clear();
            INITMissionPolygonList.Clear();

            UnitDevelopPathPlanList.Clear();

            RTBPointList.Clear();
            HandOverPointList.Clear();
            TakeOverPointList.Clear();

            FlightAreaPolygonList.Clear();
            ProhibitedAreaPolygonList.Clear();

            LAHWapointList.Clear();
            UAVWapointList.Clear();

            //FootPrint롤백
            _activeUavVisuals.Clear();
            FootprintSideLines.Clear();
            FourCornerItems.Clear();

            INITMissionLinePolygonList.Clear();
            INITMissionLineLabelList.Clear();

            FocusHeadingLineList.Clear();
            _focusHeadingLine = null;
        }

        public void InitScenarioMapClear()
        {
            //ObjectDisplayList.Clear();
            //FocusSquareList.Clear();

            INITMissionPointList.Clear();
            INITMissionLineList.Clear();
            INITMissionPolygonList.Clear();

            INITMissionLineLabelList.Clear();

            //UnitDevelopPathPlanList.Clear();

            //RTBPointList.Clear();
            //HandOverPointList.Clear();
            //TakeOverPointList.Clear();

            //FlightAreaPolygonList.Clear();
            //ProhibitedAreaPolygonList.Clear();

            //LAHWapointList.Clear();
            //UAVWapointList.Clear();

        }



        // 아이콘 사이즈 (픽셀)
        private double _iconSize = 60; // 초기 임의값
        public double IconSize
        {
            get => _iconSize;
            set
            {
                if (_iconSize != value)
                {
                    _iconSize = value;
                    //View_Unit_Map.SingletonInstance.UpdateFocusSquare();
                    OnPropertyChanged(nameof(IconSize));
                }
            }
        }

        private void Callback_OnPathPlanSave()
        {
            TempListsClear();
        }

        private void Callback_OnDevelopPathPlanAdd(int PathID, List<CoordPoint> PathPointList)
        {
            //UnitDevelopPathPlanList.Add(new MapPolyline()
            //{
            //    //ShapeId = PathID,
            //    Points = PathPointList,
            //    Stroke = Brushes.Red,
            //    //StrokeThickness = 2
            //});
            //TempUnitDevelopPathPlanList.Clear();
        }

        private void Callback_OnINITMissionSave()
        {
            TempListsClear();
        }


        private void Callback_OnINITMissionDisplayChanged(bool isDisplay)
        {
            var selectedMission = ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.SelectedinputMissionItem;
            if (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.InputMissionPackage == null || selectedMission == null)
            {
                return;
            }

            var mapVM = ViewModel_Unit_Map.SingletonInstance;

            if (isDisplay) // IsShow == true일 때 (지도에 표시)
            {
                bool isExist = false;
                switch (selectedMission.ShapeType)
                {
                    case 1: // 점
                        isExist = mapVM.INITMissionPointList.Any(p => p.MissionID == selectedMission.InputMissionID);
                        break;
                    case 2: // 선
                        isExist = mapVM.INITMissionLineList.Any(l => l.MissionId == selectedMission.InputMissionID);
                        break;
                    case 3: // 면
                        isExist = mapVM.INITMissionPolygonList.Any(p => p.MissionID == selectedMission.InputMissionID);
                        break;
                }

                if (!isExist)
                {
                    // 지도에 없는 임무이므로, 그리기 함수를 호출해서 새로 그린다.
                    ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.InitMissionSet(selectedMission);
                }
            }
            else // IsShow == false일 때 (지도에서 제거)
            {
                switch (selectedMission.ShapeType)
                {
                    case 1: // 점 제거
                        var pointToRemove = mapVM.INITMissionPointList.FirstOrDefault(p => p.MissionID == selectedMission.InputMissionID);
                        if (pointToRemove != null) mapVM.INITMissionPointList.Remove(pointToRemove);
                        break;

                    case 2: // 선 및 관련 다각형 모두 안전하게 제거
                        var lineToRemove = mapVM.INITMissionLineList.FirstOrDefault(l => l.MissionId == selectedMission.InputMissionID);
                        if (lineToRemove != null) mapVM.INITMissionLineList.Remove(lineToRemove);

                        var linePolygonsToRemove = mapVM.INITMissionLinePolygonList.Where(p => p.MissionID == selectedMission.InputMissionID).ToList();
                        foreach (var p in linePolygonsToRemove) mapVM.INITMissionLinePolygonList.Remove(p);


                        //라벨 제거
                        var labelToRemove = mapVM.INITMissionLineLabelList.FirstOrDefault(p => p.MissionID == (int)selectedMission.InputMissionID);
                        if (labelToRemove != null) mapVM.INITMissionLineLabelList.Remove(labelToRemove);

                        break;

                    case 3: // 면 다각형 안전하게 제거
                        var areaPolygonsToRemove = mapVM.INITMissionPolygonList.Where(p => p.MissionID == selectedMission.InputMissionID).ToList();
                        foreach (var p in areaPolygonsToRemove) mapVM.INITMissionPolygonList.Remove(p);
                        break;
                }
            }
        }


        public UnitMapObjectInfo ConvertToObjectInfo(UnitObjectInfo input)
        {
            // 1) 새 ObjectInfo 생성
            var info = new UnitMapObjectInfo();

            // 2) 초기 설정 (한번만 복사)
            info.ID = (uint)input.ID;
            info.Location = new GeoPoint(input.LOC.Latitude, input.LOC.Longitude);
            info.Heading = input.velocity.Heading;
            info.Type = input.Type;
            info.Status = input.Status;

            IValueConverter converter = (IValueConverter)Application.Current.Resources["ObjectTypeToStringConverter"];
            info.TypeString = converter.Convert(input.Type, typeof(string), null, CultureInfo.CurrentCulture) as string;

            if (input.Type == 1)
            {
                info.PlatformString = "UAV";
            }
            else if (input.Type == 3)
            {
                info.PlatformString = "LAH";
            }
            else
            {
                IMultiValueConverter multiConverter = (IMultiValueConverter)Application.Current.Resources["ObjectPlatformMultiValueConverter"];
                // 컨버터에 전달할 값 배열: 
                // values[0] : 플랫폼 타입, values[1] : 객체 유형 (컨버터의 주석 참고)
                object[] values = new object[] { input.PlatformType, input.Type };

                // 멀티 컨버터의 Convert 메서드 호출
                string convertedString = multiConverter.Convert(values, typeof(string), null, CultureInfo.CurrentCulture) as string;

                // 결과를 원하는 변수에 할당
                info.PlatformString = convertedString;
            }

            if (input.Type == 3) // 유인기(LAH)인 경우
            {
                if (input.Status == 2) // 비정상 상태가 최우선
                {
                    info.imagesource = (ImageSource)Application.Current.Resources["LAH_RED"];
                }
                else if (input.ID == 1) // 정상 상태의 지휘기
                {
                    info.imagesource = (ImageSource)Application.Current.Resources["LAH_BLUE"];
                }
                else // 정상 상태의 편대기
                {
                    info.imagesource = (ImageSource)Application.Current.Resources["LAH"];
                }
            }
            else // 유인기가 아닌 다른 모든 객체
            {
                info.imagesource = Imagesource_Allocater(input.Type);
            }

            return info;
        }

        public ImageSource Imagesource_Allocater(int type)
        {
            //var temp_imagesource = (ImageSource)Application.Current.Resources["Tank"];
            var temp_imagesource = (ImageSource)Application.Current.Resources["Neutral"];
            switch (type)
            {
                case 1:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["UAV_TopView"];
                    }
                    break;
                case 2:
                    {
                        //temp_imagesource = (ImageSource)Application.Current.Resources["UAV"];
                    }
                    break;
                case 3:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["LAH"];
                    }
                    break;
                case 4:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["Hostile_Panzer"];
                    }
                    break;
                case 5:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["Hostile_Tank"];
                    }
                    break;
                case 8:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["Hostile_AAG"];
                    }
                    break;
                case 9:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["Hostile_Soldier"];
                    }
                    break;
                default:
                    {

                    }
                    break;

            }
            return temp_imagesource;
        }



        public void ClearLinearPreview()
        {
            TempINITMissionLineList.Clear();
            TempINITMissionPolygonList.Clear();
            View_Unit_Map.SingletonInstance._linePoints.Clear();
            View_Unit_Map.SingletonInstance._previewSegments.Clear();
            View_Unit_Map.SingletonInstance._previewRects.Clear();
            View_Unit_Map.SingletonInstance._ghostSegment = null;
        }

        public void ClearTempDrawing()
        {
            // 1. 선형(Linear) 관련 초기화
            TempINITMissionLineList.Clear();
            View_Unit_Map.SingletonInstance._linePoints.Clear(); // 선 점들
            View_Unit_Map.SingletonInstance._previewSegments.Clear();
            View_Unit_Map.SingletonInstance._previewRects.Clear();
            View_Unit_Map.SingletonInstance._ghostSegment = null;

            // 2. ★ 다각형(Polygon) 관련 초기화 (이게 빠져서 안 지워졌던 것!)
            TempINITMissionPolygonLineList.Clear(); // 다각형 외곽선(그리는 중인 선)
            //PreINITMissionPolygonList.Clear();      // [Fix] 생성 모드 잔여물 제거 (중복/겹침 방지)

            //꼬이는 부분 있나 체크
            TempINITMissionPolygonList.Clear();     // 완료된 임시 다각형
            View_Unit_Map.SingletonInstance._polyPoints.Clear(); // 다각형 점들

            // 3. 상태(State) 초기화 (중요)
            View_Unit_Map.SingletonInstance._polygonState = PolygonState.None;
            View_Unit_Map.SingletonInstance._currentLine = null;
        }
        public void TempListsClear()
        {
            //TempINITMissionPolygonList.Clear();
            //TempCompINITMissionLineList.Clear();
            //TempCompINITMissionPointList.Clear();
            //TempCompPathPlanList.Clear();
        }

        public void TempPolygonListsClear()
        {
            TempINITMissionPolygonList.Clear();
            //TempCompINITMissionLineList.Clear();
            //TempCompINITMissionPointList.Clear();
            //TempCompPathPlanList.Clear();
        }

        public void TempPathPlanClear()
        {
            //TempCompINITMissionPolygonList.Clear();
            //TempCompINITMissionLineList.Clear();
            //TempCompINITMissionPointList.Clear();
            //TempCompPathPlanList.Clear();
            TempUnitDevelopPathPlanList.Clear();
        }

        public void LineListsClear()
        {
            //TempCompINITMissionPolygonList.Clear();
            //TempCompINITMissionLineList.Clear();
            //TempCompINITMissionPointList.Clear();
            //TempCompPathPlanList.Clear();
        }

        public void ClearTempTakeoverPoint()
        {
            //TempCompINITMissionPolygonList.Clear();
            //TempCompINITMissionLineList.Clear();
            TempINITMissionPointList.Clear();
            //TempCompPathPlanList.Clear();
        }

        public void ClearTempHandoverPoint()
        {
            TempINITMissionPointList.Clear();

        }

        public void ClearLAHWayPoint()
        {
            //TempCompINITMissionPolygonList.Clear();
            //TempCompINITMissionLineList.Clear();
            LAHWapointList.Clear();
            //TempCompPathPlanList.Clear();
        }

        public void ClearUAVWayPoint()
        {
            //TempCompINITMissionPolygonList.Clear();
            //TempCompINITMissionLineList.Clear();
            UAVWapointList.Clear();
            //TempCompPathPlanList.Clear();
        }


        public void PolygonListsClear()
        {

            //TempCompINITMissionLineList.Clear();
            //TempCompINITMissionPointList.Clear();
            //TempCompPathPlanList.Clear();
            TempINITMissionPolygonList.Clear();
        }


        public RelayCommand MouseDownCommand { get; set; }
        public void MouseDownCommandAction(object param)
        {



        }

        public RelayCommand MouseMoveCommand { get; set; }
        public void MouseMoveCommandAction(object param)
        {


        }


        private void Callback_OnINITMissionPointAdd(CustomMapPoint InputMapPoint)
        {
            TempINITMissionPointList.Clear();
            //var item = new CustomMapPoint();
            //item.MissionID = OverlayID;
            //item.Latitude = Lat;
            //item.Longitude = Lon;
            //item.TagString = item.MissionID.ToString();
            INITMissionPointList.Add(InputMapPoint);
        }

        public void Callback_OnTakeOverPointAdd(int OverlayID, float Lat, float Lon)
        {
            TempINITMissionPointList.Clear();
            var item = new CustomMapPoint();
            item.MissionID = OverlayID;
            item.Latitude = Lat;
            item.Longitude = Lon;
            item.TagString = $"UAV - {item.MissionID}\n무인기 통제권 획득";
            TakeOverPointList.Add(item);
        }

        public void Callback_OnTakeOverPointRemove(int OverlayID)
        {
            //TempINITMissionPointList.Clear();
            //var item = new CustomMapPoint();
            //item.MissionID = OverlayID;
            //item.Latitude = Lat;
            //item.Longitude = Lon;
            for (int i = TakeOverPointList.Count - 1; i >= 0; i--)
            {
                // 현재 인덱스(i)에 있는 아이템을 가져옵니다.
                var item = TakeOverPointList[i];

                // 아이템의 MissionID가 목표 OverlayID와 일치하는지 확인합니다.
                if (item.MissionID == OverlayID)
                {
                    // 조건이 맞으면 현재 인덱스의 아이템을 제거합니다.
                    TakeOverPointList.RemoveAt(i);
                }
            }
        }

        public void Callback_OnHandOverPointAdd(int OverlayID, float Lat, float Lon)
        {
            TempINITMissionPointList.Clear();
            var item = new CustomMapPoint();
            item.MissionID = OverlayID;
            item.Latitude = Lat;
            item.Longitude = Lon;
            item.TagString = $"UAV - {item.MissionID}\n무인기 통제권 인계";
            HandOverPointList.Add(item);
        }

        public void Callback_OnHandOverPointRemove(int OverlayID)
        {
            //TempINITMissionPointList.Clear();
            //var item = new CustomMapPoint();
            //item.MissionID = OverlayID;
            //item.Latitude = Lat;
            //item.Longitude = Lon;
            for (int i = HandOverPointList.Count - 1; i >= 0; i--)
            {
                // 현재 인덱스(i)에 있는 아이템을 가져옵니다.
                var item = HandOverPointList[i];

                // 아이템의 MissionID가 목표 OverlayID와 일치하는지 확인합니다.
                if (item.MissionID == OverlayID)
                {
                    // 조건이 맞으면 현재 인덱스의 아이템을 제거합니다.
                    HandOverPointList.RemoveAt(i);
                }
            }
        }



        public void Callback_OnRTBPointAdd(int OverlayID, float Lat, float Lon)
        {
            TempINITMissionPointList.Clear();
            var item = new CustomMapPoint();
            item.MissionID = OverlayID;
            item.Latitude = Lat;
            item.Longitude = Lon;
            RTBPointList.Add(item);
        }

        public void Callback_OnRTBPointRemove(int OverlayID)
        {
            //TempINITMissionPointList.Clear();
            //var item = new CustomMapPoint();
            //item.MissionID = OverlayID;
            //item.Latitude = Lat;
            //item.Longitude = Lon;
            for (int i = RTBPointList.Count - 1; i >= 0; i--)
            {
                // 현재 인덱스(i)에 있는 아이템을 가져옵니다.
                var item = RTBPointList[i];

                // 아이템의 MissionID가 목표 OverlayID와 일치하는지 확인합니다.
                if (item.MissionID == OverlayID)
                {
                    // 조건이 맞으면 현재 인덱스의 아이템을 제거합니다.
                    RTBPointList.RemoveAt(i);
                }
            }
        }

        private void Callback_OnINITMissionPolygonAdd(List<CustomMapPolygon> PolygonList)
        {
            // 1. 유효성 검사
            if (PolygonList == null || PolygonList.Count == 0) return;

            // 2. 수정/추가하려는 대상 임무 ID 확인
            int targetMissionID = PolygonList[0].MissionID;
            try { System.IO.File.AppendAllText("debug_mission.txt", $"[DEBUG] Callback_OnINITMissionPolygonAdd Called. Instance={_instanceId}, Count={PolygonList.Count}, TargetID={targetMissionID}\n"); } catch { }


            // 3. [순서 보장 로직] 삽입할 위치(Index) 찾기
            // 기존 리스트에서 해당 임무 ID를 가진 첫 번째 객체의 인덱스를 찾습니다.
            // 만약 -1이라면(없다면) 신규 생성이므로 맨 뒤에 추가(Add)하면 됩니다.
            // CustomMapPolygon으로 형변환해서 비교
            var firstExistingItem = INITMissionPolygonList
                                    .FirstOrDefault(p => p.MissionID == targetMissionID);

            int insertIndex = -1;
            if (firstExistingItem != null)
            {
                insertIndex = INITMissionPolygonList.IndexOf(firstExistingItem);
            }

            // 4. 기존 데이터 삭제 (중복 방지)
            // 역순으로 돌면서 해당 임무의 기존 폴리곤을 모두 지웁니다.
            for (int i = INITMissionPolygonList.Count - 1; i >= 0; i--)
            {
                if (INITMissionPolygonList[i].MissionID == targetMissionID)
                {
                    try { System.IO.File.AppendAllText("debug_mission.txt", $"[DEBUG] Callback: Removed existing polygon ID={INITMissionPolygonList[i].MissionID}\n"); } catch { }
                    INITMissionPolygonList.RemoveAt(i);
                }
            }

            // 5. 새 데이터 추가 (Insert or Add)
            foreach (var polygon in PolygonList)
            {
                // ----------------------------------------------------------------
                // [스타일 및 속성 설정 구간]
                // ViewModel_UC_Unit_INITMissionInfo.InitMissionSet 에서 이미 
                // 스타일(Fill, Stroke, Template)을 다 설정해서 보내주므로,
                // 여기서는 굳이 덮어쓰지 않아도 됩니다. 
                // 하지만 맵에서 강제로 스타일을 지정해야 한다면 여기서 풀어서 씁니다.
                // ----------------------------------------------------------------

                // (예시: 만약 맵 뷰모델에서 스타일을 강제해야 한다면 아래 주석 해제)
                /*
                // 채움색(투명도 있는 MediumPurple)
                var missionColor = Colors.MediumPurple; 
                missionColor.A = 64; 
                polygon.Fill = new SolidColorBrush(missionColor);

                // 테두리 (진한 보라색)
                polygon.Stroke = new SolidColorBrush(Color.FromRgb(75, 0, 130)); 
                polygon.StrokeStyle = new StrokeStyle { Thickness = 1 };
                */

                // ----------------------------------------------------------------
                // [리스트 삽입 로직]
                // ----------------------------------------------------------------

                if (insertIndex != -1)
                {
                    // 기존 위치가 있었다면, 그 자리에 끼워 넣습니다.
                    // insertIndex는 계속 증가시켜야 순서대로 들어갑니다.
                    // (주의: 삭제 후 리스트 크기가 줄었어도, 기존 시작 위치는 유효하거나 
                    //  맨 뒤에 붙여야 할 수 있으므로 범위 체크)
                    if (insertIndex <= INITMissionPolygonList.Count)
                    {
                        INITMissionPolygonList.Insert(insertIndex, polygon);
                        insertIndex++; // 다음 아이템은 그 뒤에 넣어야 하므로 증가
                    }
                    else
                    {
                        // 혹시 모를 인덱스 범위 초과 시 안전하게 Add
                        INITMissionPolygonList.Add(polygon);
                    }
                }
                else
                {
                    // 기존에 없던 임무라면 맨 뒤에 추가
                    INITMissionPolygonList.Add(polygon);
                }
            }

            // 6. 정리 (임시 객체 및 그리기 상태 초기화)
            TempINITMissionLineList.Clear();      // 임시 선분(중심선) 삭제
            TempINITMissionPolygonList.Clear();   // 임시 사각형(폭 미리보기) 삭제

            var mapView = View_Unit_Map.SingletonInstance;
            if (mapView != null)
            {
                mapView._linePoints.Clear();
                mapView._previewSegments.Clear();
                mapView._previewRects.Clear();
                mapView._state = View_Unit_Map.DrawState.None;
                mapView._ghostSegment = null;
            }
        }

        private void Callback_OnINITMissionPolyLineAdd(CustomMapLine InputMapLine)
        {
            //// 전달받은 선 객체를 영구 보관할 '선' 리스트에 추가한다.
            //INITMissionLineList.Add(InputMapLine);

            //// !!! 중요 !!!
            //// 여기서는 임시 객체를 절대 지우지 않는다.
            //// 바로 뒤이어 다각형 콜백이 호출될 것이므로, 모든 정리는 그곳에서 한번에 처리한다.
            ///
               // --- 방어 코드 추가 ---
            if (InputMapLine == null)
            {
                // 1. 객체 자체가 null인 경우
                System.Diagnostics.Debug.WriteLine("오류: 추가하려는 지도 선 객체가 null입니다!");
                return;
            }
            if (InputMapLine.Points == null)
            {
                // 2. 객체의 Points 속성이 null인 경우
                System.Diagnostics.Debug.WriteLine($"오류: MissionId {InputMapLine.MissionId} 객체의 Points 속성이 null입니다!");
                return;
            }
            // --------------------

            INITMissionLineList.Add(InputMapLine);
        }

        private void Callback_OnINITMissionLinePolygonAdd(List<CustomMapPolygon> PolygonList)
        {

            // 1. 전달받은 다각형 객체를 영구 보관할 '다각형' 리스트에 추가한다.
            foreach (var polygon in PolygonList)
            {

                INITMissionLinePolygonList.Add(polygon);
            }

            // --- 지금부터는 모든 그리기가 끝난 후의 정리 단계 ---

            // 2. 지도 위에 있던 모든 '임시' 객체들을 깨끗하게 삭제한다.
            TempINITMissionLineList.Clear();      // 임시 선분(중심선) 삭제
            TempINITMissionPolygonList.Clear();   // 임시 사각형(폭 미리보기) 삭제

            // 3. View(지도 컨트롤 자체)의 그리기 관련 상태 변수들을 모두 초기 상태로 되돌린다.
            var mapView = View_Unit_Map.SingletonInstance;
            if (mapView != null)
            {
                mapView._linePoints.Clear();
                mapView._previewSegments.Clear();
                mapView._previewRects.Clear();
                mapView._state = View_Unit_Map.DrawState.None;
                mapView._ghostSegment = null;
            }
        }

        private void Callback_OnINITMissionLineLabelAdd(CustomMapPoint labelPoint)
        {
            if (labelPoint == null) return;

            // 기존 포인트 리스트가 아닌, 라벨 전용 리스트에 추가
            INITMissionLineLabelList.Add(labelPoint);
        }


        public void Callback_OnFlightAreaPolygonAdd(int OverlayID, CustomMapPolygon Polygon)
        {
            //ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonList.Clear();
            //ViewModel_Unit_Map.SingletonInstance.INITMissionPolygonList.Add(PolygonList);

            //foreach (var item in Polygon)
            //{
            var Input = new CustomMapPolygon();
            Input.MissionID = OverlayID;
            //Input.ShapeId = Polygon.ShapeId;
            Input.Points = Polygon.Points;

            // 채움색(빨간색), 투명도 0.5
            //Input.Fill = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
            //Input.Fill = new SolidColorBrush(Color.FromArgb(32, 4, 18, 136)); // 128 = 50% 투명
            //   └ 128 = 50% 투명(0~255),  (255,0,0)은 R,G,B 순서

            // 기존 색상에서 Alpha 값만 변경하는 방식
            var skyBlue = Colors.SkyBlue;
            //128 - 50%
            //256 - 100%
            //64 - 25%
            //192 - 75%
            skyBlue.A = 64;
            Input.Fill = new SolidColorBrush(skyBlue);

            DataTemplate titleTemplate = XamlHelper.GetTemplate(
            "<Grid>" +
            "   <TextBlock Text=\"{Binding Path=Text}\" " +
            "              FontFamily=\"Malgun Gothic\" " +
            "              FontSize=\"12\" " +
            "              FontWeight=\"ExtraBold\" " + // 굵기 강화
            "              Foreground=\"DarkSlateGray\" " +
            "              HorizontalAlignment=\"Center\" " +
            "              VerticalAlignment=\"Center\">" +
            //"       <TextBlock.Effect>" +
            //"           <DropShadowEffect BlurRadius=\"2\" ShadowDepth=\"0\" Color=\"White\" Opacity=\"1\"/>" + // [팁] 흰색 테두리(Halo) 효과 추가로 가독성 극대화
            //"       </TextBlock.Effect>" +
            "   </TextBlock>" +
            "</Grid>");

            Input.TitleOptions = new ShapeTitleOptions();
            Input.TitleOptions.Pattern = "비행가능구역";
            Input.TitleOptions.Template = titleTemplate;

            // 폴리곤 테두리(선) 색상·두께도 설정 가능
            Input.Stroke = Brushes.MidnightBlue;
            //mapPoly.StrokeThickness = 2;

            //INITMissionPolygonList.Add(Input);
            FlightAreaPolygonList.Add(Input);
            //}
            TempINITMissionPolygonList.Clear();
        }

        // [신규] 비행가능구역 삭제 콜백
        private void Callback_OnFlightAreaPolygonRemove(int OverlayID)
        {
            // FlightAreaPolygonList에서 MissionID가 일치하는 폴리곤 검색
            var target = FlightAreaPolygonList.FirstOrDefault(p => p.MissionID == OverlayID);

            if (target != null)
            {
                FlightAreaPolygonList.Remove(target);
            }
        }



        public void Callback_OnProhibitedAreaPolygonAdd(int OverlayID, CustomMapPolygon Polygon)
        {
            var Input = new CustomMapPolygon();
            //Input.ShapeId = Polygon.ShapeId;
            Input.Points = Polygon.Points;

            // 채움색(빨간색), 투명도 0.5
            //Input.Fill = new SolidColorBrush(Color.FromArgb(128, 85, 107, 47)); << darkolivegreen
            //Input.Fill = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
            //Input.Fill = new SolidColorBrush(Color.FromArgb(128, 136, 41, 41));
            //   └ 128 = 50% 투명(0~255),  (255,0,0)은 R,G,B 순서


            var IndianRed = Colors.IndianRed;
            IndianRed.A = 64;
            Input.Fill = new SolidColorBrush(IndianRed);

            DataTemplate titleTemplate = XamlHelper.GetTemplate(
         "<Grid>" +
         "   <TextBlock Text=\"{Binding Path=Text}\" " +
         "              FontFamily=\"Malgun Gothic\" " +
         "              FontSize=\"12\" " +
         "              FontWeight=\"ExtraBold\" " + // 굵기 강화
         "              Foreground=\"DarkSlateGray\" " +
         "              HorizontalAlignment=\"Center\" " +
         "              VerticalAlignment=\"Center\">" +
         //"       <TextBlock.Effect>" +
         //"           <DropShadowEffect BlurRadius=\"2\" ShadowDepth=\"0\" Color=\"White\" Opacity=\"1\"/>" + // [팁] 흰색 테두리(Halo) 효과 추가로 가독성 극대화
         //"       </TextBlock.Effect>" +
         "   </TextBlock>" +
         "</Grid>");

            Input.TitleOptions = new ShapeTitleOptions();
            Input.TitleOptions.Pattern = "비행금지구역";
            Input.TitleOptions.Template = titleTemplate;

            // 폴리곤 테두리(선) 색상·두께도 설정 가능
            Input.Stroke = Brushes.MidnightBlue;
            //mapPoly.StrokeThickness = 2;

            ProhibitedAreaPolygonList.Add(Input);
            //}
            TempINITMissionPolygonList.Clear();
        }

        // [신규] 비행금지구역 삭제 콜백
        private void Callback_OnProhibitedAreaPolygonRemove(int OverlayID)
        {
            // ProhibitedAreaPolygonList에서 MissionID가 일치하는 폴리곤 검색
            var target = ProhibitedAreaPolygonList.FirstOrDefault(p => p.MissionID == OverlayID);

            if (target != null)
            {
                ProhibitedAreaPolygonList.Remove(target);
            }
        }

        //public void UpdateAllLAHWaypoints(ObservableCollection<LAHMissionPlan> allPlans)
        //{
        //    // 0. 기존에 그려진 선과 마커를 모두 삭제합니다.
        //    //LAHWapointList.Clear();
        //    LAHWpMarkerList.Clear();

        //    LAHStaticLineList.Clear();
        //    LAHPulseLineList.Clear();

        //    if (allPlans == null) return;

        //    // 배경: 약간 어둡고 투명한 주황색 (길이 계속 보이게)
        //    //var baseColor = new SolidColorBrush(Color.FromArgb(100, 255, 69, 0)); // Alpha 100
        //    var baseColor = new SolidColorBrush(Color.FromArgb(60, 255, 69, 0));

        //    // 펄스: 아주 밝은 흰색 or 형광 주황 (빛나는 느낌)
        //    //var pulseColor = new SolidColorBrush(Colors.White); // 혹은 밝은 Yellow
        //    var pulseColor = new SolidColorBrush(Color.FromArgb(255, 255, 69, 0));

        //    // 1. 수신된 모든 임무 계획(헬기 별)을 순회합니다.
        //    foreach (var plan in allPlans)
        //    {
        //        if (plan.MissionSegemntList == null) continue;

        //        foreach (var segment in plan.MissionSegemntList)
        //        {
        //            if (segment.IndividualMissionList == null) continue;

        //            // ★★★ [핵심 수정] ★★★ 
        //            // 개별 임무(IndividualMission) 루프 '안'에서 Polyline을 생성해야
        //            // 이전 임무의 마지막 점과 현재 임무의 시작 점이 연결되지 않습니다.
        //            foreach (var individualMission in segment.IndividualMissionList)
        //            {
        //                if (individualMission.WaypointList == null || individualMission.WaypointList.Count == 0) continue;

        //                // 개별 임무별로 별도의 선 객체 생성
        //                //var missionPolyline = new MapPolyline()
        //                //{
        //                //    //Stroke = System.Windows.Media.Brushes.Cyan,
        //                //    //StrokeStyle = new StrokeStyle { Thickness = 3 },

        //                //};

        //                //missionPolyline.EndLineCap = new MapLineCap
        //                //{
        //                //    Length = 12,  // 화살표 길이
        //                //    Width = 8,    // 화살표 너비
        //                //    Visible = true
        //                //};

        //                // 1. [배경선] 생성 (정적)
        //                var basePolyline = new MapPolyline
        //                {
        //                    Stroke = baseColor,
        //                    StrokeStyle = _baseStrokeStyle,
        //                    // 배경선은 캡(화살표) 없이 깔끔하게
        //                    IsGeodesic = false
        //                };

        //                // 2. [펄스선] 생성 (동적 애니메이션)
        //                //var pulsePolyline = new MapPolyline
        //                //{
        //                //    Stroke = pulseColor,
        //                //    StrokeStyle = _pulseStrokeStyle, // 공유된 애니메이션 스타일
        //                //                                     // 펄스선도 캡 없이 (둥근 점선 자체가 효과임)
        //                //    IsGeodesic = false
        //                //};

        //                // 이전 좌표 저장용 (방향 계산용)
        //                GeoPoint prevPoint = null;

        //                // 해당 개별 임무의 경로점들만 이 선에 추가
        //                //foreach (var waypoint in individualMission.WaypointList)
        //                //{
        //                //    if (waypoint.Coordinate == null) continue;

        //                //    var point = new GeoPoint(waypoint.Coordinate.Latitude, waypoint.Coordinate.Longitude);

        //                //    // 1. Polyline에 점 추가
        //                //    missionPolyline.Points.Add(point);

        //                //    // 2. 경로점 마커(MapDot) 추가
        //                //    var marker = new CustomMapPoint
        //                //    {
        //                //        Latitude = point.Latitude,
        //                //        Longitude = point.Longitude,
        //                //        TagString = $"H{plan.AircraftID}\n{waypoint.WaypoinID}"
        //                //    };
        //                //    LAHWpMarkerList.Add(marker);
        //                //}

        //                for (int i = 0; i < individualMission.WaypointList.Count; i++)
        //                {
        //                    var wp = individualMission.WaypointList[i];
        //                    if (wp.Coordinate == null) continue;
        //                    var gp = new GeoPoint(wp.Coordinate.Latitude, wp.Coordinate.Longitude);
        //                    basePolyline.Points.Add(gp);
        //                    //pulsePolyline.Points.Add(gp);

        //                    // ★ 3. Heading(방향) 계산
        //                    double heading = 0;
        //                    if (i < individualMission.WaypointList.Count - 1)
        //                    {
        //                        // 다음 점이 있으면 다음 점을 바라봄
        //                        var nextWp = individualMission.WaypointList[i + 1];
        //                        if (nextWp.Coordinate != null)
        //                        {
        //                            var nextPoint = new GeoPoint(nextWp.Coordinate.Latitude, nextWp.Coordinate.Longitude);
        //                            heading = MapOptions.CalculateHeading(gp, nextPoint);
        //                        }
        //                    }
        //                    else if (prevPoint != null)
        //                    {
        //                        // 마지막 점은 이전 점으로부터의 각도 유지
        //                        heading = MapOptions.CalculateHeading(prevPoint, gp);
        //                    }

        //                    // 마커 추가
        //                    var marker = new CustomMapPoint
        //                    {
        //                        Latitude = gp.Latitude,
        //                        Longitude = gp.Longitude,
        //                        //TagString = $"{i + 1}", // 순서 표시 (선택사항)
        //                        TagString = $"H{plan.AircraftID}\n{wp.WaypoinID}",
        //                        Heading = heading // ★ XAML에서 회전에 사용됨
        //                    };
        //                    LAHWpMarkerList.Add(marker);

        //                    prevPoint = gp;
        //                }

        //                // 점이 하나라도 있으면 지도 리스트에 추가
        //                //if (missionPolyline.Points.Count > 0)
        //                //{
        //                //    LAHWapointList.Add(missionPolyline);
        //                //}
        //                if (basePolyline.Points.Count > 0)
        //                {
        //                    LAHStaticLineList.Add(basePolyline); // 정적 레이어로
        //                    //LAHPulseLineList.Add(pulsePolyline); // 동적 레이어로
        //                }
        //            }
        //        }
        //    }

        //    // 3. 마커(Marker) 업데이트 (기존 로직 유지 - 생략 가능하지만 마커도 그려야 함)
        //    //UpdateMarkers(allPlans); // *마커 그리는 부분은 별도 메서드로 분리하거나 기존 로직 유지*

        //    // 4. ★ 핵심: 현재 활성 ID에 맞는 펄스만 다시 그리기
        //    RefreshActivePulseLines();
        //}

        public void UpdateAllLAHWaypoints(ObservableCollection<LAHMissionPlan> allPlans)
        {
            // 0. 초기화
            LAHWpMarkerList.Clear();
            LAHStaticLineList.Clear();
            LAHPulseLineList.Clear(); // 펄스 리스트는 더 이상 사용하지 않지만, 기존 잔상이 남지 않게 클리어

            if (allPlans == null) return;

            // [스타일 정의]
            // 1. 기본 경로선 (지나갔거나 예정된 경로) - 반투명 주황색
            var baseColor = new SolidColorBrush(Color.FromArgb(60, 255, 69, 0));
            var baseStyle = new StrokeStyle { Thickness = 4 };

            // 2. [수정] 활성 경로선 (현재 수행 중인 경로) - 진하고 두꺼운 주황색 (Static으로 그림)
            var activeColor = new SolidColorBrush(Color.FromArgb(255, 255, 69, 0)); // 불투명
            var activeStyle = new StrokeStyle { Thickness = 6 }; // 좀 더 두껍게 강조

            // 3. 현재 활성화된(수행 중인) 임무 ID 가져오기
            var ctrlVM = ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance;
            int[] activeIds = new int[4]; // 1~3번 헬기
            if (ctrlVM != null)
            {
                activeIds[1] = ctrlVM.ControlIndividualID1;
                activeIds[2] = ctrlVM.ControlIndividualID2;
                activeIds[3] = ctrlVM.ControlIndividualID3;
            }

            foreach (var plan in allPlans)
            {
                if (plan.MissionSegemntList == null) continue;

                // 현재 헬기의 수행 중인 임무 ID
                int aircraftId = (int)plan.AircraftID;
                int currentActiveMissionId = (aircraftId >= 1 && aircraftId <= 3) ? activeIds[aircraftId] : 0;

                foreach (var segment in plan.MissionSegemntList)
                {
                    if (segment.IndividualMissionList == null) continue;

                    foreach (var individualMission in segment.IndividualMissionList)
                    {
                        if (individualMission.WaypointList == null || individualMission.WaypointList.Count == 0) continue;

                        // -----------------------------------------------------------
                        // [수정 포인트 2] ActivePulse 제거 -> Static 라인으로 통합
                        // 현재 그리는 임무가 '수행 중'인 임무라면 강조 스타일 적용
                        // -----------------------------------------------------------
                        bool isActiveMission = (individualMission.IndividualMissionID == currentActiveMissionId);

                        var missionPolyline = new MapPolyline
                        {
                            Stroke = isActiveMission ? activeColor : baseColor,       // 색상 분기
                            StrokeStyle = isActiveMission ? activeStyle : baseStyle, // 두께 분기
                            IsGeodesic = false
                        };

                        GeoPoint prevPoint = null;
                        int wpCount = individualMission.WaypointList.Count;

                        for (int i = 0; i < wpCount; i++)
                        {
                            var wp = individualMission.WaypointList[i];

                            // -----------------------------------------------------------
                            // [수정 포인트 3] 좌표 유효성 검사 및 이벤트 로그 출력
                            // -----------------------------------------------------------
                            if (wp.Coordinate == null) continue;

                            // 좌표가 (0, 0)에 매우 가까우면 오류로 판단 (부동소수점 오차 고려)
                            if (Math.Abs(wp.Coordinate.Latitude) < 0.0001 && Math.Abs(wp.Coordinate.Longitude) < 0.0001)
                            {
                                // 운영자가 볼 수 있는 이벤트 로그에 에러 출력 (Level 4 = Error/Red)
                                string errorMsg = $"[데이터 오류] 호기:{plan.AircraftID}, 임무ID:{individualMission.IndividualMissionID}, WP:{wp.WaypoinID} 좌표가 (0,0)입니다.";

                                // UI 스레드 안전하게 호출
                                Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    ViewModel_ScenarioView.SingletonInstance.AddLog(errorMsg, 4);
                                });

                                continue; // 해당 점은 그리지 않고 건너뜀
                            }

                            var gp = new GeoPoint(wp.Coordinate.Latitude, wp.Coordinate.Longitude);
                            missionPolyline.Points.Add(gp);

                            // Heading 계산 및 마커 생성 (기존 로직 유지)
                            double heading = 0;
                            if (i < wpCount - 1)
                            {
                                var nextWp = individualMission.WaypointList[i + 1];
                                if (nextWp.Coordinate != null)
                                {
                                    var nextPoint = new GeoPoint(nextWp.Coordinate.Latitude, nextWp.Coordinate.Longitude);
                                    heading = MapOptions.CalculateHeading(gp, nextPoint);
                                }
                            }
                            else if (prevPoint != null)
                            {
                                heading = MapOptions.CalculateHeading(prevPoint, gp);
                            }

                            var marker = new CustomMapPoint
                            {
                                Latitude = gp.Latitude,
                                Longitude = gp.Longitude,
                                TagString = $"H{plan.AircraftID}\n{wp.WaypoinID}",
                                Heading = heading
                            };
                            LAHWpMarkerList.Add(marker);

                            prevPoint = gp;
                        }

                        // 점이 2개 이상일 때만 선을 추가 (점이 1개면 선이 안됨)
                        if (missionPolyline.Points.Count > 1)
                        {
                            // 펄스 리스트 대신 정적 리스트에 모두 추가
                            LAHStaticLineList.Add(missionPolyline);
                        }
                    }
                }
            }
        }

        // 현재 활성화된 임무 ID에 해당하는 경로만 '펄스' 효과를 주는 메서드
        public void RefreshActivePulseLines()
        {
            // 1. 기존 펄스만 싹 지움
            LAHPulseLineList.Clear();

            // 2. 현재 각 헬기의 수행 중인 임무 ID를 가져옴
            var ctrlVM = ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance;
            if (ctrlVM == null) return;

            int[] activeIds = new int[4]; // 1~3번 헬기 (인덱스 편의상 4개)
            activeIds[1] = ctrlVM.ControlIndividualID1;
            activeIds[2] = ctrlVM.ControlIndividualID2;
            activeIds[3] = ctrlVM.ControlIndividualID3;

            //var pulseColor = new SolidColorBrush(Colors.White); // 펄스 색상 (밝은 흰색)
            var pulseColor = new SolidColorBrush(Color.FromArgb(255, 255, 69, 0));

            // 3. 전체 계획을 순회하면서 '현재 ID'와 일치하는 녀석만 PulseList에 추가
            var allPlans = ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.LAHMissionPlanItemSource;
            if (allPlans == null) return;

            foreach (var plan in allPlans)
            {
                // 헬기 ID (1, 2, 3)
                int aircraftId = (int)plan.AircraftID;
                if (aircraftId < 1 || aircraftId > 3) continue;

                // 이 헬기가 현재 수행해야 할 미션 ID
                int currentActiveMissionId = activeIds[aircraftId];

                if (plan.MissionSegemntList == null) continue;
                foreach (var segment in plan.MissionSegemntList)
                {
                    if (segment.IndividualMissionList == null) continue;
                    foreach (var individualMission in segment.IndividualMissionList)
                    {
                        // ★★★ 조건 검사: 현재 수행 중인 미션 ID와 일치하는가? ★★★
                        if (individualMission.IndividualMissionID == currentActiveMissionId)
                        {
                            if (individualMission.WaypointList == null || individualMission.WaypointList.Count == 0) continue;

                            // 펄스 라인 생성
                            var pulsePolyline = new MapPolyline
                            {
                                Stroke = pulseColor,
                                StrokeStyle = _pulseStrokeStyle, // 애니메이션 스타일 적용
                                IsGeodesic = false
                            };

                            foreach (var wp in individualMission.WaypointList)
                            {
                                if (wp.Coordinate == null) continue;
                                pulsePolyline.Points.Add(new GeoPoint(wp.Coordinate.Latitude, wp.Coordinate.Longitude));
                            }

                            if (pulsePolyline.Points.Count > 0)
                            {
                                LAHPulseLineList.Add(pulsePolyline);
                            }
                        }
                    }
                }
            }
        }

        public void UpdateAllUAVWaypoints(ObservableCollection<UAVMissionPlan> allPlans)
        {
            // 0. 초기화
            UAVWapointList.Clear();
            UAVWpMarkerList.Clear();

            if (allPlans == null) return;

            foreach (var plan in allPlans)
            {
                if (plan.MissionSegemntList == null) continue;

                foreach (var segment in plan.MissionSegemntList)
                {
                    if (segment.IndividualMissionList == null) continue;

                    foreach (var individualMission in segment.IndividualMissionList)
                    {
                        // 비행 타입 검사 및 웨이포인트 존재 확인
                        if (individualMission.FlightType != 1 ||
                            individualMission.WaypointList == null ||
                            individualMission.WaypointList.Count == 0) continue;

                        // 1. 경로선 (Polyline) 생성 - 펄스 없이 단순 실선
                        var missionPolyline = new MapPolyline()
                        {
                            // Stroke = System.Windows.Media.Brushes.Orange, // 필요시 색상 지정
                            StrokeStyle = new StrokeStyle { Thickness = 4 }
                        };

                        // 이전 좌표 저장용 (마지막 점 방향 계산용)
                        GeoPoint prevPoint = null;
                        int wpCount = individualMission.WaypointList.Count;

                        // 2. 웨이포인트 루프 (Heading 계산을 위해 for문 사용)
                        for (int i = 0; i < wpCount; i++)
                        {
                            var wp = individualMission.WaypointList[i];
                            if (wp.Coordinate == null) continue;

                            var gp = new GeoPoint(wp.Coordinate.Latitude, wp.Coordinate.Longitude);

                            // 2-1. 경로선에 점 추가
                            missionPolyline.Points.Add(gp);

                            // 2-2. ★ Heading(방향) 계산 로직 추가 ★
                            double heading = 0;

                            if (i < wpCount - 1)
                            {
                                // 다음 점이 있으면, 현재 점 -> 다음 점의 각도 계산
                                var nextWp = individualMission.WaypointList[i + 1];
                                if (nextWp.Coordinate != null)
                                {
                                    var nextPoint = new GeoPoint(nextWp.Coordinate.Latitude, nextWp.Coordinate.Longitude);
                                    heading = MapOptions.CalculateHeading(gp, nextPoint);
                                }
                            }
                            else if (prevPoint != null)
                            {
                                // 마지막 점이면, 이전 점 -> 현재 점의 각도 유지
                                heading = MapOptions.CalculateHeading(prevPoint, gp);
                            }

                            // 2-3. 마커 생성 및 Heading 적용
                            var marker = new CustomMapPoint
                            {
                                Latitude = gp.Latitude,
                                Longitude = gp.Longitude,
                                TagString = $"U{plan.AircraftID}\n{wp.WaypoinID}",
                                Heading = heading // ★ XAML 바인딩용 헤딩 설정
                            };
                            UAVWpMarkerList.Add(marker);

                            // 현재 점을 이전 점으로 저장
                            prevPoint = gp;
                        }

                        // 3. 완성된 경로선을 지도 리스트에 추가
                        if (missionPolyline.Points.Count > 0)
                        {
                            UAVWapointList.Add(missionPolyline);
                        }
                    }
                }
            }
        }

        public void UpdateTextTestWaypoints(ObservableCollection<CustomMapPoint> inputTextTestPoints)
        {
            // 0. 기존 UAV 경로선과 마커를 모두 삭제합니다.
            TextTestWapointList.Clear();
            TextTestWpMarkerList.Clear();
            int order = 0;



            // 1. 모든 UAV 임무 계획을 순회합니다.
            var missionPolyline = new MapPolyline()
            {
                Stroke = System.Windows.Media.Brushes.Orange, // UAV 경로선 색상
                StrokeStyle = new StrokeStyle { Thickness = 2 }
            };


            // 2. 모든 임무 구간(Segment)을 순회합니다.

            // 3. 모든 개별 임무(IndividualMission)를 순회합니다.
            // 경로 비행 타입일 때만 경로를 그립니다.

            // 4. 모든 경로점을 순회합니다.
            foreach (var waypoint in inputTextTestPoints)
            {
                var point = new GeoPoint(waypoint.Latitude, waypoint.Longitude);

                // 4-1. Polyline에 점을 추가합니다.
                missionPolyline.Points.Add(point);

                // 4-2. 마커 객체를 생성하고 라벨을 설정합니다.
                var marker = new CustomMapPoint
                {
                    Latitude = point.Latitude,
                    Longitude = point.Longitude,
                    //TagString = $"U{plan.AircraftID}\n{waypoint.WaypoinID}"
                    TagString = $"{order}"
                };
                TextTestWpMarkerList.Add(marker);

                // 5. 완성된 Polyline을 지도에 추가합니다.
                if (missionPolyline.Points.Count > 0)
                {
                    TextTestWapointList.Add(missionPolyline);
                }
                order++;
            }
        }

        /// <summary>
        /// [신규] 선택된 작전 지역으로 지도의 중심과 줌을 이동합니다.
        /// </summary>
        /// <param name="areaIndex">1:지포리, 2:화성, 3:인제</param>
        public void MoveMapToBattlefield(int areaIndex)
        {
            // 모의 중일 때는 이동하지 않음 (요청사항 반영)
            if (ViewModel_ScenarioView.SingletonInstance.IsSimPlaying) return;

            GeoPoint targetCenter = null;
            double targetZoom = 11.0; // 기본 줌

            switch (areaIndex)
            {
                case 1: // 철원군 지포리
                    // 박스 좌표: (38.146, 127.294) ~ (38.110, 127.341)의 중심
                    targetCenter = new GeoPoint(38.128421, 127.317591);
                    targetZoom = 12.5;
                    break;

                case 2: // 화성 홍익
                    // 박스 좌표: (37.226, 126.970) ~ (37.208, 126.993)의 중심
                    targetCenter = new GeoPoint(37.217808, 126.981541);
                    targetZoom = 13.5; // 구역이 좁아서 좀 더 확대
                    break;

                case 3: // 인제
                    // 박스 좌표: (37.951, 128.135) ~ (37.878, 128.228)의 중심
                    targetCenter = new GeoPoint(37.914548, 128.181858);
                    targetZoom = 12.0; // 구역이 넓음
                    break;

                default:
                    return;
            }

            if (targetCenter != null)
            {
                // UI 스레드에서 View의 MapControl 속성을 직접 변경
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mapView = View_Unit_Map.SingletonInstance;

                    // ★ 주의: View_Unit_Map.xaml에서 MapControl의 x:Name이 "map"이라고 가정했습니다.
                    // 만약 이름이 다르다면(예: mapControl), 그 이름에 맞춰 수정해주세요.
                    if (mapView != null && mapView.mapControl != null)
                    {
                        mapView.mapControl.CenterPoint = targetCenter;
                        mapView.mapControl.ZoomLevel = targetZoom;
                    }
                });
            }
        }
    }
}

