using System;
using System.IO;

public class PathManager
{
    // 사용할 환경변수 이름
    private const string EnvVarName = "OSM_TILE_PATH";
    // 환경변수가 없을 때 사용할 기본 경로 (C 드라이브 최상단 추천)
    private const string DefaultPath = @"C:\Tiles_0623_14";

    public static string GetOrSetLibraryPath()
    {
        // 1. 사용자 레벨의 환경변수 가져오기
        string path = Environment.GetEnvironmentVariable(EnvVarName, EnvironmentVariableTarget.User);

        // 2. 환경변수가 없거나 비어있다면?
        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine($"[Info] 환경변수 '{EnvVarName}'가 없습니다. 기본 경로로 설정합니다.");

            path = DefaultPath;

            // 2-1. 폴더가 실제로 있는지 확인 (없으면 생성하거나 에러 처리)
            if (!Directory.Exists(path))
            {
                // 선택지 A: 없으면 폴더를 만들어버린다.
                try
                {
                    Directory.CreateDirectory(path);
                    Console.WriteLine($"[Info] 경로 생성 완료: {path}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] 폴더 생성 실패: {ex.Message}");
                    // 여기서 return null 하거나 예외를 던져야 함
                }
            }

            // 2-2. 환경변수를 '사용자(User)' 레벨에 영구 저장
            // 주의: EnvironmentVariableTarget.User는 관리자 권한 없이도 대부분 가능하지만,
            //       Machine(시스템 전체)은 관리자 권한이 필수입니다.
            Environment.SetEnvironmentVariable(EnvVarName, path, EnvironmentVariableTarget.User);

            // 2-3. **중요**: 현재 실행 중인 프로세스에도 즉시 적용 (이래야 바로 갖다 씁니다)
            Environment.SetEnvironmentVariable(EnvVarName, path, EnvironmentVariableTarget.Process);
        }
        else
        {
            Console.WriteLine($"[Info] 환경변수 감지됨: {path}");
        }

        return path;
    }
}