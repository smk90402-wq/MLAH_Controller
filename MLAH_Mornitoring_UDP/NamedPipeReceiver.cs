using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
// [확인 후 삭제] 미사용 using
//using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
//using MLAHInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Application = System.Windows.Application; // .proto 파일에서 생성된 네임스페이스




namespace MLAH_Mornitoring_UDP // UI 앱의 네임스페이스
{
    // gRPC 앱에서 보낸 데이터 패킷을 받을 클래스 (보내는 쪽과 동일해야 함)
    //public class PipeDataPacket
    //{
    //    public string MessageTypeName { get; set; }
    //    public ContextInfo Context { get; set; } // ContextInfo 클래스도 UI 프로젝트에 있어야 합니다.
    //    public byte[] ProtoData { get; set; }
    //}

    public static class MissionPlanParser
    {
        // 로깅을 위한 간단한 클래스 (실제 프로젝트에서는 NLog, Serilog 등 로거 사용)
        public static class Logger
        {
            public static void Error(string message)
            {
                Console.WriteLine($"[ERROR] {DateTime.Now}: {message}");
            }
        }

        public static class UAVMissionParser
        {
            /// <summary>
            /// BinaryReader에 특정 바이트만큼 읽을 데이터가 남았는지 확인합니다.
            /// </summary>
            /// <param name="br">바이너리 리더</param>
            /// <param name="bytesToRead">필요한 바이트 수</param>
            /// <returns>읽기 가능하면 true, 아니면 false</returns>
            private static bool CanRead(CommonUtil.BigEndianBinaryReader br, long bytesToRead)
            {
                return br.BaseStream.Length - br.BaseStream.Position >= bytesToRead;
            }

            /// <summary>
            /// FilmingProperty를 안전하게 파싱합니다. (제공된 원본 코드에 없어 새로 추가)
            /// </summary>
            private static FilmingProperty SafeParseFilmingProperty(CommonUtil.BigEndianBinaryReader br)
            {
                // FilmingProperty의 고정 크기 파트 검증
                // FieldOfView(4) + SensorType(4) + OperationMode(4) + CoordinateListN(4) + SearchSpeed(4) + TargetID(4) + SensorYaw(4) + SensorPitch(4)
                // + SensorYawAngularSpeed(12) = 44 bytes
                if (!CanRead(br, 44)) return null;

                var prop = new FilmingProperty
                {
                    FieldOfView = br.ReadSingle(),
                    SensorType = br.ReadUInt32(),
                    OperationMode = br.ReadInt32(),
                    CoordinateListN = br.ReadInt32()
                };

                // 가변 길이 CoordinateList 파싱
                prop.CoordinateList = new ObservableCollection<CoordinateList>();
                if (prop.CoordinateListN > 0)
                {
                    // CoordinateList 항목 하나의 크기는 12바이트 (float*2 + int*1)
                    if (!CanRead(br, (long)prop.CoordinateListN * 12)) return null;
                    for (int i = 0; i < prop.CoordinateListN; i++)
                    {
                        // 이 구조체는 고정 크기이므로 개별 CanRead는 생략
                        prop.CoordinateList.Add(new CoordinateList
                        {
                            Latitude = br.ReadSingle(),
                            Longitude = br.ReadSingle(),
                            Altitude = br.ReadInt32()
                        });
                    }
                }

                prop.SearchSpeed = br.ReadSingle();
                prop.TargetID = br.ReadUInt32();
                prop.SensorYaw = br.ReadSingle();
                prop.SensorPitch = br.ReadSingle();
                prop.SensorYawAngularSpeed = new SensorYawAngularSpeed
                {
                    LeftLimit = br.ReadSingle(),
                    RightLimit = br.ReadSingle(),
                    AngularRate = br.ReadSingle()
                };

                return prop;
            }

