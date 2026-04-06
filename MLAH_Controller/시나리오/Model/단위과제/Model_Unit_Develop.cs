using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel.Channels;
using MLAH_Controller;
using MLAHInterop;

namespace MLAH_Controller
{
    public class Model_Unit_Develop : CommonBase
    {

        private ObservableCollection<Unit_LAH_MovePlans> _unit_LAH_MovePlansList = new ObservableCollection<Unit_LAH_MovePlans>();
        /// <summary>
        /// 테스트용 유인공격헬기 Plan
        /// </summary>
        public ObservableCollection<Unit_LAH_MovePlans> unit_LAH_MovePlansList
        {
            get
            {
                return _unit_LAH_MovePlansList;
            }
            set
            {
                _unit_LAH_MovePlansList = value;
                OnPropertyChanged("unit_LAH_MovePlansList");
            }
        }

        //private ObservableCollection<Unit_LAH_MovePlan> _unit_LAH_MovePlans = new ObservableCollection<Unit_LAH_MovePlan> ();
        ///// <summary>
        ///// 테스트용 유인공격헬기 Plan
        ///// </summary>
        //public ObservableCollection<Unit_LAH_MovePlan> unit_LAH_MovePlans
        //{
        //    get
        //    {
        //        return _unit_LAH_MovePlans;
        //    }
        //    set
        //    {
        //        _unit_LAH_MovePlans = value;
        //        OnPropertyChanged("unit_LAH_MovePlans");
        //    }
        //}

        public ScenarioMissionResponse Convert_LAHPlan_To_GrpcLAH(ObservableCollection<Unit_LAH_MovePlan> Input)
        {
            var grpc_lah = new ScenarioMissionResponse();
            grpc_lah.Message = "ScenarioMissionResponse";
            int TempWaypointID = 0;

            foreach (var item in Input)
            {
                var grpc_lah_item = new ScenarioMission();

                //grpc_lah_item.Type = "LAH";
                grpc_lah_item.Type = "UAV";
                //grpc_lah_item.Id = (int)item.LAHID;
                //Vector4 vector_to_send = new Vector4();

//                message CameraAction
//{
//                    int32 mode = 1;

//                    double fov = 2;

//                    double x = 3;
//                    double y = 4;
//                    double z = 5;
//                    double w = 6;

//                    double sweeptime = 7;
//                }

                grpc_lah_item.Cameramission = new CameraAction();

                grpc_lah_item.Cameramission.X = 0;
                grpc_lah_item.Cameramission.Y = 0;
                grpc_lah_item.Cameramission.Z = 0;
                grpc_lah_item.Cameramission.W = 0;
                grpc_lah_item.Cameramission.Sweeptime = 0;

                grpc_lah_item.Cameramission.Mode = item.CameraMode;
                grpc_lah_item.Cameramission.Fov = item.CameraFOV;

                switch (item.CameraMode)
                {
                    case 0:
                        {
                            grpc_lah_item.Cameramission.X = item.FixedPointLat;
                            grpc_lah_item.Cameramission.Y = item.FixedPointLon;
                        }
                        break;
                    case 1:
                        {
                            grpc_lah_item.Cameramission.X = item.SweepPoint1Lat;
                            grpc_lah_item.Cameramission.Y = item.SweepPoint1Lon;
                            grpc_lah_item.Cameramission.Z = item.SweepPoint2Lat;
                            grpc_lah_item.Cameramission.W = item.SweepPoint2Lon;
                            grpc_lah_item.Cameramission.Sweeptime = item.SweepTime;
                        }
                        break;
                    case 2:
                        {
                            grpc_lah_item.Cameramission.X = item.FixedUAVRoll;
                            grpc_lah_item.Cameramission.Y = item.FixedUAVPitch;
                            grpc_lah_item.Cameramission.Z = item.FixedUAVYaw;
                        }
                        break;
                    case 3:
                        {
                            grpc_lah_item.Cameramission.X = item.ChasingTargetNum;
                        }
                        break;
                    case -1:
                        {
                            
                        }
                        break;

                }





                switch (item.Mission)
                {
                    case 0:
                        {
                            grpc_lah_item.MissionType = enumMissionType.MissionMovetogps;
                            grpc_lah_item.Mission = new Vector4();
                            grpc_lah_item.Mission.X = item.LAT;
                            grpc_lah_item.Mission.Y = item.LON;
                            grpc_lah_item.Mission.Z = item.ALT;
                            grpc_lah_item.Mission.W = item.Speed;
                        }
                        break;
                    case 2:
                        {
                            grpc_lah_item.MissionType = enumMissionType.MissionHolding;
                            grpc_lah_item.Mission = new Vector4();
                            grpc_lah_item.Mission.X = item.LAT;
                            grpc_lah_item.Mission.Y = item.LON;
                            grpc_lah_item.Mission.Z = item.ALT;
                            grpc_lah_item.Mission.W = item.Speed;
                        }
                        break;
                    case 3:
                        {
                            grpc_lah_item.MissionType = enumMissionType.MissionAttacktoAtgm;
                            grpc_lah_item.Mission = new Vector4();
                            grpc_lah_item.Mission.X = item.TargetID;
                            grpc_lah_item.Mission.Y = item.ALT;
                            grpc_lah_item.Mission.Z = item.AttackCount;
                        }
                        break;
                    case 4:
                        {
                            grpc_lah_item.MissionType = enumMissionType.MissionAttacktoHydra;
                            grpc_lah_item.Mission = new Vector4();
                            grpc_lah_item.Mission.X = item.TargetID;
                            grpc_lah_item.Mission.Y = item.ALT;
                            grpc_lah_item.Mission.Z = item.AttackCount;
                        }
                        break;
                    case 5:
                        {
                            grpc_lah_item.MissionType = enumMissionType.MissionAttacktoMinigun;
                            grpc_lah_item.Mission = new Vector4();
                            grpc_lah_item.Mission.X = item.TargetID;
                            grpc_lah_item.Mission.Y = item.ALT;
                            grpc_lah_item.Mission.Z = item.AttackCount;
                        }
                        break;
                    case 6:
                        {
                            grpc_lah_item.MissionType = enumMissionType.MissionNone;
                            grpc_lah_item.Mission = new Vector4();
                            grpc_lah_item.Mission.X = item.LAT;
                            grpc_lah_item.Mission.Y = item.LON;
                            grpc_lah_item.Mission.Z = item.ALT;
                            grpc_lah_item.Mission.W = item.Speed;
                        }
                        break;
                    default:
                        break;
                }
                grpc_lah_item.Waypointpasstype = (int)item.PassType;

                switch (item.WaitTime)
                {
                    case 0:
                        {
                            grpc_lah_item.Onwaittime = -1;
                        }
                        break;
                    case 1:
                        {
                            grpc_lah_item.Onwaittime = 0;
                        }
                        break;
                    case 2:
                        {
                            grpc_lah_item.Onwaittime = 5;
                        }
                        break;
                    case 3:
                        {
                            grpc_lah_item.Onwaittime = 10;
                        }
                        break;
                    case 4:
                        {
                            grpc_lah_item.Onwaittime = 30;
                        }
                        break;
                    case 5:
                        {
                            grpc_lah_item.Onwaittime = 60;
                        }
                        break;
                    case 6:
                        {
                            grpc_lah_item.Onwaittime = 600;
                        }
                        break;
                    default:
                        break;
                }

                //grpc_lah_item.Cameramission
                //grpc_lah_item.MissionID = 0;
                TempWaypointID++;
                grpc_lah_item.WaypointID = TempWaypointID;
                grpc_lah.MissionList.Add(grpc_lah_item);
            }

            return grpc_lah;
        }

    }

