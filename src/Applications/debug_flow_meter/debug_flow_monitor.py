import socket

MSGLEN = 10

class MySocket:
    """demonstration class only
      - coded for clarity, not efficiency
    """

    def __init__(self, sock=None):
        if sock is None:
            self.sock = socket.socket(
                            socket.AF_INET, socket.SOCK_STREAM)
        else:
            self.sock = sock

    def connect(self, host, port):
        self.sock.connect((host, port))

    def mysend(self, msg):
        totalsent = 0
        while totalsent < MSGLEN:
            sent = self.sock.send(msg[totalsent:])
            if sent == 0:
		raise RuntimeError("socket connection broken")
            totalsent = totalsent + sent

    def myreceive(self):
        chunks = []
        while True:
            chunk = self.sock.recv(1)
            if chunk == b'':
                raise RuntimeError("socket connection broken")
            elif (chunk == b','):
                break
            elif (chunk == b'\n'):
                continue
            else:
                chunks.append(chunk)
        return b''.join(chunks)

s = MySocket()
s.connect("192.168.1.143", 80)
while(True):
        print(s.myreceive())
