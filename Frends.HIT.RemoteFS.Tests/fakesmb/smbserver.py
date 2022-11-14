import os
import sys
import argparse
import logging

from impacket import smbserver, version
from impacket.ntlm import compute_lmhash, compute_nthash

if __name__ == '__main__':
    cpath = os.path.dirname(os.path.realpath(__file__))
    username = "hello-world"
    password = "h3110w0r1d"
    
    server = smbserver.SimpleSMBServer(listenAddress="0.0.0.0", listenPort=445)
    server.addShare("INTEGRATION", os.path.join(cpath, "files"), "My-Share")
    server.setSMB2Support(True)
    lmhash = compute_lmhash(password)
    nthash = compute_nthash(password)
    server.addCredential(username, 0, lmhash, nthash)
    # If empty defaults to '4141414141414141'
    server.setSMBChallenge('')
    server.setLogFile('')
    server.start()