using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Serialization;
using WhipFlashServer;

namespace WhipFlash
{
    [Serializable]
    public class PatternLayer
    {
        [XmlElement]
        public int PatternTriggerNote; //MIDI note to listen for

        [XmlElement]
        public Colour[] PatternColours; //Colours that comprise the pattern

        [XmlElement]
        public PatternType PatternLayerType; //What kind of pattern    

        [XmlElement]
        public bool LayerOverwritesHitColours; //Flash this colour instead of regular    

        [XmlElement]
        public Colour OverwriteColour;

        [XmlElement]
        public bool LayerAppliesToWholeChannels; //Use pattern colours as a channel's entire colour

        [XmlElement]
        public float PatternSpeed; //Speed in (index)/sec


        private double elapsedTime;

        public int GetCurrentIndex()
        {           
            if(PatternLayerType == PatternType.ScrollBackward)
            {
                return PatternColours.Length - (int)(Math.Floor((elapsedTime / 1000f) * PatternSpeed)) % PatternColours.Length; //todo: get rid of ints, interpolate colours in patterns
            }

            return (int)(Math.Floor((elapsedTime / 1000f) * PatternSpeed)) % PatternColours.Length; //todo: get rid of ints, interpolate colours in patterns
        }

        public float GetCurrentIndexFloat()
        {
            if (PatternLayerType == PatternType.ScrollBackward)
            {
                return PatternColours.Length - (((float)elapsedTime / 1000f) * PatternSpeed) % PatternColours.Length; 
            }

            return (((float)elapsedTime / 1000f) * PatternSpeed) % PatternColours.Length; 
        }
        public void UpdateElapsedTime()
        {
            //Only call once per frame
            elapsedTime = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMilliseconds;
        }

    }

    [Serializable]
    public enum PatternType
    {
        Default = 0, //Just colours
        ScrollForward, //Loop forward
        ScrollBackward, // Loop backwards
        Cycle, //All lights cycle colours
    }
}
