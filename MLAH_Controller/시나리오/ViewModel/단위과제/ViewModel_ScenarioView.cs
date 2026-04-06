
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DevExpress.Xpf.Map;
using DevExpress.Xpf.Spreadsheet.Forms;
using DevExpress.XtraSpreadsheet.Model;
using MLAHInterop;
using Newtonsoft.Json;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using Path = System.IO.Path;


namespace MLAH_Controller
{
    public partial class ViewModel_ScenarioView : CommonBase
    {
        #region Singleton
        static ViewModel_ScenarioView _ViewModel_ScenarioView = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_ScenarioView SingletonInstance
        {
            get
            {
                if (_ViewModel_ScenarioView == null)
                {
                    _ViewModel_ScenarioView = new ViewModel_ScenarioView();
                }
                return _ViewModel_ScenarioView;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_ScenarioView()
        {
            //_dialogService = ServiceProvider.DialogService;

            ReturnToMainCommand = new RelayCommand(ReturnToMainCommandAction);

            AddObjectCommand = new RelayCommand(AddObjectCommandAction);
            DeleteObjectCommand = new RelayCommand(DeleteObjectCommandAction);
            EditObjectCommand = new RelayCommand(EditObjectCommandAction);

            ReferenceCommand = new RelayCommand(ReferenceCommandAction);

            ObjectSetCommand = new RelayCommand(ObjectSetCommandAction);
            AbnormalZoneCommand = new RelayCommand(AbnormalZoneCommandAction);
            BattlefieldEnvCommand = new RelayCommand(BattlefieldEnvCommandAction);

            SINILCommand = new RelayCommand(SINILCommandAction);
            UDPMonitoringCommand = new RelayCommand(UDPMonitoringCommandAction);
            MonitoringCommand = new RelayCommand(MonitoringCommandAction);
            //MonitoringCommand = new RelayCommand(MonitoringCommandAction, _ => IsMonitoringCommandEnabled);

            ShowComplexityCommand = new RelayCommand(ShowComplexityCommandAction);

            ScenarioAddCommand = new RelayCommand(ScenarioAddCommandAction);
            ScenarioOpenCommand = new RelayCommand(ScenarioOpenCommandAction);
            ScenarioSaveCommand = new RelayCommand(ScenarioSaveCommandAction);
            ScenarioSaveAsCommand = new RelayCommand(ScenarioSaveAsCommandAction);

            ScenarioPlayCommand = new AsyncRelayCommand(ScenarioPlayCommandAction);
            ScenarioStopCommand = new AsyncRelayCommand(ScenarioStopCommandAction);

            ParameterCommand = new RelayCommand(ParameterCommandAction);

            CommonEvent.OnMapUnitObjectClicked += Callback_OnMapUnitObjectClicked;
            

            _uiUpdateTimer = new DispatcherTimer
            {
                //Interval = TimeSpan.FromMilliseconds(100) //10fps
                //Interval = TimeSpan.FromMilliseconds(41) //24fps
                Interval = TimeSpan.FromMilliseconds(66) //15fps
            };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;

            // [신규] 복잡도 갱신 트리거 이벤트 구독
            CommonEvent.OnPilotDecisionReceived += Callback_CalculateComplexity;
            CommonEvent.OnMissionUpdateWithoutDecisionReceived += Callback_CalculateComplexity;

            CommonEvent.OnMissionResultDataReceived += Callback_OnMissionResultReceived;
            //BitAgentManager.Instance.OnSwStatusReceived += UpdateAgentStatus;
            BitAgentManager.Instance.OnHwStatusReceived += Callback_OnHwStatusReceived;
            BitAgentManager.Instance.OnSwStatusReceived += Callback_OnSwStatusReceived;

            // 핑(Ping) 체크 타이머 설정 (1초마다 검사)
            _pingCheckTimer = new DispatcherTimer();
            _pingCheckTimer.Interval = TimeSpan.FromSeconds(1);
            _pingCheckTimer.Tick += PingCheckTimer_Tick;
            _pingCheckTimer.Start();

            StartAutomationCommand = new RelayCommand(StartAutomationAction);
            StopAutomationCommand = new RelayCommand(StopAutomationAction);
            StartSimpleAutomationCommand = new RelayCommand(StartSimpleAutomationAction);

            TurnOnAllAgentsCommand = new RelayCommand(param => _ = ControlAllAgents(true));
            TurnOffAllAgentsCommand = new RelayCommand(param => _ = ControlAllAgents(false));

            ToggleLogPanelCommand = new RelayCommand(p => IsLogPanelExpanded = !IsLogPanelExpanded);
            ClearLogCommand = new RelayCommand(p => EventLogs.Clear());
        }

        #endregion 생성자 & 콜백

        //private readonly IDialogService _dialogService;

        public bool IsObervationStart = false;

        public DispatcherTimer _uiUpdateTimer;
        //public DispatcherTimer _uiUpdateTimer2;
        //private ulong _lastProcessedTimestamp = 0;
        private ObservationRequest _lastProcessedObservation = null;
        private readonly Dictionary<uint, SensorControlCommand> _lastProcessedSensorCommands = new Dictionary<uint, SensorControlCommand>();

        /// <summary>
        /// 100ms마다 실행되며 모든 UI 업데이트를 총괄하는 최종 메서드
        /// </summary>
        //private void UiUpdateTimer_Tick(object sender, EventArgs e)
        //{
        //    if (!IsSimPlaying) return;
        //    ProcessObservationUpdates();
        //    ProcessSensorControlUpdates();
        //}

        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!IsSimPlaying) return;

            var mapVM = ViewModel_Unit_Map.SingletonInstance;

            // 1. [유닛 위치 갱신] -> Begin/EndUpdate를 쓰지 마세요!
            //    유닛은 Add/Remove가 아니라 '좌표(Property)'만 바뀝니다.
            //    그냥 두면 PropertyChanged 이벤트로 자연스럽게 이동합니다.
            try
            {
                ProcessObservationUpdates();
                ProcessSensorControlUpdates();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unit Update Error: {ex.Message}");
            }

            // 2. [펄스 라인 / 경로 갱신] -> 여기는 Begin/EndUpdate가 필수입니다.
            //    이 리스트는 Clear() 하고 Add()를 반복하므로, 
            //    이벤트를 묶어서 보내야 성능이 좋아집니다.
            //mapVM.LAHPulseLineList.BeginUpdate();
            //try
            //{
            //    mapVM.RefreshActivePulseLines();
            //}
            //finally
            //{
            //    mapVM.LAHPulseLineList.EndUpdate();
            //}
        }

        /// <summary>
        /// Observation 데이터의 변경을 감지하고 UI를 업데이트합니다.
        /// </summary>
        private void ProcessObservationUpdates()
        {
            var latestObservation = Model_ScenarioSequenceManager.SingletonInstance.GetLastObservation();

            // 모델에서 가져온 최신 데이터가 이전에 처리한 데이터와 다를 경우에만 UI 업데이트
            if (latestObservation != null && latestObservation != _lastProcessedObservation)
            {
                UpdateUIFromObservation(latestObservation);
                _lastProcessedObservation = latestObservation; // 마지막으로 처리한 데이터로 기록
            }
        }

        /// <summary>
        /// SensorControl 데이터의 변경을 감지하고 UI를 업데이트합니다.
        /// </summary>
        private void ProcessSensorControlUpdates()
        {
            var latestCommands = Model_ScenarioSequenceManager.SingletonInstance.GetLatestSensorCommands();
            if (latestCommands == null) return;

            // ★ 디버그용: 딕셔너리에 데이터가 있는지 확인
            if (latestCommands.Count > 0)
            {
                // 이 로그가 찍힌다면, 무인기 모의기가 없어도 어디선가(예: 초기화 로직)
                // 0으로 채워진 더미 데이터가 딕셔너리에 들어갔다는 뜻입니다.
                //System.Diagnostics.Debug.WriteLine($"[SensorUpdate] Count: {latestCommands.Count}");
            }

            // 모델이 가진 최신 데이터 사전을 순회
            foreach (var pair in latestCommands)
            {
                uint uavId = pair.Key;
                SensorControlCommand latestCommand = pair.Value;

                // 이전에 처리한 데이터와 비교하여 변경되었는지 확인
                // 1. 이전에 이 UAV ID를 처리한 적이 없거나,
                // 2. 이전에 처리한 데이터와 최신 데이터가 다른 객체(업데이트됨)인 경우
                if (!_lastProcessedSensorCommands.TryGetValue(uavId, out var lastProcessedCommand) || lastProcessedCommand != latestCommand)
                {
                    // UI 업데이트 로직 실행
                    UpdateUIFromSensorCommand(latestCommand);

                    // 현재 처리한 데이터를 마지막 상태로 기록 (추가 또는 덮어쓰기)
                    _lastProcessedSensorCommands[uavId] = latestCommand;
                }
            }
        }

