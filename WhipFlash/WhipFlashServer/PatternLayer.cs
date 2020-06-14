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

        public int GetCurrentIndex()
        {
            //todo: calculate this only once every frame to save some cpu
            var elapsed = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMilliseconds;

            return (int)(Math.Floor((elapsed/1000f) * PatternSpeed)) % PatternColours.Length; //todo: get rid of ints, interpolate colours in patterns
        }
        //public 
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
