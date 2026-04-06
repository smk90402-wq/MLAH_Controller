using DevExpress.Xpf.CodeView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;

namespace MLAH_Controller
{
    public class ViewModel_IntegrationTest : CommonBase
    {
        #region Singleton
        private static readonly Lazy<ViewModel_IntegrationTest> _lazy = new Lazy<ViewModel_IntegrationTest>(() => new ViewModel_IntegrationTest());
        public static ViewModel_IntegrationTest SingletonInstance => _lazy.Value;
        #endregion

        // ID 할당 규칙에 따른 ID 생성기
        //private static uint _nextLahMissionPlanId = 100_000_001;
        //private static uint _nextUav1MissionPlanId = 300_000_001;
        //private static uint _nextUav2MissionPlanId = 400_000_001;
        //private static uint _nextUav3MissionPlanId = 500_000_001;
        //private static uint _nextSegmentId = 750_000_001;
        //private static uint _nextIndividualMissionId = 800_000_001;
        //private static uint _nextWaypointId = 200_000_001;

        // ID 생성기
        private static uint _nextLahMissionPlanId = 100_000_001;
        private static uint _nextUavMissionPlanId = 300_000_001;

        // 요청하신 고정 ID 대역
        //private static uint _fixedSegmentId = 70_000_000;
        private static uint _fixedSegmentIdBase = 70_000_000;
        private static uint _nextIndividualMissionId = 800_000_001;
        //private static uint _nextWaypointId = 200_000_001;
        private static uint _nextWaypointId = 001;

        // 생성된 임무계획 ID를 보관할 리스트
        private List<uint> _generatedLahIds = new List<uint>();
        private List<uint> _generatedUavIds = new List<uint>();

        // 표적 위치 (시나리오 생성용)
        private const float TargetLat = 38.12352f;
        private const float TargetLon = 127.30367f;

        private Random _random = new Random();

        // [전술 지도 데이터]
        // P1(시작) -> P2(좌상) -> P3(우상) -> P4(종료)
        private readonly float[] _lats = { 38.12008f, 38.13727f, 38.13957f, 38.12039f };
        private readonly float[] _lons = { 127.30295f, 127.30615f, 127.32498f, 127.33419f };

        public ICommand GenerateReplansCommand { get; }
        public ICommand SendOptionsCommand { get; }
        public ICommand SendPilotDecisionCommand { get; }

        public ICommand SendImplicitUpdateCommand { get; }

        //public ICommand SendStartMissionCommand { get; }

        // [수정] 71303 대신 임무 완료 신호 테스트용 커맨드
        public ICommand SendMissionDoneCommand { get; }

        private string _logText;
        public string LogText { get => _logText; set { _logText = value; OnPropertyChanged(nameof(LogText)); } }

        public ViewModel_IntegrationTest()
        {
            GenerateReplansCommand = new RelayCommand(p => GenerateReplansAndCache());
            SendOptionsCommand = new RelayCommand(p => SendOptions());
            SendPilotDecisionCommand = new RelayCommand(p => SendDecision(p));
            SendImplicitUpdateCommand = new RelayCommand(async p => await SendImplicitUpdate());
            //SendStartMissionCommand = new RelayCommand(p => SendStartMission());
            SendMissionDoneCommand = new RelayCommand(p => SendMissionDoneSimulation());
        }