        // --- 신규 헬퍼 메서드 ---
        /// <summary>
        /// SensorControlCommand 데이터로 실제 UI를 업데이트하는 로직
        /// </summary>
        private void UpdateUIFromSensorCommand(SensorControlCommand command)
        {
            // ViewModel의 데이터 모델 업데이트
            var unit = model_UnitScenario.UnitObjectList.FirstOrDefault(x => x.ID == command.UavID);
            if (unit != null)
            {
                unit.LOC.Latitude = (float)command.SensorLat;
                unit.LOC.Longitude = (float)command.SensorLon;
                unit.LOC.Altitude = (int)command.SensorAlt;
                unit.velocity.Heading = (float)command.Heading;
                unit.velocity.Speed = (float)command.Speed;
            }

            // 지도 UI용 데이터 업데이트
            var displayUnit = ViewModel_Unit_Map.SingletonInstance.ObjectDisplayList.FirstOrDefault(x => x.ID == command.UavID);
            if (displayUnit != null)
            {
                displayUnit.Location.Latitude = (float)command.SensorLat;
                displayUnit.Location.Longitude = (float)command.SensorLon;
                displayUnit.Heading = (float)command.Heading;
            }
        }

        private void UpdateUIFromObservation(ObservationRequest message)
        {
            var model = this.model_UnitScenario;
            var mapViewModel = ViewModel_Unit_Map.SingletonInstance;

            // [1] 데이터 모델 업데이트 (백그라운드에서 해도 되지만, UI 타이머에서 해도 무방)
            var unitDictionary = model.UnitObjectList.ToDictionary(u => u.ID);

            //foreach (var item in message.Helicopters.Concat<dynamic>(message.Targets).Concat(message.Uavs))
            foreach (var item in message.Helicopters)
            {
                if (unitDictionary.TryGetValue(item.Id, out UnitObjectInfo unit))
                {
                    // 1. 업데이트 전 현재 체력(이전 값)을 변수에 저장
                    //var previousHealth = unit.Health;

                    // 2. 새로운 체력으로 업데이트
                    unit.Health = item.Health;

                    // 3. ★★★ 상태 전이 체크 ★★★
                    // 이전 체력은 100 이상이었는데, 새 체력이 100 미만이면 팝업 표시
                    //if (item.Health < 100 && previousHealth >= 100)
                    //{
                    //    // 팝업 생성 및 표시 로직 (위의 OnUnitHealthDropped 메서드 내용과 동일)
                    //    var warningPopup = new View_AlertPopUp(30);
                    //    warningPopup.Description.Text = $"{unit.Name}(ID:{unit.ID}) 피격";
                    //    //warningPopup.Reason.Text = $"현재 체력: {unit.Health:F0} / 100";
                    //    warningPopup.Show();
                    //}

                    if (item.Abnormalcause != null)
                    {
                        unit.entityAbnormalCause.Hit = item.Abnormalcause.Hit;
                        unit.entityAbnormalCause.Loss1 = item.Abnormalcause.Loss1;
                        unit.entityAbnormalCause.Loss2 = item.Abnormalcause.Loss2;
                        unit.entityAbnormalCause.Loss3 = item.Abnormalcause.Loss3;
                        unit.entityAbnormalCause.FuelWarning = item.Abnormalcause.Fuelwarning;
                        unit.entityAbnormalCause.FuelDanger = item.Abnormalcause.Fueldenger;
                        unit.entityAbnormalCause.FuelZero = item.Abnormalcause.Fuelzero;
                        unit.entityAbnormalCause.Crash = item.Abnormalcause.Crash;
                        //unit.entityAbnormalCause.Sensor = item.Abnormalcause.Sensor;
                    }

                    var cause = unit.entityAbnormalCause;
                    //unit.Status = (cause.Hit == 0 &&
                    //               cause.Loss1 == 0 &&
                    //               cause.Loss2 == 0 &&
                    //               cause.Loss3 == 0 &&
                    //               cause.FuelWarning == 0 &&
                    //               cause.FuelDanger == 0 &&
                    //               cause.FuelZero == 0 &&
                    //               cause.Crash == 0 &&
                    //               cause.Sensor == 0) ? (uint)1 : (uint)2;
                    bool isCriticalState = (cause.Hit == 1 || cause.Crash == 1);
                    if (cause.Hit == 1 || cause.Crash == 1)
                    {
                        // [기존] 팝업 띄우기
                        // var warningPopup = new View_AlertPopUp(30, 300);
                        // ...
                        // warningPopup.Show();

                        // [변경] 플래그 체크 후 로그 추가
                        if (!unit.IsAbnormalPopupShown)
                        {
                            // 1. 로그 패널에 추가 (type 2: Red Color)
                            AddLog($"[경고] {unit.Name}(ID:{unit.ID}) 피격/추락 감지됨!", 2);

                            // 2. 필요하다면 정말 중요한 건 팝업도 같이 띄울 수 있음 (선택사항)

                            unit.IsAbnormalPopupShown = true;
                        }
                    }
                    else
                    {
                        unit.IsAbnormalPopupShown = false;
                    }

                    //unit.Status = unit.Health == 100? (uint)1:(uint)2;
                    //unit.PayLoadHealth = unit.Health == 100 ? (uint)1 : (uint)2;

                    unit.Status = (uint)item.HealthStatus;
                    //unit.PayLoadHealth = (uint)item.PayloadHealthStatus;

                    unit.LOC.Latitude = (float)item.Location.Latitude;
                    unit.LOC.Longitude = (float)item.Location.Longitude;
                    unit.LOC.Altitude = (int)item.Location.Altitude;

                    double speed = Math.Sqrt(
                        Math.Pow(item.Velocity.U, 2) +
                        Math.Pow(item.Velocity.V, 2) +
                        Math.Pow(item.Velocity.W, 2)
                    );
                    unit.velocity.Speed = (float)(speed / 100);
                    unit.velocity.Heading = item.Rotation.Phi;

                    unit.Fuel = (float)item.Fuel;
                    unit.FuelWarning = (uint)item.FuelStatus;

                    unit.weapons.Type3 = (ushort)item.AtgmRound;
                    unit.weapons.Type2 = (ushort)item.HydraRound;
                    unit.weapons.Type1 = (ushort)item.MinigunRound;

                    unit.LAHStatus = item.Status;
                    //unit.AttackFlag = item.autoattckflag;

                }
            }
            // UAV 정보 업데이트 (타입-안전)
            foreach (var uavInfo in message.Uavs)
            {
                if (unitDictionary.TryGetValue(uavInfo.Id, out var unit))
                {
                    if(unit != null)
                    {
                        // ★★★ [방어 코드 추가] ★★★
                        // 언리얼에서 0,0,0 좌표나 0도 헤딩이 들어오면 무시하도록 처리
                        if (Math.Abs(unit.LOC.Latitude) < 0.000001 && Math.Abs(unit.LOC.Longitude) < 0.000001)
                        {
                            continue; // 0 좌표 데이터 스킵
                        }

                        // 헤딩도 0도인 경우 튈 수 있으므로, 필요하다면 체크 (선택 사항)
                        //if (Math.Abs(unit.velocity.Heading) < 0.001)
                        //{
                        //    continue;
                        //}

                        if (uavInfo.Subtype == "UAV")
                        {
                            //var unit = model.UnitObjectList.FirstOrDefault(x => x.ID == item.Id);

                            unit.LOC.Latitude = (float)uavInfo.Location.Latitude;
                            unit.LOC.Longitude = (float)uavInfo.Location.Longitude;
                            unit.LOC.Altitude = (int)uavInfo.Location.Altitude;
                            unit.velocity.Heading = (float)uavInfo.Rotation.Phi;

                            double speed = Math.Sqrt(Math.Pow(uavInfo.Velocity.U, 2) + Math.Pow(uavInfo.Velocity.V, 2) + Math.Pow(uavInfo.Velocity.W, 2));
                            unit.velocity.Speed = (float)speed;

                        }
                        // 1. 업데이트 전 현재 체력(이전 값)을 변수에 저장
                        //var previousHealth = unit.Health;

                        // 2. 새로운 체력으로 업데이트
                        unit.Health = uavInfo.Health;
                        //unit.Status = uavInfo.Health == 100 ? (uint)1 :(uint) 2;
                        //unit.PayLoadHealth = uavInfo.Health == 100 ? (uint)1 : (uint)2;
                        unit.Status = (uint)uavInfo.HealthStatus;
                        unit.PayLoadHealth = (uint)uavInfo.SensorStatus;

                        // 3. ★★★ 상태 전이 체크 ★★★
                        // 이전 체력은 100 이상이었는데, 새 체력이 100 미만이면 팝업 표시
                        //if (uavInfo.Health < 100 && previousHealth >= 100)
                        //{
                        //    // 팝업 생성 및 표시 로직 (위의 OnUnitHealthDropped 메서드 내용과 동일)
                        //    var warningPopup = new View_AlertPopUp(30, 300);
                        //    warningPopup.Description.Text = $"{unit.Name}(ID:{unit.ID}) 피격";
                        //    //warningPopup.Reason.Text = $"현재 체력: {unit.Health:F0} / 100";
                        //    warningPopup.Show();
                        //}
                        var cause = unit.entityAbnormalCause;
                        bool isCriticalState = (cause.Hit == 1 || cause.Crash == 1);
                        if (cause.Hit == 1 || cause.Crash == 1)
                        {
                            // [기존] 팝업 띄우기
                            // var warningPopup = new View_AlertPopUp(30, 300);
                            // ...
                            // warningPopup.Show();

                            // [변경] 플래그 체크 후 로그 추가
                            if (!unit.IsAbnormalPopupShown)
                            {
                                // 1. 로그 패널에 추가 (type 2: Red Color)
                                AddLog($"[경고] {unit.Name}(ID:{unit.ID}) 피격/추락 감지됨!", 2);

                                // 2. 필요하다면 정말 중요한 건 팝업도 같이 띄울 수 있음 (선택사항)

                                unit.IsAbnormalPopupShown = true;
                            }
                        }
                        else
                        {
                            unit.IsAbnormalPopupShown = false;
                        }

                        unit.Fuel = (float)uavInfo.Fuel;
                        unit.FuelWarning = (uint)uavInfo.FuelStatus;

                        if (uavInfo.CameraGoalPosition.Count >= 5)
                        {
                            unit.FootPrintLeftTopLat = (float)uavInfo.CameraGoalPosition[2].Latitude;
                            unit.FootPrintLeftTopLon = (float)uavInfo.CameraGoalPosition[2].Longitude;
                            unit.FootPrintLeftTopAlt = (int)uavInfo.CameraGoalPosition[2].Altitude;
                            unit.FootPrintRightTopLat = (float)uavInfo.CameraGoalPosition[4].Latitude;
                            unit.FootPrintRightTopLon = (float)uavInfo.CameraGoalPosition[4].Longitude;
                            unit.FootPrintRightTopAlt = (int)uavInfo.CameraGoalPosition[4].Altitude;
                            unit.FootPrintLeftBottomLat = (float)uavInfo.CameraGoalPosition[1].Latitude;
                            unit.FootPrintLeftBottomLon = (float)uavInfo.CameraGoalPosition[1].Longitude;
                            unit.FootPrintLeftBottomAlt = (int)uavInfo.CameraGoalPosition[1].Altitude;
                            unit.FootPrintRightBottomLat = (float)uavInfo.CameraGoalPosition[3].Latitude;
                            unit.FootPrintRightBottomLon = (float)uavInfo.CameraGoalPosition[3].Longitude;
                            unit.FootPrintRightBottomAlt = (int)uavInfo.CameraGoalPosition[3].Altitude;
                        }
                        //unit.DetectPixel = uavInfo.DetectionPixel;
                        //unit.RecogPixel = uavInfo.RecognitionPixel;
                    }
                    
                }
            }
            // 타겟 정보 업데이트 (타입-안전)
            foreach (var targetInfo in message.Targets)
            {
                if (unitDictionary.TryGetValue(targetInfo.Id, out var unit))
                {
                    unit.LOC.Latitude = (float)targetInfo.Location.Latitude;
                    unit.LOC.Longitude = (float)targetInfo.Location.Longitude;
                    unit.LOC.Altitude = (int)targetInfo.Location.Altitude;
                    unit.Health = targetInfo.Health;


                    double speed = Math.Sqrt(
                        Math.Pow(targetInfo.Velocity.U, 2) +
                        Math.Pow(targetInfo.Velocity.V, 2) +
                        Math.Pow(targetInfo.Velocity.W, 2)
                    );
                    unit.velocity.Speed = (float)(speed / 100);
                    unit.velocity.Heading = targetInfo.Rotation.Phi;

                    unit.Status = (uint)targetInfo.HealthStatus;
                    unit.Unit1TargetID = targetInfo.Unit1Targetid;

                }
            }


            // 현재 지도에 표시된 객체들을 빠르게 찾기 위한 Dictionary 생성
            var mapObjectDict = mapViewModel.ObjectDisplayList.ToDictionary(m => m.ID);
            var unitDataDict = model.UnitObjectList.ToDictionary(u => (uint)u.ID);

            // 새로 업데이트된 데이터 모델(UnitObjectList)을 기준으로 반복
            foreach (var unitData in model.UnitObjectList)
            {
                uint currentId = (uint)unitData.ID;
                if (mapObjectDict.TryGetValue(currentId, out var existingMapObject))
                {
                    // 객체가 이미 지도에 있으면 속성만 업데이트
                    existingMapObject.Location = new GeoPoint(unitData.LOC.Latitude, unitData.LOC.Longitude);
                    existingMapObject.Heading = unitData.velocity.Heading;

                    // 이미지 소스 최적화 로직 (기존과 동일)
                    ImageSource newImageSource = null;
                    if (unitData.Type == 3) // 유인기(LAH)인 경우
                    {
                        if (unitData.Status == 2) newImageSource = (ImageSource)Application.Current.Resources["LAH_RED"];
                        else if (unitData.ID == 1) newImageSource = (ImageSource)Application.Current.Resources["LAH_BLUE"];
                        else newImageSource = (ImageSource)Application.Current.Resources["LAH"];
                    }

                    if (unitData.Type == 1) // 무인기(UAV)인 경우
                    {
                        if (unitData.Status == 2) newImageSource = (ImageSource)Application.Current.Resources["UAV_TopView_Red"];
                        else newImageSource = (ImageSource)Application.Current.Resources["UAV_TopView"];
                    }

                    if (newImageSource != null && existingMapObject.imagesource != newImageSource)
                    {
                        existingMapObject.imagesource = newImageSource;
                    }
                }
                else
                {
                    // 지도에 없는 새로운 객체이면 추가
                    var newMapObject = mapViewModel.ConvertToObjectInfo(unitData);
                    mapViewModel.ObjectDisplayList.Add(newMapObject);
                }
            }

            for (int i = mapViewModel.ObjectDisplayList.Count - 1; i >= 0; i--)
            {
                var mapObject = mapViewModel.ObjectDisplayList[i];
                if (!unitDataDict.ContainsKey(mapObject.ID))
                {
                    mapViewModel.ObjectDisplayList.RemoveAt(i);
                }
            }


            // [4] 기타 UI 상태 업데이트
            this.SceneTime = TimeSpan.FromSeconds(message.Simulationtime);
        }



