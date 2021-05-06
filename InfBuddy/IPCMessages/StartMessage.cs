using System;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace InfBuddy.IPCMessages
{
    [AoContract((int)IPCOpcode.Start)]
    public class StartMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Start;

        [AoMember(0)]
        public MissionDifficulty MissionDifficulty { get; set; }
        [AoMember(1)]
        public MissionFaction MissionFaction { get; set; }
    }
}
