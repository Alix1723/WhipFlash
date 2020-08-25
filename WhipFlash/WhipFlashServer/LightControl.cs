using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Transactions;
using WhipFlash;

namespace WhipFlashServer
{
    public class LightControl
    {
        //Use keys (on/off) or drums (hit)
        bool KeysMode;

        //Channels (MIDI note(s) and colour(s) to apply to a range of LEDs
        public LightChannel[] LightChannels;
        public PatternLayer[] PatternLayers;
        PatternLayer currentPatternLayer;

        //Options
        bool FastNotesDetection;
        int FastNoteTimeThreshold; //ms between notes to be considered 'fast'
        float FastNoteVelocityThreshold; //How hard to hit to be considered 'fast'

        bool FlamNotesDetection;
        int FlamNotesTimeThreshold; //ms between flam hits
        float FlamNotesVelocityThreshold; //How hard to hit to flam (and not e.g. ghost)

        int GlobalBrightnessValue; //1-255;

        //Manage colour/vel values and output to lights here
        int updatesPerSecond;

        //Vel (envelope) control
        float decayValue;
        float offValueCap = 0.002f;

        float intensityDecayRate;
        float intensityGain; 

        //Outputs
        Colour[] stripStack;
        LedStripOutput strip;

        //State information
        bool debugmode;
        
        Colour c_IntensityColour;

        public LightControl(int striplength = 0, bool isDebug = false, LightsConfiguration inputConfig = null)
        {
            debugmode = isDebug;

            if(inputConfig!=null)
            {
                SetCurrentConfig(inputConfig);
            }

            if (striplength < 1)
            {
                foreach (LightChannel lc in LightChannels)
                {
                    striplength = Math.Max(striplength, lc.StripRangeEnd + 1);
                }
            }

            stripStack = new Colour[striplength];

            if (!debugmode)
            {
                strip = new LedStripOutput(striplength);
                strip.SetStrip(Colour.Blank());
            }

            currentPatternLayer = PatternLayers[0];

            var bgtask = Task.Run(UpdateValues);
        }

