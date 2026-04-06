@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION

REM --- 1. 오늘 날짜 설정 (YYMMDD 형식) ---
SET TODAY_DATE=%DATE:~2,2%%DATE:~5,2%%DATE:~8,2%

REM --- 2. 기본 경로 및 이름 설정 ---
SET "SOURCE_DIR=%~dp0"
SET "DEST_BASE=%USERPROFILE%\Desktop"

REM ==========================================================
REM  ★ 수정된 부분 ★
REM  경로 마지막의 '\' 문자가 ROBOCOPY의 따옴표를 이스케이프하는 오류를 방지하기 위해
REM  SOURCE_DIR의 마지막 문자가 '\'이면 제거합니다.
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
ECHO  솔루션 백업 시작
ECHO ==========================================================
ECHO.
ECHO  * 원본: %SOURCE_DIR%
ECHO  * 대상: %FINAL_DEST%
ECHO.

REM --- 4. 제외할 폴더 목록 ---
REM (사용자님이 수정한 목록으로 반영)
SET "EXCLUDE_DIRS=NugetPackages_Feed NugetPackages_Source obj .vs Tiles_0623_14"

ECHO  * 제외 폴더: %EXCLUDE_DIRS%
ECHO.
ECHO  복사 중... (용량에 따라 시간이 걸릴 수 있습니다)
ECHO.

REM --- 5. ROBOCOPY 실행 ---
REM 이제 "%SOURCE_DIR%"가 "C:\...Controller\" 가 아닌 "C:\...Controller" 로 올바르게 전달됩니다.
ROBOCOPY "%SOURCE_DIR%" "%FINAL_DEST%" /E /MT /XD %EXCLUDE_DIRS% /NFL /NJH /NJS /NP

ECHO.
ECHO ==========================================================
ECHO  복사 완료!
ECHO  바탕화면에 [ %FINAL_DEST% ] 폴더가 생성되었습니다.
ECHO ==========================================================
ECHO.

ENDLOCAL
PAUSE