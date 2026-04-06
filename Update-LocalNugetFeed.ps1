# --- 설정 ---
# 원본 패키지들이 있는 폴더 (하위 폴더 포함)
$sourceDirectory = ".\NugetPackages_Source"

# 실제 NuGet 패키지 소스로 사용할 폴더 (이 폴더는 평평한 구조를 가짐)
$feedDirectory = ".\NugetPackages_Feed"

# --- 스크립트 실행 ---
Write-Host "로컬 NuGet 피드를 업데이트합니다..."

# 피드 폴더가 없으면 생성
if (-not (Test-Path $feedDirectory)) {
    New-Item -ItemType Directory -Force -Path $feedDirectory
    Write-Host "'$feedDirectory' 폴더를 생성했습니다."
}

# 원본 폴더 및 모든 하위 폴더에서 .nupkg 파일을 찾아 피드 폴더로 복사
Get-ChildItem -Path $sourceDirectory -Filter "*.nupkg" -Recurse | ForEach-Object {
    Write-Host "$($_.Name) 파일을 복사합니다..."
    Copy-Item -Path $_.FullName -Destination $feedDirectory -Force
}

Write-Host "✅ 업데이트 완료!"