        public string prev_model_UnitScenario;
        public string prev_InitScenario;

        //시나리오 파일 생성/수정/삭제 핸들러
        //public Model_ScenarioFileHandler model_ScenarioFileHandler = new Model_ScenarioFileHandler();


        #region 시나리오 뷰 상단

        private string ScenarioFolderPath
        {
            get
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                return System.IO.Path.Combine(desktopPath, "시나리오");
            }
        }

        public RelayCommand ScenarioAddCommand { get; set; }
        public void ScenarioAddCommandAction(object param)
        {
            // 🚨 [수정 1] 무조건 가림막을 켜던 코드 삭제! (기존 상태 유지)
            // SceneBorderVisibility = Visibility.Visible; 

            // 최초 입력 요청
            string inputFileName = PromptForFileName("파일명을 입력하세요.");

            // 🚨 [방어 코드 1] 사용자가 취소를 누른 경우
            if (inputFileName == null)
            {
                // [수정 2] 가림막 상태를 건드리지 않고 그냥 조용히 빠져나감
                return;
            }

            string fileName = inputFileName + ".json";

            // 폴더가 없으면 미리 생성
            if (!Directory.Exists(ScenarioFolderPath))
            {
                Directory.CreateDirectory(ScenarioFolderPath);
            }

            // 바탕화면\시나리오 경로 사용
            string filePath = System.IO.Path.Combine(ScenarioFolderPath, fileName);

            // 파일 존재 여부 또는 빈칸(확인 버튼만 누름) 검사
            while (File.Exists(filePath) || string.IsNullOrWhiteSpace(inputFileName))
            {
                if (File.Exists(filePath))
                {
                    inputFileName = PromptForFileName("파일이 이미 존재합니다.");
                }
                else if (string.IsNullOrWhiteSpace(inputFileName))
                {
                    inputFileName = PromptForFileName("유효한 파일명을 입력하세요.");
                }

                //[방어 코드 2] 재입력 창에서 취소를 누른 경우
                if (inputFileName == null)
                {
                    // 마찬가지로 그냥 조용히 빠져나감
                    return;
                }

                fileName = inputFileName + ".json";
                filePath = System.IO.Path.Combine(ScenarioFolderPath, fileName);
            }

            // ==========================================================
            // ★ 모든 검증을 통과하여 새 파일 생성이 확정되었을 때만!
            // 기존 화면을 지우고, 가림막을 확실하게 걷어냅니다(Collapsed).
            // ==========================================================
            ScenarioViewClear();
            SceneBorderVisibility = Visibility.Collapsed;
            SceneFileName = inputFileName;
        }

