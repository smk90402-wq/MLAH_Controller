using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DevExpress.Xpf.Grid;
using MLAH_Controller;
using MLAHInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static MLAH_Controller.ViewModel_Unit_Map;

namespace MLAH_Controller
{
    public class VisibilityToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 입력된 값이 Visibility 타입이고 Visible이면 Collapsed를 반환합니다.
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;  // 기본값으로 Visible 반환
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack은 필요하지 않을 수 있지만, 구현한다면 동일한 로직을 반대로 적용합니다.
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;  // 기본값으로 Collapsed 반환
        }
    }

    public class BytesToMegabytesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return (bytes / 1024d / 1024d).ToString("N2") + " MB"; // MB 단위로 변환
            }
            return "N/A"; // 값이 유효하지 않은 경우
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(); // 단방향 변환만 지원
        }
    }

    public class BytesToGigabytesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return (bytes / 1024d / 1024d / 1024d).ToString("N2") + " GB"; // GB 단위로 변환
            }
            return "N/A"; // 값이 유효하지 않은 경우
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(); // 단방향 변환만 지원
        }
    }

    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value;
        }
    }

    // [UI 스타일] C# 코드 내에서 사용할 색상 팔레트 (XAML 리소스와 일치시킴)
    public static class SciFiColors
    {
        // XAML의 Holo_Red_On (#FF3355)
        public static readonly Brush HoloRed = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3355"));
        // XAML의 Holo_Green_On (#00FFAA)
        public static readonly Brush HoloGreen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FFAA"));
        // 비활성/알 수 없음 상태 (Dark Gray)
        public static readonly Brush DarkGray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
    }

    // 1. 하드웨어/소프트웨어 통합 상태 (LED 점멸 트리거용)
    public class StatusToCombinedStatusConverter : IMultiValueConverter
    {
        // 이 컨버터는 LED 애니메이션 트리거(DataTrigger)가 "Green" / "Red" 문자열을 바라보므로
        // 리턴 값("Green", "Red")은 XAML의 DataTrigger Value와 정확히 일치해야 합니다.
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return "Red";

            if (int.TryParse(values[0]?.ToString(), out int status1) &&
                int.TryParse(values[1]?.ToString(), out int status2))
            {
                // 둘 다 1(정상)일 때만 Green 리턴 -> Green 애니메이션 작동
                return (status1 == 1 && status2 == 1) ? "Green" : "Red";
            }

            return "Red";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 상태 정보 클래스 (ViewModel이나 UI에서 사용)
    public class SwStatusInfo
    {
        public string StatusText { get; set; }
        public Brush StatusBrush { get; set; }
    }

    // 2. 하드웨어(PWR) 상태 컨버터
    public class HwStatusToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (!int.TryParse(value.ToString(), out int typeCode)) return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 0:
                    // 단순히 "비인가" 텍스트만 띄우기보다, 상태를 명확히 함
                    info.StatusText = "비인가 (OFF)";
                    info.StatusBrush = SciFiColors.HoloRed; // 형광 크림슨 적용
                    break;
                case 1:
                    info.StatusText = "인가 (ON)";
                    info.StatusBrush = SciFiColors.HoloGreen; // 형광 민트 적용
                    break;
                default:
                    info.StatusText = "UNKNOWN";
                    info.StatusBrush = SciFiColors.DarkGray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 3. 소프트웨어(SW) 상태 컨버터
    public class SwStatusToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (!int.TryParse(value.ToString(), out int typeCode)) return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 0:
                    info.StatusText = "오류 (ERROR)";
                    info.StatusBrush = SciFiColors.HoloRed; // 형광 크림슨 적용
                    break;
                case 1:
                    info.StatusText = "정상 (OK)";
                    info.StatusBrush = SciFiColors.HoloGreen; // 형광 민트 적용
                    break;
                case 2: // 혹시 모를 대기 상태 등을 위해 예비
                    info.StatusText = "대기 (IDLE)";
                    info.StatusBrush = Brushes.Orange;
                    break;
                default:
                    info.StatusText = "UNKNOWN";
                    info.StatusBrush = SciFiColors.DarkGray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            int typeCode;
            if (!int.TryParse(value.ToString(), out typeCode))
                return value; // 변환 실패 시 원본 그대로 반환

            switch (typeCode)
            {
                case 0: return "-";
                case 1: return "Point";
                case 2: return "Linear";
                case 3: return "Polygon";
                default: return "(Unknown)";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 일반적으로 OneWay 바인딩이면 ConvertBack 구현 필요 없음
            // 만약 "문자열 -> int" 역변환이 필요하다면 추가 로직을 작성
            return null;
        }
    }

    public class MissionTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            int typeCode;
            if (!int.TryParse(value.ToString(), out typeCode))
                return value; // 변환 실패 시 원본 그대로 반환

            switch (typeCode)
            {
                //case 0: return "N/A";
                //case 1: return "이륙목표지역";
                //case 2: return "전술집결지";
                //case 3: return "항공통제점";
                //case 4: return "통제권변경지역";
                //case 5: return "공격대기지역";
                //case 6: return "목표지역";
                //case 7: return "전투진지";
                //case 8: return "착륙대기지점";
                //case 9: return "착륙지점";
                //case 10: return "RTB지점";
                //default: return "(Unknown)";
                case 0: return "N/A";
                case 1: return "협업기동";
                case 2: return "협업수색공격";
                case 3: return "협업경계";
                case 4: return "협업공중부대엄호";
                case 5: return "협업지상부대엄호";
                case 6: return "협업도심수색";
                default: return "(Unknown)";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 일반적으로 OneWay 바인딩이면 ConvertBack 구현 필요 없음
            // 만약 "문자열 -> int" 역변환이 필요하다면 추가 로직을 작성
            return null;
        }
    }

    public class FlightTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is uint flightType)
            {
                switch (flightType)
                {
                    case 1: return "경로비행";
                    case 2: return "편대비행";
                    default: return "정의되지 않음";
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class UnitMissionTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            int typeCode;
            if (!int.TryParse(value.ToString(), out typeCode))
                return value; // 변환 실패 시 원본 그대로 반환

            switch (typeCode)
            {
                case 0: return "N/A";
                case 1: return "협업기동임무";
                case 2: return "협업수색공격임무";
                case 3: return "협업경계임무";
                case 4: return "협업공중부대엄호임무";
                case 5: return "협업지상부대엄호임무";
                case 6: return "협업도심수색공격임무";
                case 7: return "Reserved 1";
                case 8: return "Reserved 2";
                case 9: return "Reserved 3";
                default: return "(Unknown)";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 일반적으로 OneWay 바인딩이면 ConvertBack 구현 필요 없음
            // 만약 "문자열 -> int" 역변환이 필요하다면 추가 로직을 작성
            return null;
        }
    }


    public class RegionTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            int typeCode;
            if (!int.TryParse(value.ToString(), out typeCode))
                return value; // 변환 실패 시 원본 그대로 반환

            switch (typeCode)
            {
                case 0: return "N/A";
                case 1: return "전술집결지";
                case 2: return "통제권변경지역";
                case 3: return "ACP";
                case 4: return "공격대기지역";
                case 5: return "전투진지";
                case 6: return "목표지역";
                case 7: return "경계지역";
                case 8: return "탑재지대";
                case 9: return "착륙지대";
                case 10: return "중요시설";
                case 11: return "도서지역";
                default: return "(Unknown)";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 일반적으로 OneWay 바인딩이면 ConvertBack 구현 필요 없음
            // 만약 "문자열 -> int" 역변환이 필요하다면 추가 로직을 작성
            return null;
        }
    }
    public class UnitIsDoneToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            bool typeCode;
            if (!bool.TryParse(value.ToString(), out typeCode))
                return value; // 변환 실패 시 원본 그대로 반환

            switch (typeCode)
            {
                case true: return "완료";
                case false: return "미완료";
                default:             
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 일반적으로 OneWay 바인딩이면 ConvertBack 구현 필요 없음
            // 만약 "문자열 -> int" 역변환이 필요하다면 추가 로직을 작성
            return null;
        }
    }

    public class IsLeaderToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            short typeCode;
            if (!short.TryParse(value.ToString(), out typeCode))
                return value; // 변환 실패 시 원본 그대로 반환

            switch (typeCode)
            {
                case 1: return "지휘기";
                default: return "편대기";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 일반적으로 OneWay 바인딩이면 ConvertBack 구현 필요 없음
            // 만약 "문자열 -> int" 역변환이 필요하다면 추가 로직을 작성
            return null;
        }
    }

    public class AttackFlagToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            int typeCode;
            if (!int.TryParse(value.ToString(), out typeCode))
                return value; // 변환 실패 시 원본 그대로 반환

            switch (typeCode)
            {
                case 1: return "수동 공격 모드";
                case 2: return "자동 공격 모드";
                default: return "Unknown";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 일반적으로 OneWay 바인딩이면 ConvertBack 구현 필요 없음
            // 만약 "문자열 -> int" 역변환이 필요하다면 추가 로직을 작성
            return null;
        }
    }

    public class LAHStatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            enumEntityStatus typeCode;
            if (!enumEntityStatus.TryParse(value.ToString(), out typeCode))
                return value; // 변환 실패 시 원본 그대로 반환

            switch (typeCode)
            {
                case enumEntityStatus.StatusFlight: return "StatusFlight";
                case enumEntityStatus.StatusWait: return "StatusWait";
                case enumEntityStatus.StatusAttack: return "StatusAttack";
                case enumEntityStatus.StatusDeath: return "StatusDeath";
                default: return "Unknown";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 일반적으로 OneWay 바인딩이면 ConvertBack 구현 필요 없음
            // 만약 "문자열 -> int" 역변환이 필요하다면 추가 로직을 작성
            return null;
        }
    }

    public class VFModeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            int typeCode;
            if (!int.TryParse(value.ToString(), out typeCode))
                return value; // 변환 실패 시 원본 그대로 반환

            switch (typeCode)
            {
                case 0: return "VF";
                case 1: return "NonVF";
                default: return "Unknown";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 일반적으로 OneWay 바인딩이면 ConvertBack 구현 필요 없음
            // 만약 "문자열 -> int" 역변환이 필요하다면 추가 로직을 작성
            return null;
        }
    }

    /// <summary>
    /// 단위과제 객체목록 전시용 컨버터
    /// </summary>
    public class UnitObjectDisplayConverter : IValueConverter
    {
        // UnitObjectInfo 객체를 받아서 원하는 문자열(LAH(1), UAV(4), 자주포(13) 등)로 변환
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var unit = value as UnitObjectInfo;
            if (unit == null)
                return "";

            // ID가 1, 2, 3일 경우는 무조건 "LAH"
            if (unit.ID >= 1 && unit.ID <= 3)
            {
                return $"LAH({unit.ID})";
            }
            // ID가 4, 5, 6일 경우는 무조건 "UAV"
            else if (unit.ID >= 4 && unit.ID <= 6)
            {
                return $"UAV({unit.ID})";
            }
            // ID가 7 이상이면 unit.Type에 따라 문자열을 결정
            else
            {
                string typeName;
                switch (unit.Type)
                {
                    case 1:
                        typeName = "UAV";
                        break;
                    case 2:
                        typeName = "KUH";
                        break;
                    case 3:
                        typeName = "LAH";
                        break;
                    case 4:
                        typeName = "장갑차";
                        break;
                    case 5:
                        typeName = "탱크";
                        break;
                    case 6:
                        typeName = "방사포";
                        break;
                    case 7:
                        typeName = "곡사포";
                        break;
                    case 8:
                        typeName = "고정고사포";
                        break;
                    case 9:
                        typeName = "특작군인";
                        break;
                    case 10:
                        typeName = "자주포";
                        break;
                    case 11:
                        typeName = "트럭";
                        break;
                    case 12:
                        typeName = "작전차량";
                        break;
                    case 13:
                        typeName = "군인";
                        break;
                    default:
                        typeName = "Unknown";
                        break;
                }
                return $"{typeName}({unit.ID})";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 양방향 바인딩이 필요하지 않다면 예외를 던지거나 null을 리턴합니다.
            throw new NotImplementedException();
        }
    }

    public class ObjectHealthStatusToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!int.TryParse(value.ToString(), out int typeCode))
                return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 2:
                    info.StatusText = "비정상";
                    info.StatusBrush = Brushes.Red; // 0일 때 빨간색
                    break;
                case 1:
                    info.StatusText = "정상";
                    // 리소스에 정의된 브러시를 사용 (예: MLAH_COLOR_Value_Brush)
                    info.StatusBrush = Application.Current.FindResource("MLAH_COLOR_Value_Brush") as Brush;
                    break;
                default:
                    info.StatusText = "(Unknown)";
                    info.StatusBrush = Brushes.Gray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IdentificationStatusToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!int.TryParse(value.ToString(), out int typeCode))
                return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 2:
                    info.StatusText = "적군";
                    info.StatusBrush = Brushes.Red; 
                    break;
                case 1:
                    info.StatusText = "아군";
                    // 리소스에 정의된 브러시를 사용 (예: MLAH_COLOR_Value_Brush)
                    //info.StatusBrush = Application.Current.FindResource("MLAH_COLOR_Value_Brush") as Brush;
                    info.StatusBrush = Brushes.SkyBlue; 
                    break;
                default:
                    info.StatusText = "(Unknown)";
                    info.StatusBrush = Brushes.Gray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            int typeCode;
            if (!int.TryParse(value.ToString(), out typeCode))
                return value; // 변환 실패 시 원본 그대로 반환

            switch (typeCode)
            {
                case 0: return "유형없음";
                case 1: return "수직이착륙무인기";
                case 2: return "유인기동헬기";
                case 3: return "유인공격헬기";
                case 4: return "장갑차";
                case 5: return "탱크";
                case 6: return "방사포";
                case 7: return "곡사포";
                case 8: return "고정고사포";
                case 9: return "특작군인";
                case 10: return "자주포";
                case 11: return "트럭";
                case 12: return "작전차량";
                case 13: return "군인";
                default: return "(Unknown)";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 일반적으로 OneWay 바인딩이면 ConvertBack 구현 필요 없음
            // 만약 "문자열 -> int" 역변환이 필요하다면 추가 로직을 작성
            return null;
        }
    }

    public class ObjectPlatformMultiValueConverter : IMultiValueConverter
    {
        /// <summary>
        /// values[0] : 플랫폼 타입 (예: int 혹은 short)
        /// values[1] : 객체 유형 (예: int 혹은 short)
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return "(Unknown)";

            if (!int.TryParse(values[0]?.ToString(), out int platformType))
                return "(Unknown)";

            if (!int.TryParse(values[1]?.ToString(), out int objectType))
                return "(Unknown)";

            // objectType에 따라 플랫폼 타입 매핑 분기
            switch (objectType)
            {
                case 1: // 수직이착륙무인기
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "UAV";
                        default: return "(Unknown)";
                    }
                case 2: // 유인기동헬기
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "KUH";
                        // 필요한 경우 추가 항목...
                        default: return "(Unknown)";
                    }
                case 3: // 유인공격헬기
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "LAH";
                        default: return "(Unknown)";
                    }
                case 4: // 장갑차
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "K200";
                        case 2: return "K221";
                        case 3: return "K806";
                        case 4: return "K808";
                        case 5: return "M2010";
                        default: return "(Unknown)";
                    }
                case 5: // 탱크
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "K1A1";
                        case 2: return "K2";
                        case 3: return "T-55";
                        case 4: return "T-72";
                        case 5: return "천마호";
                        case 6: return "폭풍호";
                        default: return "(Unknown)";
                    }
                case 6: // 방사포
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "M1992 (122mm)";
                        default: return "(Unknown)";
                    }
                case 7: // 곡사포
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "M1938 (122mm)";
                        default: return "(Unknown)";
                    }
                case 8: // 고정고사포
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "ZPU-4";
                        case 2: return "ZPU-23";
                        case 3: return "KS-12";
                        case 4: return "KS-19";
                        case 5: return "SA-3";
                        default: return "(Unknown)";
                    }
                case 9: // 특작군인
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "특작군인 (SA-16)";
                        default: return "(Unknown)";
                    }
                case 10: // 자주포
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "K55A1";
                        case 2: return "K9";
                        default: return "(Unknown)";
                    }
                case 11: // 트럭
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "K311";
                        case 2: return "K511";
                        default: return "(Unknown)";
                    }
                case 12: // 작전차량
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "K131";
                        default: return "(Unknown)";
                    }
                case 13: // 군인
                    switch (platformType)
                    {
                        case 0: return "분류 없음";
                        case 1: return "군인(공중강습)";
                        default: return "(Unknown)";
                    }
                default:
                    return "(Unknown)";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 특정 필드를 제외하는 ContractResolver
    public class DynamicContractResolver : DefaultContractResolver
    {
        private readonly string[] _excludedProperties;

        public DynamicContractResolver(params string[] excludedProperties)
        {
            _excludedProperties = excludedProperties;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            return base.CreateProperties(type, memberSerialization)
                .Where(p => !_excludedProperties.Contains(p.PropertyName)) // 제외할 필드 필터링
                .ToList();
        }
    }

    public class HalfValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return d / 2;
            return DependencyProperty.UnsetValue;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NegativeHalfValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return -(d / 2);
            return DependencyProperty.UnsetValue;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HealthToStyleConverter : IValueConverter
    {
        // XAML에서 주입받을 두 가지 Style
        public Style NormalStyle { get; set; }
        public Style BlinkingRedStyle { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value를 문자열로 바꿔서 "100"이면 NormalStyle, 아니면 BlinkingRedStyle
            if (value != null && value.ToString() == "100")
            {
                return NormalStyle;
            }
            else
            {
                return BlinkingRedStyle;
            }
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectAbnormalHitToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!int.TryParse(value.ToString(), out int typeCode))
                return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 0:
                    info.StatusText = "피격 안됨";
                    info.StatusBrush = Application.Current.FindResource("MLAH_COLOR_Value_Brush") as Brush;
                    break;
                case 1:
                    info.StatusText = "피격 됨";
                    info.StatusBrush = Brushes.Red; // 0일 때 빨간색
                    break;
                default:
                    info.StatusText = "(Unknown)";
                    info.StatusBrush = Brushes.Gray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectAbnormalDataLinkToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!int.TryParse(value.ToString(), out int typeCode))
                return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 0:
                    info.StatusText = "연결";
                    info.StatusBrush = Application.Current.FindResource("MLAH_COLOR_Value_Brush") as Brush;
                    break;
                case 1:
                    info.StatusText = "연결 끊김";
                    info.StatusBrush = Brushes.Red; // 0일 때 빨간색
                    break;
                default:
                    info.StatusText = "(Unknown)";
                    info.StatusBrush = Brushes.Gray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectAbnormalFuelToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!int.TryParse(value.ToString(), out int typeCode))
                return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 0:
                    info.StatusText = "(Unknown)";
                    info.StatusBrush = Brushes.Gray;
                    break;
                case 1:
                    info.StatusText = "정상";
                    info.StatusBrush = Application.Current.FindResource("MLAH_COLOR_Value_Brush") as Brush;
                    break;
                case 2:
                    info.StatusText = "경고";
                    info.StatusBrush = Brushes.Red; // 0일 때 빨간색
                    break;
                case 3:
                    info.StatusText = "위험";
                    info.StatusBrush = Brushes.Red; // 0일 때 빨간색
                    break;
                default:
                    info.StatusText = "(Unknown)";
                    info.StatusBrush = Brushes.Gray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectAbnormalFuelDangerToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!int.TryParse(value.ToString(), out int typeCode))
                return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 0:
                    info.StatusText = "정상";
                    info.StatusBrush = Application.Current.FindResource("MLAH_COLOR_Value_Brush") as Brush;
                    break;
                case 1:
                    info.StatusText = "위험";
                    info.StatusBrush = Brushes.Red; // 0일 때 빨간색
                    break;
                default:
                    info.StatusText = "(Unknown)";
                    info.StatusBrush = Brushes.Gray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectAbnormalFuelZeroToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!int.TryParse(value.ToString(), out int typeCode))
                return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 0:
                    info.StatusText = "정상";
                    info.StatusBrush = Application.Current.FindResource("MLAH_COLOR_Value_Brush") as Brush;
                    break;
                case 1:
                    info.StatusText = "연료 없음";
                    info.StatusBrush = Brushes.Red; // 0일 때 빨간색
                    break;
                default:
                    info.StatusText = "(Unknown)";
                    info.StatusBrush = Brushes.Gray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectAbnormalCrashToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!int.TryParse(value.ToString(), out int typeCode))
                return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 0:
                    info.StatusText = "정상";
                    info.StatusBrush = Application.Current.FindResource("MLAH_COLOR_Value_Brush") as Brush;
                    break;
                case 1:
                    info.StatusText = "추락";
                    info.StatusBrush = Brushes.Red; // 0일 때 빨간색
                    break;
                default:
                    info.StatusText = "(Unknown)";
                    info.StatusBrush = Brushes.Gray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectAbnormalSensorStateToInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!int.TryParse(value.ToString(), out int typeCode))
                return null;

            var info = new SwStatusInfo();

            switch (typeCode)
            {
                case 1:
                    info.StatusText = "정상";
                    info.StatusBrush = Application.Current.FindResource("MLAH_COLOR_Value_Brush") as Brush;
                    break;
                case 2:
                    info.StatusText = "고장";
                    info.StatusBrush = Brushes.Red; // 0일 때 빨간색
                    break;
                default:
                    info.StatusText = "(Unknown)";
                    info.StatusBrush = Brushes.Gray;
                    break;
            }

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }


    public class IndexPlusOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // AlternationIndex는 int형이므로 +1 해준다.
            if (value is int index)
            {
                return index + 1;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToDateTimeConverter : IValueConverter
    {
        /// <summary>
        /// ViewModel → View 변환 (string → formatted string)
        /// </summary>
        /// <param name="value">바인딩으로부터 들어오는 값(문자열)</param>
        /// <param name="targetType">목표 타입(일반적으로 string/TextBlock.Text)</param>
        /// <param name="parameter">XAML에서 ConverterParameter로 넣을 수 있는 값(포맷 문자열)</param>
        /// <param name="culture">현재 UI 문화권</param>
        /// <returns>변환된 값(문자열)</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 만약 원본 값이 null 혹은 string이 아니면 그냥 반환
            if (!(value is string text)) return value;

            // DateTime으로 변환 시도
            if (DateTime.TryParse(text, out DateTime dt))
            {
                // XAML에서 ConverterParameter로 "yyyy-MM-dd HH:mm:ss"를 지정해줄 수 있음
                string format = parameter as string;
                if (!string.IsNullOrEmpty(format))
                {
                    return dt.ToString(format);
                }
                else
                {
                    // 파라미터가 없으면 기본 포맷 사용
                    return dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            else
            {
                // DateTime 변환 실패 시, 원본 문자열 그대로
                return text;
            }
        }

        /// <summary>
        /// View → ViewModel 변환 (여기서는 string을 다시 DateTime 등으로 되돌릴 필요가 있으면 사용)
        /// 일반적으로 Timestamp는 읽기만 한다면 ConvertBack은 그냥 NotImplementedException이 많습니다.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 필요 없다면 그대로 NotImplementedException
            throw new NotImplementedException();
        }
    }
    public class MessageNodeChildNodesSelector : IChildNodesSelector
    {
        public IEnumerable SelectChildren(object item)
        {
            if (item is MessageNode node)
            {
                return node.Children;
            }
            return null;
        }
    }

    public class ConditionalRotationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]은 Heading, values[1]은 Type이 될 것입니다.
            if (values.Length < 2 || !(values[0] is double) || !(values[1] is int))
            {
                return 0.0; // 기본값 (회전 없음)
            }

            var heading = (double)values[0];
            var type = (int)values[1];

            // Type이 1 (UAV) 또는 3 (LAH)일 경우에만 heading 값을 반환
            if (type == 1 || type == 3)
            {
                return heading;
            }

            // 그 외의 모든 타입은 0을 반환하여 회전하지 않도록 함
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HeadingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // object 타입을 double로 변환합니다.
            if (double.TryParse(value.ToString(), out double heading))
            {
                // 값이 음수이면 360을 더해서 양수로 만듭니다.
                if (heading < 0)
                {
                    return heading + 360;
                }
                return heading;
            }
            return value; // 변환할 수 없는 경우 원본 값을 반환합니다.
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 이 앱에서는 값을 다시 원래대로 되돌릴 필요가 없으므로 구현하지 않습니다.
            throw new NotImplementedException();
        }
    }

    public class StringContainsToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value as string;
            string parameterString = parameter as string;

            if (stringValue != null && parameterString != null)
            {
                return stringValue.Contains(parameterString);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EditLayerToActiveLayerConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values 순서: [0]Enum값, [1]InitLayer, [2]FlightLayer, [3]ProhibitedLayer

            // 데이터가 충분하지 않으면 null 반환
            if (values == null || values.Length < 4) return null;

            // 1. 현재 ViewModel의 편집 모드 (Enum)
            var currentMode = values[0];

            // Enum 타입 체크 및 캐스팅 (EditLayerType은 프로젝트의 실제 Enum 타입명으로 변경)
            if (currentMode is EditLayerType mode)
            {
                switch (mode)
                {
                    case EditLayerType.InitMission:
                        return values[1]; // InitMissionLayer 반환

                    case EditLayerType.FlightArea:
                        return values[2]; // FlightAreaPolygonEditLayer 반환

                    case EditLayerType.ProhibitedArea:
                        return values[3]; // ProhibitedAreaPolygonEditLayer 반환

                    case EditLayerType.TempInitMission:
                        return values[4]; // ProhibitedAreaPolygonEditLayer 반환

                    case EditLayerType.None:
                    default:
                        return null; // 편집 모드 아님
                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
    /// <summary>
    /// bool 값을 반전시키는 컨버터 (true -> false, false -> true)
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value;
        }
    }

    // [1] Bool 값에 따라 화살표 문자열 반환
    public class BoolToArrowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // True(펼쳐짐) -> ▲ (접기)
            // False(접힘) -> ▼ (펼치기)
            if (value is bool isExpanded && isExpanded)
                return "▲";
            else
                return "▼";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
