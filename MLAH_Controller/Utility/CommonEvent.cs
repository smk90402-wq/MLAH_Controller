//using GMap.NET;
//using GMap.NET.WindowsForms;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Xpf.Map;
using DevExpress.Map;
using MLAH_Controller;
using System.Net;
using System.Net.Sockets;

namespace MLAH_Controller
{
    public class CommonEvent
    {
        public delegate void RequestFadeOut(ViewName viewName);
        public static RequestFadeOut OnRequestFadeOut;

        public delegate void MapPOSSelect(double lat, double lon, double alt);
        public static MapPOSSelect OnMapPOSSelect;

        public delegate void AbnormalZoneRectSelectStart(double lat, double lon);
        public static AbnormalZoneRectSelectStart OnAbnormalZoneRectSelectStart;

        public delegate void AbnormalZoneRectSelectEnd(double lat, double lon);
        public static AbnormalZoneRectSelectEnd OnAbnormalZoneRectSelectEnd;

        public delegate void ConfirmAbnormalZone();
        public static ConfirmAbnormalZone OnConfirmAbnormalZone;

        public delegate void ConfirmCoBaseMissionWaypoint();
        public static ConfirmCoBaseMissionWaypoint OnConfirmCoBaseMissionWaypoint;

        public delegate void BattlefieldEnvSetNight(bool IsNight);
        public static BattlefieldEnvSetNight OnBattlefieldEnvSetNight;

        public delegate void BattlefieldEnvSetLocation(double lat, double lon);
        public static BattlefieldEnvSetLocation OnBattlefieldEnvSetLocation;

        public delegate void IMWaypointInit();
        public static IMWaypointInit OnIMWaypointInit;

        //public delegate void IMWaypointsMakeFinish(List<PointLatLng> IMWaypoints);
        //public static IMWaypointsMakeFinish OnIMWaypointsMakeFinish;

        //public delegate void TargetMovePlanMakeFinish(List<PointLatLng> Waypoints);
        //public static TargetMovePlanMakeFinish OnTargetMovePlanMakeFinish;

        public delegate void TargetMovePlanSaveFinish();
        public static TargetMovePlanSaveFinish OnTargetMovePlanSaveFinish;

        public delegate void TargetMovePlanWaypointInit();
        public static TargetMovePlanWaypointInit OnTargetMovePlanWaypointInit;

        public delegate void MessageReceivedHandler(IMessage messageInstance, ContextInfo contextInfo);
        public delegate Task AsyncMessageReceivedHandler(IMessage messageInstance, ContextInfo contextInfo);

        //단위과제 이벤트 모음

        //개발용 LAH 이동경로 완성
        public delegate void DevelopPathPlanSet(List<CoordPoint> DevelopPathPlanList);
        public static DevelopPathPlanSet OnDevelopPathPlanSet;

        //개발용 LAH 이동경로 저장
        public delegate void DevelopPathPlanAdd(int PathID, List<CoordPoint> DevelopPathPlanList);
        public static DevelopPathPlanAdd OnDevelopPathPlanAdd;

        public delegate void DevelopPathPlanEdit(int OverlayID, double lat, double lon);
        public static DevelopPathPlanEdit OnDevelopPathPlanEdit;

        public delegate void DevelopPathPlanRemove(int OverlayID);
        public static DevelopPathPlanRemove OnDevelopPathPlanRemove;






        //이동경로 완성
        public delegate void PathPlanSet(List<CoordPoint> PathPlanList);
        public static PathPlanSet OnPathPlanSet;

        //지도에서 아이콘 클릭
        public delegate void MapUnitObjectClicked(uint ID);
        public static MapUnitObjectClicked OnMapUnitObjectClicked;

        //public static event Action<UnitObjectInfo> OnFocusTargetChanged;

        //이동경로 저장
        public delegate void PathPlanSave();
        public static PathPlanSave OnPathPlanSave;

        //초기임무정보 저장
        public delegate void INITMissionSave();
        public static INITMissionSave OnINITMissionSave;


        public delegate void LAHMissionPlanReceived(LAHMissionPlan LahPlan);
        public static LAHMissionPlanReceived OnLAHMissionPlanReceived;

        public delegate void UAVMissionPlanReceived(UAVMissionPlan UAVPlan);
        public static UAVMissionPlanReceived OnUAVMissionPlanReceived;

        //public delegate void MissionPlanOptionInfoReceived(MissionPlanOptionInfo InputOption);
        //public static MissionPlanOptionInfoReceived OnMissionPlanOptionInfoReceived;

        public static Action<MissionPlanOptionInfo> OnMissionPlanOptionInfoReceived;

        public static Action<PilotDecision> OnPilotDecisionReceived;

        public static Action<MissionUpdatewithoutPilotDecision> OnMissionUpdateWithoutDecisionReceived;

        public static Action<MissionResultData> OnMissionResultDataReceived;

        public delegate void UavMalFunctionStateReceived(UdpReceiveResult result);
        public static UavMalFunctionStateReceived OnUavMalFunctionStateReceived;


        #region 초기임무정보 설정 / 저장

        //초기임무정보 포인트 클릭
        public delegate void INITMissionPointSet(double lat, double lon);
        public static INITMissionPointSet OnINITMissionPointSet;

        public delegate void INITMissionPointAdd(CustomMapPoint InputMapPoint);
        public static INITMissionPointAdd OnINITMissionPointAdd;

        //초기임무정보 선 완성
        public delegate void INITMissionLinearSet(List<GeoPoint> LinearList);
        public static INITMissionLinearSet OnINITMissionLinearSet;