        private string PromptForFileName(string message)
        {
            var popup = new View_NewSceneAddPopUp();
            popup.Reason.Text = message;
            popup.Reason1.Text = "시나리오 설명을 입력하세요 (필요시)";
            popup.ShowDialog();

            if (popup.DialogResult == true)
            {
                // '확인' 버튼을 누름 (아무것도 안 적었어도 "" 반환)
                SceneFileDescription = popup.FileDescBox.Text;
                return popup.FileNameBox.Text.Trim();
            }
            else
            {
                // '취소' 버튼을 누르거나 창을 강제로 닫음 (null 반환으로 취소 상태 명확히 구분)
                return null;
            }
        }

        public RelayCommand ScenarioOpenCommand { get; set; }
        public void ScenarioOpenCommandAction(object param)
        {
            //// 실행 파일 기준 ScenarioFiles 폴더 경로
            //string scenarioFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScenarioFiles");

            //// 폴더가 없으면 생성
            //if (!Directory.Exists(scenarioFolder))
            //{
            //    Directory.CreateDirectory(scenarioFolder);
            //}

            //// OpenFileDialog 생성 및 설정
            //var openFileDialog = new Microsoft.Win32.OpenFileDialog
            //{
            //    InitialDirectory = scenarioFolder,
            //    Filter = "JSON files (*.json)|*.json",
            //    Multiselect = false // 단일 파일 선택
            //};

            //바탕화면\시나리오 폴더 경로 가져오기
            string scenarioFolder = ScenarioFolderPath;

            //폴더가 없으면 생성
            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            // OpenFileDialog 생성 및 설정
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = scenarioFolder, //기본 경로를 바탕화면\시나리오 로 설정
                Filter = "JSON files (*.json)|*.json",
                Multiselect = false
            };

