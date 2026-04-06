using DevExpress.Data.Utils;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Grid;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using static MLAH_LogAnalyzer.MessageNameMapping;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace MLAH_LogAnalyzer
{
    public static class ScenarioUtils
    {
        public static string? GetNthRecentScenarioDirectory(string baseDir, int n)
        {
            // 모든 하위 디렉토리 찾기 (필터링 주석 처리됨)
            var scenarioDirectories = Directory.GetDirectories(baseDir)
                                        // .Where(d => Path.GetFileName(d).StartsWith("Scenario_")) // 시나리오 패턴 필터링 (테스트를 위한 주석처리)
                                        .OrderByDescending(d => d)
                                        .ToList();

            if (n > 0 && n <= scenarioDirectories.Count)
            {
                return scenarioDirectories[n - 1];
            }
            return null;
        }
    }


    public class MissionAnalysisResult
    {
        public JObject SummaryJson { get; set; } // 점수, 카운트 등 (기존 JObject)
        public List<TimelineSegment> TimelineSegments { get; set; } // 차트용 세그먼트 리스트
    }

    #region Data Models for Performance View
    public class ScenarioItem : INotifyPropertyChanged
    {
        public string ScenarioName { get; set; }
        public string FullPath { get; set; }

        // 분석 결과를 저장할 속성들
        public ComplexityResult Complexity { get; set; }
        //public JObject SuccessRateData { get; set; }
        public MissionAnalysisResult AnalysisResult { get; set; }

        // 호환성을 위해 기존 SuccessRateData 프로퍼티는 AnalysisResult.SummaryJson을 가리키게 수정
        public JObject SuccessRateData
        {
            get => AnalysisResult?.SummaryJson;
            set { /* setter 필요시 구현 */ }
        }
        public float AverageReplanTime { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ComplexityResult
    {
        public uint ComplexityScore { get; set; }
        public uint AircraftCount { get; set; }
        public uint CollaborativeMissionCount { get; set; }
        public uint IndividualMissionCount { get; set; }
    }

    // 우측 상단 그리드에 표시될 항목
    public class ScenarioSummary
    {
        public string ScenarioName { get; set; }
        public uint ComplexityScore { get; set; }
        public uint AircraftCount { get; set; }
        public uint CollaborativeMissions { get; set; }
        public uint IndividualMissions { get; set; }
        public ScenarioItem OriginalItem { get; set; } // 원본 데이터 참조
    }

    // 우측 하단 그리드에 표시될 항목
    public class ReplanSummary
    {
        public string ScenarioName { get; set; }
        public string AverageReplanTime { get; set; }
    }

    public class UavSuccessMetrics
    {
        public string UavName { get; set; }
        public uint TotalRequests { get; set; }
        public uint MatchedCount { get; set; }

        public string SuccessRateDisplay => TotalRequests > 0
        ? $"{(double)MatchedCount / TotalRequests * 100:F1}%"
        : "0%";
    }

    public class SegmentStyleSelector : StyleSelector
    {
        // XAML에서 이 속성들에 스타일 리소스를 연결할 것입니다.
        public Style NormalStyle { get; set; }
        public Style HatchedStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            // 'item'은 Series에 바인딩된 원본 데이터 객체(TimelineSegment)입니다.
            if (item is TimelineSegment segment)
            {
                if (segment.IsHatched)
                {
                    return HatchedStyle;
                }
                else
                {
                    return NormalStyle;
                }
            }

            return base.SelectStyle(item, container);
        }
    }
    #endregion

    /// <summary>
    /// 성능분석 - 개별임무 성공 판정 및 타임라인 생성
    /// 데이터 소스: 0501(임무상태), 0401(비행/카메라), 0302(임무계획), TargetData_Truth(표적위치)
    /// 출력: 임무별 성공/실패 + 사유, UAV별 타임라인 세그먼트
    /// </summary>
    public static class MissionSuccessCalculator
    {
        // 임무 타입 및 모드 정의 (ICD 문서 기준)
        public enum MissionType
        {
            None = 0,             // None
            TargetTracking = 1,   // 표적추적
            TargetAttack = 2,     // 표적공격
            AreaSearch = 3,       // 영역수색
            AreaPatrol = 4,       // 영역경계
            PointRecon = 5,       // 좌표점정찰
            CorridorRecon = 6,    // 통로정찰
            Movement = 7,         // 이동
            Cover = 8,            // 엄호
            Conceal = 9           // 은엄폐
        }
        public enum FlightMode { NotUsed = 0, AutoTakeoff = 1, AutoLanding = 2, TransferMove = 3, TacticalMove = 4, RTB = 5, Formation = 6, PathMove = 7, PointNav = 8, TargetTracking = 9 }
        public enum PayloadMode { NotUsed = 0, Coordinate = 1, AreaSearch = 2, AutoTracking = 3, FixedToAirframe = 4, AutoScan = 5 }

        private const double EarthRadiusMeters = 6371000.0;

        private static string GetMissionNameKR(MissionType type)
        {
            switch (type)
            {
                case MissionType.TargetTracking: return "표적추적";
                case MissionType.TargetAttack: return "표적공격";
                case MissionType.AreaSearch: return "영역수색";
                case MissionType.AreaPatrol: return "영역경계";
                case MissionType.PointRecon: return "좌표점정찰";
                case MissionType.CorridorRecon: return "통로정찰";
                case MissionType.Movement: return "이동";
                case MissionType.Cover: return "엄호";
                case MissionType.Conceal: return "은엄폐";
                default: return type.ToString();
            }
        }

        private static readonly Dictionary<MissionType, string> MissionColorMap = new()
{
    { MissionType.TargetTracking, "Crimson" },        // 표적추적: 진홍색
    { MissionType.TargetAttack, "OrangeRed" },        // 표적공격: 주홍색
    { MissionType.AreaSearch, "DodgerBlue" },         // 영역수색: 파란색
    { MissionType.AreaPatrol, "Orange" },             // 영역경계: 주황색
    { MissionType.PointRecon, "MediumPurple" },       // 좌표점정찰: 보라색
    { MissionType.CorridorRecon, "Teal" },            // 통로정찰: 청록색
    { MissionType.Movement, "Gray" },                 // 이동: 회색
    { MissionType.Cover, "ForestGreen" },             // 엄호: 녹색
    { MissionType.Conceal, "SaddleBrown" }            // 은엄폐: 갈색
};

        /// <summary>
        /// [메인] 전체 개별임무 성공 판정 + 타임라인 세그먼트 생성
        /// 1) 0501에서 UAV별 임무 타임라인 구축 (겹침 없는 세그먼트)
        /// 2) 임무별 성공 기준 평가 (커버리지/표적추적/이동 등)
        /// 3) 미수행 임무(재계획 대체) 마커 생성
        /// </summary>
        public static async Task<MissionAnalysisResult> CalculateIndividualMissionSuccessAsync(string scenarioDir, List<FlightData> flightDataList,
            List<IndividualMission> individualMissions,
            List<RealTargetData> targetDataList,
            string srtmFilePath)
        {
            int totalMissions = 0;
            int successfulMissions = 0;

            Dictionary<uint, uint> uavTotalRequests = new Dictionary<uint, uint>();
            Dictionary<uint, uint> uavMatchedCounts = new Dictionary<uint, uint>();
            List<TimelineSegment> timelineResults = new List<TimelineSegment>();

            // ★ [수정] 0501 임무 상태 데이터 로드 → individualMissionID 기반 시간 범위 추출
            var missionStatusList = LoadMissionStatus(scenarioDir);
            Debug.WriteLine($"[MissionStatus] 시나리오: {scenarioDir}");
            Debug.WriteLine($"[MissionStatus] 0501 데이터 로드 완료: {missionStatusList.Count}건");
            if (missionStatusList.Count > 0)
            {
                var first = missionStatusList.First();
                var last = missionStatusList.Last();
                Debug.WriteLine($"[MissionStatus] 0501 시간 범위: {first.Timestamp} ~ {last.Timestamp}");
            }
            // 0401 비행 데이터 시간 범위도 출력
            if (flightDataList.Count > 0)
            {
                var fdFirst = flightDataList.Where(f => f.Timestamp > 0).OrderBy(f => f.Timestamp).First();
                var fdLast = flightDataList.OrderByDescending(f => f.Timestamp).First();
                Debug.WriteLine($"[MissionStatus] 0401 시간 범위: {fdFirst.Timestamp} ~ {fdLast.Timestamp}");
            }

            // 타겟 데이터 시간순 정렬 (빠른 검색용)
            var sortedTargetData = targetDataList.OrderBy(t => t.Timestamp).ToList();

            // ★ [핵심] UAV별 임무 타임라인을 한 번에 구축 (겹침 없는 세그먼트)
            var missionTimelines = BuildMissionTimelines(missionStatusList);
            foreach (var kvp in missionTimelines)
            {
                Debug.WriteLine($"[Timeline] UAV{kvp.Key}: {kvp.Value.Count}개 세그먼트");
                foreach (var seg in kvp.Value)
                    Debug.WriteLine($"  {seg.MissionID}: {seg.Start} ~ {seg.End}");
            }

            // 중복 임무 제거 (같은 individualMissionID가 여러 InputMission에서 반복될 수 있음)
            var processedMissionIDs = new HashSet<ulong>();
            // 미수행 임무를 2nd pass에서 처리하기 위해 수집
            var unexecutedMissions = new List<IndividualMission>();
            // 수행된 임무의 시간범위 기록 (미수행 임무 위치 추정용)
            var executedTimeRanges = new Dictionary<ulong, (ulong Start, ulong End)>(); // missionID → (start, end)

            // ═══ 1st Pass: 수행된 임무 처리, 미수행 임무 수집 ═══
            foreach (var mission in individualMissions)
            {
                uint uavID = mission.aircraftID;

                // [안전] individualMissionInfo null 체크
                if (mission?.individualMissionInfo == null)
                    continue;

                // 중복 제거: 같은 individualMissionID는 한 번만 처리
                if (!processedMissionIDs.Add(mission.individualMissionID))
                    continue;

                MissionType type = (MissionType)mission.individualMissionInfo.individualMissionType;

                // ★ 빌드된 타임라인에서 임무 시간 범위 조회 (겹침 원천 차단)
                var (missionStart, missionEnd) = GetMissionTimeRangeFromTimeline(
                    mission.individualMissionID, uavID, missionTimelines);

                // 0501에서 못 찾으면 → 미수행 임무로 2nd pass에서 처리
                if (missionStart == 0 || missionEnd <= missionStart)
                {
                    Debug.WriteLine($"[MissionStatus] 임무 {mission.individualMissionID} (UAV{uavID}, {GetMissionNameKR(type)}) 미수행 (재계획으로 대체)");
                    unexecutedMissions.Add(mission);
                    continue;
                }

                totalMissions++;
                if (!uavTotalRequests.ContainsKey(uavID)) uavTotalRequests[uavID] = 0;
                uavTotalRequests[uavID]++;

                Debug.WriteLine($"[MissionStatus] 임무 {mission.individualMissionID} (UAV{uavID}, {GetMissionNameKR(type)}): {missionStart} ~ {missionEnd}");

                // 수행된 임무 시간범위 기록
                executedTimeRanges[mission.individualMissionID] = (missionStart, missionEnd);

                // 성공 여부 및 상세 정보 평가
                bool isSuccess = false;
                string details = "구간 없음";

                var evalResult = await EvaluateMissionSuccessAsync(mission, flightDataList, sortedTargetData, uavID, missionStart, missionEnd, srtmFilePath);
                isSuccess = evalResult.IsSuccess;
                details = evalResult.Details;

                if (isSuccess)
                {
                    successfulMissions++;
                    if (!uavMatchedCounts.ContainsKey(uavID)) uavMatchedCounts[uavID] = 0;
                    uavMatchedCounts[uavID]++;
                }

                string missionNameKR = GetMissionNameKR(type);
                string statusText = isSuccess ? "성공" : "실패";
                string color = isSuccess ? (MissionColorMap.TryGetValue(type, out var c) ? c : "Gray") : "Black";

                timelineResults.Add(new TimelineSegment
                {
                    TaskName = "임무 수행 현황",
                    State = $"{missionNameKR} ({statusText})",
                    MetricDetails = details,
                    StartTime = Epoch.AddMilliseconds(missionStart).ToLocalTime(),
                    EndTime = Epoch.AddMilliseconds(missionEnd).ToLocalTime(),
                    Color = color,
                    IsHatched = !isSuccess,
                    UavID = uavID
                });
            }

            // ═══ 2nd Pass: 미수행 임무 마커를 올바른 시간 위치에 배치 ═══
            foreach (var mission in unexecutedMissions)
            {
                uint uavID = mission.aircraftID;
                MissionType type = (MissionType)mission.individualMissionInfo.individualMissionType;
                string missionNameKR = GetMissionNameKR(type);
                ulong inputMissionID = mission.relatedMission?.inputMissionID ?? 0;

                // 같은 UAV + 같은 inputMissionID의 수행된 임무 중 직전 임무 찾기
                // (같은 패키지 내에서 이 미수행 임무보다 작은 ID 중 가장 큰 것)
                ulong markerPosition = 0;
                var samePkgExecuted = individualMissions
                    .Where(m => m.aircraftID == uavID
                             && m.relatedMission?.inputMissionID == inputMissionID
                             && m.individualMissionID < mission.individualMissionID
                             && executedTimeRanges.ContainsKey(m.individualMissionID))
                    .OrderByDescending(m => m.individualMissionID)
                    .FirstOrDefault();

                if (samePkgExecuted != null)
                {
                    // 직전 수행 임무의 종료 시점에 배치
                    markerPosition = executedTimeRanges[samePkgExecuted.individualMissionID].End;
                }
                else
                {
                    // 같은 inputMission에서 못 찾으면 → 같은 UAV의 수행된 임무 중 직전 것
                    var anyPrevExecuted = individualMissions
                        .Where(m => m.aircraftID == uavID
                                 && m.individualMissionID < mission.individualMissionID
                                 && executedTimeRanges.ContainsKey(m.individualMissionID))
                        .OrderByDescending(m => m.individualMissionID)
                        .FirstOrDefault();

                    if (anyPrevExecuted != null)
                        markerPosition = executedTimeRanges[anyPrevExecuted.individualMissionID].End;
                    else if (missionTimelines.ContainsKey(uavID) && missionTimelines[uavID].Count > 0)
                        markerPosition = missionTimelines[uavID].First().Start; // 최후 폴백: 타임라인 시작
                }

                if (markerPosition > 0)
                {
                    ulong markerEnd = markerPosition + 3000; // 3초 마커
                    timelineResults.Add(new TimelineSegment
                    {
                        TaskName = "미수행",
                        State = $"{missionNameKR} (미수행)",
                        MetricDetails = "재계획으로 대체됨",
                        StartTime = Epoch.AddMilliseconds(markerPosition).ToLocalTime(),
                        EndTime = Epoch.AddMilliseconds(markerEnd).ToLocalTime(),
                        Color = "DarkGray",
                        IsHatched = true,
                        UavID = uavID
                    });
                    Debug.WriteLine($"[MissionStatus] 미수행 마커: {mission.individualMissionID} (UAV{uavID}, {missionNameKR}) → 위치: {markerPosition}");
                }
            }

            // JSON 결과 생성 로직
            double successRate = (totalMissions == 0) ? 0 : (double)successfulMissions / totalMissions * 100.0;
            JObject finalResult = new JObject { ["Score"] = (uint)Math.Round(successRate, MidpointRounding.AwayFromZero), ["UAVMetrics"] = new JObject() };
            foreach (var id in uavTotalRequests.Keys)
            {
                ((JObject)finalResult["UAVMetrics"])[$"UAV_{id}"] = new JObject { ["TotalRequests"] = uavTotalRequests[id], ["MatchedCount"] = uavMatchedCounts.ContainsKey(id) ? uavMatchedCounts[id] : 0 };
            }

            return new MissionAnalysisResult
            {
                SummaryJson = finalResult,
                TimelineSegments = timelineResults
            };
        }

        // [폴백] 0501에서 못 찾을 때 0401의 FlightMode/PayloadMode 매칭으로 임무 구간 추정 (현재 미사용)
        private static (ulong Start, ulong End) GetEffectiveMissionSegment(IndividualMission mission, List<FlightData> flightData, uint uavID)
        {
            MissionType type = (MissionType)mission.individualMissionInfo.individualMissionType;

            // 해당 임무에 맞는 목표 비행/장비 모드 결정
            FlightMode targetFlightMode = FlightMode.PathMove;
            PayloadMode targetPayloadMode = PayloadMode.FixedToAirframe;

            // (스위치 문은 기존과 동일)
            switch (type)
            {
                case MissionType.TargetTracking: targetFlightMode = FlightMode.TargetTracking; targetPayloadMode = PayloadMode.AutoTracking; break;
                case MissionType.AreaSearch: targetFlightMode = FlightMode.PathMove; targetPayloadMode = PayloadMode.AreaSearch; break;
                case MissionType.AreaPatrol: targetFlightMode = FlightMode.PathMove; targetPayloadMode = PayloadMode.AreaSearch; break;
                case MissionType.PointRecon: targetFlightMode = FlightMode.PointNav; targetPayloadMode = PayloadMode.Coordinate; break;
                case MissionType.CorridorRecon: targetFlightMode = FlightMode.PathMove; targetPayloadMode = PayloadMode.AutoScan; break;
                case MissionType.Movement: targetFlightMode = FlightMode.PathMove; targetPayloadMode = PayloadMode.FixedToAirframe; break;
            }

            // 해당 UAV의 데이터만 추출
            var uavData = flightData
                .Where(f => f.AircraftID == uavID && f.FlightDataLog != null)
                .OrderBy(f => f.Timestamp)
                .ToList();

            ulong startTs = 0;
            ulong endTs = 0;

            foreach (var data in uavData)
            {
                // ★ [수정] 위에서 추가한 속성을 사용하여 값을 가져옵니다.
                // 데이터 모델에 추가한 이름(FlightMode, PayloadMode)을 정확히 쓰세요.
                int currentFlightMode = data.FlightMode;
                int currentPayloadMode = data.PayloadMode;

                // 조건 비교 (Enum 형변환)
                bool isMatching = (currentFlightMode == (int)targetFlightMode) &&
                                  (currentPayloadMode == (int)targetPayloadMode);

                if (isMatching)
                {
                    if (startTs == 0) startTs = data.Timestamp; // 진입 시점 기록
                }
                else
                {
                    // 매칭되다가 끊긴 순간이 바로 이탈 시점
                    if (startTs > 0 && endTs == 0)
                    {
                        endTs = data.Timestamp;
                        break; // 이탈 시점을 찾았으면 루프 종료 (하나의 구간만 찾는다고 가정)
                    }
                }
            }

            // 로그 끝까지 모드가 유지된 경우 처리
            if (startTs > 0 && endTs == 0 && uavData.Count > 0)
            {
                endTs = uavData.Last().Timestamp;
            }

            return (startTs, endTs);
        }

        /// <summary>
        /// 0501 데이터에서 UAV별 임무 타임라인 구축
        /// - 전환 시점 기반으로 겹치지 않는 (Start, End, MissionID) 세그먼트 리스트 반환
        /// - 3초 미만 flickering 세그먼트는 이전 세그먼트에 병합
        /// - 병합 후 인접 동일 임무는 통합
        /// </summary>
        private static Dictionary<uint, List<(ulong Start, ulong End, ulong MissionID)>> BuildMissionTimelines(
            List<MissionStatusEntry> statusList)
        {
            const ulong FLICKER_THRESHOLD_MS = 3000; // 3초 미만 세그먼트는 flickering으로 간주

            var result = new Dictionary<uint, List<(ulong Start, ulong End, ulong MissionID)>>();

            // UAV별로 그룹핑
            var byUav = statusList.GroupBy(s => s.AircraftID);

            foreach (var group in byUav)
            {
                uint uavID = group.Key;
                var entries = group.OrderBy(s => s.Timestamp).ToList();

                if (entries.Count == 0) continue;

                // 1단계: 원시 세그먼트 생성
                var rawSegments = new List<(ulong Start, ulong End, ulong MissionID)>();
                ulong segStart = entries[0].Timestamp;
                ulong currentMissionID = entries[0].IndividualMissionID;

                for (int i = 1; i < entries.Count; i++)
                {
                    if (entries[i].IndividualMissionID != currentMissionID)
                    {
                        rawSegments.Add((segStart, entries[i - 1].Timestamp, currentMissionID));
                        segStart = entries[i].Timestamp;
                        currentMissionID = entries[i].IndividualMissionID;
                    }
                }
                rawSegments.Add((segStart, entries.Last().Timestamp, currentMissionID));

                // 2단계: flickering 세그먼트 제거 (3초 미만 → 이전 세그먼트에 병합)
                var merged = new List<(ulong Start, ulong End, ulong MissionID)>();
                foreach (var seg in rawSegments)
                {
                    ulong duration = seg.End - seg.Start;
                    if (duration < FLICKER_THRESHOLD_MS && merged.Count > 0)
                    {
                        // 짧은 세그먼트 → 이전 세그먼트의 종료시간을 연장
                        var prev = merged[merged.Count - 1];
                        merged[merged.Count - 1] = (prev.Start, seg.End, prev.MissionID);
                        Debug.WriteLine($"[Timeline] UAV{uavID}: flickering 세그먼트 병합 (Mission {seg.MissionID}, {duration}ms → Mission {prev.MissionID}에 흡수)");
                    }
                    else
                    {
                        merged.Add(seg);
                    }
                }

                // 3단계: 병합 후 인접한 동일 임무 세그먼트 통합
                var final = new List<(ulong Start, ulong End, ulong MissionID)>();
                foreach (var seg in merged)
                {
                    if (final.Count > 0 && final[final.Count - 1].MissionID == seg.MissionID)
                    {
                        // 같은 임무 → 합치기
                        var prev = final[final.Count - 1];
                        final[final.Count - 1] = (prev.Start, seg.End, prev.MissionID);
                    }
                    else
                    {
                        final.Add(seg);
                    }
                }

                result[uavID] = final;
            }

            return result;
        }

        /// <summary>
        /// 빌드된 타임라인에서 특정 임무의 가장 긴 세그먼트를 조회
        /// (같은 missionID가 flickering으로 여러 번 나타날 경우 가장 긴 구간이 실제 수행 구간)
        /// </summary>
        private static (ulong Start, ulong End) GetMissionTimeRangeFromTimeline(
            ulong individualMissionID, uint uavID,
            Dictionary<uint, List<(ulong Start, ulong End, ulong MissionID)>> timelines)
        {
            if (!timelines.ContainsKey(uavID))
                return (0, 0);

            var matching = timelines[uavID]
                .Where(s => s.MissionID == individualMissionID)
                .ToList();

            if (matching.Count == 0)
                return (0, 0);

            // 가장 긴 세그먼트 반환
            var longest = matching.OrderByDescending(s => s.End - s.Start).First();
            return (longest.Start, longest.End);
        }

        /// <summary>
        /// 0501 임무 진행 상태 데이터 로드
        /// 각 타임스탬프별로 UAV가 수행 중인 individualMissionID를 추출
        /// BuildMissionTimelines의 입력으로 사용
        /// </summary>
        public static List<MissionStatusEntry> LoadMissionStatus(string scenarioDir)
        {
            var list = new List<MissionStatusEntry>();
            string targetDir = FindTargetDirectory(scenarioDir, "0501");
            if (string.IsNullOrEmpty(targetDir))
            {
                Debug.WriteLine("[MissionStatus] 0501 폴더를 찾을 수 없음");
                return list;
            }

            var files = Directory.GetFiles(targetDir, "*.json");
            foreach (var file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    JToken token = JToken.Parse(content);
                    JArray jsonArray = token is JArray ? (JArray)token : new JArray(token);

                    foreach (var item in jsonArray)
                    {
                        ulong timestamp = item["timestamp"]?.Value<ulong>() ?? 0;
                        var progressList = item["individualMissionProgressStatusList"] as JArray;
                        if (progressList == null) continue;

                        foreach (var entry in progressList)
                        {
                            uint aircraftID = entry["aircraftID"]?.Value<uint>() ?? 0;
                            var currentMission = entry["currentIndividualMission"];
                            ulong missionID = currentMission?["individualMissionID"]?.Value<ulong>() ?? 0;

                            if (aircraftID > 0 && missionID > 0)
                            {
                                list.Add(new MissionStatusEntry
                                {
                                    Timestamp = timestamp,
                                    AircraftID = aircraftID,
                                    IndividualMissionID = missionID
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MissionStatus] 0501 파싱 오류 ({Path.GetFileName(file)}): {ex.Message}");
                }
            }

            Debug.WriteLine($"[MissionStatus] 총 {list.Count}건 로드 (파일 {files.Length}개)");
            return list.OrderBy(s => s.Timestamp).ToList();
        }

        /// <summary>
        /// 0501 임무 상태 데이터 엔트리
        /// </summary>
        public class MissionStatusEntry
        {
            public ulong Timestamp { get; set; }
            public uint AircraftID { get; set; }
            public ulong IndividualMissionID { get; set; }
        }

        /// <summary>
        /// 임무 타입별 성공 조건 평가
        /// - 영역수색/통로정찰: 관측달성도(SR 적용) >= 95%
        /// - 표적추적: 카메라 중심~표적 거리 100m 이내 + LOS 유지
        /// - 이동/엄호/은엄폐: 수행 완료로 간주
        /// </summary>
        private static async Task<(bool IsSuccess, string Details)> EvaluateMissionSuccessAsync(
            IndividualMission mission, List<FlightData> flightData, List<RealTargetData> targetData,
            uint uavID, ulong startTs, ulong endTs, string srtmFilePath)
        {
            MissionType type = (MissionType)mission.individualMissionInfo.individualMissionType;

            switch (type)
            {
                case MissionType.AreaSearch:
                case MissionType.AreaPatrol:
                case MissionType.CorridorRecon:
                    // ★ [디버그] 개별임무 영역 좌표 확인
                    var mInfo = mission.individualMissionInfo;
                    if (mInfo.areaList?.Count > 0)
                    {
                        var firstCoord = mInfo.areaList[0].coordinateList?.FirstOrDefault();
                        var coordCount = mInfo.areaList.Sum(a => a.coordinateList?.Count ?? 0);
                        Debug.WriteLine($"[Area Debug] 임무 {mission.individualMissionID} (UAV{uavID}): " +
                            $"areaList={mInfo.areaList.Count}개, 좌표수={coordCount}, " +
                            $"첫좌표=({firstCoord?.latitude:F6}, {firstCoord?.longitude:F6})");
                    }
                    else if (mInfo.lineList?.Count > 0)
                    {
                        var firstCoord = mInfo.lineList[0].coordinateList?.FirstOrDefault();
                        var coordCount = mInfo.lineList.Sum(l => l.coordinateList?.Count ?? 0);
                        Debug.WriteLine($"[Line Debug] 임무 {mission.individualMissionID} (UAV{uavID}): " +
                            $"lineList={mInfo.lineList.Count}개, 좌표수={coordCount}, width={mInfo.lineList[0].width}, " +
                            $"첫좌표=({firstCoord?.latitude:F6}, {firstCoord?.longitude:F6})");
                    }

                    // 관측달성도 (공간해상도 반영) 계산 - 임무 시간 범위로 필터링
                    uint combinedScore = CoverageCalculator.getIndividualMissionCoverageWithSRScore(mission, flightData, uavID, startTs, endTs);
                    // ★ [디버그] 시간 필터 없는 점수와 비교
                    uint noFilterScore = CoverageCalculator.getIndividualMissionCoverageWithSRScore(mission, flightData, uavID);
                    Debug.WriteLine($"[Coverage Result] 임무 {mission.individualMissionID} (UAV{uavID}): " +
                        $"시간필터O={combinedScore}%, 시간필터X={noFilterScore}%");

                    bool isCovSuccess = combinedScore >= 95;
                    string covReason = isCovSuccess ? "" : $"\n  [실패] 관측달성도 {combinedScore}% < 기준 95%";

                    return (isCovSuccess, $"관측달성도 : {combinedScore}%{covReason}");

                case MissionType.TargetTracking:
                case MissionType.PointRecon:
                    // 표적 추적/좌표점정찰 성공 여부 계산
                    var (isTrackSuccess, trackDetail) = await CheckTargetTrackingSuccessDetailAsync(
                        mission, flightData, targetData, uavID, startTs, endTs, srtmFilePath);

                    return (isTrackSuccess, trackDetail);

                case MissionType.Movement:
                    // 이동은 웨이포인트 도달 여부
                    return (true, "경로 이탈 : 없음\n웨이포인트 : 도달");

                case MissionType.TargetAttack:
                case MissionType.Cover:
                case MissionType.Conceal:
                    return (true, "수행 완료");

                default:
                    return (true, "해당 없음");
            }
        }

        /// <summary>
        /// 표적추적/좌표점정찰 성공 판정
        /// 조건: 카메라 중심~표적 3D 거리 100m 이내 진입 후 유지 + LOS(지형 차폐 없음)
        /// 100m 진입 후 이탈 시점에서 즉시 실패 반환
        /// </summary>
        private static async Task<(bool IsSuccess, string Details)> CheckTargetTrackingSuccessDetailAsync(
            IndividualMission mission, List<FlightData> flightData, List<RealTargetData> targetData,
            uint uavID, ulong startTs, ulong endTs, string srtmFilePath)
        {
            MissionType type = (MissionType)mission.individualMissionInfo.individualMissionType;
            string missionLabel = type == MissionType.TargetTracking ? "표적추적" : "좌표점정찰";

            uint targetID = mission.individualMissionInfo.targetID ?? 0;
            if (targetID == 0)
                return (false, $"[실패] 표적 ID 미지정");

            // 유효 구간 내의 UAV 데이터만 필터링
            var segmentData = flightData
                .Where(f => f.AircraftID == uavID && f.Timestamp >= startTs && f.Timestamp <= endTs)
                .OrderBy(f => f.Timestamp).ToList();

            if (segmentData.Count == 0)
                return (false, $"[실패] 구간 내 비행 데이터 없음");

            bool hasEntered100m = false;
            int totalChecks = 0;
            int within100mCount = 0;
            double minDist = double.MaxValue;
            double maxDist = 0;
            double lastFailDist = 0;
            string failReason = "";

            foreach (var uavLog in segmentData)
            {
                if (uavLog.CameraDataLog == null || uavLog.FlightDataLog == null) continue;

                var targetsAtTime = FindClosestTargetEntry(uavLog.Timestamp, targetData);
                var target = targetsAtTime.FirstOrDefault(t => t.Unit1TargetID == targetID);
                if (target == null) continue;

                totalChecks++;

                float camCenterLat = uavLog.CameraDataLog.CameraCenterPoint.Latitude;
                float camCenterLon = uavLog.CameraDataLog.CameraCenterPoint.Longitude;
                float uavAlt = uavLog.FlightDataLog.Altitude;

                float targetLat = (float)target.Latitude;
                float targetLon = (float)target.Longitude;
                float targetAlt = (float)target.Altitude;

                double dist3D = Calculate3DDistanceMeters(camCenterLat, camCenterLon, uavAlt, targetLat, targetLon, targetAlt);

                if (dist3D < minDist) minDist = dist3D;
                if (dist3D > maxDist) maxDist = dist3D;
                if (dist3D <= 100.0) within100mCount++;

                bool isLosClear = await CommunicationCalculator.CheckLineOfSight(
                    camCenterLat, camCenterLon, uavAlt, targetLat, targetLon, targetAlt, srtmFilePath);

                bool isConditionMet = (dist3D <= 100.0) && isLosClear;

                if (!hasEntered100m)
                {
                    if (isConditionMet) hasEntered100m = true;
                }
                else
                {
                    if (!isConditionMet)
                    {
                        lastFailDist = dist3D;
                        failReason = !isLosClear
                            ? $"[실패] LOS 차폐 발생 (거리 {dist3D:F0}m)"
                            : $"[실패] 표적거리 {dist3D:F0}m > 기준 100m";
                        // 상세 정보 포함하여 반환
                        return (false, $"표적거리 : 최소 {minDist:F0}m / 최대 {maxDist:F0}m\n{failReason}");
                    }
                }
            }

            if (totalChecks == 0)
                return (false, $"[실패] 표적(ID:{targetID}) 데이터 매칭 불가");

            if (!hasEntered100m)
                return (false, $"표적거리 : 최소 {minDist:F0}m\n[실패] 100m 이내 진입 불가 (최소거리 {minDist:F0}m)");

            return (true, $"표적거리 : 100m 이내 유지 ({within100mCount}/{totalChecks} 프레임)");
        }

        #region Helper Methods (Math & Search)

        // 시간상 가장 가까운 타겟 프레임 찾기 (기존 코드 재활용)
        private static List<Target> FindClosestTargetEntry(ulong flightTimestamp, List<RealTargetData> sortedTargetDataList)
        {
            if (!sortedTargetDataList.Any()) return new List<Target>();

            var targetTimestamps = sortedTargetDataList.Select(e => {
                ulong ts = (ulong)e.Timestamp;
                return ts < 10000000000 ? ts * 1000 : ts;
            }).ToList();

            int idx = targetTimestamps.BinarySearch(flightTimestamp);
            if (idx < 0) idx = ~idx;

            if (idx == 0) return sortedTargetDataList[0].TargetList ?? new List<Target>();
            if (idx == sortedTargetDataList.Count) return sortedTargetDataList.Last().TargetList ?? new List<Target>();

            long diffBefore = Math.Abs((long)flightTimestamp - (long)targetTimestamps[idx - 1]);
            long diffAfter = Math.Abs((long)flightTimestamp - (long)targetTimestamps[idx]);

            return diffBefore <= diffAfter ? sortedTargetDataList[idx - 1].TargetList ?? new List<Target>() : sortedTargetDataList[idx].TargetList ?? new List<Target>();
        }

        // 수정 전 코드:
        //   private static double Calculate3DDistanceMeters(float lat1, float lon1, float alt1, float lat2, float lon2, float alt2)
        //   {
        //       var dLat = ToRadians(lat2 - lat1);
        //       var dLon = ToRadians(lon2 - lon1);
        //       var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
        //             + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        //       var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        //       double distance2D = EarthRadiusMeters * c;
        //       double altDiff = Math.Abs(alt1 - alt2);
        //       return Math.Sqrt((distance2D * distance2D) + (altDiff * altDiff));
        //   }
        //
        // 문제: 한 줄에 삼각함수 4개 체이닝 시 Debug는 중간값을 메모리에 저장(64비트 반올림),
        //   Release는 레지스터 유지(정밀도 변동 또는 연산 재배치). float 파라미터의
        //   암시적 double 승격 시점도 JIT 최적화에 따라 달라짐.
        // 수정: float→double 명시적 캐스팅, 중간값을 개별 변수에 저장하여 반올림 순서 고정.
        private static double Calculate3DDistanceMeters(float lat1, float lon1, float alt1, float lat2, float lon2, float alt2)
        {
            double dLat = ToRadians((double)lat2 - (double)lat1);
            double dLon = ToRadians((double)lon2 - (double)lon1);
            double sinDLat = Math.Sin(dLat / 2);
            double cosLat1 = Math.Cos(ToRadians((double)lat1));
            double cosLat2 = Math.Cos(ToRadians((double)lat2));
            double sinDLon = Math.Sin(dLon / 2);
            double a = (sinDLat * sinDLat) + (cosLat1 * cosLat2 * sinDLon * sinDLon);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance2D = EarthRadiusMeters * c;

            double altDiff = Math.Abs((double)alt1 - (double)alt2);
            return Math.Sqrt((distance2D * distance2D) + (altDiff * altDiff));
        }

        private static double ToRadians(double angle) => Math.PI * angle / 180.0;
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        #endregion

        // 1. 개별 임무(0302) 로드 및 ID 주입 함수
        public static List<IndividualMission> LoadIndividualMissions(string scenarioDir)
        {
            var resultList = new List<IndividualMission>();
            string targetDir = FindTargetDirectory(scenarioDir, "IndividualMissionPlan");
            if (string.IsNullOrEmpty(targetDir)) targetDir = FindTargetDirectory(scenarioDir, "0302");
            if (string.IsNullOrEmpty(targetDir)) return resultList;

            var files = Directory.GetFiles(targetDir, "*.json");
            foreach (var filepath in files)
            {
                try
                {
                    string jsonText = File.ReadAllText(filepath);

                    // ★ 수정: 배열([])인지 객체({})인지 확인하여 처리
                    var token = JToken.Parse(jsonText);
                    List<IndividualMissionPackage> packages = new List<IndividualMissionPackage>();

                    if (token.Type == JTokenType.Array)
                    {
                        // 배열이면 그대로 리스트로 변환
                        packages = token.ToObject<List<IndividualMissionPackage>>();
                    }
                    else if (token.Type == JTokenType.Object)
                    {
                        // 단일 객체면 리스트에 하나 추가
                        var singlePackage = token.ToObject<IndividualMissionPackage>();
                        if (singlePackage != null)
                        {
                            packages.Add(singlePackage);
                        }
                    }

                    // 데이터 추출 및 ID 주입
                    if (packages != null)
                    {
                        foreach (var package in packages)
                        {
                            if (package.individualMissionList != null)
                            {
                                foreach (var mission in package.individualMissionList)
                                {
                                    mission.aircraftID = package.aircraftID; // ID 주입
                                    resultList.Add(mission);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 에러 발생 시 로그 출력 (디버깅용)
                    Console.WriteLine($"Error parsing IndividualMission {filepath}: {ex.Message}");
                }
            }
            return resultList;
        }

        // 2. 비행 데이터(0401) 로드 함수
        public static List<FlightData> LoadFlightData(string scenarioDir)
        {
            var list = new List<FlightData>();
            string targetDir = FindTargetDirectory(scenarioDir, "0401");
            if (string.IsNullOrEmpty(targetDir)) return list;

            var files = Directory.GetFiles(targetDir, "*.json");
            foreach (var file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);

                    JToken token = JToken.Parse(content);
                    JArray jsonArray = token is JArray ? (JArray)token : new JArray(token);

                    foreach (var item in jsonArray)
                    {
                        ulong timestamp = item["timestamp"]?.Value<ulong>() ?? 0;

                        // agentStateList 배열 순회
                        var agentList = item["agentStateList"] as JArray;
                        if (agentList == null) continue;

                        foreach (var agent in agentList)
                        {
                            // 1. 기본 정보 추출
                            uint aircraftID = agent["aircraftID"]?.Value<uint>() ?? 0;
                            bool isUnmanned = agent["isUnmanned"]?.Value<bool>() ?? false;

                            // UAV(무인기)가 아니면 모드 정보가 없을 수 있음 (mannedInfo 등)
                            // 필요하다면 여기서 if (aircraftID <= 3) continue; 등을 할 수도 있음

                            var flightData = new FlightData
                            {
                                Timestamp = timestamp,
                                AircraftID = aircraftID,
                                FlightDataLog = new FlightDataLog()
                            };

                            // 2. 좌표 추출
                            var coord = agent["coordinate"];
                            if (coord != null)
                            {
                                flightData.FlightDataLog.Latitude = coord["latitude"]?.Value<float?>() ?? 0f;
                                flightData.FlightDataLog.Longitude = coord["longitude"]?.Value<float?>() ?? 0f;
                                flightData.FlightDataLog.Altitude = coord["altitude"]?.Value<float?>() ?? 0f;
                            }

                            // 3. ★ 모드 정보 추출 (여기가 핵심!) ★
                            // JSON 구조: agent -> unmannedInfo -> flightMode
                            // JSON 구조: agent -> unmannedInfo -> sensorInfo -> operationalMode
                            var unmannedInfo = agent["unmannedInfo"];
                            if (unmannedInfo != null && unmannedInfo.Type != JTokenType.Null)
                            {
                                flightData.FlightMode = unmannedInfo["flightMode"]?.Value<int?>() ?? 0;

                                var sensorInfo = unmannedInfo["sensorInfo"];
                                if (sensorInfo != null && sensorInfo.Type != JTokenType.Null)
                                {
                                    flightData.PayloadMode = sensorInfo["operationalMode"]?.Value<int?>() ?? 0;

                                    // CameraDataLog 파싱 (footprintCornerList → 4코너 + centerCoordinate)
                                    var corners = sensorInfo["footprintCornerList"] as JArray;
                                    var center = sensorInfo["centerCoordinate"];
                                    if (corners != null && corners.Count >= 4)
                                    {
                                        flightData.CameraDataLog = new CameraDataLog
                                        {
                                            CameraTopLeft = new CameraPoint
                                            {
                                                Latitude = corners[0]["latitude"]?.Value<float?>() ?? 0f,
                                                Longitude = corners[0]["longitude"]?.Value<float?>() ?? 0f
                                            },
                                            CameraTopRight = new CameraPoint
                                            {
                                                Latitude = corners[1]["latitude"]?.Value<float?>() ?? 0f,
                                                Longitude = corners[1]["longitude"]?.Value<float?>() ?? 0f
                                            },
                                            CameraBottomLeft = new CameraPoint
                                            {
                                                Latitude = corners[2]["latitude"]?.Value<float?>() ?? 0f,
                                                Longitude = corners[2]["longitude"]?.Value<float?>() ?? 0f
                                            },
                                            CameraBottomRight = new CameraPoint
                                            {
                                                Latitude = corners[3]["latitude"]?.Value<float?>() ?? 0f,
                                                Longitude = corners[3]["longitude"]?.Value<float?>() ?? 0f
                                            },
                                            CameraCenterPoint = center != null && center.Type != JTokenType.Null ? new CameraPoint
                                            {
                                                Latitude = center["latitude"]?.Value<float?>() ?? 0f,
                                                Longitude = center["longitude"]?.Value<float?>() ?? 0f
                                            } : new CameraPoint()
                                        };
                                    }
                                }
                            }

                            list.Add(flightData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error parsing FlightData {file}: {ex.Message}");
                }
            }
            return list.OrderBy(f => f.Timestamp).ToList();
        }

        // 3. 타겟 데이터 로드 함수
        public static List<RealTargetData> LoadRealTargetData(string scenarioDir)
        {
            var list = new List<RealTargetData>();
            string targetFilePattern = "TargetData_Truth*.json";

            // 최종적으로 파일을 찾을 후보 디렉토리 리스트
            var candidateDirectories = new List<string>();

            // 1. [기존] 현재 시나리오 폴더 내부 (raw/Scenario_A/RealTarget)
            candidateDirectories.Add(scenarioDir);
            candidateDirectories.Add(Path.Combine(scenarioDir, "RealTarget"));
            candidateDirectories.Add(Path.Combine(scenarioDir, "target"));

            // 2. [추가] 상위 폴더로 이동하여 'target' 폴더 탐색 (Parallel 구조 대응)
            // 현재: ...\logs\raw\Scenario_2025-11-27T101837
            // 목표: ...\logs\target\Scenario_2025-11-27T101955
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(scenarioDir);

                // raw 폴더로 이동 (.../logs/raw)
                DirectoryInfo? rawDir = dirInfo.Parent;

                // logs 폴더로 이동 (.../logs)
                DirectoryInfo? logsDir = rawDir?.Parent;

                if (logsDir != null)
                {
                    // logs/target 경로 생성
                    string parallelTargetDir = Path.Combine(logsDir.FullName, "target");

                    if (Directory.Exists(parallelTargetDir))
                    {
                        // target 폴더 안에 있는 모든 Scenario_... 폴더들을 후보에 추가
                        var subDirs = Directory.GetDirectories(parallelTargetDir, "Scenario_*");
                        candidateDirectories.AddRange(subDirs);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[경로 탐색 오류] 상위 폴더 접근 실패: {ex.Message}");
            }

            // 3. 수집된 후보 디렉토리들에서 실제 파일 찾기
            string[] files = Array.Empty<string>();

            foreach (var dir in candidateDirectories)
            {
                if (Directory.Exists(dir))
                {
                    var foundFiles = Directory.GetFiles(dir, targetFilePattern, SearchOption.AllDirectories); // 하위까지 깊게 검색
                    if (foundFiles.Length > 0)
                    {
                        // 파일을 찾았으면 루프 종료 (가장 먼저 발견된 폴더 우선)
                        // 만약 raw와 target의 시나리오 폴더명이 타임스탬프 때문에 다르더라도, 
                        // target 폴더 안에 있는 첫 번째 시나리오의 파일을 가져오게
                        files = foundFiles;
                        Console.WriteLine($"[성공] 표적 파일 찾음: {dir}");
                        break;
                    }
                }
            }

            // 파일이 없으면 빈 리스트 반환
            if (files.Length == 0)
            {
                Console.WriteLine($"[오류] 표적 데이터 파일을 찾을 수 없습니다. (검색 경로: {scenarioDir} 및 상위 target 폴더)");
                return list;
            }

            // 4. 파일 파싱 (기존 로직 동일)
            var sortedFiles = files.OrderBy(f => GetFileSequenceNumber(f, "TargetData_Truth")).ToList();

            foreach (var file in sortedFiles)
            {
                try
                {
                    string content = File.ReadAllText(file);

                    // 배열 보정 로직 (혹시 모를 포맷 에러 방지)
                    if (!content.TrimStart().StartsWith("[")) content = "[" + content + "]";

                    JToken token = JToken.Parse(content);
                    JArray jsonArray = token is JArray ? (JArray)token : new JArray(token);

                    foreach (var item in jsonArray)
                    {
                        var targetData = item.ToObject<RealTargetData>();
                        if (targetData != null)
                        {
                            list.Add(targetData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[오류] 표적 파일 파싱 실패 ({Path.GetFileName(file)}): {ex.Message}");
                }
            }

            return list.OrderBy(t => t.Timestamp).ToList();
        }

        // [헬퍼] 파일명 정렬용 (기존 Utils에 있다면 재사용, 없다면 여기에 추가)
        private static int GetFileSequenceNumber(string filePath, string filePrefix)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.Equals(filePrefix, StringComparison.OrdinalIgnoreCase)) return 0;

            // "TargetData_Truth_1" -> 1 추출
            string suffix = fileName.Replace(filePrefix, "").Replace("_", "");
            return int.TryParse(suffix, out int result) ? result : int.MaxValue;
        }

        /// <summary>
        /// [핵심] 시나리오 폴더 내부에서 특정 이름의 데이터 폴더를 동적으로 찾기
        /// 예: scenarioDir/SBC3/targetName 또는 scenarioDir/targetName 등을 탐색
        /// </summary>
        public static string FindTargetDirectory(string rootScenarioDir, string targetFolderName)
        {
            if (string.IsNullOrEmpty(rootScenarioDir) || !Directory.Exists(rootScenarioDir)) return null;

            // 1. 루트 바로 아래에 있는지 확인 (예: Scenario_.../MissionPlan)
            string directPath = Path.Combine(rootScenarioDir, targetFolderName);
            if (Directory.Exists(directPath)) return directPath;

            // 2. 1단계 하위 폴더들(SBC3, SBC1 등) 안에 있는지 확인
            try
            {
                var subDirs = Directory.GetDirectories(rootScenarioDir);
                foreach (var subDir in subDirs)
                {
                    string deepPath = Path.Combine(subDir, targetFolderName);
                    if (Directory.Exists(deepPath)) return deepPath;
                }
            }
            catch { }

            return null; // 못 찾음
        }

    }


}
