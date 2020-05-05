using Iot.Device.BrickPi3.Sensors;
using Iot.Device.Pn532.ListPassive;
using Iot.Device.Ws28xx;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightFxServer
{
    public class LightControl
    {
        //Manage colour/vel values and output to lights here
        int updatesPerSecond = 60;
        int testValue = 0;

        float decayValue = 0.65f; //Multiplier
        float offValueCap = 0.005f;

        LedStripOutput strip;
        System.Drawing.Color currentComboColour;
        System.Drawing.Color[] stripStack = new System.Drawing.Color[83]; //de-hard code

        //Color c_EmptyComboColor = Color.FromArgb(255, 1, 1, 1); //Black
        System.Drawing.Color[] c_multiplierColours =
        {
            System.Drawing.Color.FromArgb(255, 255, 216, 50), //Yellow (0x combo)
            System.Drawing.Color.FromArgb(255, 255, 150, 0), //Orange
            System.Drawing.Color.FromArgb(255, 25, 255, 25), //Green
            System.Drawing.Color.FromArgb(255, 255, 50, 255), //Purple
            System.Drawing.Color.FromArgb(255, 50, 150, 255) //Light blue
        };

        

        //TODO: List configurable note inputs, list configurable groupings of lights as dictionary? config?
        public static int[] TriggerNotes = { 38, 48, }; //List MIDI notes to listen for here
        public static float[] HitValues = { -1f, -1f }; //-1 off
        public static Tuple<int, int>[] stripRanges = new Tuple<int, int>[2]; //test
        public static System.Drawing.Color[] c_HitColours = { 
            System.Drawing.Color.FromArgb(255, 255, 15, 0),  //R
            System.Drawing.Color.FromArgb(255, 168, 120, 0) }; //Y

        public LightControl()
        {
            
            strip = new LedStripOutput(stripStack.Length);
            strip.SetStrip(System.Drawing.Color.Empty);
            currentComboColour = c_multiplierColours[0];

            stripRanges[0] = Tuple.Create(0, 45); //R
            stripRanges[1] = Tuple.Create(46, 82); //Y

            var bgtask = Task.Run(UpdateValues);
        }        


        public void ProcessEvent(MidiEvent eventIn)
        {          
            //Filter to NoteOn Events
            if (eventIn.EventType == (int)EventTypes.NoteOn || eventIn.EventType== (int)EventTypes.HitOn && eventIn.NoteVelocity>0)
            {
                //Filter to specific note nums#
                for(int i = 0; i < TriggerNotes.Length; i++)
                {
                    if(eventIn.NoteNumber == TriggerNotes[i])
                    {
                        HitValues[i] = eventIn.NoteVelocity / 127f;
                        Console.WriteLine($"Set Hitvalues {i} to {HitValues[i]}");
                        break;
                    }

                }
            }

            //Todo: enable/disable/threshold special events?
            //Todo: rewrite most of this to feed out to a seperate array which gets consumed by Update
            if(false & eventIn.EventType == (int)EventTypes.SpecialEvent)
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
                    var SegmentLength = 7; //about 5 per segment, 6 spare at the end (0-10)

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
                
                

                //strip.SetStrip(stripStack);
            }
        }

        //Main refresh loop
        public async Task UpdateValues()
        {
            try
            {
                Console.WriteLine("Started refreshing...");
                while (true)
                {
                    for (int i = 0; i < TriggerNotes.Length; i++)
                    {
                        if (HitValues[i] >= 0)
                        {
                            if (HitValues[i] <= offValueCap)
                            {
                                HitValues[i] = -1;
                                Console.WriteLine($"HV{i} end");
                                //Force off here
                                SetStackRange(stripRanges[i], System.Drawing.Color.Empty);
                            }
                            else
                            {
                                Console.WriteLine($"HV{i} = {HitValues[i]}");
                                SetStackRange(stripRanges[i], System.Drawing.Color.FromArgb(c_HitColours[i].A,
                                    (int)(c_HitColours[i].R * HitValues[i]),
                                    (int)(c_HitColours[i].G * HitValues[i]),
                                    (int)(c_HitColours[i].B * HitValues[i])));

                                //Envelope control happens here
                                HitValues[i] = HitValues[i] * decayValue;
                            }

                        }
                    }

                    //Refresh strip
                    strip.SetStrip(stripStack);
                    await Task.Delay(1000 / updatesPerSecond);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error in refresh loop!!!");
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        void SetStackRange(Tuple<int,int> targetRange, System.Drawing.Color colourInput )
        {
            for(int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = colourInput;
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

        public void TestStrip(System.Drawing.Color[] setColours, int offset = 0)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = setColours[(offset+i) % setColours.Length];
            }
            strip.SetStrip(stripStack);
        }
    }

    //Note: Not strictly MIDI event bytes, but just what I've gathered through testing
    enum EventTypes
    {
        HitOn = 153, //2 events per hit, one with Velocity value and one with 0 Velocity to 'end' it
        NoteOn = 144, //Actually just for keys?
        NoteOff = 128,
        //ControlChange =  244? Pitch is then a factor of NV and Vel
        //Pedal = 185, HH is a factor of Vel only
        PitchBend = 244,
        HiHatPedal = 185,
        SpecialEvent = 100 //Special events controlled by non-instruments
            //NV 1 is Combo (v = 0-10), NV 2 is Multiplier&SP (V = 0-5)
    }
}