            // 파일 탐색창 표시
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string selectedFile = openFileDialog.FileName;
                try
                {
                    // 선택된 파일의 JSON 내용 읽기
                    string jsonContent = File.ReadAllText(selectedFile);

                    ScenarioViewClear();
                    
                    // JSON 역직렬화하여 model_UnitScenario에 할당
                    model_UnitScenario = JsonConvert.DeserializeObject<Model_UnitScenario>(jsonContent);

                    SceneFileName =  model_UnitScenario.ScenarioName;
                    SceneFileDescription = model_UnitScenario.ScenarioDesc;

                    //scenario.MessageID = 2;
                    PackageTypeComboIndex = (int)model_UnitScenario.InitScenario.InputMissionPackage.MissionType;
                    SensorTypeComboIndex = (int)model_UnitScenario.InitScenario.InputMissionPackage.DateAndNight;

                    ScenarioViewInit();

                    SceneBorderVisibility = Visibility.Collapsed;
                    // 필요에 따라 UI 갱신 또는 추가 작업 수행
                }
                catch (Exception ex)
                {
                    // 오류 발생 시 메시지 출력
                    System.Windows.MessageBox.Show("시나리오 파일을 로드하는 중 오류가 발생했습니다: " + ex.Message);
                }

            }
        }
        //controltype 1:local 2:message
        public void ScenarioViewInit()
        {
           
                if (model_UnitScenario == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. 맵 및 각종 리스트 초기화
                //ViewModel_Unit_Map.SingletonInstance.MapClear();
                //ScenarioViewClear();
                var packageVM = ViewModel_UC_Unit_MissionPackage.SingletonInstance;
                

                // UnitObjectList 복원 및 지도에 표시
                foreach (var item in model_UnitScenario.UnitObjectList)
                {
                    // model_UnitScenario는 이미 복원된 상태이므로 다시 Add할 필요는 없습니다.
                    // 바로 지도에 그리는 로직만 수행합니다.
                    var mapItem = ViewModel_Unit_Map.SingletonInstance.ConvertToObjectInfo(item);
                    ViewModel_Unit_Map.SingletonInstance.ObjectDisplayList.Add(mapItem);
                }

                // 초기 임무 정보 복원
                foreach (var item in model_UnitScenario.InitScenario.InputMissionPackage.InputMissionList)
                {
                    ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.inputMissionList.Add(item);
                    ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.InitMissionSet(item);
                }

                // 참조 정보 복원
                foreach (var item in model_UnitScenario.InitScenario.MissionReferencePackage.TakeOverInfoList)
                {
                    ViewModel_UC_Unit_MissionPackage.SingletonInstance.TakeOverItemSource.Add(item);
                    ViewModel_Unit_Map.SingletonInstance.Callback_OnTakeOverPointAdd((int)item.AircraftID, item.CoordinateList.Latitude, item.CoordinateList.Longitude);
                }

                foreach (var item in model_UnitScenario.InitScenario.MissionReferencePackage.HandOverInfoList)
                {
                    ViewModel_UC_Unit_MissionPackage.SingletonInstance.HandOverItemSource.Add(item);
                    ViewModel_Unit_Map.SingletonInstance.Callback_OnHandOverPointAdd((int)item.AircraftID, item.CoordinateList.Latitude, item.CoordinateList.Longitude);
                }

                //RTB 복원 - 여기는 ID 재발급 필요
                int tempRtbId = 0; // 로드 시에는 0부터 다시 시작
                foreach (var item in model_UnitScenario.InitScenario.MissionReferencePackage.RTBCoordinateList)
                {
                    // Wrapper 생성 (ID 부여)
                    var uiItem = new RTB_UI_Item(item, tempRtbId);

                    // UI 리스트에 추가
                    packageVM.RTBItemSource.Add(uiItem);

                    // 지도에 추가 (새로 부여된 ID 사용)
                    ViewModel_Unit_Map.SingletonInstance.Callback_OnRTBPointAdd(uiItem.UI_ID, item.Latitude, item.Longitude);

                    tempRtbId++; // ID 증가
                }
                packageVM.SetRTBCounter(tempRtbId);

                // [1] 비행가능구역 (FlightArea) 복원 - 주석 해제 및 로직 수정
                int flightAreaIdCounter = 0; // ID 재발급용 임시 변수
                foreach (var item in model_UnitScenario.InitScenario.MissionReferencePackage.FlightAreaList)
                {
                    // 1. Wrapper 생성 (ID 부여)
                    var wrapper = new FlightAreaWrapper(item, flightAreaIdCounter);

                    // 2. UI 리스트에 추가
                    ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaItemSource.Add(wrapper);

                    // 3. 지도에 그리기 (ID 연동)
                    var mapPoly = new CustomMapPolygon();
                    mapPoly.MissionID = wrapper.UI_ID; // ★ ID 심기
                    foreach (var inneritem in item.AreaLatLonList)
                    {
                        mapPoly.Points.Add(new GeoPoint(inneritem.Latitude, inneritem.Longitude));
                    }
                    // 지도 추가 이벤트 호출
                    CommonEvent.OnFlightAreaPolygonAdd?.Invoke(wrapper.UI_ID, mapPoly);

                    flightAreaIdCounter++;
                }
                // ★ 중요: 패키지 VM의 ID 카운터를 현재 로드된 개수만큼 동기화
                ViewModel_UC_Unit_MissionPackage.SingletonInstance.SetFlightAreaCounter(flightAreaIdCounter);

                
                int prohibitedAreaIdCounter = 0; // ID 재발급용 임시 변수
                foreach (var item in model_UnitScenario.InitScenario.MissionReferencePackage.ProhibitedAreaList)
                {
                    // 1. Wrapper 생성 (ID 부여)
                    var wrapper = new ProhibitedAreaWrapper(item, prohibitedAreaIdCounter);

                    // 2. UI 리스트에 추가
                    ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaItemSource.Add(wrapper);

                    // 3. 지도에 그리기 (ID 연동)
                    var mapPoly = new CustomMapPolygon();
                    mapPoly.MissionID = wrapper.UI_ID; // ★ ID 심기
                    foreach (var inneritem in item.AreaLatLonList)
                    {
                        mapPoly.Points.Add(new GeoPoint(inneritem.Latitude, inneritem.Longitude));
                    }
                    // 지도 추가 이벤트 호출
                    CommonEvent.OnProhibitedAreaPolygonAdd?.Invoke(wrapper.UI_ID, mapPoly);

                    prohibitedAreaIdCounter++;
                }
                // ★ 중요: 패키지 VM의 ID 카운터를 현재 로드된 개수만큼 동기화
                ViewModel_UC_Unit_MissionPackage.SingletonInstance.SetProhibitedAreaCounter(prohibitedAreaIdCounter);
            });
        }

        public void ScenarioViewInit(InputMissionPackageJson input)
        {

            if (input == null)
            {
                return;
            }
            else
            {
                InputMissionPackage newPackage = new InputMissionPackage();
                newPackage.InputMissionList = input.InputMissionList;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 초기 임무 정보 복원
                    foreach (var item in newPackage.InputMissionList)
                    {
                        ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.inputMissionList.Add(item);
                        ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.InitMissionSet(item);
                    }
                });
             

                // 참조 정보 복원
                //foreach (var item in input.MissionReferencePackage.TakeOverInfoList)
                //{
                //    ViewModel_UC_Unit_MissionPackage.SingletonInstance.TakeOverItemSource.Add(item);
                //    ViewModel_Unit_Map.SingletonInstance.Callback_OnTakeOverPointAdd((int)item.AircraftID, item.CoordinateList.Latitude, item.CoordinateList.Longitude);
                //}

                //foreach (var item in input.MissionReferencePackage.HandOverInfoList)
                //{
                //    ViewModel_UC_Unit_MissionPackage.SingletonInstance.HandOverItemSource.Add(item);
                //    ViewModel_Unit_Map.SingletonInstance.Callback_OnHandOverPointAdd((int)item.AircraftID, item.CoordinateList.Latitude, item.CoordinateList.Longitude);
                //}

                //foreach (var item in input.MissionReferencePackage.RTBCoordinateList)
                //{
                //    ViewModel_UC_Unit_MissionPackage.SingletonInstance.RTBItemSource.Add(item);
                //    ViewModel_Unit_Map.SingletonInstance.Callback_OnRTBPointAdd(0, item.Latitude, item.Longitude);
                //}

                //int FlightAreaCount = 0;
                //foreach (var item in input.MissionReferencePackage.FlightAreaList)
                //{

                //    ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaItemSource.Add(item);
                //    var InputPolygon = new CustomMapPolygon();
                //    InputPolygon.MissionID = FlightAreaCount;
                //    foreach (var inneritem in item.AreaLatLonList)
                //    {
                //        var InputItem = new GeoPoint(inneritem.Latitude, inneritem.Longitude);
                //        InputPolygon.Points.Add(InputItem);
                //    }
                //    ViewModel_Unit_Map.SingletonInstance.Callback_OnFlightAreaPolygonAdd(InputPolygon);
                //    FlightAreaCount++;
                //}

                //int ProhibitedAreaCount = 0;
                //foreach (var item in input.MissionReferencePackage.ProhibitedAreaList)
                //{
                //    ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaItemSource.Add(item);
                //    var InputPolygon = new CustomMapPolygon();
                //    InputPolygon.MissionID = ProhibitedAreaCount;
                //    foreach (var inneritem in item.AreaLatLonList)
                //    {
                //        var InputItem = new GeoPoint(inneritem.Latitude, inneritem.Longitude);
                //        InputPolygon.Points.Add(InputItem);
                //    }
                //    ViewModel_Unit_Map.SingletonInstance.Callback_OnFlightAreaPolygonAdd(InputPolygon);
                //    ProhibitedAreaCount++;
                //}
            }

        }


        public RelayCommand ScenarioSaveCommand { get; set; }
        public void ScenarioSaveCommandAction(object param)
        {
            // 현재 시간을 ISO 8601 포맷으로 변환 ("o" 형식 지정자 사용)
            //string timestamp = DateTime.Now.ToString("o");

            model_UnitScenario.ScenarioName = SceneFileName;
            model_UnitScenario.ScenarioDesc = SceneFileDescription;
            //model_UnitScenario.InitScenario.InputMissionPackage = new InputMissionPackage();

            model_UnitScenario.InitScenario.InputMissionPackage.MissionType = (uint)PackageTypeComboIndex;
            model_UnitScenario.InitScenario.InputMissionPackage.DateAndNight = (uint)SensorTypeComboIndex;



            var settingsIncludeAll = new JsonSerializerSettings
            {
                ContractResolver = new DynamicContractResolver() // 아무 필드도 제외하지 않음
            };

            string scenarioJson = JsonConvert.SerializeObject(model_UnitScenario, Formatting.Indented, settingsIncludeAll);
            //model_ScenarioFileHandler.Save_ModelScenarioFile_To_JsonFile(Scene_file);


            // 파일 경로 설정, 파일 이름은 시나리오 이름을 기반
            //string fileName = $"{model_UnitScenario.ScenarioName}.json";
            string fileName = $"{model_UnitScenario.ScenarioName}.json";


            //string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScenarioFiles", fileName);
            string filePath = System.IO.Path.Combine(ScenarioFolderPath, fileName);

            //if (!Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
            //{
            //    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            //}
            string directoryPath = System.IO.Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // JSON 문자열을 파일로 저장
            File.WriteAllText(filePath, scenarioJson);

            var pop_error = new View_PopUp(5);
            pop_error.Description.Text = "시나리오 파일 저장";
            pop_error.Reason.Text = "저장 완료";
            pop_error.Show();

            // 콘솔에 저장 완료 메시지 출력 (옵션)
            //Console.WriteLine($"Scenario saved to {filePath}");
        }

        public RelayCommand ScenarioSaveAsCommand { get; set; }
        public void ScenarioSaveAsCommandAction(object param)
        {
            // 1. 사용자에게 새로운 파일명을 묻습니다.
            string newFileName = PromptForFileName($"현재 '{SceneFileName}'을(를) 복제하여 새로 저장합니다.\n새 파일명을 입력하세요.");

            // 2. 사용자가 입력을 취소했거나 빈 칸이면 중단
            if (string.IsNullOrWhiteSpace(newFileName))
            {
                return;
            }

            // 3. 현재 작업 중인 파일명과 설명을 새 것으로 교체합니다.
            SceneFileName = newFileName;

            // (선택사항) 복제임을 명시하고 싶다면 설명에 추가할 수도 있습니다.
            // SceneFileDescription = "[복제본] " + SceneFileDescription;

            // 4. 기존의 '저장' 로직을 그대로 호출하여 새 파일명으로 JSON을 생성합니다.
            ScenarioSaveCommandAction(null);

            // 5. 완료 알림
            Application.Current.Dispatcher.Invoke(() =>
            {
                var pop_info = new View_PopUp(1); // 알맞은 아이콘 인덱스
                pop_info.Description.Text = "시나리오 복제 완료";
                pop_info.Reason.Text = $"'{newFileName}.json'으로 성공적으로 저장되었습니다.\n이제 새 시나리오에서 작업을 계속합니다.";
                pop_info.Show();
            });
        }

        public AsyncRelayCommand ScenarioPlayCommand { get; set; }

        public async Task ScenarioPlayCommandAction(object param)
        {
            SceneStatus = "모의 준비 중";
            IsSimPlaying = true;
            IsSimPlayingRev = false;
            SimPlayButtonEnable = false;
            ViewModel_UC_Unit_MissionPackage.SingletonInstance.SimPlayButtonEnable = false;

            ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.RefreshButtonStateBySimulation(true);

            EditButtonEnable = false;
            DeleteButtonEnable = false;
            Model_ScenarioSequenceManager.SingletonInstance.IsClickedMake = true;
            IsObervationStart = true;
            prev_model_UnitScenario = JsonConvert.SerializeObject(model_UnitScenario);

            //Model_ScenarioSequenceManager.SingletonInstance.ObjectMake();

            // InitScenario 가져오기
            var scenario = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.InitScenario;
            scenario.MessageID = 2;
            scenario.InputMissionPackage.MissionType = (uint)PackageTypeComboIndex;
            scenario.InputMissionPackage.DateAndNight = (uint)SensorTypeComboIndex;
            //scenario.InputMissionPackage.AircraftIDsN = (uint)model_UnitScenario.UnitObjectList.Count(x => x.Type == 1 || x.Type == 3);

            // UnitObjectList에서 Type이 1 또는 3인 객체를 필터링하고,
            // 각 객체의 ID만 선택한 후,
            // 오름차순으로 정렬하여 새로운 리스트를 생성
            var sortedAircraftIDs = model_UnitScenario.UnitObjectList
                .Where(unit => unit.Type == 1 || unit.Type == 3) // 조건: Type이 1 또는 3
                .Select(unit => unit.ID)                         // 프로젝션: unit 객체에서 ID 속성만 추출
                .OrderBy(id => id)                               // 정렬: ID를 기준으로 오름차순 정렬
                .ToList();                                       // 결과: List<uint> 형태로 최종 변환

            // --- 2. 개수 할당 ---
            // 찾은 ID의 개수를 AircraftIDsN에 할당
            scenario.InputMissionPackage.AircraftIDsN = (uint)sortedAircraftIDs.Count;

            // --- 3. 실제 ID 리스트 할당 ---
            // 기존 리스트를 초기화
            //scenario.InputMissionPackage.AircraftIDList.Clear();
            scenario.InputMissionPackage.AircraftIDs.Clear();

            // 정렬된 ID 목록을 하나씩 순회하면서
            // AircraftIDs 객체를 생성하고, 이를 AircraftIDList에 추가
            foreach (uint id in sortedAircraftIDs)
            {
                //scenario.InputMissionPackage.AircraftIDList.AircraftIDs.Add(new AircraftIDs { AircraftID = id });
                scenario.InputMissionPackage.AircraftIDs.Add(id);
            }


            // 5) 최종적으로 다시 JSON으로 직렬화
            string finalJson = JsonConvert.SerializeObject(scenario);

            prev_InitScenario = finalJson;

            // 6) UTF8 바이트 배열로 변환
            byte[] sendBytes = Encoding.UTF8.GetBytes(finalJson);

            // 7) ControlOper 송신용 메서드를 이용하여
            //    수신 포트가 50009(디스플레이)인 127.0.0.1로 전송

            await Model_ScenarioSequenceManager.SingletonInstance.ObjectMake();

            //임무 통제
            await UDPModule.SingletonInstance.SendUDPMessageAsync(sendBytes, "192.168.20.100", 50002);

            //시현 모의
            await UDPModule.SingletonInstance.SendUDPMessageAsync(sendBytes, "192.168.20.100", 50004);

            await UDPModule.SingletonInstance.SendUDPMessageAsync(sendBytes, "192.168.20.101", 50002);
            await UDPModule.SingletonInstance.SendUDPMessageAsync(sendBytes, "192.168.20.102", 50002);
            await UDPModule.SingletonInstance.SendUDPMessageAsync(sendBytes, "192.168.20.103", 50002);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                //var pop_error = new View_PopUp(5);
                //pop_error.Description.Text = "메시지 송신";
                //pop_error.Reason.Text = "초기임무정보 송신";
                //pop_error.Show();
                // 메시지 수신 시 로그 남기기
                ViewModel_ScenarioView.SingletonInstance.AddLog($"초기임무정보 송신",4);
            });

            _uiUpdateTimer.Start();
            //_uiUpdateTimer2.Start();

        }

       

        public AsyncRelayCommand ScenarioStopCommand { get; set; }

        public async Task ScenarioStopCommandAction(object param)
        {
            _uiUpdateTimer.Stop();
            //_uiUpdateTimer2.Stop();

            SceneStatus = "모의 종료";
            IsSimPlaying = false;
            IsSimPlayingRev = true;
            SimPlayButtonEnable = true;
            ViewModel_UC_Unit_MissionPackage.SingletonInstance.SimPlayButtonEnable = true;

            ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.RefreshButtonStateBySimulation(true);

            EditButtonEnable = true;
            DeleteButtonEnable = true;
            //Model_ScenarioSequenceManager.SingletonInstance.IsUAVMakeFinsh = false;
            IsObervationStart = false;
            SceneTime = TimeSpan.FromSeconds(0);

            _lastProcessedObservation = null;
            Model_ScenarioSequenceManager.SingletonInstance.ClearLastObservation();

            Model_ScenarioSequenceManager.SingletonInstance.IsClickedMake = false;
            //Model_ScenarioSequenceManager.SingletonInstance.ObjectReqCount = 0;

            //언리얼 살아있는지 체크해서 살아있으면 이렇게하고
            //죽었으면 1 줘야함
            await Model_ScenarioSequenceManager.SingletonInstance.InitUnreal();

            await Task.Delay(100);

            if (model_UnitScenario?.UnitObjectList != null)
            {
                foreach (var unit in model_UnitScenario.UnitObjectList)
                {
                    // 1. 플래그 초기화 (기존 코드)
                    unit.IsAbnormalPopupShown = false;
                    unit.FuelWarning = 1;
                    unit.PayLoadHealth = 1;
                    //unit.Status = 1;

                    // 2. ★ 상태 값(State)도 0으로 초기화해야 안전함 ★
                    if (unit.entityAbnormalCause != null)
                    {
                        unit.entityAbnormalCause.Hit = 0;
                        unit.entityAbnormalCause.Crash = 0;
                        unit.entityAbnormalCause.FuelWarning = 0;
                        unit.entityAbnormalCause.Loss1 = 0;
                        unit.entityAbnormalCause.Loss2 = 0;
                        unit.entityAbnormalCause.Loss3 = 0;
                        // 나머지 필드들도 필요하면 0으로...
                    }

                    // 상태 복구 (선택)
                    unit.Status = 1; // 1:Normal, 2:Hit/Abnormal
                }
            }

            if (!string.IsNullOrEmpty(prev_model_UnitScenario))
            {
                try
                {
                    // 1. JSON 문자열을 역직렬화하여 최초 상태의 객체를 새로 생성합니다.
                    Model_UnitScenario restoredScenario = JsonConvert.DeserializeObject<Model_UnitScenario>(prev_model_UnitScenario);

                    // 2. 현재 UI와 데이터 모델을 깨끗하게 비웁니다.
                    ScenarioViewClear();

                    // 3. 현재 클래스의 시나리오 모델을 복원된 객체로 완전히 교체합니다.
                    this.model_UnitScenario = restoredScenario;

                    PackageTypeComboIndex = (int)restoredScenario.InitScenario.InputMissionPackage.MissionType;
                    SensorTypeComboIndex = (int)restoredScenario.InitScenario.InputMissionPackage.DateAndNight;

                    // 4. 복원된 데이터 모델을 기반으로 UI를 다시 초기화합니다.
                    ScenarioViewInit(); // 내부적으로 this.model_UnitScenario를 사용
                }
                catch (JsonException ex)
                {
                    // 역직렬화 실패 시 예외 처리 (예: 에러 팝업 표시)
                    var pop_error = new View_PopUp(0);
                    pop_error.Description.Text = "시나리오 복원 실패";
                    pop_error.Reason.Text = $"데이터를 복원하는 중 오류가 발생했습니다: {ex.Message}";
                    pop_error.Show();
                }
            }
            else
            {
                // 만약 저장된 스냅샷이 없다면, 기본 초기 상태로 되돌립니다.
                ScenarioViewClear();
                ScenarioViewInit();
            }




        }

        




        public RelayCommand SINILCommand { get; set; }

        public void SINILCommandAction(object param)
        {
            CommonUtil.ShowFadeWindow(ViewName.SINILSim);
        }

        public RelayCommand UDPMonitoringCommand { get; set; }

        public void UDPMonitoringCommandAction(object param)
        {
            CommonUtil.ShowFadeWindow(ViewName.UDPMornitoring);
        }

        public RelayCommand MonitoringCommand { get; set; }

        //private bool _isMonitoringPopupOpening = false;
        public async void MonitoringCommandAction(object param)
        {
            ViewModel_MainView.SingletonInstance.MornitoringCommandAction(param);
        }

        public RelayCommand ReturnToMainCommand { get; set; }

        public void ReturnToMainCommandAction(object param)
        {
            // 기존의 Hide() 호출 및 이벤트 방식 삭제
            /*
            View_ScenarioView.SingletonInstance.Hide();
            CommonEvent.OnRequestFadeOut?.Invoke(ViewName.Main);
            */

            // 새로운 방식: ShellViewModel에 메인 뷰로 전환하도록 명령합니다.
            ShellViewModel.Instance.GoToMainViewCommand.Execute(null);
        }

        public RelayCommand ParameterCommand { get; set; }

        public void ParameterCommandAction(object param)
        {
            // 기존의 Hide() 호출 및 이벤트 방식 삭제
            /*
            View_ScenarioView.SingletonInstance.Hide();
            CommonEvent.OnRequestFadeOut?.Invoke(ViewName.Main);
            */

            // 새로운 방식: ShellViewModel에 메인 뷰로 전환하도록 명령합니다.
            //ShellViewModel.Instance.GoToMainViewCommand.Execute(null);

            var parameterPopUp = View_Parameter.SingletonInstance;
            parameterPopUp.Show();
            parameterPopUp.Topmost = true;
        }

        public RelayCommand AbnormalZoneCommand { get; set; }

        public void AbnormalZoneCommandAction(object param)
        {
            //ViewModel_ScenarioObject_PopUp.SingletonInstance.ModeVisibilitySetter(VisibilityMode.Add);
            CommonUtil.ShowFadeWindow(ViewName.AbnormalZone);
        }

        public RelayCommand BattlefieldEnvCommand { get; set; }

        public void BattlefieldEnvCommandAction(object param)
        {
            //ViewModel_ScenarioObject_PopUp.SingletonInstance.ModeVisibilitySetter(VisibilityMode.Add);
            CommonUtil.ShowFadeWindow(ViewName.BattlefieldEnv);
        }

        public RelayCommand ObjectSetCommand { get; set; }

        public void ObjectSetCommandAction(object param)
        {
            //ViewModel_Object_Set_PopUp.SingletonInstance.RefreshCommandAction(new object());
            //CommonUtil.ShowFadeWindow(ViewName.ObjectSet);
            var objectSetPopup = View_Object_Set_PopUp.SingletonInstance;
            objectSetPopup.Show();
            objectSetPopup.Topmost = true;
        }

        public void ScenarioViewClear()
        {
            //컬렉션 클리어
            // UI 스레드에서 UI 관련 객체를 변경하는 것이 안전합니다.
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 지도 클리어
                ViewModel_Unit_Map.SingletonInstance.MapClear();

                // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
                // 단 한 줄로 모든 시나리오 데이터와 하위 데이터를 재귀적으로 초기화!
                model_UnitScenario.Clear();
                // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★

                // ViewModel_UnitScenario 모델에 포함되지 않은 다른 ViewModel들의 데이터 초기화
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.LinearLOCList.Clear();
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.PolygonLOCList.Clear();
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.AreaList.Clear();
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.inputMissionList.Clear();

                ViewModel_UC_Unit_MissionPackage.SingletonInstance.TakeOverItemSource.Clear();
                ViewModel_UC_Unit_MissionPackage.SingletonInstance.HandOverItemSource.Clear();

                var packageVM = ViewModel_UC_Unit_MissionPackage.SingletonInstance;
                packageVM.RTBItemSource.Clear();
                packageVM.ResetRTBCounter();

                packageVM.FlightAreaInnterItemSource.Clear();
                packageVM.FlightAreaItemSource.Clear();
                packageVM.ResetFlightAreaCounter(); // ★ 카운터 초기화 추가

                packageVM.ProhibitedAreaInnterItemSource.Clear();
                packageVM.ProhibitedAreaItemSource.Clear();
                packageVM.ResetProhibitedAreaCounter(); // ★ 카운터 초기화 추가

                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.RTBItemSource.Clear();
                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaInnterItemSource.Clear();
                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaItemSource.Clear();
                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaInnterItemSource.Clear();
                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaItemSource.Clear();

                ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.MissionSegmentItemSource.Clear();
                ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.IndividualMissionPlanItemSource.Clear();
                ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.WayPointLAHItemSource.Clear();

                ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.MissionSegmentItemSource.Clear();
                ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.IndividualMissionPlanItemSource.Clear();
                ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.WayPointUAVItemSource.Clear();

                ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.LAHMissionPlanItemSource.Clear();
                ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.UAVMissionPlanItemSource.Clear();

                // 버튼 상태 업데이트
                EditButtonEnable = false;
                DeleteButtonEnable = false;
            });

        }

        public void ScenarioViewClearFromMessage()
        {
            //컬렉션 클리어
            // UI 스레드에서 UI 관련 객체를 변경하는 것이 안전합니다.
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 지도 클리어
                ViewModel_Unit_Map.SingletonInstance.InitScenarioMapClear();

                // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
                // 단 한 줄로 모든 시나리오 데이터와 하위 데이터를 재귀적으로 초기화!
                //model_UnitScenario.Clear();
                // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★

                // ViewModel_UnitScenario 모델에 포함되지 않은 다른 ViewModel들의 데이터 초기화
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.LinearLOCList.Clear();
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.PolygonLOCList.Clear();
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.AreaList.Clear();
                ViewModel_UC_Unit_INITMissionInfo.SingletonInstance.inputMissionList.Clear();

                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.TakeOverItemSource.Clear();
                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.HandOverItemSource.Clear();
                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.RTBItemSource.Clear();
                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaInnterItemSource.Clear();
                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.FlightAreaItemSource.Clear();
                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaInnterItemSource.Clear();
                //ViewModel_UC_Unit_MissionPackage.SingletonInstance.ProhibitedAreaItemSource.Clear();

                //ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.LAHMissionPlanItemSource.Clear();
                //ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.UAVMissionPlanItemSource.Clear();

                // 버튼 상태 업데이트
                //EditButtonEnable = false;
                //DeleteButtonEnable = false;
            });

        }

        #endregion 시나리오 뷰 상단

        #region 객체 생성/수정/삭제

        //객체 목록 생성 버튼
        public RelayCommand AddObjectCommand { get; set; }
        public void AddObjectCommandAction(object param)
        {
            ViewModel_ScenarioObject_PopUp.SingletonInstance.PopupMode = VisibilityMode.Add;
            ViewModel_ScenarioObject_PopUp.SingletonInstance.ModeVisibilitySetter(VisibilityMode.Add);
            CommonUtil.ShowFadeWindow(ViewName.ScenarioObjectPopUp);
        }

        public RelayCommand DeleteObjectCommand { get; set; }

        public void DeleteObjectCommandAction(object param)
        {
            // 1. 삭제할 객체가 선택되었는지 확인합니다.
            if (SelectedScenarioObject == null)
            {
                return;
            }

            var dialog = new View_PopUp_Dialog
            {
                Description = { Text = "삭제 확인" },
                Reason = { Text = $"{SelectedScenarioObject.Name} (ID: {SelectedScenarioObject.ID}) 객체를 정말 삭제하시겠습니까?" }
            };

            // ShowDialog()는 창이 닫힐 때까지 코드 실행을 멈추고, 사용자의 선택(true/false)을 반환합니다.
            bool? result = dialog.ShowDialog();

            // 사용자가 '예'를 누르지 않았으면(DialogResult가 true가 아니면) 메서드를 종료합니다.
            if (result != true)
            {
                return;
            }

            var objectToDelete = SelectedScenarioObject;
            int deletedIndex = ListSelectedIndex;

            // 3. (제거됨) 지휘기(Leader) 삭제 시 처리 로직을 제거했습니다.

            // 4. 데이터 모델에서 객체 삭제
            model_UnitScenario.UnitObjectList.Remove(objectToDelete);

            // 5. 지도(Map)에서 객체 삭제
            var mapObjectToRemove = ViewModel_Unit_Map.SingletonInstance.ObjectDisplayList.FirstOrDefault(m => m.ID == objectToDelete.ID);
            if (mapObjectToRemove != null)
            {
                ViewModel_Unit_Map.SingletonInstance.ObjectDisplayList.Remove(mapObjectToRemove);
            }

            // 6. 리스트의 선택 항목을 재설정합니다.
            if (model_UnitScenario.UnitObjectList.Count > 0)
            {
                // 삭제된 아이템의 인덱스나 그 이전 인덱스를 선택하여 범위를 벗어나지 않도록 합니다.
                int newIndex = Math.Min(deletedIndex, model_UnitScenario.UnitObjectList.Count - 1);
                SelectedScenarioObject = model_UnitScenario.UnitObjectList[newIndex];
                ListSelectedIndex = newIndex;
            }
            else
            {
                // 리스트가 비었으면 선택을 완전히 해제합니다.
                SelectedScenarioObject = null;
                EditButtonEnable = false;
                DeleteButtonEnable = false;
            }

        }

        public RelayCommand EditObjectCommand { get; set; }

        public void EditObjectCommandAction(object param)
        {
            ViewModel_ScenarioObject_PopUp.SingletonInstance.PopupMode = VisibilityMode.Edit;
            ViewModel_ScenarioObject_PopUp.SingletonInstance.ModeVisibilitySetter(VisibilityMode.Edit);
            CommonUtil.ShowFadeWindow(ViewName.ScenarioObjectPopUp);
        }

        #endregion 객체 생성/수정/삭제



        public RelayCommand ReferenceCommand { get; set; }

        public void ReferenceCommandAction(object param)
        {
            CommonUtil.ShowFadeWindow(ViewName.Ref);
        }


        #region 복잡협업 복잡도

        public RelayCommand ShowComplexityCommand { get; set; }
        public void ShowComplexityCommandAction(object param)
        {

            if (View_Complexity.SingletonInstance.Visibility == Visibility.Visible)
            {
                View_Complexity.SingletonInstance.Visibility = Visibility.Collapsed;
            }
            else
            {
                View_Complexity.SingletonInstance.Visibility = Visibility.Visible;
            }

            //if (View_ScenarioView.SingletonInstance.Left < 2560)
            //{
            //    View_Complexity.SingletonInstance.Left = 278;
            //}
            //else
            //{
            //    View_Complexity.SingletonInstance.Left = 2560 + 278;
            //}

        }
        #endregion 복합!협업 복잡도

        // 마지막으로 LAHMalFunctionState_Send를 보낸 시각
        private DateTime _lastLahStatesSentTime = DateTime.MinValue;


        //public async void Callback_SensorControl(UdpReceiveResult result)
        //{
        //    if (IsSimPlaying)
        //    {
        //        byte[] buffer = result.Buffer;
        //        SensorControlCommand command = ParseSensorControlCommand(buffer);

        //        await Application.Current.Dispatcher.InvokeAsync(new Action(() =>
        //        {
        //            var model = model_UnitScenario;
        //            {
        //                if (model.UnitObjectList.Count > 0 &&
        //                    model.UnitObjectList.Any(x => x.Type == 1))
        //                {
        //                    var unit = model.UnitObjectList.FirstOrDefault(x => x.ID == command.UavID);
        //                    if (unit != null)
        //                    {
        //                        unit.LOC.Latitude = (float)command.SensorLat;
        //                        unit.LOC.Longitude = (float)command.SensorLon;
        //                        unit.LOC.Altitude = (int)command.SensorAlt;
        //                        unit.velocity.Heading = (float)command.Heading;
        //                        unit.velocity.Speed = (float)command.Speed;

        //                        var displayUnit = ViewModel_Unit_Map.SingletonInstance.ObjectDisplayList
        //                            .FirstOrDefault(x => x.ID == command.UavID);
        //                        if (displayUnit != null)
        //                        {
        //                            displayUnit.Location.Latitude = (float)command.SensorLat;
        //                            displayUnit.Location.Longitude = (float)command.SensorLon;
        //                            displayUnit.Heading = (float)command.Heading;
        //                        }
        //                    }
        //                }
        //            }
        //        }));



        //    }

        //}


        public async void Callback_UavMalFunctionState(UdpReceiveResult result)
        {
            //Application.Current.Dispatcher.InvokeAsync(new Action(() =>
            //{
            //    byte[] buffer = result.Buffer;

            //    UAVMalFunctionState state = ParseUavMalFunctionState(buffer);

            //    if (model_UnitScenario.UnitObjectList.Count > 0)
            //    {
            //        if (model_UnitScenario.UnitObjectList.Where(x => x.Type == 1).Count() > 0)
            //        {
            //            if (model_UnitScenario.UnitObjectList.FirstOrDefault(x => x.ID == state.UavID) != null)
            //            {
            //                var targetUnit = model_UnitScenario.UnitObjectList.FirstOrDefault(x => x.ID == state.UavID);
            //                if (targetUnit != null)
            //                {
            //                    model_UnitScenario.UnitObjectList.Where(x => x.ID == state.UavID).FirstOrDefault().Status = state.Health;
            //                    model_UnitScenario.UnitObjectList.Where(x => x.ID == state.UavID).FirstOrDefault().PayLoadHealth = state.PayloadHealth;
            //                    model_UnitScenario.UnitObjectList.Where(x => x.ID == state.UavID).FirstOrDefault().FuelWarning = state.FuelWarning;

            //                    //View_Unit_Map.SingletonInstance.UpdateFocusSquare();
            //                }

            //            }

            //        }

            //    }
            //}));

            if (App.IsShuttingDown) return;

            // 1. 데이터 처리 (파싱, 객체 검색)는 백그라운드 스레드에서 먼저 수행합니다.
            byte[] buffer = result.Buffer;
            UAVMalFunctionState state = ParseUavMalFunctionState(buffer);

            // LINQ를 한 번만 사용하여 대상 객체를 효율적으로 찾습니다.
            var targetUnit = model_UnitScenario.UnitObjectList.FirstOrDefault(x => x.ID == state.UavID);

            // 2. 대상 객체를 찾은 경우에만 UI 스레드로 전환하여 속성을 업데이트합니다.
            if (targetUnit != null)
            {
                // UI 속성 변경은 반드시 Dispatcher를 통해 수행합니다.
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    targetUnit.Status = state.Health;
                    targetUnit.PayLoadHealth = state.PayloadHealth;
                    targetUnit.FuelWarning = state.FuelWarning;
                });
            }


        }

 

        private UAVMalFunctionState ParseUavMalFunctionState(byte[] buffer)
        {
            // SensorControlCommand의 총 길이 (바이트 수)
            const int expectedLength = 20;
            if (buffer.Length < expectedLength)
            {
                throw new ArgumentException("수신된 버퍼의 길이가 SensorControlCommand에 부족합니다.");
            }

            //UAVMalFunctionState command = new UAVMalFunctionState();
            //int offset = 0;

            //// 순서대로 각 필드 파싱 (리틀 엔디언을 가정)
            //command.MessageID = BitConverter.ToUInt32(buffer, offset);
            //offset += 4;

            //command.UavID = BitConverter.ToUInt32(buffer, offset);
            //offset += 4;

            //command.Health = BitConverter.ToUInt32(buffer, offset);
            //offset += 4;

            //command.PayloadHealth = BitConverter.ToUInt32(buffer, offset);
            //offset += 4;

            //command.FuelWarning = BitConverter.ToUInt32(buffer, offset);

            UAVMalFunctionState command = new UAVMalFunctionState();
            int offset = 0;

            // 순서대로 각 필드를 빅 엔디안으로 파싱
            command.MessageID = CommonUtil.ReadUInt32BigEndian(buffer, offset);
            offset += 4;

            command.UavID = CommonUtil.ReadUInt32BigEndian(buffer, offset);
            offset += 4;

            command.Health = CommonUtil.ReadUInt32BigEndian(buffer, offset);
            offset += 4;

            command.PayloadHealth = CommonUtil.ReadUInt32BigEndian(buffer, offset);
            offset += 4;

            command.FuelWarning = CommonUtil.ReadUInt32BigEndian(buffer, offset);

            return command;
        }

        private void Callback_OnMapUnitObjectClicked(uint ID)
        {
            var selected = model_UnitScenario.UnitObjectList.FirstOrDefault(x => x.ID == ID);

            if (selected != null)
            {
                // 이미 선택된 객체면 패스
                if (SelectedScenarioObject?.ID == selected.ID)
                    return;

                SelectedScenarioObject = selected;
                //View_Unit_Map.SingletonInstance.UpdateFocusSquare();
            }

        }

        #region [복잡협업 복잡도 계산 로직]

        // 1. 이벤트 콜백 (파라미터가 달라도 같은 계산 로직을 수행하므로 오버로딩 또는 object 처리)
        private void Callback_CalculateComplexity(PilotDecision data) => CalculateComplexity();
        private void Callback_CalculateComplexity(MissionUpdatewithoutPilotDecision data) => CalculateComplexity();

        /// <summary>
        /// 유인기/무인기 대수 및 임무(협업기저/개별) 개수를 기반으로 복잡도를 계산하여 UI를 갱신합니다.
        /// 식: (유인기 + 무인기) * (총 협업기저임무 수 + 총 개별임무 수)
        /// </summary>
        private void CalculateComplexity()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. 유인기(Type:3) + 무인기(Type:1) 대수 산출
                int helicopterCounts = model_UnitScenario.UnitObjectList.Count(x => x.Type == 3);
                int uavCounts = model_UnitScenario.UnitObjectList.Count(x => x.Type == 1);
                int totalUnits = helicopterCounts + uavCounts;

                // 2. 전체 임무 개수 산출 (협업기저임무 + 개별임무)
                var missionCounts = GetTotalMissionCounts();
                int totalMissions = missionCounts.TotalSegments + missionCounts.TotalIndividuals;

                // 3. 복잡도 공식 적용
                // 식: (유인기 + 무인기) * (협업기저임무 + 개별임무)
                int complexityValue = totalUnits * totalMissions;

                // 4. 프로퍼티 갱신
                ScenarioComplexity = complexityValue;

                // (디버깅 로그)
                // System.Diagnostics.Debug.WriteLine($"[복잡도] Unit({totalUnits}) * (Seg:{missionCounts.TotalSegments} + Ind:{missionCounts.TotalIndividuals}) = {ScenarioComplexity}");
            });
        }

        /// <summary>
        /// 현재 로드된 모든 LAH/UAV 계획에서 협업기저임무(Segment)와 개별임무(Individual)의 총 개수를 반환합니다.
        /// </summary>
        private (int TotalSegments, int TotalIndividuals) GetTotalMissionCounts()
        {
            int totalSegments = 0;
            int totalIndividuals = 0;

            // 1. LAH (유인기) 임무 계획 순회
            var lahPlans = ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.LAHMissionPlanItemSource;
            if (lahPlans != null)
            {
                foreach (var plan in lahPlans)
                {
                    // 각 헬기의 MissionSegmentList 순회
                    if (plan.MissionSegemntList != null)
                    {
                        totalSegments += plan.MissionSegemntList.Count; // 협업기저임무 개수 누적

                        foreach (var segment in plan.MissionSegemntList)
                        {
                            // 각 Segment의 IndividualMissionList 순회
                            if (segment.IndividualMissionList != null)
                            {
                                totalIndividuals += segment.IndividualMissionList.Count; // 개별임무 개수 누적
                            }
                        }
                    }
                }
            }

            // 2. UAV (무인기) 임무 계획 순회
            var uavPlans = ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.UAVMissionPlanItemSource;
            if (uavPlans != null)
            {
                foreach (var plan in uavPlans)
                {
                    // 각 무인기의 MissionSegmentList 순회
                    if (plan.MissionSegemntList != null)
                    {
                        totalSegments += plan.MissionSegemntList.Count; // 협업기저임무 개수 누적

                        foreach (var segment in plan.MissionSegemntList)
                        {
                            // 각 Segment의 IndividualMissionList 순회
                            if (segment.IndividualMissionList != null)
                            {
                                totalIndividuals += segment.IndividualMissionList.Count; // 개별임무 개수 누적
                            }
                        }
                    }
                }
            }

            return (totalSegments, totalIndividuals);
        }

        #endregion

    }





}