    public class Unit_LAH_MovePlans : CommonBase
    {
        private ObservableCollection<Unit_LAH_MovePlan> _unit_LAH_MovePlans = new ObservableCollection<Unit_LAH_MovePlan>();
        public ObservableCollection<Unit_LAH_MovePlan> unit_LAH_MovePlans
        {
            get
            {
                return _unit_LAH_MovePlans;
            }
            set
            {
                _unit_LAH_MovePlans = value;
                OnPropertyChanged("unit_LAH_MovePlans");
            }
        }

        private uint _UnitID;
        /// <summary>
        /// 유인공격헬기 ID
        /// </summary>
        public uint UnitID
        {
            get
            {
                return _UnitID;
            }
            set
            {
                _UnitID = value;
                OnPropertyChanged("UnitID");
            }
        }
    }

    public class Unit_LAH_MovePlan : CommonBase
    {
       

        private double _LAT;
        /// <summary>
        /// 유인공격헬기 웨이포인트 위도
        /// </summary>
        public double LAT
        {
            get
            {
                return _LAT;
            }
            set
            {
                _LAT = value;
                OnPropertyChanged("LAT");
            }
        }

        private double _LON;
        /// <summary>
        /// 유인공격헬기 웨이포인트 경도
        /// </summary>
        public double LON
        {
            get
            {
                return _LON;
            }
            set
            {
                _LON = value;
                OnPropertyChanged("LON");
            }
        }

        private double _ALT;
        /// <summary>
        /// 유인공격헬기 웨이포인트 고도
        /// </summary>
        public double ALT
        {
            get
            {
                return _ALT;
            }
            set
            {
                _ALT = value;
                OnPropertyChanged("ALT");
            }
        }

        private double _Speed = 0;
        /// <summary>
        /// 유인공격헬기 웨이포인트 지정속도
        /// </summary>
        public double Speed
        {
            get
            {
                return _Speed;
            }
            set
            {
                _Speed = value;
                OnPropertyChanged("Speed");
            }
        }

