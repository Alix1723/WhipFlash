using System;
using System.Collections.Generic;
using System.Text;

namespace LightFxServer
{
    public class MidiEvent
    {
        public int EventType;
        public int NoteNumber;
        public int NoteVelocity;
        public int DataValue3;
        public int TimeStamp;

        public MidiEvent() { }

        public MidiEvent(int type, int number, int velocity, int value, int timestamp)
        {
            this.EventType = type;
            this.NoteNumber = number;
            this.NoteVelocity = velocity;
            this.DataValue3 = value;
            this.TimeStamp = timestamp;
        }

        public MidiEvent(string type, string number, string velocity, string value, string timestamp)
        {
            //Format: "eventtype, note, velocity, data3, 
            int.TryParse(type, out this.EventType);
            int.TryParse(number, out this.NoteNumber);
            int.TryParse(velocity, out this.NoteVelocity);
            int.TryParse(value, out this.DataValue3);
            int.TryParse(timestamp, out this.TimeStamp);
        }

        public override string ToString()
        {
            return $"{EventType},{NoteNumber},{NoteVelocity},{DataValue3},{TimeStamp}";

        }
    }
}
