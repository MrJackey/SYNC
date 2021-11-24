# Sync

Sync is a network library for Unity built upon [RevenantX/LiteNetLib](https://github.com/RevenantX/LiteNetLib).

This library was originally built as my master thesis at Yrgo as a Game Creator Programmer.

## How to Use
Sync is designed to be similiar to Unity. It provides a number of components as well as an API for when writing scripts.

### SYNC Class
The static SYNC class is where you will find most of the tools. It has methods to perform network 
actions such as connecting and disconnecting, events for network related changes and properties 
depicting the network state. 

It it also where you can find the common Unity functions such as Instantiate and Destroy 
modified to work with Sync.

### SYNC Identity
The SYNC Identity component is required on each object synchronized between instances. It keeps 
track of the object over the network when using any of the other components provided by Sync. It is 
also where you setup if an object should be controlled by the server or a client.

### SYNC Transform
The SYNC Transform component syncs the object's transform between instances. It contains several 
options to customize how it functions to best fit your game. The component also has support for 
interpolation and extrapolation if you wish to use it.

### SYNC Animator
The SYNC Animator component allow you to sync the animations on an object. It does this by keeping 
track and synchronizing the parameters of the animator. 

Note: The SYNC animator does not synchronize any trigger parameters. Try to use booleans instead.

### SYNCBehaviour
SYNCBehaviour is a MonoBehaviour class that you can inherit from to gain further functionality 
from Sync. Scripts inheriting from SYNCBehaviour are able to execute remote procedure calls 
on both servers and clients. This is done by using the following methods:

```c#
void InvokeServer(string methodName, params object[] args);

void InvokeClient(int clientID, string methodName, params object[] args);

void InvokeClients(string methodName, params object[] args);
```

### Assigning Authority
Objects which have a SYNC Identity component have the option to be controlled by either the server 
or a client. To assign client authority over an object, simply select Client in the dropdown. 
The client who should receive the authority must then instantiate it themselves.

Note: If an object with client authority is instantiated on a server which is not hosted 
on a client, the server will take authority over the object.
