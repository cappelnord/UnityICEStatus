== UnityICEStatus ==

Just for fun; this behaviour queries the API used by the ICE portal if connected to the ICE WiFi of Deutsche Bahn trains. The API provides information about the current trip as well as status information (like current speed or current position). The information is polled every 5 seconds which seems to be roughly the update rate.

Unfortunately the precision of the data is not precise enough to develop games or experiences as train travel accompagnements. For example speed is derived from the trains position and will not be updated when the train does not have valid GPS informations (like in tunnels). 

I still decided to publish this MonoBehaviour for fun and exploration. Some information (like events for arriving at/departing from a station) is derived from both station status information and speed for better accuracy. In case some things can be improved upon let me know or send me a pull request.

Of course Deutsche Bahn could decide at any moment to change their API. In that case this MonoBehaviour will cease to work. 

==  Usage ==

Just drop the object onto an object in Unity. Once started it will continously try to query the API and update its public properties. The **status** and **tripInfo** property reflects the raw data of the API. All other public properties can be used for convenience. 

There are also several UnityEvents you can subscribe to.

For further information please consult the source code.

== Files ==

The C# files can be directly used on Unity objects. The StreamingAssets folder can be used to mock input when not in a train (set useTestData to true in the Start method.)