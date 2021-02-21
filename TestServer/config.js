let config = {};

// If you don't need all websocket confirmations, then set this to false!
config.allow_listen_all = false;

// The host and port which calls from Unity plugin clients will call to access RPC commands indirectly and other things.
// In the test level it uses a publically available server for simple testing, do not abuse it or you will be IP banned.
// In this file it is configured to use localhost, update it to your remote server IP when launched there.
config.hostname = "127.0.0.1";
config.host_port = 28103;
config.websocket = {};
config.websocket_hostname = "127.0.0.1";
config.websocket.host_port = 28104;
config.work_callback_hostname = "127.0.0.1";
config.work_callback_host_port = 28105;

// These are settings which are needed to communicate with the Nano node.
config.node = {};
// Websocket address + port of the node. See config-node.toml config settings
config.node.ws_address = "ws://[::1]:7078"; // port 57000 for beta

// The RPC server settings for communicating with the node. See "port" in config-rpc.toml of the Nano node
config.node.rpc = {};
config.node.rpc.address = "[::1]";
config.node.rpc.port = 7076; // port 55000 for beta

// DPOW credentials if you have them.
// Otherwise work_generate will use the node, this will take a while if using the CPU (especially on VPS).
// Use a GPU with the node or delegate to a work server with a GPU (see nano_work_server).
config.dpow = {};
config.dpow.enabled = false;
config.dpow.user = "<your_username>";
config.dpow.api_key = "<your_api_key>";
config.dpow.ws_address = "wss://dpow.nanocenter.org/service_ws/";

module.exports = config;