        /// <summary>
        /// 1. MUM-T 전술 기동 재계획 생성
        /// [수정 사항]: 임무지역 너비(308m)를 고려하여 오프셋을 축소함.
        /// </summary>
        private void GenerateReplansAndCache()
        {
            LogText = "로그 초기화...\n";
            Model_ScenarioSequenceManager.SingletonInstance.ClearReplanCacheAndOptions();

            _generatedLahIds.Clear();
            _generatedUavIds.Clear();

            // [거리 계산 근거]
            // 너비 308m -> 반경 154m. 
            // 안전 마진 고려하여 중앙선에서 약 100m 이격 목표.
            // 0.001도 ≈ 111m 이므로 0.0009f 적용.

            float forwardDist = 0.0025f; // 전방 약 270m 리드 (회전 시 이탈 방지 위해 축소)
            float sideDist = 0.0009f;    // 측면 약 100m 이격 (임무지역 154m 내 안착)

            // 1. LAH (지휘기): 중앙선 비행
            LAHMissionPlan lahPlan = CreateLahTacticalPlan(1, 0, 0);
            _generatedLahIds.Add(lahPlan.MissionPlanID);
            Model_ScenarioSequenceManager.SingletonInstance.Callback_OnLAHMissionPlanReceived(lahPlan);
            LogText += $"[LAH 생성] ID: {lahPlan.MissionPlanID} (본대: 중앙)\n";

            // 1. LAH (지휘기): 중앙선 비행
            LAHMissionPlan lahPlan2 = CreateLahTacticalPlan(2, 0, sideDist);
            _generatedLahIds.Add(lahPlan2.MissionPlanID);
            Model_ScenarioSequenceManager.SingletonInstance.Callback_OnLAHMissionPlanReceived(lahPlan2);
            LogText += $"[LAH 생성] ID: {lahPlan2.MissionPlanID} (본대: 중앙)\n";

            // 3. ★ LAH #3 (편대기2): 좌측 비행 및 1008번 표적 공격 추가 ★
            LAHMissionPlan lahPlan3 = CreateLahTacticalPlan(3, 0, -sideDist);

            // 웨이포인트 2번(세 번째 점)에 공격 명령 삽입
            if (lahPlan3.MissionSegemntList.Count > 0 && lahPlan3.MissionSegemntList[0].IndividualMissionList.Count > 0)
            {
                var firstMission = lahPlan3.MissionSegemntList[0].IndividualMissionList[0];

                // 웨이포인트가 충분히 생성되었는지 확인 후 공격 할당
                if (firstMission.WaypointList.Count > 2)
                {
                    // 파이썬 모듈 규칙에 따라 원본(8) + 1000 = 1008
                    firstMission.WaypointList[2].Attack.TargetID = 8;
                    firstMission.WaypointList[2].Attack.WeaponType = 3; // 1: 기관총, 2: 로켓(Hydra), 3: 대전차미사일(ATGM)
                }
            }
            _generatedLahIds.Add(lahPlan3.MissionPlanID);
            Model_ScenarioSequenceManager.SingletonInstance.Callback_OnLAHMissionPlanReceived(lahPlan3);
            LogText += $"[LAH 생성] ID: {lahPlan3.MissionPlanID} (편대기2: 좌측, ★표적 8번 공격 추가됨)\n";

            // 2. UAV #1 (정찰): 전방 + 우측 100m
            UAVMissionPlan uavPlan1 = CreateUavTacticalPlan(4, forwardDist, sideDist);
            _generatedUavIds.Add(uavPlan1.MissionPlanID);
            Model_ScenarioSequenceManager.SingletonInstance.Callback_OnUAVMissionPlanReceived(uavPlan1);
            LogText += $"[UAV#1 생성] ID: {uavPlan1.MissionPlanID} (정찰: 전방 우측 100m)\n";

            // 3. UAV #2 (정찰): 전방 + 좌측 100m (음수 적용)
            UAVMissionPlan uavPlan2 = CreateUavTacticalPlan(5, forwardDist, -sideDist);
            _generatedUavIds.Add(uavPlan2.MissionPlanID);
            Model_ScenarioSequenceManager.SingletonInstance.Callback_OnUAVMissionPlanReceived(uavPlan2);
            LogText += $"[UAV#2 생성] ID: {uavPlan2.MissionPlanID} (정찰: 전방 좌측 100m)\n";

            LogText += "--------------------------------------\n[전술] 임무지역(Width 308m) 내 편대 경로 생성 완료.";
        }