        //초기임무정보 선 완성 - 단위과제
        //public delegate void INITMissionLineSet(List<GeoPoint> CenterPoints, List<GeoPoint> LinearList, int width);
        public delegate void INITMissionLineSet(LinearMissionResultSet result);
        public static INITMissionLineSet OnINITMissionLineSet;
        

        //초기임무정보 선 저장 - 단위과제
        //public delegate void INITMissionLineAdd(List<GeoPoint> LinearList, int width);
        //public static INITMissionLineAdd OnINITMissionLineAdd;

        //초기임무정보 다각형 완성
        public delegate void INITMissionPolygonSet(List<GeoPoint> PolygonList);
        public static INITMissionPolygonSet OnINITMissionPolygonSet;

        //초기임무정보 다각형 수정
        public delegate void INITMissionPolygonUpdated(List<GeoPoint> PolygonList);
        public static INITMissionPolygonSet OnINITMissionPolygonUpdated;

        //초기임무정보 다각형 저장
        public delegate void INITMissionPolygonAdd(List<CustomMapPolygon> PolygonList);
        public static INITMissionPolygonAdd OnINITMissionPolygonAdd;

        //초기임무정보 선 저장
        public delegate void INITMissionPolyLineAdd(CustomMapLine PolygonList);
        public static INITMissionPolyLineAdd OnINITMissionPolyLineAdd;

        //초기임무정보 선-다각형 저장
        public delegate void INITMissionLinePolygonAdd(List<CustomMapPolygon> PolygonList);
        public static INITMissionLinePolygonAdd OnINITMissionLinePolygonAdd;

        public static Action<CustomMapPoint> OnINITMissionLineLabelAdd;

        //전시 체크박스 이벤트
        public delegate void INITMissionDisplayChanged(bool IsDisplay);
        public static INITMissionDisplayChanged OnINITMissionDisplayChanged;

        #endregion 초기임무정보 설정 / 저장



        #region 비행참조정보 설정 / 저장

        //TakeOver 통제권 획득
        public delegate void TakeOverPointSet(double lat, double lon);
        public static TakeOverPointSet OnTakeOverPointSet;

        public delegate void TakeOverPointAdd(int OverlayID, float lat, float lon);
        public static TakeOverPointAdd OnTakeOverPointAdd;

        public delegate void TakeOverPointEdit(int OverlayID, double lat, double lon);
        public static TakeOverPointEdit OnTakeOverPointEdit;

        public delegate void TakeOverPointRemove(int OverlayID);
        public static TakeOverPointRemove OnTakeOverPointRemove;

        //TakeOver 통제권 반납
        public delegate void HandOverPointSet(double lat, double lon);
        public static HandOverPointSet OnHandOverPointSet;

        public delegate void HandOverPointAdd(int OverlayID, float lat, float lon);
        public static HandOverPointAdd OnHandOverPointAdd;

        public delegate void HandOverPointEdit(int OverlayID, double lat, double lon);
        public static HandOverPointEdit OnHandOverPointEdit;

        public delegate void HandOverPointRemove(int OverlayID);
        public static HandOverPointRemove OnHandOverPointRemove;

        //RTB
        public delegate void RTBPointSet(double lat, double lon);
        public static RTBPointSet OnRTBPointSet;

        public delegate void RTBPointAdd(int OverlayID, float lat, float lon);
        public static RTBPointAdd OnRTBPointAdd;

        public delegate void RTBPointEdit(int OverlayID, double lat, double lon);
        public static RTBPointEdit OnRTBPointEdit;

        public delegate void RTBPointRemove(int OverlayID);
        public static RTBPointRemove OnRTBPointRemove;

        //FlightArea 비행가능구역
        public delegate void FlightAreaPolygonSet(List<GeoPoint> Polygon);
        public static FlightAreaPolygonSet OnFlightAreaPolygonSet;

        public delegate void FlightAreaPolygonAdd(int OverlayID, CustomMapPolygon Polygon);
        public static FlightAreaPolygonAdd OnFlightAreaPolygonAdd;

        public delegate void FlightAreaPolygonEdit(int OverlayID, CustomMapPolygon Polygon);
        public static FlightAreaPolygonEdit OnFlightAreaPolygonEdit;

        public delegate void FlightAreaPolygonRemove(int OverlayID);
        public static FlightAreaPolygonRemove OnFlightAreaPolygonRemove;

        //ProhibitedArea 비행가능구역
        public delegate void ProhibitedAreaPolygonSet(List<GeoPoint> Polygon);
        public static ProhibitedAreaPolygonSet OnProhibitedAreaPolygonSet;

        public delegate void ProhibitedAreaPolygonAdd(int OverlayID, CustomMapPolygon Polygon);
        public static ProhibitedAreaPolygonAdd OnProhibitedAreaPolygonAdd;

        public delegate void ProhibitedAreaPolygonEdit(int OverlayID, CustomMapPolygon Polygon);
        public static ProhibitedAreaPolygonEdit OnProhibitedAreaPolygonEdit;

        public delegate void ProhibitedAreaPolygonRemove(int OverlayID);
        public static ProhibitedAreaPolygonRemove OnProhibitedAreaPolygonRemove;

        //비행가능구역/비행금지구역 수정할 때
        public static Action<List<GeoPoint>> OnFlightAreaPolygonUpdated;
        public static Action<List<GeoPoint>> OnProhibitedAreaPolygonUpdated;

        #endregion 비행참조정보 설정 / 저장



        public delegate void ClientSessionCleanedUp(string Peer);
        public static ClientSessionCleanedUp OnClientSessionCleanedUp;

    }
}
