using System;
using System.Collections.Generic;
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
            return (int)(Math.Floor((DateTime.UtcNow.Ticks/10000000)*PatternSpeed)) % PatternColours.Length;
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
