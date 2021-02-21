# UnityNanoPlugin
Plugin for integrating the Nano cryptocurrency into the Unity Engine.

Tested on Windows/Linux/MacOS/Android/iOS/HTML with Unity 2019.4.20f1

Features include: functions for processing blocks, creating seeds + reading/saving them to disk in password-protected encrypted files. Generating qr codes, listening for payments to single accounts, private key payouts and many more.

### To run your own test servers (recommended for any intense testing)
Requires a nano node, npm and nodejs installed.
1. Run the nano_node after enabling rpc and websocket in `config-node.toml` file.
`nano_node --daemon`
2. `cd TestServer`
3. Modify the `config.js` settings to be applicable for your system.
4. `npm install`
5. `node server.js`

A nano node is required to be running which the websocket & http server (for RPC request) will talk with. Websocket & RPC should be enabled in the `node-config.toml` nano file. 
A http server (for RPC requests) is definitely needed for communicating with the nano node via JSON-RPC, a test node.js server is supplied for this called `server.js`. A websocket server to receive notifications from nano network is highly recommended to make the most use of the plugin functionality. Another test server called `websocket_node.js` is supplied for this, both are found in the ./TestServer directory. Running `server.js` will also run `websocket_node.js`. The websocket script makes 2 connections to the node, 1 is filtered output and 1 gets all websocket events (usual for visualisers). If you only need filtered output (recommended!) then disable `allow_listen_all=false` in `config.js`.  

`NanoUtils.cs` contains various generic functions such as creating seeds, encrypting/decrypting them using AES with a password, converting to accounts, converting between Raw and Nano and various other things.  
`NanoWebsocket.cpp` maintains the websocket connection to the proxies.
`NanoManager.cpp` is where the other functionality is located

Recommendation setups for:
#### Arcade machines
Listen to payment - Have 1 seed per arcade machine, start at first index then increment each time a payment is needed. This only checks pending blocks, don't have anything else pocketing these funds automatically. Every time a new payment is needed, move to the next index. Only 1 payment can be listening at any 1 time!  
Create a QR code for the account/amount required.  
Then listen for the payment.  
For payouts do a similar process with showing a QR Code (use the variant taking a private key), and listen for payout.  

#### Single player  
Create seed for player and store it encrypted with password (also check for local seed files if they want to open them)  
Loop through seed files.  
Save seed.  
Send and wait for confirmation.  

#### Multiplayer  
Process seed (as above)  
Create block locally and hand off to server.  
Server does validation (checks block is valid) then does appropriate action.  

Limitations
- The test servers should not be used in production due to lack of security/ddos protection. Likely canditates for a future version are the NanoRPCProxy.
- Only supports state blocks (v1)
- There is an index out of bounds error when using the Websocket with HTML (WebAssembly) so that is not currently supported, but other functionality is.

Any donation contributions are welcome: nano_15qry5fsh4mjbsgsqj148o8378cx5ext4dpnh5ex99gfck686nxkiuk9uouy
![download](https://user-images.githubusercontent.com/650038/97703969-70d90c80-1aa9-11eb-80b6-30bfad6dce31.png)