        /// <summary>
        /// [LAH 전용] 전술 경로 생성
        /// </summary>
        private LAHMissionPlan CreateLahTacticalPlan(uint aircraftId, float forwardOffset, float sideOffset)
        {
            var plan = new LAHMissionPlan
            {
                MissionPlanID = _nextLahMissionPlanId++,
                AircraftID = aircraftId
            };

            var segment = new MissionSegmentLAH
            {
                MissionSegmentID = _fixedSegmentIdBase,
                MissionSegmentType = 1 // 기동
            };

            int baseAlt = 600; // LAH 고도

            // Mission 1: P1 -> P2 (이 구간에 표적이 있음)
            // ★ 웨이포인트 개수(Count)를 10으로 늘려 촘촘하게 생성
            AddTacticalSegmentLAH(segment.IndividualMissionList, 0, 1, baseAlt, forwardOffset, sideOffset, 10);

            // Mission 2: P2 -> P3 (기존 3개 유지)
            AddTacticalSegmentLAH(segment.IndividualMissionList, 1, 2, baseAlt, forwardOffset, sideOffset, 3);

            // Mission 3: P3 -> P4
            AddTacticalSegmentLAH(segment.IndividualMissionList, 2, 3, baseAlt, forwardOffset, sideOffset, 3);

            segment.IndividualMissionLisnN = (uint)segment.IndividualMissionList.Count;
            plan.MissionSegemntList.Add(segment);
            plan.MissionSegemntN = (uint)plan.MissionSegemntList.Count;

            return plan;
        }

        /// <summary>
        /// [UAV 전용] 전술 경로 생성
        /// </summary>
        /// <summary>
        /// [UAV 전용] 전술 경로 생성 (시나리오 기반 수동 웨이포인트)
        /// </summary>
        private UAVMissionPlan CreateUavTacticalPlan(uint aircraftId, float forwardOffset, float sideOffset)
        {
            var plan = new UAVMissionPlan
            {
                MissionPlanID = _nextUavMissionPlanId++,
                AircraftID = aircraftId
            };

            var segment = new MissionSegmentUAV
            {
                MissionSegmentID = _fixedSegmentIdBase + aircraftId,
                MissionSegmentType = 1
            };

            //int baseAlt = 1200;
            int baseAlt = 800;
            float uavSpeed = 150;

            // --------------------------------------------------------------------------------
            // ★ Mission 1: P1 -> P2 구간
            // --------------------------------------------------------------------------------
            var mission1 = new IndividualMissionUAV { IndividualMissionID = _nextIndividualMissionId++, FlightType = 1 };

            int idxP1 = 0; int idxP2 = 1;

            // [수정 1] 시작점(WP0)은 전방 이격 없이(0), 측면 이격(sideOffset)만 적용하여 헬기와 나란히 배치
            var (startLatOff, startLonOff) = CalculateOffsetVector(idxP1, idxP2, 0, sideOffset);

            // [수정 2] 이동 중(WP1~)에는 전방 이격(forwardOffset)을 적용하여 앞서 나가게 함
            var (moveLatOff, moveLonOff) = CalculateOffsetVector(idxP1, idxP2, forwardOffset, sideOffset);


            // WP 0: 시작점 (헬기와 동일 선상에서 측면으로만 벌어짐) -> 기체 고정
            float wp0Lat = _lats[idxP1] + startLatOff;
            float wp0Lon = _lons[idxP1] + startLonOff;
            AddUavWaypoint(mission1.WaypointList, wp0Lat, wp0Lon, baseAlt, uavSpeed);

            // WP 1: 스위핑 시작점 (여기서부터는 앞서 나간 위치 적용)
            float ratio1 = 0.4f;
            float wp1Lat = _lats[idxP1] + (TargetLat - _lats[idxP1]) * ratio1 + moveLatOff;
            float wp1Lon = _lons[idxP1] + (TargetLon - _lons[idxP1]) * ratio1 + moveLonOff;
            AddUavWaypoint(mission1.WaypointList, wp1Lat, wp1Lon, baseAlt, uavSpeed);

            // WP 2: 표적 포착 지점 (표적 바로 앞 + 오프셋)
            float ratio2 = 0.95f;
            float wp2Lat = _lats[idxP1] + (TargetLat - _lats[idxP1]) * ratio2 + moveLatOff;
            float wp2Lon = _lons[idxP1] + (TargetLon - _lons[idxP1]) * ratio2 + moveLonOff;
            AddUavWaypoint(mission1.WaypointList, wp2Lat, wp2Lon, baseAlt, uavSpeed);

            // WP 3: 구간 종료점
            float wp3Lat = _lats[idxP2] + moveLatOff;
            float wp3Lon = _lons[idxP2] + moveLonOff;
            AddUavWaypoint(mission1.WaypointList, wp3Lat, wp3Lon, baseAlt, uavSpeed);

            // ... (이하 동일) ...
            if (mission1.WaypointList.Count > 0) mission1.WaypointList.Last().NextWaypointID = 0;
            mission1.WaypointListN = (uint)mission1.WaypointList.Count;
            segment.IndividualMissionList.Add(mission1);

            // Mission 2 & 3: 나머지 구간 (전방 이격 유지)
            AddTacticalSegmentUAV(segment.IndividualMissionList, 1, 2, baseAlt, forwardOffset, sideOffset, 2);
            AddTacticalSegmentUAV(segment.IndividualMissionList, 2, 3, baseAlt, forwardOffset, sideOffset, 2);

            segment.IndividualMissionListN = (uint)segment.IndividualMissionList.Count;
            plan.MissionSegemntList.Add(segment);
            plan.MissionSegemntN = (uint)plan.MissionSegemntList.Count;

            return plan;
        }

