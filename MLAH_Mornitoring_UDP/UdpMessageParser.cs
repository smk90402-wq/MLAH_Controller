using System;
using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MLAH_Mornitoring_UDP
{
    /// <summary>
    /// UDP 바이트 배열을 각 메시지 타입별로 파싱하는 통합 클래스.
    /// Controller(UDPModule)에서 NamedPipe로 전달되는 모든 메시지를 처리합니다.
    /// </summary>
    public static class UdpMessageParser
    {
        /// <summary>
        /// UDP 바이트 배열을 파싱하여 ContextInfo 객체를 생성합니다.
        /// </summary>
        public static ContextInfo ParseUdpPacket(byte[] buffer, string ip, int port)
        {
            var context = new ContextInfo
            {
                IP = ip,
                Port = port.ToString(),
                Protocol = "UDP",
                ReceivedTime = DateTime.Now.ToString("HH:mm:ss")
            };
            object parsedObject = null;
            uint messageId = 0;
            string messageName = "Unknown";

            // --- 1. JSON 메시지 처리 ---
            if (buffer.Length > 0 && buffer[0] == '{')
            {
                string jsonString = Encoding.UTF8.GetString(buffer);
                try
                {
                    JObject jObj = JObject.Parse(jsonString);
                    messageId = jObj["MessageID"]?.Value<uint>() ?? 0;

                    switch (messageId)
                    {
                        case 53111:
                            parsedObject = jObj.ToObject<LAHMissionPlan>();
                            messageName = "LAHMissionPlan";
                            break;
                        case 53112:
                            parsedObject = jObj.ToObject<UAVMissionPlan>();
                            messageName = "UAVMissionPlan";
                            break;
                        case 53113:
                            parsedObject = jObj.ToObject<MissionPlanOptionInfo>();
                            messageName = "MissionPlanOptionInfo";
                            break;
                        case 51310:
                            parsedObject = jObj;
                            messageName = "InputMissionPackageJson";
                            break;
                        default:
                            messageName = $"JsonMessage (ID: {messageId})";
                            parsedObject = jObj;
                            break;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
            // --- 2. 바이너리 메시지 처리 ---
            else if (buffer.Length >= 4)
            {
                // 신규 헤더 포맷 확인 (16바이트 헤더, ID는 오프셋 12에 2바이트)
                ushort messageIdNew = 0;
                if (buffer.Length >= 14)
                {
                    messageIdNew = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(12, 2));
                }

                // 신규 포맷 메시지 우선 처리
                if (messageIdNew == 53115)
                {
                    parsedObject = ParseMissionResult(buffer);
                    messageName = "MissionResultData";
                    messageId = 53115;
                }
                else
                {
                    // 기존 포맷 (맨 앞 4바이트가 ID)
                    messageId = CommonUtil.ReadUInt32BigEndian(buffer, 0);

                    switch (messageId)
                    {
                        case 1:
                            parsedObject = ParseSWStatus(buffer);
                            messageName = "SWStatus";
                            break;
                        case 4:
                            parsedObject = ParseSensorControlCommand(buffer);
                            messageName = "SensorControlCommand";
                            break;
                        case 6:
                            parsedObject = ParseLahStates(buffer);
                            messageName = "Lah_States";
                            break;
                        case 7:
                            parsedObject = ParseUavMalFunctionState(buffer);
                            messageName = "UAVMalFunctionState";
                            break;
                        case 8:
                            parsedObject = ParseUAVMalFunctionCommand(buffer);
                            messageName = "UAVMalFunctionCommand";
                            break;
                        case 9:
                            parsedObject = ParseOperatingCommand(buffer);
                            messageName = "OperatingCommand";
                            break;
                        case 14:
                            parsedObject = ParseLAHMalFunctionState(buffer);
                            messageName = "LAHMalFunctionState";
                            break;
                        case 51331:
                            parsedObject = ParsePilotDecision(buffer);
                            messageName = "PilotDecision";
                            break;
                        case 53113:
                            parsedObject = ParseMissionPlanOptionInfo(buffer);
                            messageName = "MissionPlanOptionInfo";
                            break;
                        case 53114:
                            parsedObject = ParseMissionUpdateWithoutDecision(buffer);
                            messageName = "MissionUpdateWithoutDecision";
                            break;
                        default:
                            messageName = $"BinaryMessage (ID: {messageId})";
                            parsedObject = buffer;
                            break;
                    }
                }
            }

            if (parsedObject == null) return null;

            context.MessageName = messageName;
            context.ID = (int)messageId;
            context.OriginalObject = parsedObject;

            return context;
        }

        // ──────────────────────────────────────────────
        //  바이너리 메시지 파싱 함수들
        // ──────────────────────────────────────────────

        public static SWStatus ParseSWStatus(byte[] buffer)
        {
            if (buffer.Length < 12) return null;
            var status = new SWStatus();
            status.MessageID = CommonUtil.ReadUInt32BigEndian(buffer, 0);
            status.DeviceID = CommonUtil.ReadUInt32BigEndian(buffer, 4);
            status.State = CommonUtil.ReadUInt32BigEndian(buffer, 8);
            return status;
        }

        public static SensorControlCommand ParseSensorControlCommand(byte[] buffer)
        {
            if (buffer.Length < 84) return null;
            var command = new SensorControlCommand();
            int offset = 0;
            command.MessageID = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            command.UavID = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            command.SensorType = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            command.HorizontalFov = CommonUtil.ReadSingleBigEndian(buffer, offset); offset += 4;
            command.VerticalFov = CommonUtil.ReadSingleBigEndian(buffer, offset); offset += 4;
            command.GimbalRoll = CommonUtil.ReadSingleBigEndian(buffer, offset); offset += 4;
            command.GimbalPitch = CommonUtil.ReadSingleBigEndian(buffer, offset); offset += 4;
            command.SensorLat = CommonUtil.ReadDoubleBigEndian(buffer, offset); offset += 8;
            command.SensorLon = CommonUtil.ReadDoubleBigEndian(buffer, offset); offset += 8;
            command.SensorAlt = CommonUtil.ReadDoubleBigEndian(buffer, offset); offset += 8;
            command.Roll = CommonUtil.ReadDoubleBigEndian(buffer, offset); offset += 8;
            command.Pitch = CommonUtil.ReadDoubleBigEndian(buffer, offset); offset += 8;
            command.Heading = CommonUtil.ReadDoubleBigEndian(buffer, offset); offset += 8;
            command.Speed = CommonUtil.ReadDoubleBigEndian(buffer, offset); offset += 8;
            command.Fuel = CommonUtil.ReadSingleBigEndian(buffer, offset);
            return command;
        }

        public static Lah_States ParseLahStates(byte[] buffer)
        {
            if (buffer.Length < 14) return null;

            var lahStates = new Lah_States();
            int offset = 0;

            lahStates.MessageID = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            lahStates.PresenceVector = buffer[offset++];
            Array.Copy(buffer, offset, lahStates.TimeStamp, 0, 5);
            offset += 5;
            lahStates.StatesN = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;

            lahStates.States = new States[lahStates.StatesN];

            for (int i = 0; i < lahStates.StatesN; i++)
            {
                if (buffer.Length < offset + 45) break;

                var stateItem = new States();
                stateItem.AircraftID = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;

                stateItem.CoordinateList = new CoordinateList();
                stateItem.CoordinateList.Latitude = CommonUtil.ReadSingleBigEndian(buffer, offset); offset += 4;
                stateItem.CoordinateList.Longitude = CommonUtil.ReadSingleBigEndian(buffer, offset); offset += 4;
                stateItem.CoordinateList.Altitude = CommonUtil.ReadInt32BigEndian(buffer, offset); offset += 4;

                stateItem.Velocity = new Velocity();
                stateItem.Velocity.Speed = CommonUtil.ReadSingleBigEndian(buffer, offset); offset += 4;
                stateItem.Velocity.Heading = CommonUtil.ReadSingleBigEndian(buffer, offset); offset += 4;

                stateItem.Fuel = CommonUtil.ReadSingleBigEndian(buffer, offset); offset += 4;

                stateItem.Weapons = new Weapons();
                stateItem.Weapons.Type1 = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
                stateItem.Weapons.Type2 = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
                stateItem.Weapons.Type3 = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;

                if (stateItem.LastSignalTime == null) stateItem.LastSignalTime = new byte[5];
                Array.Copy(buffer, offset, stateItem.LastSignalTime, 0, 5);
                offset += 5;

                lahStates.States[i] = stateItem;
            }

            return lahStates;
        }

        public static UAVMalFunctionState ParseUavMalFunctionState(byte[] buffer)
        {
            if (buffer.Length < 20) return null;
            var state = new UAVMalFunctionState();
            int offset = 0;
            state.MessageID = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            state.UavID = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            state.Health = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            state.PayloadHealth = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            state.FuelWarning = CommonUtil.ReadUInt32BigEndian(buffer, offset);
            return state;
        }

        public static UAVMalFunctionCommand ParseUAVMalFunctionCommand(byte[] buffer)
        {
            if (buffer.Length < 20) return null;
            var command = new UAVMalFunctionCommand();
            int offset = 0;
            command.MessageID = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            command.UavID = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            command.Health = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            command.PayloadHealth = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            command.FuelWarning = CommonUtil.ReadUInt32BigEndian(buffer, offset);
            return command;
        }

        public static OperatingCommand ParseOperatingCommand(byte[] buffer)
        {
            if (buffer.Length < 8) return null;
            var command = new OperatingCommand();
            command.MessageID = CommonUtil.ReadUInt32BigEndian(buffer, 0);
            command.Command = CommonUtil.ReadUInt32BigEndian(buffer, 4);
            return command;
        }

        public static LAHMalFunctionState ParseLAHMalFunctionState(byte[] buffer)
        {
            if (buffer.Length < 8) return null;

            var state = new LAHMalFunctionState();
            int offset = 0;

            state.MessageID = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
            state.LAHN = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;

            state.LAH = new LAH[state.LAHN];

            for (int i = 0; i < state.LAHN; i++)
            {
                if (buffer.Length < offset + 11) break;

                var lahItem = new LAH();
                lahItem.AircraftID = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;
                lahItem.Health = CommonUtil.ReadUInt32BigEndian(buffer, offset); offset += 4;

                lahItem.DatalinkStatus = new DatalinkStatus();
                lahItem.DatalinkStatus.IsConnectedToUAV1 = buffer[offset++] != 0;
                lahItem.DatalinkStatus.IsConnectedToUAV2 = buffer[offset++] != 0;
                lahItem.DatalinkStatus.IsConnectedToUAV3 = buffer[offset++] != 0;

                state.LAH[i] = lahItem;
            }

            return state;
        }

        // ──────────────────────────────────────────────
        //  Controller에서 추가된 메시지 파싱 함수들
        // ──────────────────────────────────────────────

        public static PilotDecision ParsePilotDecision(byte[] buffer)
        {
            // MessageID(4) + PresenceVector(1) + Timestamp(5) + Ignore(1) + EditOptionsIDConverter(4) = 15 bytes
            if (buffer.Length < 15) return null;

            var decision = new PilotDecision();
            int offset = 0;

            decision.MessageID = CommonUtil.ReadUInt32BigEndian(buffer, offset);
            offset += 4;

            offset += 1; // PresenceVector
            offset += 5; // Timestamp

            decision.Ignore = buffer[offset] != 0;
            offset += 1;

            decision.EditOptionsIDConverter = CommonUtil.ReadUInt32BigEndian(buffer, offset);

            return decision;
        }

        public static MissionPlanOptionInfo ParseMissionPlanOptionInfo(byte[] buffer)
        {
            // MessageID(4) + PV(1) + TS(5) + AutoExec(1) + OptionListN(4) = 15 bytes
            if (buffer.Length < 15) return null;

            var optionInfo = new MissionPlanOptionInfo();
            ReadOnlySpan<byte> span = buffer;
            int offset = 0;

            optionInfo.MessageID = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            offset += 1; // PresenceVector
            offset += 5; // Timestamp

            optionInfo.AutoExecution = span[offset] != 0;
            offset += 1;

            optionInfo.OptionListN = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            for (int i = 0; i < optionInfo.OptionListN; i++)
            {
                if (buffer.Length < offset + 33) return optionInfo; // 최소 OptionList 고정부

                var option = new OptionList();

                option.OptionID = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.Recommend = span[offset] != 0;
                offset += 1;

                option.OptionName = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.SurvivalRate = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.TimeContraction = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.RecogEffectiveness = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.FuelWarning = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.Distance = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.Target = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;

                // UAV Mission Plan ID 리스트
                if (buffer.Length < offset + 4) return optionInfo;
                option.UAVMissionPlanIDListN = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;
                for (int j = 0; j < option.UAVMissionPlanIDListN; j++)
                {
                    if (buffer.Length < offset + 4) return optionInfo;
                    option.UAVMissionPlanIDList.Add(BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset)));
                    offset += 4;
                }

                // LAH Mission Plan ID 리스트
                if (buffer.Length < offset + 4) return optionInfo;
                option.LAHMissionPlanIDListN = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;
                for (int k = 0; k < option.LAHMissionPlanIDListN; k++)
                {
                    if (buffer.Length < offset + 4) return optionInfo;
                    option.LAHMissionPlanIDList.Add(BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset)));
                    offset += 4;
                }

                optionInfo.OptionList.Add(option);
            }

            return optionInfo;
        }

        public static MissionUpdatewithoutPilotDecision ParseMissionUpdateWithoutDecision(byte[] buffer)
        {
            // MessageID(4) + PV(1) + TS(5) + UAVListN(4) = 14 bytes
            if (buffer.Length < 14) return null;

            var update = new MissionUpdatewithoutPilotDecision();
            ReadOnlySpan<byte> span = buffer;
            int offset = 0;

            update.MessageID = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            offset += 1; // PresenceVector
            offset += 5; // Timestamp

            update.UAVMissionPlanIDListN = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;
            for (int j = 0; j < update.UAVMissionPlanIDListN; j++)
            {
                if (buffer.Length < offset + 4) return update;
                update.UAVMissionPlanIDList.Add(BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset)));
                offset += 4;
            }

            if (buffer.Length < offset + 4) return update;
            update.LAHMissionPlanIDListN = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;
            for (int k = 0; k < update.LAHMissionPlanIDListN; k++)
            {
                if (buffer.Length < offset + 4) return update;
                update.LAHMissionPlanIDList.Add(BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset)));
                offset += 4;
            }

            return update;
        }

        public static MissionResultData ParseMissionResult(byte[] buffer)
        {
            // Header(16) + PresenceVector(1) + Timestamp(5) + SystemRecommend(4) = 26 bytes 최소
            if (buffer.Length < 26) return null;

            var data = new MissionResultData();
            int offset = 0;

            // 16바이트 헤더
            data.SplitInfo = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
            offset += 2;

            data.DataLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
            offset += 2;

            data.SourceID = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset, 4));
            offset += 4;

            data.DestID = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset, 4));
            offset += 4;

            data.MessageID = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
            offset += 2;

            data.Properties = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
            offset += 2;

            // 데이터 필드
            data.PresenceVector = buffer[offset];
            offset += 1;

            // Timestamp (5바이트 -> long)
            long timestamp = 0;
            timestamp |= (long)buffer[offset + 0] << 32;
            timestamp |= (long)buffer[offset + 1] << 24;
            timestamp |= (long)buffer[offset + 2] << 16;
            timestamp |= (long)buffer[offset + 3] << 8;
            timestamp |= (long)buffer[offset + 4];
            data.Timestamp = timestamp;
            offset += 5;

            data.SystemRecommend = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset, 4));

            return data;
        }
    }
}
