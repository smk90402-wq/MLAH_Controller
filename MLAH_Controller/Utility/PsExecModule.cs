using Microsoft.Xaml.Behaviors;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MLAH_Controller
{

    public class PsExecModule
    {
        public void ExecuteCommand()
        {
            // PsExec 경로 지정
            string psExecPath = @"PsExec.exe";

            // 원격 컴퓨터 주소 및 사용자 정보
            string remoteComputerName = "RTV-24N01";
            //string remoteComputerName = "192.168.50.64";
            string userName = "user";
            string password = "1234";

            // 실행할 명령
            string commandToRun = "notepad.exe";

            try
            {
                Process process = new Process();

                // PsExec 명령어 구성
                process.StartInfo.FileName = psExecPath;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                //process.StartInfo.Arguments = $"\\\\{remoteComputerName} -u {userName} -p {password} {commandToRun}";
                process.StartInfo.Arguments = $"\\\\{remoteComputerName} -u {userName}  {commandToRun}";

                // 명령 실행
                process.Start();
                //process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        public void ExecuteCommand(string remoteComputerName, string userName, string password, string ExecPath)
        {
            // PsExec 경로 지정
            string psExecPath = @"PsExec.exe";

            // 원격 컴퓨터 주소 및 사용자 정보
            //remoteComputerName = "127.0.0.1";
            //userName = "user";
            //password = "1234";

            // 실행할 명령
            //string commandToRun = "notepad.exe";

            try
            {
                Process process = new Process();

                // PsExec 명령어 구성
                process.StartInfo.FileName = psExecPath;
                //process.StartInfo.CreateNoWindow = true;
                //process.StartInfo.CreateNoWindow = false;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.UseShellExecute = true; // UseShellExecute를 true로 설정해야 Verb 사용 가능
                process.StartInfo.Verb = "runas"; // 관리자 권한으로 실행
                //process.StartInfo.RedirectStandardOutput = true;
                //process.StartInfo.RedirectStandardError = true;

                string remoteWorkingDirectory = System.IO.Path.GetDirectoryName(ExecPath);

                process.StartInfo.Arguments = $"-accepteula -d -e \\\\{remoteComputerName} -i 1 -u {userName} -p {password} -h -w \"{remoteWorkingDirectory}\" \"{ExecPath}\"";
                //process.StartInfo.Arguments = $"-accepteula -d \\\\{remoteComputerName} -i 1 -u {userName} -p \"\" -h \"{ExecPath}\"";
                

                // 작업 디렉토리 명시 (필요한 경우)
                //process.StartInfo.WorkingDirectory = Path.GetDirectoryName(psExecPath);
                process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

                // 명령 실행
                process.Start();
                //process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task QuitCommand(string remoteComputerName, string userName, string password, string ExecPath)
        {
            // PsExec 경로 지정
            string psExecPath = @"PsExec.exe";

            // 원격 컴퓨터 주소 및 사용자 정보
            //remoteComputerName = "127.0.0.1";
            //userName = "user";
            //password = "1234";

            // 실행할 명령
            //string commandToRun = "notepad.exe";

            // ExecPath에서 파일 이름만 추출 (확장자 없이)
            string processName = Path.GetFileNameWithoutExtension(ExecPath);
            processName = processName + ".exe";
            //PsExec \\원격컴퓨터명 - u 사용자명 - p 비밀번호 taskkill / IM notepad.exe / F

            //BootstrapPackagedGame.exe
            //MLAHProject.exe

            try
            {
                Process process = new Process();

                // PsExec 명령어 구성
                process.StartInfo.FileName = psExecPath;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = true; // UseShellExecute를 true로 설정해야 Verb 사용 가능
                process.StartInfo.Verb = "runas"; // 관리자 권한으로 실행
                process.StartInfo.Arguments = $"-accepteula -d -e \\\\{remoteComputerName} -i 1 -u {userName} -p {password} -h cmd /c taskkill /IM {processName} /T /F"; // -h 옵션 추가
                //process.StartInfo.Arguments = $"-accepteula -d \\\\{remoteComputerName} -i 1 -u {userName} -p \"\" -h cmd /c taskkill /IM {processName} /T /F"; // -h 옵션 추가


                // 명령 실행
                process.Start();
                //process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void QuitCommandUnreal(string remoteComputerName, string userName, string password, string ExecPath)
        {
            // PsExec 경로 지정
            string psExecPath = @"PsExec.exe";

            // 원격 컴퓨터 주소 및 사용자 정보
            //remoteComputerName = "127.0.0.1";
            //userName = "user";
            //password = "1234";

            // 실행할 명령
            //string commandToRun = "notepad.exe";

            // ExecPath에서 파일 이름만 추출 (확장자 없이)
            string processName = Path.GetFileNameWithoutExtension(ExecPath);
            //processName = processName + ".exe";
            processName = "MLAHProject.exe";
            //PsExec \\원격컴퓨터명 - u 사용자명 - p 비밀번호 taskkill / IM notepad.exe / F

            //BootstrapPackagedGame.exe
            //MLAHProject.exe

            try
            {
                Process process = new Process();

                // PsExec 명령어 구성
                process.StartInfo.FileName = psExecPath;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = true; // UseShellExecute를 true로 설정해야 Verb 사용 가능
                process.StartInfo.Verb = "runas"; // 관리자 권한으로 실행
                process.StartInfo.Arguments = $"-accepteula -d \\\\{remoteComputerName} -i 1 -u {userName} -p {password} -h cmd /c taskkill /IM {processName} /T /F"; // -h 옵션 추가
                //process.StartInfo.Arguments = $"-accepteula -d \\\\{remoteComputerName} -i 1 -u {userName} -p \"\" -h cmd /c taskkill /IM {processName} /T /F"; // -h 옵션 추가


                // 명령 실행
                process.Start();
                //process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        public async Task QuitCommandUnrealAsync(string remoteComputerName, string userName, string password, string ExecPath)
        {
            string psExecPath = @"PsExec.exe";
            string processName = "MLAHProject.exe"; // 언리얼 프로세스 이름은 고정

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = psExecPath;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false; // 비동기 대기를 위해 false로 설정
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                //process.StartInfo.Arguments = $"-accepteula \\\\{remoteComputerName} -u {userName} -p {password} cmd /c taskkill /IM {processName} /T /F";
                process.StartInfo.Arguments = $"-accepteula -d -e \\\\{remoteComputerName} -i 1 -u {userName} -p {password} -h cmd /c taskkill /IM {processName} /T /F"; // -h 옵션 추가

                process.Start();

                // 프로세스가 끝날 때까지 비동기적으로 대기 (최대 10초)
                await process.WaitForExitAsync(new CancellationTokenSource(10000).Token);
            }
            catch (Exception ex)
            {
                // 타임아웃 또는 기타 오류 발생 시 로그 기록
                Console.WriteLine($"Error quitting unreal command: {ex.Message}");
            }
        }

        public void ShutdownCommand(string remoteComputerName, string userName, string password)
        {
            // PsExec.exe의 경로 (절대 경로 또는 현재 작업 디렉토리에 복사되어 있다면 상대 경로 사용 가능)
            string psExecPath = @"PsExec.exe";  // 또는 @"C:\Tools\PsExec.exe"와 같이 절대 경로 사용

            // 원격 시스템에서 실행할 시스템 종료 명령어
            string shutdownCommand = "shutdown /s /t 0";

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = psExecPath;
                process.StartInfo.CreateNoWindow = false; // 콘솔창이 보일 수 있으나, shutdown은 빠르게 종료됨
                //process.StartInfo.UseShellExecute = true;   // runas를 사용하기 위해 true
                //process.StartInfo.Verb = "runas";             // 관리자 권한으로 실행

                // PsExec 인수 구성:
                // -accepteula : 라이선스 동의 처리
                // \\\\{remoteComputerName} : 원격 컴퓨터 지정
                // -u {userName} -p {password} : 원격 컴퓨터 로그인 정보
                // 마지막에 shutdown 명령어를 추가하여 시스템 종료 실행
                process.StartInfo.Arguments = $"-accepteula \\\\{remoteComputerName} -u {userName} -p {password} {shutdownCommand}";
                process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

                process.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        //public void WarmUpConnection(string remoteComputerName, string userName, string password)
        //{
        //    // 원격지에서 아무 작업도 하지 않는 'rem' 주석 명령어를 사용
        //    string warmUpCommand = "cmd /c \"rem\"";

        //    var startInfo = new ProcessStartInfo
        //    {
        //        FileName = @"PsExec.exe",
        //        Arguments = $"-accepteula -d \\\\{remoteComputerName} -u {userName} -p {password} {warmUpCommand}",
        //        CreateNoWindow = true,
        //        UseShellExecute = false
        //    };

        //    try
        //    {
        //        Process.Start(startInfo);
        //    }
        //    catch (Exception ex)
        //    {
        //        // 예열 실패는 치명적이지 않으므로 로그만 남깁니다.
        //        Console.WriteLine($"Warm-up failed for {remoteComputerName}: {ex.Message}");
        //    }
        //}

        public async Task WarmUpConnectionAsync(string remoteComputerName, string userName, string password)
        {
            string warmUpCommand = "cmd /c \"rem\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = @"PsExec.exe",
                // ▼▼▼ 핵심 수정: -i -h 옵션 추가 ▼▼▼
                Arguments = $"-accepteula \\\\{remoteComputerName} -i -e -h -u {userName} -p {password} {warmUpCommand}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    // 1. 프로세스가 끝날 때까지 비동기적으로 기다립니다.
                    await process.WaitForExitAsync();

                    // 2. 출력과 에러 스트림을 비동기적으로 읽어옵니다.
                    string error = await process.StandardError.ReadToEndAsync();

                    // 3. 결과를 확인하고 로그를 남깁니다.
                    if (process.ExitCode == 0)
                    {
                        Debug.WriteLine($"Warm-up SUCCESS for {remoteComputerName}.");
                    }
                    else
                    {
                        Debug.WriteLine($"Warm-up FAILED for {remoteComputerName}. Exit Code: {process.ExitCode}");
                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.WriteLine($"Error Details: {error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Warm-up CRITICAL FAILED for {remoteComputerName}: {ex.Message}");
            }
        }

    }


}