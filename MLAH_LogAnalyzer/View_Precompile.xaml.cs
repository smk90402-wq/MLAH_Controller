using DevExpress.Mvvm; // DelegateCommand용
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MLAH_LogAnalyzer
{
    // 매칭 정보를 담을 모델 클래스
    public class ScenarioMatchInfo : INotifyPropertyChanged
    {
        public string RawFolderName { get; set; }
        public string RawFullPath { get; set; }
        public DateTime RawTime { get; set; }

        public string TargetFolderName { get; set; }
        public string TargetFullPath { get; set; } // Nullable
        public double TimeDiffSec { get; set; }

        private bool _isAlreadyGenerated = false;
        public bool IsAlreadyGenerated
        {
            get => _isAlreadyGenerated;
            set { _isAlreadyGenerated = value; OnPropertyChanged(nameof(IsAlreadyGenerated)); }
        }

        private string _existingFileName;
        public string ExistingFileName
        {
            get => _existingFileName;
            set { _existingFileName = value; OnPropertyChanged(nameof(ExistingFileName)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class View_Precompile : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        // 바인딩 속성들
        private string _LogText = "";
        public string LogText { get => _LogText; set { _LogText = value; OnPropertyChanged(nameof(LogText)); } }

        private string _SelectedRootPath = "선택된 경로 없음";
        public string SelectedRootPath { get => _SelectedRootPath; set { _SelectedRootPath = value; OnPropertyChanged(nameof(SelectedRootPath)); } }

        private bool _IsGenerationEnabled = false;
        public bool IsGenerationEnabled { get => _IsGenerationEnabled; set { _IsGenerationEnabled = value; OnPropertyChanged(nameof(IsGenerationEnabled)); } }

        // 매칭된 리스트 (화면 표시용)
        public ObservableCollection<ScenarioMatchInfo> MatchedScenarios { get; set; } = new ObservableCollection<ScenarioMatchInfo>();

        public ICommand GenerateCommand { get; private set; }

        public View_Precompile()
        {
            InitializeComponent();
            this.DataContext = this;
            GenerateCommand = new DelegateCommand(async () => await RunGenerationAsync());
        }


        //경로 비교를 위한 정규화 헬퍼
        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            try
            {
                // 절대 경로로 변환 -> 소문자 변환 -> 끝의 슬래시 제거
                return Path.GetFullPath(path).ToLowerInvariant().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path.ToLowerInvariant().Trim();
            }
        }

        // 로그 추가 헬퍼
        private void AppendLog(string message)
        {
            // UI 스레드에서 실행 보장
            Application.Current.Dispatcher.Invoke(() =>
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                LogText += $"[{time}] {message}\n";
            });
        }

        private void Button_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            // 1. 현재 로그인한 사용자의 바탕화면 경로 가져오기
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // 2. 바탕화면 아래의 '분석데이터' 폴더 경로 조합
            string targetPath = Path.Combine(desktopPath, "분석데이터");

            // 3. 만약 '분석데이터' 폴더가 존재하지 않으면, 기본 바탕화면을 경로로 사용
            if (!Directory.Exists(targetPath))
            {
                targetPath = desktopPath;
            }

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = targetPath // ★ 다이얼로그가 처음 열릴 때의 경로 설정
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SelectedRootPath = dialog.FileName;
                ScanAndMatchFolders(SelectedRootPath);
            }
        }

        // [핵심] 폴더 스캔 및 시간 근접 매칭 알고리즘
        private async void ScanAndMatchFolders(string rootPath)
        {
            MatchedScenarios.Clear();
            LogText = ""; // 로그 초기화
            AppendLog($"경로 스캔 시작: {rootPath}");

            // 1. Raw / Target 폴더 찾기
            string rawBasePath = FindSubFolderPath(rootPath, "raw");
            string targetBasePath = FindSubFolderPath(rootPath, "target");
            string outputDir = Path.Combine(rootPath, "structured");

            if (rawBasePath == null)
            {
                AppendLog("❌ 오류: 'raw' 폴더를 찾을 수 없습니다.");
                IsGenerationEnabled = false;
                return;
            }

            // 2. 비동기 처리
            await Task.Run(() =>
            {
                // UI 스레드 접근 없이 로그 기록을 위해 Invoke 사용 (AppendLog 내부 구현에 따라 다름, 여기선 안전하게 처리됨 가정)
                AppendLog($"✓ Raw 경로 확인: {rawBasePath}");
                if (targetBasePath != null) AppendLog($"✓ Target 경로 확인: {targetBasePath}");
                else AppendLog("⚠ 경고: 'target' 폴더를 찾지 못했습니다. Raw 데이터만 처리합니다.");

                // A. 폴더 목록 파싱
                var rawList = ParseScenarioFolders(rawBasePath);
                var targetList = ParseScenarioFolders(targetBasePath);

                AppendLog($"\n=== 매칭 분석 중... ===");
                AppendLog($"📄 감지된 폴더 - Raw: {rawList.Count}개, Target: {targetList.Count}개");

                // B. 이미 생성된 JSON 파일 맵핑
                var existingMap = new Dictionary<string, string>();
                if (Directory.Exists(outputDir))
                {
                    var jsonFiles = Directory.GetFiles(outputDir, "Scenario*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            using (var doc = System.Text.Json.JsonDocument.Parse(json))
                            {
                                if (doc.RootElement.TryGetProperty("SourceLogPath", out var srcElem))
                                {
                                    // 저장된 경로를 가져와서 정규화(Normalize)
                                    string srcPath = NormalizePath(srcElem.GetString());

                                    if (!string.IsNullOrEmpty(srcPath) && !existingMap.ContainsKey(srcPath))
                                    {
                                        existingMap.Add(srcPath, Path.GetFileName(file));
                                    }
                                }
                            }
                        }
                        catch { /* 파일 읽기 에러 무시 */ }
                    }
                }

                // C. 매칭 로직 및 UI 업데이트
                // foreach 대신 인덱스를 알 수 있는 for문으로 변경
                for (int i = 0; i < rawList.Count; i++)
                {
                    var raw = rawList[i];

                    // 1. 기존 로직: 시간 기반 매칭 (10분 이내에서 가장 가까운 것)
                    var bestTarget = targetList
                        .Select(t => new { Info = t, Diff = Math.Abs((t.Timestamp - raw.Timestamp).TotalSeconds) })
                        .Where(x => x.Diff < 600)
                        .OrderBy(x => x.Diff)
                        .FirstOrDefault();

                    // 2. [신규 로직] 10분 내 매칭되는 데이터가 없다면, 폴더 오름차순(인덱스 순서)으로 강제 매칭
                    if (bestTarget == null && i < targetList.Count)
                    {
                        var fallbackTarget = targetList[i];
                        bestTarget = new
                        {
                            Info = fallbackTarget,
                            Diff = Math.Abs((fallbackTarget.Timestamp - raw.Timestamp).TotalSeconds)
                        };
                    }

                    // 비교할 때도 현재 Raw 경로를 정규화해서 비교
                    string normalizedRawPath = NormalizePath(raw.FullPath);
                    bool isAlreadyGen = existingMap.ContainsKey(normalizedRawPath);

                    var matchInfo = new ScenarioMatchInfo
                    {
                        RawFolderName = raw.Name,
                        RawFullPath = raw.FullPath,
                        RawTime = raw.Timestamp,
                        TargetFolderName = bestTarget?.Info.Name ?? "(없음)", // 짝이 모자라면 "(없음)" 처리됨
                        TargetFullPath = bestTarget?.Info.FullPath,
                        TimeDiffSec = bestTarget?.Diff ?? 0,

                        // 이미 존재하는지 체크
                        IsAlreadyGenerated = isAlreadyGen,
                        ExistingFileName = isAlreadyGen ? existingMap[normalizedRawPath] : null
                    };

                    Application.Current.Dispatcher.Invoke(() => MatchedScenarios.Add(matchInfo));
                }
            });

            // 3. 결과 종합 및 로그 출력 (메인 스레드)
            int totalCount = MatchedScenarios.Count;
            int existingCount = MatchedScenarios.Count(x => x.IsAlreadyGenerated);
            int newCount = totalCount - existingCount;

            if (totalCount > 0)
            {
                if (newCount == 0)
                {
                    // 모든 파일이 이미 존재함
                    IsGenerationEnabled = false;
                    AppendLog($"\n[결과] 모든 시나리오({totalCount}건)가 이미 생성되어 있습니다.");
                }
                else
                {
                    // 신규 작업 건이 있음
                    IsGenerationEnabled = true;
                    AppendLog($"\n[결과] 분석 완료: 총 {totalCount}건");
                    if (existingCount > 0) AppendLog($" - 기존 파일 존재: {existingCount}건 (제외됨)");
                    AppendLog($"👉 신규 생성 대기: {newCount}건");
                }
            }
            else
            {
                // 아예 파일이 없음
                IsGenerationEnabled = false;
                AppendLog("\n[결과] 처리할 시나리오 폴더를 찾지 못했습니다.");
            }
        }

        // [핵심] 변환 실행 (Start Generation)
        private async Task RunGenerationAsync()
        {
            IsGenerationEnabled = false; // 중복 클릭 방지
            string outputDir = Path.Combine(SelectedRootPath, "structured");
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            //생성해야 할 목록만 필터링 (이미 있는건 제외)
            var targetsToProcess = MatchedScenarios.Where(x => !x.IsAlreadyGenerated).ToList();

            if (targetsToProcess.Count == 0)
            {
                AppendLog("⚠ 모든 시나리오가 이미 생성되어 있습니다.");
                IsGenerationEnabled = true;
                return;
            }

            int nextNum = 1;
            var existingFiles = Directory.GetFiles(outputDir, "Scenario*.json");
            foreach (var file in existingFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file); // Scenario12
                if (name.StartsWith("Scenario") && int.TryParse(name.Substring(8), out int num))
                {
                    if (num >= nextNum) nextNum = num + 1;
                }
            }

            AppendLog("\n=== 변환 프로세스 시작 ===");
            AppendLog($"\n=== 신규 변환 시작 ({targetsToProcess.Count}개) ===");

            await Task.Run(() =>
            {
                int currentNum = nextNum;
                foreach (var match in targetsToProcess)
                {
                    AppendLog($"\n>>> 시나리오 {currentNum} 생성 중...");
                    AppendLog($"    - Raw: {match.RawFolderName}");
                    AppendLog($"    - Target: {match.TargetFolderName}");

                    bool result = Utils.DataTransformation.ConvertSingleScenario(
                  match.RawFullPath,
                  match.TargetFullPath,
                  outputDir,
                  currentNum,
                  AppendLog
              );

                    if (result)
                    {
                        AppendLog($"✅ 분석 데이터 생성 완료: Scenario{currentNum}.json");
                        //UI에 생성 완료 즉시 반영 (빨간불 들어오게)
                        match.IsAlreadyGenerated = true;
                        match.ExistingFileName = $"Scenario{currentNum}.json";
                    }
                    else
                    {
                        AppendLog($"❌ 생성 실패: Scenario{currentNum}");
                    }

                    currentNum++;
                }
            });

            AppendLog("\n=== 모든 작업이 완료되었습니다 ===");
            IsGenerationEnabled = true;
        }

        // 헬퍼: 하위 폴더 찾기
        private string FindSubFolderPath(string root, string targetName)
        {
            // 1. 바로 아래 확인
            string p1 = Path.Combine(root, targetName);
            if (Directory.Exists(p1)) return p1;

            // 2. logs/targetName 확인
            string p2 = Path.Combine(root, "logs", targetName);
            if (Directory.Exists(p2)) return p2;

            return null;
        }

        // 헬퍼: 시나리오 폴더 파싱
        private class FolderItem { public string Name; public string FullPath; public DateTime Timestamp; }
        private List<FolderItem> ParseScenarioFolders(string path)
        {
            var list = new List<FolderItem>();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return list;

            foreach (var d in Directory.GetDirectories(path))
            {
                var dirInfo = new DirectoryInfo(d);
                // "Scenario_yyyy-MM-ddTHHmmss" 파싱
                if (dirInfo.Name.StartsWith("Scenario_"))
                {
                    string datePart = dirInfo.Name.Substring(9); // "Scenario_".Length
                    if (DateTime.TryParseExact(datePart, "yyyy-MM-ddTHHmmss", null, DateTimeStyles.None, out DateTime dt))
                    {
                        list.Add(new FolderItem { Name = dirInfo.Name, FullPath = d, Timestamp = dt });
                    }
                }
            }
            return list.OrderBy(x => x.Timestamp).ToList();
        }

        // 로그창 오토 스크롤
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb) tb.ScrollToEnd();
        }
    }
}