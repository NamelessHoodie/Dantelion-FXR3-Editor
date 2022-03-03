using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SoulsFormats;

namespace FXR3_XMLR
{
    /// <summary>
    /// An SFX definition file used in DS3 and Sekiro. Extension: .fxr
    /// </summary>
    public class Fxr3 : SoulsFile<Fxr3>
    {
        public Fxr3.FxrVersion Version { get; set; }

        public int Id { get; set; }

        public Fxr3.FfxStateMachine RootStateMachine { get; set; }

        public Fxr3.FfxEffectCallA RootEffectCall { get; set; }

        public List<int> Section12S { get; set; }

        public List<int> Section13S { get; set; }

        public Fxr3()
        {
            this.Version = Fxr3.FxrVersion.DarkSouls3;
            this.RootStateMachine = new Fxr3.FfxStateMachine();
            this.RootEffectCall = new Fxr3.FfxEffectCallA();
            this.Section12S = new List<int>();
            this.Section13S = new List<int>();
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
            this.Version = br.ReadEnum16<Fxr3.FxrVersion>();
            br.AssertInt32(1);
            this.Id = br.ReadInt32();
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
            if (this.Version == Fxr3.FxrVersion.Sekiro)
            {
                int num4 = br.ReadInt32();
                int count1 = br.ReadInt32();
                int num5 = br.ReadInt32();
                int count2 = br.ReadInt32();
                br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                this.Section12S = new List<int>((IEnumerable<int>)br.GetInt32s((long)num4, count1));
                this.Section13S = new List<int>((IEnumerable<int>)br.GetInt32s((long)num5, count2));
            }
            else
            {
                this.Section12S = new List<int>();
                this.Section13S = new List<int>();
            }
            br.Position = (long)num2;
            this.RootStateMachine = new Fxr3.FfxStateMachine(br);
            br.Position = (long)num3;
            this.RootEffectCall = new Fxr3.FfxEffectCallA(br);
        }

