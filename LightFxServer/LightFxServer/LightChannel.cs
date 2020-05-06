using System;

namespace LightFxServer
{
    public class LightChannel
    {
        public float HitValue { get; private set; } = -1f;                                       //Brightness of the lights, updated each refresh
        public int ChannelTriggerNote { get; private set; }                //Midi value to listen for (todo: multiple?)
        private float velocityModifier = 1.0f;                              //Multiplier from input vel to output brightness
        public Tuple<int, int> StripRange { get; private set; }            //LED indexes to start and end on;
        public System.Drawing.Color HitColour { get; private set; }       //Colour 

        public LightChannel(int trigNote, float velMod, int startRange, int endRange, System.Drawing.Color setColour)
        {
            this.ChannelTriggerNote = trigNote;
            this.velocityModifier = velMod;
            this.StripRange = Tuple.Create(startRange, endRange);
            this.HitColour = setColour;
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
                Math.Clamp((int)(HitColour.A * HitValue), 0, 255),
                Math.Clamp((int)(HitColour.R * HitValue), 0, 255),
                Math.Clamp((int)(HitColour.G * HitValue), 0, 255),
                Math.Clamp((int)(HitColour.B * HitValue), 0, 255)); 
        }

        public System.Drawing.Color GetMultipliedColour(System.Drawing.Color overrideColour)
        {
            return System.Drawing.Color.FromArgb(
                Math.Clamp((int)(overrideColour.A * HitValue), 0, 255),
                Math.Clamp((int)(overrideColour.R * HitValue), 0, 255),
                Math.Clamp((int)(overrideColour.G * HitValue), 0, 255),
                Math.Clamp((int)(overrideColour.B * HitValue), 0, 255));
        }

        //todo: make serializable
    }
}
