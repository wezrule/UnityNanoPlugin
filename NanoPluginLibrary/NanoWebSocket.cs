using System;
using System.Collections.Generic;

using NativeWebSocket;
using UnityEngine;

namespace NanoPlugin
{
  public delegate void OnClose();
  public delegate void OnConfirmation(WebsocketConfirmationResponseData responseData);
  public delegate void OnOpen(bool isError, bool isReconnect);
  public class NanoWebSocket : MonoBehaviour
  {
    WebSocket websocket;

    bool isListeningAll = false;
    bool isReconnection = false;

    Dictionary<string, int> registeredAccounts = new Dictionary<string, int>(); // public key hex string and number of times it was registered

    public List<OnOpen> openDelegates = new List<OnOpen>();
    public List<OnConfirmation> filteredConfirmationDelegates = new List<OnConfirmation>();
    public List<OnConfirmation> confirmationDelegates = new List<OnConfirmation>();

    public string url;

    // Start is called before the first frame update
    async void Start()
    {
      websocket = new WebSocket(url);

      websocket.OnOpen += async () =>
      {
        Debug.Log("Connection open!");

        if (isListeningAll)
        {
          ListenAll();
        }

        foreach (var registeredAccount in registeredAccounts)
        {
          AccountRegisterRequest request = new AccountRegisterRequest
          {
            account = NanoUtils.PublicKeyToAddress (registeredAccount.Key),
            action = "register_account"
          };
          await websocket.SendText(JsonUtility.ToJson(request));
        }

        foreach (var del in openDelegates)
        {
          del(false, isReconnection);
        }
      };

      websocket.OnError += (e) =>
      {
        foreach (var del in openDelegates)
        {
          del(true, isReconnection);
        }
        Debug.Log("Error! " + e);
      };

      websocket.OnClose += (e) =>
      {
        Debug.Log("Connection closed!");
      };

      websocket.OnMessage += (bytes) =>
      {
        var json = System.Text.Encoding.UTF8.GetString(bytes);

        // Confirmation
        var websocketConfirmationResponseData = JsonUtility.FromJson<WebsocketConfirmationResponseData>(json);

        if (websocketConfirmationResponseData.is_filtered)
        {
          foreach (var del in filteredConfirmationDelegates)
          {
            del(websocketConfirmationResponseData);
          }
        }
        else
        {
          foreach (var del in confirmationDelegates)
          {
            del(websocketConfirmationResponseData);
          }
        }

        Debug.Log("Received OnMessage! (" + bytes.Length + " bytes) " + json);
      };

      await websocket.Connect();
    }

    private float time = 0.0f;
    async void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
      websocket.DispatchMessageQueue();
#endif

      // Reconnect every 5 seconds if there is a disconnect
      time += Time.deltaTime;
      if (time > 5)
      {
        if (websocket.State == WebSocketState.Closed)
        {
          isReconnection = true;
          await websocket.Connect();
        }
        time = 0;
      }
    }

    public async void RegisterAccount(String address)
    {
      int count;
      var publicKey = NanoUtils.AddressToPublicKeyHexString(address);
      if (registeredAccounts.TryGetValue(publicKey, out count))
      {
        registeredAccounts[publicKey]++;
      }
      else
      {
        registeredAccounts[publicKey] = 1;
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
          // Don't send if we're not connected.
          return;
        }

        AccountRegisterRequest request = new AccountRegisterRequest();
        request.account = address;
        request.action = "register_account";
        await websocket.SendText(JsonUtility.ToJson(request));
      }
    }
    public async void UnregisterAccount(string address)
    {
      int count;
      var publicKey = NanoUtils.AddressToPublicKeyHexString(address);
      if (registeredAccounts.TryGetValue(publicKey, out count))
      {
        registeredAccounts[publicKey]--;
      }
      else
      {
        registeredAccounts.Remove(publicKey);
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
          return;
        }

        AccountRegisterRequest request = new AccountRegisterRequest();
        request.account = address;
        request.action = "unregister_account";

        await websocket.SendText(JsonUtility.ToJson(request));
      }
    }

    public async void ListenAll()
    {
      await websocket.SendText("{\"action\":\"listen_all\"}");
      isListeningAll = true;
    }

    public async void UnlistenAll()
    {
      await websocket.SendText("{\"action\":\"unlisten_all\"}");
      isListeningAll = false;
    }

    private async void OnApplicationQuit()
    {
      await websocket.Close();
    }
  }

  [Serializable]
  public class ElectionInfo
  {
    public string duration;
    public string time;
    public string tally;
    public string request_count;
    public string blocks;
    public string voters;
  }

  [Serializable]
  public class BlockWebsocket
  {
    public string type = "state";
    public string account;
    public string previous;
    public string representative;
    public string balance;
    public string link;
    public string link_as_account;
    public string signature;
    public string work;
    public string subtype;
  }

  [Serializable]
  public class Message
  {
    public string account;
    public string amount;
    public string hash;
    public string confirmation_type;
    public ElectionInfo election_info;
    public BlockWebsocket block;
  }

  [Serializable]
  public class WebsocketConfirmationResponseData
  {
    public string topic;
    public string time;
    public Message message;
    public bool is_filtered;
  }

  [Serializable]
  public class AccountRegisterRequest
  {
    public string account;
    public string action;
  }
}
