using System;
using System.Xml.Serialization;

namespace WhipFlashServer
{
    [Serializable]
    public class LightChannel
    {
        [XmlArray]
        public int[] ChannelTriggerNotes;             //The MIDI note(s) to listen for
        [XmlElement]
        private float velocityModifier = 1.0f;                              //Multiplier from input vel to output brightness
        [XmlElement]
        public int StripRangeStart;            //LED indexes to start and end on;
        [XmlElement]
        public int StripRangeEnd;            //LED indexes to start and end on;
        [XmlArray]
        public Colour[] HitColours;       //Colour 

        [XmlIgnore]
        public float HitValue = -1f;                  //Brightness of the lights, updated each refresh
        [XmlIgnore]
        public int LastNoteColourIndex = 0;           //Which note was hit last (and therefore should use this colour)
        [XmlIgnore]
        public float ChannelIntensity = 0f;           //Intensity multiplier (caused by fast hits and flams)
        [XmlIgnore]
        private int lastTimeStamp = 0;                     

        public LightChannel()
        {
            this.ChannelTriggerNotes = new[] { 0 };
            this.velocityModifier = 1.0f;
            this.StripRangeStart = 0;
            this.StripRangeEnd = 0;
            this.HitColours = new[] { Colour.Blank() };
        }

        //Single note + colour
        public LightChannel(int trigNote, float velMod, int startRange, int endRange, Colour setColour)
        {
            this.ChannelTriggerNotes = new[] { trigNote };
            this.velocityModifier = velMod;
            this.StripRangeStart = startRange;
            this.StripRangeEnd = endRange;
            this.HitColours = new[] { setColour };
        }

        //Many notes + colours
        public LightChannel(int[] trigNotes, float velMod, int startRange, int endRange, Colour[] setColours)
        {
            this.ChannelTriggerNotes = trigNotes;
            this.StripRangeStart = startRange;
            this.StripRangeEnd = endRange;
            this.HitColours = setColours;
        }

        public void SetHitValue(float val)
        {
            //Multiply here
            HitValue = val * velocityModifier;
        }

        public void DecayHitValue(float decay)
        {
            HitValue = HitValue * decay;
        }

        public void DecayIntensityValue(float decay)
        {
            ChannelIntensity = Math.Clamp(ChannelIntensity - decay,0,1);
        }

        public void SetNoteIndex(int index)
        {
            LastNoteColourIndex = index;
        }

        public int GetTimeFromLastHit(int nowTimestamp)
        {
            int difference = nowTimestamp - lastTimeStamp;
            lastTimeStamp = nowTimestamp;
            return difference;
        }

        public void SetIntensity(float nintensity)
        {
            ChannelIntensity = nintensity;
        }

        public Tuple<int,int> GetStripRange()
        {
            return Tuple.Create(StripRangeStart, StripRangeEnd);
        }

        public Colour GetCurrentHitColour()
        {
            return HitColours[LastNoteColourIndex];
        }

        public override string ToString()
        {
            return $"Channnel note# {this.ChannelTriggerNotes[LastNoteColourIndex]} / HV {this.HitValue} / range {this.StripRangeStart}-{this.StripRangeEnd} / colour {this.HitColours[LastNoteColourIndex]}";
        }     
    }
}
