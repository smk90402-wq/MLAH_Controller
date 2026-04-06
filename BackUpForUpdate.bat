@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION

REM --- 1. 오늘 날짜 설정 (YYMMDD 형식) ---
SET TODAY_DATE=%DATE:~2,2%%DATE:~5,2%%DATE:~8,2%

REM --- 2. 기본 경로 및 이름 설정 ---
SET "SOURCE_DIR=%~dp0"
SET "DEST_BASE=%USERPROFILE%\Desktop"

REM ==========================================================
REM   경로 마지막의 '\' 문자가 ROBOCOPY의 따옴표를 이스케이프하는 오류 방지
IF "%SOURCE_DIR:~-1%"=="\" SET "SOURCE_DIR=%SOURCE_DIR:~0,-1%"
REM ==========================================================

SET "BASE_NAME=%TODAY_DATE%_controller"

REM --- 3. 대상 폴더 이름 및 버전 확인 ---
SET "FINAL_DEST=%DEST_BASE%\%BASE_NAME%"

IF EXIST "%FINAL_DEST%\" (
    SET "COUNTER=1"
    :VERSION_LOOP
    SET "FINAL_DEST=%DEST_BASE%\%BASE_NAME%_!COUNTER!"
    IF EXIST "%FINAL_DEST%\" (
        SET /A COUNTER+=1
        GOTO :VERSION_LOOP
    )
)

ECHO.
ECHO ==========================================================
ECHO   솔루션 백업 시작
ECHO ==========================================================
ECHO.
ECHO   * 원본: %SOURCE_DIR%
ECHO   * 대상: %FINAL_DEST%
ECHO.

REM --- 4. 제외 설정 ---

REM 4-1. 제외할 폴더 목록 (/XD)
SET "EXCLUDE_DIRS=NugetPackages_Feed NugetPackages_Source obj .vs Tiles_0623_14 bin Resources"

REM 4-2. 제외할 파일 및 확장자 목록 (/XF)
REM ★ 여기에 제외하고 싶은 파일명이나 확장자를 공백으로 구분해 적으세요.
REM 예: *.sln *.bat *.suo *.user *.pdb
SET "EXCLUDE_FILES=*.sln *.bat *.suo *.user *.pdb *.config *.ps1 *.csproj *.bak"

ECHO   * 제외 폴더: %EXCLUDE_DIRS%
ECHO   * 제외 파일: %EXCLUDE_FILES%
ECHO.
ECHO   복사 중... (용량에 따라 시간이 걸릴 수 있습니다)
ECHO.

REM --- 5. ROBOCOPY 실행 ---
REM ★ /XF %EXCLUDE_FILES% 옵션이 추가되었습니다.
ROBOCOPY "%SOURCE_DIR%" "%FINAL_DEST%" /E /MT /XD %EXCLUDE_DIRS% /XF %EXCLUDE_FILES% /NFL /NJH /NJS /NP

ECHO.
ECHO ==========================================================
ECHO   복사 완료!
ECHO   바탕화면에 [ %FINAL_DEST% ] 폴더가 생성되었습니다.
ECHO ==========================================================
ECHO.

ENDLOCAL
PAUSE