using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightFxServer
{
    class LightControl
    {
        //Manage colour/vel values and output to lights here
        int updatesPerSecond = 60;
        int testValue = 0;

        public LightControl()
        {
            //todo: launch background colour value processing
            var bgtask = Task.Run(UpdateValues);
        }

        public void ProcessEvent(MidiEvent eventIn)
        {          
            //Filter to NoteOn Events
            if (eventIn.EventType == (int)EventTypes.NoteOn)
            {
                //Filter to specific note nums#
                if (Program.TriggerNotes.Any(nt => nt == eventIn.NoteNumber))
                {
                    testValue = eventIn.NoteVelocity*2;
                    //Console.WriteLine($"Note {eventIn.NoteNumber} Velocity {eventIn.NoteVelocity}"); 
                }
            }

            if(eventIn.EventType == (int)EventTypes.SpecialEvent)
            {
                Console.WriteLine($"Got a {eventIn.NoteNumber} event, {eventIn.NoteVelocity} value");
            }
        }

        public async Task UpdateValues()
        {
            while (true)
            {
                if (testValue > 0)
                {
                    //Envelope control happens here
                    testValue = (int)Math.Floor((decimal)(testValue / 2));
                    Console.WriteLine(new String('=', testValue));
                }

                await Task.Delay((int)(1000 / updatesPerSecond));
            }
        }
    }

    //Note: Not strictly MIDI event bytes, but just what I've gathered through testing
    enum EventTypes
    {
        NoteOn = 144,
        NoteOff = 128,
        //ControlChange =  244? Pitch is then a factor of NV and Vel
        //Pedal = 185, HH is a factor of Vel only
        PitchBend = 244,
        HiHatPedal = 185,
        SpecialEvent = 100 //Special events controlled by non-instruments
            //NV 1 is Combo (v = 0-10), NV 2 is Multiplier&SP (V = 0-5)
    }
}
