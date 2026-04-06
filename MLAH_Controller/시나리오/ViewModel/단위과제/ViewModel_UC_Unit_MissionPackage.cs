using DevExpress.Xpf.Map;
using System.Collections.ObjectModel;

namespace MLAH_Controller
{
    
    public partial class ViewModel_UC_Unit_MissionPackage : CommonBase
    {
        #region Singleton
        static ViewModel_UC_Unit_MissionPackage _ViewModel_UC_Unit_MissionPackage = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_UC_Unit_MissionPackage SingletonInstance
        {
            get
            {
                if (_ViewModel_UC_Unit_MissionPackage == null)
                {
                    _ViewModel_UC_Unit_MissionPackage = new ViewModel_UC_Unit_MissionPackage();
                }
                return _ViewModel_UC_Unit_MissionPackage;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_UC_Unit_MissionPackage()
        {
            CommonEvent.OnTakeOverPointSet += Callback_OnTakeOverPointSet;
            CommonEvent.OnHandOverPointSet += Callback_OnHandOverPointSet;
            CommonEvent.OnRTBPointSet += Callback_OnRTBPointSet;

            //CommonEvent.OnINITMissionLinearSet += Callback_OnINITMissionLinearSet;
            CommonEvent.OnFlightAreaPolygonSet += Callback_OnFlightAreaPolygonSet;
            CommonEvent.OnProhibitedAreaPolygonSet += Callback_OnProhibitedAreaPolygonSet;

            TakeOverButton1Command = new RelayCommand(TakeOverButton1CommandAction);
            TakeOverButton2Command = new RelayCommand(TakeOverButton2CommandAction);
            TakeOverButton3Command = new RelayCommand(TakeOverButton3CommandAction);

            HandOverButton1Command = new RelayCommand(HandOverButton1CommandAction);
            HandOverButton2Command = new RelayCommand(HandOverButton2CommandAction);
            HandOverButton3Command = new RelayCommand(HandOverButton3CommandAction);

            RTBButton1Command = new RelayCommand(RTBButton1CommandAction);
            RTBButton2Command = new RelayCommand(RTBButton2CommandAction);
            RTBButton3Command = new RelayCommand(RTBButton3CommandAction);

            FlightAreaButton1Command = new RelayCommand(FlightAreaButton1CommandAction);
            FlightAreaButton2Command = new RelayCommand(FlightAreaButton2CommandAction);
            FlightAreaButton3Command = new RelayCommand(FlightAreaButton3CommandAction);

            ProhibitedAreaButton1Command = new RelayCommand(ProhibitedAreaButton1CommandAction);
            ProhibitedAreaButton2Command = new RelayCommand(ProhibitedAreaButton2CommandAction);
            ProhibitedAreaButton3Command = new RelayCommand(ProhibitedAreaButton3CommandAction);

            CommonEvent.OnFlightAreaPolygonUpdated += Callback_OnFlightAreaPolygonUpdated;
            CommonEvent.OnProhibitedAreaPolygonUpdated += Callback_OnProhibitedAreaPolygonUpdated;
        }

        #endregion 생성자 & 콜백

        // [1] ID 카운터 관리 메서드 추가
        public void ResetFlightAreaCounter() => _flightAreaIdCounter = 0;
        public void SetFlightAreaCounter(int nextId) => _flightAreaIdCounter = nextId;

        public void ResetProhibitedAreaCounter() => _prohibitedAreaIdCounter = 0; // 변수 선언 필요
        public void SetProhibitedAreaCounter(int nextId) => _prohibitedAreaIdCounter = nextId; // 변수 선언 필요

        /// <summary>
        /// 특정 모드를 제외하고, 현재 편집/생성 중인 모든 작업을 취소(초기화)합니다.
        /// </summary>
        /// <param name="excludeMode">취소하지 말아야 할 모드 (현재 진입하려는 모드)</param>
        private void CancelOtherModes(ActiveMode excludeMode)
        {
            if (excludeMode != ActiveMode.FlightArea && (FlightAreaState == MissionEditState.Creating || FlightAreaState == MissionEditState.Editing))
                FlightAreaButton3CommandAction(null);

            if (excludeMode != ActiveMode.ProhibitedArea && (ProhibitedAreaState == MissionEditState.Creating || ProhibitedAreaState == MissionEditState.Editing))
                ProhibitedAreaButton3CommandAction(null);

            if (excludeMode != ActiveMode.TakeOver && TakeOverState != MissionEditState.None)
                TakeOverButton3CommandAction(null);

            if (excludeMode != ActiveMode.HandOver && HandOverState != MissionEditState.None)
                HandOverButton3CommandAction(null);

            if (excludeMode != ActiveMode.RTB && RTBState != MissionEditState.None)
                RTBButton3CommandAction(null);
        }


        #region 비행 가능구역 금지구역 수정

        // 지도 드래그 시 -> 그리드(InnerSource) 실시간 업데이트
        private void Callback_OnFlightAreaPolygonUpdated(List<GeoPoint> points)
        {
            // 편집 모드가 아니면 무시
            if (FlightAreaState != MissionEditState.Editing) return;

            FlightAreaInnterItemSource.Clear();
            foreach (var p in points)
            {
                FlightAreaInnterItemSource.Add(new AreaLatLonInfo
                {
                    Latitude = (float)p.Latitude,
                    Longitude = (float)p.Longitude
                });
            }
        }

        private void Callback_OnProhibitedAreaPolygonUpdated(List<GeoPoint> points)
        {
            // 편집 모드가 아니면 무시
            if (ProhibitedAreaState != MissionEditState.Editing) return;

            ProhibitedAreaInnterItemSource.Clear();
            foreach (var p in points)
            {
                ProhibitedAreaInnterItemSource.Add(new AreaLatLonInfo
                {
                    Latitude = (float)p.Latitude,
                    Longitude = (float)p.Longitude
                });
            }
        }

        #endregion 비행 가능구역 금지구역 수정

        #region [신규] 맵 오버레이 정보 패널 속성

        private bool _IsInfoPanelVisible = false;
        public bool IsInfoPanelVisible
        {
            get => _IsInfoPanelVisible;
            set { _IsInfoPanelVisible = value; OnPropertyChanged("IsInfoPanelVisible"); }
        }

        private string _CurrentModeTitle = "";
        public string CurrentModeTitle
        {
            get => _CurrentModeTitle;
            set { _CurrentModeTitle = value; OnPropertyChanged("CurrentModeTitle"); }
        }

        private string _CurrentShortcutKey = "";
        public string CurrentShortcutKey
        {
            get => _CurrentShortcutKey;
            set { _CurrentShortcutKey = value; OnPropertyChanged("CurrentShortcutKey"); }
        }

        /// <summary>
        /// 현재 활성화된 모드를 확인하여 패널 정보를 갱신합니다.
        /// </summary>
        private void UpdateInfoPanelState()
        {
            if (TakeOverState != MissionEditState.None)
            {
                IsInfoPanelVisible = true;
                CurrentModeTitle = "통제권 획득 지역 설정";
                CurrentShortcutKey = "Z"; // 아까 정한 키
            }
            else if (HandOverState != MissionEditState.None)
            {
                IsInfoPanelVisible = true;
                CurrentModeTitle = "통제권 인계 지역 설정";
                CurrentShortcutKey = "X";
            }
            else if (RTBState != MissionEditState.None)
            {
                IsInfoPanelVisible = true;
                CurrentModeTitle = "무인기 귀환 지역 설정";
                CurrentShortcutKey = "C";
            }
            else if (FlightAreaState != MissionEditState.None)
            {
                IsInfoPanelVisible = true;

                if (FlightAreaState == MissionEditState.Creating)
                {
                    CurrentModeTitle = "비행 가능 구역 생성 (점 추가)";
                    CurrentShortcutKey = "V"; // 생성 모드일 때만 단축키 표시
                }
                else if (FlightAreaState == MissionEditState.Editing)
                {
                    // [수정] 편집 모드일 때 안내 문구 변경
                    CurrentModeTitle = "비행 가능 구역 수정 (점을 드래그하세요)";
                    CurrentShortcutKey = ""; // 드래그는 단축키가 없으므로 숨김
                }
            }
            else if (ProhibitedAreaState != MissionEditState.None)
            {
                IsInfoPanelVisible = true;
                CurrentModeTitle = "비행 금지 구역 설정";
                CurrentShortcutKey = "B";
            }
            else
            {
                // 아무 모드도 아니면 숨김
                IsInfoPanelVisible = false;
                CurrentModeTitle = "";
                CurrentShortcutKey = "";
            }
        }
        #endregion

        #region [버튼 상태 관리] 점(Point) 그룹

        // 1. 통제권 획득 (TakeOver) 버튼 상태 갱신
        private void UpdateTakeOverButtonState()
        {
            // [1] 모의 실행 중이면 강제 잠금
            bool isSimPlaying = ViewModel_ScenarioView.SingletonInstance.IsSimPlaying;
            if (isSimPlaying)
            {
                TakeOverButton1Enable = false; // 생성
                TakeOverButton2Enable = false; // 수정
                TakeOverButton3Enable = false; // 삭제
                return;
            }

            // [2] 편집 모드(Creating/Editing) 상태일 때
            if (TakeOverState == MissionEditState.Creating || TakeOverState == MissionEditState.Editing)
            {
                TakeOverButton1Enable = true;  // 저장(생성) 버튼 활성
                TakeOverButton2Enable = false; // 수정 버튼 비활성 (이미 작업 중)
                TakeOverButton3Enable = true;  // 취소 버튼 활성
            }
            // [3] 평상시 (None) 상태일 때
            else
            {
                TakeOverButton1Enable = true; // 생성 버튼은 항상 활성

                // ★ 핵심: 선택된 항목(Row)이 있을 때만 수정/삭제 활성화
                bool hasSelection = TakeOverSelectedItem != null;
                TakeOverButton2Enable = hasSelection;
                TakeOverButton3Enable = hasSelection;
            }
        }

        // 2. 통제권 인계 (HandOver) 버튼 상태 갱신
        private void UpdateHandOverButtonState()
        {
            bool isSimPlaying = ViewModel_ScenarioView.SingletonInstance.IsSimPlaying;
            if (isSimPlaying)
            {
                HandOverButton1Enable = false;
                HandOverButton2Enable = false;
                HandOverButton3Enable = false;
                return;
            }

            if (HandOverState == MissionEditState.Creating || HandOverState == MissionEditState.Editing)
            {
                HandOverButton1Enable = true;
                HandOverButton2Enable = false;
                HandOverButton3Enable = true;
            }
            else
            {
                HandOverButton1Enable = true;

                bool hasSelection = HandOverSelectedItem != null;
                HandOverButton2Enable = hasSelection;
                HandOverButton3Enable = hasSelection;
            }
        }

        // 3. 귀환 (RTB) 버튼 상태 갱신
        private void UpdateRTBButtonState()
        {
            bool isSimPlaying = ViewModel_ScenarioView.SingletonInstance.IsSimPlaying;
            if (isSimPlaying)
            {
                RTBButton1Enable = false;
                RTBButton2Enable = false;
                RTBButton3Enable = false;
                return;
            }

            if (RTBState == MissionEditState.Creating || RTBState == MissionEditState.Editing)
            {
                RTBButton1Enable = true;
                RTBButton2Enable = false;
                RTBButton3Enable = true;
            }
            else
            {
                RTBButton1Enable = true;

                bool hasSelection = RTBSelectedItem != null;
                RTBButton2Enable = hasSelection;
                RTBButton3Enable = hasSelection;
            }
        }

        #endregion
        private void UpdateFlightAreaButtonState()
        {
            bool isSimPlaying = ViewModel_ScenarioView.SingletonInstance.IsSimPlaying;
            // 1. [최우선] 모의 실행 중이면? -> 전부 잠금
            if (isSimPlaying)
            {
                FlightAreaButton1Enable = false; // 생성/저장
                FlightAreaButton2Enable = false; // 수정
                FlightAreaButton3Enable = false; // 삭제/취소
                return;
            }

            // 2. 편집 모드(생성 중 or 수정 중) 상태일 때
            if (FlightAreaState == MissionEditState.Creating || FlightAreaState == MissionEditState.Editing)
            {
                // 생성/수정 중일 때는 '저장(Btn1)'과 '취소(Btn3)'만 활성화
                FlightAreaButton1Enable = true;  // 저장
                FlightAreaButton2Enable = false; // 수정 (이미 하는 중이니 잠금)
                FlightAreaButton3Enable = true;  // 취소
            }
            // 3. 평상시 (None 상태)
            else
            {
                FlightAreaButton1Enable = true; // 생성 버튼은 항상 활성화 (새로 만들기)

                // 수정/삭제 버튼은 '선택된 항목'이 있을 때만 활성화
                bool hasSelection = FlightAreaSelectedItem != null;
                FlightAreaButton2Enable = hasSelection;
                FlightAreaButton3Enable = hasSelection;
            }
        }
        private void UpdateProhibitedAreaButtonState()
        {
            bool isSimPlaying = ViewModel_ScenarioView.SingletonInstance.IsSimPlaying;
            // 1. [최우선] 모의 실행 중이면? -> 전부 잠금
            if (isSimPlaying)
            {
                ProhibitedAreaButton1Enable = false; // 생성/저장
                ProhibitedAreaButton2Enable = false; // 수정
                ProhibitedAreaButton3Enable = false; // 삭제/취소
                return;
            }

            // 2. 편집 모드(생성 중 or 수정 중) 상태일 때
            if (ProhibitedAreaState == MissionEditState.Creating || ProhibitedAreaState == MissionEditState.Editing)
            {
                // 생성/수정 중일 때는 '저장(Btn1)'과 '취소(Btn3)'만 활성화
                ProhibitedAreaButton1Enable = true;  // 저장
                ProhibitedAreaButton2Enable = false; // 수정 (이미 하는 중이니 잠금)
                ProhibitedAreaButton3Enable = true;  // 취소
            }
            // 3. 평상시 (None 상태)
            else
            {
                ProhibitedAreaButton1Enable = true; // 생성 버튼은 항상 활성화 (새로 만들기)

                // 수정/삭제 버튼은 '선택된 항목'이 있을 때만 활성화
                bool hasSelection = ProhibitedAreaSelectedItem != null;
                ProhibitedAreaButton2Enable = hasSelection;
                ProhibitedAreaButton3Enable = hasSelection;
            }
        }


        /// <summary>
        /// 카운터를 0으로 초기화합니다. (새 시나리오 / 초기화 시 호출)
        /// </summary>
        public void ResetRTBCounter()
        {
            _rtbIdCounter = 0;
        }

        /// <summary>
        /// 카운터를 특정 값으로 설정합니다. (파일 로드 후 동기화 시 호출)
        /// </summary>
        /// <param name="nextId">다음 생성될 ID 값</param>
        public void SetRTBCounter(int nextId)
        {
            _rtbIdCounter = nextId;
        }


        public void ClearFlightAreaTempData()
        {
            FlightAreaChecked = false; // 토글 끄기
            FlightAreaInnterItemSource.Clear(); // 데이터 비우기
            ViewModel_Unit_Map.SingletonInstance.ClearTempDrawing(); // 지도 임시 객체 삭제
        }

        public void ClearProhibitedAreaTempData()
        {
            ProhibitedAreaChecked = false;
            ProhibitedAreaInnterItemSource.Clear();
            ViewModel_Unit_Map.SingletonInstance.ClearTempDrawing();
        }

        private void Callback_OnTakeOverPointSet(double lat, double lon)
        {
            TakeOverLAT = (float)lat;
            TakeOverLON = (float)lon;
            TakeOverChecked = false;
        }

        public RelayCommand TakeOverButton1Command { get; set; }

        public void TakeOverButton1CommandAction(object param)
        {
            if (TakeOverState == MissionEditState.None)
            {
                // -------------------------------------------------------------
                // [신규 기능] 빈 UAV ID 자동 선택 로직
                // -------------------------------------------------------------
                // 현재 리스트에 등록된 AircraftID들을 가져옵니다.
                var existingIds = TakeOverItemSource.Select(x => x.AircraftID).ToList();

                // 순차적으로 비어있는 ID를 찾아서 콤보박스 인덱스를 설정합니다.
                // ID 4 (UAV#1) -> Index 0
                // ID 5 (UAV#2) -> Index 1
                // ID 6 (UAV#3) -> Index 2

                if (!existingIds.Contains(4))
                {
                    TakeOverUAVIDIndex = 0; // 4번이 없으면 1번 UAV 선택
                }
                else if (!existingIds.Contains(5))
                {
                    TakeOverUAVIDIndex = 1; // 4번은 있고 5번이 없으면 2번 UAV 선택
                }
                else if (!existingIds.Contains(6))
                {
                    TakeOverUAVIDIndex = 2; // 4,5번 있고 6번이 없으면 3번 UAV 선택
                }
                // 4, 5, 6이 다 있으면 기존 선택 유지 (건드리지 않음)
                // -------------------------------------------------------------

                //좌표 데이터 초기화 (0으로 리셋)
                // 이렇게 하면 이전에 찍었던 값이 남아있어서 저장되는 사고를 방지
                TakeOverLAT = 0;
                TakeOverLON = 0;
                TakeOverALT = 1000; // 필요시 기본값(예: 1000)으로

                TakeOverEditEnable = true;
                TakeOverCheckEditEnable = true;
                TakeOverButton2Enable = false;
                TakeOverButton3Enable = true;
                TakeOverButton1Text = "저장";
                TakeOverButton3Text = "취소";
                TakeOverState = MissionEditState.Creating;

                
            }
            else if (TakeOverState == MissionEditState.Creating)
            {
                //좌표 유효성 검사 (0,0 이면 저장 차단)
                // 운용자가 토글을 켰다 껐거나, 아무것도 안 하고 엔터만 쳤을 때를 방어
                if (TakeOverLAT == 0 && TakeOverLON == 0)
                {
                    var pop = new View_PopUp(5);
                    pop.Description.Text = "저장 실패";
                    pop.Reason.Text = "좌표가 설정되지 않았습니다.\n지도에서 위치를 선택하거나 직접 입력하세요.";
                    pop.Show();
                    return; // 저장 중단
                }

                TakeOverEditEnable = false;
                TakeOverCheckEditEnable = false;
                TakeOverButton2Enable = false;
                TakeOverButton3Enable = false;
                TakeOverButton1Text = "생성";
                TakeOverButton3Text = "삭제";
                TakeOverState = MissionEditState.None;

                uint aircraftID = (uint)(TakeOverUAVIDIndex + 4);

                bool alreadyExists = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.TakeOverInfoList.Any(info => info.AircraftID == aircraftID);

                if (alreadyExists)
                {
                    var pop_error = new View_PopUp(5);
                    pop_error.Description.Text = "지점 추가 불가능";
                    pop_error.Reason.Text = "이미 같은 AircraftID가 존재합니다.";
                    pop_error.Show();
                    //CommonEvent.OnTakeOverPointAdd?.Invoke(TakeOverSelectedIndex, TakeOverLAT, TakeOverLON);
                    ViewModel_Unit_Map.SingletonInstance.ClearTempTakeoverPoint();
                    return; // 이미 존재하면 추가하지 않음
                }
                    

                var InputTakeOver = new TakeOverHandOverInfo();
                InputTakeOver.AircraftID = (uint)TakeOverUAVIDIndex + 4;
                InputTakeOver.CoordinateList.Latitude = TakeOverLAT;
                InputTakeOver.CoordinateList.Longitude = TakeOverLON;
                InputTakeOver.CoordinateList.Altitude = TakeOverALT;

                TakeOverItemSource.Add(InputTakeOver);
                var TempIndex = TakeOverItemSource.Count();
                TakeOverSelectedIndex = TempIndex - 1;

                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.TakeOverInfoListN = (uint)TempIndex;
                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.TakeOverInfoList.Add(InputTakeOver);

                //임시 파일 지우기 => 지도 Add
                //이벤트 이름 바꿔야함
                CommonEvent.OnTakeOverPointAdd?.Invoke((int)InputTakeOver.AircraftID, TakeOverLAT,TakeOverLON);

            }
            else if (TakeOverState == MissionEditState.Editing)
            {
                if (TakeOverSelectedItem == null) return;

                // 1. 데이터 모델 업데이트 (참조된 객체 값 변경)
                TakeOverSelectedItem.CoordinateList.Latitude = TakeOverLAT;
                TakeOverSelectedItem.CoordinateList.Longitude = TakeOverLON;
                TakeOverSelectedItem.CoordinateList.Altitude = TakeOverALT;
                // ID는 콤보박스에서 바꿨다면 반영, 아니면 유지
                // TakeOverSelectedItem.AircraftID = (uint)(TakeOverUAVIDIndex + 4); 

                // 2. 지도 업데이트 (지우고 다시 그림)
                // 기존 위치 삭제
                CommonEvent.OnTakeOverPointRemove?.Invoke((int)TakeOverSelectedItem.AircraftID);
                // 새 위치 추가
                CommonEvent.OnTakeOverPointAdd?.Invoke((int)TakeOverSelectedItem.AircraftID, TakeOverLAT, TakeOverLON);

                // 3. 상태 종료
                TakeOverState = MissionEditState.None;
                TakeOverEditEnable = false;
                TakeOverCheckEditEnable = false;
                TakeOverButton1Text = "생성";
                TakeOverButton3Text = "삭제";
                TakeOverChecked = false;
                ViewModel_Unit_Map.SingletonInstance.ClearTempTakeoverPoint();
            }
        }

        public RelayCommand TakeOverButton2Command { get; set; }

        public void TakeOverButton2CommandAction(object param)
        {
            // 1. 선택된 항목이 없으면 리턴 (방어 코드)
            if (TakeOverSelectedItem == null) return;

            // 2. 다른 모드들 끄기 (상호 배제)
            CancelOtherModes(ActiveMode.TakeOver); // 적절한 모드로 설정

            // 3. 현재 값 백업 (취소 대비)
            _backupLat = TakeOverLAT;
            _backupLon = TakeOverLON;
            _backupAlt = TakeOverALT;
            _backupIdIndex = TakeOverUAVIDIndex;

            // 4. UI 상태 변경 (Editing 모드)
            TakeOverState = MissionEditState.Editing;

            TakeOverEditEnable = true;
            TakeOverCheckEditEnable = true; // 지도 찍기 가능하도록

            TakeOverButton1Text = "저장"; // 생성 -> 저장
            TakeOverButton3Text = "취소"; // 삭제 -> 취소

            // 수정 중에는 '수정' 버튼 자체는 비활성 (UpdateTakeOverButtonState에서 처리됨)
            // 지도의 찍기 모드는 끄거나, 혹은 켜서 바로 수정하게 할 수 있음 (여기선 끔)
            TakeOverChecked = false;
        }

        public RelayCommand TakeOverButton3Command { get; set; }

        public void TakeOverButton3CommandAction(object param)
        {
            // ---------------------------------------------------------
            // 1. [삭제 모드] (State가 None일 때, 버튼 텍스트: "삭제")
            // ---------------------------------------------------------
            if (TakeOverState == MissionEditState.None)
            {
                // 방어 코드: 선택된 아이템이 없으면 아무것도 하지 않음
                if (TakeOverSelectedItem == null) return;

                // (1) 지도(Map) 상의 아이콘 제거 요청
                CommonEvent.OnTakeOverPointRemove?.Invoke((int)TakeOverSelectedItem.AircraftID);

                // (2) 실제 데이터 모델(백엔드 데이터)에서 제거
                var targetModelList = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.TakeOverInfoList;
                var itemToRemove = targetModelList.FirstOrDefault(m => m.AircraftID == TakeOverSelectedItem.AircraftID);

                if (itemToRemove != null)
                {
                    targetModelList.Remove(itemToRemove);
                    ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.TakeOverInfoListN = (uint)targetModelList.Count;
                }

                // (3) UI 리스트(DataGrid)에서 제거
                TakeOverItemSource.Remove(TakeOverSelectedItem);

                // (4) 선택 상태 초기화 (삭제 후 아무것도 선택 안 된 상태로)
                TakeOverSelectedIndex = -1;
            }
            // ---------------------------------------------------------
            // 2. [취소 모드] (State가 Creating일 때, 버튼 텍스트: "취소")
            // ---------------------------------------------------------
            else if (TakeOverState == MissionEditState.Creating)
            {
                // (1) UI 컨트롤 비활성화 및 버튼 텍스트 원상복구
                TakeOverEditEnable = false;       // 텍스트박스 잠금
                TakeOverCheckEditEnable = false;  // 체크박스 잠금

                TakeOverButton2Enable = false;    // 수정 버튼 잠금
                TakeOverButton3Enable = false;    // 삭제 버튼 잠금 (나중에 그리드 선택하면 다시 켜짐)

                TakeOverButton1Text = "생성";     // 저장 -> 생성
                TakeOverButton3Text = "삭제";     // 취소 -> 삭제

                // (2) 상태 초기화
                TakeOverState = MissionEditState.None;

                // (3) 지도 상호작용 중단 (중요!)
                TakeOverChecked = false; // 좌표 찍기 토글 해제 (단축키 Z 기능 해제)

                // (4) 지도에 찍혀있던 '임시 빨간 점' 제거
                ViewModel_Unit_Map.SingletonInstance.ClearTempTakeoverPoint();
            }
            else if (TakeOverState == MissionEditState.Editing)
            {
                // 1. 백업해둔 값으로 UI 복구
                TakeOverLAT = _backupLat;
                TakeOverLON = _backupLon;
                TakeOverALT = _backupAlt;
                TakeOverUAVIDIndex = _backupIdIndex;

                // (선택된 모델 값도 롤백할 필요가 있다면 여기서 해줌 - 아직 저장 안했으므로 UI만 돌리면 됨)

                // 2. 상태 복구
                TakeOverState = MissionEditState.None;
                TakeOverEditEnable = false;
                TakeOverCheckEditEnable = false;
                TakeOverButton1Text = "생성";
                TakeOverButton3Text = "삭제";
                TakeOverChecked = false;
                ViewModel_Unit_Map.SingletonInstance.ClearTempTakeoverPoint();
            }
        }


        private void Callback_OnHandOverPointSet(double lat, double lon)
        {
            HandOverLAT = (float)lat;
            HandOverLON = (float)lon;
            HandOverChecked = false;
        }

        public RelayCommand HandOverButton1Command { get; set; }

        public void HandOverButton1CommandAction(object param)
        {
            if (HandOverState == MissionEditState.None)
            {
                // -------------------------------------------------------------
                // [신규 기능] 빈 UAV ID 자동 선택 로직
                // -------------------------------------------------------------
                // 현재 리스트에 등록된 AircraftID들을 가져옵니다.
                var existingIds = HandOverItemSource.Select(x => x.AircraftID).ToList();

                // 순차적으로 비어있는 ID를 찾아서 콤보박스 인덱스를 설정합니다.
                // ID 4 (UAV#1) -> Index 0
                // ID 5 (UAV#2) -> Index 1
                // ID 6 (UAV#3) -> Index 2

                if (!existingIds.Contains(4))
                {
                    HandOverUAVIDIndex = 0; // 4번이 없으면 1번 UAV 선택
                }
                else if (!existingIds.Contains(5))
                {
                    HandOverUAVIDIndex = 1; // 4번은 있고 5번이 없으면 2번 UAV 선택
                }
                else if (!existingIds.Contains(6))
                {
                    HandOverUAVIDIndex = 2; // 4,5번 있고 6번이 없으면 3번 UAV 선택
                }
                // 4, 5, 6이 다 있으면 기존 선택 유지 (건드리지 않음)
                // -------------------------------------------------------------
                HandOverLAT = 0;
                HandOverLON = 0;
                HandOverALT = 1000;

                HandOverEditEnable = true;
                HandOverCheckEditEnable = true;
                HandOverButton2Enable = false;
                HandOverButton3Enable = true;
                HandOverButton1Text = "저장";
                HandOverButton3Text = "취소";
                HandOverState = MissionEditState.Creating;


            }
            else if (HandOverState == MissionEditState.Creating)
            {
                //좌표 유효성 검사 (0,0 이면 저장 차단)
                // 운용자가 토글을 켰다 껐거나, 아무것도 안 하고 엔터만 쳤을 때를 방어
                if (HandOverLAT == 0 && HandOverLON == 0)
                {
                    var pop = new View_PopUp(5);
                    pop.Description.Text = "저장 실패";
                    pop.Reason.Text = "좌표가 설정되지 않았습니다.\n지도에서 위치를 선택하거나 직접 입력하세요.";
                    pop.Show();
                    return; // 저장 중단
                }

                HandOverEditEnable = false;
                HandOverCheckEditEnable = false;
                HandOverButton2Enable = false;
                HandOverButton3Enable = false;
                HandOverButton1Text = "생성";
                HandOverButton3Text = "삭제";
                HandOverState = MissionEditState.None;

                uint aircraftID = (uint)(HandOverUAVIDIndex + 4);

                bool alreadyExists = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.HandOverInfoList.Any(info => info.AircraftID == aircraftID);

                if (alreadyExists)
                {
                    var pop_error = new View_PopUp(5);
                    pop_error.Description.Text = "지점 추가 불가능";
                    pop_error.Reason.Text = "이미 같은 AircraftID가 존재합니다.";
                    pop_error.Show();
                    //CommonEvent.OnHandOverPointAdd?.Invoke(HandOverSelectedIndex, HandOverLAT, HandOverLON);
                    ViewModel_Unit_Map.SingletonInstance.ClearTempHandoverPoint();
                    return; // 이미 존재하면 추가하지 않음
                }

                var InputHandOver = new TakeOverHandOverInfo();
                InputHandOver.AircraftID = (uint)HandOverUAVIDIndex + 4;
                InputHandOver.CoordinateList.Latitude = HandOverLAT;
                InputHandOver.CoordinateList.Longitude = HandOverLON;
                InputHandOver.CoordinateList.Altitude = HandOverALT;

                HandOverItemSource.Add(InputHandOver);
                var TempIndex = HandOverItemSource.Count();
                HandOverSelectedIndex = TempIndex - 1;

                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.HandOverInfoListN = (uint)TempIndex;
                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.HandOverInfoList.Add(InputHandOver);

                //임시 파일 지우기 => 지도 Add
                CommonEvent.OnHandOverPointAdd?.Invoke((int)InputHandOver.AircraftID, HandOverLAT, HandOverLON);

            }
            else if (HandOverState == MissionEditState.Editing)
            {
                if (HandOverSelectedItem == null) return;

                // 모델 업데이트
                HandOverSelectedItem.CoordinateList.Latitude = HandOverLAT;
                HandOverSelectedItem.CoordinateList.Longitude = HandOverLON;
                HandOverSelectedItem.CoordinateList.Altitude = HandOverALT;

                // 지도 갱신 (Remove -> Add)
                CommonEvent.OnHandOverPointRemove?.Invoke((int)HandOverSelectedItem.AircraftID);
                CommonEvent.OnHandOverPointAdd?.Invoke((int)HandOverSelectedItem.AircraftID, HandOverLAT, HandOverLON);

                // 종료
                HandOverState = MissionEditState.None;
                HandOverEditEnable = false;
                HandOverCheckEditEnable = false;
                HandOverButton1Text = "생성";
                HandOverButton3Text = "삭제";
                HandOverChecked = false;
                ViewModel_Unit_Map.SingletonInstance.ClearTempHandoverPoint();
            }
        }

        public RelayCommand HandOverButton2Command { get; set; }

        public void HandOverButton2CommandAction(object param)
        {
            if (HandOverSelectedItem == null) return;
            CancelOtherModes(ActiveMode.HandOver);

            // 백업
            _backupLat = HandOverLAT;
            _backupLon = HandOverLON;
            _backupAlt = HandOverALT;
            _backupIdIndex = HandOverUAVIDIndex;

            // 상태 변경
            HandOverState = MissionEditState.Editing;
            HandOverEditEnable = true;
            HandOverCheckEditEnable = true;
            HandOverButton1Text = "저장";
            HandOverButton3Text = "취소";
            HandOverChecked = false;
        }

        public RelayCommand HandOverButton3Command { get; set; }

        public void HandOverButton3CommandAction(object param)
        {
            // ---------------------------------------------------------
            // 1. [삭제 모드] (State가 None일 때)
            // ---------------------------------------------------------
            if (HandOverState == MissionEditState.None)
            {
                if (HandOverSelectedItem == null) return;

                // (1) 지도 상의 아이콘 제거 요청 (이벤트가 정의되어 있다고 가정)
                CommonEvent.OnHandOverPointRemove?.Invoke((int)HandOverSelectedItem.AircraftID);

                // (2) 백엔드 데이터 모델에서 제거
                var targetModelList = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.HandOverInfoList;
                var itemToRemove = targetModelList.FirstOrDefault(m => m.AircraftID == HandOverSelectedItem.AircraftID);

                if (itemToRemove != null)
                {
                    targetModelList.Remove(itemToRemove);
                    ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.HandOverInfoListN = (uint)targetModelList.Count;
                }

                // (3) UI 리스트에서 제거
                HandOverItemSource.Remove(HandOverSelectedItem);

                // (4) 선택 초기화
                HandOverSelectedIndex = -1;
            }
            // ---------------------------------------------------------
            // 2. [취소 모드] (State가 Creating일 때)
            // ---------------------------------------------------------
            else if (HandOverState == MissionEditState.Creating)
            {
                // (1) UI 컨트롤 원상복구
                HandOverEditEnable = false;
                HandOverCheckEditEnable = false;

                HandOverButton2Enable = false;
                HandOverButton3Enable = false;

                HandOverButton1Text = "생성";
                HandOverButton3Text = "삭제";

                // (2) 상태 초기화
                HandOverState = MissionEditState.None;

                // (3) 지도 상호작용 해제 (단축키 X 기능 해제)
                HandOverChecked = false;

                // (4) 지도에 찍혀있던 '임시 점' 제거
                // (Map ViewModel에서 TempList를 비우는 메서드를 호출 - 기존 코드에서 ClearTempTakeoverPoint를 공용으로 쓰는 것으로 보임)
                ViewModel_Unit_Map.SingletonInstance.ClearTempHandoverPoint();
            }
            else if (HandOverState == MissionEditState.Editing)
            {
                // 백업 복구
                HandOverLAT = _backupLat;
                HandOverLON = _backupLon;
                HandOverALT = _backupAlt;
                HandOverUAVIDIndex = _backupIdIndex;

                // 종료
                HandOverState = MissionEditState.None;
                HandOverEditEnable = false;
                HandOverCheckEditEnable = false;
                HandOverButton1Text = "생성";
                HandOverButton3Text = "삭제";
                HandOverChecked = false;
                ViewModel_Unit_Map.SingletonInstance.ClearTempHandoverPoint();
            }
        }

        private void Callback_OnRTBPointSet(double lat, double lon)
        {
            RTBLAT = (float)lat;
            RTBLON = (float)lon;
            RTBChecked = false;
        }

        public RelayCommand RTBButton1Command { get; set; }

        public void RTBButton1CommandAction(object param)
        {
            if (RTBState == MissionEditState.None)
            {
                RTBLAT = 0;
                RTBLON = 0;
                RTBALT = 1000;

                RTBEditEnable = true;
                RTBCheckEditEnable = true;
                RTBButton2Enable = false;
                RTBButton3Enable = true;
                RTBButton1Text = "저장";
                RTBButton3Text = "취소";
                RTBState = MissionEditState.Creating;


            }
          
            else if (RTBState == MissionEditState.Creating)
            {
                //좌표 유효성 검사 (0,0 이면 저장 차단)
                // 운용자가 토글을 켰다 껐거나, 아무것도 안 하고 엔터만 쳤을 때를 방어
                if (RTBLAT == 0 && RTBLON == 0)
                {
                    var pop = new View_PopUp(5);
                    pop.Description.Text = "저장 실패";
                    pop.Reason.Text = "좌표가 설정되지 않았습니다.\n지도에서 위치를 선택하거나 직접 입력하세요.";
                    pop.Show();
                    return; // 저장 중단
                }

                // 1. [Model] 순수 데이터 생성 (UDP 전송용)
                var realData = new RTBCoordinateInfo();
                realData.Latitude = RTBLAT;
                realData.Longitude = RTBLON;
                realData.Altitude = RTBALT;

                // 2. [Wrapper] UI용 포장지 생성 (여기서 ID 부여!)
                var uiItem = new RTB_UI_Item(realData, _rtbIdCounter++);

                // 3. 리스트에 각각 추가
                RTBItemSource.Add(uiItem); // UI에는 포장지 추가

                // 백엔드(InitScenario)에는 알맹이만 추가
                var modelList = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.RTBCoordinateList;
                modelList.Add(realData);
                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.RTBCoordinateListN = (uint)modelList.Count;

                // 4. 지도 그리기 (Wrapper의 ID 사용)
                CommonEvent.OnRTBPointAdd?.Invoke(uiItem.UI_ID, RTBLAT, RTBLON);

                // 선택 처리 (방금 추가한 것 선택)
                RTBSelectedIndex = RTBItemSource.Count - 1;

                // 초기화
                RTBEditEnable = false;
                RTBCheckEditEnable = false;
                RTBButton2Enable = false;
                RTBButton3Enable = false;
                RTBButton1Text = "생성";
                RTBButton3Text = "삭제";

                RTBState = MissionEditState.None;
                RTBChecked = false;
                ViewModel_Unit_Map.SingletonInstance.ClearTempTakeoverPoint();
            }
            else if (RTBState == MissionEditState.Editing)
            {
                if (RTBSelectedItem == null) return;

                // 1. Wrapper 및 실제 Model 업데이트
                // RTBSelectedItem은 Wrapper이므로, 프로퍼티를 변경하면 Model도 같이 바뀝니다 (Wrapper 구조에 따라 다름)
                // 만약 Wrapper가 단순히 Model을 들고만 있다면:
                RTBSelectedItem.Latitude = RTBLAT;   // Wrapper 프로퍼티 갱신 (화면 갱신용)
                RTBSelectedItem.Longitude = RTBLON;
                RTBSelectedItem.Altitude = RTBALT;

                // 실제 통신용 Model 갱신 (확실하게 하기 위해)
                RTBSelectedItem.Model.Latitude = RTBLAT;
                RTBSelectedItem.Model.Longitude = RTBLON;
                RTBSelectedItem.Model.Altitude = RTBALT;

                // 2. 지도 갱신 (Remove -> Add)
                // Wrapper에 있는 UI_ID를 사용
                CommonEvent.OnRTBPointRemove?.Invoke(RTBSelectedItem.UI_ID);
                CommonEvent.OnRTBPointAdd?.Invoke(RTBSelectedItem.UI_ID, RTBLAT, RTBLON);

                // 3. 종료
                RTBState = MissionEditState.None;
                RTBEditEnable = false;
                RTBCheckEditEnable = false;
                RTBButton1Text = "생성";
                RTBButton3Text = "삭제";
                RTBChecked = false;
                ViewModel_Unit_Map.SingletonInstance.ClearTempTakeoverPoint();
            }
        }

        public RelayCommand RTBButton2Command { get; set; }

        public void RTBButton2CommandAction(object param)
        {
            if (RTBSelectedItem == null) return;
            CancelOtherModes(ActiveMode.RTB);

            // 백업
            _backupLat = RTBLAT;
            _backupLon = RTBLON;
            _backupAlt = RTBALT;

            // 상태 변경
            RTBState = MissionEditState.Editing;
            RTBEditEnable = true;
            RTBCheckEditEnable = true;
            RTBButton1Text = "저장";
            RTBButton3Text = "취소";
            RTBChecked = false;
        }

        public RelayCommand RTBButton3Command { get; set; }

        public void RTBButton3CommandAction(object param)
        {
            if (RTBState == MissionEditState.None)
            {
                if (RTBSelectedItem == null) return;

                // 1. 지도에서 삭제 (Wrapper의 ID 사용 - 인덱스 밀림 걱정 없음)
                CommonEvent.OnRTBPointRemove?.Invoke(RTBSelectedItem.UI_ID);

                // 2. 백엔드(InitScenario)에서 알맹이 삭제
                var modelList = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.RTBCoordinateList;
                modelList.Remove(RTBSelectedItem.Model); // 참조값으로 정확히 삭제됨
                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.RTBCoordinateListN = (uint)modelList.Count;

                // 3. UI 리스트에서 포장지 삭제
                RTBItemSource.Remove(RTBSelectedItem);

                RTBSelectedIndex = -1;
            }
            // ---------------------------------------------------------
            // 2. [취소 모드] (State가 Creating일 때)
            // ---------------------------------------------------------
            else if (RTBState == MissionEditState.Creating)
            {
                // (1) UI 컨트롤 원상복구
                RTBEditEnable = false;
                RTBCheckEditEnable = false;

                RTBButton2Enable = false;
                RTBButton3Enable = false;

                RTBButton1Text = "생성";
                RTBButton3Text = "삭제";

                // (2) 상태 초기화
                RTBState = MissionEditState.None;

                // (3) 지도 상호작용 해제 (단축키 C 기능 해제)
                RTBChecked = false;

                // (4) 지도에 찍혀있던 '임시 점' 제거
                // (기존 코드에서 RTBChecked = true 일 때 ClearTempTakeoverPoint를 호출하고 있었으므로 동일하게 처리하거나, 공용 Clear 사용)
                ViewModel_Unit_Map.SingletonInstance.ClearTempTakeoverPoint();
            }
            else if (RTBState == MissionEditState.Editing)
            {
                // 백업 복구
                RTBLAT = _backupLat;
                RTBLON = _backupLon;
                RTBALT = _backupAlt;

                // 종료
                RTBState = MissionEditState.None;
                RTBEditEnable = false;
                RTBCheckEditEnable = false;
                RTBButton1Text = "생성";
                RTBButton3Text = "삭제";
                RTBChecked = false;
                ViewModel_Unit_Map.SingletonInstance.ClearTempTakeoverPoint();
            }
        }
        private void Callback_OnFlightAreaPolygonSet(List<GeoPoint> PolygonList)
        {
            FlightAreaInnterItemSource.Clear();
            foreach (var loc in PolygonList)
            {
                var Lat = loc.Latitude;
                var Lon = loc.Longitude;
                var item = new AreaLatLonInfo();

                item.Latitude = (float)Lat;
                item.Longitude = (float)Lon;
                //item.Altitude = 0;
                FlightAreaInnterItemSource.Add(item);
            }
        }
        public RelayCommand FlightAreaButton1Command { get; set; }

        
        // [수정] 생성 (저장) 버튼 로직
        public void FlightAreaButton1CommandAction(object param)
        {
            if (FlightAreaState == MissionEditState.None)
            {
                // [생성 모드 진입]
                FlightAreaEditEnable = true;
                FlightAreaCheckEditEnable = true;
                //FlightAreaButton2Enable = false;
                //FlightAreaButton3Enable = true;
                FlightAreaButton1Text = "저장";
                FlightAreaButton3Text = "취소";
                FlightAreaState = MissionEditState.Creating;

                // 임시 리스트 초기화 (새로 그리기 위해)
                FlightAreaInnterItemSource.Clear();
            }
            else if (FlightAreaState == MissionEditState.Creating)
            {
                // 폴리곤은 최소 3개의 점이 필요합니다.
                if (FlightAreaInnterItemSource.Count < 3)
                {
                    var pop = new View_PopUp(5);
                    pop.Description.Text = "저장 실패";
                    pop.Reason.Text = "유효한 영역이 아닙니다.\n최소 3개 이상의 지점을 선택하여 영역을 구성해주세요.";
                    pop.Show();
                    return; // 저장 중단
                }

                // 1. Model 생성 (순수 데이터)
                var model = new FlightAreaInfo();
                model.AltitudeLimits.LowerLimit = FlightAreaLowerALT;
                model.AltitudeLimits.UpperLimit = FlightAreaUpperALT;

                // 임시로 그려진 점들을 Model에 옮겨 담음
                foreach (var item in FlightAreaInnterItemSource)
                {
                    model.AreaLatLonList.Add(new AreaLatLonInfo
                    {
                        Latitude = item.Latitude,
                        Longitude = item.Longitude
                    });
                }
                model.AreaLatLonListN = (uint)model.AreaLatLonList.Count;

                // 2. Wrapper 생성 (ID 부여)
                var wrapper = new FlightAreaWrapper(model, _flightAreaIdCounter++);

                // 3. UI 리스트 및 백엔드 리스트 추가
                FlightAreaItemSource.Add(wrapper);
                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.FlightAreaList.Add(model);
                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.FlightAreaListN = (uint)FlightAreaItemSource.Count;

                // 4. 지도에 폴리곤 추가 요청
                // (지도 뷰모델은 CustomMapPolygon 객체를 받으므로, ID를 심어서 보냄)
                var mapPoly = new CustomMapPolygon();
                mapPoly.MissionID = wrapper.UI_ID; // ★ ID 연동
                foreach (var p in model.AreaLatLonList)
                {
                    mapPoly.Points.Add(new GeoPoint(p.Latitude, p.Longitude));
                }
                CommonEvent.OnFlightAreaPolygonAdd?.Invoke(wrapper.UI_ID,mapPoly);

                // 5. 초기화
                FlightAreaState = MissionEditState.None;
                FlightAreaChecked = false; // 지도 그리기 모드 해제
                FlightAreaInnterItemSource.Clear(); // 임시 점 제거
                //ViewModel_Unit_Map.SingletonInstance.TempListsClear(); // 지도상의 임시 선/점 제거
                ViewModel_Unit_Map.SingletonInstance.ClearTempDrawing(); // 지도 임시 객체 삭제

                // 버튼 상태 복구
                FlightAreaEditEnable = false;
                FlightAreaCheckEditEnable = false;
                //FlightAreaButton2Enable = false;
                //FlightAreaButton3Enable = false;
                FlightAreaButton1Text = "생성";
                FlightAreaButton3Text = "삭제";
            }
            else if (FlightAreaState == MissionEditState.Editing)
            {
                // 1. 유효성 검사 (점 3개 이상)
                if (FlightAreaInnterItemSource.Count < 3)
                {
                    // 에러 팝업 띄우기 (기존 코드 참고)
                    return;
                }

                // 2. 원본 Model에 현재 Grid 데이터(수정된 값) 덮어쓰기
                var model = FlightAreaSelectedItem.Model;
                model.AreaLatLonList.Clear();

                foreach (var item in FlightAreaInnterItemSource)
                {
                    model.AreaLatLonList.Add(new AreaLatLonInfo
                    {
                        Latitude = item.Latitude,
                        Longitude = item.Longitude
                    });
                }
                model.AreaLatLonListN = (uint)model.AreaLatLonList.Count;

                // (옵션) 고도 정보도 수정했다면 반영
                model.AltitudeLimits.LowerLimit = FlightAreaLowerALT;
                model.AltitudeLimits.UpperLimit = FlightAreaUpperALT;

                // 3. 편집 종료 처리
                FlightAreaState = MissionEditState.None;

                // 버튼/UI 원상복구
                FlightAreaEditEnable = false;

                FlightAreaCheckEditEnable = false;
                FlightAreaChecked = false;

                FlightAreaButton1Text = "생성";
                //FlightAreaButton2Enable = true; // 수정 버튼 다시 활성
                //FlightAreaButton3Enable = true;
                FlightAreaButton3Text = "삭제";

                // 4. 지도 편집 모드 종료
                ViewModel_Unit_Map.SingletonInstance.IsFlightAreaEditMode = false;

                // 5. 백업 데이터 날리기 (필요 없음)
                _flightAreaBackup.Clear();
            }
        }


        public RelayCommand FlightAreaButton2Command { get; set; }

        //수정 버튼
        public void FlightAreaButton2CommandAction(object param)
        {
            //[진입 전 정리] 나(FlightArea)를 제외한 모든 모드 취소
            CancelOtherModes(ActiveMode.FlightArea);

            // 선택된 항목이 없으면 리턴
            if (FlightAreaSelectedItem == null) return;

            // 1. 상태 변경
            FlightAreaState = MissionEditState.Editing;

            // 2. 버튼 및 UI 제어
            FlightAreaEditEnable = true;
            FlightAreaCheckEditEnable = false;
            FlightAreaChecked = false;

            FlightAreaButton1Text = "저장";
            //FlightAreaButton2Enable = false;  // 수정 버튼 비활성
            //FlightAreaButton3Enable = true;
            FlightAreaButton3Text = "취소";

            // 3. Grid에 현재 데이터 채우기 (Detail View)
            FlightAreaInnterItemSource.Clear();
            foreach (var item in FlightAreaSelectedItem.Model.AreaLatLonList)
            {
                FlightAreaInnterItemSource.Add(new AreaLatLonInfo
                {
                    Latitude = item.Latitude,
                    Longitude = item.Longitude
                });
            }

            // 4. ★ [중요] 취소를 대비해 원본 데이터 백업 (Deep Copy)
            _flightAreaBackup.Clear();
            foreach (var item in FlightAreaInnterItemSource)
            {
                _flightAreaBackup.Add(new AreaLatLonInfo
                {
                    Latitude = item.Latitude,
                    Longitude = item.Longitude
                });
            }
            //맵 뷰모델에 레이어 활성화 신호
            var mapVM = ViewModel_Unit_Map.SingletonInstance;

            // 1) 상태 플래그 켜기
            mapVM.IsFlightAreaEditMode = true;

            // 2) 컨버터가 바라보는 Enum 값 변경 (이 시점에 레이어가 교체됨)
            mapVM.CurrentEditLayer = ViewModel_Unit_Map.EditLayerType.FlightArea;
        }

        public RelayCommand FlightAreaButton3Command { get; set; }

        // [수정] 삭제 / 취소 버튼 로직
        public void FlightAreaButton3CommandAction(object param)
        {
            // 1. [삭제 모드] - 파라미터로 넘어온 객체를 삭제 (Grid의 버튼 클릭 시)
            // 만약 선택된 항목(SelectedItem)을 지우는 방식이라면 FlightAreaSelectedItem 사용

            // *팁: 마스터-디테일에서는 행 안에 삭제 버튼을 두는 게 편하므로 param을 활용합니다.
            // param이 없으면 SelectedItem을 사용합니다.
            var target = param as FlightAreaWrapper ?? FlightAreaSelectedItem;

            if (FlightAreaState == MissionEditState.None)
            {
                if (target == null) return;

                // (1) 지도에서 삭제 (UI_ID 사용)
                CommonEvent.OnFlightAreaPolygonRemove?.Invoke(target.UI_ID);

                // (2) 백엔드 모델 삭제
                var modelList = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.FlightAreaList;
                modelList.Remove(target.Model);
                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.FlightAreaListN = (uint)modelList.Count;

                // (3) UI 리스트 삭제
                FlightAreaItemSource.Remove(target);

                FlightAreaSelectedItem = null;
            }
            // 2. [취소 모드]
            else if (FlightAreaState == MissionEditState.Creating)
            {
                // UI 복구 및 임시 데이터 초기화
                FlightAreaEditEnable = false;
                FlightAreaCheckEditEnable = false;
                //FlightAreaButton2Enable = false;
                //FlightAreaButton3Enable = false;
                FlightAreaButton1Text = "생성";
                FlightAreaButton3Text = "삭제";
                FlightAreaState = MissionEditState.None;
                FlightAreaChecked = false;

                ClearFlightAreaTempData();
            }
            // [추가] 편집(수정) 취소 로직
            else if (FlightAreaState == MissionEditState.Editing)
            {
                // 1. 백업해둔 데이터로 복구
                if (FlightAreaSelectedItem != null)
                {
                    var model = FlightAreaSelectedItem.Model;
                    model.AreaLatLonList.Clear();

                    // 백업본을 다시 모델에 넣음
                    foreach (var backupItem in _flightAreaBackup)
                    {
                        model.AreaLatLonList.Add(new AreaLatLonInfo
                        {
                            Latitude = backupItem.Latitude,
                            Longitude = backupItem.Longitude
                        });
                    }
                    model.AreaLatLonListN = (uint)model.AreaLatLonList.Count;

                    // Grid(화면)도 백업본으로 다시 그림
                    FlightAreaInnterItemSource.Clear();
                    foreach (var item in _flightAreaBackup)
                    {
                        FlightAreaInnterItemSource.Add(item);
                    }
                }

                // 2. 지도 강제 업데이트 (원래 모양으로 되돌리기)
                // 지도에 있는 해당 폴리곤을 찾아서 좌표를 리셋해줘야 함.
                // 이를 위해 '삭제 후 다시 추가'하거나, '좌표 업데이트 이벤트'를 날려야 합니다.
                // 가장 쉬운 방법:
                CommonEvent.OnFlightAreaPolygonRemove?.Invoke(FlightAreaSelectedItem.UI_ID); // 일단 지우고

                // 다시 그림 (백업된 좌표로)
                var mapPoly = new CustomMapPolygon();
                mapPoly.MissionID = FlightAreaSelectedItem.UI_ID;
                foreach (var p in FlightAreaInnterItemSource)
                {
                    mapPoly.Points.Add(new GeoPoint(p.Latitude, p.Longitude));
                }
                CommonEvent.OnFlightAreaPolygonAdd?.Invoke(FlightAreaSelectedItem.UI_ID, mapPoly);


                // 3. 상태 종료
                FlightAreaState = MissionEditState.None;

                FlightAreaEditEnable = false;
                FlightAreaCheckEditEnable = false;

                FlightAreaButton1Text = "생성";
                //FlightAreaButton2Enable = true;
                //FlightAreaButton3Enable = true;
                FlightAreaButton3Text = "삭제";

                // [기존 코드] 지도 편집 모드 끄기
                ViewModel_Unit_Map.SingletonInstance.IsFlightAreaEditMode = false;

                // ★ [추가] 레이어 타입도 None으로 변경하여 MapEditor 비활성화 확실히 처리
                ViewModel_Unit_Map.SingletonInstance.CurrentEditLayer = ViewModel_Unit_Map.EditLayerType.None;

                _flightAreaBackup.Clear();
            }
        }


        private void Callback_OnProhibitedAreaPolygonSet(List<GeoPoint> PolygonList)
        {
            ProhibitedAreaInnterItemSource.Clear();
            foreach (var loc in PolygonList)
            {
                var Lat = loc.Latitude;
                var Lon = loc.Longitude;
                var item = new AreaLatLonInfo();

                item.Latitude = (float)Lat;
                item.Longitude = (float)Lon;
                //item.Altitude = 0;
                ProhibitedAreaInnterItemSource.Add(item);
            }
        }
        public RelayCommand ProhibitedAreaButton1Command { get; set; }

        public void ProhibitedAreaButton1CommandAction(object param)
        {
            if (ProhibitedAreaState == MissionEditState.None)
            {
                // [생성 모드 진입]
                ProhibitedAreaEditEnable = true;
                ProhibitedAreaCheckEditEnable = true;
                //FlightAreaButton2Enable = false;
                //FlightAreaButton3Enable = true;
                ProhibitedAreaButton1Text = "저장";
                ProhibitedAreaButton3Text = "취소";
                ProhibitedAreaState = MissionEditState.Creating;

                // 임시 리스트 초기화 (새로 그리기 위해)
                ProhibitedAreaInnterItemSource.Clear();
            }
            else if (ProhibitedAreaState == MissionEditState.Creating)
            {
                // 폴리곤은 최소 3개의 점이 필요합니다.
                if (ProhibitedAreaInnterItemSource.Count < 3)
                {
                    var pop = new View_PopUp(5);
                    pop.Description.Text = "저장 실패";
                    pop.Reason.Text = "유효한 영역이 아닙니다.\n최소 3개 이상의 지점을 선택하여 영역을 구성해주세요.";
                    pop.Show();
                    return; // 저장 중단
                }

                // 1. Model 생성 (순수 데이터)
                var model = new ProhibitedArea();
                model.AltitudeLimits.LowerLimit = ProhibitedAreaLowerALT;
                model.AltitudeLimits.UpperLimit = ProhibitedAreaUpperALT;

                // 임시로 그려진 점들을 Model에 옮겨 담음
                foreach (var item in ProhibitedAreaInnterItemSource)
                {
                    model.AreaLatLonList.Add(new AreaLatLonInfo
                    {
                        Latitude = item.Latitude,
                        Longitude = item.Longitude
                    });
                }
                model.AreaLatLonListN = (uint)model.AreaLatLonList.Count;

                // 2. Wrapper 생성 (ID 부여)
                var wrapper = new ProhibitedAreaWrapper(model, _prohibitedAreaIdCounter++);

                // 3. UI 리스트 및 백엔드 리스트 추가
                ProhibitedAreaItemSource.Add(wrapper);
                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.ProhibitedAreaList.Add(model);
                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.ProhibitedAreaListN = (uint)ProhibitedAreaItemSource.Count;

                // 4. 지도에 폴리곤 추가 요청
                // (지도 뷰모델은 CustomMapPolygon 객체를 받으므로, ID를 심어서 보냄)
                var mapPoly = new CustomMapPolygon();
                mapPoly.MissionID = wrapper.UI_ID; // ★ ID 연동
                foreach (var p in model.AreaLatLonList)
                {
                    mapPoly.Points.Add(new GeoPoint(p.Latitude, p.Longitude));
                }
                CommonEvent.OnProhibitedAreaPolygonAdd?.Invoke(wrapper.UI_ID, mapPoly);

                // 5. 초기화
                ProhibitedAreaState = MissionEditState.None;
                ProhibitedAreaChecked = false; // 지도 그리기 모드 해제
                ProhibitedAreaInnterItemSource.Clear(); // 임시 점 제거
                ViewModel_Unit_Map.SingletonInstance.ClearTempDrawing(); // 지도 임시 객체 삭제

                // 버튼 상태 복구
                ProhibitedAreaEditEnable = false;
                ProhibitedAreaCheckEditEnable = false;
                //FlightAreaButton2Enable = false;
                //FlightAreaButton3Enable = false;
                ProhibitedAreaButton1Text = "생성";
                ProhibitedAreaButton3Text = "삭제";
            }
            else if (ProhibitedAreaState == MissionEditState.Editing)
            {
                // 1. 유효성 검사 (점 3개 이상)
                if (ProhibitedAreaInnterItemSource.Count < 3)
                {
                    // 에러 팝업 띄우기 (기존 코드 참고)
                    return;
                }

                // 2. 원본 Model에 현재 Grid 데이터(수정된 값) 덮어쓰기
                var model = ProhibitedAreaSelectedItem.Model;
                model.AreaLatLonList.Clear();

                foreach (var item in ProhibitedAreaInnterItemSource)
                {
                    model.AreaLatLonList.Add(new AreaLatLonInfo
                    {
                        Latitude = item.Latitude,
                        Longitude = item.Longitude
                    });
                }
                model.AreaLatLonListN = (uint)model.AreaLatLonList.Count;

                // (옵션) 고도 정보도 수정했다면 반영
                model.AltitudeLimits.LowerLimit = ProhibitedAreaLowerALT;
                model.AltitudeLimits.UpperLimit = ProhibitedAreaUpperALT;

                // 3. 편집 종료 처리
                ProhibitedAreaState = MissionEditState.None;

                // 버튼/UI 원상복구
                ProhibitedAreaEditEnable = false;

                ProhibitedAreaCheckEditEnable = false;
                ProhibitedAreaChecked = false;

                ProhibitedAreaButton1Text = "생성";
                //FlightAreaButton2Enable = true; // 수정 버튼 다시 활성
                //FlightAreaButton3Enable = true;
                ProhibitedAreaButton3Text = "삭제";

                // 4. 지도 편집 모드 종료
                ViewModel_Unit_Map.SingletonInstance.IsProhibitedAreaEditMode = false;

                // 5. 백업 데이터 날리기 (필요 없음)
                _prohibitedAreaBackup.Clear();
            }
        }

        public RelayCommand ProhibitedAreaButton2Command { get; set; }

        //수정 버튼
        public void ProhibitedAreaButton2CommandAction(object param)
        {
            // (ProhibitedArea)를 제외한 모든 모드 취소
            CancelOtherModes(ActiveMode.ProhibitedArea);

            // 선택된 항목이 없으면 리턴
            if (ProhibitedAreaSelectedItem == null) return;

            // 1. 상태 변경
            ProhibitedAreaState = MissionEditState.Editing;

            // 2. 버튼 및 UI 제어
            ProhibitedAreaEditEnable = true;
            ProhibitedAreaCheckEditEnable = false;
            ProhibitedAreaChecked = false;

            ProhibitedAreaButton1Text = "저장";
            //FlightAreaButton2Enable = false;  // 수정 버튼 비활성
            //FlightAreaButton3Enable = true;
            ProhibitedAreaButton3Text = "취소";

            // 3. Grid에 현재 데이터 채우기 (Detail View)
            ProhibitedAreaInnterItemSource.Clear();
            foreach (var item in ProhibitedAreaSelectedItem.Model.AreaLatLonList)
            {
                ProhibitedAreaInnterItemSource.Add(new AreaLatLonInfo
                {
                    Latitude = item.Latitude,
                    Longitude = item.Longitude
                });
            }

            // 4. ★ [중요] 취소를 대비해 원본 데이터 백업 (Deep Copy)
            _prohibitedAreaBackup.Clear();
            foreach (var item in ProhibitedAreaInnterItemSource)
            {
                _prohibitedAreaBackup.Add(new AreaLatLonInfo
                {
                    Latitude = item.Latitude,
                    Longitude = item.Longitude
                });
            }

            // 맵 레이어 활성화
            var mapVM = ViewModel_Unit_Map.SingletonInstance;

            mapVM.IsProhibitedAreaEditMode = true;
            mapVM.CurrentEditLayer = ViewModel_Unit_Map.EditLayerType.ProhibitedArea;
        }

        public RelayCommand ProhibitedAreaButton3Command { get; set; }

        // [수정] 삭제 / 취소 버튼 로직
        public void ProhibitedAreaButton3CommandAction(object param)
        {
            // 1. [삭제 모드] - 파라미터로 넘어온 객체를 삭제 (Grid의 버튼 클릭 시)
            // 만약 선택된 항목(SelectedItem)을 지우는 방식이라면 FlightAreaSelectedItem 사용

            // *팁: 마스터-디테일에서는 행 안에 삭제 버튼을 두는 게 편하므로 param을 활용합니다.
            // param이 없으면 SelectedItem을 사용합니다.
            var target = param as ProhibitedAreaWrapper ?? ProhibitedAreaSelectedItem;

            if (ProhibitedAreaState == MissionEditState.None)
            {
                if (target == null) return;

                // (1) 지도에서 삭제 (UI_ID 사용)
                CommonEvent.OnProhibitedAreaPolygonRemove?.Invoke(target.UI_ID);

                // (2) 백엔드 모델 삭제
                var modelList = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.ProhibitedAreaList;
                modelList.Remove(target.Model);
                ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario.MissionReferencePackage.ProhibitedAreaListN = (uint)modelList.Count;

                // (3) UI 리스트 삭제
                ProhibitedAreaItemSource.Remove(target);

                ProhibitedAreaSelectedItem = null;
            }
            // 2. [취소 모드]
            else if (ProhibitedAreaState == MissionEditState.Creating)
            {
                // UI 복구 및 임시 데이터 초기화
                ProhibitedAreaEditEnable = false;
                ProhibitedAreaCheckEditEnable = false;
                //FlightAreaButton2Enable = false;
                //FlightAreaButton3Enable = false;
                ProhibitedAreaButton1Text = "생성";
                ProhibitedAreaButton3Text = "삭제";
                ProhibitedAreaState = MissionEditState.None;
                ProhibitedAreaChecked = false;

                ClearProhibitedAreaTempData();
            }
            // [추가] 편집(수정) 취소 로직
            else if (ProhibitedAreaState == MissionEditState.Editing)
            {
                // 1. 백업해둔 데이터로 복구
                if (ProhibitedAreaSelectedItem != null)
                {
                    var model = ProhibitedAreaSelectedItem.Model;
                    model.AreaLatLonList.Clear();

                    // 백업본을 다시 모델에 넣음
                    foreach (var backupItem in _prohibitedAreaBackup)
                    {
                        model.AreaLatLonList.Add(new AreaLatLonInfo
                        {
                            Latitude = backupItem.Latitude,
                            Longitude = backupItem.Longitude
                        });
                    }
                    model.AreaLatLonListN = (uint)model.AreaLatLonList.Count;

                    // Grid(화면)도 백업본으로 다시 그림
                    ProhibitedAreaInnterItemSource.Clear();
                    foreach (var item in _prohibitedAreaBackup)
                    {
                        ProhibitedAreaInnterItemSource.Add(item);
                    }
                }

                // 2. 지도 강제 업데이트 (원래 모양으로 되돌리기)
                // 지도에 있는 해당 폴리곤을 찾아서 좌표를 리셋해줘야 함.
                // 이를 위해 '삭제 후 다시 추가'하거나, '좌표 업데이트 이벤트'를 날려야 합니다.
                // 가장 쉬운 방법:
                CommonEvent.OnProhibitedAreaPolygonRemove?.Invoke(ProhibitedAreaSelectedItem.UI_ID); // 일단 지우고

                // 다시 그림 (백업된 좌표로)
                var mapPoly = new CustomMapPolygon();
                mapPoly.MissionID = ProhibitedAreaSelectedItem.UI_ID;
                foreach (var p in ProhibitedAreaInnterItemSource)
                {
                    mapPoly.Points.Add(new GeoPoint(p.Latitude, p.Longitude));
                }
                CommonEvent.OnProhibitedAreaPolygonAdd?.Invoke(ProhibitedAreaSelectedItem.UI_ID, mapPoly);


                // 3. 상태 종료
                ProhibitedAreaState = MissionEditState.None;

                ProhibitedAreaEditEnable = false;
                ProhibitedAreaCheckEditEnable = false;

                ProhibitedAreaButton1Text = "생성";
                //FlightAreaButton2Enable = true;
                //FlightAreaButton3Enable = true;
                ProhibitedAreaButton3Text = "삭제";

                // [기존 코드] 지도 편집 모드 끄기
                ViewModel_Unit_Map.SingletonInstance.IsProhibitedAreaEditMode = false;

                // ★ [추가] 레이어 타입도 None으로 변경하여 MapEditor 비활성화 확실히 처리
                ViewModel_Unit_Map.SingletonInstance.CurrentEditLayer = ViewModel_Unit_Map.EditLayerType.None;

                _prohibitedAreaBackup.Clear();
            }
        }

    }

}
