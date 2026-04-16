@ECHO OFF
CHCP 65001 >NUL
SETLOCAL

SET "ROOT=%~dp0"
IF "%ROOT:~-1%"=="\" SET "ROOT=%ROOT:~0,-1%"

SET "PUBLISH_DIR=%ROOT%\publish"

ECHO.
ECHO ==========================================================
ECHO  Release 단일 실행파일 발행
ECHO ==========================================================
ECHO.
ECHO  출력: %PUBLISH_DIR%
ECHO.

IF EXIST "%PUBLISH_DIR%" (
    ECHO  기존 publish 폴더 삭제 중...
    RMDIR /S /Q "%PUBLISH_DIR%"
)

ECHO.
ECHO [1/4] MLAH_Controller 발행 중...
dotnet publish "%ROOT%\MLAH_Controller\MLAH_Controller.csproj" -c Release -r win-x64 -p:Platform=x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o "%PUBLISH_DIR%\MLAH_Controller" --nologo -v quiet
IF ERRORLEVEL 1 GOTO :FAIL

ECHO.
ECHO [2/4] MLAH_LogAnalyzer 발행 중...
dotnet publish "%ROOT%\MLAH_LogAnalyzer\MLAH_LogAnalyzer.csproj" -c Release -r win-x64 -p:Platform=x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o "%PUBLISH_DIR%\MLAH_LogAnalyzer" --nologo -v quiet
IF ERRORLEVEL 1 GOTO :FAIL

ECHO.
ECHO [3/4] MLAH_Mornitoring 발행 중...
dotnet publish "%ROOT%\MLAH_Mornitoring\MLAH_Mornitoring.csproj" -c Release -r win-x64 -p:Platform=x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o "%PUBLISH_DIR%\MLAH_Mornitoring" --nologo -v quiet
IF ERRORLEVEL 1 GOTO :FAIL

ECHO.
ECHO [4/4] MLAH_Mornitoring_UDP 발행 중...
dotnet publish "%ROOT%\MLAH_Mornitoring_UDP\MLAH_Mornitoring_UDP.csproj" -c Release -r win-x64 -p:Platform=x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o "%PUBLISH_DIR%\MLAH_Mornitoring_UDP" --nologo -v quiet
IF ERRORLEVEL 1 GOTO :FAIL

ECHO.
ECHO ==========================================================
ECHO  발행 완료!
ECHO  결과: %PUBLISH_DIR%
ECHO ==========================================================
ECHO.
GOTO :END

:FAIL
ECHO.
ECHO ==========================================================
ECHO  발행 실패!
ECHO ==========================================================

:END
ENDLOCAL
PAUSE
