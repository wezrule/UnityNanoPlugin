const express = require("express");
const config = require("./config");
const fetch = require("node-fetch");
const bodyParser = require("body-parser");
const cors = require("cors");
const app = express();
app.use(cors());
app.options("*", cors());
const hostname = config.work_callback_hostname;
const port = config.work_callback_host_port;

const nodeWs = require("./websocket_node");

const WS = require("ws");
const ReconnectingWebSocket = require("reconnecting-websocket");
const dpow_wss = new ReconnectingWebSocket(config.dpow.ws_address, [], {
  WebSocket: WS,
  connectionTimeout: 1000,
  maxRetries: 100000,
  maxReconnectionDelay: 2000,
  minReconnectionDelay: 10, // if not set, initial connection will take a few seconds by default
});

let id = 0;
let using_dpow = false;
let dpow_request_map = new Map();
dpow_wss.onopen = () => {
  using_dpow = true;

  // dPOW sent us a message
  dpow_wss.onmessage = (msg) => {
    console.log("DPOW message");
    let data_json = JSON.parse(msg.data);
    let value_l = dpow_request_map.get(data_json.id);
    if (typeof value_l !== "undefined") {
      if (!data_json.hasOwnProperty("error")) {
        let new_response = {};
        new_response.work = data_json.work;
        new_response.hash = value_l.hash;
        value_l.res.json(new_response);
      }
    }
  };
};

app.use(bodyParser.json());

app.post("/", (req, res) => {
  res.setHeader("Content-Type", "application/json");
  let obj;
  try {
    obj = req.body;
  } catch (e) {
    res.json({ error: "malformed json request" });
    return;
  }

  let action = obj.action;
  if (action == "work_generate" && config.dpow.enabled && using_dpow) {
    console.log("DPOW work");
    // Send to the dpow server and return the same request
    ++id;
    const dpow_request = {
      user: config.dpow.user,
      api_key: config.dpow.api_key,
      hash: obj.hash,
      id: id,
    };

    let value = {};
    value.res = res;
    value.hash = obj.hash;
    dpow_request_map.set(id, value);

    // Send a request to the dpow server
    dpow_wss.send(JSON.stringify(dpow_request));
  } else {
    res.json({ error: "Work server not supported" });
  }
});

app.listen(port, hostname, () => {
  console.log(`Work JSON-RPC server listening on port http://${hostname}:${port}`);
});
