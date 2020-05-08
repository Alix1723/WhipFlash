using System;

namespace LightFxServer
{
    public class LightChannel
    {
        public float HitValue { get; private set; } = -1f;                  //Brightness of the lights, updated each refresh

        public int[] ChannelTriggerNotes { get; private set; }             //The MIDI note(s) to listen for
              
        private float velocityModifier = 1.0f;                              //Multiplier from input vel to output brightness
        public Tuple<int, int> StripRange { get; private set; }            //LED indexes to start and end on;
        public System.Drawing.Color[] HitColours { get; private set; }       //Colour 

        public int LastNoteColourIndex { get; private set; } = 0;           //Which note was hit last (and therefore should use this colour)

        //Single note + colour
        public LightChannel(int trigNote, float velMod, int startRange, int endRange, System.Drawing.Color setColour, int overrideMidiNote = 0)
        {
            this.ChannelTriggerNotes[0] = trigNote;
            this.velocityModifier = velMod;
            this.StripRange = Tuple.Create(startRange, endRange);
            this.HitColours[0] = setColour;
        }

        //Many notes + colours
        public LightChannel(int[] trigNotes, float velMod, int startRange, int endRange, System.Drawing.Color[] setColours, int overrideMidiNote = 0)
        {
            this.ChannelTriggerNotes = trigNotes;
            this.velocityModifier = velMod;
            this.StripRange = Tuple.Create(startRange, endRange);
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

        public System.Drawing.Color GetMultipliedColour()
        {
            return System.Drawing.Color.FromArgb(
                Math.Clamp((int)(HitColours[LastNoteColourIndex].A * HitValue), 0, 255),
                Math.Clamp((int)(HitColours[LastNoteColourIndex].R * HitValue), 0, 255),
                Math.Clamp((int)(HitColours[LastNoteColourIndex].G * HitValue), 0, 255),
                Math.Clamp((int)(HitColours[LastNoteColourIndex].B * HitValue), 0, 255)); 
        }

        public System.Drawing.Color GetMultipliedColour(System.Drawing.Color overrideColour)
        {
            return System.Drawing.Color.FromArgb(
                Math.Clamp((int)(overrideColour.A * HitValue), 0, 255),
                Math.Clamp((int)(overrideColour.R * HitValue), 0, 255),
                Math.Clamp((int)(overrideColour.G * HitValue), 0, 255),
                Math.Clamp((int)(overrideColour.B * HitValue), 0, 255));
        }

        public void SetNoteIndex(int index)
        {
            LastNoteColourIndex = index;
        }
    }
}
