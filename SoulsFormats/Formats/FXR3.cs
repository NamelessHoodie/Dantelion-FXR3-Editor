using System.Collections.Generic;
using System.Xml.Serialization;

namespace SoulsFormats
{
    /// <summary>
    /// An SFX definition file used in DS3 and Sekiro. Extension: .fxr
    /// </summary>
    public class FXR3 : SoulsFile<FXR3>
    {
        public FXR3.FXRVersion Version { get; set; }

        public int ID { get; set; }

        public FXR3.FFXStateMachine RootStateMachine { get; set; }

        public FXR3.FFXEffectCallA RootEffectCall { get; set; }

        public List<int> Section12s { get; set; }

        public List<int> Section13s { get; set; }

        public FXR3()
        {
            this.Version = FXR3.FXRVersion.DarkSouls3;
            this.RootStateMachine = new FXR3.FFXStateMachine();
            this.RootEffectCall = new FXR3.FFXEffectCallA();
            this.Section12s = new List<int>();
            this.Section13s = new List<int>();
        }

        protected override bool Is(BinaryReaderEx br)
        {
            if (br.Length < 8L)
                return false;
            string ascii = br.GetASCII(0L, 4);
            short int16 = br.GetInt16(6L);
            return ascii == "FXR\0" && (int16 == (short)4 || int16 == (short)5);
        }

        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = false;
            br.AssertASCII("FXR\0");
            int num1 = (int)br.AssertInt16(new short[1]);
            this.Version = br.ReadEnum16<FXR3.FXRVersion>();
            br.AssertInt32(1);
            this.ID = br.ReadInt32();
            int num2 = br.ReadInt32();
            br.AssertInt32(1);
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            int num3 = br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.AssertInt32(1);
            br.AssertInt32(new int[1]);
            if (this.Version == FXR3.FXRVersion.Sekiro)
            {
                int num4 = br.ReadInt32();
                int count1 = br.ReadInt32();
                int num5 = br.ReadInt32();
                int count2 = br.ReadInt32();
                br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                this.Section12s = new List<int>((IEnumerable<int>)br.GetInt32s((long)num4, count1));
                this.Section13s = new List<int>((IEnumerable<int>)br.GetInt32s((long)num5, count2));
            }
            else
            {
                this.Section12s = new List<int>();
                this.Section13s = new List<int>();
            }
            br.Position = (long)num2;
            this.RootStateMachine = new FXR3.FFXStateMachine(br);
            br.Position = (long)num3;
            this.RootEffectCall = new FXR3.FFXEffectCallA(br);
        }

