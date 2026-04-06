using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitMiracle.LibTiff.Classic;
using System.IO;

namespace MLAH_LogAnalyzer
{
    public class SrtmReader
    {
        private Tiff _tiff;
        private int _width;
        private int _height;

        // SRTM 62_05의 고정 범위 (파일명에 따라 다름)
        // 62_05 = 경도 125~130, 위도 35~40
        private const double MinLon = 125.0;
        private const double MaxLat = 40.0;
        // [확인 후 삭제] 미사용 상수 - 어디서도 참조되지 않음
        //private const double CellSize = 0.00083333333333333;

        public SrtmReader(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("SRTM file not found", filePath);

            _tiff = Tiff.Open(filePath, "r");
            if (_tiff == null)
                throw new Exception("Could not open TIFF file.");

            // 이미지 크기 가져오기
            FieldValue[] widthField = _tiff.GetField(TiffTag.IMAGEWIDTH);
            FieldValue[] heightField = _tiff.GetField(TiffTag.IMAGELENGTH);

            _width = widthField[0].ToInt();
            _height = heightField[0].ToInt();
        }

        public void Close()
        {
            _tiff?.Dispose();
        }

        /// <summary>
        /// 특정 위경도의 해발 고도(미터)를 반환합니다.
        /// 범위 밖이면 -32768 (No Data) 반환
        /// </summary>
        public short GetElevation(double lat, double lon)
        {
            // 1. 범위를 벗어났는지 체크
            if (lon < MinLon || lon >= MinLon + 5.0 || lat > MaxLat || lat <= MaxLat - 5.0)
            {
                return -32768; // 데이터 없음 (또는 0 처리)
            }

            // 2. 위경도를 픽셀 좌표(x, y)로 변환
            // X: 왼쪽(125도)에서 얼마나 떨어졌나
            // Y: 위쪽(40도)에서 얼마나 내려왔나 (TIFF는 위가 0)
            int x = (int)((lon - MinLon) * (_width / 5.0));
            int y = (int)((MaxLat - lat) * (_height / 5.0));

            // 좌표 범위 안전 체크
            if (x < 0) x = 0; if (x >= _width) x = _width - 1;
            if (y < 0) y = 0; if (y >= _height) y = _height - 1;

            // 3. 해당 스캔라인(행) 읽기
            // 16-bit signed integer (short) 데이터라고 가정
            byte[] scanline = new byte[_tiff.ScanlineSize()];
            _tiff.ReadScanline(scanline, y);

            // 4. 바이트 배열에서 값 추출 (Little Endian 기준)
            // 1픽셀당 2바이트씩 차지하므로 인덱스는 x * 2
            int index = x * 2;
            short elevation = BitConverter.ToInt16(scanline, index);

            return elevation;
        }
    }
}