        public void ProcessEvent(MidiEvent eventIn)
        {
            //Filter to note events
            if (eventIn.EventType == (int)EventTypes.NoteOn 
                || eventIn.EventType == (int)EventTypes.NoteOff 
                || eventIn.EventType == (int)EventTypes.HitOn && eventIn.NoteVelocity > 0)
            {
                //Filter to specific note nums#
                foreach (LightChannel curChannel in LightChannels)
                {
                    for (int i = 0; i < curChannel.ChannelTriggerNotes.Length; i++)
                    {
                        if (eventIn.NoteNumber == curChannel.ChannelTriggerNotes[i])
                        {
                            if (eventIn.EventType == (int)EventTypes.NoteOff)
                            {
                                curChannel.SetHitValue(0f);
                            }
                            else
                            {
                                curChannel.SetHitValue((eventIn.NoteVelocity / 127f));
                                curChannel.SetNoteIndex(i);

                                int noteDelta = curChannel.GetTimeFromLastHit(eventIn.TimeStamp);

                                //Flams and fast notes
                                if (noteDelta < FlamNotesTimeThreshold & FlamNotesDetection)
                                {
                                    if (curChannel.HitValue > FlamNotesVelocityThreshold)
                                    {
                                        curChannel.SetIntensity(1);
                                    }
                                }
                                else if (noteDelta < FastNoteTimeThreshold & FastNotesDetection)
                                {
                                    if (curChannel.HitValue > FastNoteVelocityThreshold)
                                    {
                                        curChannel.SetIntensity(Math.Min(curChannel.ChannelIntensity + (curChannel.HitValue * 0.15f), 1)); //todo: configurable
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }

            //Special events 
            if (eventIn.EventType == (int)EventTypes.SpecialEvent)
            {
                Console.WriteLine($"Got a {eventIn.NoteNumber} event, {eventIn.NoteVelocity} value");

                //Multiplier (and SP)
                if (eventIn.NoteNumber == 1 ) //"Change pattern" event
                {
                    SetPatternLayer(eventIn.NoteVelocity);
                }
                //Combo meter
                /*if (eventIn.NoteNumber == 1)
                {
                    var comboMeter = eventIn.NoteVelocity;
                }*/

                    //todo: fancy stuff like combo meters in the future
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
                    if (debugmode)
                    {
                        //ow my eyes
                        //Console.Clear();
                        //Console.WriteLine("LC debug:");
                    }

                    ClearStack();
                    currentPatternLayer.UpdateElapsedTime();

                    for (int channelIndex = 0; channelIndex < LightChannels.Length; channelIndex++)
                    {
                        LightChannel currentChannel = LightChannels[channelIndex];

                        //Apply pattern layers
                        if (currentPatternLayer.PatternLayerType != PatternType.Default)
                        {
                            if (currentPatternLayer.LayerAppliesToWholeChannels)
                            {
                                SetStackRange(currentChannel.GetStripRange(),
                                currentPatternLayer.PatternColours[channelIndex % currentPatternLayer.PatternColours.Length]);
                            }
                            else
                            {
                                if (currentPatternLayer.PatternLayerType == PatternType.Cycle)
                                {
                                    SetStackRangeInterpolatedMono(currentChannel.GetStripRange(),
                                    currentPatternLayer.PatternColours,
                                    currentPatternLayer.GetCurrentIndexFloat());
                                }
                                else
                                {
                                    SetStackRangeInterpolated(currentChannel.GetStripRange(),
                                    currentPatternLayer.PatternColours,
                                    currentPatternLayer.GetCurrentIndexFloat());
                                }
                            }

                        }

                        //Hit colours
                        if (currentChannel.HitValue >= 0)
                        {
                            if (currentChannel.HitValue <= offValueCap)
                            {
                                currentChannel.SetHitValue(-1f);
                            }

                            if (currentPatternLayer.LayerOverwritesHitColours)
                            {
                                OverlayStackRange(currentChannel.GetStripRange(),
                                    Colour.MultiplyColours(currentPatternLayer.OverwriteColour, currentChannel.HitValue));
                            }
                            else
                            {
                                //todo: overwrite and flams are still exclusive
                                OverlayStackRange(currentChannel.GetStripRange(),
                                    Colour.MultiplyColours(
                                        Colour.CrossfadeColours(
                                            currentChannel.GetCurrentHitColour(),
                                            c_IntensityColour,
                                            currentChannel.ChannelIntensity),

                                        currentChannel.HitValue));
                            }
    
                            //Envelope control happens here
                            if (!KeysMode) { currentChannel.DecayHitValue(decayValue); } 
                        }

                        if (currentChannel.ChannelIntensity > 0)
                        {
                            currentChannel.DecayIntensityValue(intensityDecayRate);
                        }

                        //debug
                        if (debugmode)
                        {
                            //Console.WriteLine(lightChannel.ToString());
                        }                      
                    }
                   
                    //Refresh strip
                    if (!debugmode)
                    {
                        if(GlobalBrightnessValue < 255)
                        {
                            //rescale 
                            for(int clr = 0; clr < stripStack.Length; clr++)
                            {
                                stripStack[clr] = Colour.MultiplyColours(stripStack[clr], (float)(GlobalBrightnessValue / 255.0f));
                            }
                        }

                        strip.SetStrip(stripStack);
                    }
                    await Task.Delay(1000 / updatesPerSecond);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in refresh loop");
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        //Set (overwrite) colour
        public void SetStackRange(Tuple<int, int> targetRange, Colour colourInput)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = colourInput;
            }
        }

        //Set (overwrite) colours
        public void SetStackRange(Tuple<int, int> targetRange, Colour[] colourInputs, int offset = 0)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = colourInputs[(k + offset) % colourInputs.Length];
            }
        }

        public void SetStackRangeInterpolated(Tuple<int, int> targetRange, Colour[] colourInputs, float offset)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                int before = ((int)Math.Floor(offset)+k) % colourInputs.Length;
                int after = ((int)Math.Ceiling(offset)+k) % colourInputs.Length;
                var interpolated = Colour.CrossfadeColours(colourInputs[before], colourInputs[after], (offset % 1));

                stripStack[k] = interpolated;
            }
        }

        public void SetStackRangeInterpolatedMono(Tuple<int, int> targetRange, Colour[] colourInputs, float offset)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                int before = ((int)Math.Floor(offset)) % colourInputs.Length;
                int after = ((int)Math.Ceiling(offset)) % colourInputs.Length;
                var interpolated = Colour.CrossfadeColours(colourInputs[before], colourInputs[after], (offset % 1));

                stripStack[k] = interpolated;
            }
        }