        protected override void Write(BinaryWriterEx bw)
        {
            bw.WriteASCII("FXR\0");
            bw.WriteInt16((short)0);
            bw.WriteUInt16((ushort)this.Version);
            bw.WriteInt32(1);
            bw.WriteInt32(this.Id);
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
            if (this.Version == Fxr3.FxrVersion.Sekiro)
            {
                bw.ReserveInt32("Section12Offset");
                bw.WriteInt32(this.Section12S.Count);
                bw.ReserveInt32("Section13Offset");
                bw.WriteInt32(this.Section13S.Count);
                bw.ReserveInt32("Section14Offset");
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
            }
            bw.FillInt32("Section1Offset", (int)bw.Position);
            this.RootStateMachine.Write(bw);
            bw.Pad(16);
            bw.FillInt32("Section2Offset", (int)bw.Position);
            this.RootStateMachine.WriteSection2S(bw);
            bw.Pad(16);
            bw.FillInt32("Section3Offset", (int)bw.Position);
            List<Fxr3.FfxState> states = this.RootStateMachine.States;
            List<Fxr3.FfxTransition> section3S = new List<Fxr3.FfxTransition>();
            for (int index = 0; index < states.Count; ++index)
                states[index].WriteSection3S(bw, index, section3S);
            bw.FillInt32("Section3Count", section3S.Count);
            bw.Pad(16);
            bw.FillInt32("Section4Offset", (int)bw.Position);
            List<Fxr3.FfxEffectCallA> section4S = new List<Fxr3.FfxEffectCallA>();
            this.RootEffectCall.Write(bw, section4S);
            this.RootEffectCall.WriteSection4S(bw, section4S);
            bw.FillInt32("Section4Count", section4S.Count);
            bw.Pad(16);
            bw.FillInt32("Section5Offset", (int)bw.Position);
            int section5Count = 0;
            for (int index = 0; index < section4S.Count; ++index)
                section4S[index].WriteSection5S(bw, index, ref section5Count);
            bw.FillInt32("Section5Count", section5Count);
            bw.Pad(16);
            bw.FillInt32("Section6Offset", (int)bw.Position);
            section5Count = 0;
            List<Fxr3.FfxActionCall> section6S = new List<Fxr3.FfxActionCall>();
            for (int index = 0; index < section4S.Count; ++index)
                section4S[index].WriteSection6S(bw, index, ref section5Count, section6S);
            bw.FillInt32("Section6Count", section6S.Count);
            bw.Pad(16);
            bw.FillInt32("Section7Offset", (int)bw.Position);
            List<Fxr3.FfxProperty> section7S = new List<Fxr3.FfxProperty>();
            for (int index = 0; index < section6S.Count; ++index)
                section6S[index].WriteSection7S(bw, index, section7S);
            bw.FillInt32("Section7Count", section7S.Count);
            bw.Pad(16);
            bw.FillInt32("Section8Offset", (int)bw.Position);
            List<Fxr3.Section8> section8S = new List<Fxr3.Section8>();
            for (int index = 0; index < section7S.Count; ++index)
                section7S[index].WriteSection8S(bw, index, section8S);
            bw.FillInt32("Section8Count", section8S.Count);
            bw.Pad(16);
            bw.FillInt32("Section9Offset", (int)bw.Position);
            List<Fxr3.Section9> section9S = new List<Fxr3.Section9>();
            for (int index = 0; index < section8S.Count; ++index)
                section8S[index].WriteSection9S(bw, index, section9S);
            bw.FillInt32("Section9Count", section9S.Count);
            bw.Pad(16);
            bw.FillInt32("Section10Offset", (int)bw.Position);
            List<Fxr3.Section10> section10S = new List<Fxr3.Section10>();
            for (int index = 0; index < section6S.Count; ++index)
                section6S[index].WriteSection10S(bw, index, section10S);
            bw.FillInt32("Section10Count", section10S.Count);
            bw.Pad(16);
            bw.FillInt32("Section11Offset", (int)bw.Position);
            int section11Count = 0;
            for (int index = 0; index < section3S.Count; ++index)
                section3S[index].WriteSection11S(bw, index, ref section11Count);
            for (int index = 0; index < section6S.Count; ++index)
                section6S[index].WriteSection11S(bw, index, ref section11Count);
            for (int index = 0; index < section7S.Count; ++index)
                section7S[index].WriteSection11S(bw, index, ref section11Count);
            for (int index = 0; index < section8S.Count; ++index)
                section8S[index].WriteSection11S(bw, index, ref section11Count);
            for (int index = 0; index < section9S.Count; ++index)
                section9S[index].WriteSection11S(bw, index, ref section11Count);
            for (int index = 0; index < section10S.Count; ++index)
                section10S[index].WriteSection11S(bw, index, ref section11Count);
            bw.FillInt32("Section11Count", section11Count);
            bw.Pad(16);
            if (this.Version != Fxr3.FxrVersion.Sekiro)
                return;
            bw.FillInt32("Section12Offset", (int)bw.Position);
            bw.WriteInt32s((IList<int>)this.Section12S);
            bw.Pad(16);
            bw.FillInt32("Section13Offset", (int)bw.Position);
            bw.WriteInt32s((IList<int>)this.Section13S);
            bw.Pad(16);
            bw.FillInt32("Section14Offset", (int)bw.Position);
        }

        public enum FxrVersion : ushort
        {
            DarkSouls3 = 4,
            Sekiro = 5,
        }

        public class FfxStateMachine
        {
            public List<Fxr3.FfxState> States { get; set; }

            public FfxStateMachine() => this.States = new List<Fxr3.FfxState>();

            internal FfxStateMachine(BinaryReaderEx br)
            {
                br.AssertInt32(new int[1]);
                int capacity = br.ReadInt32();
                int num = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num);
                this.States = new List<Fxr3.FfxState>(capacity);
                for (int index = 0; index < capacity; ++index)
                    this.States.Add(new Fxr3.FfxState(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(this.States.Count);
                bw.ReserveInt32("Section1Section2sOffset");
                bw.WriteInt32(0);
            }

            internal void WriteSection2S(BinaryWriterEx bw)
            {
                bw.FillInt32("Section1Section2sOffset", (int)bw.Position);
                for (int index = 0; index < this.States.Count; ++index)
                    this.States[index].Write(bw, index);
            }
        }

