using Iot.Device.BrickPi3.Sensors;
using Iot.Device.Ws28xx;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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

        LedStripOutput strip;
        System.Drawing.Color currentComboColour;
        System.Drawing.Color[] stripStack = new System.Drawing.Color[56];

        //Color c_EmptyComboColor = Color.FromArgb(255, 1, 1, 1); //Black
        System.Drawing.Color[] c_multiplierColours =
        {
            System.Drawing.Color.FromArgb(255, 255, 216, 50), //Yellow (0x combo)
            System.Drawing.Color.FromArgb(255, 255, 150, 0), //Orange
            System.Drawing.Color.FromArgb(255, 25, 255, 25), //Green
            System.Drawing.Color.FromArgb(255, 255, 50, 255), //Purple
            System.Drawing.Color.FromArgb(255, 50, 150, 255) //Light blue
        };

        public LightControl()
        {
            //todo: launch background colour value processing
            var bgtask = Task.Run(UpdateValues);
            strip = new LedStripOutput(56);
            strip.SetStrip(System.Drawing.Color.Empty);
            currentComboColour = c_multiplierColours[0];
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

                //Multiplier (and SP)
                if (eventIn.NoteNumber == 2)
                {                    
                    currentComboColour = c_multiplierColours[eventIn.NoteVelocity];
                    UpdateStackValues(currentComboColour);
                    Console.WriteLine($"Setting multiplier colour to: {eventIn.NoteVelocity} ({currentComboColour})");
                }
                //Combo meter
                //Todo: also need to update for SP change
                if (eventIn.NoteNumber == 1)
                {
                    ClearStack();
                    var comboMeter = eventIn.NoteVelocity;
                    var SegmentLength = 5; //about 5 per segment, 6 spare at the end (0-10)

                    if (comboMeter == 0) 
                    { 
                        for(int i = 0; i < stripStack.Length; i++) 
                        { 
                            stripStack[i] = System.Drawing.Color.Empty; 
                        }
                    }
                    else
                    {

                        for (int i = 1; i < 11; i++)
                        { //Combo goes 0 - 10 (11 values)
                            for (int j = 0; j < SegmentLength; j++)
                            {
                                stripStack[j + ((i - 1) * SegmentLength)] = (comboMeter >= i) ? currentComboColour : System.Drawing.Color.Empty;
                            }
                        }
                    }

                    
                }       
                
                

                strip.SetStrip(stripStack);
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

        void ClearStack()
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = System.Drawing.Color.Transparent;
            }
        }

        void UpdateStackValues(System.Drawing.Color newColour)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                if (stripStack[i].A > 0)
                {
                    stripStack[i] = newColour;
                }
            }

            strip.SetStrip(stripStack);
        }

        public void TestStrip(System.Drawing.Color setColour)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = setColour;
            }
            strip.SetStrip(stripStack);
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
