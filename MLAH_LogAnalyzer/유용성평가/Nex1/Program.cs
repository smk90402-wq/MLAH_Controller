//using MLAH_LogAnalyzer;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;
//using System.Xml;
//using UtilityEvaluation;

//Console.OutputEncoding = Encoding.UTF8;

//Console.WriteLine("분석 모드를 선택하세요:");
//Console.WriteLine("1. 빠른 점수 요약 (모든 시나리오)");
//Console.WriteLine("2. 상세 분석 (특정 시나리오)");
//// Console.WriteLine("3. 커버리지 시각화 (지도)");
//// Console.WriteLine("4. 커버리지 타임라인 시각화");

//// DeleteScenarios(new[] { 4, 5, 6, 7 });
//// static void DeleteScenarios(int[] scenarioNumbers)
//// {
////     string structuredPath = Path.Combine(Directory.GetCurrentDirectory(), "UtilityEvaluation", "logs", "structured");

////     if (!Directory.Exists(structuredPath))
////     {
////         Console.WriteLine($"structured 폴더가 존재하지 않습니다: {structuredPath}");
////         return;
////     }

////     Console.WriteLine("\n=== 기존 시나리오 파일 삭제 ===");

////     foreach (int scenarioNumber in scenarioNumbers)
////     {
////         string fileName = $"Scenario{scenarioNumber}.json";
////         string filePath = Path.Combine(structuredPath, fileName);

////         try
////         {
////             if (File.Exists(filePath))
////             {
////                 File.Delete(filePath);
////                 Console.WriteLine($"✓ 삭제 완료: {fileName}");
////             }
////             else
////             {
////                 Console.WriteLine($"- 파일 없음: {fileName}");
////             }
////         }
////         catch (Exception ex)
////         {
////             Console.WriteLine($"✗ 삭제 실패: {fileName} - {ex.Message}");
////         }
////     }

////     Console.WriteLine();
//// }

//// var dataCollection = new DataCollection();
//// dataCollection.CollectData();

//string? choice = Console.ReadLine();
//// JSON 직렬화 옵션 설정 
//var options = new JsonSerializerOptions { WriteIndented = true };

//// 빠른 점수 요약
//if (choice == "1")
//{
//    var scenarioScores = await ScoreSummary.getScore(); // 시나리오 마다의 점수를 Dictionary로 반환함

//    var scenarioScoresJson = JsonSerializer.Serialize(scenarioScores, options);
//    Console.WriteLine(scenarioScoresJson);
//    return;
//}

//// // 커버리지 시각화 (지도)
//// if (choice == "3")
//// {
////     Console.Write("시나리오 번호를 입력하세요: ");
////     if (int.TryParse(Console.ReadLine(), out int visualizeScenario))
////     {
////         CoverageVisualizer.VisualizeScenario(visualizeScenario);
////     }
////     return;
//// }

//// // 커버리지 타임라인 시각화
//// if (choice == "4")
//// {
////     Console.Write("시나리오 번호를 입력하세요: ");
////     if (int.TryParse(Console.ReadLine(), out int timelineScenario))
////     {
////         CoverageVisualizer.VisualizeTimeline(timelineScenario);
////     }
////     return;
//// }

//// 상세 분석 모드

//// 테스트용 시나리오 번호 설정
//int scenarioNumber = 3;

//try
//{
//    // 1. 커버리지 분석
//    Console.WriteLine("1. 커버리지 분석");
//    AnalysisResult? coverageResult = CoverageCalculator.getCoverageData(scenarioNumber);

//    if (coverageResult == null)
//    {
//        Console.WriteLine("커버리지 분석 실패");
//        return;
//    }

//    string coverageJsonOutput = JsonSerializer.Serialize(coverageResult, options);
//    Console.WriteLine(coverageJsonOutput);

//    // 2. 통신 가용성 분석
//    Console.WriteLine("\n2. 통신 가용성 분석");
//    CommunicationResult? communicationResult = await CommunicationCalculator.getCommunicationData(scenarioNumber);

//    if (communicationResult == null)
//    {
//        Console.WriteLine("통신 가용성 분석 실패");
//        return;
//    }

//    string communicationJsonOutput = JsonSerializer.Serialize(communicationResult, options);
//    Console.WriteLine(communicationJsonOutput);

//    // 3. 위험 노출 분석
//    Console.WriteLine("\n3. 위험 노출 분석");
//    SafetyResult? safetyResult = await SafetyLevelCalculator.getSafetyData(scenarioNumber);

//    if (safetyResult == null)
//    {
//        Console.WriteLine("위험 노출 분석 실패");
//        return;
//    }

//    string safetyJsonOutput = JsonSerializer.Serialize(safetyResult, options);
//    Console.WriteLine(safetyJsonOutput);

//    // 4. 임무 분배 효과도 분석
//    Console.WriteLine("\n4. 임무 분배 효과도 분석");
//    MissionDistributionResult? missionDistributionResult = MissionDistributionCalculator.getMissionDistributionData(scenarioNumber);

//    if (missionDistributionResult == null)
//    {
//        Console.WriteLine("임무 분배 효과도 분석 실패");
//        return;
//    }

//    string missionDistributionJsonOutput = JsonSerializer.Serialize(missionDistributionResult, options);
//    Console.WriteLine(missionDistributionJsonOutput);

//    // 5. 공간해상도 분석d
//    Console.WriteLine("\n5. 공간해상도 분석");
//    SpatialResolutionResult? spatialResolutionResult = SRCalculator.getSRData(scenarioNumber);

//    if (spatialResolutionResult == null)
//    {
//        Console.WriteLine("공간해상도 분석 실패");
//        return;
//    }

//    // 공간해상도 분석 결과 출력
//    string spatialResolutionJsonOutput = JsonSerializer.Serialize(spatialResolutionResult, options);
//    Console.WriteLine(spatialResolutionJsonOutput);
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"분석 중 오류 발생: {ex.Message}");
//    Console.WriteLine($"스택 트레이스: {ex.StackTrace}");
//}