        // UAV 웨이포인트 추가 헬퍼 함수
        private void AddUavWaypoint(ObservableCollection<WaypointUAV> list, float lat, float lon, int alt, float speed)
        {
            list.Add(new WaypointUAV
            {
                WaypoinID = _nextWaypointId++,
                Coordinate = new Coordinate { Latitude = lat, Longitude = lon, Altitude = alt },
                Speed = speed,
                NextWaypointID = _nextWaypointId
            });
        }

        /// <summary>
        /// [LAH 전용] 개별 임무 및 웨이포인트 추가
        /// </summary>
        private void AddTacticalSegmentLAH(ObservableCollection<IndividualMissionLAH> list, int idxStart, int idxEnd, int alt, float fwdOff, float sideOff, int count)
        {
            var (latOff, lonOff) = CalculateOffsetVector(idxStart, idxEnd, fwdOff, sideOff);
            var mission = new IndividualMissionLAH { IndividualMissionID = _nextIndividualMissionId++ };

            // GenerateInterpolatedWaypoints에도 count 전달
            GenerateInterpolatedWaypoints(idxStart, idxEnd, alt, latOff, lonOff, count, (lat, lon, a) =>
            {
                return new WaypointLAH
                {
                    WaypoinID = _nextWaypointId++,
                    Coordinate = new Coordinate { Latitude = lat, Longitude = lon, Altitude = a },
                    Speed = 30, // LAH 속도
                    NextWaypointID = _nextWaypointId,
                    Hovering = 0,
                    Attack = new Attack()
                };
            }, mission.WaypointList);

            if (mission.WaypointList.Count > 0) mission.WaypointList.Last().NextWaypointID = 0;
            mission.WaypointListN = (uint)mission.WaypointList.Count;
            list.Add(mission);
        }

