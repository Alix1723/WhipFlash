import sys, pygame, pygame.midi, socket
pygame.init()
pygame.midi.init()

TCP_IP = '192.168.1.17'#'127.0.0.1'
TCP_PORT = 5005
BUFFER_SIZE = 512

print("Connecting to ", TCP_IP, TCP_PORT) 
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect((TCP_IP, TCP_PORT))
print("Successfully connected to ", TCP_IP, TCP_PORT) 
for x in range(0, pygame.midi.get_count()):
    print("Device", x,pygame.midi.get_device_info(x))
 
inp = pygame.midi.Input(3)
print("reading device #, #",1,pygame.midi.get_device_info(1))

while True:
    
    if inp.poll():
        midi_events = inp.read(256)
        for m_e in midi_events:
            #print("event")
            message = "{},{},{},{},{},".format(m_e[0][0], m_e[0][1], m_e[0][2], m_e[0][3], m_e[1])
            s.send(message.encode('ascii'))

    # wait 10ms - this is arbitrary, but wait(0) still resulted
    # in 100% cpu utilization
    pygame.time.wait(10)

s.close()