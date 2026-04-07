
using DevExpress.Map;
using DevExpress.Map.Kml.Model;
using DevExpress.Mvvm.Native;
using DevExpress.Pdf.ContentGeneration;
using DevExpress.Xpf.CodeView;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Layout.Core;
using DevExpress.Xpf.Map;
using DevExpress.XtraRichEdit.Model;
using MLAH_Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
//using GMap.NET;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using static MLAH_Controller.CommonUtil;



namespace MLAH_Controller
{


    public partial class ViewModel_UC_Unit_INITMissionInfo : CommonBase
    {
        #region Singleton
        static ViewModel_UC_Unit_INITMissionInfo _ViewModel_UC_Unit_INITMissionInfo = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_UC_Unit_INITMissionInfo SingletonInstance
        {
            get
            {
                if (_ViewModel_UC_Unit_INITMissionInfo == null)
                {
                    _ViewModel_UC_Unit_INITMissionInfo = new ViewModel_UC_Unit_INITMissionInfo();
                }
                return _ViewModel_UC_Unit_INITMissionInfo;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_UC_Unit_INITMissionInfo()
        {
            // 디자인 모드가 아닐 때(즉, 실제 런타임일 때)만 실행
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                CommonEvent.OnINITMissionPointSet += Callback_OnINITMissionPointSet;
                CommonEvent.OnINITMissionLineSet += Callback_OnINITMissionLineSet;
                CommonEvent.OnINITMissionPolygonSet += Callback_OnINITMissionPolygonSet;
                CommonEvent.OnINITMissionPolygonUpdated += Callback_OnINITMissionPolygonUpdated;

            }

            // Command 초기화는 디자인 모드에서도 안전하므로 그대로 둡니다.
            Button1Command = new RelayCommand(Button1CommandAction);
            Button2Command = new RelayCommand(Button2CommandAction);
            Button3Command = new RelayCommand(Button3CommandAction);
            PolygonButton1Command = new RelayCommand(PolygonButton1CommandAction);
            PolygonButton2Command = new RelayCommand(PolygonButton2CommandAction);
            PolygonButton3Command = new RelayCommand(PolygonButton3CommandAction);
        }


        // 1. 지도에서 점 찍고 우클릭 완료 시 호출됨 (생성 모드)
        private void Callback_OnINITMissionPolygonSet(List<GeoPoint> points)
        {
            if (PolygonState != MissionEditState.Creating) return;

            TempPolygonPoints.Clear();
            var srtmReader = ViewModel_Unit_Map.SingletonInstance.SrtmReaderInstance;
            foreach (var p in points)
            {
                int individualAlt = 0;
                if (srtmReader != null)
                {
                    short elevation = srtmReader.GetElevation(p.Latitude, p.Longitude);
                    individualAlt = (elevation == -32768) ? 0 : elevation;
                }

                TempPolygonPoints.Add(new CoordinateInfo
                {
                    Latitude = (float)p.Latitude,
                    Longitude = (float)p.Longitude,
                    Altitude = individualAlt // 입력된 고도 일괄 적용
                });
            }
        }

        // 지도에서 점을 드래그해서 놓았을 때 호출되는 함수
        private void Callback_OnINITMissionPolygonUpdated(List<GeoPoint> points)
        {
            // [중요] 폴리곤 상태가 '수정 중(Editing)'이라면, 
            // 부모 상태(_state)가 Creating이든 Editing이든 상관없이 무조건 좌표를 받아야 합니다.
            if (PolygonState != MissionEditState.Editing) return;

            // 1. 기존 좌표 리스트 비우기
            TempPolygonPoints.Clear();
            var srtmReader = ViewModel_Unit_Map.SingletonInstance.SrtmReaderInstance;

            // 2. 지도에서 받아온 최신 좌표로 채우기
            foreach (var p in points)
            {
                int individualAlt = 0;
                if (srtmReader != null)
                {
                    short elevation = srtmReader.GetElevation(p.Latitude, p.Longitude);
                    individualAlt = (elevation == -32768) ? 0 : elevation;
                }

                TempPolygonPoints.Add(new CoordinateInfo
                {
                    Latitude = (float)p.Latitude,
                    Longitude = (float)p.Longitude,
                    // 고도는 기존 컨트롤 값을 유지하거나 p.Altitude를 쓸 수 있음
                    Altitude = (int)PolygonAltControl
                });
            }

            // (선택 사항) 만약 실시간으로 Model까지 갱신하고 싶다면 여기서 UpdateModelFromTempPoints() 호출
            // 하지만 보통은 '저장' 버튼 누를 때 하므로 여기선 TempPoints만 갱신하면 됨.
        }

        private void LoadExistingPolygons(InputMission mission)
        {
            // 1. UI 그리드용 리스트 초기화
            AreaWrapperList.Clear();

            // 2. 카운터(인덱스) 초기화
            // 이 임무의 0번째, 1번째... 폴리곤임을 식별하기 위해 0으로 리셋합니다.
            _areaIdCounter = 0;

            if (mission.Polygons == null || mission.Polygons.AreaList == null) return;

            foreach (var area in mission.Polygons.AreaList)
            {
                // Wrapper 생성 (UI_ID는 0, 1, 2... 순서가 됨)
                var wrapper = new InitMissionAreaWrapper(area, _areaIdCounter++);
                AreaWrapperList.Add(wrapper);

                // ★ 지도는 이미 Global List에 그려져 있으므로 건드리지 않음!
            }
        }

        // [신규] 상단 메인 버튼(임무 생성/수정/삭제) 상태 관리
        private void UpdateMainControlState()
        {
            bool hasSelection = SelectedinputMissionItem != null;

            if (_state == MenuButtonState.None)
            {
                // 대기 상태
                EditEnable = false;
                IsShapeTypeEditable = false; // [추가] 대기 상태일 때는 확실히 잠금

                Button1Text = "생성";
                Button2Text = "수정";
                Button3Text = "삭제";

                Button1Enable = true;
                Button2Enable = hasSelection;
                Button3Enable = hasSelection;
            }
            else // Creating or Editing
            {
                // 입력 중
                EditEnable = true;

                // 💡 [핵심 수정] Creating일 때뿐만 아니라, Editing일 때도 ShapeType을 바꿀 수 있게 풀어줍니다.
                // 기존: IsShapeTypeEditable = (_state == MenuButtonState.Creating);
                IsShapeTypeEditable = (_state == MenuButtonState.Creating || _state == MenuButtonState.Editing);

                Button1Text = "저장";
                Button3Text = "취소";

                Button1Enable = true;
                Button2Enable = false;
                Button3Enable = true;
            }

            UpdatePolygonControlState();
            UpdateMapInfoPanel();
        }

        // 기존 메서드 보완: 버튼 + 토글 + 텍스트박스 상태 일괄 관리
        // [수정] 하위 폴리곤 버튼 상태 관리 (상단 상태 의존성 추가)
        private void UpdatePolygonControlState()
        {
            // 1. [핵심] 상위 임무 상태가 'None'이면 하위 버튼은 무조건 잠금
            if (_state == MenuButtonState.None)
            {
                PolygonButton1Enable = false; // 생성 불가
                PolygonButton2Enable = false;
                PolygonButton3Enable = false;
                PolygonEditEnable = false;
                PolygonTypeChecked = false;   // 토글도 강제 해제
                return; // 더 볼 것도 없이 종료
            }

            // 2. 만약 ShapeType이 폴리곤(3)이 아니라면 잠금
            if (ShapeTypeIndex != 3) // 3: Area/Polygon
            {
                PolygonButton1Enable = false;
                // ... (나머지도 false 처리하거나 Visibility로 가려질 테니 생략 가능)
                return;
            }

            // 3. 여기서부터는 기존 로직 (상위 상태가 Creating/Editing이고, 폴리곤 모드일 때)
            bool hasPolySelection = SelectedAreaWrapper != null;

            switch (PolygonState)
            {
                case MissionEditState.None:
                    PolygonEditEnable = false;
                    PolygonTypeChecked = false;

                    PolygonButton1Text = "생성";
                    PolygonButton2Text = "수정";
                    PolygonButton3Text = "삭제";

                    PolygonButton1Enable = true; // 이제 생성 가능
                    PolygonButton2Enable = hasPolySelection;
                    PolygonButton3Enable = hasPolySelection;
                    break;

                case MissionEditState.Creating:
                    PolygonEditEnable = true;
                    PolygonTypeChecked = false;

                    PolygonButton1Text = "저장";
                    PolygonButton3Text = "취소";

                    PolygonButton1Enable = true;
                    PolygonButton2Enable = false;
                    PolygonButton3Enable = true;
                    break;

                case MissionEditState.Editing:
                    PolygonEditEnable = true;
                    PolygonTypeChecked = false;

                    PolygonButton1Text = "저장";
                    PolygonButton3Text = "취소";

                    PolygonButton1Enable = true;
                    PolygonButton2Enable = false;
                    PolygonButton3Enable = true;
                    break;
            }
        }


        private void UpdateMapInfoPanel()
        {
            // 1. 대기 상태면 숨김
            if (_state == MenuButtonState.None)
            {
                IsInfoPanelVisible = false;
                CurrentModeTitle = "";
                CurrentShortcutKey = "";
                return;
            }

            // 2. 생성/수정 상태면 일단 띄움
            IsInfoPanelVisible = true;

            // 3. ShapeType에 따라 텍스트 분기
            switch (ShapeTypeIndex)
            {
                case 1: // Point
                    CurrentModeTitle = (_state == MenuButtonState.Creating) ? "지점(Point) 생성 모드" : "지점(Point) 수정 모드";
                    CurrentShortcutKey = "Q";
                    break;

                case 2: // Line
                    CurrentModeTitle = (_state == MenuButtonState.Creating) ? "경로(Line) 생성 모드" : "경로(Line) 수정 모드";
                    CurrentShortcutKey = "W";
                    break;

                case 3: // Area
                        // 폴리곤은 하위 버튼(생성/수정)을 눌러야 실질적인 지도 작업이 시작됨
                    if (PolygonState == MissionEditState.Creating)
                    {
                        CurrentModeTitle = "구역(Area) 생성 (점 추가)";
                        CurrentShortcutKey = "E";
                    }
                    else if (PolygonState == MissionEditState.Editing)
                    {
                        CurrentModeTitle = "구역(Area) 수정 (점 드래그)";
                        CurrentShortcutKey = "E";
                    }
                    else
                    {
                        // 면(Area) 라디오 버튼만 누르고 아직 하위 '생성/수정' 버튼을 안 눌렀을 때
                        CurrentModeTitle = "하위 구역 생성/수정 버튼을 눌러주세요";
                        CurrentShortcutKey = "-";
                    }
                    break;
            }
        }

        // [Helper] 다른 수정 모드 취소
        private void CancelOtherEditModes()
        {
            if (_state == MenuButtonState.Creating) Button1CommandAction(null); // 상단 생성 취소
            // 필요 시 다른 부분도 취소
        }


        private void Callback_OnINITMissionLineSet(LinearMissionResultSet result)
        {
            // DTO 객체에서 데이터 꺼내 쓰기
            WidthControl = result.WidthMeters;
            LinearLOCList.Clear();
            var srtmReader = ViewModel_Unit_Map.SingletonInstance.SrtmReaderInstance;
            foreach (var gp in result.CenterPoints) // result.CenterPoints 사용
            {
                int individualAlt = 0;
                if (srtmReader != null)
                {
                    short elevation = srtmReader.GetElevation(gp.Latitude, gp.Longitude);
                    individualAlt = (elevation == -32768) ? 0 : elevation;
                }

                LinearLOCList.Add(new CoordinateInfo
                {
                    Latitude = (float)gp.Latitude,
                    Longitude = (float)gp.Longitude,
                    Altitude = individualAlt
                });
            }

            // DTO로부터 '사각형 목록'을 받아서 저장
            this.FinalSegmentRectangles = result.SegmentRectangles;

            IsLineSelected = true;
        }

        #endregion 생성자 & 콜백


        public RelayCommand Button1Command { get; set; }

        public void Button1CommandAction(object param)
        {
            if (_state == MenuButtonState.None)
            {
                _state = MenuButtonState.Creating;
                AreaWrapperList.Clear();
                _areaIdCounter = 0;
                ViewModel_Unit_Map.SingletonInstance.ClearTempDrawing();

                Name_Control = GetNextAvailableMissionID();
                Order_Control = GetNextAvailableSequenceNumber();

                UpdateMainControlState();
            }
            else if (_state == MenuButtonState.Creating || _state == MenuButtonState.Editing)
            {
                // 1. [하위 폴리곤 자동 저장 처리]
                // 폴리곤 생성/수정 중인데 상단 '임무 저장'을 누른 경우 하위 저장을 먼저 끝냅니다.
                if (IsAreaSelected && (PolygonState == MissionEditState.Creating || PolygonState == MissionEditState.Editing))
                {
                    PolygonButton1CommandAction(null); // 하위 저장 강제 호출

                    // 만약 점을 3개 미만으로 찍어서 저장이 실패했다면 찌꺼기를 취소 처리
                    if (PolygonState != MissionEditState.None)
                    {
                        PolygonButton3CommandAction(null);
                    }
                }

                // =========================================================================
                // 🚨 [방어 코드 0] 임무 유형(MissionType) 유효성 검사
                // =========================================================================
                if (MissionTypeIndex == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var pop_error = new View_PopUp(5); // 에러 아이콘 인덱스
                        pop_error.Description.Text = "임무 저장 불가";
                        pop_error.Reason.Text = "임무 유형이 'N/A'로 지정되어 있습니다.\n목록에서 알맞은 임무 유형을 선택해주세요.";
                        pop_error.Show();
                    });
                    return; // 저장 중단
                }

                // =========================================================================
                // 🚨 [방어 코드] 형상(Shape)별 필수 데이터 유효성 검사 (저장 차단 및 팝업)
                // =========================================================================
                if (IsPointSelected && (PointLat_Control == 0 && PointLon_Control == 0))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var pop_error = new View_PopUp(5); // 에러 아이콘 인덱스로 조정 (예: 0)
                        pop_error.Description.Text = "임무 저장 불가";
                        pop_error.Reason.Text = "지점(Point) 좌표가 입력되지 않았습니다.\n지도에 지점을 클릭하여 생성해주세요.";
                        pop_error.Show();
                    });
                    return; // 저장 중단
                }
                else if (IsLineSelected && (LinearLOCList == null || LinearLOCList.Count < 2))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var pop_error = new View_PopUp(5);
                        pop_error.Description.Text = "임무 저장 불가";
                        pop_error.Reason.Text = "경로(Line) 데이터가 불완전합니다.\n지도에 2개 이상의 점을 찍어 선을 완성해주세요.";
                        pop_error.Show();
                    });
                    return; // 저장 중단
                }
                else if (IsAreaSelected && (AreaWrapperList == null || AreaWrapperList.Count == 0))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var pop_error = new View_PopUp(5);
                        pop_error.Description.Text = "임무 저장 불가";
                        pop_error.Reason.Text = "다각형(Area) 구역이 없습니다.\n지도에 최소 1개 이상의 구역을 생성해주세요.";
                        pop_error.Show();
                    });
                    return; // 저장 중단
                }
                // =========================================================================

                // 3. 유효성 검사를 모두 통과했다면, 실제 데이터 저장 로직 진행
                if (_state == MenuButtonState.Creating)
                {
                    // 순번 중복 확인 및 밀어내기
                    uint desiredOrder = Order_Control;
                    if (!HandleSequenceNumberConflict(desiredOrder, null))
                        return; // 취소 선택 시 저장 중단

                    EditEnable = false;
                    Button2Enable = false;
                    Button3Enable = false;
                    Button1Text = "생성";
                    Button3Text = "삭제";

                    var InputMission = new InputMission();
                    InputMission.InputMissionID = GetNextAvailableMissionID();
                    InputMission.SequenceNumber = desiredOrder;
                    InputMission.InputMissionType = (uint)MissionTypeIndex;
                    InputMission.RegionType = RegionTypeIndex;
                    InputMission.IsDone = IsDoneIndex == 1 ? false : true;

                    // 점 저장
                    if (IsPointSelected == true)
                    {
                        InputMission.ShapeType = 1;
                        var InputPoint = new CoordinateInfo
                        {
                            Latitude = (float)PointLat_Control,
                            Longitude = (float)PointLon_Control,
                            Altitude = (int)PointAlt_Control
                        };
                        InputMission.Coordinate = InputPoint;
                    }
                    // 선 저장
                    else if (IsLineSelected == true)
                    {
                        InputMission.ShapeType = 2;
                        if (InputMission.PolyLine == null) InputMission.PolyLine = new PolyLineInfo();
                        InputMission.PolyLine.Width = (uint)WidthControl;
                        if (InputMission.PolyLine.CoordinateList == null) InputMission.PolyLine.CoordinateList = new ObservableCollection<CoordinateInfo>();

                        foreach (var p in LinearLOCList)
                        {
                            InputMission.PolyLine.CoordinateList.Add(new CoordinateInfo
                            {
                                Latitude = p.Latitude,
                                Longitude = p.Longitude,
                                Altitude = p.Altitude
                            });
                        }
                        InputMission.PolyLine.CoordinateListN = (uint)InputMission.PolyLine.CoordinateList.Count;
                    }
                    // 다각형 저장
                    else if (IsAreaSelected == true)
                    {
                        InputMission.ShapeType = 3;
                        InputMission.Polygons = new PolyGon();
                        InputMission.Polygons.AreaList = new ObservableCollection<AreaInfo>();

                        foreach (var wrapper in AreaWrapperList)
                        {
                            wrapper.Model.CoordinateListN = (uint)wrapper.Model.CoordinateList.Count;
                            InputMission.Polygons.AreaList.Add(wrapper.Model);
                            InputMission.Polygons.AreaListN++;
                        }
                        ViewModel_Unit_Map.SingletonInstance.PreINITMissionPolygonList.Clear();
                    }

                    // 지도 갱신 및 시나리오 리스트 추가
                    InitMissionSet(InputMission);
                    inputMissionList.Add(InputMission);

                    if (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.InputMissionPackage == null)
                        ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.InputMissionPackage = new InputMissionPackage();

                    if (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.InputMissionPackage.InputMissionList == null)
                        ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.InputMissionPackage.InputMissionList = new ObservableCollection<InputMission>();

                    ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.InputMissionPackage.InputMissionList.Add(InputMission);
                    ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.InputMissionPackage.InputMissionListN++;

                    SelectedinputMissionListIndex = inputMissionList.Count - 1;
                    _state = MenuButtonState.None;
                    UpdateMainControlState();
                }
                else if (_state == MenuButtonState.Editing)
                {
                    if (IsAreaSelected && AreaWrapperList.Count == 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var pop_error = new View_PopUp(5,300);
                            pop_error.Description.Text = "임무 수정 불가";
                            pop_error.Reason.Text = "다각형 구역이 모두 삭제되어 빈 임무가 되었습니다.\n구역을 추가하거나 임무 자체를 삭제(취소)해주세요.";
                            pop_error.Show();
                        });
                        return;
                    }

                    var targetMission = SelectedinputMissionItem;

                    // 순번 재정렬 (위/아래 이동 모두 처리)
                    if (targetMission != null && Order_Control != targetMission.SequenceNumber)
                    {
                        if (!HandleSequenceNumberReorder(Order_Control, targetMission))
                            return; // 취소 선택 시 저장 중단
                    }

                    EditEnable = false;
                    Button2Enable = false;
                    Button3Enable = false;
                    Button1Text = "생성";
                    Button3Text = "삭제";

                    if (targetMission != null)
                    {
                        if (Name_Control != 0) targetMission.InputMissionID = Name_Control;
                        targetMission.SequenceNumber = Order_Control;
                        targetMission.InputMissionType = (uint)MissionTypeIndex;
                        targetMission.RegionType = RegionTypeIndex;
                        targetMission.IsDone = IsDoneIndex == 1 ? false : true;

                        // ==========================================================
                        // 점(Point) 업데이트 로직
                        // ==========================================================
                        if (IsPointSelected)
                        {
                            targetMission.ShapeType = 1;
                            targetMission.Coordinate = new CoordinateInfo
                            {
                                Latitude = (float)PointLat_Control,
                                Longitude = (float)PointLon_Control,
                                Altitude = (int)PointAlt_Control
                            };

                            // 기존 점 제거 (InitMissionSet에서 새 점 추가됨)
                            var mapVM_pt = ViewModel_Unit_Map.SingletonInstance;
                            var pointToRemove = mapVM_pt.INITMissionPointList
                                .FirstOrDefault(p => p.MissionID == (int)targetMission.InputMissionID);
                            if (pointToRemove != null) mapVM_pt.INITMissionPointList.Remove(pointToRemove);
                        }

                        // ==========================================================
                        // 선(Line) 업데이트 로직
                        // ==========================================================
                        if (IsLineSelected)
                        {
                            // 1. 모델 데이터 갱신
                            if (targetMission.PolyLine == null) targetMission.PolyLine = new PolyLineInfo();
                            if (targetMission.PolyLine.CoordinateList == null) targetMission.PolyLine.CoordinateList = new ObservableCollection<CoordinateInfo>();

                            targetMission.PolyLine.Width = (uint)WidthControl;
                            targetMission.PolyLine.CoordinateList.Clear();

                            foreach (var p in LinearLOCList)
                            {
                                targetMission.PolyLine.CoordinateList.Add(new CoordinateInfo
                                {
                                    Latitude = p.Latitude,
                                    Longitude = p.Longitude,
                                    Altitude = p.Altitude
                                });
                            }
                            targetMission.PolyLine.CoordinateListN = (uint)targetMission.PolyLine.CoordinateList.Count;

                            // 2. ★ 지도에 남아있는 기존 선의 잔재(중심선, 폭 사각형, 라벨) 완벽 제거
                            var mapVM = ViewModel_Unit_Map.SingletonInstance;

                            // 중심선 제거
                            var lineToRemove = mapVM.INITMissionLineList.FirstOrDefault(l => l.MissionId == targetMission.InputMissionID);
                            if (lineToRemove != null) mapVM.INITMissionLineList.Remove(lineToRemove);

                            // 폭(Width) 회랑 다각형 제거
                            var linePolygonsToRemove = mapVM.INITMissionLinePolygonList.Where(p => p.MissionID == targetMission.InputMissionID).ToList();
                            foreach (var p in linePolygonsToRemove) mapVM.INITMissionLinePolygonList.Remove(p);

                            // 라벨 제거
                            var labelToRemove = mapVM.INITMissionLineLabelList.FirstOrDefault(p => p.MissionID == targetMission.InputMissionID);
                            if (labelToRemove != null) mapVM.INITMissionLineLabelList.Remove(labelToRemove);
                        }

                        // [폴리곤 업데이트] (점/선 로직도 추후 필요 시 여기에 추가)
                        if (IsAreaSelected)
                        {
                            if (targetMission.Polygons == null) targetMission.Polygons = new PolyGon();
                            if (targetMission.Polygons.AreaList == null) targetMission.Polygons.AreaList = new ObservableCollection<AreaInfo>();

                            targetMission.Polygons.AreaList.Clear();

                            foreach (var wrapper in AreaWrapperList)
                            {
                                wrapper.Model.CoordinateListN = (uint)wrapper.Model.CoordinateList.Count;
                                targetMission.Polygons.AreaList.Add(wrapper.Model);
                            }
                            targetMission.Polygons.AreaListN = (uint)targetMission.Polygons.AreaList.Count;
                        }

                        // 지도 다시 그리기
                        InitMissionSet(targetMission);
                    }

                    // 상태 초기화
                    _state = MenuButtonState.None;
                    PolygonState = MissionEditState.None;
                    UpdateMainControlState();

                    // 임시 객체 정리 및 최신 데이터로 그리드 재로딩 (앞서 요청하신 내용 적용)
                    ViewModel_Unit_Map.SingletonInstance.ClearTempDrawing();
                    LoadExistingPolygons(targetMission);
                }
            }
        }

        // [상단] 임무 수정 버튼 (기존 임무를 불러와서 수정 모드 진입)
        public RelayCommand Button2Command { get; set; }
        public void Button2CommandAction(object param)
        {
            // 선택된 임무가 없으면 리턴
            if (SelectedinputMissionItem == null) return;

            // 1. 상태 변경 (Creating -> Editing)
            _state = MenuButtonState.Editing;

            // 2. UI 활성화
            //EditEnable = true;          // 텍스트박스 풀기
            //Button1Enable = true;       // 저장 버튼 활성
            //Button2Enable = false;      // 수정 버튼 비활성 (이미 눌렀으니까)
            //Button3Enable = true;       // 취소 버튼 활성

            //Button1Text = "저장";
            //Button3Text = "취소";

            // 3. 기존 데이터 로드 (AreaWrapperList 채우기)
            // SelectedinputMissionItem setter에서 LoadExistingPolygons를 호출하므로
            // 여기서는 별도로 호출 안 해도 되지만, 확실히 하기 위해 호출해도 됨.
            LoadExistingPolygons(SelectedinputMissionItem);

            // 4. 지도 정리 (혹시 모를 임시 객체 제거)
            ViewModel_Unit_Map.SingletonInstance.ClearTempDrawing();

            UpdateMainControlState();
        }

        // 수정 중인 아이템의 원래 인덱스를 저장할 변수
        private int _editingItemIndex = -1;


        // ─────────────────────────────────────────────────────────────
        // [버튼 1] 폴리곤 생성(Create) / 저장(Save)
        // ─────────────────────────────────────────────────────────────
        public RelayCommand PolygonButton1Command { get; set; }
        public void PolygonButton1CommandAction(object param)
        {
            var mapVM = ViewModel_Unit_Map.SingletonInstance;

            // [Situation 1] 대기 상태 -> 생성 모드 진입
            if (PolygonState == MissionEditState.None)
            {
                PolygonState = MissionEditState.Creating;
                UpdatePolygonControlState();

                TempPolygonPoints.Clear();

                // 지도에 남아있던 선 그리기 임시 객체 정리
                mapVM.ClearTempDrawing();

                // 편집 레이어 활성화 (Temp 대상)
                mapVM.IsTempINITMissionPolygonEditMode = true;
                mapVM.CurrentEditLayer = ViewModel_Unit_Map.EditLayerType.TempInitMission;
            }
            // [Situation 3] 수정 모드 -> 변경 사항 저장
            else if (PolygonState == MissionEditState.Editing)
            {
                SaveEditedPolygon();
            }

            // [Situation 2] 생성 모드 -> 폴리곤 1개 임시 저장 (WrapperList에 추가)
            else if (PolygonState == MissionEditState.Creating)
            {
                if (TempPolygonPoints.Count < 3) return; // 유효성 검사

                // 1. Model 생성
                var newArea = new AreaInfo();
                newArea.IsHole = (IsHoleIndex == 0); // 0:제외, 1:포함 (순서 확인 필요)
                foreach (var item in TempPolygonPoints)
                {
                    newArea.CoordinateList.Add(new CoordinateInfo
                    {
                        Latitude = item.Latitude,
                        Longitude = item.Longitude,
                        Altitude = item.Altitude
                    });
                }
                newArea.CoordinateListN = (uint)newArea.CoordinateList.Count;

                // 2. Wrapper 생성 및 UI 리스트 추가
                var wrapper = new InitMissionAreaWrapper(newArea, _areaIdCounter++);
                AreaWrapperList.Add(wrapper);

                // 3. 지도 [임시 레이어]에 그리기 (화면 표시용)
                var mapPoly = new CustomMapPolygon { MissionID = wrapper.UI_ID };

                // 스타일 설정 (Hole 여부에 따라)
                SetPolygonStyle(mapPoly, newArea.IsHole);

                foreach (var p in newArea.CoordinateList)
                    mapPoly.Points.Add(new GeoPoint(p.Latitude, p.Longitude));

                // ★ Pre 리스트에 추가하여 화면에 고정 표시
                mapVM.PreINITMissionPolygonList.Add(mapPoly);

                // 3. 정리 (Temp 비우기)
                mapVM.TempINITMissionPolygonList.Clear(); // 이제 Temp는 비움
                mapVM.TempINITMissionPolygonLineList.Clear(); // 라인 비움

                // 4. 상태 초기화
                PolygonState = MissionEditState.None;
                UpdatePolygonControlState();
                TempPolygonPoints.Clear();


                // 편집 모드 종료
                mapVM.IsTempINITMissionPolygonEditMode = false;
                mapVM.CurrentEditLayer = ViewModel_Unit_Map.EditLayerType.None;
            }

        }

        public RelayCommand Button3Command { get; set; }
        public void Button3CommandAction(object param)
        {
            var mapVM = ViewModel_Unit_Map.SingletonInstance;

            if (_state == MenuButtonState.None)
            {
                // ─────────────────────────────────────────────────────────────
                // [삭제 모드] 현재 선택된 메인 임무 삭제 (ShapeType별 분기)
                // ─────────────────────────────────────────────────────────────
                if (SelectedinputMissionItem != null)
                {
                    var targetMission = SelectedinputMissionItem;
                    int targetId = (int)targetMission.InputMissionID;

                    // 1. ShapeType(점, 선, 면)에 따른 지도 객체 완벽 삭제
                    if (targetMission.ShapeType == 1) // 점 (Point)
                    {
                        var pointsToRemove = mapVM.INITMissionPointList.OfType<CustomMapPoint>().Where(p => p.MissionID == targetId).ToList();
                        foreach (var pt in pointsToRemove) mapVM.INITMissionPointList.Remove(pt);
                    }
                    else if (targetMission.ShapeType == 2) // 선 (Line)
                    {
                        // A. 선(중심선) 삭제
                        var linesToRemove = mapVM.INITMissionLineList.OfType<CustomMapLine>().Where(l => l.MissionId == targetId).ToList();
                        foreach (var l in linesToRemove) mapVM.INITMissionLineList.Remove(l);

                        // B. 선의 폭(Width)으로 생성된 회랑 다각형(Corridor) 삭제
                        // (맵 뷰모델에 LinePolygonList가 별도로 존재할 경우 여기서 지움)
                        if (mapVM.INITMissionLinePolygonList != null)
                        {
                            var polysToRemove = mapVM.INITMissionLinePolygonList.OfType<CustomMapPolygon>().Where(p => p.MissionID == targetId).ToList();
                            foreach (var p in polysToRemove) mapVM.INITMissionLinePolygonList.Remove(p);
                        }

                        // C. 선의 중앙에 띄운 라벨용 점(Point/Box) 삭제
                        // (맵 뷰모델에 LineLabelList가 별도로 존재할 경우 여기서 지움)
                        if (mapVM.INITMissionLineLabelList != null)
                        {
                            var labelsToRemove = mapVM.INITMissionLineLabelList.OfType<CustomMapPoint>().Where(p => p.MissionID == targetId).ToList();
                            foreach (var label in labelsToRemove) mapVM.INITMissionLineLabelList.Remove(label);
                        }
                    }
                    else if (targetMission.ShapeType == 3) // 면 (Polygon)
                    {
                        var polysToRemove = mapVM.INITMissionPolygonList.OfType<CustomMapPolygon>().Where(p => p.MissionID == targetId).ToList();
                        foreach (var p in polysToRemove) mapVM.INITMissionPolygonList.Remove(p);
                    }

                    // (만약 위처럼 직접 리스트에서 안 지우고 삭제용 CommonEvent를 사용 중이시라면 아래처럼 호출하셔도 됩니다)
                    // CommonEvent.OnINITMissionRemove?.Invoke(targetId);

                    // 2. 글로벌 임무 리스트에서 완전 삭제
                    inputMissionList.Remove(targetMission);

                    // 3. 시나리오 패키지 모델에서도 동기화하여 삭제 처리
                    var initScenario = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario?.InitScenario;
                    if (initScenario?.InputMissionPackage?.InputMissionList != null)
                    {
                        var packageList = initScenario.InputMissionPackage.InputMissionList;
                        if (packageList.Contains(targetMission))
                        {
                            packageList.Remove(targetMission);
                            initScenario.InputMissionPackage.InputMissionListN = (uint)packageList.Count; // 개수 갱신
                        }

                        // 삭제 후 순번을 0부터 다시 일괄 부여 (기존 순번 순서 유지, ID는 변경 안 함)
                        var sorted = packageList.OrderBy(m => m.SequenceNumber).ToList();
                        for (int i = 0; i < sorted.Count; i++)
                        {
                            sorted[i].SequenceNumber = (uint)i;
                        }
                    }

                    // 4. UI 텍스트 및 그리드 강제 비우기
                    SelectedinputMissionItem = null;
                    AreaWrapperList.Clear();
                    LinearLOCList.Clear();
                    PointLat_Control = 0; PointLon_Control = 0; PointAlt_Control = 0;
                    WidthControl = 0;
                }
            }
            else if (_state == MenuButtonState.Creating || _state == MenuButtonState.Editing)
            {
                // ─────────────────────────────────────────────────────────────
                // [취소 모드] 임무 생성 또는 수정 작업 취소
                // ─────────────────────────────────────────────────────────────

                // 1. 하위 폴리곤이 수정/생성 중이라면 하위 '취소' 동작 강제 호출 (찌꺼기 제거 및 원상복구)
                if (ShapeTypeIndex == 3 && PolygonState != MissionEditState.None)
                {
                    PolygonButton3CommandAction(null);
                }

                // 2. 맵에 그려진 임시 객체들 완벽 초기화 (점/선 그리기 임시 리스트도 있다면 여기서 Clear)
                mapVM.ClearTempDrawing();
                mapVM.PreINITMissionPolygonList?.Clear();
                // mapVM.TempINITMissionLineList?.Clear(); // 선 그리기가 있을 경우 주석 해제

                // 3. UI 및 데이터 롤백
                if (_state == MenuButtonState.Editing && SelectedinputMissionItem != null)
                {
                    // 수정을 취소했으므로, XAML 바인딩으로 틀어졌을 수 있는 화면 데이터를 원본으로 다시 덮어씌움
                    var temp = SelectedinputMissionItem;
                    SelectedinputMissionItem = null; // 바인딩 갱신 트리거를 위해 null 세팅 후
                    SelectedinputMissionItem = temp; // 재할당하여 Setter의 LoadExistingPolygons 등 재호출 유도

                    //실시간으로 삭제했던 폴리곤 등을 다시 원본 기준으로 롤백(다시 그리기)
                    InitMissionSet(temp);
                }
                else if (_state == MenuButtonState.Creating)
                {
                    // 생성을 취소했으므로 입력 중이던 하위 목록 모두 초기화
                    AreaWrapperList.Clear();
                    LinearLOCList.Clear();
                    PointLat_Control = 0; PointLon_Control = 0; PointAlt_Control = 0;
                    WidthControl = 0;
                }
            }

            // ─────────────────────────────────────────────────────────────
            // [공통 마무리] 상태를 None으로 돌리고 UI 전체 갱신
            // ─────────────────────────────────────────────────────────────
            _state = MenuButtonState.None;
            PolygonState = MissionEditState.None; // 하위 폴리곤 상태도 안전하게 닫기
            UpdateMainControlState();
        }

        // ─────────────────────────────────────────────────────────────
        // [버튼 2] 폴리곤 수정(Edit)
        // ─────────────────────────────────────────────────────────────
        // [하단] 폴리곤 수정 버튼
        public RelayCommand PolygonButton2Command { get; set; }
        public void PolygonButton2CommandAction(object param)
        {
            if (SelectedAreaWrapper == null) return;

            var mapVM = ViewModel_Unit_Map.SingletonInstance;

            // 1. 상태 변경
            PolygonState = MissionEditState.Editing;
            UpdatePolygonControlState();

            // 2. 데이터 채우기
            FillTempPointsFromWrapper(SelectedAreaWrapper);

            // 3. ★ [핵심] 리스트에서 타겟 찾기 (Index 기반 매칭)
            CustomMapPolygon targetPoly = null;
            IList sourceList = null;

            // [수정 중] Global List에서 찾을 때
            if (_state == MenuButtonState.Editing)
            {
                sourceList = mapVM.INITMissionPolygonList;

                // [★보완★] 단순히 리스트의 순서(Index)로 찾지 말고, 
                // MissionID와 PolygonIndex가 모두 일치하는지 확인해야 안전합니다.
                targetPoly = mapVM.INITMissionPolygonList
                                .OfType<CustomMapPolygon>() // 형변환 안전장치
                                .FirstOrDefault(p =>
                                    p.MissionID == (int)SelectedinputMissionItem.InputMissionID &&
                                    p.PolygonIndex == SelectedAreaWrapper.UI_ID); // UI_ID와 PolygonIndex 매핑
            }
            // [생성 중] PreList에서 찾을 때
            else if (_state == MenuButtonState.Creating)
            {
                sourceList = mapVM.PreINITMissionPolygonList;
                // 생성 중일 때는 MissionID를 임시 ID(UI_ID)로 쓰고 있으므로 기존 로직 유지
                targetPoly = mapVM.PreINITMissionPolygonList.FirstOrDefault(p => p.MissionID == SelectedAreaWrapper.UI_ID);
            }

            // 4. 찾은 폴리곤을 리스트에서 잠시 제거하고 Temp로 이동
            if (targetPoly != null && sourceList != null)
            {
                _editingItemIndex = sourceList.IndexOf(targetPoly); // 원래 있던 위치(순서) 기억
                sourceList.Remove(targetPoly);
            }
            //else
            //{
            //    _editingItemIndex = -1;
            //}

            // 5. Temp에 추가 (편집 시작)
            var editPoly = new CustomMapPolygon { MissionID = SelectedAreaWrapper.UI_ID };
            SetPolygonStyle(editPoly, SelectedAreaWrapper.IsHole);
            foreach (var p in SelectedAreaWrapper.Model.CoordinateList)
                editPoly.Points.Add(new GeoPoint(p.Latitude, p.Longitude));

            mapVM.TempINITMissionPolygonList.Clear();
            mapVM.TempINITMissionPolygonList.Add(editPoly);

            // 6. 맵 모드 활성화
            mapVM.IsTempINITMissionPolygonEditMode = true;
            mapVM.CurrentEditLayer = ViewModel_Unit_Map.EditLayerType.TempInitMission;
        }

        // ─────────────────────────────────────────────────────────────
        // [버튼 3] 폴리곤 삭제(Delete) / 취소(Cancel)
        // ─────────────────────────────────────────────────────────────
        public RelayCommand PolygonButton3Command { get; set; }
        public void PolygonButton3CommandAction(object param)
        {
            var mapVM = ViewModel_Unit_Map.SingletonInstance;

            // [Situation 1] 대기 상태 -> 삭제
            if (PolygonState == MissionEditState.None)
            {
                var target = param as InitMissionAreaWrapper ?? SelectedAreaWrapper;
                if (target == null) return;

                // UI 리스트 제거
                AreaWrapperList.Remove(target);

                // 2. 지도 실시간 반영
                if (_state == MenuButtonState.Creating)
                {
                    // 생성 중일 때는 Pre 리스트에서 제거
                    var mapPoly = mapVM.PreINITMissionPolygonList.FirstOrDefault(p => p.MissionID == target.UI_ID);
                    if (mapPoly != null) mapVM.PreINITMissionPolygonList.Remove(mapPoly);
                }
                else if (_state == MenuButtonState.Editing)
                {
                    // ★ [수정] 수정 중일 때는 Global 리스트에서 즉시 제거하여 지도에 실시간 반영
                    var mapPoly = mapVM.INITMissionPolygonList.OfType<CustomMapPolygon>()
                                       .FirstOrDefault(p => p.MissionID == (int)SelectedinputMissionItem.InputMissionID && p.PolygonIndex == target.UI_ID);

                    if (mapPoly != null) mapVM.INITMissionPolygonList.Remove(mapPoly);
                }
            }
            // [Situation 2] 생성 중 -> 취소
            else if (PolygonState == MissionEditState.Creating)
            {
                // Temp만 비우면 됨 (Pre에는 아직 안 들어갔으므로)
                mapVM.TempINITMissionPolygonList.Clear();
                mapVM.ClearTempDrawing();

                PolygonState = MissionEditState.None;
                UpdatePolygonControlState();

                mapVM.IsTempINITMissionPolygonEditMode = false;
                mapVM.CurrentEditLayer = ViewModel_Unit_Map.EditLayerType.None;
            }
            // [Situation 3] 수정 중 -> 취소 (원상복구)
            else if (PolygonState == MissionEditState.Editing)
            {
                // 1. 데이터 복구 (Model <- Backup)
                RestoreModelFromBackup(); // 기존 로직

                // 지도 복구: Temp -> 버림, Pre -> 원래 자리에 복구
                mapVM.TempINITMissionPolygonList.Clear();

                var restorePoly = new CustomMapPolygon { MissionID = SelectedAreaWrapper.UI_ID };
                SetPolygonStyle(restorePoly, SelectedAreaWrapper.IsHole);
                foreach (var p in SelectedAreaWrapper.Model.CoordinateList)
                    restorePoly.Points.Add(new GeoPoint(p.Latitude, p.Longitude));

                // ★ 취소 시에도 원래 자리에 복구
                if (_editingItemIndex >= 0 && _editingItemIndex <= mapVM.PreINITMissionPolygonList.Count)
                {
                    mapVM.PreINITMissionPolygonList.Insert(_editingItemIndex, restorePoly);
                }
                else
                {
                    mapVM.PreINITMissionPolygonList.Add(restorePoly);
                }
                _editingItemIndex = -1;

                // 3. 상태 종료
                PolygonState = MissionEditState.None;
                UpdatePolygonControlState();
                mapVM.IsTempINITMissionPolygonEditMode = false;
                mapVM.CurrentEditLayer = ViewModel_Unit_Map.EditLayerType.None;
                _backupPoints.Clear();
            }
        }

        private void SaveEditedPolygon()
        {
            try { System.IO.File.AppendAllText("debug_mission.txt", $"[DEBUG] SaveEditedPolygon Called. State={_state}\n"); } catch { }
            var mapVM = ViewModel_Unit_Map.SingletonInstance;

            // 1. 모델 업데이트
            UpdateModelFromTempPoints();

            // 2. 복귀 객체 생성
            int finalMissionID = (_state == MenuButtonState.Editing)
                                 ? (int)SelectedinputMissionItem.InputMissionID
                                 : SelectedAreaWrapper.UI_ID;

            try { System.IO.File.AppendAllText("debug_mission.txt", $"[DEBUG] SaveEditedPolygon: State={_state}, FinalMissionID={finalMissionID}\n"); } catch { }

            var finalPoly = new CustomMapPolygon { MissionID = finalMissionID };

            // [★핵심 수정★] Wrapper의 UI_ID(순서 번호)를 맵 객체의 PolygonIndex에 반드시 다시 넣어줘야 함!
            // 그래야 다음에 다시 수정할 때 위 A단계 로직에서 찾을 수 있음.
            finalPoly.PolygonIndex = SelectedAreaWrapper.UI_ID;

            // -----------------------------------------------------------------------------------
            // [★추가★] 텍스트(ID) 표시를 위한 템플릿 설정 (InitMissionSet 로직과 동일하게 적용)
            // -----------------------------------------------------------------------------------

            // 1) 템플릿 생성 (InitMissionSet에 있는 것과 동일)
            DataTemplate titleTemplate = XamlHelper.GetTemplate(
                "<Grid>" +
                "   <TextBlock Text=\"{Binding Path=Text}\" " +
                "              FontFamily=\"Malgun Gothic\" " +
                "              FontSize=\"12\" " +
                "              FontWeight=\"ExtraBold\" " +
                "              Foreground=\"Black\" " +
                "              HorizontalAlignment=\"Center\" " +
                "              VerticalAlignment=\"Center\">" +
                "   </TextBlock>" +
                "</Grid>");

            // 2) TitleOptions 설정
            finalPoly.TitleOptions = new ShapeTitleOptions();
            // 실제 임무 ID를 표시할지, UI_ID를 표시할지 결정 (보통은 실제 임무 ID)
            finalPoly.TitleOptions.Pattern = finalMissionID.ToString();
            finalPoly.TitleOptions.Template = titleTemplate;
            // -----------------------------------------------------------------------------------

            SetPolygonStyle(finalPoly, SelectedAreaWrapper.IsHole);

            foreach (var p in SelectedAreaWrapper.Model.CoordinateList)
                finalPoly.Points.Add(new GeoPoint(p.Latitude, p.Longitude));

            // 3. 돌아갈 리스트 결정
            IList targetList = null;
            if (_state == MenuButtonState.Creating) targetList = mapVM.PreINITMissionPolygonList;
            else if (_state == MenuButtonState.Editing) targetList = mapVM.INITMissionPolygonList;

            // 4. 원래 위치에 복구
            if (targetList != null)
            {
                // 인덱스 범위 체크 후 삽입
                if (_editingItemIndex >= 0 && _editingItemIndex <= targetList.Count)
                    targetList.Insert(_editingItemIndex, finalPoly);
                else
                    targetList.Add(finalPoly);
            }

            // 5. 정리
            mapVM.TempINITMissionPolygonList.Clear();
            PolygonState = MissionEditState.None;
            UpdatePolygonControlState();

            mapVM.IsTempINITMissionPolygonEditMode = false;
            mapVM.CurrentEditLayer = ViewModel_Unit_Map.EditLayerType.None;
            _backupPoints.Clear();
        }

        private void SetPolygonStyle(CustomMapPolygon poly, bool isHole)
        {
            if (isHole)
            {
                // 제외 구역 스타일
                var originalHatchBrush = (DrawingBrush)Application.Current.FindResource("HatchBrushRed");
                var clonedBrush = originalHatchBrush.Clone();
                clonedBrush.Opacity = 0.6;
                poly.Fill = clonedBrush;
                poly.Stroke = Brushes.Red;
                poly.StrokeStyle = new StrokeStyle { Thickness = 2, DashArray = new DoubleCollection { 8, 4 } };
            }
            else
            {
                // 포함 구역 스타일
                var c = Colors.MediumPurple; c.A = 64;
                poly.Fill = new SolidColorBrush(c);
                poly.Stroke = new SolidColorBrush(Color.FromRgb(75, 0, 130));
                poly.StrokeStyle = new StrokeStyle { Thickness = 1 };
            }
        }

        /// <summary>
        /// [수정 모드 진입 시] Wrapper의 데이터를 Temp(그리드용)와 Backup(취소용)으로 복사
        /// </summary>
        private void FillTempPointsFromWrapper(InitMissionAreaWrapper wrapper)
        {
            // 1. 리스트 초기화
            TempPolygonPoints.Clear();
            _backupPoints.Clear();

            if (wrapper == null || wrapper.Model == null || wrapper.Model.CoordinateList == null)
                return;

            // 2. 모델의 좌표를 순회하며 Deep Copy (값 복사)
            foreach (var p in wrapper.Model.CoordinateList)
            {
                // A. UI 그리드 바인딩용 (화면에 보여줄 리스트)
                TempPolygonPoints.Add(new CoordinateInfo
                {
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Altitude = p.Altitude
                });

                // B. 취소 시 복구용 백업 리스트
                _backupPoints.Add(new CoordinateInfo
                {
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Altitude = p.Altitude
                });
            }

            // 3. 기타 속성(Hole 여부, 고도 등)도 컨트롤에 반영
            // 콤보박스: 0="제외(Hole)", 1="포함" 이라고 가정했을 때
            IsHoleIndex = wrapper.Model.IsHole ? 0 : 1;

            // (선택 사항) 고도 컨트롤에 첫 번째 점의 고도를 대표값으로 보여줄지 여부
            if (wrapper.Model.CoordinateList.Count > 0)
            {
                PolygonAltControl = wrapper.Model.CoordinateList[0].Altitude;
            }
        }

        /// <summary>
        /// [저장 버튼 클릭 시] 수정된 Temp 데이터를 원본 Model에 반영
        /// </summary>
        private void UpdateModelFromTempPoints()
        {
            if (SelectedAreaWrapper == null || SelectedAreaWrapper.Model == null) return;

            var model = SelectedAreaWrapper.Model;

            // 1. 기존 좌표 리스트 클리어
            model.CoordinateList.Clear();

            // 2. TempPolygonPoints(현재 수정된 값)를 Model에 주입
            foreach (var item in TempPolygonPoints)
            {
                model.CoordinateList.Add(new CoordinateInfo
                {
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    Altitude = item.Altitude
                });
            }

            // 3. 메타 데이터 갱신
            model.CoordinateListN = (uint)model.CoordinateList.Count;

            // 콤보박스 인덱스에 따라 Hole 여부 갱신 (0: 제외, 1: 포함)
            model.IsHole = (IsHoleIndex == 0);
        }

        /// <summary>
        /// [취소 버튼 클릭 시] 백업해둔 데이터로 Model과 UI를 원상복구
        /// </summary>
        private void RestoreModelFromBackup()
        {
            if (SelectedAreaWrapper == null || SelectedAreaWrapper.Model == null) return;

            var model = SelectedAreaWrapper.Model;

            // 1. Model 데이터 복구
            model.CoordinateList.Clear();
            foreach (var p in _backupPoints)
            {
                model.CoordinateList.Add(new CoordinateInfo
                {
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Altitude = p.Altitude
                });
            }
            model.CoordinateListN = (uint)model.CoordinateList.Count;

            // (Model의 IsHole 등 다른 속성도 백업했다면 여기서 복구해야 함. 
            // 현재 코드상 백업은 좌표만 했으므로, IsHole 등은 수정 전 상태가 유지된다고 가정하거나 별도 변수로 백업 필요)

            // 2. UI(Grid) 데이터 복구 (선택 사항: 어차피 모드가 끝나서 그리드가 비활성되겠지만, 깔끔하게 처리)
            TempPolygonPoints.Clear();
            foreach (var p in _backupPoints)
            {
                TempPolygonPoints.Add(new CoordinateInfo
                {
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Altitude = p.Altitude
                });
            }
        }

        public void InitMissionSet(InputMission Input)
        {
            try { System.IO.File.AppendAllText("debug_mission.txt", $"[DEBUG] InitMissionSet Called. ID={Input.InputMissionID}\n"); } catch { }
            //InputMission.InputMissionType = (uint)MissionTypeIndex;
            //InputMission.IsDone = IsDoneIndex == 1 ? false : true;
            //InputMission.IsDisplay = true;
            var mapVM = ViewModel_Unit_Map.SingletonInstance;

            switch (Input.ShapeType)
            {
                case 1:
                    {
                        // 기존 점 제거 (중복 방지)
                        var pointToRemove = mapVM.INITMissionPointList
                            .FirstOrDefault(p => p.MissionID == (int)Input.InputMissionID);
                        if (pointToRemove != null) mapVM.INITMissionPointList.Remove(pointToRemove);

                        var InputPoint = new CustomMapPoint();
                        InputPoint.MissionID = (int)Input.InputMissionID;
                        InputPoint.TagString = Input.InputMissionID.ToString();
                        InputPoint.Latitude = (float)Input.Coordinate.Latitude;
                        InputPoint.Longitude = (float)Input.Coordinate.Longitude;
                        CommonEvent.OnINITMissionPointAdd?.Invoke(InputPoint);
                        break;
                    }

                case 2:
                    {
                        // 0. 기존에 이 임무로 그려진 선/회랑/라벨 제거 (중복 방지)
                        var lineToRemove = mapVM.INITMissionLineList.FirstOrDefault(l => l.MissionId == (int)Input.InputMissionID);
                        if (lineToRemove != null) mapVM.INITMissionLineList.Remove(lineToRemove);

                        var linePolygonsToRemove = mapVM.INITMissionLinePolygonList.Where(p => p.MissionID == (int)Input.InputMissionID).ToList();
                        foreach (var p in linePolygonsToRemove) mapVM.INITMissionLinePolygonList.Remove(p);

                        var labelToRemove = mapVM.INITMissionLineLabelList.FirstOrDefault(p => p.MissionID == (int)Input.InputMissionID);
                        if (labelToRemove != null) mapVM.INITMissionLineLabelList.Remove(labelToRemove);

                        // 1. 중심선 객체 생성 (데이터 모델로부터)
                        var drawingLine = new CustomMapLine { MissionId = (int)Input.InputMissionID };

                        foreach (var p in Input.PolyLine.CoordinateList)
                        {
                            drawingLine.Points.Add(new GeoPoint(p.Latitude, p.Longitude));
                        }
                        // 스타일 지정 (선택적)
                        drawingLine.Stroke = Brushes.Black;
                        drawingLine.StrokeStyle = new StrokeStyle { Thickness = 1, DashArray = new System.Windows.Media.DoubleCollection { 3, 3 } };

                        // 2. ★ [수정] 라벨(Label) 별도 생성
                        if (drawingLine.Points.Count > 0)
                        {
                            // 중간 지점 계산
                            int midIndex = drawingLine.Points.Count / 2;
                            var anchor = drawingLine.Points[midIndex];

                            // 라벨용 CustomMapPoint 생성
                            var labelPoint = new CustomMapPoint();
                            labelPoint.MissionID = (int)Input.InputMissionID; // ID 연결
                            labelPoint.Latitude = anchor.GetY();
                            labelPoint.Longitude = anchor.GetX();
                            labelPoint.TagString = Input.InputMissionID.ToString(); // 텍스트

                            // 필요하다면 Template 설정 (ViewModel_Unit_Map 쪽 XAML에서 일괄 적용 추천)
                            // labelPoint.TitleTemplate = titleTemplate; 

                            // ★ [핵심] 포인트 추가 이벤트(OnINITMissionPointAdd)가 아니라, 
                            // 새로 만든 라벨 전용 이벤트(OnINITMissionLineLabelAdd)를 호출
                            CommonEvent.OnINITMissionLineLabelAdd?.Invoke(labelPoint);
                        }

                        // 2. 폭 다각형 객체들 '즉석에서 계산'
                        var polygonsToSend = new List<CustomMapPolygon>();
                        int half = (int)Input.PolyLine.Width / 2;

                        if (drawingLine.Points.Count >= 2)
                        {
                            for (int i = 0; i < drawingLine.Points.Count - 1; i++)
                            {
                                var A = (GeoPoint)drawingLine.Points[i];
                                var B = (GeoPoint)drawingLine.Points[i + 1];

                                // GeometryHelper를 사용해 꼭짓점 계산
                                var (nx, ny) = CustomGeometryHelper.UnitPerp(A, B);
                                var A1 = CustomGeometryHelper.Offset(A, nx, ny, half);
                                var A2 = CustomGeometryHelper.Offset(A, -nx, -ny, half);
                                var B1 = CustomGeometryHelper.Offset(B, nx, ny, half);
                                var B2 = CustomGeometryHelper.Offset(B, -nx, -ny, half);

                                var corridorSegment = new CustomMapPolygon { MissionID = (int)Input.InputMissionID };
                                corridorSegment.Points.AddRange(new[] { A1, B1, B2, A2 });
                                polygonsToSend.Add(corridorSegment);
                            }
                        }

                        // 3. 계산된 객체들을 이벤트로 전달
                        CommonEvent.OnINITMissionPolyLineAdd?.Invoke(drawingLine);
                        CommonEvent.OnINITMissionLinePolygonAdd?.Invoke(polygonsToSend);
                        break;
                    }

                case 3:
                    {
                        var DrawPolygonList = new List<CustomMapPolygon>();

                        if (Input.Polygons != null)
                        {
                            if (Input.Polygons.AreaList != null)
                            {
                                if (Input.Polygons.AreaList.Count > 0)
                                {
                                    // 1. ★ 기존에 이 임무(MissionID)로 그려진 폴리곤들을 Global List에서 찾아서 제거
                                    // (수정 저장 시 중복 생성 방지)
                                    var itemsToRemove = mapVM.INITMissionPolygonList
                                                             .Where(x => x.MissionID == (int)Input.InputMissionID)
                                                             .ToList();

                                    foreach (var item in itemsToRemove)
                                    {
                                        try { System.IO.File.AppendAllText("debug_mission.txt", $"[DEBUG] InitMissionSet: Removing existing polygon ID={item.MissionID}\n"); } catch { }
                                        mapVM.INITMissionPolygonList.Remove(item);
                                    }

                                    foreach (var item in Input.Polygons.AreaList)
                                    {
                                        var DrawPolygon = new CustomMapPolygon();
                                        DrawPolygon.MissionID = (int)Input.InputMissionID;
                                        int polygonindex = 0;

                                        DataTemplate titleTemplate = XamlHelper.GetTemplate(
                                        "<Grid>" +
                                        "   <TextBlock Text=\"{Binding Path=Text}\" " +
                                        "              FontFamily=\"Malgun Gothic\" " +
                                        "              FontSize=\"12\" " +
                                        "              FontWeight=\"ExtraBold\" " + // 굵기 강화
                                        "              Foreground=\"Black\" " +
                                        "              HorizontalAlignment=\"Center\" " +
                                        "              VerticalAlignment=\"Center\">" +
                                        //"       <TextBlock.Effect>" +
                                        //"           <DropShadowEffect BlurRadius=\"2\" ShadowDepth=\"0\" Color=\"White\" Opacity=\"1\"/>" + // [팁] 흰색 테두리(Halo) 효과 추가로 가독성 극대화
                                        //"       </TextBlock.Effect>" +
                                        "   </TextBlock>" +
                                        "</Grid>");


                                        DrawPolygon.TitleOptions = new ShapeTitleOptions();
                                        DrawPolygon.TitleOptions.Pattern = Input.InputMissionID.ToString();
                                        DrawPolygon.TitleOptions.Template = titleTemplate;


                                        if (item.IsHole)
                                        {
                                            // [제외 영역 스타일]
                                            // 채우기: XAML에 정의한 빗금 무늬 브러시 적용
                                            // 1. App.xaml 또는 UserControl.Resources에서 원본 브러시를 찾는다.
                                            var originalHatchBrush = (DrawingBrush)Application.Current.FindResource("HatchBrushRed");

                                            // 2. Clone() 메서드로 수정 가능한 '복사본'을 만든다.
                                            var clonedBrush = originalHatchBrush.Clone();

                                            // 3. 이제 에러 없이 복사본의 속성을 수정할 수 있다.
                                            clonedBrush.Opacity = 0.6;

                                            // 4. 수정된 복사본을 Fill 속성에 할당한다.
                                            DrawPolygon.Fill = clonedBrush;



                                            // 테두리: 굵은 붉은색 점선
                                            DrawPolygon.Stroke = Brushes.MidnightBlue;
                                            DrawPolygon.StrokeStyle = new StrokeStyle { Thickness = 2, DashArray = new DoubleCollection { 8, 4 } };
                                        }
                                        else
                                        {
                                            // [일반 영역 스타일]
                                            //DrawPolygon.Fill = Brushes.SkyBlue;
                                            //DrawPolygon.Stroke = Brushes.Navy;
                                            //DrawPolygon.StrokeStyle = new StrokeStyle { Thickness = 1 };

                                            //var fillColor = Colors.MediumPurple;

                                            // Fill: MediumPurple (25% 투명도)
                                            var missionColor = Colors.MediumPurple; // #9370DB
                                            missionColor.A = 64;
                                            DrawPolygon.Fill = new SolidColorBrush(missionColor);
                                            //DrawPolygon.Fill = new SolidColorBrush(fillColor) { Opacity = 0.4 };

                                            //DrawPolygon.Stroke = Brushes.MidnightBlue;
                                            // Stroke: 진한 보라색 (가시성 확보)
                                            DrawPolygon.Stroke = new SolidColorBrush(Color.FromRgb(75, 0, 130)); // Indigo
                                            DrawPolygon.StrokeStyle = new StrokeStyle { Thickness = 1 };
                                        }

                                        foreach (var inputItem in item.CoordinateList)
                                        {
                                            var InputItem = new CoordinateInfo();
                                            InputItem.Latitude = inputItem.Latitude;
                                            InputItem.Longitude = inputItem.Longitude;
                                            //InputItem.Altitude = inputItem.Altitude;
                                            InputItem.Altitude = inputItem.Altitude;

                                            var Item = new GeoPoint(InputItem.Latitude, InputItem.Longitude);
                                            //DrawPolygon.PolygonCoordItems.CoordIndex = (int)InputArea.CoordinateListN;
                                            DrawPolygon.Points.Add(Item);

                                        }
                                        DrawPolygon.PolygonIndex = polygonindex;
                                        //DrawPolygon.IsShow = true;
                                        DrawPolygonList.Add(DrawPolygon);
                                        polygonindex++;
                                    }
                                }
                            }
                        }
                        CommonEvent.OnINITMissionPolygonAdd?.Invoke(DrawPolygonList);
                        break;
                    }

                default:
                    break;
            }
            // 임시로 사용했던 데이터 초기화 (신규 생성 시에만 데이터가 있었으므로 null 체크)
            FinalSegmentRectangles?.Clear();
            //AreaList?.Clear();
        }

        /// <summary>
        /// 그리드용 + 모델용 양쪽 컬렉션에서 전체 임무 목록을 합쳐 반환 (중복 제거).
        /// </summary>
        private List<InputMission> GetAllMissions(InputMission excludeMission = null)
        {
            var packageList = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario
                ?.InitScenario?.InputMissionPackage?.InputMissionList;

            var all = new HashSet<InputMission>(inputMissionList);
            if (packageList != null)
            {
                foreach (var m in packageList)
                    all.Add(m);
            }

            if (excludeMission != null)
                all.Remove(excludeMission);

            return all.ToList();
        }

        /// <summary>
        /// [생성용] 순번 충돌 처리: desiredOrder가 이미 존재하면 다이얼로그를 띄우고,
        /// 예 선택 시 해당 순번 이상을 +1씩 밀어냄.
        /// </summary>
        private bool HandleSequenceNumberConflict(uint desiredOrder, InputMission excludeMission)
        {
            var allMissions = GetAllMissions(excludeMission);

            bool hasDuplicate = allMissions.Any(m => m.SequenceNumber == desiredOrder);
            if (!hasDuplicate) return true;

            bool? result = Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new View_PopUp_Dialog();
                dialog.Description.Text = "순번 중복";
                dialog.Reason.Text = $"순번 {desiredOrder}이(가) 이미 존재합니다.\n기존 순번을 밀어내고 삽입하시겠습니까?";
                return dialog.ShowDialog();
            });

            if (result != true) return false;

            var toShift = allMissions
                .Where(m => m.SequenceNumber >= desiredOrder)
                .OrderByDescending(m => m.SequenceNumber)
                .ToList();
            foreach (var m in toShift)
                m.SequenceNumber += 1;

            return true;
        }

        /// <summary>
        /// [수정용] 순번 재정렬: 기존 위치에서 새 위치로 이동하면서
        /// 사이 항목들의 순번을 자동 조정.
        /// 예: 0,1,2,3에서 3→1이면 기존 1,2가 2,3으로 밀림.
        /// 예: 0,1,2,3에서 1→3이면 기존 2,3이 1,2로 당겨짐.
        /// </summary>
        private bool HandleSequenceNumberReorder(uint desiredOrder, InputMission editingMission)
        {
            uint oldOrder = editingMission.SequenceNumber;
            if (oldOrder == desiredOrder) return true;

            var allMissions = GetAllMissions(editingMission);

            bool hasDuplicate = allMissions.Any(m => m.SequenceNumber == desiredOrder);

            if (hasDuplicate)
            {
                bool? result = Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new View_PopUp_Dialog();
                    dialog.Description.Text = "순번 변경";
                    dialog.Reason.Text = $"순번을 {oldOrder}에서 {desiredOrder}(으)로\n변경하시겠습니까? 기존 순번이 조정됩니다.";
                    return dialog.ShowDialog();
                });

                if (result != true) return false;
            }

            if (desiredOrder < oldOrder)
            {
                // 위로 이동 (3→1): [desiredOrder, oldOrder) 범위를 +1씩 밀어냄
                var toShift = allMissions
                    .Where(m => m.SequenceNumber >= desiredOrder && m.SequenceNumber < oldOrder)
                    .OrderByDescending(m => m.SequenceNumber)
                    .ToList();
                foreach (var m in toShift)
                    m.SequenceNumber += 1;
            }
            else
            {
                // 아래로 이동 (1→3): (oldOrder, desiredOrder] 범위를 -1씩 당김
                var toShift = allMissions
                    .Where(m => m.SequenceNumber > oldOrder && m.SequenceNumber <= desiredOrder)
                    .OrderBy(m => m.SequenceNumber)
                    .ToList();
                foreach (var m in toShift)
                    m.SequenceNumber -= 1;
            }

            return true;
        }

        /// <summary>
        /// 현재 임무 목록(inputMissionList)을 검사하여
        /// 0부터 시작하는 순서 중 비어있는 가장 작은 번호를 반환합니다.
        /// </summary>
        private uint GetNextAvailableSequenceNumber()
        {
            // 1. 리스트가 비어있으면 0번 리턴
            if (inputMissionList == null || inputMissionList.Count == 0)
                return 0;

            // 2. 현재 존재하는 번호들을 오름차순으로 정렬해서 가져옴
            var existingNumbers = inputMissionList
                                    .Select(m => m.SequenceNumber)
                                    .OrderBy(n => n)
                                    .Distinct() // 혹시 모를 중복 제거
                                    .ToList();

            // 3. 0부터 차례대로 비교하며 빈 구멍 찾기
            uint expectedNum = 0;
            foreach (var num in existingNumbers)
            {
                if (num != expectedNum)
                {
                    // 예: 0, 1, 3 이면 -> expected는 2인데 num은 3임 -> 2가 비었음!
                    return expectedNum;
                }
                expectedNum++;
            }

            // 4. 구멍이 없으면 맨 마지막 번호 + 1 리턴
            return expectedNum;
        }

        /// <summary>
        /// 현재 임무 목록을 검사하여 70,000,000부터 시작하는 ID 중 
        /// 비어있는 가장 작은 번호를 반환합니다.
        /// </summary>
        private uint GetNextAvailableMissionID()
        {
            // 시작 번호 설정 (기존 코드의 기본값 참고)
            const uint BASE_ID = 70000000;

            // 1. 리스트가 비어있으면 시작 번호 리턴
            if (inputMissionList == null || inputMissionList.Count == 0)
                return BASE_ID;

            // 2. 현재 존재하는 ID들을 오름차순으로 정렬해서 가져옴
            var existingIDs = inputMissionList
                                .Select(m => m.InputMissionID)
                                .Where(id => id >= BASE_ID) // 혹시 모를 이상한 값 필터링
                                .OrderBy(id => id)
                                .Distinct()
                                .ToList();

            // 3. BASE_ID부터 차례대로 비교하며 빈 구멍 찾기
            uint expectedID = BASE_ID;
            foreach (var id in existingIDs)
            {
                if (id == expectedID)
                {
                    // ID가 존재하면 다음 번호를 기대함
                    expectedID++;
                }
                else if (id > expectedID)
                {
                    // 구멍 발견! (예: 70...00, 70...02 이면 expected는 01인데 id가 02임)
                    return expectedID;
                }
            }

            // 4. 구멍이 없으면 맨 마지막 번호 + 1 리턴
            return expectedID;
        }

        public void RefreshButtonStateBySimulation(bool isPlaying)
        {
            if (isPlaying)
            {
                // 모의 중일 때는 모든 버튼과 편집 기능 강제 비활성화
                Button1Enable = false;
                Button2Enable = false;
                Button3Enable = false;
                PolygonButton1Enable = false;
                PolygonButton2Enable = false;
                PolygonButton3Enable = false;
                EditEnable = false;
                PolygonEditEnable = false;
                IsShapeTypeEditable = false;
            }
            else
            {
                // 모의가 끝나면 현재 상태(_state)를 재평가해서 버튼 상태 원상복구
                UpdateMainControlState();
            }
        }
    }

}