        /// <summary>
        /// [UAV 전용] 개별 임무 및 웨이포인트 추가
        /// </summary>
        private void AddTacticalSegmentUAV(ObservableCollection<IndividualMissionUAV> list, int idxStart, int idxEnd, int alt, float fwdOff, float sideOff, int count)
        {
            var (latOff, lonOff) = CalculateOffsetVector(idxStart, idxEnd, fwdOff, sideOff);
            var mission = new IndividualMissionUAV { IndividualMissionID = _nextIndividualMissionId++, FlightType = 1 };

            GenerateInterpolatedWaypoints(idxStart, idxEnd, alt, latOff, lonOff, count, (lat, lon, a) =>
            {
                return new WaypointUAV
                {
                    WaypoinID = _nextWaypointId++,
                    Coordinate = new Coordinate { Latitude = lat, Longitude = lon, Altitude = a },
                    Speed = 150, // UAV 속도
                    NextWaypointID = _nextWaypointId
                };
            }, mission.WaypointList);

            if (mission.WaypointList.Count > 0) mission.WaypointList.Last().NextWaypointID = 0;
            mission.WaypointListN = (uint)mission.WaypointList.Count;
            list.Add(mission);
        }

        /// <summary>
        /// [공통] 벡터 오프셋 계산 로직
        /// </summary>
        private (float latOffset, float lonOffset) CalculateOffsetVector(int idxStart, int idxEnd, float fwdOff, float sideOff)
        {
            float sLat = _lats[idxStart];
            float sLon = _lons[idxStart];
            float eLat = _lats[idxEnd];
            float eLon = _lons[idxEnd];

            double dLat = eLat - sLat;
            double dLon = eLon - sLon;
            double length = Math.Sqrt(dLat * dLat + dLon * dLon);

            if (length == 0) return (0, 0);

            double uLat = dLat / length;
            double uLon = dLon / length;

            // 우측 벡터 (y, -x)
            double rLat = uLon;
            double rLon = -uLat;

            float finalLat = (float)(uLat * fwdOff + rLat * sideOff);
            float finalLon = (float)(uLon * fwdOff + rLon * sideOff);

            return (finalLat, finalLon);
        }

        /// <summary>
        /// [공통] 웨이포인트 보간 생성기 (제네릭 활용)
        /// </summary>
        private void GenerateInterpolatedWaypoints<T>(int idxStart, int idxEnd, int alt, float latOff, float lonOff, int count, Func<float, float, int, T> createWp, ObservableCollection<T> targetList)
        {
            float sLat = _lats[idxStart];
            float sLon = _lons[idxStart];
            float eLat = _lats[idxEnd];
            float eLon = _lons[idxEnd];

            // int count = 3; // 기존 하드코딩 제거하고 인자값 사용
            for (int i = 0; i <= count; i++)
            {
                float ratio = (float)i / count;
                float curLat = sLat + (eLat - sLat) * ratio + latOff;
                float curLon = sLon + (eLon - sLon) * ratio + lonOff;
                targetList.Add(createWp(curLat, curLon, alt));
            }
        }

        /// <summary>
        /// 2. 재계획 옵션(53113) 메시지 수신을 시뮬레이션합니다. (개선된 버전)
        /// </summary>
        private void SendOptions()
        {
            // 1. 생성된 재계획이 있는지 확인 (LAH 3개, UAV 3개)
            if (_generatedLahIds.Count < 3 || _generatedUavIds.Count < 3)
            {
                LogText = "먼저 재계획을 3개씩 생성해주세요. (현재 LAH: " + _generatedLahIds.Count + ", UAV: " + _generatedUavIds.Count + ")";
                return;
            }

            var optionInfo = new MissionPlanOptionInfo();
            LogText = "[옵션 전송] 3개의 재계획 옵션 생성 시도 (각 옵션은 LAH 3개, UAV 3개 포함)...\n";

            for (int i = 1; i <= 3; i++)
            {
                var option = new OptionList { OptionID = (uint)i };

                // [수정] 랜덤으로 1개만 할당하는 대신, 생성된 ID 리스트 전체를 할당합니다.
                option.LAHMissionPlanIDList.AddRange(_generatedLahIds);
                option.UAVMissionPlanIDList.AddRange(_generatedUavIds);

                // [수정] 랜덤으로 2번째 ID를 추가하던 로직 제거
                // if (_generatedLahIds.Count > 1 && _random.Next(0, 2) == 1) ...
                // if (_generatedUavIds.Count > 1 && _random.Next(0, 2) == 1) ...

                optionInfo.OptionList.Add(option);
                LogText += $" - 옵션 {i}: LAH({string.Join(",", option.LAHMissionPlanIDList)}), UAV({string.Join(",", option.UAVMissionPlanIDList)})\n";
            }

            CommonEvent.OnMissionPlanOptionInfoReceived?.Invoke(optionInfo);
            LogText += "옵션 전송 완료.";
        }

