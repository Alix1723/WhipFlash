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
        public float velocityModifier = 1.0f;                              //Multiplier from input vel to output brightness
        [XmlElement]
        public int StripRangeStart;            //LED indexes to start and end on;
        [XmlElement]
        public int StripRangeEnd;            //LED indexes to start and end on;
        [XmlArray]
        public Colour[] HitColours;       //Colour 
        [XmlElement]
        public bool ChannelIgnoresPatterns; //Skip showing patterns on this

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
            this.ChannelIgnoresPatterns = false;
            this.HitColours = new[] { Colour.Blank() };
        }

        //Single note + colour
        public LightChannel(int trigNote, float velMod, int startRange, int endRange, bool ignoresPatterns, Colour setColour)
        {
            this.ChannelTriggerNotes = new[] { trigNote };
            this.velocityModifier = velMod;
            this.StripRangeStart = startRange;
            this.StripRangeEnd = endRange;
            this.HitColours = new[] { setColour };
            this.ChannelIgnoresPatterns = ignoresPatterns;
        }

        //Many notes + colours
        public LightChannel(int[] trigNotes, float velMod, int startRange, int endRange, bool ignoresPatterns, Colour[] setColours)
        {
            this.ChannelTriggerNotes = trigNotes;
            this.StripRangeStart = startRange;
            this.StripRangeEnd = endRange;
            this.HitColours = setColours;
            this.ChannelIgnoresPatterns = ignoresPatterns;
        }

        public void SetHitValue(float val)
        {
            //Multiply here
            HitValue = val * velocityModifier;
            //Console.WriteLine($"Setting HitValue to {HitValue}");
        }

        const float floatPi = (float)(Math.PI);// * 0.5f);
        const float floatHalfPi = (float)(Math.PI / 2);// * 0.5f);

        public void DecayHitValue(float decay, CurveType decayCurveType, float delta = 1.0f)
        {
            float decayRelative = (((float)decay/1000) * delta);

            switch(decayCurveType)
            {
                case CurveType.Smooth:
                    HitValue = HitValue - (Math.Clamp((float)Math.Sin(floatPi * HitValue) * (decayRelative), 0.0001f, 1.0f));
                    break;
                case CurveType.Fast:
                    HitValue = HitValue - (Math.Clamp((float)Math.Cos(floatHalfPi * HitValue) * (decayRelative), 0.0001f, 1.0f));
                    break;
                case CurveType.Slow:
                    HitValue = HitValue - (Math.Clamp((float)Math.Sin(floatHalfPi * HitValue) * (decayRelative), 0.0001f, 1.0f));
                    break;
                case CurveType.Default:
                default:
                    HitValue = HitValue - (decayRelative);
                    //Console.WriteLine($"Debug: Subtracting {decayRelative} from {HitValue}");
                    break;
            }
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
