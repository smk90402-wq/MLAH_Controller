using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace MLAH_LogAnalyzer
{
    public static class Utils
    {

        // 수정 전 코드:
        //   private static Dictionary<string, WeakReference<ScenarioData>> _scenarioCache =
        //       new Dictionary<string, WeakReference<ScenarioData>>(StringComparer.OrdinalIgnoreCase);
        //
        // 문제: Debug는 GC가 보수적 → WeakReference 대상이 오래 생존 → 캐시 히트.
        //   Release는 GC가 공격적 → 분석 도중 캐시 수거 → 미스 발생 → 파일 재로드.
        //   동일 입력인데 캐시 히트/미스 여부에 따라 다른 객체 인스턴스를 사용하게 되어
        //   이후 계산 결과에 영향.
        // 수정: 강한 참조 복원. ClearScenarioCache()에서 명시적 해제.
        private static Dictionary<string, ScenarioData> _scenarioCache = new Dictionary<string, ScenarioData>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 새로운 폴더를 열 때 캐시 메모리를 비워주는 함수
        /// </summary>
        public static void ClearScenarioCache()
        {
            _scenarioCache.Clear();
            GC.Collect(); // 가비지 컬렉터 강제 호출로 메모리 확보
        }

        /// <summary>
        /// 파일의 절대 경로를 받아 ScenarioData 객체를 로드 (캐싱 적용)
        /// </summary>
        public static async Task<ScenarioData?> LoadScenarioDataByPath(string fullFilePath)
        {
            try
            {
                if (!File.Exists(fullFilePath)) return null;

                if (_scenarioCache.TryGetValue(fullFilePath, out var cached))
                {
                    return cached;
                }

                // 2. 캐시에 없거나 GC에 의해 회수된 경우 파일에서 읽어오기
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                using (FileStream fs = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ScenarioData data = await JsonSerializer.DeserializeAsync<ScenarioData>(fs, options);

                    if (data != null)
                    {
                        _scenarioCache[fullFilePath] = data;
                    }
                    return data;
                }
            }
            catch (JsonException)
            {
                Console.WriteLine($"경고: {Path.GetFileName(fullFilePath)}은(는) 올바른 시나리오 파일이 아닙니다.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류: {Path.GetFileName(fullFilePath)} 로드 실패 - {ex.Message}");
                return null;
            }
        }


        //public static async Task<ScenarioData?> LoadScenarioData(string baseDirectory, int scenarioNumber)
        //{
        //    try
        //    {
        //        string scenarioFileName = $"Scenario{scenarioNumber}.json";
        //        string scenarioPath = Path.Combine(baseDirectory, scenarioFileName);

        //        if (!File.Exists(scenarioPath))
        //        {
        //            string subFolderPath = Path.Combine(baseDirectory, $"Scenario{scenarioNumber}", scenarioFileName);
        //            if (File.Exists(subFolderPath))
        //            {
        //                scenarioPath = subFolderPath;
        //            }
        //            else
        //            {
        //                Console.WriteLine($"오류: 시나리오 파일을 찾을 수 없습니다 - {scenarioPath}");
        //                return null;
        //            }
        //        }

        //        // 2. 'options' 변수를 선언 및 설정
        //        var options = new JsonSerializerOptions
        //        {
        //            PropertyNameCaseInsensitive = true
        //        };

        //        // 3. FileStream을 사용하여 비동기(메모리 절약 + UI 버벅임 방지)
        //        using (FileStream fs = new FileStream(scenarioPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        //        {
        //            // 4. await를 사용하여 비동기 역직렬화 수행
        //            // scenarioData 변수 선언과 동시에 할당하거나, 바로 리턴
        //            return await JsonSerializer.DeserializeAsync<ScenarioData>(fs, options);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"오류: 시나리오 {scenarioNumber} 로드 중 예외 발생 - {ex.Message}");
        //        return null;
        //    }
        //}

        /// <summary>
        /// 사용 가능한 시나리오 파일 목록을 반환
        /// </summary>
        /// <returns>시나리오 번호 목록</returns>
        //public static List<int> GetAvailableScenarios(string baseDirectory)
        //{
        //    var availableScenarios = new List<int>();

        //    try
        //    {
        //        // ✅ [수정] 하드코딩 제거 및 전달받은 경로 유효성 검사
        //        if (string.IsNullOrEmpty(baseDirectory) || !Directory.Exists(baseDirectory))
        //        {
        //            Console.WriteLine($"로그 디렉토리를 찾을 수 없습니다: {baseDirectory}");
        //            return availableScenarios;
        //        }

        //        // 1. 루트 경로에서 Scenario*.json 검색
        //        var scenarioFiles = Directory.GetFiles(baseDirectory, "Scenario*.json").ToList();

        //        // 2. (옵션) 만약 폴더 구조가 'Scenario1/Scenario1.json' 형태라면 하위 폴더도 검색
        //        // 필요 없다면 이 부분은 주석 처리하세요.
        //        var subDirectories = Directory.GetDirectories(baseDirectory, "Scenario*");
        //        foreach (var subDir in subDirectories)
        //        {
        //            var filesInSub = Directory.GetFiles(subDir, "Scenario*.json");
        //            scenarioFiles.AddRange(filesInSub);
        //        }

        //        // 중복 제거 (파일 경로 기준)
        //        scenarioFiles = scenarioFiles.Distinct().ToList();

        //        foreach (var file in scenarioFiles)
        //        {
        //            string fileName = Path.GetFileNameWithoutExtension(file);
        //            // 파일명이 "Scenario"로 시작하고 숫자가 뒤에 오는지 확인
        //            if (fileName.StartsWith("Scenario", StringComparison.OrdinalIgnoreCase) && fileName.Length > 8)
        //            {
        //                string numberPart = fileName.Substring(8); // "Scenario" (8글자) 이후
        //                if (int.TryParse(numberPart, out int scenarioNumber))
        //                {
        //                    if (!availableScenarios.Contains(scenarioNumber))
        //                    {
        //                        availableScenarios.Add(scenarioNumber);
        //                    }
        //                }
        //            }
        //        }

        //        availableScenarios.Sort();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"시나리오 목록 조회 중 오류 발생: {ex.Message}");
        //    }

        //    return availableScenarios;
        //}

        public class DataTransformation
        {

            /// <summary>
            /// [신규] 단일 시나리오를 변환하고 저장하는 메서드 (View에서 호출용)
            /// </summary>
            public static bool ConvertSingleScenario(string rawFolderPath,string? targetFolderPath,string outputDirectory,int scenarioNumber, Action<string> logger)
            {
                try
                {
                    logger($"[Utils] 시나리오 {scenarioNumber} 데이터 빌드 시작...");

                    // 1. Raw 데이터 로드 준비
                    string sbc3Path = Path.Combine(rawFolderPath, "SBC3");
                    string agentPath = Path.Combine(sbc3Path, "0401");
                    string missionPath = Path.Combine(sbc3Path, "0501");

                    if (!Directory.Exists(agentPath) || !Directory.Exists(missionPath))
                    {
                        logger($"[Error] 필수 폴더 누락: {sbc3Path}");
                        return false;
                    }

                    // 1-1. 0201에서 inputMissionPackageID 읽어서 plan 파일 경로 결정
                    string msg0201Path = Path.Combine(sbc3Path, "0201");
                    string planPath;
                    if (Directory.Exists(msg0201Path))
                    {
                        JsonElement msg0201Data = MergeJsonFiles(msg0201Path, "0201");
                        string packageId = "100"; // fallback
                        if (msg0201Data.ValueKind == JsonValueKind.Array && msg0201Data.GetArrayLength() > 0)
                        {
                            var first = msg0201Data[0];
                            if (first.TryGetProperty("inputMissionPackageID", out JsonElement idElem))
                            {
                                packageId = idElem.GetRawText();
                            }
                        }
                        planPath = Path.Combine(sbc3Path, "InputMissionPlan", $"{packageId}.json");
                        logger($"[Utils] 0201에서 InputMissionPackageID={packageId} 확인");
                    }
                    else
                    {
                        string planDir = Path.Combine(sbc3Path, "InputMissionPlan");
                        if (Directory.Exists(planDir))
                        {
                            var planFiles = Directory.GetFiles(planDir, "*.json");
                            if (planFiles.Length == 1)
                            {
                                planPath = planFiles[0];
                                logger($"[Utils] 0201 폴더 없음, InputMissionPlan에서 파일 감지: {Path.GetFileName(planPath)}");
                            }
                            else if (planFiles.Length > 1)
                            {
                                planPath = planFiles[0];
                                logger($"[Utils] 0201 폴더 없음, InputMissionPlan에 파일 {planFiles.Length}개 — 첫 번째 사용: {Path.GetFileName(planPath)}");
                            }
                            else
                            {
                                logger($"[Error] 0201 폴더 없고 InputMissionPlan에 json 파일도 없음");
                                return false;
                            }
                        }
                        else
                        {
                            logger($"[Error] 0201 폴더, InputMissionPlan 폴더 모두 없음");
                            return false;
                        }
                    }

                    if (!File.Exists(planPath))
                    {
                        logger($"[Error] InputMissionPlan 파일 없음: {planPath}");
                        return false;
                    }

                    // 2. 파일 병합 및 로드
                    JsonElement agentData = MergeJsonFiles(agentPath, "0401");
                    JsonElement missionData = MergeJsonFiles(missionPath, "0501");

                    using var planStream = File.OpenRead(planPath);
                    using var planDoc = JsonDocument.Parse(planStream);

                    // 3. 데이터 빌드 (기존 BuildScenarioData 활용)
                    // targetFolderPath가 null이어도 처리가능하도록 내부 로직이 되어있음
                    ScenarioData data = BuildScenarioData(agentData, missionData, planDoc.RootElement, targetFolderPath);

                    //원본 경로 정보 주입 (나중에 중복 체크를 위해)
                    data.SourceLogPath = rawFolderPath;
                    data.TargetLogPath = targetFolderPath ?? "";

                    // 4. 저장 (ScenarioN.json) - 단일 파일 쓰기이므로 JSON 깨짐 없음
                    string fileName = $"Scenario{scenarioNumber}.json";
                    string fullPath = Path.Combine(outputDirectory, fileName);

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(data, options);
                    File.WriteAllText(fullPath, jsonString);

                    logger($"[Utils] 저장 완료: {fileName} (Target 매칭: {(targetFolderPath != null ? "성공" : "없음")})");
                    return true;
                }
                catch (Exception ex)
                {
                    logger($"[Error] 변환 중 예외 발생: {ex.Message}");
                    return false;
                }
            }
            

            /// <summary>
            /// 폴더 내의 모든 JSON 파일을 읽어서 배열로 병합
            /// </summary>
            private static JsonElement MergeJsonFiles(string folderPath, string filePrefix)
            {
                // JsonElement 대신 JSON 원본 텍스트로 저장
                var allDataJsonStrings = new List<string>();

                // 폴더 내의 모든 JSON 파일 찾기 (0401.json, 0401_1.json, 0401_2.json 등)
                var jsonFiles = Directory.GetFiles(folderPath, $"{filePrefix}*.json")
                    .OrderBy(f => GetFileSequenceNumber(f, filePrefix))
                    .ToList();


                foreach (var jsonFile in jsonFiles)
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(jsonFile);
                        using var doc = JsonDocument.Parse(jsonContent);

                        int elementCount = 0;

                        // 배열이면 각 요소를 추가, 단일 객체면 그대로 추가
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in doc.RootElement.EnumerateArray())
                            {
                                // Clone() 대신 GetRawText()로 원본 JSON 저장
                                allDataJsonStrings.Add(element.GetRawText());
                                elementCount++;
                            }
                        }
                        else
                        {
                            allDataJsonStrings.Add(doc.RootElement.GetRawText());
                            elementCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"         경고: {Path.GetFileName(jsonFile)} 파일 읽기 실패 - {ex.Message}");
                    }
                }

                // 모든 JSON 텍스트를 하나의 배열로 결합
                string mergedJson = "[" + string.Join(",", allDataJsonStrings) + "]";

                // 결합된 JSON 문자열을 파싱하여 반환
                using var mergedDoc = JsonDocument.Parse(mergedJson);
                return mergedDoc.RootElement.Clone();
            }

            /// <summary>
            /// 파일명에서 순서 번호 추출 (0401.json=0, 0401_1.json=1, 0401_2.json=2)
            /// </summary>
            private static int GetFileSequenceNumber(string filePath, string filePrefix)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                // "0401.json" 형태 (순서 번호 없음)
                if (fileName.Equals(filePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }

                // "0401_1.json", "0401_2.json" 형태
                string suffix = fileName.Substring(filePrefix.Length);
                if (suffix.StartsWith("_"))
                {
                    string numberPart = suffix.Substring(1); // '_' 제거
                    if (int.TryParse(numberPart, out int sequenceNumber))
                    {
                        return sequenceNumber;
                    }
                }

                // 파싱 실패 시 매우 큰 숫자 반환 (마지막에 정렬)
                return int.MaxValue;
            }

            public static ScenarioData BuildScenarioData(JsonElement agentStatus, JsonElement missionProgress, JsonElement inputMissionPlan, string? targetFolderPath)
            {

                var flightDataList = new List<FlightData>();
                var realTargetDataList = new List<RealTargetData>();
                var missionDetailList = new List<MissionDetail>();

                flightDataList = BuildFlightData(agentStatus, missionProgress, targetFolderPath);
                realTargetDataList = BuildRealTargetData(targetFolderPath);
                missionDetailList = BuildMissionDetail(missionProgress, inputMissionPlan);

                return new ScenarioData
                {
                    FlightData = flightDataList,
                    RealTargetData = realTargetDataList,
                    MissionDetail = missionDetailList
                };

            }

            public static List<FlightData> BuildFlightData(JsonElement agentStatus, JsonElement missionProgress, string? targetFolderPath)
            {
                var flightDataList = new List<FlightData>();
                uint savedSegmentID = uint.MaxValue;
                List<ulong> criteriaTimes = [];
                Dictionary<ulong, uint> timestampToSegmentID = new Dictionary<ulong, uint>();
                foreach (var mission in missionProgress.EnumerateArray())
                {
                    if (!mission.TryGetProperty("timestamp", out JsonElement tsElem) ||
                        !mission.TryGetProperty("currentInputMissionID", out JsonElement missionIdElem))
                        continue;

                    ulong timestamp = tsElem.GetUInt64();
                    uint missionSegmentID = missionIdElem.GetUInt32();

                    if (savedSegmentID != missionSegmentID)
                    {
                        savedSegmentID = missionSegmentID;
                        criteriaTimes.Add(timestamp);
                        timestampToSegmentID[timestamp] = savedSegmentID;
                    }
                }

                foreach (var agent in agentStatus.EnumerateArray())
                {
                    if (!agent.TryGetProperty("agentStateList", out JsonElement agentStateListElem) ||
                        agentStateListElem.ValueKind != JsonValueKind.Array)
                        continue;

                    if (!agent.TryGetProperty("timestamp", out JsonElement agentTsElem))
                        continue;

                    ulong Timestamp = agentTsElem.GetUInt64();

                    var agentStateArray = agentStateListElem.EnumerateArray().ToArray();

                    for (int i = 1; i < 7; i++) //UAV 1 ~ 6
                    {
                        if (i - 1 >= agentStateArray.Length)
                            continue;

                        var agentState = agentStateArray[i - 1];

                        if (!agentState.TryGetProperty("aircraftID", out JsonElement aircraftIdElem))
                            continue;

                        uint AircraftID = aircraftIdElem.GetUInt32();
                        uint MissionSegmentID = FindSegmentID(Timestamp, criteriaTimes, timestampToSegmentID);

                        //if (MissionSegmentID == 0)// 0 인 경우 임무 시작전. ( missionSegmentID 가 null 인 경우)
                        //{
                        //    continue;
                        //}

                        FlightDataLog? FlightDataLog = null;
                        if (agentState.TryGetProperty("coordinate", out JsonElement coordElement) &&
                            coordElement.ValueKind != JsonValueKind.Null)
                        {
                            // latitude, longitude, altitude 각각 null 체크
                            float? latitude = GetNullableFloat(coordElement, "latitude");
                            float? longitude = GetNullableFloat(coordElement, "longitude");
                            float? altitude = GetNullableFloat(coordElement, "altitude");

                            // 모든 좌표값이 유효한 경우에만 FlightDataLog 생성
                            if (latitude.HasValue && longitude.HasValue && altitude.HasValue)
                            {
                                FlightDataLog = new FlightDataLog
                                {
                                    Latitude = latitude ?? 0.0f,
                                    Longitude = longitude ?? 0.0f,
                                    Altitude = altitude ?? 0.0f
                                };
                            }
                            else
                            {
                                continue;
                            }
                        }

                        CameraDataLog? CameraDataLog = null;
                        if (i >= 4)
                        {
                            if (agentState.TryGetProperty("unmannedInfo", out JsonElement unmannedInfo) &&
                                unmannedInfo.ValueKind != JsonValueKind.Null &&
                                unmannedInfo.TryGetProperty("sensorInfo", out JsonElement sensorInfo) &&
                                sensorInfo.ValueKind != JsonValueKind.Null &&
                                sensorInfo.TryGetProperty("footprintCornerList", out JsonElement footprintList) &&
                                footprintList.ValueKind == JsonValueKind.Array)
                            {
                                var corners = footprintList.EnumerateArray().ToArray();
                                if (corners.Length == 4)
                                {
                                    // FootprintCornerList 순서: [0]=TopLeft, [1]=TopRight, [2]=BottomLeft, [3]=BottomRight
                                    // 올바른 사각형을 만들기 위한 순서 매핑
                                    var topLeft = GetCameraPoint(corners[0]);
                                    var topRight = GetCameraPoint(corners[1]);
                                    var bottomLeft = GetCameraPoint(corners[2]);
                                    var bottomRight = GetCameraPoint(corners[3]);

                                    // 모든 코너가 유효한 경우에만 CameraDataLog 생성
                                    if (topLeft != null && topRight != null && bottomRight != null && bottomLeft != null)
                                    {
                                        CameraDataLog = new CameraDataLog
                                        {
                                            CameraTopLeft = topLeft,
                                            CameraTopRight = topRight,
                                            CameraBottomLeft = bottomLeft,
                                            CameraBottomRight = bottomRight
                                        };
                                    }
                                }
                            }
                        }
                        if (FlightDataLog != null)
                        {
                            flightDataList.Add(new FlightData
                            {
                                AircraftID = AircraftID,
                                Timestamp = Timestamp,
                                MissionSegmentID = MissionSegmentID,
                                FlightDataLog = FlightDataLog,
                                CameraDataLog = CameraDataLog
                            });
                        }
                    }
                }

                // RealLAHData 항상 병합: LOS 등 부가 필드 추가, 0401에 헬기 위치 없으면 보충
                if (!string.IsNullOrEmpty(targetFolderPath))
                {
                    MergeRealLAHData(flightDataList, targetFolderPath, criteriaTimes, timestampToSegmentID);
                }

                return flightDataList;
            }

            /// <summary>
            /// RealLAHData 병합: LOS 등 부가 필드는 항상 추가, 헬기 위치는 0401에 없을 때만 보충
            /// </summary>
            private static void MergeRealLAHData(List<FlightData> flightDataList, string targetFolderPath, List<ulong> criteriaTimes, Dictionary<ulong, uint> timestampToSegmentID)
            {
                var lahFiles = Directory.GetFiles(targetFolderPath, "RealLAHData*.json")
                    .OrderBy(f => GetFileSequenceNumber(f, "RealLAHData"))
                    .ToList();

                if (!lahFiles.Any()) return;

                // 0401에서 로드된 헬기 데이터를 (timestamp, aircraftID)로 인덱싱
                var existingLookup = new Dictionary<(ulong ts, uint id), FlightData>();
                var aircraftHas0401 = new HashSet<uint>();
                // 헬기별 0401 타임스탬프 정렬 리스트 (LOS 근접 매핑용)
                var sortedTimestamps = new Dictionary<uint, List<ulong>>();
                foreach (var fd in flightDataList)
                {
                    if (fd.AircraftID <= 3)
                    {
                        var key = (fd.Timestamp, fd.AircraftID);
                        existingLookup[key] = fd;
                        aircraftHas0401.Add(fd.AircraftID);
                        if (!sortedTimestamps.ContainsKey(fd.AircraftID))
                            sortedTimestamps[fd.AircraftID] = new List<ulong>();
                        sortedTimestamps[fd.AircraftID].Add(fd.Timestamp);
                    }
                }
                // 이진탐색을 위해 정렬
                foreach (var list in sortedTimestamps.Values)
                    list.Sort();

                foreach (var file in lahFiles)
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(file).Trim();

                        // JSON 포맷 교정
                        jsonContent = System.Text.RegularExpressions.Regex.Replace(jsonContent, @"\}\s*\{", "},{");
                        if (!jsonContent.StartsWith("[")) jsonContent = "[" + jsonContent;
                        if (!jsonContent.EndsWith("]")) jsonContent = jsonContent + "]";

                        using var doc = JsonDocument.Parse(jsonContent);

                        foreach (var entry in doc.RootElement.EnumerateArray())
                        {
                            if (!entry.TryGetProperty("TimeStamp", out JsonElement timestampElement)) continue;

                            ulong timestamp = timestampElement.GetUInt64();
                            if (timestamp < 10000000000)
                                timestamp *= 1000;

                            uint missionSegmentID = FindSegmentID(timestamp, criteriaTimes, timestampToSegmentID);

                            if (entry.TryGetProperty("LAHData", out JsonElement lahDataArray) && lahDataArray.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var lah in lahDataArray.EnumerateArray())
                                {
                                    if (!lah.TryGetProperty("ID", out JsonElement lahIdElem) ||
                                        lahIdElem.ValueKind != JsonValueKind.Number)
                                        continue;

                                    uint aircraftId = lahIdElem.GetUInt32();
                                    if (aircraftId > 3) continue;

                                    // LOS 필드는 LAHData 각 항목 안에 존재
                                    int? losUav4 = null, losUav5 = null, losUav6 = null;
                                    if (lah.TryGetProperty("loss_uav4", out JsonElement lu4) && lu4.ValueKind == JsonValueKind.Number)
                                        losUav4 = lu4.GetInt32();
                                    if (lah.TryGetProperty("loss_uav5", out JsonElement lu5) && lu5.ValueKind == JsonValueKind.Number)
                                        losUav5 = lu5.GetInt32();
                                    if (lah.TryGetProperty("loss_uav6", out JsonElement lu6) && lu6.ValueKind == JsonValueKind.Number)
                                        losUav6 = lu6.GetInt32();

                                    var key = (timestamp, aircraftId);

                                    if (existingLookup.TryGetValue(key, out FlightData? existing))
                                    {
                                        // 0401에 정확히 일치 -> LOS 병합
                                        existing.LosUav4 = losUav4;
                                        existing.LosUav5 = losUav5;
                                        existing.LosUav6 = losUav6;
                                    }
                                    else if (aircraftHas0401.Contains(aircraftId))
                                    {
                                        // 0401에 헬기 데이터 있지만 timestamp 불일치 -> 가장 가까운 0401 항목에 LOS 병합
                                        var tsList = sortedTimestamps[aircraftId];
                                        int idx = tsList.BinarySearch(timestamp);
                                        if (idx < 0) idx = ~idx; // 삽입 위치
                                        // 이전/이후 중 가까운 쪽 선택
                                        ulong bestTs = 0;
                                        long bestDiff = long.MaxValue;
                                        if (idx > 0)
                                        {
                                            long diff = (long)(timestamp - tsList[idx - 1]);
                                            if (diff < bestDiff) { bestDiff = diff; bestTs = tsList[idx - 1]; }
                                        }
                                        if (idx < tsList.Count)
                                        {
                                            long diff = (long)(tsList[idx] - timestamp);
                                            if (diff < bestDiff) { bestDiff = diff; bestTs = tsList[idx]; }
                                        }
                                        if (bestTs > 0 && existingLookup.TryGetValue((bestTs, aircraftId), out FlightData? nearest))
                                        {
                                            nearest.LosUav4 = losUav4;
                                            nearest.LosUav5 = losUav5;
                                            nearest.LosUav6 = losUav6;
                                        }
                                    }
                                    else
                                    {
                                        // 0401에 해당 헬기 데이터가 전혀 없을 때만 RealLAHData 위치로 보충
                                        float lahLat = lah.TryGetProperty("Latitude", out JsonElement lahLatElem) && lahLatElem.ValueKind == JsonValueKind.Number
                                            ? lahLatElem.GetSingle() : 0f;
                                        float lahLon = lah.TryGetProperty("Longitude", out JsonElement lahLonElem) && lahLonElem.ValueKind == JsonValueKind.Number
                                            ? lahLonElem.GetSingle() : 0f;
                                        float lahAlt = lah.TryGetProperty("Altitude", out JsonElement lahAltElem) && lahAltElem.ValueKind == JsonValueKind.Number
                                            ? lahAltElem.GetSingle() : 0f;

                                        FlightDataLog fdLog = new FlightDataLog
                                        {
                                            Latitude = lahLat,
                                            Longitude = lahLon,
                                            Altitude = lahAlt
                                        };

                                        var newEntry = new FlightData
                                        {
                                            AircraftID = aircraftId,
                                            Timestamp = timestamp,
                                            MissionSegmentID = missionSegmentID,
                                            FlightDataLog = fdLog,
                                            CameraDataLog = null,
                                            LosUav4 = losUav4,
                                            LosUav5 = losUav5,
                                            LosUav6 = losUav6
                                        };

                                        flightDataList.Add(newEntry);
                                        existingLookup[key] = newEntry;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[경고] {Path.GetFileName(file)} 파싱 실패 - {ex.Message}");
                    }
                }
            }

            private static uint ParseTargetStatus(JsonElement targetElement)
            {
                if (!targetElement.TryGetProperty("Status", out JsonElement stElem)) return 0;
                if (stElem.ValueKind == JsonValueKind.Number) return stElem.GetUInt32();
                if (stElem.ValueKind == JsonValueKind.String)
                {
                    string s = stElem.GetString() ?? "";
                    if (s.Contains("DEATH", StringComparison.OrdinalIgnoreCase)) return 3;
                    if (s.Contains("WAIT", StringComparison.OrdinalIgnoreCase)) return 0;
                    if (uint.TryParse(s, out uint parsed)) return parsed;
                }
                return 0;
            }

            private static float? GetNullableFloat(JsonElement element, string propertyName)
            {
                if (element.TryGetProperty(propertyName, out JsonElement propElement) &&
                    propElement.ValueKind == JsonValueKind.Number)
                {
                    return propElement.GetSingle();
                }
                return null;
            }

            // CameraPoint를 안전하게 생성 (NULL 고려)
            private static CameraPoint? GetCameraPoint(JsonElement coordElement)
            {
                if (coordElement.ValueKind == JsonValueKind.Null)
                    return null;

                float? latitude = GetNullableFloat(coordElement, "latitude");
                float? longitude = GetNullableFloat(coordElement, "longitude");

                // latitude와 longitude 둘 다 있고 0 이 아니어야 유효한 포인트
                if (latitude.HasValue && longitude.HasValue && latitude != 0.0 && longitude != 0.0)
                {
                    return new CameraPoint
                    {
                        Latitude = latitude.Value,
                        Longitude = longitude.Value
                    };
                }

                return null;
            }

            private static uint FindSegmentID(ulong timestamp, List<ulong> criteriaTimes, Dictionary<ulong, uint> timestampToSegmentID)
            {
                if (criteriaTimes == null || criteriaTimes.Count == 0)
                    throw new InvalidOperationException("criteriaTimes가 비어 있습니다.");

                // timestamp가 첫 번째 기준 시간보다 작으면 협업기저임무 존재 X -> Continue
                if (timestamp < criteriaTimes[0])
                {
                    return 0;
                }

                // timestamp보다 작거나 같은 가장 큰 criteriaTime 찾기
                ulong? foundCriteriaTime = null;
                foreach (var criteriaTime in criteriaTimes)
                {
                    if (criteriaTime <= timestamp)
                    {
                        foundCriteriaTime = criteriaTime;
                    }
                    else
                    {
                        break; // criteriaTimes는 정렬되어 있으므로 더 이상 찾을 필요 없음
                    }
                }

                if (foundCriteriaTime.HasValue)
                {
                    return timestampToSegmentID[foundCriteriaTime.Value];
                }
                Console.WriteLine("Segment 를 찾을수 없습니다.");
                return uint.MaxValue;
            }

            public static List<RealTargetData> BuildRealTargetData(string? targetFolderPath)
            {
                var realTargetDataList = new List<RealTargetData>();

                // target 폴더 경로가 null이거나 존재하지 않으면 빈 리스트 반환
                if (string.IsNullOrEmpty(targetFolderPath) || !Directory.Exists(targetFolderPath))
                {
                    Console.WriteLine($"         경고: target 폴더를 찾을 수 없습니다: {targetFolderPath ?? "null"}");
                    return realTargetDataList;
                }

                // TargetData_Truth*.json 우선, 없으면 RealTargetData*.json 사용
                var targetFiles = Directory.GetFiles(targetFolderPath, "TargetData_Truth*.json")
                    .OrderBy(f => GetFileSequenceNumber(f, "TargetData_Truth"))
                    .ToList();

                if (!targetFiles.Any())
                {
                    // RealTargetData*.json fallback (SubType 포함된 언리얼 로그)
                    targetFiles = Directory.GetFiles(targetFolderPath, "RealTargetData*.json")
                        .OrderBy(f => GetFileSequenceNumber(f, "RealTargetData"))
                        .ToList();
                }

                if (!targetFiles.Any())
                {
                    Console.WriteLine($"         경고: Target 파일을 찾을 수 없습니다: {targetFolderPath}");
                    return realTargetDataList;
                }

                foreach (var targetFile in targetFiles)
                {
                    try
                    {
                        // [보정 로직 추가] 파일을 읽어서 앞뒤에 대괄호가 없으면 붙여줌
                        string jsonContent = File.ReadAllText(targetFile).Trim();
                        if (!jsonContent.StartsWith("[")) jsonContent = "[" + jsonContent;
                        if (!jsonContent.EndsWith("]")) jsonContent = jsonContent + "]";


                        // JSON 배열로 파싱
                        using var doc = JsonDocument.Parse(jsonContent);

                        if (doc.RootElement.ValueKind != JsonValueKind.Array)
                        {
                            Console.WriteLine($"         경고: {Path.GetFileName(targetFile)}이 배열 형식이 아닙니다.");
                            continue;
                        }

                        int timestampCount = 0;

                        // 각 타임스탬프 엔트리 처리
                        foreach (var timestampEntry in doc.RootElement.EnumerateArray())
                        {
                            try
                            {
                                // TimeStamp 읽기
                                if (!timestampEntry.TryGetProperty("TimeStamp", out JsonElement timestampElement))
                                {
                                    Console.WriteLine($"         경고: TimeStamp 속성을 찾을 수 없습니다.");
                                    continue;
                                }

                                ulong timestamp = timestampElement.GetUInt64();

                                //보정 로직 추가]
                                // Agent 데이터(0401)는 약 12~13자리(ms), TargetData는 9~10자리(sec)인 경우 동기화를 위해 보정
                                // 기준: 100억(10,000,000,000) 미만이면 '초' 단위로 판단하고 1000을 곱함
                                // (현재 Unix Time '초'는 약 17억 대이므로 안전한 기준)
                                if (timestamp < 10000000000)
                                {
                                    timestamp *= 1000;
                                }

                                var targetDataList = new List<Target>();

                                // "TargetList" 또는 "TargetData" 키 모두 시도
                                JsonElement targetListElement = default;
                                bool hasTargetArray = (timestampEntry.TryGetProperty("TargetList", out targetListElement) ||
                                                       timestampEntry.TryGetProperty("TargetData", out targetListElement))
                                                      && targetListElement.ValueKind == JsonValueKind.Array;

                                if (hasTargetArray)
                                {
                                    foreach (var targetElement in targetListElement.EnumerateArray())
                                    {
                                        try
                                        {
                                            // ID: "ID" 또는 "TargetID"
                                            uint targetId = 0;
                                            if (targetElement.TryGetProperty("ID", out JsonElement idElem) && idElem.ValueKind == JsonValueKind.Number)
                                                targetId = idElem.GetUInt32();
                                            else if (targetElement.TryGetProperty("TargetID", out JsonElement tidElem) && tidElem.ValueKind == JsonValueKind.Number)
                                                targetId = tidElem.GetUInt32();

                                            float tgtLat = targetElement.TryGetProperty("Latitude", out JsonElement tgtLatElem) && tgtLatElem.ValueKind == JsonValueKind.Number ? tgtLatElem.GetSingle() : 0f;
                                            float tgtLon = targetElement.TryGetProperty("Longitude", out JsonElement tgtLonElem) && tgtLonElem.ValueKind == JsonValueKind.Number ? tgtLonElem.GetSingle() : 0f;
                                            float tgtAlt = targetElement.TryGetProperty("Altitude", out JsonElement tgtAltElem) && tgtAltElem.ValueKind == JsonValueKind.Number ? tgtAltElem.GetSingle() : 0f;

                                            var target = new Target
                                            {
                                                Type = targetElement.TryGetProperty("Type", out JsonElement typeElem) ? typeElem.GetString() ?? string.Empty : string.Empty,
                                                Subtype = targetElement.TryGetProperty("SubType", out JsonElement subElem) ? subElem.GetString() ?? string.Empty : string.Empty,
                                                ID = targetId,
                                                Latitude = tgtLat,
                                                Longitude = tgtLon,
                                                Altitude = tgtAlt,
                                                Status = ParseTargetStatus(targetElement),
                                                IsEnemy = targetElement.TryGetProperty("IsEnemy", out JsonElement isEnemyElem) && isEnemyElem.GetBoolean(),
                                                LAH1LOS = targetElement.TryGetProperty("LAH1LOS", out JsonElement lah1Elem) && lah1Elem.GetBoolean(),
                                                LAH2LOS = targetElement.TryGetProperty("LAH2LOS", out JsonElement lah2Elem) && lah2Elem.GetBoolean(),
                                                LAH3LOS = targetElement.TryGetProperty("LAH3LOS", out JsonElement lah3Elem) && lah3Elem.GetBoolean(),
                                                Unit1TargetID = targetElement.TryGetProperty("Unit1TargetID", out JsonElement u1tidElem) && u1tidElem.ValueKind == JsonValueKind.Number ? u1tidElem.GetUInt32() : 0
                                            };

                                            targetDataList.Add(target);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"         경고: Target 데이터 파싱 실패 - {ex.Message}");
                                        }
                                    }
                                }

                                // 타임스탬프별로 Target 리스트 추가
                                realTargetDataList.Add(new RealTargetData
                                {
                                    Timestamp = timestamp,
                                    TargetList = targetDataList
                                });

                                timestampCount++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"         경고: 타임스탬프 엔트리 파싱 실패 - {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"         경고: {Path.GetFileName(targetFile)} 파일 읽기 실패 - {ex.Message}");
                    }
                }

                return realTargetDataList;
            }

            public static List<MissionDetail> BuildMissionDetail(JsonElement missionProgress, JsonElement inputMissionPlan)
            {
                var missionDetailList = new List<MissionDetail>();

                if (!inputMissionPlan.TryGetProperty("inputMissionList", out JsonElement inputMissionList) ||
                    inputMissionList.ValueKind != JsonValueKind.Array)
                    return missionDetailList;

                foreach (var inputMission in inputMissionList.EnumerateArray())
                {
                    if (!inputMission.TryGetProperty("inputMissionID", out JsonElement missionIdElem))
                        continue;

                    uint missionSegmentID = missionIdElem.GetUInt32();

                    if (!inputMission.TryGetProperty("missionDetail", out JsonElement missionDetailJson))
                        continue;

                    var missionDetail = new MissionDetail
                    {
                        MissionSegmentID = missionSegmentID,
                        LineList = new List<LineList>(),
                        AreaList = new List<AreaList>(),
                        MissionPauseTimeStamp = GetMissionPauseTimeStamp.Extract(missionProgress, missionSegmentID)
                    };

                    // LineList 처리
                    if (missionDetailJson.TryGetProperty("lineList", out JsonElement lineListElement) &&
                        lineListElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var lineElement in lineListElement.EnumerateArray())
                        {
                            uint lineWidth = lineElement.TryGetProperty("width", out JsonElement widthElem) && widthElem.ValueKind == JsonValueKind.Number
                                ? widthElem.GetUInt32() : 0;

                            var line = new LineList
                            {
                                Width = lineWidth,
                                CoordinateList = new List<Coordinate>()
                            };

                            if (lineElement.TryGetProperty("coordinateList", out JsonElement coordListElement) &&
                                coordListElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var coordElement in coordListElement.EnumerateArray())
                                {
                                    float lat = coordElement.TryGetProperty("latitude", out JsonElement latE) && latE.ValueKind == JsonValueKind.Number ? latE.GetSingle() : 0f;
                                    float lon = coordElement.TryGetProperty("longitude", out JsonElement lonE) && lonE.ValueKind == JsonValueKind.Number ? lonE.GetSingle() : 0f;
                                    float alt = coordElement.TryGetProperty("altitude", out JsonElement altE) && altE.ValueKind == JsonValueKind.Number ? altE.GetSingle() : 0f;

                                    line.CoordinateList.Add(new Coordinate
                                    {
                                        Latitude = lat,
                                        Longitude = lon,
                                        Altitude = alt
                                    });
                                }
                            }

                            missionDetail.LineList.Add(line);
                        }
                    }

                    // AreaList 처리
                    if (missionDetailJson.TryGetProperty("areaList", out JsonElement areaListElement) &&
                        areaListElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var areaElement in areaListElement.EnumerateArray())
                        {
                            var area = new AreaList
                            {
                                CoordinateList = new List<Coordinate>()
                            };

                            if (areaElement.TryGetProperty("coordinateList", out JsonElement coordListElement) &&
                                coordListElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var coordElement in coordListElement.EnumerateArray())
                                {
                                    float lat = coordElement.TryGetProperty("latitude", out JsonElement latE) && latE.ValueKind == JsonValueKind.Number ? latE.GetSingle() : 0f;
                                    float lon = coordElement.TryGetProperty("longitude", out JsonElement lonE) && lonE.ValueKind == JsonValueKind.Number ? lonE.GetSingle() : 0f;
                                    float alt = coordElement.TryGetProperty("altitude", out JsonElement altE) && altE.ValueKind == JsonValueKind.Number ? altE.GetSingle() : 0f;

                                    area.CoordinateList.Add(new Coordinate
                                    {
                                        Latitude = lat,
                                        Longitude = lon,
                                        Altitude = alt
                                    });
                                }
                            }

                            missionDetail.AreaList.Add(area);
                        }
                    }

                    missionDetailList.Add(missionDetail);
                }

                return missionDetailList;
            }
        }

        /// <summary>
        /// MissionProgress 데이터에서 UAV별 Pause 구간을 추출하는 클래스
        /// </summary>
        public static class GetMissionPauseTimeStamp
        {
            /// <summary>
            /// 특정 MissionSegment의 Pause 구간 추출
            /// </summary>
            /// <param name="missionProgress">0501.json의 JsonElement</param>
            /// <param name="targetSegmentID">대상 MissionSegmentID</param>
            /// <returns>UAV별 Pause 구간 정보</returns>
            public static MissionPauseTimeStamp? Extract(JsonElement missionProgress, uint targetSegmentID)
            {
                try
                {
                    // UAV별 pause 구간 추적 (UAV 4, 5, 6만 해당)
                    var uav4Pauses = new List<PauseTimeRange>();
                    var uav5Pauses = new List<PauseTimeRange>();
                    var uav6Pauses = new List<PauseTimeRange>();

                    // UAV별 pause 시작 시간 추적
                    ulong? uav4PauseStart = null;
                    ulong? uav5PauseStart = null;
                    ulong? uav6PauseStart = null;

                    // timestamp로 정렬된 데이터 처리
                    var sortedProgress = missionProgress.EnumerateArray()
                        .Where(p =>
                        {
                            if (p.TryGetProperty("currentInputMissionID", out JsonElement missionId) &&
                                p.TryGetProperty("timestamp", out _))
                            {
                                return missionId.GetUInt32() == targetSegmentID;
                            }
                            return false;
                        })
                        .OrderBy(p => p.GetProperty("timestamp").GetUInt64())
                        .ToList();

                    if (!sortedProgress.Any())
                    {
                        // Console.WriteLine($"       경고: MissionSegment {targetSegmentID}에 해당하는 데이터가 없습니다.");
                        return null;
                    }

                    //Console.WriteLine($"       MissionSegment {targetSegmentID}: {sortedProgress.Count}개 레코드 분석 중...");

                    // 각 timestamp별로 UAV 상태 확인
                    foreach (var progress in sortedProgress)
                    {
                        ulong timestamp = progress.GetProperty("timestamp").GetUInt64();

                        if (!progress.TryGetProperty("individualMissionProgressStatusList", out JsonElement progressList) ||
                            progressList.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        var progressArray = progressList.EnumerateArray().ToArray();

                        // UAV 4 (index 0 - 첫 번째 요소)
                        if (progressArray.Length > 0)
                        {
                            ProcessUavPause(progressArray[0], timestamp, 4, ref uav4PauseStart, uav4Pauses);
                        }

                        // UAV 5 (index 1 - 두 번째 요소)
                        if (progressArray.Length > 1)
                        {
                            ProcessUavPause(progressArray[1], timestamp, 5, ref uav5PauseStart, uav5Pauses);
                        }

                        // UAV 6 (index 2 - 세 번째 요소)
                        if (progressArray.Length > 2)
                        {
                            ProcessUavPause(progressArray[2], timestamp, 6, ref uav6PauseStart, uav6Pauses);
                        }
                    }

                    // 마지막에 pause 상태로 끝난 경우 처리 - 다음 mission 시작 지점 확인
                    if (sortedProgress.Any())
                    {
                        ulong lastTimestamp = sortedProgress.Last().GetProperty("timestamp").GetUInt64();

                        // 다음 mission이 있는지 확인하고, pause 종료 시점 찾기
                        var nextMissionData = missionProgress.EnumerateArray()
                            .Where(p =>
                            {
                                if (p.TryGetProperty("currentInputMissionID", out JsonElement missionId) &&
                                    p.TryGetProperty("timestamp", out JsonElement tsVal))
                                {
                                    return missionId.GetUInt32() != targetSegmentID &&
                                           tsVal.GetUInt64() > lastTimestamp;
                                }
                                return false;
                            })
                            .OrderBy(p => p.GetProperty("timestamp").GetUInt64())
                            .Take(5)  // 다음 mission의 처음 몇 개만 확인
                            .ToList();

                        if (uav4PauseStart.HasValue)
                        {
                            ulong pauseEnd = lastTimestamp;

                            // 다음 mission에서 pause가 종료되는지 확인
                            foreach (var nextProgress in nextMissionData)
                            {
                                if (nextProgress.TryGetProperty("individualMissionProgressStatusList", out JsonElement progressList) &&
                                    progressList.ValueKind == JsonValueKind.Array)
                                {
                                    var progressArray = progressList.EnumerateArray().ToArray();
                                    if (progressArray.Length > 0 &&
                                        progressArray[0].TryGetProperty("currentIndividualMissionProgress", out JsonElement progress))
                                    {
                                        if (progress.GetUInt32() != 100)
                                        {
                                            pauseEnd = nextProgress.GetProperty("timestamp").GetUInt64();
                                            break;
                                        }
                                    }
                                }
                            }

                            uav4Pauses.Add(new PauseTimeRange
                            {
                                Start = uav4PauseStart.Value,
                                End = pauseEnd
                            });
                            //Console.WriteLine($"       UAV4: Pause 종료 확인 ({TimestampToDateTime(uav4PauseStart.Value):HH:mm:ss} ~ {TimestampToDateTime(pauseEnd):HH:mm:ss})");
                        }

                        if (uav5PauseStart.HasValue)
                        {
                            ulong pauseEnd = lastTimestamp;

                            foreach (var nextProgress in nextMissionData)
                            {
                                if (nextProgress.TryGetProperty("individualMissionProgressStatusList", out JsonElement progressList) &&
                                    progressList.ValueKind == JsonValueKind.Array)
                                {
                                    var progressArray = progressList.EnumerateArray().ToArray();
                                    if (progressArray.Length > 1 &&
                                        progressArray[1].TryGetProperty("currentIndividualMissionProgress", out JsonElement progress))
                                    {
                                        if (progress.GetUInt32() != 100)
                                        {
                                            pauseEnd = nextProgress.GetProperty("timestamp").GetUInt64();
                                            break;
                                        }
                                    }
                                }
                            }

                            uav5Pauses.Add(new PauseTimeRange
                            {
                                Start = uav5PauseStart.Value,
                                End = pauseEnd
                            });
                            // console.WriteLine($"       UAV5: Pause 종료 확인 ({TimestampToDateTime(uav5PauseStart.Value):HH:mm:ss} ~ {TimestampToDateTime(pauseEnd):HH:mm:ss})");
                        }

                        if (uav6PauseStart.HasValue)
                        {
                            ulong pauseEnd = lastTimestamp;

                            foreach (var nextProgress in nextMissionData)
                            {
                                if (nextProgress.TryGetProperty("individualMissionProgressStatusList", out JsonElement progressList) &&
                                    progressList.ValueKind == JsonValueKind.Array)
                                {
                                    var progressArray = progressList.EnumerateArray().ToArray();
                                    if (progressArray.Length > 2 &&
                                        progressArray[2].TryGetProperty("currentIndividualMissionProgress", out JsonElement progress))
                                    {
                                        if (progress.GetUInt32() != 100)
                                        {
                                            pauseEnd = nextProgress.GetProperty("timestamp").GetUInt64();
                                            break;
                                        }
                                    }
                                }
                            }

                            uav6Pauses.Add(new PauseTimeRange
                            {
                                Start = uav6PauseStart.Value,
                                End = pauseEnd
                            });
                            // console.WriteLine($"       UAV6: Pause 종료 확인 ({TimestampToDateTime(uav6PauseStart.Value):HH:mm:ss} ~ {TimestampToDateTime(pauseEnd):HH:mm:ss})");
                        }
                    }

                    // pause 구간이 하나라도 있으면 MissionPauseTimeStamp 반환
                    if (uav4Pauses.Any() || uav5Pauses.Any() || uav6Pauses.Any())
                    {
                        int totalPauses = uav4Pauses.Count + uav5Pauses.Count + uav6Pauses.Count;
                        // console.WriteLine($"       ✓ Pause 구간 {totalPauses}개 발견 (UAV4:{uav4Pauses.Count}, UAV5:{uav5Pauses.Count}, UAV6:{uav6Pauses.Count})");

                        return new MissionPauseTimeStamp
                        {
                            UAV4 = uav4Pauses.Any() ? uav4Pauses : null,
                            UAV5 = uav5Pauses.Any() ? uav5Pauses : null,
                            UAV6 = uav6Pauses.Any() ? uav6Pauses : null
                        };
                    }

                    // console.WriteLine($"       - Pause 구간 없음");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"       ✗ Pause 추출 오류: {ex.Message}");
                    return null;
                }
            }

            /// <summary>
            /// 개별 UAV의 pause 상태 처리
            /// </summary>
            private static void ProcessUavPause(
                JsonElement uavProgress,
                ulong timestamp,
                int uavId,
                ref ulong? pauseStart,
                List<PauseTimeRange> pauseList)
            {
                if (!uavProgress.TryGetProperty("currentIndividualMissionProgress", out JsonElement progressElement))
                {
                    return;
                }

                uint progress = progressElement.GetUInt32();

                if (progress == 100)
                {
                    // Pause 시작
                    if (!pauseStart.HasValue)
                    {
                        pauseStart = timestamp;
                        // Console.WriteLine($"       UAV{uavId}: Pause 시작 ({TimestampToDateTime(timestamp):HH:mm:ss}), progress={progress}");
                    }
                }
                else
                {
                    // Pause 종료
                    if (pauseStart.HasValue)
                    {
                        pauseList.Add(new PauseTimeRange
                        {
                            Start = pauseStart.Value,
                            End = timestamp
                        });

                        double durationSeconds = (timestamp - pauseStart.Value) / 1000.0;
                        // Console.WriteLine($"       UAV{uavId}: Pause 종료 ({TimestampToDateTime(pauseStart.Value):HH:mm:ss} ~ {TimestampToDateTime(timestamp):HH:mm:ss}, {durationSeconds:F1}초), progress={progress}");

                        pauseStart = null;
                    }
                }
            }

            /// <summary>
            /// 모든 MissionSegment에 대한 Pause 구간 추출
            /// </summary>
            /// <param name="missionProgress">0501.json의 JsonElement</param>
            /// <param name="missionSegmentIDs">추출할 MissionSegmentID 목록</param>
            /// <returns>MissionSegmentID별 Pause 구간 딕셔너리</returns>
            public static Dictionary<uint, MissionPauseTimeStamp?> ExtractAll(
                JsonElement missionProgress,
                List<uint> missionSegmentIDs)
            {
                var result = new Dictionary<uint, MissionPauseTimeStamp?>();

                foreach (var segmentId in missionSegmentIDs)
                {
                    // Console.WriteLine($"    - MissionSegment {segmentId} Pause 추출 중...");
                    result[segmentId] = Extract(missionProgress, segmentId);
                }

                return result;
            }

            /// <summary>
            /// Timestamp를 DateTime으로 변환 (디버깅용)
            /// </summary>
            private static DateTime TimestampToDateTime(ulong timestamp)
            {
                try
                {
                    // Unix epoch (1970-01-01) 기준 milliseconds
                    return DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp).DateTime;
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }

            /// <summary>
            /// Pause 구간 정보를 콘솔에 출력
            /// </summary>
            public static void PrintPauseInfo(MissionPauseTimeStamp? pauseInfo)
            {
                if (pauseInfo == null)
                {
                    // Console.WriteLine("    Pause 구간 없음");
                    return;
                }

                // Console.WriteLine("    === Pause 구간 정보 ===");

                if (pauseInfo.UAV4 != null && pauseInfo.UAV4.Any())
                {
                    // Console.WriteLine($"    UAV4: {pauseInfo.UAV4.Count}개 구간");
                    foreach (var pause in pauseInfo.UAV4)
                    {
                        double duration = (pause.End - pause.Start) / 1000.0;
                        // Console.WriteLine($"      - {TimestampToDateTime(pause.Start):HH:mm:ss} ~ {TimestampToDateTime(pause.End):HH:mm:ss} ({duration:F1}초)");
                    }
                }

                if (pauseInfo.UAV5 != null && pauseInfo.UAV5.Any())
                {
                    // Console.WriteLine($"    UAV5: {pauseInfo.UAV5.Count}개 구간");
                    foreach (var pause in pauseInfo.UAV5)
                    {
                        double duration = (pause.End - pause.Start) / 1000.0;
                        // Console.WriteLine($"      - {TimestampToDateTime(pause.Start):HH:mm:ss} ~ {TimestampToDateTime(pause.End):HH:mm:ss} ({duration:F1}초)");
                    }
                }

                if (pauseInfo.UAV6 != null && pauseInfo.UAV6.Any())
                {
                    // Console.WriteLine($"    UAV6: {pauseInfo.UAV6.Count}개 구간");
                    foreach (var pause in pauseInfo.UAV6)
                    {
                        double duration = (pause.End - pause.Start) / 1000.0;
                        // Console.WriteLine($"      - {TimestampToDateTime(pause.Start):HH:mm:ss} ~ {TimestampToDateTime(pause.End):HH:mm:ss} ({duration:F1}초)");
                    }
                }
            }
        }
    }
}