            /// <summary>
            /// IndividualMissionUAV를 안전하게 파싱합니다.
            /// </summary>
            private static IndividualMissionUAV SafeParseIndividualMissionUAV(CommonUtil.BigEndianBinaryReader br)
            {
                var im = new IndividualMissionUAV();

                // 고정 파트 최소 사이즈 검증 (ID(4) + IsDone(1) + AllocatedArea.Count(4) + FlightType(4)) = 13 bytes
                if (!CanRead(br, 13)) return null;

                im.IndividualMissionID = br.ReadUInt32();
                im.IsDone = br.ReadBoolean();

                // AllocatedArea 파싱
                im.AllocatedArea = new AllocatedArea { CoordinateListN = br.ReadUInt32() };
                im.AllocatedArea.CoordinateList = new ObservableCollection<CoordinateList>();
                if (im.AllocatedArea.CoordinateListN > 0)
                {
                    // [검증] CoordinateList에 필요한 총 바이트가 남아있는지 확인 (항목당 12바이트)
                    if (!CanRead(br, (long)im.AllocatedArea.CoordinateListN * 12)) return null;
                    for (int k = 0; k < im.AllocatedArea.CoordinateListN; k++)
                    {
                        im.AllocatedArea.CoordinateList.Add(new CoordinateList
                        {
                            Latitude = br.ReadSingle(),
                            Longitude = br.ReadSingle(),
                            Altitude = br.ReadInt32()
                        });
                    }
                }

                im.FlightType = br.ReadUInt32();

                // FlightType에 따른 가변 파트 파싱
                if (im.FlightType == 1) // 경로 비행
                {
                    if (!CanRead(br, 4)) return null; // WaypointListN(4)
                    im.WaypointListN = br.ReadUInt32();
                    im.WaypointList = new ObservableCollection<WaypointUAV>();
                    if (im.WaypointListN > 0)
                    {
                        // WaypointUAV의 고정 크기는 52바이트 (ID(4) + Coord(12) + Speed(4) + ETA(4) + ECF(4) + NextID(4) + PassType(4) + Loiter(16))
                        // FilmingProperty는 별도 파싱
                        for (int w = 0; w < im.WaypointListN; w++)
                        {
                            if (!CanRead(br, 52)) return null; // Waypoint 고정부 검증

                            var wp = new WaypointUAV { WaypointID = br.ReadUInt32() };
                            wp.Coordinate = new Coordinate { Latitude = br.ReadSingle(), Longitude = br.ReadSingle(), Altitude = br.ReadInt32() };
                            wp.Speed = br.ReadSingle();
                            wp.ETA = br.ReadUInt32();
                            wp.ECF = br.ReadSingle();
                            wp.NextWaypointID = br.ReadUInt32();
                            wp.WaypointPassType = br.ReadUInt32();
                            wp.LoiterProperty = new LoiterProperty { Radius = br.ReadInt32(), Direction = br.ReadUInt32(), Time = br.ReadInt32(), Speed = br.ReadSingle() };

                            wp.FilmingProperty = SafeParseFilmingProperty(br);
                            if (wp.FilmingProperty == null) return null; // FilmingProperty 파싱 실패

                            im.WaypointList.Add(wp);
                        }
                    }
                }
                else if (im.FlightType == 2) // 편대 비행
                {
                    // FormationInfo 크기는 16바이트 (LeaderID(4) + Formation(12))
                    if (!CanRead(br, 16)) return null;
                    var formationInfo = new FormationInfo { LeaderAircraftID = br.ReadUInt32() };
                    formationInfo.Formation = new Formation { dX = br.ReadInt32(), dY = br.ReadInt32(), dZ = br.ReadInt32() };
                    im.FormationInfo = formationInfo;
                }

                return im;
            }

            /// <summary>
            /// MissionSegmentUAV를 안전하게 파싱합니다.
            /// </summary>
            private static MissionSegmentUAV SafeParseMissionSegmentUAV(CommonUtil.BigEndianBinaryReader br)
            {
                // 고정 파트 최소 사이즈 검증 (ID(4) + IsDone(1) + Type(4) + IndvN(4)) = 13 bytes
                if (!CanRead(br, 13)) return null;

                var seg = new MissionSegmentUAV
                {
                    MissionSegmentID = br.ReadUInt32(),
                    IsDone = br.ReadBoolean(),
                    MissionSegmentType = br.ReadUInt32(),
                    IndividualMissionListN = br.ReadUInt32()
                };

                seg.IndividualMissionList = new ObservableCollection<IndividualMissionUAV>();
                if (seg.IndividualMissionListN > 0)
                {
                    for (int j = 0; j < seg.IndividualMissionListN; j++)
                    {
                        var im = SafeParseIndividualMissionUAV(br);
                        // [검증] 하위 구조체 파싱이 실패했는지 확인
                        if (im == null)
                        {
                            return null;
                        }
                        seg.IndividualMissionList.Add(im);
                    }
                }
                return seg;
            }

