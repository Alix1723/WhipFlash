# WhipFlash
An experiment to control RGB light performances from MIDI devices and other sources. Primarily for playing drums with rhythm games (Clone Hero).

Configurable by an XML file containing MIDI note(s) to listen for, what colour(s) to flash, and what range of LEDs to apply this to. Also has options for Star Power effects, flash decay rate, and support for MIDI keyboards with discrete note on and off messages.

Contains:

- Server which accepts TCP messages and controls RGB lights, intended for use with a Raspberry Pi or other small computer connnected to the strip of LEDs directly. 
- Clients which can be run beside or remotely from the server to feed it messages from instruments (like a MIDI keyboard or drum kit) or programs (like Clone Hero)

# Example with CH

<iframe width="560" height="315" src="https://www.youtube.com/embed/pHzCSnUrR5Q" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

The setup I use at the moment is:

Kit plugged in via USB to Raspberry Pi 3b+;
Python Script to read MIDI inputs and send them to the FX server via TCP;
FX server that takes in messages, figures out if they're triggering a note, and makes the assigned strip of LEDs flash their specific colour(s).

There's also a small program reading the colour of a few pixels from the game to track whether Star Power is active or not, and when these events happen it sends a 'special event' message to the server to toggle the blue star power effect on or off. 
