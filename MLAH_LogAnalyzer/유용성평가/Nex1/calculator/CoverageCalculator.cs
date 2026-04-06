using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Noding;
using System.Collections.ObjectModel;
// [확인 후 삭제] 미사용 using
//using DevExpress.XtraSpreadsheet.Model;

namespace MLAH_LogAnalyzer
{

    #region 클래스 정의
    /// <summary>
    /// UAV 커버리지 결과 데이터
    /// </summary>
    public class UavCoverageResult
    {
        public List<ulong> Timestamps { get; set; } = new List<ulong>();
        public List<float> Coverages { get; set; } = new List<float>();
        public Geometry CumulativeFootprints { get; set; } = new GeometryFactory().CreatePolygon();
        public Geometry CumulativeUavFootprints { get; set; } = new GeometryFactory().CreatePolygon();
        public float CumulativeCoveragePercentage { get; set; } = 0.0f;
    }

    /// <summary>
    /// 좌표 출력 데이터
    /// </summary>
    public class CoordinateOutput
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }
    }

    /// <summary>
    /// 미촬영 지역 출력 데이터 (AreaN 형태로 동적 속성 지원)
    /// </summary>
    public class MissingRegionOutput : Dictionary<string, List<CoordinateOutput>>
    {
    }

    /// <summary>
    /// 커버리지 데이터 출력
    /// </summary>
    public class CoverageDataOutput
    {
        public ulong Timestamp { get; set; }
        public uint MissionSegmentID { get; set; }
        public float Coverage { get; set; }
    }

    /// <summary>
    /// 미션 세그먼트별 상세 데이터
    /// </summary>
    public class MissionSegmentData
    {
        public float Coverage { get; set; }
        public float RequiredArea { get; set; }
        public float FilmedArea { get; set; }
    }

    /// <summary>
    /// 커버리지 분석 결과
    /// </summary>
    public class AnalysisResult
    {
        public List<CoverageDataOutput> CoverageDatas { get; set; } = new List<CoverageDataOutput>();
        public Dictionary<uint, MissionSegmentData> MissionSegmentDatas { get; set; } = new Dictionary<uint, MissionSegmentData>();
        public MissingRegionOutput MissingRegions { get; set; } = new MissingRegionOutput();
        public float RequiedArea { get; set; }
        public float FilmedArea { get; set; }
        public uint Score { get; set; }

        //무인기 정보
        public ObservableCollection<EvaluationUAVInfo> UavInfos = new ObservableCollection<EvaluationUAVInfo>();
    }

    public class EvaluationUAVInfo
    {
        public int UAVID { get; set; }
        public ObservableCollection<UavSnapshot> Snapshots = new ObservableCollection<UavSnapshot>();
    }

    /// <summary>
    /// [수정 제안] 특정 타임스탬프의 UAV 상태 정보 (항적 + 촬영영역)
    /// </summary>
    public class UavSnapshot : CommonBase
    {
        public ulong Timestamp { get; set; }

        private Coordinate _Position = new Coordinate();
        public Coordinate Position
        {
            get
            {
                return _Position;
            }
            set
            {
                _Position = value;
                OnPropertyChanged("Position");
            }
        }
        public CameraDataLog Footprint { get; set; } // (기존 CameraDataLog 클래스 사용), 촬영 정보가 없으면 null
    }

    /// <summary>
    /// 좌표 비교를 위한 Comparer
    /// </summary>
    public class CoordinateComparer : IEqualityComparer<NetTopologySuite.Geometries.Coordinate>
    {
        private const double TOLERANCE = 0.0000001;

        public bool Equals(NetTopologySuite.Geometries.Coordinate? x, NetTopologySuite.Geometries.Coordinate? y)
        {
            if (x == null || y == null) return x == y;
            return Math.Abs(x.X - y.X) < TOLERANCE && Math.Abs(x.Y - y.Y) < TOLERANCE;
        }

        public int GetHashCode(NetTopologySuite.Geometries.Coordinate obj)
        {
            return obj.X.GetHashCode() ^ obj.Y.GetHashCode();
        }
    }

    /// <summary>
    /// IndividualMissionPlan JSON 구조 매핑
    /// </summary>
    public class IndividualMissionPackage
    {
        public ulong timestamp { get; set; }
        public string Source { get; set; }
        public ulong individualMissionPackageID { get; set; }
        public uint aircraftID { get; set; }
        public List<IndividualMission> individualMissionList { get; set; }
    }

    /// <summary>
    /// 개별 임무 정보
    /// </summary>
    public class IndividualMission
    {
        public ulong individualMissionID { get; set; }
        public bool isDone { get; set; }
        public RelatedMission relatedMission { get; set; }
        public IndividualMissionInfo individualMissionInfo { get; set; }
        public ulong pathID { get; set; }
        public uint aircraftID { get; set; }
    }

    /// <summary>
    /// 개별 임무 상세 정보
    /// </summary>
    public class IndividualMissionInfo
    {
        public int individualMissionType { get; set; }
        public int patternType { get; set; }
        public bool autoZoomIn { get; set; }
        public List<CoordinateData>? coordinateList { get; set; }
        public List<LineData>? lineList { get; set; }
        public List<AreaData>? areaList { get; set; }
        public uint? targetID { get; set; }
    }

    /// <summary>
    /// 좌표 데이터
    /// </summary>
    public class CoordinateData
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double altitude { get; set; }
    }

    public class LineData
    {
        public float width { get; set; }
        public List<CoordinateData> coordinateList { get; set; }

    }

    public class AreaData
    {
        public List<CoordinateData> coordinateList { get; set; }
        public bool isHole { get; set; }
    }

    /// <summary>
    /// 관련 임무 정보
    /// </summary>
    public class RelatedMission
    {
        public int relatedMissionType { get; set; }
        public ulong inputMissionID { get; set; }
        public ulong priorMissionID { get; set; }
    }
    #endregion

    /// <summary>
    /// 관측달성도(Coverage) 계산기
    ///
    /// 개요
    ///   UAV 카메라 촬영 영역(footprint)과 임무 영역(mission polygon)의 교차 비율 계산.
    ///   촬영유효도(SR, Spatial Resolution) 필터를 적용하는 버전과 미적용 버전이 각각 존재.
    ///
    /// SR(촬영유효도) 필터
    ///   각 카메라 프레임의 GSD(Ground Sample Distance, cm/px)를 계산하여
    ///   기준값(기본 7.895 cm/px) 초과 프레임은 "해상도 부족"으로 판정, 촬영 영역에서 제외.
    ///   GSD = 카메라 footprint 실측 크기(m) / 센서 픽셀 수 → cm/px 변환
    ///   SRCalculator 클래스에서 HasValidCorners → CalculateDistanceMeters → CalculateGsdCmPerPixel 순서.
    ///
    /// 5Hz → 15Hz 보간
    ///   0401 카메라 데이터는 5Hz(초당 5프레임)로 기록. 프레임 간 간극이 커서
    ///   커버리지가 실제보다 낮게 나올 수 있어 인접 프레임 사이에
    ///   2개 보간 프레임 삽입(3등분)하여 15Hz로 확장.
    ///   보간 → SR 필터 순서로 수행.
    ///
    /// 메서드 구분 (호출처별)
    ///
    ///   유용성평가 점수 경로 (SR 적용)
    ///     getCoverageScore(FlightData, MissionDetail, ...)
    ///       → CalculateTotalCoveredAreaOptimized (전체 UAV 합산, SR 필터)
    ///       → 레이더 차트 점수
    ///
    ///   유용성평가 상세분석 경로 (SR 미적용)
    ///     getCoverage(FlightData, MissionDetail)
    ///       → ProcessFlightDataForCoverage (시간별 추이, 세그먼트별 점수, 미촬영 지역)
    ///       → 관측달성도 탭 차트/지도 UI
    ///       → 상세분석은 실제 촬영 전체 영역을 보여주는 것이 목적이므로 SR 미적용
    ///
    ///   성능분석 촬영범위 (SR 미적용)
    ///     getIndividualMissionCoverageScore(IndividualMission, ...)
    ///       → 개별임무 1건 + UAV 1대, 촬영 범위(%)
    ///       → 타임라인 바 "촬영범위" 메트릭
    ///
    ///   성능분석 관측달성도 (SR 적용)
    ///     getIndividualMissionCoverageWithSRScore(IndividualMission, ...)
    ///       → 개별임무 1건 + UAV 1대, SR 필터 후 관측달성도(%)
    ///       → 타임라인 바 "관측달성도" 메트릭
    ///
    /// 공통 계산 흐름
    ///   1. 임무 영역 폴리곤 생성 (CreateMissionPolygon / CreateIndividualMissionPolygon)
    ///   2. 0401 카메라 데이터 필터링 (UAV ID, 시간 범위)
    ///   3. 5Hz → 15Hz 보간 (ExpandFlightDataWithInterpolation)
    ///   4. SR 적용 시 GSD 기준 초과 프레임 제외
    ///   5. 카메라 footprint → 폴리곤 변환 (CreateFootprintPolygons)
    ///   6. 폴리곤 병합 (CascadedUnion + Douglas-Peucker 단순화)
    ///   7. 임무영역 교차 촬영영역 → 커버리지 비율 산출
    /// </summary>
    public static class CoverageCalculator
    {
        private static readonly GeometryFactory GeometryFactory = new GeometryFactory();
        private const float AREA_CONVERSION_FACTOR = 111.0f * 111.0f; // 도(degree)² → km² 변환 계수

        /// <summary>
        /// 유용성평가 - 시나리오 데이터 커버리지 상세분석 (SR 미적용)
        /// getCoverage() 호출하여 시간별 추이, 세그먼트별 점수, 미촬영 지역 등 반환.
        /// </summary>
        //public static async Task<AnalysisResult?> getCoverageData(string baseDirectory, int scenarioNumber)
        public static async Task<AnalysisResult?> getCoverageData(ScenarioData scenarioData)
        {
            // [수정] baseDirectory 전달
            //ScenarioData? scenarioData = await Utils.LoadScenarioData(baseDirectory, scenarioNumber);

            if (scenarioData == null)
            {
                //Console.WriteLine($"시나리오 {scenarioNumber} 데이터를 로드할 수 없어 커버리지 분석을 수행할 수 없습니다.");
                Console.WriteLine($"시나리오 데이터를 로드할 수 없어 커버리지 분석을 수행할 수 없습니다.");
                return null;
            }

            return getCoverage(scenarioData.FlightData, scenarioData.MissionDetail);
        }

        /// <summary>
        /// 시나리오 데이터 기반 커버리지 점수만 반환 (SR 적용)
        /// getCoverageScore(flightData, missionDetail) 오버로드 호출.
        /// 레이더 차트 점수(0~100)만 필요할 때 사용하는 경량 메서드.
        /// </summary>
        //public static async Task<uint?> getCoverageScore(string baseDirectory, int scenarioNumber)
         public static async Task<uint?> getCoverageScore(ScenarioData scenarioData)
        {
            try
            {
                // await 추가
                //ScenarioData? scenarioData = await Utils.LoadScenarioData(baseDirectory, scenarioNumber);
                if (scenarioData == null) return null;
                return getCoverageScore(scenarioData.FlightData, scenarioData.MissionDetail);
            }
            catch { return null; }
        }

        /// <summary>
        /// 유용성평가 협업기저임무 기준 커버리지 점수 (SR 적용)
        ///
        /// 호출처: 유용성평가 레이더 차트 점수
        /// 계산 흐름:
        ///   1. MissionDetail → 임무 영역 폴리곤 생성
        ///   2. CalculateTotalCoveredAreaOptimized()로 전체 UAV 촬영 영역 계산
        ///      (5Hz→15Hz 보간 → SR 필터(GSD 기준 이하만) → footprint 병합)
        ///   3. 임무영역 교차 촬영영역 / 임무영역 x 100 = 점수
        ///
        /// SR 적용 이유: 레이더 차트 점수는 유효 해상도로 촬영된 비율이므로
        ///              해상도 미달 프레임은 촬영으로 인정하지 않음.
        /// </summary>
        /// <param name="flightDataList">0401 비행/카메라 데이터 (전체 UAV)</param>
        /// <param name="missionDetailList">협업기저임무 영역 정보</param>
        /// <param name="srThreshold">GSD 기준값 (cm/px, 기본 7.895). 0이면 SR 필터 미적용</param>
        /// <param name="camMode">카메라 모드 (1=EO, 2=IR). 센서 해상도가 다르므로 GSD에 영향</param>
        /// <returns>커버리지 점수 (0~100)</returns>
        public static uint getCoverageScore(List<FlightData> flightDataList, List<MissionDetail> missionDetailList,
            float srThreshold = 7.895f, int camMode = 1)
        {
            Geometry? missionPolygon = CreateMissionPolygon(missionDetailList);
            if (missionPolygon == null || missionPolygon.Area <= 0)
                return 0;

            // 전체 UAV 촬영 영역 (SR 필터 + 5Hz→15Hz 보간)
            Geometry totalCoveredArea = CalculateTotalCoveredAreaOptimized(flightDataList, srThreshold, camMode);

            // 임무 영역과 촬영 영역의 교차 비율
            Geometry overallCoveredArea = missionPolygon.Intersection(totalCoveredArea);
            return (uint)Math.Round((overallCoveredArea.Area / missionPolygon.Area) * 100, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 전체 UAV 촬영 영역 계산 (SR 적용)
        ///
        /// getCoverageScore()에서 호출. 전체 UAV(ID>3)의 카메라 footprint 합산 Geometry 반환.
        ///
        /// 처리 순서:
        ///   1. AircraftID > 3 (UAV 4,5,6), CameraDataLog 있는 레코드만 필터링
        ///   2. 5Hz → 15Hz 선형 보간 (ExpandFlightDataWithInterpolation)
        ///   3. 각 프레임에 대해:
        ///      a. SR 적용 시 SRCalculator로 GSD 계산, 기준 초과면 skip
        ///      b. footprint 4개 꼭짓점으로 폴리곤 생성
        ///   4. SIMPLIFY_BATCH_SIZE(500)개마다 배치 병합 + Douglas-Peucker 단순화
        ///      (폴리곤 점 수 폭증 방지)
        ///   5. 최종 MAX_POINTS_THRESHOLD(1000)점 초과 시 추가 단순화
        ///
        /// 반환값은 도(degree) 단위 좌표계 Geometry. 면적은 degree 단위이므로
        /// km 변환 시 AREA_CONVERSION_FACTOR(111 x 111)를 곱해야 함.
        /// </summary>
        /// <param name="flightDataList">0401 비행/카메라 데이터 (전체)</param>
        /// <param name="srThreshold">GSD 기준값 (cm/px, 0이면 SR 필터 미적용)</param>
        /// <param name="camMode">카메라 모드 (1=EO, 2=IR)</param>
        /// <returns>전체 촬영 영역 Geometry (도 단위 좌표계)</returns>
        public static Geometry CalculateTotalCoveredAreaOptimized(List<FlightData> flightDataList,
            float srThreshold = 7.895f, int camMode = 1)
        {
            const int SIMPLIFY_BATCH_SIZE = 500;     // 배치 크기 (병합 주기)
            const double SIMPLIFY_TOLERANCE = 0.0001; // Douglas-Peucker 단순화 허용치 (도 단위, ~11m)
            const int MAX_POINTS_THRESHOLD = 1000;    // 최종 점 수 제한

            var sortedFlightData = flightDataList
                .Where(x => x.AircraftID > 3 && x.CameraDataLog != null && x.Timestamp > 0)
                .OrderBy(x => x.Timestamp)
                .ToList();

            // 5Hz → 15Hz 선형 보간 (카메라 footprint 간격 보정)
            sortedFlightData = ExpandFlightDataWithInterpolation(sortedFlightData);

            Geometry totalCoveredArea = GeometryFactory.CreatePolygon();
            var batchFootprints = new List<Geometry>();
            int processedCount = 0;
            bool applySR = srThreshold > 0; // SR 필터 적용 여부

            foreach (var flightEntry in sortedFlightData)
            {
                var cam = flightEntry.CameraDataLog;
                if (cam == null) continue;

                // SR 필터: GSD 기준 초과 프레임 제외
                if (applySR)
                {
                    if (!SRCalculator.HasValidCorners(cam))
                        continue;

                    double widthMeters = SRCalculator.CalculateDistanceMeters(
                        cam.CameraTopLeft.Latitude, cam.CameraTopLeft.Longitude,
                        cam.CameraTopRight.Latitude, cam.CameraTopRight.Longitude);
                    double heightMeters = SRCalculator.CalculateDistanceMeters(
                        cam.CameraTopLeft.Latitude, cam.CameraTopLeft.Longitude,
                        cam.CameraBottomLeft.Latitude, cam.CameraBottomLeft.Longitude);

                    double gsd = SRCalculator.CalculateGsdCmPerPixel(widthMeters, heightMeters, camMode);
                    if (gsd > srThreshold)
                        continue;
                }

                try
                {
                    var footprintPolygons = CreateFootprintPolygons(new List<CameraDataLog> { cam });
                    if (footprintPolygons.Any())
                    {
                        Geometry currentFootprint = CombinePolygons(footprintPolygons);
                        if (currentFootprint.Area > 0)
                            batchFootprints.Add(currentFootprint);
                    }
                }
                catch { continue; }

                processedCount++;

                if (processedCount % SIMPLIFY_BATCH_SIZE == 0 && batchFootprints.Any())
                {
                    totalCoveredArea = MergeBatchWithSimplification(totalCoveredArea, batchFootprints, SIMPLIFY_TOLERANCE);
                    batchFootprints.Clear();
                }
            }

            if (batchFootprints.Any())
            {
                totalCoveredArea = MergeBatchWithSimplification(totalCoveredArea, batchFootprints, SIMPLIFY_TOLERANCE);
            }

            if (totalCoveredArea.NumPoints > MAX_POINTS_THRESHOLD)
            {
                totalCoveredArea = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(totalCoveredArea, SIMPLIFY_TOLERANCE);
            }

            return totalCoveredArea;
        }

        /// <summary>
        /// 배치를 병합하고 단순화
        /// </summary>
        private static Geometry MergeBatchWithSimplification(Geometry totalArea, List<Geometry> batch, double tolerance)
        {
            //var batchUnion = CascadedUnion(batch);
            //batchUnion = batchUnion.Buffer(0);
            //batchUnion = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(batchUnion, tolerance);

            //totalArea = totalArea.Union(batchUnion);
            //totalArea = totalArea.Buffer(0);
            //totalArea = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(totalArea, tolerance);

            //return totalArea;

            var batchUnion = CascadedUnion(batch);
            batchUnion = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(batchUnion, tolerance);

            try
            {
                totalArea = totalArea.Union(batchUnion);
            }
            catch (NetTopologySuite.Geometries.TopologyException)
            {
                try
                {
                    totalArea = totalArea.Buffer(0).Union(batchUnion.Buffer(0));
                }
                catch
                {
                    // 무시
                }
            }

            totalArea = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(totalArea, tolerance);
            return totalArea;
        }

        /// <summary>
        /// 유용성평가 커버리지 상세 분석 (SR 미적용)
        ///
        /// 호출처: 유용성평가 관측달성도 탭 (차트, 지도, 미촬영 지역 표시)
        ///
        /// 계산 흐름:
        ///   1. MissionDetail → 전체 임무 영역 폴리곤 + 세그먼트별 폴리곤 생성
        ///   2. ProcessFlightDataForCoverage():
        ///      - 5Hz→15Hz 보간 → footprint 생성 → 배치 병합
        ///      - 매 INTERSECTION_INTERVAL(10)번째 타임스탬프마다 세그먼트별 커버리지 계산
        ///      - 나머지는 이전 값 재사용 (성능 최적화)
        ///   3. 전체 점수, 촬영 면적(km2), 세그먼트별 점수/면적, 미촬영 지역 좌표 산출
        ///
        /// SR 미적용 이유: 상세분석 탭은 실제 촬영 전체 영역을 보여주는 것이 목적.
        ///   SR 필터는 점수(getCoverageScore)에만 반영, 상세 UI에서는 필터링 없이 표시.
        ///
        /// 반환값 AnalysisResult 구조:
        ///   - CoverageDatas: 타임스탬프별 세그먼트 커버리지 추이 (차트)
        ///   - MissionSegmentDatas: 세그먼트별 최종 커버리지/면적
        ///   - MissingRegions: 미촬영 지역 좌표 (지도 표시)
        ///   - Score: 전체 커버리지 점수 (0~100)
        ///   - RequiedArea / FilmedArea: 필요면적 / 촬영면적 (km2)
        /// </summary>
        public static AnalysisResult getCoverage(List<FlightData> flightDataList, List<MissionDetail> missionDetailList)
        {
            var analysisResult = new AnalysisResult();

            // 임무 지역 폴리곤 생성
            Geometry? missionPolygon = CreateMissionPolygon(missionDetailList);
            if (missionPolygon == null)
            {
                return analysisResult;
            }

            // MissionSegment별 폴리곤 생성
            Dictionary<uint, Geometry> missionSegmentPolygons = CreateMissionSegmentPolygons(missionDetailList);

            // 총 필요 면적 계산 (km²로 변환)
            analysisResult.RequiedArea = (float)(missionPolygon.Area * AREA_CONVERSION_FACTOR);

            var (segmentCoveredAreas, totalCoveredArea) = ProcessFlightDataForCoverage(flightDataList, missionSegmentPolygons, analysisResult);

            // MissionSegment별 커버리지 데이터 생성 및 세그먼트별 촬영 영역 반환
            //var segmentCoveredAreas = ProcessFlightDataForCoverage(flightDataList, missionSegmentPolygons, analysisResult);

            // 전체 촬영 영역 계산
            //Geometry totalCoveredArea = CalculateTotalCoveredArea(flightDataList);

            // 전체 커버리지 점수 및 촬영 면적 계산
            CalculateOverallCoverageMetrics(missionPolygon, totalCoveredArea, analysisResult);

            // MissionSegment별 커버리지 계산 (세그먼트별 촬영 영역 사용)
            CalculateMissionSegmentCoverages(missionSegmentPolygons, segmentCoveredAreas, analysisResult);

            // 미촬영 지역 정보 생성
            PopulateMissingRegions(analysisResult, missionPolygon, totalCoveredArea);
            return analysisResult;
        }

        /// <summary>
        /// 성능분석 개별임무 커버리지 점수 - 촬영범위 (SR 미적용)
        ///
        /// 호출처: 성능분석 타임라인 바의 "촬영범위(%)" 메트릭
        ///
        /// 계산 흐름:
        ///   1. 개별임무(0302) → 임무 영역 폴리곤 (lineList→직사각형 / areaList→다각형)
        ///   2. 해당 UAV 카메라 데이터를 시간 범위(startTs~endTs)로 필터링
        ///   3. 5Hz → 15Hz 보간
        ///   4. 전체 footprint 폴리곤 변환 → 병합 (SR 필터 없이 전부 포함)
        ///   5. 임무영역 교차 촬영영역 / 임무영역 x 100 = 촬영범위(%)
        ///
        /// SR 미적용 이유: "촬영범위"는 순수하게 카메라가 촬영한 물리적 영역 비율을 나타낸다.
        ///   해상도 충족 여부 무관, 카메라가 해당 영역을 지나갔는지 측정하는 지표.
        ///   SR 적용한 관측달성도는 getIndividualMissionCoverageWithSRScore에서 계산.
        /// </summary>
        /// <param name="individualMission">개별임무 정보 (0302 기반)</param>
        /// <param name="flightDataList">0401 비행/카메라 데이터</param>
        /// <param name="uavID">대상 무인기 ID (4, 5, 6)</param>
        /// <param name="startTs">임무 시작 타임스탬프 (0501 기반, 0이면 전체)</param>
        /// <param name="endTs">임무 종료 타임스탬프</param>
        /// <returns>촬영범위 (0~100)</returns>
        public static uint getIndividualMissionCoverageScore(IndividualMission individualMission, List<FlightData> flightDataList, uint uavID, ulong startTs = 0, ulong endTs = 0)
        {
            if (individualMission == null)
                return 0;

            // 개별임무 영역 폴리곤 (lineList → 직사각형 확장, areaList → 단순 폴리곤)
            var missionPolygon = CreateIndividualMissionPolygon(individualMission);
            if (missionPolygon == null || !missionPolygon.IsValid || missionPolygon.Area <= 0)
                return 0;

            // 임무 시간 범위로 카메라 데이터 필터링
            var query = flightDataList
                .Where(f => f.AircraftID == uavID && f.CameraDataLog != null && f.Timestamp > 0);
            if (startTs > 0 && endTs > startTs)
                query = query.Where(f => f.Timestamp >= startTs && f.Timestamp <= endTs);

            var filteredData = query.OrderBy(f => f.Timestamp).ToList();

            // 5Hz → 15Hz 보간
            filteredData = ExpandFlightDataWithInterpolation(filteredData);

            var cameraDataLogs = filteredData.Select(c => c.CameraDataLog).ToList();
            var uavPolygon = CombinePolygons(CreateFootprintPolygons(cameraDataLogs));

            if (uavPolygon.Area <= 0)
                return 0;

            var intersectedArea = SafeIntersection(missionPolygon, uavPolygon);
            return (uint)Math.Round((intersectedArea.Area / missionPolygon.Area) * 100, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 성능분석 개별임무 커버리지 점수 - 관측달성도 (SR 적용)
        ///
        /// 호출처: 성능분석 타임라인 바 "관측달��도" 메트릭
        ///
        /// 계산 흐름:
        ///   1. 개별임무(0302) → 임무 영역 폴리곤 생성
        ///   2. 해당 UAV 카메라 데이터를 시간 범위로 필터링
        ///   3. 5Hz → 15Hz 보간
        ///   4. 각 프레임 SR 필터:
        ///      a. HasValidCorners: footprint 꼭짓점 유효성
        ///      b. CalculateDistanceMeters: 위경도 → 실거리(m)
        ///      c. CalculateGsdCmPerPixel: 실거리 / 센서 픽셀 → GSD(cm/px)
        ///      d. GSD 기준 이하 프레임만 qualifiedCameraLogs에 추가
        ///   5. 유효 프레임만 촬영 영역 폴리곤 생성 → 병합
        ///   6. 임무영역 교차 유효촬영영역 / 임무영역 x 100 = 관측달성도(%)
        ///
        /// getIndividualMissionCoverageScore와 차이:
        ///   - 이 메서드: GSD 기준 충족 프레임만 인정 → 해상도 충족 촬영 비율
        ///   - Score 버전: 전체 프레임 인정 → 물리적 촬영 범위 비율
        ///   동일 임무에서 이 값 <= Score 값. SR로 걸러지는 프레임이 있으므로.
        /// </summary>
        /// <param name="individualMission">개별임무 정보 (0302 기반)</param>
        /// <param name="flightDataList">0401 비행/카메라 데이터</param>
        /// <param name="uavID">대상 무인기 ID (4, 5, 6)</param>
        /// <param name="startTs">임무 시작 타임스탬프 (0이면 전체 시간)</param>
        /// <param name="endTs">임무 종료 타임스탬프</param>
        /// <param name="srThreshold">GSD 기준값 (cm/px, 기본 7.895)</param>
        /// <param name="camMode">카메라 모드 (1=EO, 2=IR)</param>
        /// <returns>관측달성도 (0~100)</returns>
        public static uint getIndividualMissionCoverageWithSRScore(
            IndividualMission individualMission,
            List<FlightData> flightDataList,
            uint uavID,
            ulong startTs = 0,
            ulong endTs = 0,
            float srThreshold = 7.895f,
            int camMode = 1)
        {
            if (individualMission == null)
                return 0;

            var missionPolygon = CreateIndividualMissionPolygon(individualMission);
            if (missionPolygon == null || !missionPolygon.IsValid || missionPolygon.Area <= 0)
                return 0;

            // 시간 필터 + GSD 기준 충족 필터
            var qualifiedCameraLogs = new List<CameraDataLog>();

            var query = flightDataList
                .Where(f => f.AircraftID == uavID && f.CameraDataLog != null && f.Timestamp > 0);
            if (startTs > 0 && endTs > startTs)
                query = query.Where(f => f.Timestamp >= startTs && f.Timestamp <= endTs);

            var uavFlightData = query.OrderBy(f => f.Timestamp).ToList();
            uavFlightData = ExpandFlightDataWithInterpolation(uavFlightData);

            foreach (var fd in uavFlightData)
            {
                var cam = fd.CameraDataLog;
                if (!SRCalculator.HasValidCorners(cam))
                    continue;

                // footprint 실측 크기 → GSD 계산
                double widthMeters = SRCalculator.CalculateDistanceMeters(
                    cam.CameraTopLeft.Latitude, cam.CameraTopLeft.Longitude,
                    cam.CameraTopRight.Latitude, cam.CameraTopRight.Longitude);
                double heightMeters = SRCalculator.CalculateDistanceMeters(
                    cam.CameraTopLeft.Latitude, cam.CameraTopLeft.Longitude,
                    cam.CameraBottomLeft.Latitude, cam.CameraBottomLeft.Longitude);

                double gsd = SRCalculator.CalculateGsdCmPerPixel(widthMeters, heightMeters, camMode);

                // GSD가 기준 이하인 프레임만 유효 촬영으로 인정
                if (gsd <= srThreshold)
                    qualifiedCameraLogs.Add(cam);
            }

            if (qualifiedCameraLogs.Count == 0)
                return 0;

            var uavPolygon = CombinePolygons(CreateFootprintPolygons(qualifiedCameraLogs));
            if (uavPolygon.Area <= 0)
                return 0;

            var intersectedArea = SafeIntersection(missionPolygon, uavPolygon);
            return (uint)Math.Round((intersectedArea.Area / missionPolygon.Area) * 100, MidpointRounding.AwayFromZero);
        }



        /// <summary>
        /// 성능분석 개별임무(0302) → 임무 영역 폴리곤 생성
        ///
        /// getIndividualMissionCoverageScore / getIndividualMissionCoverageWithSRScore 공통 호출.
        ///
        /// 임무 유형별 폴리곤 생성:
        ///   - lineList (통로정찰): 각 선분을 width 기반 직사각형 확장
        ///     UnitPerp() 수직 벡터 → Offset() 반폭 이동 → 4개 꼭짓점 직사각형
        ///   - areaList (영역수색): 좌표 목록을 폴리곤으로 변환
        ///   - 복수 영역이면 CascadedUnion 병합
        ///   - 유효하지 않은 폴리곤은 Buffer(0) 보정
        /// </summary>
        private static Geometry CreateIndividualMissionPolygon(IndividualMission individualMission)
        {
            var mInfo = individualMission.individualMissionInfo;
            var missionGeometries = new List<Geometry>();

            // 1. lineList 처리: width를 적용한 직사각형 폴리곤 생성
            if (mInfo.lineList?.Count > 0)
            {
                foreach (var line in mInfo.lineList)
                {
                    if (line.coordinateList == null || line.coordinateList.Count < 2)
                        continue;

                    var lineCoords = line.coordinateList
                        .Select(c => new NetTopologySuite.Geometries.Coordinate(c.longitude, c.latitude))
                        .ToList();

                    double halfWidthMeters = line.width / 2.0;

                    for (int i = 0; i < lineCoords.Count - 1; i++)
                    {
                        var A = lineCoords[i];
                        var B = lineCoords[i + 1];

                        var (nx, ny) = UnitPerp(A, B);

                        var A1 = Offset(A, nx, ny, halfWidthMeters);
                        var A2 = Offset(A, -nx, -ny, halfWidthMeters);
                        var B1 = Offset(B, nx, ny, halfWidthMeters);
                        var B2 = Offset(B, -nx, -ny, halfWidthMeters);

                        var rectCoords = new NetTopologySuite.Geometries.Coordinate[] { A1, B1, B2, A2, A1 };
                        var rectPolygon = GeometryFactory.CreatePolygon(new LinearRing(rectCoords));
                        missionGeometries.Add(rectPolygon);
                    }
                }
            }
            // 2. areaList 처리: 단순 폴리곤
            else if (mInfo.areaList?.Count > 0)
            {
                foreach (var area in mInfo.areaList)
                {
                    if (area.coordinateList == null || area.coordinateList.Count < 3)
                        continue;

                    var areaCoords = area.coordinateList
                        .Select(c => new NetTopologySuite.Geometries.Coordinate(c.longitude, c.latitude))
                        .ToList();

                    if (areaCoords.First() != areaCoords.Last())
                        areaCoords.Add(areaCoords.First());

                    missionGeometries.Add(GeometryFactory.CreatePolygon(new LinearRing(areaCoords.ToArray())));
                }
            }

            if (!missionGeometries.Any())
                return null;

            // 복수 폴리곤 병합
            Geometry result = missionGeometries.Count == 1
                ? missionGeometries[0]
                : CascadedUnion(missionGeometries);

            if (!result.IsValid)
                result = result.Buffer(0);

            return result;
        }

        #region 내부 헬퍼 메서드들

        /// <summary>
        /// 유용성평가 협업기저임무(MissionDetail) → 전체 임무 영역 폴리곤 생성
        ///
        /// getCoverageScore / getCoverage 모두에서 호출.
        /// MissionDetail의 AreaList(면형) + LineList(선형) 폴리곤 변환 후 CascadedUnion 병합.
        /// LineList는 CreateIndividualMissionPolygon과 동일한 width 기반 직사각형 확장.
        /// </summary>
        public static Geometry? CreateMissionPolygon(List<MissionDetail> missionDetailList, GeometryFactory geometryFactory = null)
        {
            if (geometryFactory == null) geometryFactory = new GeometryFactory();
            var missionGeometries = new List<Geometry>();

            foreach (var missionSegment in missionDetailList)
            {
                // 1. AreaList (면형) 처리
                if (missionSegment.AreaList != null)
                {
                    foreach (var area in missionSegment.AreaList)
                    {
                        if (area.CoordinateList != null && area.CoordinateList.Count >= 3)
                        {
                            var segmentCoordinates = area.CoordinateList
                                .Select(c => new NetTopologySuite.Geometries.Coordinate(c.Longitude, c.Latitude)).ToList();

                            // 폴리곤 닫기
                            if (segmentCoordinates.First() != segmentCoordinates.Last())
                                segmentCoordinates.Add(segmentCoordinates.First());

                            missionGeometries.Add(geometryFactory.CreatePolygon(new LinearRing(segmentCoordinates.ToArray())));
                        }
                    }
                }

                // 2. LineList (선형) 처리 -> ★통제기와 동일한 직사각형 로직으로 변경★
                if (missionSegment.LineList != null)
                {
                    foreach (var line in missionSegment.LineList)
                    {
                        if (line.CoordinateList != null && line.CoordinateList.Count >= 2)
                        {
                            var lineCoords = line.CoordinateList
                                .Select(c => new NetTopologySuite.Geometries.Coordinate(c.Longitude, c.Latitude)).ToList();

                            double halfWidthMeters = line.Width / 2.0; // 반폭 적용

                            for (int i = 0; i < lineCoords.Count - 1; i++)
                            {
                                var A = lineCoords[i];
                                var B = lineCoords[i + 1];

                                var (nx, ny) = UnitPerp(A, B);

                                var A1 = Offset(A, nx, ny, halfWidthMeters);
                                var A2 = Offset(A, -nx, -ny, halfWidthMeters);
                                var B1 = Offset(B, nx, ny, halfWidthMeters);
                                var B2 = Offset(B, -nx, -ny, halfWidthMeters);

                                // 직사각형 폴리곤 생성 (닫힌 도형이 되도록 A1을 마지막에 추가)
                                var rectCoords = new NetTopologySuite.Geometries.Coordinate[] { A1, B1, B2, A2, A1 };
                                var rectPolygon = geometryFactory.CreatePolygon(new LinearRing(rectCoords));

                                missionGeometries.Add(rectPolygon);
                            }
                        }
                    }
                }
            }

            if (!missionGeometries.Any()) return null;

            // [최적화] CascadedUnion으로 효율적 병합 (SafeUnion 반복보다 빠름)
            Geometry missionPolygon = CascadedUnion(missionGeometries);
            
            // 토폴로지 오류 처리
            if (!missionPolygon.IsValid)
            {
                missionPolygon = missionPolygon.Buffer(0);
            }

            return missionPolygon;
        }


        /// <summary>
        /// 좌표가 유효한지 검증 (0,0 또는 이상값 체크)
        /// </summary>
        private static bool IsValidCoordinate(CameraPoint point)
        {
            if (point == null) return false;

            // 위도/경도가 0이거나 범위를 벗어나면 무효
            if (point.Latitude == 0 && point.Longitude == 0) return false;
            if (point.Latitude < -90 || point.Latitude > 90) return false;
            if (point.Longitude < -180 || point.Longitude > 180) return false;

            return true;
        }

        // 선분 AB에 수직인 단위 벡터 (도→미터 변환 후 계산)
        private static (double x, double y) UnitPerp(NetTopologySuite.Geometries.Coordinate A, NetTopologySuite.Geometries.Coordinate B)
        {
            double mPerDegLat = 111000.0;
            double mPerDegLon = Math.Cos(((A.Y + B.Y) / 2) * Math.PI / 180.0) * 111000.0;
            double vx = (B.X - A.X) * mPerDegLon;
            double vy = (B.Y - A.Y) * mPerDegLat;
            double len = Math.Sqrt(vx * vx + vy * vy);
            if (len < 1e-6) return (0, 0);
            return (-vy / len, vx / len);
        }

        // 좌표를 (ux, uy) 방향으로 dist(m)만큼 이동 (도 단위 반환)
        private static NetTopologySuite.Geometries.Coordinate Offset(NetTopologySuite.Geometries.Coordinate src, double ux, double uy, double dist)
        {
            double latDeg = (uy * dist) / 111000.0;
            double lonDeg = (ux * dist) / (Math.Cos(src.Y * Math.PI / 180.0) * 111000.0);
            return new NetTopologySuite.Geometries.Coordinate(src.X + lonDeg, src.Y + latDeg);
        }

        /// <summary>
        /// 안전한 Union 연산 (Buffer(0)으로 geometry 수정)
        /// </summary>
        private static Geometry SafeUnion(Geometry geom1, Geometry geom2)
        {
            try
            {
                // Buffer(0)으로 invalid geometry 수정
                geom1 = geom1.Buffer(0);
                geom2 = geom2.Buffer(0);

                return geom1.Union(geom2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Union 연산 실패: {ex.Message}");
                // 실패 시 더 큰 geometry 반환
                return geom1.Area > geom2.Area ? geom1 : geom2;
            }
        }

        /// <summary>
        /// 안전한 Intersection 연산 (Buffer(0)으로 geometry 수정)
        /// </summary>
        private static Geometry SafeIntersection(Geometry geom1, Geometry geom2)
        {
            try
            {
                // Buffer(0)으로 invalid geometry 수정
                geom1 = geom1.Buffer(0);
                geom2 = geom2.Buffer(0);

                return geom1.Intersection(geom2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Intersection 연산 실패: {ex.Message}");
                // 실패 시 빈 geometry 반환
                return GeometryFactory.CreatePolygon();
            }
        }

        private static double CalculateDistance(CameraPoint p1, CameraPoint p2)
        {
            double dx = p2.Longitude - p1.Longitude;
            double dy = p2.Latitude - p1.Latitude;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 카메라 데이터로부터 폴리곤 생성
        /// </summary>
        private static List<Polygon> CreateFootprintPolygons(List<CameraDataLog> cameraDataLogList)
        {
            var footprintPolygons = new List<Polygon>();

            foreach (var cameraLog in cameraDataLogList)
            {
                if (cameraLog == null)
                    continue;

                var topLeft = cameraLog.CameraTopLeft;
                var topRight = cameraLog.CameraTopRight;
                var bottomLeft = cameraLog.CameraBottomLeft;
                var bottomRight = cameraLog.CameraBottomRight;

                // 모든 좌표가 유효한지 검증
                if (!IsValidCoordinate(topLeft) || !IsValidCoordinate(topRight) ||
                    !IsValidCoordinate(bottomLeft) || !IsValidCoordinate(bottomRight))
                {
                    continue;
                }

                if (CalculateDistance(topRight, topLeft) > 0.005 || CalculateDistance(bottomRight, bottomLeft) > 0.005 ||
                    CalculateDistance(topLeft, bottomLeft) > 0.005 || CalculateDistance(topRight, bottomRight) > 0.005)
                {
                    // 너무 큰 면적은 무시
                    continue;
                }

                try
                {
                    var footprintCoords = new NetTopologySuite.Geometries.Coordinate[]
                    {
                        new NetTopologySuite.Geometries.Coordinate(topLeft!.Longitude, topLeft.Latitude),
                        new NetTopologySuite.Geometries.Coordinate(topRight!.Longitude, topRight.Latitude),
                        new NetTopologySuite.Geometries.Coordinate(bottomRight!.Longitude, bottomRight.Latitude),
                        new NetTopologySuite.Geometries.Coordinate(bottomLeft!.Longitude, bottomLeft.Latitude),
                        new NetTopologySuite.Geometries.Coordinate(topLeft.Longitude, topLeft.Latitude)
                    };

                    // 중복되지 않는 유효한 점이 최소 3개 이상인지 확인
                    var distinctCoords = footprintCoords.Take(4).Distinct(new CoordinateComparer()).ToList();
                    if (distinctCoords.Count >= 3)
                    {
                        var polygon = GeometryFactory.CreatePolygon(new LinearRing(footprintCoords));

                        // 유효한 폴리곤인지 확인
                        //if (polygon.IsValid && polygon.Area > 0)
                        //{
                        //    footprintPolygons.Add(polygon);
                        //}
                        if (polygon.Area > 0)
                        {
                            footprintPolygons.Add(polygon);
                        }
                    }
                }
                catch (Exception)
                {
                    // 잘못된 geometry는 무시
                    continue;
                }
            }

            return footprintPolygons;
        }

        /// <summary>
        /// 폴리곤들을 하나로 합침
        /// </summary>
        private static Geometry CombinePolygons(List<Polygon> polygons)
        {
            if (!polygons.Any()) return GeometryFactory.CreatePolygon();

            try
            {
                // Buffer(0)으로 자기교차 폴리곤 정리 후 CascadedUnion으로 병합
                var validPolygons = polygons
                    .Select(p => p.Buffer(0))
                    .Where(p => p != null && !p.IsEmpty)
                    .ToList();

                if (!validPolygons.Any()) return GeometryFactory.CreatePolygon();

                return NetTopologySuite.Operation.Union.CascadedPolygonUnion.Union(validPolygons);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CombinePolygons 실패: {ex.Message}");
                return polygons.First().Buffer(0);
            }
        }

        /// <summary>
        /// Segment 준비 (단순화 및 Envelope 생성)
        /// </summary>
        private static (Dictionary<uint, Envelope>, Dictionary<uint, Geometry>) PrepareSegments(Dictionary<uint, Geometry> segmentPolygons)
        {
            var segmentEnvelopes = new Dictionary<uint, Envelope>();
            var simplifiedSegmentPolygons = new Dictionary<uint, Geometry>();

            foreach (var kvp in segmentPolygons)
            {
                segmentEnvelopes[kvp.Key] = kvp.Value.EnvelopeInternal;

                var simplified = kvp.Value.Buffer(0);
                if (simplified.NumPoints > 500)
                {
                    simplified = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(simplified, 0.00005);
                }
                simplifiedSegmentPolygons[kvp.Key] = simplified;
            }

            return (segmentEnvelopes, simplifiedSegmentPolygons);
        }

        /// <summary>
        /// Footprint 수집
        /// </summary>
        private static List<Geometry> CollectFootprints(List<FlightData> flights, ulong timestamp, ref long footprintTime)
        {
            var timestampFootprints = new List<Geometry>();
            foreach (var flight in flights)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var footprint = ProcessCameraDataForFootprints(flight.CameraDataLog, timestamp, flight.AircraftID);
                footprintTime += sw.ElapsedMilliseconds;

                if (footprint.Area > 0)
                    timestampFootprints.Add(footprint);
            }
            return timestampFootprints;
        }

        /// <summary>
        /// 배치를 전체 영역에 병합
        /// </summary>
        //private static Geometry MergeBatchToTotal(Geometry totalArea, List<Geometry> batch, double tolerance)
        //{
        //    var batchUnion = CascadedUnion(batch);
        //    batchUnion = batchUnion.Buffer(0);
        //    batchUnion = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(batchUnion, tolerance);

        //    totalArea = totalArea.Union(batchUnion);
        //    totalArea = totalArea.Buffer(0);
        //    totalArea = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(totalArea, tolerance);

        //    return totalArea;
        //}

        private static Geometry MergeBatchToTotal(Geometry totalArea, List<Geometry> batch, double tolerance)
        {
            var batchUnion = CascadedUnion(batch);

            // 단순화 적용 (Tolerance를 0.00001로 낮췄으므로 면적 깎임 방지됨)
            batchUnion = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(batchUnion, tolerance);

            try
            {
                totalArea = totalArea.Union(batchUnion);
            }
            catch (NetTopologySuite.Geometries.TopologyException)
            {
                try
                {
                    totalArea = totalArea.Buffer(0).Union(batchUnion.Buffer(0));
                }
                catch
                {
                    try
                    {
                        // 실패 시 미세 부풀림으로 강제 병합 시도
                        totalArea = totalArea.Buffer(0.000001).Union(batchUnion.Buffer(0.000001));
                    }
                    catch
                    {
                        // 무시
                    }
                }
            }

            // 최종 단순화
            totalArea = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(totalArea, tolerance);
            return totalArea;
        }

        /// <summary>
        /// Segment별 커버리지 계산
        /// </summary>
        private static void CalculateSegmentCoverages(
            ulong timestamp,
            Geometry totalCoveredArea,
            Dictionary<uint, Geometry> segmentPolygons,
            Dictionary<uint, Geometry> simplifiedSegmentPolygons,
            Dictionary<uint, Envelope> segmentEnvelopes,
            Dictionary<uint, Geometry> segmentCoveredAreas,
            AnalysisResult result,
            double tolerance,
            int maxPoints)
        {
            Geometry simplifiedCoveredArea = totalCoveredArea.NumPoints > maxPoints
                ? NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(totalCoveredArea, tolerance)
                : totalCoveredArea;

            foreach (var segmentId in segmentPolygons.Keys)
            {
                Geometry segmentPolygon = simplifiedSegmentPolygons[segmentId];
                Envelope segmentEnvelope = segmentEnvelopes[segmentId];

                if (!simplifiedCoveredArea.EnvelopeInternal.Intersects(segmentEnvelope))
                {
                    float previousCoverage = segmentPolygon.Area > 0
                        ? (float)((segmentCoveredAreas[segmentId].Area / segmentPolygon.Area) * 100)
                        : 0;

                    result.CoverageDatas.Add(new CoverageDataOutput
                    {
                        Timestamp = timestamp,
                        MissionSegmentID = segmentId,
                        Coverage = previousCoverage
                    });
                    continue;
                }

                try
                {
                    Geometry coveredInSegment = segmentPolygon.Intersection(simplifiedCoveredArea);
                    segmentCoveredAreas[segmentId] = coveredInSegment;

                    float coveragePercentage = segmentPolygon.Area > 0
                        ? (float)((coveredInSegment.Area / segmentPolygon.Area) * 100)
                        : 0;

                    result.CoverageDatas.Add(new CoverageDataOutput
                    {
                        Timestamp = timestamp,
                        MissionSegmentID = segmentId,
                        Coverage = coveragePercentage
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      경고: Segment {segmentId} Intersection 실패 - {ex.Message}");
                    result.CoverageDatas.Add(new CoverageDataOutput
                    {
                        Timestamp = timestamp,
                        MissionSegmentID = segmentId,
                        Coverage = 0
                    });
                }
            }
        }

        /// <summary>
        /// Segment 커버리지 재사용
        /// </summary>
        private static void ReuseSegmentCoverages(
            ulong timestamp,
            Dictionary<uint, Geometry> segmentPolygons,
            Dictionary<uint, Geometry> segmentCoveredAreas,
            AnalysisResult result)
        {
            foreach (var segmentId in segmentPolygons.Keys)
            {
                float previousCoverage = segmentPolygons[segmentId].Area > 0
                    ? (float)((segmentCoveredAreas[segmentId].Area / segmentPolygons[segmentId].Area) * 100)
                    : 0;

                result.CoverageDatas.Add(new CoverageDataOutput
                {
                    Timestamp = timestamp,
                    MissionSegmentID = segmentId,
                    Coverage = previousCoverage
                });
            }
        }

        /// <summary>
        /// 두 FlightDataLog를 선형 보간하여 중간 위치 생성
        /// </summary>
        private static FlightDataLog InterpolateFlightDataLog(FlightDataLog log1, FlightDataLog log2, double t)
        {
            if (log1 == null || log2 == null)
                return null;

            return new FlightDataLog
            {
                // 수정 전 코드:
                //   Latitude  = (float)(log1.Latitude  + (log2.Latitude  - log1.Latitude)  * t),
                //   Longitude = (float)(log1.Longitude + (log2.Longitude - log1.Longitude) * t),
                //   Altitude  = (float)(log1.Altitude  + (log2.Altitude  - log1.Altitude)  * t)
                //
                // 문제: log1.Latitude(float)와 t(double) 혼합 연산에서
                //   Debug는 float 뺄셈 후 double 승격, Release는 처음부터 double 승격 후 연산.
                //   좌표 보간이 커버리지/SR 폴리곤의 기반이라 미세 차이가 Union까지 전파.
                // 수정: 모든 피연산자를 명시적 (double) 캐스팅하여 승격 시점 고정.
                Latitude = (float)((double)log1.Latitude + ((double)log2.Latitude - (double)log1.Latitude) * t),
                Longitude = (float)((double)log1.Longitude + ((double)log2.Longitude - (double)log1.Longitude) * t),
                Altitude = (float)((double)log1.Altitude + ((double)log2.Altitude - (double)log1.Altitude) * t)
            };
        }

        /// <summary>
        /// 두 카메라 데이터 로그를 선형 보간하여 중간값 생성
        /// </summary>
        private static CameraDataLog InterpolateCameraDataLog(CameraDataLog log1, CameraDataLog log2, double t)
        {
            if (log1 == null || log2 == null)
                return null;

            var interpolated = new CameraDataLog();

            // 각 코너점을 선형 보간
            interpolated.CameraTopLeft = InterpolateCameraPoint(log1.CameraTopLeft, log2.CameraTopLeft, t);
            interpolated.CameraTopRight = InterpolateCameraPoint(log1.CameraTopRight, log2.CameraTopRight, t);
            interpolated.CameraBottomLeft = InterpolateCameraPoint(log1.CameraBottomLeft, log2.CameraBottomLeft, t);
            interpolated.CameraBottomRight = InterpolateCameraPoint(log1.CameraBottomRight, log2.CameraBottomRight, t);
            interpolated.CameraCenterPoint = InterpolateCameraPoint(log1.CameraCenterPoint, log2.CameraCenterPoint, t);

            return interpolated;
        }

        /// <summary>
        /// 두 카메라 포인트를 선형 보간
        /// </summary>
        private static CameraPoint InterpolateCameraPoint(CameraPoint p1, CameraPoint p2, double t)
        {
            if (p1 == null || p2 == null)
                return null;

            return new CameraPoint
            {
                Latitude = (float)((double)p1.Latitude + ((double)p2.Latitude - (double)p1.Latitude) * t),
                Longitude = (float)((double)p1.Longitude + ((double)p2.Longitude - (double)p1.Longitude) * t)
            };
        }

        /// <summary>
        /// 5Hz → 15Hz 선형 보간 (카메라 footprint 간격 보정)
        ///
        /// 0401 카메라 데이터는 약 5Hz 기록. 프레임 간 UAV 이동거리가 커서
        /// footprint 사이 빈 영역이 생겨 커버리지가 실제보다 낮게 나옴.
        ///
        /// 보간 방식:
        ///   - 인접 프레임 A, B 사이에 t=0.333, t=0.667 지점 2개 삽입 (3등분)
        ///   - 카메라 4개 꼭짓점 + 중심점 + 비행위치 모두 선형 보간
        ///   - 결과적으로 데이터 3배 (5Hz → 15Hz)
        ///
        /// UAV별 독립 처리:
        ///   - UAV ID가 다른 데이터 사이에서는 보간하지 않음
        ///   - 각 UAV별 보간 후 전체 타임스탬프 순 정렬
        ///
        /// 보간은 SR 필터보다 먼저 수행.
        /// 보간된 프레임도 SR 필터를 통과해야 유효 촬영으로 인정
        /// </summary>
        private static List<FlightData> ExpandFlightDataWithInterpolation(List<FlightData> originalFlightData)
        {
            if (originalFlightData.Count < 2)
                return originalFlightData;

            // 1. UAV별로 데이터 분리
            var dataByUAV = new Dictionary<uint, List<FlightData>>();
            foreach (var flight in originalFlightData)
            {
                if (!dataByUAV.ContainsKey(flight.AircraftID))
                    dataByUAV[flight.AircraftID] = new List<FlightData>();
                dataByUAV[flight.AircraftID].Add(flight);
            }

            // 2. 각 UAV별로 보간 수행
            var expandedData = new List<FlightData>();

            foreach (var uavId in dataByUAV.Keys.OrderBy(x => x))
            {
                var uavFlights = dataByUAV[uavId];
                var uavExpandedData = new List<FlightData>();
                int addedFrames = 0;

                for (int i = 0; i < uavFlights.Count; i++)
                {
                    var current = uavFlights[i];
                    uavExpandedData.Add(current);

                    // 같은 UAV의 다음 데이터가 있으면 항상 보간 수행
                    if (i < uavFlights.Count - 1)
                    {
                        var next = uavFlights[i + 1];
                        ulong timeDiff = next.Timestamp - current.Timestamp;

                        // 모든 인접 데이터 쌍에 대해 보간 수행 (시간 조건 없음)
                        // 보간: 3등분 (t=0.333, t=0.667)
                        for (int interpIdx = 1; interpIdx < 3; interpIdx++)
                        {
                            double t = interpIdx / 3.0;
                            var interpolatedCamera = InterpolateCameraDataLog(current.CameraDataLog, next.CameraDataLog, t);
                            var interpolatedPosition = InterpolateFlightDataLog(current.FlightDataLog, next.FlightDataLog, t);
                            
                            if (interpolatedCamera != null && interpolatedPosition != null)
                            {
                                var interpolatedData = new FlightData
                                {
                                    AircraftID = current.AircraftID,
                                    Timestamp = current.Timestamp + (ulong)Math.Round(timeDiff * t, MidpointRounding.AwayFromZero),
                                    MissionPlanID = current.MissionPlanID,
                                    MissionSegmentID = current.MissionSegmentID,
                                    FlightMode = current.FlightMode,
                                    PayloadMode = current.PayloadMode,
                                    FlightDataLog = interpolatedPosition,  // 보간된 위치 사용
                                    CameraDataLog = interpolatedCamera    // 보간된 카메라 데이터 사용
                                };

                                uavExpandedData.Add(interpolatedData);
                                addedFrames++;
                            }
                        }
                    }
                }

                expandedData.AddRange(uavExpandedData);
                Console.WriteLine($"    [UAV {uavId}] {uavFlights.Count} → {uavExpandedData.Count} ({addedFrames} 프레임 추가)");
            }

            // 3. 다시 시간순으로 정렬 (중요!)
            expandedData = expandedData.OrderBy(x => x.Timestamp).ToList();

            Console.WriteLine($"    [보간 완료] {originalFlightData.Count} → {expandedData.Count} (총 {(expandedData.Count - originalFlightData.Count)} 프레임 추가)");

            return expandedData;
        }

        /// <summary>
        /// 비행 데이터 처리 및 시간별 커버리지 계산 (SR 미적용)
        ///
        /// getCoverage()에서 호출. 전체 카메라 데이터를 시간순으로 처리:
        ///   - 각 타임스탬프 footprint를 폴리곤으로 변환
        ///   - SIMPLIFY_BATCH_SIZE(50)개마다 배치 병합 + 단순화
        ///   - INTERSECTION_INTERVAL(10)번째마다 세그먼트별 Intersection 계산
        ///   - 나머지는 이전 값 재사용
        ///
        /// SR 미적용: 상세분석용이므로 모든 프레임을 촬영으로 인정.
        ///
        /// 반환:
        ///   - segmentCoveredAreas: 세그먼트별 최종 촬영 영역
        ///   - totalCoveredArea: 전체 촬영 영역 (세그먼트 무관)
        /// </summary>
        private static (Dictionary<uint, Geometry> segmentCoveredAreas, Geometry totalCoveredArea) ProcessFlightDataForCoverage(
             List<FlightData> flightDataList,
             Dictionary<uint, Geometry> segmentPolygons,
             AnalysisResult result)
        {
            Console.WriteLine($"    [ProcessFlightDataForCoverage] 시작");
            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Segment 단순화 및 Envelope 준비
            var (segmentEnvelopes, simplifiedSegmentPolygons) = PrepareSegments(segmentPolygons);
            var segmentCoveredAreas = segmentPolygons.Keys.ToDictionary(id => id, _ => (Geometry)GeometryFactory.CreatePolygon());

            // 데이터 정렬 및 필터링
            var phaseStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var sortedFlightData = flightDataList
                .Where(x => x.AircraftID > 3 && x.CameraDataLog != null && x.Timestamp > 0)
                .OrderBy(x => x.Timestamp)
                .ToList();

            // [신규] 5Hz → 15Hz 보간 적용
            sortedFlightData = ExpandFlightDataWithInterpolation(sortedFlightData);

            var uniqueTimestamps = sortedFlightData
                .Select(x => x.Timestamp)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            Console.WriteLine($"    [Phase 1] 데이터 정렬 & 보간: {phaseStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"    - 총 {uniqueTimestamps.Count}개 timestamp, {sortedFlightData.Count}개 flight data");

            // [최적화] 성능 튜닝 상수
            const int SIMPLIFY_BATCH_SIZE = 50;      // 배치 크기
            const double SIMPLIFY_TOLERANCE = 0.0004; // 톨러런스 (빠른 처리)
            const int MAX_POINTS_THRESHOLD = 800;     // 점 허용치
            const int INTERSECTION_INTERVAL = 10;     // Intersection 계산 간격 (10개마다)

            Geometry totalCoveredArea = GeometryFactory.CreatePolygon();
            var batchFootprints = new List<Geometry>();
            int processedCount = 0;
            long footprintTime = 0, unionTime = 0, intersectionTime = 0, simplifyTime = 0;

            foreach (var timestamp in uniqueTimestamps)
            {
                processedCount++;

                // Footprint 수집
                var timestampFlights = sortedFlightData.Where(x => x.Timestamp == timestamp).ToList();
                var timestampFootprints = CollectFootprints(timestampFlights, timestamp, ref footprintTime);

                // Footprint 병합
                if (timestampFootprints.Any())
                {
                    var unionSw = System.Diagnostics.Stopwatch.StartNew();
                    batchFootprints.Add(CascadedUnion(timestampFootprints));
                    unionTime += unionSw.ElapsedMilliseconds;
                }

                // 배치 병합 및 단순화
                if (processedCount % SIMPLIFY_BATCH_SIZE == 0 && batchFootprints.Any())
                {
                    var simplifySw = System.Diagnostics.Stopwatch.StartNew();
                    totalCoveredArea = MergeBatchToTotal(totalCoveredArea, batchFootprints, SIMPLIFY_TOLERANCE);
                    batchFootprints.Clear();
                    simplifyTime += simplifySw.ElapsedMilliseconds;

                    Console.WriteLine($"    [Batch {processedCount / SIMPLIFY_BATCH_SIZE}] Points: {totalCoveredArea.NumPoints}");
                }

                // Intersection 계산 (10개마다)
                bool shouldCalculateIntersection = (processedCount % INTERSECTION_INTERVAL == 0) ||
                                                   (processedCount == uniqueTimestamps.Count);

                if (shouldCalculateIntersection)
                {
                    var intersectSw = System.Diagnostics.Stopwatch.StartNew();
                    CalculateSegmentCoverages(timestamp, totalCoveredArea, segmentPolygons, simplifiedSegmentPolygons,
                                             segmentEnvelopes, segmentCoveredAreas, result, SIMPLIFY_TOLERANCE, MAX_POINTS_THRESHOLD);
                    intersectionTime += intersectSw.ElapsedMilliseconds;
                }
                else
                {
                    // 이전 값 재사용
                    ReuseSegmentCoverages(timestamp, segmentPolygons, segmentCoveredAreas, result);
                }

                // 진행상황 출력
                if (processedCount % 200 == 0)
                {
                    Console.WriteLine($"    [Progress] {processedCount}/{uniqueTimestamps.Count} ({(processedCount * 100.0 / uniqueTimestamps.Count):F1}%)");
                    Console.WriteLine($"      - Footprint: {footprintTime}ms, Union: {unionTime}ms, Simplify: {simplifyTime}ms, Intersection: {intersectionTime}ms");
                }
            }

            // 마지막 배치 처리
            if (batchFootprints.Any())
            {
                Console.WriteLine($"    [Final Batch] 남은 {batchFootprints.Count}개 처리");
                totalCoveredArea = MergeBatchWithSimplification(totalCoveredArea, batchFootprints, SIMPLIFY_TOLERANCE);
            }

            // 최종 단순화
            if (totalCoveredArea.NumPoints > MAX_POINTS_THRESHOLD)
            {
                int before = totalCoveredArea.NumPoints;
                totalCoveredArea = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(totalCoveredArea, SIMPLIFY_TOLERANCE);
                Console.WriteLine($"    [Final Simplify] Points: {before} → {totalCoveredArea.NumPoints}");
            }

            totalStopwatch.Stop();
            Console.WriteLine($"    [ProcessFlightDataForCoverage] 완료: {totalStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"      - Footprint={footprintTime}ms, Union={unionTime}ms, Simplify={simplifyTime}ms, Intersection={intersectionTime}ms");
            Console.WriteLine($"      - 최종 Points: {totalCoveredArea.NumPoints}, Intersection 횟수: {uniqueTimestamps.Count / INTERSECTION_INTERVAL}회");

            return (segmentCoveredAreas, totalCoveredArea);
        }

   
        // 수정 전 코드:
        //   private static Geometry CascadedUnion(List<Geometry> geometries)
        //   {
        //       if (!geometries.Any()) return GeometryFactory.CreatePolygon();
        //       if (geometries.Count == 1) return geometries[0];
        //       try
        //       {
        //           var geometryCollection = GeometryFactory.CreateGeometryCollection(geometries.ToArray());
        //           return geometryCollection.Union();
        //       }
        //       catch (NetTopologySuite.Geometries.TopologyException)
        //       {
        //           Geometry result = geometries[0].Buffer(0);
        //           foreach (var geom in geometries.Skip(1))
        //           {
        //               try { result = result.Union(geom.Buffer(0)); }
        //               catch
        //               {
        //                   try { result = result.Union(geom.Buffer(0.000001)); }
        //                   catch { continue; }
        //               }
        //           }
        //           return result;
        //       }
        //   }
        //
        // 문제: Release JIT 최적화로 부동소수점 중간값이 달라지면
        //   Debug에서 성공하는 Union()이 Release에서 TopologyException을 던지거나 그 반대.
        //   Debug는 원본 Union 경로, Release는 Buffer(0.000001) 확장 경로를 타서
        //   완전히 다른 면적 산출. 수치 차이의 가장 큰 원인.
        // 수정: 모든 입력을 Buffer(0)으로 미리 정규화하여 빌드 무관하게 동일 경로 보장.
        private static Geometry CascadedUnion(List<Geometry> geometries)
        {
            if (!geometries.Any()) return GeometryFactory.CreatePolygon();
            if (geometries.Count == 1) return geometries[0].Buffer(0);

            var normalized = geometries.Select(g => g.Buffer(0)).Where(g => !g.IsEmpty).ToList();
            if (!normalized.Any()) return GeometryFactory.CreatePolygon();

            try
            {
                var geometryCollection = GeometryFactory.CreateGeometryCollection(normalized.ToArray());
                return geometryCollection.Union();
            }
            catch (NetTopologySuite.Geometries.TopologyException)
            {
                Geometry result = normalized[0];

                foreach (var geom in normalized.Skip(1))
                {
                    try
                    {
                        result = result.Union(geom);
                    }
                    catch
                    {
                        continue;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// 카메라 데이터를 발자국 폴리곤으로 변환
        /// </summary>
        private static Geometry ProcessCameraDataForFootprints(CameraDataLog? cameraDataLog, ulong timestamp, uint aircraftId)
        {
            if (cameraDataLog == null)
                return GeometryFactory.CreatePolygon();

            try
            {
                var footprintPolygons = CreateFootprintPolygons(new List<CameraDataLog> { cameraDataLog });
                return CombinePolygons(footprintPolygons);
            }
            catch (Exception e)
            {
                Console.WriteLine($"발자국 폴리곤 처리 중 오류 발생 (Timestamp: {timestamp}, AircraftID: {aircraftId}): {e.Message}");
                return GeometryFactory.CreatePolygon();
            }
        }

        /// <summary>
        /// 전체 커버리지 메트릭 계산
        /// </summary>
        private static void CalculateOverallCoverageMetrics(Geometry missionPolygon, Geometry totalCoveredArea, AnalysisResult result)
        {
            if (missionPolygon.Area > 0)
            {
                Geometry overallCoveredArea = missionPolygon.Intersection(totalCoveredArea);
                result.Score = (uint)Math.Round((overallCoveredArea.Area / missionPolygon.Area) * 100, MidpointRounding.AwayFromZero);
                result.FilmedArea = (float)(overallCoveredArea.Area * AREA_CONVERSION_FACTOR);
            }
            else
            {
                result.Score = 0;
                result.FilmedArea = 0;
            }
        }

        /// <summary>
        /// 미촬영 지역 정보 생성
        /// </summary>
        private static void PopulateMissingRegions(AnalysisResult result, Geometry missionPolygon, Geometry totalCoveredArea)
        {
            if (missionPolygon.Area <= 0) return;

            Geometry missingArea = missionPolygon.Difference(totalCoveredArea);

            for (int i = 0; i < missingArea.NumGeometries; i++)
            {
                if (missingArea.GetGeometryN(i) is Polygon missingPolygon)
                {
                    var coordinates = missingPolygon.ExteriorRing.Coordinates
                        .Select(coord => new CoordinateOutput
                        {
                            Latitude = (float)coord.Y,
                            Longitude = (float)coord.X
                        })
                        .ToList();

                    result.MissingRegions[$"Area{i + 1}"] = coordinates;
                }
            }
        }

        /// <summary>
        /// MissionSegment별 폴리곤 생성
        /// </summary>
        public static Dictionary<uint, Geometry> CreateMissionSegmentPolygons(List<MissionDetail> missionDetailList)
        {
            var segmentPolygons = new Dictionary<uint, Geometry>();

            foreach (var missionSegment in missionDetailList)
            {
                var segmentGeometries = new List<Geometry>();

                // AreaList 처리 (폴리곤)
                if (missionSegment.AreaList != null)
                {
                    foreach (var area in missionSegment.AreaList)
                    {
                        if (area.CoordinateList != null && area.CoordinateList.Count >= 3)
                        {
                            var segmentCoordinates = area.CoordinateList
                                .Select(c => new NetTopologySuite.Geometries.Coordinate(c.Longitude, c.Latitude))
                                .ToList();

                            // 폴리곤 닫기
                            if (segmentCoordinates.Any() && segmentCoordinates.First() != segmentCoordinates.Last())
                            {
                                segmentCoordinates.Add(segmentCoordinates.First());
                            }

                            try
                            {
                                var polygon = GeometryFactory.CreatePolygon(new LinearRing(segmentCoordinates.ToArray()));
                                segmentGeometries.Add(polygon);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"MissionSegment {missionSegment.MissionSegmentID} 폴리곤 생성 중 오류: {e.Message}");
                            }
                        }
                    }
                }


                // 2. LineList (선형) 처리 -> ★통제기와 동일한 직사각형 로직으로 변경★
                if (missionSegment.LineList != null)
                {
                    foreach (var line in missionSegment.LineList)
                    {
                        if (line.CoordinateList != null && line.CoordinateList.Count >= 2)
                        {
                            var lineCoords = line.CoordinateList
                                .Select(c => new NetTopologySuite.Geometries.Coordinate(c.Longitude, c.Latitude)).ToList();

                            double halfWidthMeters = line.Width / 2.0; // 반폭 적용

                            for (int i = 0; i < lineCoords.Count - 1; i++)
                            {
                                var A = lineCoords[i];
                                var B = lineCoords[i + 1];

                                var (nx, ny) = UnitPerp(A, B);

                                var A1 = Offset(A, nx, ny, halfWidthMeters);
                                var A2 = Offset(A, -nx, -ny, halfWidthMeters);
                                var B1 = Offset(B, nx, ny, halfWidthMeters);
                                var B2 = Offset(B, -nx, -ny, halfWidthMeters);

                                // 직사각형 폴리곤 생성 (닫힌 도형이 되도록 A1을 마지막에 추가)
                                var rectCoords = new NetTopologySuite.Geometries.Coordinate[] { A1, B1, B2, A2, A1 };
                                var rectPolygon = GeometryFactory.CreatePolygon(new LinearRing(rectCoords));

                                segmentGeometries.Add(rectPolygon);
                            }
                        }
                    }
                }

                // [최적화] CascadedUnion으로 효율적 병합
                if (segmentGeometries.Any())
                {
                    Geometry segmentPolygon = segmentGeometries.Count == 1 
                        ? segmentGeometries.First() 
                        : CascadedUnion(segmentGeometries);
                    segmentPolygons[missionSegment.MissionSegmentID] = segmentPolygon;
                }
            }

            return segmentPolygons;
        }

        /// <summary>
        /// MissionSegment별 커버리지 계산
        /// </summary>
        private static void CalculateMissionSegmentCoverages(
            Dictionary<uint, Geometry> segmentPolygons,
            Dictionary<uint, Geometry> segmentCoveredAreas,
            AnalysisResult result)
        {
            foreach (var kvp in segmentPolygons)
            {
                uint segmentId = kvp.Key;
                Geometry segmentPolygon = kvp.Value;

                if (segmentPolygon.Area > 0)
                {
                    try
                    {
                        // 해당 세그먼트의 촬영 영역 가져오기
                        Geometry coveredArea = segmentCoveredAreas.ContainsKey(segmentId)
                            ? segmentCoveredAreas[segmentId]
                            : GeometryFactory.CreatePolygon();

                        Geometry coveredInSegment = segmentPolygon.Intersection(coveredArea);
                        float coveragePercentage = (float)Math.Round((coveredInSegment.Area / segmentPolygon.Area) * 100, 1, MidpointRounding.AwayFromZero);

                        // MissionSegmentData 생성
                        result.MissionSegmentDatas[segmentId] = new MissionSegmentData
                        {
                            Coverage = coveragePercentage,
                            RequiredArea = (float)(segmentPolygon.Area * AREA_CONVERSION_FACTOR),
                            FilmedArea = (float)(coveredInSegment.Area * AREA_CONVERSION_FACTOR)
                        };
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"MissionSegment {segmentId} 커버리지 계산 중 오류: {e.Message}");
                        result.MissionSegmentDatas[segmentId] = new MissionSegmentData
                        {
                            Coverage = 0.0f,
                            RequiredArea = (float)(segmentPolygon.Area * AREA_CONVERSION_FACTOR),
                            FilmedArea = 0.0f
                        };
                    }
                }
                else
                {
                    result.MissionSegmentDatas[segmentId] = new MissionSegmentData
                    {
                        Coverage = 0.0f,
                        RequiredArea = 0.0f,
                        FilmedArea = 0.0f
                    };
                }
            }
        }

        #endregion
    }
}