        public class FfxState
        {
            public List<Fxr3.FfxTransition> Transitions { get; set; }

            public FfxState() => this.Transitions = new List<Fxr3.FfxTransition>();

            internal FfxState(BinaryReaderEx br)
            {
                br.AssertInt32(new int[1]);
                int capacity = br.ReadInt32();
                int num = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num);
                this.Transitions = new List<Fxr3.FfxTransition>(capacity);
                for (int index = 0; index < capacity; ++index)
                    this.Transitions.Add(new Fxr3.FfxTransition(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(this.Transitions.Count);
                bw.ReserveInt32(string.Format("Section2Section3sOffset[{0}]", (object)index));
                bw.WriteInt32(0);
            }

            internal void WriteSection3S(
              BinaryWriterEx bw,
              int index,
              List<Fxr3.FfxTransition> section3S)
            {
                bw.FillInt32(string.Format("Section2Section3sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Fxr3.FfxTransition transition in this.Transitions)
                    transition.Write(bw, section3S);
            }
        }

        public class FfxTransition
        {
            [XmlAttribute]
            public int TargetStateIndex { get; set; }

            public int Unk10 { get; set; }

            public int Unk38 { get; set; }

            public int Section11Data1 { get; set; }

            public int Section11Data2 { get; set; }

            public FfxTransition()
            {
            }

            internal FfxTransition(BinaryReaderEx br)
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

            internal void Write(BinaryWriterEx bw, List<Fxr3.FfxTransition> section3S)
            {
                int count = section3S.Count;
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
                section3S.Add(this);
            }

            internal void WriteSection11S(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section3Section11Offset1[{0}]", (object)index), (int)bw.Position);
                bw.WriteInt32(this.Section11Data1);
                bw.FillInt32(string.Format("Section3Section11Offset2[{0}]", (object)index), (int)bw.Position);
                bw.WriteInt32(this.Section11Data2);
                section11Count += 2;
            }
        }

        public class FfxEffectCallA
        {
            [XmlAttribute]
            public short EffectId { get; set; }

            public List<Fxr3.FfxEffectCallA> EffectAs { get; set; }

            public List<Fxr3.FfxEffectCallB> EffectBs { get; set; }

            public List<Fxr3.FfxActionCall> Actions { get; set; }

            public FfxEffectCallA()
            {
                this.EffectAs = new List<Fxr3.FfxEffectCallA>();
                this.EffectBs = new List<Fxr3.FfxEffectCallB>();
                this.Actions = new List<Fxr3.FfxActionCall>();
            }

            internal FfxEffectCallA(BinaryReaderEx br)
            {
                this.EffectId = br.ReadInt16();
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
                this.EffectAs = new List<Fxr3.FfxEffectCallA>(capacity3);
                for (int index = 0; index < capacity3; ++index)
                    this.EffectAs.Add(new Fxr3.FfxEffectCallA(br));
                br.StepOut();
                br.StepIn((long)num3);
                this.EffectBs = new List<Fxr3.FfxEffectCallB>(capacity1);
                for (int index = 0; index < capacity1; ++index)
                    this.EffectBs.Add(new Fxr3.FfxEffectCallB(br));
                br.StepOut();
                br.StepIn((long)num4);
                this.Actions = new List<Fxr3.FfxActionCall>(capacity2);
                for (int index = 0; index < capacity2; ++index)
                    this.Actions.Add(new Fxr3.FfxActionCall(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, List<Fxr3.FfxEffectCallA> section4S)
            {
                int count = section4S.Count;
                bw.WriteInt16(this.EffectId);
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
                section4S.Add(this);
            }

            internal void WriteSection4S(BinaryWriterEx bw, List<Fxr3.FfxEffectCallA> section4S)
            {
                int num = section4S.IndexOf(this);
                if (this.EffectAs.Count == 0)
                {
                    bw.FillInt32(string.Format("Section4Section4sOffset[{0}]", (object)num), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section4Section4sOffset[{0}]", (object)num), (int)bw.Position);
                    foreach (Fxr3.FfxEffectCallA effectA in this.EffectAs)
                        effectA.Write(bw, section4S);
                    foreach (Fxr3.FfxEffectCallA effectA in this.EffectAs)
                        effectA.WriteSection4S(bw, section4S);
                }
            }

            internal void WriteSection5S(BinaryWriterEx bw, int index, ref int section5Count)
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

            internal void WriteSection6S(
              BinaryWriterEx bw,
              int index,
              ref int section5Count,
              List<Fxr3.FfxActionCall> section6S)
            {
                bw.FillInt32(string.Format("Section4Section6sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Fxr3.FfxActionCall action in this.Actions)
                    action.Write(bw, section6S);
                for (int index1 = 0; index1 < this.EffectBs.Count; ++index1)
                    this.EffectBs[index1].WriteSection6S(bw, section5Count + index1, section6S);
                section5Count += this.EffectBs.Count;
            }
        }

        public class FfxEffectCallB
        {
            [XmlAttribute]
            public short EffectId { get; set; }

            public List<Fxr3.FfxActionCall> Actions { get; set; }

            public FfxEffectCallB() => this.Actions = new List<Fxr3.FfxActionCall>();

            internal FfxEffectCallB(BinaryReaderEx br)
            {
                this.EffectId = br.ReadInt16();
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
                this.Actions = new List<Fxr3.FfxActionCall>(capacity);
                for (int index = 0; index < capacity; ++index)
                    this.Actions.Add(new Fxr3.FfxActionCall(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt16(this.EffectId);
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

            internal void WriteSection6S(
              BinaryWriterEx bw,
              int index,
              List<Fxr3.FfxActionCall> section6S)
            {
                bw.FillInt32(string.Format("Section5Section6sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Fxr3.FfxActionCall action in this.Actions)
                    action.Write(bw, section6S);
            }
        }

        public class FfxActionCall
        {
            [XmlAttribute]
            public short ActionId { get; set; }

            public bool Unk02 { get; set; }

            public bool Unk03 { get; set; }

            public int Unk04 { get; set; }

            public List<Fxr3.FfxProperty> Properties1 { get; set; }

            public List<Fxr3.FfxProperty> Properties2 { get; set; }

            public List<Fxr3.Section10> Section10S { get; set; }

            public List<Fxr3.FfxField> Fields1 { get; set; }

            public List<Fxr3.FfxField> Fields2 { get; set; }

            public FfxActionCall()
            {
                this.Properties1 = new List<Fxr3.FfxProperty>();
                this.Properties2 = new List<Fxr3.FfxProperty>();
                this.Section10S = new List<Fxr3.Section10>();
                this.Fields1 = new List<Fxr3.FfxField>();
                this.Fields2 = new List<Fxr3.FfxField>();
            }

            internal FfxActionCall(BinaryReaderEx br)
            {
                this.ActionId = br.ReadInt16();
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
                this.Properties1 = new List<Fxr3.FfxProperty>(capacity2);
                for (int index = 0; index < capacity2; ++index)
                    this.Properties1.Add(new Fxr3.FfxProperty(br));
                this.Properties2 = new List<Fxr3.FfxProperty>(capacity3);
                for (int index = 0; index < capacity3; ++index)
                    this.Properties2.Add(new Fxr3.FfxProperty(br));
                br.StepOut();
                br.StepIn((long)num2);
                this.Section10S = new List<Fxr3.Section10>(capacity1);
                for (int index = 0; index < capacity1; ++index)
                    this.Section10S.Add(new Fxr3.Section10(br));
                br.StepOut();
                br.StepIn((long)num1);
                this.Fields1 = Fxr3.FfxField.ReadMany(br, count1);
                this.Fields2 = Fxr3.FfxField.ReadMany(br, count2);
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, List<Fxr3.FfxActionCall> section6S)
            {
                int count = section6S.Count;
                bw.WriteInt16(this.ActionId);
                bw.WriteBoolean(this.Unk02);
                bw.WriteBoolean(this.Unk03);
                bw.WriteInt32(this.Unk04);
                bw.WriteInt32(this.Fields1.Count);
                bw.WriteInt32(this.Section10S.Count);
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
                section6S.Add(this);
            }

            internal void WriteSection7S(BinaryWriterEx bw, int index, List<Fxr3.FfxProperty> section7S)
            {
                bw.FillInt32(string.Format("Section6Section7sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Fxr3.FfxProperty ffxProperty in this.Properties1)
                    ffxProperty.Write(bw, section7S);
                foreach (Fxr3.FfxProperty ffxProperty in this.Properties2)
                    ffxProperty.Write(bw, section7S);
            }

            internal void WriteSection10S(BinaryWriterEx bw, int index, List<Fxr3.Section10> section10S)
            {
                bw.FillInt32(string.Format("Section6Section10sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Fxr3.Section10 section10 in this.Section10S)
                    section10.Write(bw, section10S);
            }

            internal void WriteSection11S(BinaryWriterEx bw, int index, ref int section11Count)
            {
                if (this.Fields1.Count == 0 && this.Fields2.Count == 0)
                {
                    bw.FillInt32(string.Format("Section6Section11sOffset[{0}]", (object)index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section6Section11sOffset[{0}]", (object)index), (int)bw.Position);
                    foreach (Fxr3.FfxField ffxField in this.Fields1)
                        ffxField.Write(bw);
                    foreach (Fxr3.FfxField ffxField in this.Fields2)
                        ffxField.Write(bw);
                    section11Count += this.Fields1.Count + this.Fields2.Count;
                }
            }
        }

        [XmlInclude(typeof(Fxr3.FfxField.FfxFieldFloat))]
        [XmlInclude(typeof(Fxr3.FfxField.FfxFieldInt))]
        public abstract class FfxField
        {
            public static Fxr3.FfxField Read(BinaryReaderEx br)
            {
                float single = br.GetSingle(br.Position);
                Fxr3.FfxField ffxField;
                if ((double)single >= 9.99999974737875E-05 && (double)single < 1000000.0 || (double)single <= -9.99999974737875E-05 && (double)single > -1000000.0)
                    ffxField = (Fxr3.FfxField)new Fxr3.FfxField.FfxFieldFloat()
                    {
                        Value = single
                    };
                else
                    ffxField = (Fxr3.FfxField)new Fxr3.FfxField.FfxFieldInt()
                    {
                        Value = br.GetInt32(br.Position)
                    };
                br.Position += 4L;
                return ffxField;
            }

            public static List<Fxr3.FfxField> ReadMany(BinaryReaderEx br, int count)
            {
                List<Fxr3.FfxField> ffxFieldList = new List<Fxr3.FfxField>();
                for (int index = 0; index < count; ++index)
                    ffxFieldList.Add(Fxr3.FfxField.Read(br));
                return ffxFieldList;
            }

            public static List<Fxr3.FfxField> ReadManyAt(
              BinaryReaderEx br,
              int offset,
              int count)
            {
                br.StepIn((long)offset);
                List<Fxr3.FfxField> ffxFieldList = Fxr3.FfxField.ReadMany(br, count);
                br.StepOut();
                return ffxFieldList;
            }

            public abstract void Write(BinaryWriterEx bw);

            public class FfxFieldFloat : Fxr3.FfxField
            {
                [XmlAttribute]
                public float Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteSingle(this.Value);
            }

            public class FfxFieldInt : Fxr3.FfxField
            {
                [XmlAttribute]
                public int Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteInt32(this.Value);
            }
        }

        public class FfxProperty
        {
            [XmlAttribute]
            public short TypeEnumA { get; set; }

            [XmlAttribute]
            public int TypeEnumB { get; set; }

            public List<Fxr3.Section8> Section8S { get; set; }

            public List<Fxr3.FfxField> Fields { get; set; }

            public FfxProperty()
            {
                this.Section8S = new List<Fxr3.Section8>();
                this.Fields = new List<Fxr3.FfxField>();
            }

            internal FfxProperty(BinaryReaderEx br)
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
                this.Section8S = new List<Fxr3.Section8>(capacity);
                for (int index = 0; index < capacity; ++index)
                    this.Section8S.Add(new Fxr3.Section8(br));
                br.StepOut();
                this.Fields = new List<Fxr3.FfxField>();
                this.Fields = Fxr3.FfxField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<Fxr3.FfxProperty> section7S)
            {
                int count = section7S.Count;
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
                bw.WriteInt32(this.Section8S.Count);
                bw.WriteInt32(0);
                section7S.Add(this);
            }

            internal void WriteSection8S(BinaryWriterEx bw, int index, List<Fxr3.Section8> section8S)
            {
                bw.FillInt32(string.Format("Section7Section8sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Fxr3.Section8 section8 in this.Section8S)
                    section8.Write(bw, section8S);
            }

            internal void WriteSection11S(BinaryWriterEx bw, int index, ref int section11Count)
            {
                if (this.Fields.Count == 0)
                {
                    bw.FillInt32(string.Format("Section7Section11sOffset[{0}]", (object)index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section7Section11sOffset[{0}]", (object)index), (int)bw.Position);
                    foreach (Fxr3.FfxField field in this.Fields)
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

            public List<Fxr3.Section9> Section9S { get; set; }

            public List<Fxr3.FfxField> Fields { get; set; }

            public Section8()
            {
                this.Section9S = new List<Fxr3.Section9>();
                this.Fields = new List<Fxr3.FfxField>();
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
                this.Section9S = new List<Fxr3.Section9>(capacity);
                for (int index = 0; index < capacity; ++index)
                    this.Section9S.Add(new Fxr3.Section9(br));
                br.StepOut();
                this.Fields = Fxr3.FfxField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<Fxr3.Section8> section8S)
            {
                int count = section8S.Count;
                bw.WriteUInt16(this.Unk00);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(this.Unk04);
                bw.WriteInt32(this.Fields.Count);
                bw.WriteInt32(this.Section9S.Count);
                bw.ReserveInt32(string.Format("Section8Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section8Section9sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                section8S.Add(this);
            }

            internal void WriteSection9S(BinaryWriterEx bw, int index, List<Fxr3.Section9> section9S)
            {
                bw.FillInt32(string.Format("Section8Section9sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Fxr3.Section9 section9 in this.Section9S)
                    section9.Write(bw, section9S);
            }

            internal void WriteSection11S(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section8Section11sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Fxr3.FfxField field in this.Fields)
                    field.Write(bw);
                section11Count += this.Fields.Count;
            }
        }

        public class Section9
        {
            public int Unk04 { get; set; }

            public List<Fxr3.FfxField> Fields { get; set; }

            public Section9() => this.Fields = new List<Fxr3.FfxField>();

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
                this.Fields = Fxr3.FfxField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<Fxr3.Section9> section9S)
            {
                int count = section9S.Count;
                bw.WriteInt16((short)48);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(this.Unk04);
                bw.WriteInt32(this.Fields.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section9Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                section9S.Add(this);
            }

            internal void WriteSection11S(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section9Section11sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Fxr3.FfxField field in this.Fields)
                    field.Write(bw);
                section11Count += this.Fields.Count;
            }
        }

        public class Section10
        {
            public List<Fxr3.FfxField> Fields { get; set; }

            public Section10() => this.Fields = new List<Fxr3.FfxField>();

            internal Section10(BinaryReaderEx br)
            {
                int offset = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int count = br.ReadInt32();
                br.AssertInt32(new int[1]);
                this.Fields = Fxr3.FfxField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<Fxr3.Section10> section10S)
            {
                int count = section10S.Count;
                bw.ReserveInt32(string.Format("Section10Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(this.Fields.Count);
                bw.WriteInt32(0);
                section10S.Add(this);
            }

            internal void WriteSection11S(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section10Section11sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Fxr3.FfxField field in this.Fields)
                    field.Write(bw);
                section11Count += this.Fields.Count;
            }
        }
    }
    public class Fxr3EnhancedSerialization
    {
        public static Fxr3 XmlToFxr3(XDocument xml)
        {
            XmlSerializer test = new XmlSerializer(typeof(Fxr3));
            XmlReader xmlReader = xml.CreateReader();

            return (Fxr3)test.Deserialize(xmlReader);
        }
        public static XDocument Fxr3ToXml(Fxr3 fxr)
        {
            XDocument xDoc = new XDocument();

            using (var xmlWriter = xDoc.CreateWriter())
            {
                var thing = new XmlSerializer(typeof(Fxr3));
                thing.Serialize(xmlWriter, fxr);
            }

            return xDoc;
        }
    }
}