        //Add colours together on the stack
        public void AddStackRange(Tuple<int, int> targetRange, Colour addColourInput)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = Colour.AddColours(stripStack[k], addColourInput); //
            }
        }

        //Combine colours already on the stack
        public void OverlayStackRange(Tuple<int, int> targetRange, Colour overlayColourInput)
        {
            for (int k = targetRange.Item1; k <= targetRange.Item2; k++)
            {
                stripStack[k] = Colour.OverlayColours(stripStack[k], overlayColourInput);
            }
        }

        public void ClearStack()
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = Colour.Blank();
            }
        }

        public void ClearStack(Colour newColour)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = newColour;
            }
        }

        public void TestStrip(Colour[] setColours, int offset = 0)
        {
            for (int i = 0; i < stripStack.Length; i++)
            {
                stripStack[i] = setColours[(offset + i) % setColours.Length];
            }
            strip.SetStrip(stripStack);
        }

        public void SetPatternLayer(int patternIndex)
        {
            if (patternIndex>=0 && patternIndex < PatternLayers.Length)
            {
                currentPatternLayer = PatternLayers[patternIndex];
            }
            else
            {
                Console.WriteLine($"Can't change to pattern {patternIndex}");
            }        
        }

        public void LoadParameters(string filepath)
        {
            throw new NotImplementedException();

        }

        //Params
        public LightsConfiguration GetCurrentConfig()
        {
            LightsConfiguration outLc = new LightsConfiguration();

            outLc.KeysMode = KeysMode;
            outLc.UpdatesPerSecond = updatesPerSecond;
            outLc.DefinedChannels = LightChannels;
            outLc.FastNotesDetection = FastNotesDetection;
            outLc.FastNotesTimeThreshold = FastNoteTimeThreshold;
            outLc.FastNotesVelocityThreshold = FastNoteVelocityThreshold;
            outLc.FlamNotesDetection = FlamNotesDetection;
            outLc.FlamNotesTimeThreshold = FlamNotesTimeThreshold;
            outLc.FlamNotesVelocityThreshold = FlamNotesVelocityThreshold;
            outLc.GlobalBrightnessValue = GlobalBrightnessValue;
            outLc.HitDecayRate = decayValue;
            outLc.IntensityDecayRate = intensityDecayRate;
            outLc.IntensityGain = intensityGain;
            outLc.ColourIntensityHighlight = c_IntensityColour;
            outLc.DefinedPatternLayers = PatternLayers;

            return outLc;
        }

        private void SetCurrentConfig(LightsConfiguration conf)
        {
            this.KeysMode = conf.KeysMode;
            this.updatesPerSecond = conf.UpdatesPerSecond;
            this.LightChannels = conf.DefinedChannels;
            this.PatternLayers = conf.DefinedPatternLayers;
            this.FastNotesDetection = conf.FastNotesDetection;
            this.FastNoteTimeThreshold = conf.FastNotesTimeThreshold;
            this.FastNoteVelocityThreshold = conf.FastNotesVelocityThreshold;
            this.FlamNotesDetection = conf.FlamNotesDetection;
            this.FlamNotesTimeThreshold = conf.FlamNotesTimeThreshold;
            this.FlamNotesVelocityThreshold = conf.FlamNotesVelocityThreshold;
            this.GlobalBrightnessValue = Math.Clamp(conf.GlobalBrightnessValue, 1, 255);
            this.decayValue = conf.HitDecayRate;
            this.intensityDecayRate = conf.IntensityDecayRate;
            this.c_IntensityColour = conf.ColourIntensityHighlight;

            this.intensityGain = conf.IntensityGain;
            Console.WriteLine("Set lights config");
        }

        ~LightControl()
        {
            //Todo: autosave on close?
        }
    }

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
