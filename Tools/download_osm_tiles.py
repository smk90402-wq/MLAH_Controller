"""
OSM 타일 다운로드 스크립트 (Zoom Level 16)
작전지역 3곳(작전구역/인제/화성)의 Level 16 타일을 다운로드합니다.
OSM 한국 타일은 한글 라벨로 렌더링됩니다.

사용법:
    python download_osm_tiles.py              # 3개 작전지역 다운로드
    python download_osm_tiles.py --threads 6  # 스레드 수 조정
"""

import os
import sys
import time
import math
import argparse
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from threading import Lock

# ======== 설정 ========
TILE_DIR = r"C:\Tiles_0623_14"
ZOOM_LEVEL = 16

# 작전지역 3곳 (여유 패딩 포함)
AREAS = [
    {
        "name": "작전구역 (인제/지포리 일대)",
        "lat_min": 37.82, "lat_max": 38.30,
        "lon_min": 127.10, "lon_max": 127.72,
    },
    {
        "name": "인제 훈련장",
        "lat_min": 37.86, "lat_max": 37.97,
        "lon_min": 128.11, "lon_max": 128.25,
    },
    {
        "name": "화성 홍익 일대",
        "lat_min": 37.19, "lat_max": 37.25,
        "lon_min": 126.95, "lon_max": 127.01,
    },
]

# OSM 타일 서버 (a/b/c 서브도메인 분산)
TILE_SERVERS = [
    "https://a.tile.openstreetmap.org/{z}/{x}/{y}.png",
    "https://b.tile.openstreetmap.org/{z}/{x}/{y}.png",
    "https://c.tile.openstreetmap.org/{z}/{x}/{y}.png",
]

USER_AGENT = "MLAH_Controller_TileDownloader/1.0"
RETRY_COUNT = 3
RETRY_DELAY = 2.0


# ======== 좌표 변환 ========
def lat_lon_to_tile(lat, lon, zoom):
    n = 2 ** zoom
    x = int((lon + 180.0) / 360.0 * n)
    lat_rad = math.radians(lat)
    y = int((1.0 - math.log(math.tan(lat_rad) + 1.0 / math.cos(lat_rad)) / math.pi) / 2.0 * n)
    return x, y


# ======== 다운로드 ========
print_lock = Lock()
stats = {"downloaded": 0, "skipped": 0, "failed": 0, "total": 0}


def download_tile(x, y, z, output_dir, server_idx):
    filename = f"os_{x}_{y}_{z}.png"
    filepath = os.path.join(output_dir, filename)

    if os.path.exists(filepath):
        with print_lock:
            stats["skipped"] += 1
        return True

    url = TILE_SERVERS[server_idx % len(TILE_SERVERS)].format(z=z, x=x, y=y)

    for attempt in range(RETRY_COUNT):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(req, timeout=15) as response:
                data = response.read()
                with open(filepath, "wb") as f:
                    f.write(data)
            with print_lock:
                stats["downloaded"] += 1
            return True
        except Exception:
            if attempt < RETRY_COUNT - 1:
                time.sleep(RETRY_DELAY)
            else:
                with print_lock:
                    stats["failed"] += 1
                return False
    return False


def print_progress(area_name=""):
    done = stats["downloaded"] + stats["skipped"] + stats["failed"]
    total = stats["total"]
    pct = done * 100 / total if total > 0 else 0
    sys.stdout.write(
        f"\r[{area_name}] {done:,}/{total:,} ({pct:.1f}%) | "
        f"다운로드: {stats['downloaded']:,} | "
        f"건너뜀: {stats['skipped']:,} | "
        f"실패: {stats['failed']:,}   "
    )
    sys.stdout.flush()


def get_tile_list(area, zoom):
    x_min, y_top = lat_lon_to_tile(area["lat_max"], area["lon_min"], zoom)
    x_max, y_bottom = lat_lon_to_tile(area["lat_min"], area["lon_max"], zoom)
    return x_min, x_max, y_top, y_bottom


def main():
    parser = argparse.ArgumentParser(description="OSM 타일 다운로드 (작전지역 Level 16)")
    parser.add_argument("--zoom", type=int, default=16, help="줌 레벨 (기본: 16)")
    parser.add_argument("--threads", type=int, default=4, help="다운로드 스레드 수 (기본: 4)")
    parser.add_argument("--delay", type=float, default=0.05, help="요청 간 딜레이 초 (기본: 0.05)")
    args = parser.parse_args()

    z = args.zoom
    output_dir = os.path.join(TILE_DIR, str(z))
    os.makedirs(output_dir, exist_ok=True)

    # 전체 타일 목록 생성
    all_tiles = []
    print("=" * 60)
    print(f"  OSM 타일 다운로드 (Zoom Level {z})")
    print(f"  저장 경로: {output_dir}")
    print("=" * 60)

    for area in AREAS:
        x_min, x_max, y_min, y_max = get_tile_list(area, z)
        count = (x_max - x_min + 1) * (y_max - y_min + 1)
        print(f"\n  {area['name']}")
        print(f"    위경도: ({area['lat_min']}, {area['lon_min']}) ~ ({area['lat_max']}, {area['lon_max']})")
        print(f"    타일: X={x_min}~{x_max}, Y={y_min}~{y_max}")
        print(f"    개수: {count:,}")

        idx = len(all_tiles)
        for x in range(x_min, x_max + 1):
            for y in range(y_min, y_max + 1):
                all_tiles.append((x, y, z, output_dir, idx))
                idx += 1

    total = len(all_tiles)
    print(f"\n  총 타일 수: {total:,}")
    print(f"  예상 용량: {total * 15 / 1024:.1f} MB")
    print(f"  스레드: {args.threads}")
    print("=" * 60)

    confirm = input("\n다운로드를 시작하시겠습니까? (y/n): ").strip().lower()
    if confirm != "y":
        print("취소되었습니다.")
        return

    stats["total"] = total
    start_time = time.time()
    print()

    with ThreadPoolExecutor(max_workers=args.threads) as executor:
        futures = {}
        for tile_args in all_tiles:
            future = executor.submit(download_tile, *tile_args)
            futures[future] = tile_args
            time.sleep(args.delay / args.threads)

        last_print = 0
        for future in as_completed(futures):
            now = time.time()
            if now - last_print > 0.5:
                print_progress("전체")
                last_print = now

    print_progress("전체")
    elapsed = time.time() - start_time

    print(f"\n\n완료! 소요 시간: {elapsed:.0f}초")
    print(f"다운로드: {stats['downloaded']:,}, 건너뜀: {stats['skipped']:,}, 실패: {stats['failed']:,}")
    print(f"저장 경로: {output_dir}")

    if stats["failed"] > 0:
        print(f"\n실패한 타일이 {stats['failed']:,}개 있습니다. 스크립트를 다시 실행하면 이어받기됩니다.")

    print(f"\n다운로드 완료 후 View_Unit_Map.xaml의 MaxZoomLevel을 {z}으로 변경하세요.")


if __name__ == "__main__":
    main()
