using System;
using System.Buffers.Binary;

namespace MLAH_Controller
{
    // =================================================================
    // 1. 공통 상수 정의 (ID, Type)
    // =================================================================

    public enum MsgID : int
    {
        SW_STATUS = 1,   // BitAgent -> Controller
        HW_STATUS = 2,   // BitAgent -> Controller
        SW_CONTROL = 3,  // Controller -> BitAgent
        HW_CONTROL = 4   // Controller -> BitAgent
    }

    public enum TargetType : int
    {
        BattleSim1 = 1,
        SituationAwareness = 11,
        BattleSim2 = 2,
        BattleSim3 = 3,
        MissionControl = 4,
        DisplaySim = 41,
        ICDModule = 42,
        UAVSim1 = 5,
        UAVSim2 = 6,
        UAVSim3 = 7,
        RTVTest1 = 8,
        RTVTest2 = 9
    }

    // =================================================================
    // 2. 메시지 클래스 (리틀 엔디안 적용)
    // =================================================================

    // [ID: 1] SW 상태 보고 (BitAgent -> Controller)
    public class BitAgent_SwStatus
    {
        public const int ID = 1;
        public int MessageID = ID;
        public int SwType;
        public int Status;  // 1: 켜짐, 0: 꺼짐

        public byte[] ToBytes()
        {
            byte[] bytes = new byte[12];
            // ★ Little Endian
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0), MessageID);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), SwType);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), Status);
            return bytes;
        }
    }

    // [ID: 2] HW 상태 보고 (BitAgent -> Controller)
    public class BitAgent_HwStatus
    {
        public const int ID = 2;
        public int MessageID = ID;
        public int HwType;
        public int Status;  // 1: Alive

        public byte[] ToBytes()
        {
            byte[] bytes = new byte[12];
            // ★ Little Endian
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0), MessageID);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), HwType);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), Status);
            return bytes;
        }
    }

    // [ID: 3] SW 제어 명령 (Controller -> BitAgent)
    public class BitAgent_SWControl
    {
        public int MessageID;
        public int ControlType;
        public int Command;     // 1: 실행, 0: 종료
        public BitAgent_SWControl() { }
        public BitAgent_SWControl(byte[] data)
        {
            if (data.Length < 12) return;
            // ★ Little Endian
            MessageID = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0));
            ControlType = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4));
            Command = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8));
        }
    }

    // [ID: 4] HW 제어 명령 (Controller -> BitAgent)
    public class BitAgent_HWControl
    {
        public int MessageID;
        public int ControlType;
        public int Command;

        public BitAgent_HWControl(byte[] data)
        {
            if (data.Length < 12) return;
            // ★ Little Endian
            MessageID = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0));
            ControlType = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4));
            Command = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8));
        }
    }
}