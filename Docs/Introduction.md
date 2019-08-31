# Introduction

The Net Transport package contains 3 separate packages:
 - ```ClassDev.Networking.Transport.LowLevel``` - The lowest level system, directly linked to the UdpClient, used for sending/receiving and encoding messages.
 - ```ClassDev.Networking.Transport``` - The middle level system, used for connecting, disconnecting, sending reliable/unreliable messages and handling received messages. *(Depends on ClassDev.Networking.Transport.LowLevel)*
 - ```ClassDev.Networking``` - The very top level system, used for server/client management, NAT Punchthrough, dynamic migration etc. *(Depends on ClassDev.Networking.Transport)*

 The packages can be utilized separately, assuming that you comply with their dependency. For example, you can build your own top level for server/client management using the ```ClassDev.Networking.Transport```, but at the moment you cannot utilize the ```ClassDev.Networking``` package without having ```ClassDev.Networking.Transport```.

All the public available properties/methods are documented accordingly. However, if you are interested in the private properties/methods and how the classes work specifically, please refer to the summary above each method in the source code.

Lets get started!
