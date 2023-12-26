import UdpComms as U
import time
import json
from random import choice


class GazeEvent:
    def __init__(self, name):
        self.name = name


def rx(sock):
    data = sock.ReadReceivedData()  # read data
    # if NEW data has been received since last ReadReceivedData function call
    if data != None:
        print(data)  # print new received data


def tx(sock):
    # x = choice(["closed", "open"])
    x = choice([0, 1])
    # Send this string to other application
    sock.SendData(str(x))


if __name__ == "__main__":
    # Create UDP socket to use for sending (and receiving)
    sock = U.UdpComms(
        udpIP="127.0.0.1",
        portTX=8000,
        portRX=8001,
        enableRX=True,
        suppressWarnings=True,
    )
    while True:
        rx(sock)
        tx(sock)
        time.sleep(5)