            /// <summary>
            /// byte 배열을 UAVMissionPlan 객체로 파싱합니다. 데이터 유효성 검사를 포함합니다.
            /// </summary>
            /// <param name="buffer">파싱할 byte 배열</param>
            /// <returns>성공 시 UAVMissionPlan 객체, 실패 시 null</returns>
            public static UAVMissionPlan ParseUAVMissionPlan(byte[] buffer)
            {
                // [검증] 버퍼가 null이거나 최소 헤더 크기보다 작은지 확인
                // MessageID(4) + PV(1) + TS(5) + PlanID(4) + AircraftID(4) + SegN(4) = 22 bytes
                long minHeaderSize = 22;
                if (buffer == null || buffer.Length < minHeaderSize)
                {
                    Logger.Error("유효하지 않은 메시지: 메시지 길이가 최소 헤더보다 작습니다.");
                    return null;
                }

                var plan = new UAVMissionPlan();

                try
                {
                    using (var ms = new MemoryStream(buffer))
                    using (var br = new CommonUtil.BigEndianBinaryReader(ms))
                    {
                        // ─── 1. 기본 헤더 파싱 ──────────────────────────────────
                        plan.MessageID = br.ReadUInt32();
                        plan.PresenceVector = br.ReadByte();
                        plan.Timestamp = br.ReadBytes(5);
                        plan.MissionPlanID = br.ReadUInt32();
                        plan.AircraftID = br.ReadUInt32();
                        plan.MissionSegemntN = br.ReadUInt32();

                        // ─── 2. MissionSegmentUAV 리스트 파싱 ────────────────────
                        plan.MissionSegemntList = new ObservableCollection<MissionSegmentUAV>();
                        for (int i = 0; i < plan.MissionSegemntN; i++)
                        {
                            var seg = SafeParseMissionSegmentUAV(br);
                            // [검증] 세그먼트 파싱이 실패했는지 확인
                            if (seg == null)
                            {
                                Logger.Error("유효하지 않은 메시지: MissionSegment 파싱 중 데이터가 부족합니다.");
                                return null;
                            }
                            plan.MissionSegemntList.Add(seg);
                        }

                        // [검증] 모든 파싱이 끝난 후, 스트림에 남은 데이터가 있는지 확인
                        if (br.BaseStream.Position != br.BaseStream.Length)
                        {
                            Logger.Error("유효하지 않은 메시지: 파싱 후에도 데이터가 남아있습니다.");
                            return null;
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    Logger.Error("유효하지 않은 메시지: 예상치 못하게 메시지 끝에 도달했습니다.");
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.Error($"파싱 중 알 수 없는 오류 발생: {ex.Message}");
                    return null;
                }

                return plan;
            }
        }

        /// <summary>
        /// 빅 엔디안으로 직렬화된 바이트 배열을 LAHMissionPlan 객체로 역직렬화(파싱)합니다.
        /// </summary>
        //public static LAHMissionPlan ParseLAHMissionPlan(byte[] buffer)
        //{
        //    var plan = new LAHMissionPlan();

        //    using (var ms = new MemoryStream(buffer))
        //    using (var br = new BigEndianBinaryReader(ms))
        //    {
        //        // ─── 1. 기본 헤더 파싱 ──────────────────────────────────
        //        plan.MessageID = br.ReadUInt32();
        //        plan.PresenceVector = br.ReadByte();
        //        plan.Timestamp = br.ReadBytes(5);
        //        plan.MissionPlanID = br.ReadUInt32();
        //        plan.AircraftID = br.ReadUInt32();

        //        // ─── 2. MissionSegmentLAH 리스트 파싱 ────────────────────
        //        plan.MissionSegemntN = br.ReadUInt32();
        //        plan.MissionSegemntList = new ObservableCollection<MissionSegmentLAH>();

        //        for (int i = 0; i < plan.MissionSegemntN; i++)
        //        {
        //            var seg = new MissionSegmentLAH
        //            {
        //                MissionSegmentID = br.ReadUInt32(),
        //                IsDone = br.ReadBoolean(),
        //                MissionSegmentType = br.ReadUInt32()
        //            };

        //            seg.IndividualMissionListN = br.ReadUInt32();
        //            seg.IndividualMissionList = new ObservableCollection<IndividualMissionLAH>();

        //            for (int j = 0; j < seg.IndividualMissionListN; j++)
        //            {
        //                var im = new IndividualMissionLAH
        //                {
        //                    IndividualMissionID = br.ReadUInt32(),
        //                    IsDone = br.ReadBoolean()
        //                };

        //                im.WaypointListN = br.ReadUInt32();
        //                im.WaypointList = new ObservableCollection<WaypointLAH>();

        //                for (int w = 0; w < im.WaypointListN; w++)
        //                {
        //                    var wp = new WaypointLAH { WaypointID = br.ReadUInt32() };
        //                    wp.Coordinate = new Coordinate { Latitude = br.ReadSingle(), Longitude = br.ReadSingle(), Altitude = br.ReadInt32() };
        //                    wp.Speed = br.ReadSingle();
        //                    wp.ETA = br.ReadInt32();
        //                    wp.NextWaypointID = br.ReadUInt32();
        //                    wp.Hovering = br.ReadUInt32();
        //                    wp.Attack = new Attack { TargetID = br.ReadUInt32(), WeaponType = br.ReadUInt32() };
        //                    im.WaypointList.Add(wp);
        //                }
        //                seg.IndividualMissionList.Add(im);
        //            }
        //            plan.MissionSegemntList.Add(seg);
        //        }
        //    }
        //    return plan;
        //}

        public static class LAHMissionParser
        {
            /// <summary>
            /// BinaryReader에 특정 바이트만큼 읽을 데이터가 남았는지 확인합니다.
            /// </summary>
            private static bool CanRead(CommonUtil.BigEndianBinaryReader br, long bytesToRead)
            {
                return br.BaseStream.Length - br.BaseStream.Position >= bytesToRead;
            }

            /// <summary>
            /// WaypointLAH를 안전하게 파싱합니다.
            /// </summary>
            private static WaypointLAH SafeParseWaypointLAH(CommonUtil.BigEndianBinaryReader br)
            {
                // WaypointLAH의 고정 크기: 40 바이트
                // WaypointID(4) + Coordinate(12) + Speed(4) + ETA(4) + NextWaypointID(4) + Hovering(4) + Attack(8)
                if (!CanRead(br, 40)) return null;

                var wp = new WaypointLAH { WaypointID = br.ReadUInt32() };
                wp.Coordinate = new Coordinate { Latitude = br.ReadSingle(), Longitude = br.ReadSingle(), Altitude = br.ReadInt32() };
                wp.Speed = br.ReadSingle();
                wp.ETA = br.ReadInt32();
                wp.NextWaypointID = br.ReadUInt32();
                wp.Hovering = br.ReadUInt32();
                wp.Attack = new Attack { TargetID = br.ReadUInt32(), WeaponType = br.ReadUInt32() };

                return wp;
            }

            /// <summary>
            /// IndividualMissionLAH를 안전하게 파싱합니다.
            /// </summary>
            private static IndividualMissionLAH SafeParseIndividualMissionLAH(CommonUtil.BigEndianBinaryReader br)
            {
                // 고정 파트 최소 사이즈 검증: 9 바이트 (ID(4) + IsDone(1) + WaypointListN(4))
                if (!CanRead(br, 9)) return null;

                var im = new IndividualMissionLAH
                {
                    IndividualMissionID = br.ReadUInt32(),
                    IsDone = br.ReadBoolean(),
                    WaypointListN = br.ReadUInt32()
                };

                im.WaypointList = new ObservableCollection<WaypointLAH>();
                if (im.WaypointListN > 0)
                {
                    for (int w = 0; w < im.WaypointListN; w++)
                    {
                        // [검증] 하위 Waypoint 구조체 파싱 시도 및 실패 확인
                        var wp = SafeParseWaypointLAH(br);
                        if (wp == null)
                        {
                            // Waypoint 파싱 실패 시 상위로 전파
                            return null;
                        }
                        im.WaypointList.Add(wp);
                    }
                }

                return im;
            }

            /// <summary>
            /// MissionSegmentLAH를 안전하게 파싱합니다.
            /// </summary>
            private static MissionSegmentLAH SafeParseMissionSegmentLAH(CommonUtil.BigEndianBinaryReader br)
            {
                // 고정 파트 최소 사이즈 검증: 13 바이트 (ID(4) + IsDone(1) + Type(4) + IndvN(4))
                if (!CanRead(br, 13)) return null;

                var seg = new MissionSegmentLAH
                {
                    MissionSegmentID = br.ReadUInt32(),
                    IsDone = br.ReadBoolean(),
                    MissionSegmentType = br.ReadUInt32(),
                    IndividualMissionListN = br.ReadUInt32()
                };

                seg.IndividualMissionList = new ObservableCollection<IndividualMissionLAH>();
                if (seg.IndividualMissionListN > 0)
                {
                    for (int j = 0; j < seg.IndividualMissionListN; j++)
                    {
                        // [검증] 하위 IndividualMission 구조체 파싱 시도 및 실패 확인
                        var im = SafeParseIndividualMissionLAH(br);
                        if (im == null)
                        {
                            return null;
                        }
                        seg.IndividualMissionList.Add(im);
                    }
                }

                return seg;
            }

            /// <summary>
            /// byte 배열을 LAHMissionPlan 객체로 파싱합니다. 데이터 유효성 검사를 포함합니다.
            /// </summary>
            /// <param name="buffer">파싱할 byte 배열</param>
            /// <returns>성공 시 LAHMissionPlan 객체, 실패 시 null</returns>
            public static LAHMissionPlan ParseLAHMissionPlan(byte[] buffer)
            {
                // [검증] 버퍼가 null이거나 최소 헤더 크기보다 작은지 확인
                // MessageID(4) + PV(1) + TS(5) + PlanID(4) + AircraftID(4) + SegN(4) = 22 bytes
                long minHeaderSize = 22;
                if (buffer == null || buffer.Length < minHeaderSize)
                {
                    Logger.Error("유효하지 않은 LAH 메시지: 메시지 길이가 최소 헤더보다 작습니다.");
                    return null;
                }

                var plan = new LAHMissionPlan();

                try
                {
                    using (var ms = new MemoryStream(buffer))
                    using (var br = new CommonUtil.BigEndianBinaryReader(ms))
                    {
                        // ─── 1. 기본 헤더 파싱 ──────────────────────────────────
                        plan.MessageID = br.ReadUInt32();
                        plan.PresenceVector = br.ReadByte();
                        plan.Timestamp = br.ReadBytes(5);
                        plan.MissionPlanID = br.ReadUInt32();
                        plan.AircraftID = br.ReadUInt32();
                        plan.MissionSegemntN = br.ReadUInt32();

                        // ─── 2. MissionSegmentLAH 리스트 파싱 ────────────────────
                        plan.MissionSegemntList = new ObservableCollection<MissionSegmentLAH>();
                        for (int i = 0; i < plan.MissionSegemntN; i++)
                        {
                            var seg = SafeParseMissionSegmentLAH(br);
                            // [검증] 세그먼트 파싱이 실패했는지 확인
                            if (seg == null)
                            {
                                Logger.Error("유효하지 않은 LAH 메시지: MissionSegment 파싱 중 데이터가 부족합니다.");
                                return null;
                            }
                            plan.MissionSegemntList.Add(seg);
                        }

                        // [검증] 모든 파싱이 끝난 후, 스트림에 남은 데이터가 있는지 확인
                        if (br.BaseStream.Position != br.BaseStream.Length)
                        {
                            Logger.Error("유효하지 않은 LAH 메시지: 파싱 후에도 데이터가 남아있습니다.");
                            return null;
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    Logger.Error("유효하지 않은 LAH 메시지: 예상치 못하게 메시지 끝에 도달했습니다.");
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.Error($"LAH 메시지 파싱 중 알 수 없는 오류 발생: {ex.Message}");
                    return null;
                }

                return plan;
            }
        }

    }

    public class PipeDataPacketUdpRaw
    {
        public string SenderIp { get; set; }
        public int SenderPort { get; set; }
        public byte[] UdpData { get; set; } // UDP로 받은 원본 byte 배열
    }

    public class NamedPipeReceiver
    {
        private const string PipeName = "MLAHMonitoringPipeUDP"; // 파이프 이름 (gRPC 앱과 동일해야 함)

        // 백그라운드 스레드에서 파이프 서버를 계속 실행
        public void Start()
        {
            Task.Run(() => ListenLoop());
        }

        private async Task ListenLoop()
        {
            // 이 루프는 서버 스트림 자체에 심각한 오류가 발생했을 때
            // 서버를 재생성하기 위해 존재합니다.
            while (true)
            {
                // 1. 클라이언트와의 한 번의 완전한 세션을 위해 파이프 서버를 생성합니다.
                using (var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    try
                    {
                        // 2. 클라이언트의 연결을 기다립니다.
                        await pipeServer.WaitForConnectionAsync();
                        //System.Diagnostics.Debug.WriteLine("Pipe Client Connected. Start listening for messages...");

                        // 3. [핵심 수정] 연결이 유지되는 동안 계속해서 메시지를 읽습니다.
                        while (pipeServer.IsConnected)
                        {
                            // ProcessPipeData는 한 메시지를 읽고 처리합니다.
                            // 클라이언트 연결이 끊기면 이 메서드에서 IOException이 발생합니다.
                            await ProcessPipeData(pipeServer);
                        }
                    }
                    catch (IOException)
                    {
                        // 클라이언트가 정상적으로 또는 비정상적으로 연결을 끊었을 때 발생하는 예상된 예외입니다.
                        // using 블록이 끝나면서 pipeServer 리소스가 정리되고, 외부 while 루프가 새 연결을 위해 새 서버를 생성합니다.
                        System.Diagnostics.Debug.WriteLine("Pipe Client Disconnected. Waiting for new connection...");
                    }
                    catch (Exception ex)
                    {
                        // 예상치 못한 다른 오류 처리
                        //System.Diagnostics.Debug.WriteLine($"Pipe Listen Error: {ex.Message}");
                        await Task.Delay(1000); // 오류가 너무 빠르게 반복되는 것을 방지
                    }
                } // 여기서 pipeServer.Dispose()가 호출되어 세션이 완전히 정리됩니다.
            }
        }


        private async Task ProcessPipeData(NamedPipeServerStream pipeServer)
        {
            // 1. 데이터 길이(4바이트)를 먼저 읽음
            byte[] lengthBuffer = new byte[4];

            // [수정] ReadAsync의 반환값(읽은 바이트 수)을 확인하여 정상적인 연결 종료를 감지합니다.
            int bytesRead = await pipeServer.ReadAsync(lengthBuffer, 0, 4);
            if (bytesRead < 4)
            {
                // 0 바이트를 읽었거나 4바이트보다 적게 읽었다면 클라이언트가 연결을 닫은 것입니다.
                throw new IOException("Client disconnected while reading data length.");
            }

            int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (dataLength <= 0)
            {
                throw new InvalidDataException("Received invalid data length.");
            }

            // 2. 실제 데이터 읽기
            byte[] dataBuffer = new byte[dataLength];
            int totalBytesRead = 0;
            while (totalBytesRead < dataLength)
            {
                bytesRead = await pipeServer.ReadAsync(dataBuffer, totalBytesRead, dataLength - totalBytesRead);
                if (bytesRead == 0)
                {
                    // 데이터를 읽는 도중 연결이 끊겼습니다.
                    throw new IOException("Client disconnected while reading data content.");
                }
                totalBytesRead += bytesRead;
            }

            string jsonString = Encoding.UTF8.GetString(dataBuffer);

            // 1. 단순화된 Raw Packet으로 역직렬화
            var rawPacket = JsonConvert.DeserializeObject<PipeDataPacketUdpRaw>(jsonString);
            if (rawPacket?.UdpData == null) return;

            // 2. 여기서 UDP 데이터를 파싱하여 ContextInfo 객체 생성
            ContextInfo context = UdpMessageParser.ParseUdpPacket(rawPacket.UdpData, rawPacket.SenderIp, rawPacket.SenderPort);

            if (context != null)
            {
                // 3. 완성된 ContextInfo를 Model의 큐로 전달
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    Model_Mornitoring_PopUp.SingletonInstance.EnqueueMessage(context);
                });
            }
        }

    }
}