using MLAH_Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Collections;
using System.Reflection;
using DevExpress.Xpf.Map;

namespace MLAH_Controller
{
    public  class CommonUtil
    {
        public static void ClearCollections(object obj)
        {
            if (obj == null)
                return;

            // 현재 객체의 모든 public 인스턴스 프로퍼티를 순회
            foreach (PropertyInfo prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // 프로퍼티가 읽기/쓰기가 가능하고, 값이 null이 아닌지 확인
                if (prop.CanRead && prop.CanWrite)
                {
                    var value = prop.GetValue(obj);
                    if (value == null)
                        continue;

                    // ICollection 인터페이스를 구현하는 경우
                    if (value is ICollection collection)
                    {
                        // Clear 메서드가 있다면 호출
                        MethodInfo clearMethod = prop.PropertyType.GetMethod("Clear");
                        clearMethod?.Invoke(value, null);
                    }
                    // 문자열이나 값 형식이 아니라면 재귀 호출
                    else if (!prop.PropertyType.IsValueType && prop.PropertyType != typeof(string))
                    {
                        ClearCollections(value);
                    }
                }
            }
        }

        // CommonUtil.cs 또는 다른 헬퍼 클래스
        public static void ShowPopup(Window popupWindow) // 특정 ViewName 대신 Window 객체를 직접 받도록 변경
        {
            // Owner를 현재 애플리케이션의 메인 윈도우(ShellView)로 지정합니다.
            // 이렇게 하면 팝업이 항상 메인 윈도우 위에 표시되고, OS가 관계를 명확히 인지합니다.
            popupWindow.Owner = Application.Current.MainWindow;

            // 만약 모달(Modal) 대화상자처럼 부모 창을 조작 못하게 하려면 ShowDialog()를 사용합니다.
            // popupWindow.ShowDialog(); 

            // 모달이 아니라면 Show()를 사용하되, Owner 지정이 중요합니다.
            // 기존의 페이드인 애니메이션 로직을 여기에 추가할 수 있습니다.
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.2)), // 팝업은 더 빠르게
            };

            if (!popupWindow.IsVisible)
            {
                popupWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                popupWindow.Show();
            }
            popupWindow.Activate();
        }

        public static void ShowFadeWindow(ViewName viewName)
        {
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                EasingFunction = new QuadraticEase()
            };

            switch (viewName)
            {
                case ViewName.Main:
                    {
                        //var window = View_MainView.SingletonInstance;
                        //if (!window.IsVisible) // 중복 방지
                        //{
                        //    window.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        //    window.Show();
                        //}
                        //window.Activate();
                    }
                    break;

                case ViewName.ScenarioView:
                    {
                        //var window = View_ScenarioView.SingletonInstance;
                        //if (!window.IsVisible) // 중복 방지
                        //{
                        //    window.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        //    window.Show();
                        //}
                        //window.Activate();
                    }
                    break;

                case ViewName.ScenarioObjectPopUp:
                    {
                        //View_ScenarioObject_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        {
                            var window = View_ScenarioObject_PopUp.SingletonInstance;
                            if (!window.IsVisible) // 중복 방지
                            {
                                window.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                                window.Show();
                            }
                            window.Activate();
                        }
                    }
                    break;

                case ViewName.AbnormalZone:
                    {
                        View_AbnormalZone_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        //View_ScenarioObject_PopUp.SingletonInstance.OnApplyTemplate();
                        View_AbnormalZone_PopUp.SingletonInstance.Show();
                    }
                    break;

                case ViewName.ConfigPopup:
                    {
                        View_Config_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        //View_ScenarioObject_PopUp.SingletonInstance.OnApplyTemplate();
                        View_Config_PopUp.SingletonInstance.Show();
                    }
                    break;

                case ViewName.BattlefieldEnv:
                    {
                        View_BattlefieldEnv_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        //View_ScenarioObject_PopUp.SingletonInstance.OnApplyTemplate();
                        View_BattlefieldEnv_PopUp.SingletonInstance.Show();
                    }
                    break;

                case ViewName.ObjectSet:
                    {
                        View_Object_Set_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        //View_ScenarioObject_PopUp.SingletonInstance.OnApplyTemplate();
                        View_Object_Set_PopUp.SingletonInstance.Show();
                    }
                    break;

                case ViewName.SetIM:
                    {
                        //View_SetIM_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        //View_ScenarioObject_PopUp.SingletonInstance.OnApplyTemplate();
                        //View_SetIM_PopUp.SingletonInstance.Show();
                    }
                    break;

                case ViewName.Complexity:
                    {
                        View_Complexity.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        //View_ScenarioObject_PopUp.SingletonInstance.OnApplyTemplate();
                        View_Complexity.SingletonInstance.Show();
                    }
                    break;

                case ViewName.UDPMornitoring:
                    {
                        //View_Mornitoring_UDP_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        //View_ScenarioObject_PopUp.SingletonInstance.OnApplyTemplate();
                        //View_Mornitoring_UDP_PopUp.SingletonInstance.Show();
                    }
                    break;

                //case ViewName.Mornitoring:
                //    {
                //        View_Mornitoring_PopUp.SingletonInstance.Topmost = false;
                        
                //        //View_Mornitoring_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                //        //View_ScenarioObject_PopUp.SingletonInstance.OnApplyTemplate();
                //        View_Mornitoring_PopUp.SingletonInstance.Show();

                //        View_Mornitoring_PopUp.SingletonInstance.Topmost = true;
                //    }
                //    break;
                case ViewName.SINILSim:
                    {
                        View_SINIL_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        View_SINIL_PopUp.SingletonInstance.Show();
                    }
                    break;
                case ViewName.MUMTMission:
                    {
                        //View_MUMTMission_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
                        //View_MUMTMission_PopUp.SingletonInstance.Show();
                    }
                    break;


                default:
                    break;

            }
        }


        //public static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        //{
        //    using (MemoryStream outStream = new MemoryStream())
        //    {
        //        // PngBitmapEncoder를 사용하여 PNG 형식으로 인코딩
        //        BitmapEncoder enc = new PngBitmapEncoder();
        //        enc.Frames.Add(BitmapFrame.Create(bitmapImage));
        //        enc.Save(outStream);
        //        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

        //        // 투명 배경이 포함된 Bitmap을 반환
        //        return new Bitmap(bitmap);
        //    }
        //}


        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        //  ini파일에서 읽기 .
        static public string Readini_Click(string Section, string Key, string Path)
        {
            StringBuilder temp = new StringBuilder(255);
            int ret = GetPrivateProfileString(Section, Key, "", temp, 255, Path);
            return temp.ToString();


            //strINIFilePath = AppDomain.CurrentDomain.BaseDirectory;
            //strINIFilePath = strINIFilePath + "SDRS_DisplaySW_CONFIG.ini";
            //strRTSP_URL = Readini_Click("RTSP_Set", "RTSP_URL", strINIFilePath);
            //streamUri = new Uri(strRTSP_URL);
        }

        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);

        // INI 파일에 쓰기
        static public void WriteINI(string Section, string Key, string Value, string Path)
        {
            WritePrivateProfileString(Section, Key, Value, Path);
        }

        public void OpenFile(object param)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            //openFileDialog.Filter = "Media Files|*.mp4;*.mkv;*.avi|All Files|*.*";
            openFileDialog.Filter = "xls Files|*.xls|All Files|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                //string excelFilePath = "C:\\Users\\user\\Desktop\\시간_궤도_자세데이터_속도방향접근_240201.xlsx";
                string excelFilePath = openFileDialog.FileName;


            }
            else
            {
                //탐색기 열고 취소 눌렀을 때
            }
        }

        // Config.ini에서 IPSet 값에 따라 IP 설정을 반환
        // 1=NEX1(실장비), 2=Local(로컬 테스트)
        public static class IPConfig
        {
            public static string GrpcServerIP { get; private set; } = "192.168.30.78";
            public static string UdpSendIP { get; private set; } = "192.168.30.78";
            public static string UdpRecvIP { get; private set; } = "192.168.20.200";

            public static void Load()
            {
                string iniPath = AppDomain.CurrentDomain.BaseDirectory + "Config.ini";
                string ipSetStr = Readini_Click("IPSet", "IPSet", iniPath);
                int.TryParse(ipSetStr, out int ipSet);

                switch (ipSet)
                {
                    case 1: // NEX1
                        GrpcServerIP = "192.168.20.200";
                        UdpSendIP = "192.168.20.201";
                        UdpRecvIP = "192.168.20.200";
                        break;
                    default: // Local
                        GrpcServerIP = "192.168.30.78";
                        UdpSendIP = "192.168.30.78";
                        UdpRecvIP = "192.168.20.200";
                        break;
                }

                System.Diagnostics.Debug.WriteLine($"[IPConfig] IPSet={ipSet}, gRPC={GrpcServerIP}, UDP Send={UdpSendIP}");
            }
        }

        // 지구 반지름(km)
        public const double EarthRadius = 6371;



        public static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        // 사각형의 너비 계산
        //public static double CalculateRectangleArea(List<PointLatLng> points)
        //{
        //    // 가정: points는 사각형의 네 꼭짓점을 나타냄
        //    double width = HaversineDistance(points[0], points[1]);
        //    double height = HaversineDistance(points[1], points[2]);

        //    return width * height;
        //}

        public static string GetHgtFileName(double latitude, double longitude)
        {
            int latFloor = (int)Math.Floor(latitude);
            int lonFloor = (int)Math.Floor(longitude);
            string latPrefix = latFloor >= 0 ? "N" : "S";
            string lonPrefix = lonFloor >= 0 ? "E" : "W";
            return $"{latPrefix}{Math.Abs(latFloor):D2}{lonPrefix}{Math.Abs(lonFloor):D3}.hgt";
        }

        public static int GetElevationFromCoords(double latitude, double longitude)
        {
            string filePath = GetHgtFileName(latitude, longitude);
            //return ReadElevation(filePath, latitude, longitude);
            filePath = "hgts\\" + filePath;

            if (!File.Exists(filePath))
            {
                //Console.WriteLine($"File not found: {filePath}");
                // 파일이 없을 경우 적절한 처리를 여기에 추가
                return -1; // 예시로 -1을 반환하지만, 필요에 따라 다른 방식으로 처리 가능
            }

            try
            {
                return ReadElevation(filePath, latitude, longitude);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is IOException)
            {
                //Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
                // 파일을 읽는 도중 발생할 수 있는 예외를 처리
                return -1; // 예시로 -1을 반환하지만, 필요에 따라 다른 방식으로 처리 가능
            }
        }

        public static int ReadElevation(string filePath, double latitude, double longitude)
        {
            int size = 1201; // SRTM3의 경우. SRTM1의 경우 3601을 사용
            double row = (latitude - Math.Floor(latitude)) * (size - 1);
            double col = (longitude - Math.Floor(longitude)) * (size - 1);
            int rowOffset = (int)Math.Round(row);
            int colOffset = (int)Math.Round(col);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int offset = ((size - rowOffset - 1) * size + colOffset) * 2;
                fs.Seek(offset, SeekOrigin.Begin);
                byte[] buffer = new byte[2];
                fs.Read(buffer, 0, 2);

                int elevation = BitConverter.IsLittleEndian ? BitConverter.ToInt16(new byte[] { buffer[1], buffer[0] }, 0) : BitConverter.ToInt16(buffer, 0);
                return elevation == -32768 ? 0 : elevation; // -32768은 누락된 데이터를 나타냅니다.
            }
        }


        public class BigEndianBinaryWriter : BinaryWriter
        {
            public BigEndianBinaryWriter(Stream stream) : base(stream) { }

            public override void Write(int value)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                base.Write(bytes);
            }

            public override void Write(uint value)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                base.Write(bytes);
            }

            public override void Write(short value)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                base.Write(bytes);
            }

            public override void Write(ushort value)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                base.Write(bytes);
            }

            public override void Write(long value)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                base.Write(bytes);
            }

            public override void Write(ulong value)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                base.Write(bytes);
            }

            public override void Write(float value)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                base.Write(bytes);
            }

            public override void Write(double value)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                base.Write(bytes);
            }

            // byte와 sbyte는 1바이트이므로 엔디안 변환 불필요
            // bool, string 등도 엔디안 영향 없음
        }

        

        /// <summary>
        /// 스트림에서 빅 엔디안(네트워크 바이트 순서)으로 데이터를 읽는 BinaryReader입니다.
        /// </summary>
        public class BigEndianBinaryReader : BinaryReader
        {
            public BigEndianBinaryReader(Stream stream) : base(stream) { }

            public override int ReadInt32()
            {
                // 4바이트를 읽고, 시스템이 리틀 엔디안이면 순서를 뒤집습니다.
                var bytes = base.ReadBytes(4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                return BitConverter.ToInt32(bytes, 0);
            }

            public override uint ReadUInt32()
            {
                // 4바이트를 읽고, 시스템이 리틀 엔디안이면 순서를 뒤집습니다.
                var bytes = base.ReadBytes(4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                return BitConverter.ToUInt32(bytes, 0);
            }

            public override float ReadSingle()
            {
                // 4바이트를 읽고, 시스템이 리틀 엔디안이면 순서를 뒤집습니다.
                var bytes = base.ReadBytes(4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                return BitConverter.ToSingle(bytes, 0);
            }

            public override double ReadDouble()
            {
                // 8바이트를 읽고, 시스템이 리틀 엔디안이면 순서를 뒤집습니다.
                var bytes = base.ReadBytes(8);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                return BitConverter.ToDouble(bytes, 0);
            }

            // ReadBoolean (1 byte) 과 ReadByte (1 byte)는 엔디안과 무관하므로
            // 오버라이드할 필요가 없습니다.
        }

        /// <summary>
        /// 빅 엔디안 바이트 배열의 특정 위치에서 4바이트를 읽어 Unsigned Integer로 변환합니다.
        /// </summary>
        /// <param name="buffer">소스 바이트 배열</param>
        /// <param name="startIndex">읽기 시작할 인덱스</param>
        /// <returns>변환된 uint 값</returns>
        public static uint ReadUInt32BigEndian(byte[] buffer, int startIndex)
        {
            if (buffer == null || buffer.Length < startIndex + 4)
            {
                throw new ArgumentException("버퍼가 너무 작거나 유효하지 않습니다.");
            }

            // 빅 엔디안: MSB (Most Significant Byte)가 가장 낮은 주소에 위치
            // buffer[startIndex]가 최상위 바이트
            return ((uint)buffer[startIndex] << 24) |
                   ((uint)buffer[startIndex + 1] << 16) |
                   ((uint)buffer[startIndex + 2] << 8) |
                   ((uint)buffer[startIndex + 3]);
        }

        /// <summary>
        /// 빅 엔디안 바이트 배열에서 4바이트를 읽어 Single(float)로 변환합니다.
        /// </summary>
        public static float ReadSingleBigEndian(byte[] buffer, int startIndex)
        {
            // 시스템이 리틀 엔디안일 경우, 바이트 순서를 뒤집어야 합니다.
            if (BitConverter.IsLittleEndian)
            {
                byte[] segment = new byte[4];
                Array.Copy(buffer, startIndex, segment, 0, 4);
                Array.Reverse(segment); // 복사본의 순서를 뒤집음
                return BitConverter.ToSingle(segment, 0);
            }
            else
            {
                return BitConverter.ToSingle(buffer, startIndex);
            }
        }

        /// <summary>
        /// 빅 엔디안 바이트 배열에서 8바이트를 읽어 Double로 변환합니다.
        /// </summary>
        public static double ReadDoubleBigEndian(byte[] buffer, int startIndex)
        {
            // 시스템이 리틀 엔디안일 경우, 바이트 순서를 뒤집어야 합니다.
            if (BitConverter.IsLittleEndian)
            {
                byte[] segment = new byte[8];
                Array.Copy(buffer, startIndex, segment, 0, 8);
                Array.Reverse(segment); // 복사본의 순서를 뒤집음
                return BitConverter.ToDouble(segment, 0);
            }
            else
            {
                return BitConverter.ToDouble(buffer, startIndex);
            }
        }

        public static class CustomGeometryHelper
        {
            public static (double x, double y) UnitPerp(GeoPoint A, GeoPoint B)
            {
                double mPerDegLat = 111000.0;
                double mPerDegLon = Math.Cos(((A.Latitude + B.Latitude) / 2) * Math.PI / 180.0) * 111000.0;
                double vx = (B.Longitude - A.Longitude) * mPerDegLon;
                double vy = (B.Latitude - A.Latitude) * mPerDegLat;
                double len = Math.Sqrt(vx * vx + vy * vy);
                if (len < 1e-6) return (0, 0);
                return (-vy / len, vx / len);
            }

            public static GeoPoint Offset(GeoPoint src, double ux, double uy, double dist)
            {
                double latDeg = (uy * dist) / 111000.0;
                double lonDeg = (ux * dist) / (Math.Cos(src.Latitude * Math.PI / 180.0) * 111000.0);
                return new GeoPoint(src.Latitude + latDeg, src.Longitude + lonDeg);
            }
        }


    }
}