        protected override void Write(BinaryWriterEx bw)
        {
            bw.WriteASCII("FXR\0");
            bw.WriteInt16((short)0);
            bw.WriteUInt16((ushort)this.Version);
            bw.WriteInt32(1);
            bw.WriteInt32(this.ID);
            bw.ReserveInt32("Section1Offset");
            bw.WriteInt32(1);
            bw.ReserveInt32("Section2Offset");
            bw.WriteInt32(this.RootStateMachine.States.Count);
            bw.ReserveInt32("Section3Offset");
            bw.ReserveInt32("Section3Count");
            bw.ReserveInt32("Section4Offset");
            bw.ReserveInt32("Section4Count");
            bw.ReserveInt32("Section5Offset");
            bw.ReserveInt32("Section5Count");
            bw.ReserveInt32("Section6Offset");
            bw.ReserveInt32("Section6Count");
            bw.ReserveInt32("Section7Offset");
            bw.ReserveInt32("Section7Count");
            bw.ReserveInt32("Section8Offset");
            bw.ReserveInt32("Section8Count");
            bw.ReserveInt32("Section9Offset");
            bw.ReserveInt32("Section9Count");
            bw.ReserveInt32("Section10Offset");
            bw.ReserveInt32("Section10Count");
            bw.ReserveInt32("Section11Offset");
            bw.ReserveInt32("Section11Count");
            bw.WriteInt32(1);
            bw.WriteInt32(0);
            if (this.Version == FXR3.FXRVersion.Sekiro)
            {
                bw.ReserveInt32("Section12Offset");
                bw.WriteInt32(this.Section12s.Count);
                bw.ReserveInt32("Section13Offset");
                bw.WriteInt32(this.Section13s.Count);
                bw.ReserveInt32("Section14Offset");
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
            }
            bw.FillInt32("Section1Offset", (int)bw.Position);
            this.RootStateMachine.Write(bw);
            bw.Pad(16);
            bw.FillInt32("Section2Offset", (int)bw.Position);
            this.RootStateMachine.WriteSection2s(bw);
            bw.Pad(16);
            bw.FillInt32("Section3Offset", (int)bw.Position);
            List<FXR3.FFXState> states = this.RootStateMachine.States;
            List<FXR3.FFXTransition> section3s = new List<FXR3.FFXTransition>();
            for (int index = 0; index < states.Count; ++index)
                states[index].WriteSection3s(bw, index, section3s);
            bw.FillInt32("Section3Count", section3s.Count);
            bw.Pad(16);
            bw.FillInt32("Section4Offset", (int)bw.Position);
            List<FXR3.FFXEffectCallA> section4s = new List<FXR3.FFXEffectCallA>();
            this.RootEffectCall.Write(bw, section4s);
            this.RootEffectCall.WriteSection4s(bw, section4s);
            bw.FillInt32("Section4Count", section4s.Count);
            bw.Pad(16);
            bw.FillInt32("Section5Offset", (int)bw.Position);
            int section5Count = 0;
            for (int index = 0; index < section4s.Count; ++index)
                section4s[index].WriteSection5s(bw, index, ref section5Count);
            bw.FillInt32("Section5Count", section5Count);
            bw.Pad(16);
            bw.FillInt32("Section6Offset", (int)bw.Position);
            section5Count = 0;
            List<FXR3.FFXActionCall> section6s = new List<FXR3.FFXActionCall>();
            for (int index = 0; index < section4s.Count; ++index)
                section4s[index].WriteSection6s(bw, index, ref section5Count, section6s);
            bw.FillInt32("Section6Count", section6s.Count);
            bw.Pad(16);
            bw.FillInt32("Section7Offset", (int)bw.Position);
            List<FXR3.FFXProperty> section7s = new List<FXR3.FFXProperty>();
            for (int index = 0; index < section6s.Count; ++index)
                section6s[index].WriteSection7s(bw, index, section7s);
            bw.FillInt32("Section7Count", section7s.Count);
            bw.Pad(16);
            bw.FillInt32("Section8Offset", (int)bw.Position);
            List<FXR3.Section8> section8s = new List<FXR3.Section8>();
            for (int index = 0; index < section7s.Count; ++index)
                section7s[index].WriteSection8s(bw, index, section8s);
            bw.FillInt32("Section8Count", section8s.Count);
            bw.Pad(16);
            bw.FillInt32("Section9Offset", (int)bw.Position);
            List<FXR3.Section9> section9s = new List<FXR3.Section9>();
            for (int index = 0; index < section8s.Count; ++index)
                section8s[index].WriteSection9s(bw, index, section9s);
            bw.FillInt32("Section9Count", section9s.Count);
            bw.Pad(16);
            bw.FillInt32("Section10Offset", (int)bw.Position);
            List<FXR3.Section10> section10s = new List<FXR3.Section10>();
            for (int index = 0; index < section6s.Count; ++index)
                section6s[index].WriteSection10s(bw, index, section10s);
            bw.FillInt32("Section10Count", section10s.Count);
            bw.Pad(16);
            bw.FillInt32("Section11Offset", (int)bw.Position);
            int section11Count = 0;
            for (int index = 0; index < section3s.Count; ++index)
                section3s[index].WriteSection11s(bw, index, ref section11Count);
            for (int index = 0; index < section6s.Count; ++index)
                section6s[index].WriteSection11s(bw, index, ref section11Count);
            for (int index = 0; index < section7s.Count; ++index)
                section7s[index].WriteSection11s(bw, index, ref section11Count);
            for (int index = 0; index < section8s.Count; ++index)
                section8s[index].WriteSection11s(bw, index, ref section11Count);
            for (int index = 0; index < section9s.Count; ++index)
                section9s[index].WriteSection11s(bw, index, ref section11Count);
            for (int index = 0; index < section10s.Count; ++index)
                section10s[index].WriteSection11s(bw, index, ref section11Count);
            bw.FillInt32("Section11Count", section11Count);
            bw.Pad(16);
            if (this.Version != FXR3.FXRVersion.Sekiro)
                return;
            bw.FillInt32("Section12Offset", (int)bw.Position);
            bw.WriteInt32s((IList<int>)this.Section12s);
            bw.Pad(16);
            bw.FillInt32("Section13Offset", (int)bw.Position);
            bw.WriteInt32s((IList<int>)this.Section13s);
            bw.Pad(16);
            bw.FillInt32("Section14Offset", (int)bw.Position);
        }

