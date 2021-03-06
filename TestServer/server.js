console.log("Do not use in production!!!!!!!!!!!!");

const express = require("express");
const config = require("./config");
const fetch = require("node-fetch");
const bodyParser = require("body-parser");
const cors = require("cors");
const app = express();
app.use(cors());
app.options("*", cors());
const hostname = config.hostname;
const port = config.host_port;

const serverWorkCB = require("./server_work_callback");
const nodeWs = require("./websocket_node");

const nodeUrl =
  "http://" + config.node.rpc.address + ":" + config.node.rpc.port;

// Send request to the RPC server of the node and record response
const send = (json_string) => {
  return new Promise((resolve) => {
    fetch(nodeUrl, {
      method: "POST",
      body: json_string,
      headers: {
        "Content-Type": "application/json",
        Accepts: "application/json",
      },
    })
      .then((res) => res.json())
      .then((json) => {
        resolve(json);
      })
      .catch((error) => {
        resolve({ error: error });
      });
  });
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

  // The only RPC commands which are allowed. If dpow is not working, it will use the node to generate work.
  // May want to set up a GPU work server if not using dpow!!
  const allowed_actions = [
    "block_count",
    "account_info",
    "account_balance",
    "block_info",
    "pending",
    "process",
    "work_generate",
  ];

  if (allowed_actions.includes(action)) {
    // Just forward to RPC server. TODO: Should probably check for validity

    if (action == "work_generate") {
      obj.use_peers = true;
      // Don't allow users to specify the difficulty/multiplier as it can put strain on the work server
      delete obj.difficulty;
      delete obj.multiplier;
    }
    send(JSON.stringify(obj))
      .then((rpc_response_json) => {
        res.json(rpc_response_json);
      })
      .catch((e) => {
        console.log(e);
        res.json({ error: "1" });
      });
  } else {
    res.json({ error: "Not an allowed action" });
  }
});

app.listen(port, hostname, () => {
  console.log(`JSON-RPC server listening on port http://${hostname}:${port}`);
});