        /// <summary>
        /// 3. 조종사 결정(51331) 메시지 수신을 시뮬레이션합니다.
        /// </summary>
        private void SendDecision(object parameter)
        {
            if (parameter == null) return;
            uint decisionId = uint.Parse(parameter.ToString());

            var decision = new PilotDecision { EditOptionsIDConverter = decisionId };

            CommonEvent.OnPilotDecisionReceived?.Invoke(decision);
            LogText = $"[결정 전송] 조종사가 옵션 #{decisionId}을(를) 선택했습니다. UI와 지도를 확인하세요.";
        }


        private async Task SendImplicitUpdate()
        {
            if (_generatedLahIds.Count == 0 && _generatedUavIds.Count == 0)
            {
                LogText = "먼저 재계획을 생성해주세요 (버튼 1).";
                return;
            }

            LogText = "[시스템 인가] 53114 메시지 생성 및 전송...\n";

            var updateMessage = new MissionUpdatewithoutPilotDecision();

            // 생성된 모든 LAH/UAV 재계획 ID를 포함시킵니다.
            updateMessage.LAHMissionPlanIDList.AddRange(_generatedLahIds);
            updateMessage.UAVMissionPlanIDList.AddRange(_generatedUavIds);

            // 리스트 개수 설정 (중요)
            updateMessage.LAHMissionPlanIDListN = (uint)_generatedLahIds.Count;
            updateMessage.UAVMissionPlanIDListN = (uint)_generatedUavIds.Count;

            // 이벤트 호출 (매니저가 이를 수신하여 UI 초기화 및 재계획 전시 수행)
            CommonEvent.OnMissionUpdateWithoutDecisionReceived?.Invoke(updateMessage);

            LogText += $" - 시스템 인가 완료. (LAH: {_generatedLahIds.Count}대)\n";
            LogText += " -> UI가 초기화되고, 인가 종류 'System'으로 표시되며 대기 상태가 됩니다.";

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                //var pop_error = new View_PopUp(10);
                //pop_error.Description.Text = "메시지 수신";
                //pop_error.Reason.Text = $"임무 인가 - System";
                //pop_error.Show();
                ViewModel_ScenarioView.SingletonInstance.AddLog($"임무 인가 - System", 3);
            });

            await Task.CompletedTask;


        }

        private void SendMissionDoneSimulation()
        {
            // 현재 ViewModel_UC_Unit_LAHMissionPlan의 상태를 보고, 
            // 현재 수행 중인 임무 ID를 가져와서 '완료' 신호를 보냅니다.

            var vm = ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance;

            // 테스트 편의상 LAH 1번의 현재 임무를 완료시키는 것으로 가정
            int currentMissionId = vm.ControlIndividualID1;

            if (currentMissionId == 0)
            {
                LogText = "현재 LAH 1번이 수행 중인 임무가 없습니다. (인가 후 임무 시작 필요)";
                return;
            }

            // 매니저에게 완료 알림 직접 전달 (gRPC 수신 시뮬레이션)
            Model_ScenarioSequenceManager.SingletonInstance.ProcessWaypointDone(1, currentMissionId);

            LogText = $"[완료 신호] LAH 1번의 임무 {currentMissionId} 완료 신호를 보냈습니다.\n" +
                      "UI에서 완료 표시(O) -> 다음 임무 갱신 과정을 확인하세요.";
        }
    }
}