        private int _Mission = 0;
        /// <summary>
        /// 유인공격헬기 임무
        /// 0 : 이동 / 1 : 로이터링(무인기) / 2 : 대기 / 3 : 미사일 공격 / 4 : 로켓 공격 / 5 : 미니건 공격 / 6 : None / 
        /// </summary>
        public int Mission
        {
            get
            {
                return _Mission;
            }
            set
            {
                _Mission = value;
                OnPropertyChanged("Mission");
            }
        }

        private int _PassType = 1;
        /// <summary>
        /// 경로점 통과비행 방식
        /// 0 : Fly Over / 1: Fly By
        /// </summary>
        public int PassType
        {
            get
            {
                return _PassType;
            }
            set
            {
                _PassType = value;
                OnPropertyChanged("PassType");
            }
        }

        private int _WaitTime = 0;
        /// <summary>
        /// Hold 명령(None도 추가?) 대기시간
        /// -1 : 무한대기 / 0 : 다음 임무 바로 수행 / 1이상 : 초단위
        /// </summary>
        public int WaitTime
        {
            get
            {
                return _WaitTime;
            }
            set
            {
                _WaitTime = value;
                OnPropertyChanged("WaitTime");
            }
        }

        private int _TargetID = 0;
        /// <summary>
        /// 공격 표적 ID
        /// </summary>
        public int TargetID
        {
            get
            {
                return _TargetID;
            }
            set
            {
                _TargetID = value;
                OnPropertyChanged("TargetID");
            }
        }

        private int _AttackCount = 0;
        /// <summary>
        /// 공격 횟수
        /// </summary>
        public int AttackCount
        {
            get
            {
                return _AttackCount;
            }
            set
            {
                _AttackCount = value;
                OnPropertyChanged("AttackCount");
            }
        }

        private int _CameraMode = 0;
        public int CameraMode
        {
            get
            {
                return _CameraMode;
            }
            set
            {
                _CameraMode = value;
                OnPropertyChanged("CameraMode");
            }
        }

        private double _CameraFOV = 0;
        public double CameraFOV
        {
            get
            {
                return _CameraFOV;
            }
            set
            {
                _CameraFOV = value;
                OnPropertyChanged("CameraFOV");
            }
        }

        private double _FixedPointLat = 0;
        public double FixedPointLat
        {
            get
            {
                return _FixedPointLat;
            }
            set
            {
                _FixedPointLat = value;
                OnPropertyChanged("FixedPointLat");
            }
        }

        private double _FixedPointLon = 0;
        public double FixedPointLon
        {
            get
            {
                return _FixedPointLon;
            }
            set
            {
                _FixedPointLon = value;
                OnPropertyChanged("FixedPointLon");
            }
        }

        private double _SweepPoint1Lat = 0;
        public double SweepPoint1Lat
        {
            get
            {
                return _SweepPoint1Lat;
            }
            set
            {
                _SweepPoint1Lat = value;
                OnPropertyChanged("SweepPoint1Lat");
            }
        }

        private double _SweepPoint1Lon = 0;
        public double SweepPoint1Lon
        {
            get
            {
                return _SweepPoint1Lon;
            }
            set
            {
                _SweepPoint1Lon = value;
                OnPropertyChanged("SweepPoint1Lon");
            }
        }

        private double _SweepPoint2Lat = 0;
        public double SweepPoint2Lat
        {
            get
            {
                return _SweepPoint2Lat;
            }
            set
            {
                _SweepPoint2Lat = value;
                OnPropertyChanged("SweepPoint2Lat");
            }
        }

        private double _SweepPoint2Lon = 0;
        public double SweepPoint2Lon
        {
            get
            {
                return _SweepPoint2Lon;
            }
            set
            {
                _SweepPoint2Lon = value;
                OnPropertyChanged("SweepPoint2Lon");
            }
        }

        private double _SweepTime = 0;
        public double SweepTime
        {
            get
            {
                return _SweepTime;
            }
            set
            {
                _SweepTime = value;
                OnPropertyChanged("SweepTime");
            }
        }

        private double _FixedUAVRoll = 0;
        public double FixedUAVRoll
        {
            get
            {
                return _FixedUAVRoll;
            }
            set
            {
                _FixedUAVRoll = value;
                OnPropertyChanged("FixedUAVRoll");
            }
        }

        private double _FixedUAVPitch = 0;
        public double FixedUAVPitch
        {
            get
            {
                return _FixedUAVPitch;
            }
            set
            {
                _FixedUAVPitch = value;
                OnPropertyChanged("FixedUAVPitch");
            }
        }

        private double _FixedUAVYaw = 0;
        public double FixedUAVYaw
        {
            get
            {
                return _FixedUAVYaw;
            }
            set
            {
                _FixedUAVYaw = value;
                OnPropertyChanged("FixedUAVYaw");
            }
        }

        private double _ChasingTargetNum = 0;
        public double ChasingTargetNum
        {
            get
            {
                return _ChasingTargetNum;
            }
            set
            {
                _ChasingTargetNum = value;
                OnPropertyChanged("ChasingTargetNum");
            }
        }
    }






}