        public enum FXRVersion : ushort
        {
            DarkSouls3 = 4,
            Sekiro = 5,
        }

        public class FFXStateMachine
        {
            public List<FXR3.FFXState> States { get; set; }

            public FFXStateMachine() => this.States = new List<FXR3.FFXState>();

            internal FFXStateMachine(BinaryReaderEx br)
            {
                br.AssertInt32(new int[1]);
                int capacity = br.ReadInt32();
                int num = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num);
                this.States = new List<FXR3.FFXState>(capacity);
                for (int index = 0; index < capacity; ++index)
                    this.States.Add(new FXR3.FFXState(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(this.States.Count);
                bw.ReserveInt32("Section1Section2sOffset");
                bw.WriteInt32(0);
            }

            internal void WriteSection2s(BinaryWriterEx bw)
            {
                bw.FillInt32("Section1Section2sOffset", (int)bw.Position);
                for (int index = 0; index < this.States.Count; ++index)
                    this.States[index].Write(bw, index);
            }
        }

        public class FFXState
        {
            public List<FXR3.FFXTransition> Transitions { get; set; }

            public FFXState() => this.Transitions = new List<FXR3.FFXTransition>();

            internal FFXState(BinaryReaderEx br)
            {
                br.AssertInt32(new int[1]);
                int capacity = br.ReadInt32();
                int num = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num);
                this.Transitions = new List<FXR3.FFXTransition>(capacity);
                for (int index = 0; index < capacity; ++index)
                    this.Transitions.Add(new FXR3.FFXTransition(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(this.Transitions.Count);
                bw.ReserveInt32(string.Format("Section2Section3sOffset[{0}]", (object)index));
                bw.WriteInt32(0);
            }

            internal void WriteSection3s(
              BinaryWriterEx bw,
              int index,
              List<FXR3.FFXTransition> section3s)
            {
                bw.FillInt32(string.Format("Section2Section3sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FXR3.FFXTransition transition in this.Transitions)
                    transition.Write(bw, section3s);
            }
        }

        public class FFXTransition
        {
            [XmlAttribute]
            public int TargetStateIndex { get; set; }

            public int Unk10 { get; set; }

            public int Unk38 { get; set; }

            public int Section11Data1 { get; set; }

            public int Section11Data2 { get; set; }

            public FFXTransition()
            {
            }

            internal FFXTransition(BinaryReaderEx br)
            {
                int num1 = (int)br.AssertInt16((short)11);
                int num2 = (int)br.AssertByte(new byte[1]);
                int num3 = (int)br.AssertByte((byte)1);
                br.AssertInt32(new int[1]);
                this.TargetStateIndex = br.ReadInt32();
                br.AssertInt32(new int[1]);
                this.Unk10 = br.AssertInt32(16842748, 16842749);
                br.AssertInt32(new int[1]);
                br.AssertInt32(1);
                br.AssertInt32(new int[1]);
                int num4 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                this.Unk38 = br.AssertInt32(16842748, 16842749);
                br.AssertInt32(new int[1]);
                br.AssertInt32(1);
                br.AssertInt32(new int[1]);
                int num5 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                this.Section11Data1 = br.GetInt32((long)num4);
                this.Section11Data2 = br.GetInt32((long)num5);
            }

            internal void Write(BinaryWriterEx bw, List<FXR3.FFXTransition> section3s)
            {
                int count = section3s.Count;
                bw.WriteInt16((short)11);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(0);
                bw.WriteInt32(this.TargetStateIndex);
                bw.WriteInt32(0);
                bw.WriteInt32(this.Unk10);
                bw.WriteInt32(0);
                bw.WriteInt32(1);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section3Section11Offset1[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(this.Unk38);
                bw.WriteInt32(0);
                bw.WriteInt32(1);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section3Section11Offset2[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                section3s.Add(this);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section3Section11Offset1[{0}]", (object)index), (int)bw.Position);
                bw.WriteInt32(this.Section11Data1);
                bw.FillInt32(string.Format("Section3Section11Offset2[{0}]", (object)index), (int)bw.Position);
                bw.WriteInt32(this.Section11Data2);
                section11Count += 2;
            }
        }

        public class FFXEffectCallA
        {
            [XmlAttribute]
            public short EffectID { get; set; }

            public List<FXR3.FFXEffectCallA> EffectAs { get; set; }

            public List<FXR3.FFXEffectCallB> EffectBs { get; set; }

            public List<FXR3.FFXActionCall> Actions { get; set; }

            public FFXEffectCallA()
            {
                this.EffectAs = new List<FXR3.FFXEffectCallA>();
                this.EffectBs = new List<FXR3.FFXEffectCallB>();
                this.Actions = new List<FXR3.FFXActionCall>();
            }

            internal FFXEffectCallA(BinaryReaderEx br)
            {
                this.EffectID = br.ReadInt16();
                int num1 = (int)br.AssertByte(new byte[1]);
                int num2 = (int)br.AssertByte((byte)1);
                br.AssertInt32(new int[1]);
                int capacity1 = br.ReadInt32();
                int capacity2 = br.ReadInt32();
                int capacity3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num4 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num5 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num5);
                this.EffectAs = new List<FXR3.FFXEffectCallA>(capacity3);
                for (int index = 0; index < capacity3; ++index)
                    this.EffectAs.Add(new FXR3.FFXEffectCallA(br));
                br.StepOut();
                br.StepIn((long)num3);
                this.EffectBs = new List<FXR3.FFXEffectCallB>(capacity1);
                for (int index = 0; index < capacity1; ++index)
                    this.EffectBs.Add(new FXR3.FFXEffectCallB(br));
                br.StepOut();
                br.StepIn((long)num4);
                this.Actions = new List<FXR3.FFXActionCall>(capacity2);
                for (int index = 0; index < capacity2; ++index)
                    this.Actions.Add(new FXR3.FFXActionCall(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, List<FXR3.FFXEffectCallA> section4s)
            {
                int count = section4s.Count;
                bw.WriteInt16(this.EffectID);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(0);
                bw.WriteInt32(this.EffectBs.Count);
                bw.WriteInt32(this.Actions.Count);
                bw.WriteInt32(this.EffectAs.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section4Section5sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section4Section6sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section4Section4sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                section4s.Add(this);
            }

            internal void WriteSection4s(BinaryWriterEx bw, List<FXR3.FFXEffectCallA> section4s)
            {
                int num = section4s.IndexOf(this);
                if (this.EffectAs.Count == 0)
                {
                    bw.FillInt32(string.Format("Section4Section4sOffset[{0}]", (object)num), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section4Section4sOffset[{0}]", (object)num), (int)bw.Position);
                    foreach (FXR3.FFXEffectCallA effectA in this.EffectAs)
                        effectA.Write(bw, section4s);
                    foreach (FXR3.FFXEffectCallA effectA in this.EffectAs)
                        effectA.WriteSection4s(bw, section4s);
                }
            }

            internal void WriteSection5s(BinaryWriterEx bw, int index, ref int section5Count)
            {
                if (this.EffectBs.Count == 0)
                {
                    bw.FillInt32(string.Format("Section4Section5sOffset[{0}]", (object)index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section4Section5sOffset[{0}]", (object)index), (int)bw.Position);
                    for (int index1 = 0; index1 < this.EffectBs.Count; ++index1)
                        this.EffectBs[index1].Write(bw, section5Count + index1);
                    section5Count += this.EffectBs.Count;
                }
            }

            internal void WriteSection6s(
              BinaryWriterEx bw,
              int index,
              ref int section5Count,
              List<FXR3.FFXActionCall> section6s)
            {
                bw.FillInt32(string.Format("Section4Section6sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FXR3.FFXActionCall action in this.Actions)
                    action.Write(bw, section6s);
                for (int index1 = 0; index1 < this.EffectBs.Count; ++index1)
                    this.EffectBs[index1].WriteSection6s(bw, section5Count + index1, section6s);
                section5Count += this.EffectBs.Count;
            }
        }

        public class FFXEffectCallB
        {
            [XmlAttribute]
            public short EffectID { get; set; }

            public List<FXR3.FFXActionCall> Actions { get; set; }

            public FFXEffectCallB() => this.Actions = new List<FXR3.FFXActionCall>();

            internal FFXEffectCallB(BinaryReaderEx br)
            {
                this.EffectID = br.ReadInt16();
                int num1 = (int)br.AssertByte(new byte[1]);
                int num2 = (int)br.AssertByte((byte)1);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                int capacity = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                int num3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num3);
                this.Actions = new List<FXR3.FFXActionCall>(capacity);
                for (int index = 0; index < capacity; ++index)
                    this.Actions.Add(new FXR3.FFXActionCall(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt16(this.EffectID);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(this.Actions.Count);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section5Section6sOffset[{0}]", (object)index));
                bw.WriteInt32(0);
            }

            internal void WriteSection6s(
              BinaryWriterEx bw,
              int index,
              List<FXR3.FFXActionCall> section6s)
            {
                bw.FillInt32(string.Format("Section5Section6sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FXR3.FFXActionCall action in this.Actions)
                    action.Write(bw, section6s);
            }
        }

        public class FFXActionCall
        {
            [XmlAttribute]
            public short ActionID { get; set; }

            public bool Unk02 { get; set; }

            public bool Unk03 { get; set; }

            public int Unk04 { get; set; }

            public List<FXR3.FFXProperty> Properties1 { get; set; }

            public List<FXR3.FFXProperty> Properties2 { get; set; }

            public List<FXR3.Section10> Section10s { get; set; }

            public List<FXR3.FFXField> Fields1 { get; set; }

            public List<FXR3.FFXField> Fields2 { get; set; }

            public FFXActionCall()
            {
                this.Properties1 = new List<FXR3.FFXProperty>();
                this.Properties2 = new List<FXR3.FFXProperty>();
                this.Section10s = new List<FXR3.Section10>();
                this.Fields1 = new List<FXR3.FFXField>();
                this.Fields2 = new List<FXR3.FFXField>();
            }

            internal FFXActionCall(BinaryReaderEx br)
            {
                this.ActionID = br.ReadInt16();
                this.Unk02 = br.ReadBoolean();
                this.Unk03 = br.ReadBoolean();
                this.Unk04 = br.ReadInt32();
                int count1 = br.ReadInt32();
                int capacity1 = br.ReadInt32();
                int capacity2 = br.ReadInt32();
                int count2 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int capacity3 = br.ReadInt32();
                int num1 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num2 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.StepIn((long)num3);
                this.Properties1 = new List<FXR3.FFXProperty>(capacity2);
                for (int index = 0; index < capacity2; ++index)
                    this.Properties1.Add(new FXR3.FFXProperty(br));
                this.Properties2 = new List<FXR3.FFXProperty>(capacity3);
                for (int index = 0; index < capacity3; ++index)
                    this.Properties2.Add(new FXR3.FFXProperty(br));
                br.StepOut();
                br.StepIn((long)num2);
                this.Section10s = new List<FXR3.Section10>(capacity1);
                for (int index = 0; index < capacity1; ++index)
                    this.Section10s.Add(new FXR3.Section10(br));
                br.StepOut();
                br.StepIn((long)num1);
                this.Fields1 = FXR3.FFXField.ReadMany(br, count1);
                this.Fields2 = FXR3.FFXField.ReadMany(br, count2);
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, List<FXR3.FFXActionCall> section6s)
            {
                int count = section6s.Count;
                bw.WriteInt16(this.ActionID);
                bw.WriteBoolean(this.Unk02);
                bw.WriteBoolean(this.Unk03);
                bw.WriteInt32(this.Unk04);
                bw.WriteInt32(this.Fields1.Count);
                bw.WriteInt32(this.Section10s.Count);
                bw.WriteInt32(this.Properties1.Count);
                bw.WriteInt32(this.Fields2.Count);
                bw.WriteInt32(0);
                bw.WriteInt32(this.Properties2.Count);
                bw.ReserveInt32(string.Format("Section6Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section6Section10sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section6Section7sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                section6s.Add(this);
            }

            internal void WriteSection7s(BinaryWriterEx bw, int index, List<FXR3.FFXProperty> section7s)
            {
                bw.FillInt32(string.Format("Section6Section7sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FXR3.FFXProperty ffxProperty in this.Properties1)
                    ffxProperty.Write(bw, section7s);
                foreach (FXR3.FFXProperty ffxProperty in this.Properties2)
                    ffxProperty.Write(bw, section7s);
            }

            internal void WriteSection10s(BinaryWriterEx bw, int index, List<FXR3.Section10> section10s)
            {
                bw.FillInt32(string.Format("Section6Section10sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FXR3.Section10 section10 in this.Section10s)
                    section10.Write(bw, section10s);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                if (this.Fields1.Count == 0 && this.Fields2.Count == 0)
                {
                    bw.FillInt32(string.Format("Section6Section11sOffset[{0}]", (object)index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section6Section11sOffset[{0}]", (object)index), (int)bw.Position);
                    foreach (FXR3.FFXField ffxField in this.Fields1)
                        ffxField.Write(bw);
                    foreach (FXR3.FFXField ffxField in this.Fields2)
                        ffxField.Write(bw);
                    section11Count += this.Fields1.Count + this.Fields2.Count;
                }
            }
        }

        [XmlInclude(typeof(FXR3.FFXField.FFXFieldFloat))]
        [XmlInclude(typeof(FXR3.FFXField.FFXFieldInt))]
        public abstract class FFXField
        {
            public static FXR3.FFXField Read(BinaryReaderEx br)
            {
                float single = br.GetSingle(br.Position);
                FXR3.FFXField ffxField;
                if ((double)single >= 9.99999974737875E-05 && (double)single < 1000000.0 || (double)single <= -9.99999974737875E-05 && (double)single > -1000000.0)
                    ffxField = (FXR3.FFXField)new FXR3.FFXField.FFXFieldFloat()
                    {
                        Value = single
                    };
                else
                    ffxField = (FXR3.FFXField)new FXR3.FFXField.FFXFieldInt()
                    {
                        Value = br.GetInt32(br.Position)
                    };
                br.Position += 4L;
                return ffxField;
            }

            public static List<FXR3.FFXField> ReadMany(BinaryReaderEx br, int count)
            {
                List<FXR3.FFXField> ffxFieldList = new List<FXR3.FFXField>();
                for (int index = 0; index < count; ++index)
                    ffxFieldList.Add(FXR3.FFXField.Read(br));
                return ffxFieldList;
            }

            public static List<FXR3.FFXField> ReadManyAt(
              BinaryReaderEx br,
              int offset,
              int count)
            {
                br.StepIn((long)offset);
                List<FXR3.FFXField> ffxFieldList = FXR3.FFXField.ReadMany(br, count);
                br.StepOut();
                return ffxFieldList;
            }

            public abstract void Write(BinaryWriterEx bw);

            public class FFXFieldFloat : FXR3.FFXField
            {
                [XmlAttribute]
                public float Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteSingle(this.Value);
            }

            public class FFXFieldInt : FXR3.FFXField
            {
                [XmlAttribute]
                public int Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteInt32(this.Value);
            }
        }

        public class FFXProperty
        {
            [XmlAttribute]
            public short TypeEnumA { get; set; }

            [XmlAttribute]
            public int TypeEnumB { get; set; }

            public List<FXR3.Section8> Section8s { get; set; }

            public List<FXR3.FFXField> Fields { get; set; }

            public FFXProperty()
            {
                this.Section8s = new List<FXR3.Section8>();
                this.Fields = new List<FXR3.FFXField>();
            }

            internal FFXProperty(BinaryReaderEx br)
            {
                this.TypeEnumA = br.ReadInt16();
                int num1 = (int)br.AssertByte(new byte[1]);
                int num2 = (int)br.AssertByte((byte)1);
                this.TypeEnumB = br.ReadInt32();
                int count = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int offset = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int capacity = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num3);
                this.Section8s = new List<FXR3.Section8>(capacity);
                for (int index = 0; index < capacity; ++index)
                    this.Section8s.Add(new FXR3.Section8(br));
                br.StepOut();
                this.Fields = new List<FXR3.FFXField>();
                this.Fields = FXR3.FFXField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<FXR3.FFXProperty> section7s)
            {
                int count = section7s.Count;
                bw.WriteInt16(this.TypeEnumA);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(this.TypeEnumB);
                bw.WriteInt32(this.Fields.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section7Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section7Section8sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(this.Section8s.Count);
                bw.WriteInt32(0);
                section7s.Add(this);
            }

            internal void WriteSection8s(BinaryWriterEx bw, int index, List<FXR3.Section8> section8s)
            {
                bw.FillInt32(string.Format("Section7Section8sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FXR3.Section8 section8 in this.Section8s)
                    section8.Write(bw, section8s);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                if (this.Fields.Count == 0)
                {
                    bw.FillInt32(string.Format("Section7Section11sOffset[{0}]", (object)index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section7Section11sOffset[{0}]", (object)index), (int)bw.Position);
                    foreach (FXR3.FFXField field in this.Fields)
                        field.Write(bw);
                    section11Count += this.Fields.Count;
                }
            }
        }

        public class Section8
        {
            [XmlAttribute]
            public ushort Unk00 { get; set; }

            public int Unk04 { get; set; }

            public List<FXR3.Section9> Section9s { get; set; }

            public List<FXR3.FFXField> Fields { get; set; }

            public Section8()
            {
                this.Section9s = new List<FXR3.Section9>();
                this.Fields = new List<FXR3.FFXField>();
            }

            internal Section8(BinaryReaderEx br)
            {
                this.Unk00 = br.ReadUInt16();
                int num1 = (int)br.AssertByte(new byte[1]);
                int num2 = (int)br.AssertByte((byte)1);
                this.Unk04 = br.ReadInt32();
                int count = br.ReadInt32();
                int capacity = br.ReadInt32();
                int offset = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num3);
                this.Section9s = new List<FXR3.Section9>(capacity);
                for (int index = 0; index < capacity; ++index)
                    this.Section9s.Add(new FXR3.Section9(br));
                br.StepOut();
                this.Fields = FXR3.FFXField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<FXR3.Section8> section8s)
            {
                int count = section8s.Count;
                bw.WriteUInt16(this.Unk00);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(this.Unk04);
                bw.WriteInt32(this.Fields.Count);
                bw.WriteInt32(this.Section9s.Count);
                bw.ReserveInt32(string.Format("Section8Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section8Section9sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                section8s.Add(this);
            }

            internal void WriteSection9s(BinaryWriterEx bw, int index, List<FXR3.Section9> section9s)
            {
                bw.FillInt32(string.Format("Section8Section9sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FXR3.Section9 section9 in this.Section9s)
                    section9.Write(bw, section9s);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section8Section11sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FXR3.FFXField field in this.Fields)
                    field.Write(bw);
                section11Count += this.Fields.Count;
            }
        }

        public class Section9
        {
            public int Unk04 { get; set; }

            public List<FXR3.FFXField> Fields { get; set; }

            public Section9() => this.Fields = new List<FXR3.FFXField>();

            internal Section9(BinaryReaderEx br)
            {
                int num1 = (int)br.AssertInt16((short)48);
                int num2 = (int)br.AssertByte(new byte[1]);
                int num3 = (int)br.AssertByte((byte)1);
                this.Unk04 = br.ReadInt32();
                int count = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int offset = br.ReadInt32();
                br.AssertInt32(new int[1]);
                this.Fields = FXR3.FFXField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<FXR3.Section9> section9s)
            {
                int count = section9s.Count;
                bw.WriteInt16((short)48);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(this.Unk04);
                bw.WriteInt32(this.Fields.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section9Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                section9s.Add(this);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section9Section11sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FXR3.FFXField field in this.Fields)
                    field.Write(bw);
                section11Count += this.Fields.Count;
            }
        }

        public class Section10
        {
            public List<FXR3.FFXField> Fields { get; set; }

            public Section10() => this.Fields = new List<FXR3.FFXField>();

            internal Section10(BinaryReaderEx br)
            {
                int offset = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int count = br.ReadInt32();
                br.AssertInt32(new int[1]);
                this.Fields = FXR3.FFXField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<FXR3.Section10> section10s)
            {
                int count = section10s.Count;
                bw.ReserveInt32(string.Format("Section10Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(this.Fields.Count);
                bw.WriteInt32(0);
                section10s.Add(this);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section10Section11sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FXR3.FFXField field in this.Fields)
                    field.Write(bw);
                section11Count += this.Fields.Count;
            }
        }
    }
}