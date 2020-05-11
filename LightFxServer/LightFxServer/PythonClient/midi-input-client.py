import sys, pygame, pygame.midi, socket
pygame.init()
pygame.midi.init()

inputSelection = -1

TCP_IP = '127.0.0.1'
TCP_PORT = 5005
BUFFER_SIZE = 512

if(len(sys.argv) > 1):
	inputSelection = int(sys.argv[1])
	if(len(sys.argv) > 2):
		TCP_IP = str(sys.argv[2])
		TCP_PORT = int(sys.argv[3])

print("Connecting to ", TCP_IP, TCP_PORT) 
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect((TCP_IP, TCP_PORT))
print("Successfully connected to ", TCP_IP, TCP_PORT) 

if(inputSelection = -1):
	for x in range(0, pygame.midi.get_count()):
	    print("Device", x,pygame.midi.get_device_info(x))
 
	inputSelection = int(input())

inp = pygame.midi.Input(inputSelection) #Todo: configurable
print("reading device #, #",chosenvalue,pygame.midi.get_device_info(chosenvalue))

while True:
    
    if inp.poll():
        midi_events = inp.read(256)
        for m_e in midi_events:
            print("event")
            message = "{},{},{},{},{},".format(m_e[0][0], m_e[0][1], m_e[0][2], m_e[0][3], m_e[1])
            print(message)
            s.send(message.encode('ascii'))

    # wait 10ms - this is arbitrary, but wait(0) still resulted
    # in 100% cpu utilization
    pygame.time.wait(10)

s.close()