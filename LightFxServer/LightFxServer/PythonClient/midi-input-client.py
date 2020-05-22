import sys, pygame, pygame.midi, socket
pygame.init()
pygame.midi.init()

#Remixed from https://wiki.python.org/moin/TcpCommunication and other pygame midi examples

#args: deviceID, IP, port

inputSelection = -1

TCP_IP = '127.0.0.1'
TCP_PORT = 5005
BUFFER_SIZE = 256

if(len(sys.argv) > 1):
	inputSelection = int(sys.argv[1])
	if(len(sys.argv) > 2):
		TCP_IP = str(sys.argv[2])
		TCP_PORT = int(sys.argv[3])

print("Connecting to ", TCP_IP, TCP_PORT) 
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect((TCP_IP, TCP_PORT))
print("Successfully connected to ", TCP_IP, TCP_PORT) 

if(inputSelection == -1):
    for x in range(0, pygame.midi.get_count()):
	    print("Device", x,pygame.midi.get_device_info(x))
        
    inputSelection = int(input())

inp = pygame.midi.Input(inputSelection)
print("reading device #, #",inputSelection,pygame.midi.get_device_info(inputSelection))

while True:
    
    if inp.poll():
        midi_events = inp.read(BUFFER_SIZE)
        for m_e in midi_events:
            print("event")
            message = "{},{},{},{},{},".format(m_e[0][0], m_e[0][1], m_e[0][2], m_e[0][3], m_e[1])
            print(message)
            s.send(message.encode('ascii'))
    pygame.time.wait(10)

s.close()