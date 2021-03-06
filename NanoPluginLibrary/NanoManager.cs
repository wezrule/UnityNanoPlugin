using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NanoPlugin
{
  public class PendingBlock
  {
    public NanoAmount amount;
    public string source;
  };

  public enum ConfType
  {
    SendTo,
    SendFrom,
    Receive
  }

  public class ListeningPayment
  {
    public string address;
    public NanoAmount amount;
    public int watcherId;
    public bool exactAmount;
    public Action<bool> callback;
  }

  public class ListeningPayout
  {
    public float expiryTime;
    public DateTime timerStart;
    public string address;
    public int watcherId;
    public Action<bool> callback;
  }

  public class WatcherInfo
  {
    public WatcherInfo(NanoAmount amount, ConfType confType, string hash)
    {
      this.amount = amount;
      this.confType = confType;
      this.hash = hash;
    }

    public NanoAmount amount;
    public ConfType confType;
    public string hash;
  };

  public class NanoManager : MonoBehaviour
  {
    private float timeUpdate = 0.0f;
    private int watcherId = 0;

    private ListeningPayment listeningPayment = new ListeningPayment();
    private ListeningPayout listeningPayout = new ListeningPayout();

    //  private delegate void OnCloseDelegate();

    public void AddOnWebsocketConnectListener(OnOpen callback)
    {
      websocket.openDelegates.Add(callback);
    }

    public void AddConfirmationListener(OnConfirmation callback)
    {
      websocket.confirmationDelegates.Add(callback);
    }

    public void AddFilteredConfirmationListener(OnConfirmation callback)
    {
      websocket.filteredConfirmationDelegates.Add(callback);
    }

    public void ListenAllConfirmations()
    {
      websocket.ListenAll();
    }

    public void UnlistenAllConfirmations()
    {
      websocket.UnlistenAll();
    }

    public void ListenForPaymentWaitConfirmation(string address, NanoAmount amount, bool exactAmount, Action<bool> callback)
    {
      if (listeningPayment.callback != null)
      {
        Unwatch(listeningPayment.address, listeningPayment.watcherId);
      }

      listeningPayment.address = address;
      listeningPayment.amount = amount;
      listeningPayment.callback = callback;
      listeningPayment.watcherId = Watch(address, (watcherInfo) => { });
      listeningPayment.exactAmount = exactAmount;
    }

    public void CancelPayment(string address)
    {
      Unwatch(address, listeningPayment.watcherId);
      listeningPayment.callback = null;
    }

    public void ListenForPayoutWaitConfirmation(string address, int expiryTime, Action<bool> callback)
    {
      if (listeningPayout.callback != null)
      {
        Unwatch(listeningPayout.address, listeningPayout.watcherId);
      }

      listeningPayout.address = address;
      listeningPayout.expiryTime = expiryTime;
      listeningPayout.timerStart = DateTime.UtcNow;
      listeningPayout.callback = callback;
      listeningPayout.watcherId = Watch(address, (watcherInfo) => { });
    }

    public void CancelPayout(string address)
    {
      Unwatch(address, listeningPayout.watcherId);
      listeningPayout.callback = null;
    }

    private Dictionary<string, Dictionary<int, Action<WatcherInfo>>> watchers = new Dictionary<string, Dictionary<int, Action<WatcherInfo>>>();

    // Returns an id which must be used if wanting to unwatch an account
    public int Watch(string address, Action<WatcherInfo> callback)
    {
      websocket.RegisterAccount(address);
      Dictionary<int, Action<WatcherInfo>> val;
      if (watchers.TryGetValue(address, out val))
      {
        val[watcherId] = callback;
      }
      else
      {
        var dict = new Dictionary<int, Action<WatcherInfo>>();
        dict[watcherId] = callback;
        watchers.Add(address, dict);
      }

      return watcherId++;
    }

    public void Unwatch(string address, int id)
    {
      Dictionary<int, Action<WatcherInfo>> val;
      if (watchers.TryGetValue(address, out val))
      {
        Action<WatcherInfo> callback;
        if (val.TryGetValue(id, out callback))
        {
          val.Remove(id);
          if (val.Count == 0)
          {
            watchers.Remove(address);
          }
          websocket.UnregisterAccount(address);
        }
      }
    }

    // Only call this once...
    public void SetupFilteredConfirmationMessageWebsocketListener()
    {
      if (websocket != null)
      {
        AddFilteredConfirmationListener((websocketConfirmationResponseData) =>
        {
          // Need to determine if it's:
          // 1 - Send to an account we are watching (we need to check pending, and fire off a loop to get these blocks (highest amount
          // first), if above a certain amount)
          // 2 - Send from an account we are watching (our balance goes down, check account_info)
          // 3 - Receive (pocket) an account we are watching (no need to check pending, but need to check balance. But this could be an old
          // block, so need to check account_info balance)

          // We could be monitoring multiple accounts which may be interacting with each other so need to check all

          if (websocketConfirmationResponseData.message.block.subtype.Equals(NanoUtils.GetBlockTypeStr(BlockType.send)))
          {
            var linkAsAccount = websocketConfirmationResponseData.message.block.link_as_account;
            if (watchers.ContainsKey(linkAsAccount))
            {
              // Invoke callbacks
              foreach (var val in watchers[linkAsAccount])
              {
                val.Value(new WatcherInfo(new NanoAmount(websocketConfirmationResponseData.message.amount), ConfType.SendTo, websocketConfirmationResponseData.message.hash));
              }
            }

            var address = websocketConfirmationResponseData.message.block.account;
            if (watchers.ContainsKey(address))
            {
              // Invoke callbacks
              foreach (var val in watchers[address])
              {
                val.Value(new WatcherInfo(new NanoAmount(websocketConfirmationResponseData.message.amount), ConfType.SendFrom, websocketConfirmationResponseData.message.hash));
              }
            }

            // Check for payment
            if (listeningPayment.callback != null)
            {
              if (listeningPayment.address.Equals(linkAsAccount) && listeningPayment.amount.Equals(new NanoAmount(websocketConfirmationResponseData.message.amount)) || (!listeningPayment.exactAmount && listeningPayment.amount > new NanoAmount(websocketConfirmationResponseData.message.amount)))
              {
                Unwatch(listeningPayment.address, listeningPayment.watcherId);
                listeningPayment.callback(false);
                listeningPayment.callback = null;
              }
            }

            // Check for payout
            if (listeningPayout.callback != null)
            {
              if (listeningPayout.address.Equals(address) && websocketConfirmationResponseData.message.block.balance.Equals("0"))
              {
                Unwatch(listeningPayout.address, listeningPayout.watcherId);
                listeningPayout.callback(false);
                listeningPayout.callback = null;
              }
            }
          }
          else if ((websocketConfirmationResponseData.message.block.subtype.Equals(NanoUtils.GetBlockTypeStr(BlockType.receive))) || (websocketConfirmationResponseData.message.block.subtype.Equals(NanoUtils.GetBlockTypeStr(BlockType.open))))
          {
            var account = websocketConfirmationResponseData.message.account;
            if (watchers.ContainsKey(account))
            {
              // Invoke callbacks
              foreach (var val in watchers[account])
              {
                val.Value(new WatcherInfo(new NanoAmount(websocketConfirmationResponseData.message.amount), ConfType.Receive, websocketConfirmationResponseData.message.hash));
              }
            }
          }

          if (blockListener.ContainsKey(websocketConfirmationResponseData.message.hash))
          {
            blockListener[websocketConfirmationResponseData.message.hash](false, websocketConfirmationResponseData.message.hash);
            blockListener.Remove(websocketConfirmationResponseData.message.hash);
          }
        });
      }
      else
      {
        Debug.Log("Websocket must be initialised before calling SetupConfirmationListener");
      }
    }

    public IEnumerator BlockConfirmed(string hash, Action<bool> callback)
    {
      yield return rpc.BlockInfo(hash, (response) =>
      {
        var confirmed = JsonUtility.FromJson<BlockInfoResponse>(response).confirmed;
        if (confirmed == null)
        {
          callback(false);
        }
        else
        {
          callback(Boolean.Parse(confirmed));
        }
      });
    }

    public IEnumerator WorkGenerate(string address, string previous, Action<string> callback)
    {
      var hashForWork = previous == null ? NanoUtils.AddressToPublicKeyHexString(address) : previous;
      yield return rpc.WorkGenerate(hashForWork, (response) =>
      {
        callback(JsonUtility.FromJson<WorkGenerateResponse>(response).work);
      });
    }

    public IEnumerator AccountFrontier(string address, Action<string> callback)
    {
      yield return rpc.AccountInfo(address, (response) =>
      {
        callback(JsonUtility.FromJson<AccountInfoResponse>(response).frontier);
      });
    }

    public IEnumerator Balance(string address, Action<NanoAmount, NanoAmount> callback)
    {
      yield return rpc.AccountBalance(address, (response) =>
      {
        var accountBalanceResponse = JsonUtility.FromJson<AccountBalanceResponse>(response);
        if (accountBalanceResponse != null && accountBalanceResponse.balance != null)
        {
          callback(new NanoAmount(System.Numerics.BigInteger.Parse(accountBalanceResponse.balance)), new NanoAmount(System.Numerics.BigInteger.Parse(accountBalanceResponse.pending)));
        }
        else
        {
          callback(null, null);
        }
      });
    }

    public IEnumerator AccountInfo(string address, Action<AccountInfoResponse> callback)
    {
      yield return rpc.AccountInfo(address, (response) =>
      {
        callback(JsonUtility.FromJson<AccountInfoResponse>(response));
      });
    }

    public IEnumerator PendingBlocks(string address, Action<List<PendingBlock>> callback)
    {
      List<PendingBlock> pendingBlocks = new List<PendingBlock>();
      yield return rpc.PendingBlocks(address, (responsePending) =>
      {
        if (responsePending != null)
        {
          var json = JSON.Parse(responsePending);
          foreach (System.Collections.Generic.KeyValuePair<string, JSONNode> kvp in json["blocks"])
          {
            PendingBlock pendingBlock = new PendingBlock();
            pendingBlock.source = kvp.Key;
            pendingBlock.amount = new NanoAmount((string)kvp.Value);
            pendingBlocks.Add(pendingBlock);
          }
        }
      });

      callback(pendingBlocks);
    }

    public Block CreateBlock(string address, byte[] privateKey, NanoAmount balance, string link, string previous, string rep, string work)
    {
      Block block = new Block();
      block.account = address;
      block.balance = balance.ToString();
      block.link = link;
      block.previous = previous == null ? "0000000000000000000000000000000000000000000000000000000000000000" : previous;
      block.representative = rep;
      // Sign the block
      var hash = NanoUtils.HashStateBlock(address, block.previous, balance.ToString(), rep, link);
      var signature = NanoUtils.SignHash(hash, privateKey);
      block.signature = signature;
      block.work = work;
      return block;
    }

    public IEnumerator Process(Block block, BlockType blockType, Action<string> callback)
    {
      yield return rpc.Process(block, NanoUtils.GetBlockTypeStr(blockType), (response) =>
      {
        callback(JsonUtility.FromJson<ProcessResponse>(response).hash);
      });
    }

    public IEnumerator Send(string toAddress, NanoAmount amount, string privateKey, string work, Action<bool, string> callback)
    {
      // First we get the frontier
      NanoAmount currentBalance = null;
      string previous = null;
      string rep = defaultRep;
      string fromAddress = NanoUtils.PrivateKeyToAddress(privateKey);
      yield return AccountInfo(fromAddress, (accountInfo) =>
      {
        currentBalance = new NanoAmount(accountInfo.balance);
        previous = accountInfo.frontier;
        rep = accountInfo.representative;
      });

      if (previous != null)
      {
        if (String.IsNullOrEmpty(work))
        {
          // Generate the work
          yield return WorkGenerate(fromAddress, previous, (workResponse) =>
          {
            work = workResponse;
          });
        }

        if (!String.IsNullOrEmpty(work))
        {
          // Create the block to send
          var newBalance = currentBalance - amount;
          var block = CreateBlock(fromAddress, NanoUtils.HexStringToByteArray(privateKey), newBalance, NanoUtils.AddressToPublicKeyHexString(toAddress), previous, rep, work);
          yield return Process(block, BlockType.send, (hash) =>
          {
            if (hash != null)
            {
              callback(false, hash);
            }
            else
            {
              callback(true, "");
            }
            Debug.Log(hash);
          });
        }
        else
        {
          callback(true, "");
          Debug.Log("Invalid work");
        }
      }
      else
      {
        callback(true, "");
        Debug.Log("No account exists to send from");
      }
    }

    public IEnumerator Send(string toAddress, NanoAmount amount, string privateKey, Action<bool, string> callback)
    {
      yield return Send(toAddress, amount, privateKey, null, callback);
    }

    public IEnumerator Receive(string address, PendingBlock pendingBlock, string privateKey, string work, Action<bool, string> callback)
    {
      // First we get the frontier
      NanoAmount currentBalance = null;
      string previous = null;
      var rep = defaultRep;

      yield return AccountInfo(address, (accountInfo) =>
      {
        currentBalance = new NanoAmount(accountInfo.balance);
        previous = accountInfo.frontier;
        if (previous != null)
        {
          rep = accountInfo.representative;
        }
      });

      if (String.IsNullOrEmpty(work))
      {
        // Generate the work
        yield return WorkGenerate(address, previous, (workResponse) =>
        {
          work = workResponse;
        });
      }

      if (!String.IsNullOrEmpty(work))
      {
        // Create the block to receive
        var newBalance = currentBalance + pendingBlock.amount;
        var block = CreateBlock(address, NanoUtils.HexStringToByteArray(privateKey), newBalance, pendingBlock.source, previous, rep, work);
        yield return Process(block, previous == null ? BlockType.open : BlockType.receive, (hash) =>
        {
          if (hash != null)
          {
            callback(false, hash);
          }
          else
          {
            callback(true, "");
          }
          Debug.Log(hash);
        });
      }
      else
      {
        callback(true, "");
      }
    }

    public IEnumerator Receive(string address, PendingBlock pendingBlock, string privateKey, Action<bool, string> callback)
    {
      yield return Receive(address, pendingBlock, privateKey, String.Empty, callback);
    }

    class KeyCallback
    {
      public KeyCallback(string privateKey, Action<Block> callback)
      {
        this.privateKey = privateKey;
        this.callback = callback;
      }

      public string privateKey;
      public Action<Block> callback;
    };

    private Dictionary<string, Action<bool, string>> blockListener = new Dictionary<string, Action<bool, string>>();
    private Dictionary<string, KeyCallback> keyListeners = new Dictionary<string, KeyCallback>(); // For automatic pocketing

    public IEnumerator SendWaitConf(string toAddress, NanoAmount amount, string privateKey, Action<bool, string> callback)
    {
      yield return Send(toAddress, amount, privateKey, (error, hash) =>
      {
        if (error)
        {
          callback(error, hash);
        }
        else
        {
          // Register and save callback
          blockListener[hash] = callback;
        }
      });
    }

    public IEnumerator ReceiveWaitConf(string address, PendingBlock pendingBlock, string privateKey, Action<bool, string> callback)
    {
      yield return Receive(address, pendingBlock, privateKey, (error, hash) =>
      {
        if (error)
        {
          callback(error, hash);
        }
        else
        {
          // Register and save callback
          blockListener[hash] = callback;
        }
      });
    }

    public void AutomatePocketing(string address, string privateKey, Action<Block> callback)
    {
      keyListeners[address] = new KeyCallback(privateKey, callback);
    }

    public void UnautomatePocketing(string address)
    {
      keyListeners.Remove(address);
    }

    void Start()
    {
      rpc = new NanoPlugin.RPC(rpcURL);
    }

    void Update()
    {
      // Check registered blocks we are listening for
      timeUpdate += Time.deltaTime;
      if (timeUpdate > 2)
      {
        // Check all blocks we are listening for
        foreach (var block in blockListener)
        {
          // Check if block is confirmed
          StartCoroutine(BlockConfirmedHandler(block.Key, block.Value));
        }

        // Check for pending blocks for account we are automatically pocketing
        foreach (var key in keyListeners)
        {
          StartCoroutine(AutomateHandler(key.Key, key.Value));
        }

        // Just setting websocket
        StartCoroutine(CheckPaymentPayoutHandler());
        timeUpdate = 0;
      }
    }

    IEnumerator BlockConfirmedHandler(string hash, Action<bool, string> callback)
    {
      yield return BlockConfirmed(hash, (confirmed) =>
      {
        // Check so that we don't call the callback twice
        if (confirmed && blockListener.ContainsKey(hash))
        {
          callback(false, hash);
          blockListener.Remove(hash);
        }
      }
     );
    }

    IEnumerator AutomateHandler(string address, KeyCallback keyCallback)
    {
      // Check pending blocks for this account
      List<PendingBlock> pendingBlocks = null;
      yield return PendingBlocks(address, (pendingBlocksResponse) =>
      {
        pendingBlocks = pendingBlocksResponse;
      });

      if (pendingBlocks != null && pendingBlocks.Count > 0)
      {
        var pendingBlock = pendingBlocks[0];
        string hash = null;
        yield return ReceiveWaitConf(address, pendingBlock, keyCallback.privateKey, (error, hashResponse) =>
        {
          hash = hashResponse;
        });

        if (hash != null)
        {
          yield return rpc.BlockInfo(hash, (response) =>
          {
            var block = JsonUtility.FromJson<BlockInfoResponse>(response).contents;
            keyCallback.callback(block);
          });
        }
      }
    }

    IEnumerator CheckPaymentPayoutHandler()
    {
      // Payment listener
      if (listeningPayment.callback != null)
      {
        // Find a pending block with greater than this amount
        List<PendingBlock> pendingBlocks = null;
        yield return PendingBlocks(listeningPayment.address, (pendingBlocksResponse) =>
        {
          pendingBlocks = pendingBlocksResponse;
        });

        if (pendingBlocks != null && pendingBlocks.Count > 0)
        {
          // Check if any pending blocks meet the requirements
          foreach (var pendingBlock in pendingBlocks)
          {
            if (pendingBlock.amount.Equals(listeningPayment.amount) || (!listeningPayment.exactAmount && pendingBlock.amount > listeningPayment.amount))
            {
              Unwatch(listeningPayment.address, listeningPayment.watcherId);
              listeningPayment.callback(false);
              listeningPayment.callback = null;
            }
          }
        }
      }

      // Payout listener
      if (listeningPayout.callback != null)
      {
        // Check if payment has expired
        var expired = (DateTime.UtcNow - listeningPayout.timerStart).Seconds > listeningPayout.expiryTime;
        if (expired)
        {
          Unwatch(listeningPayout.address, listeningPayout.watcherId);
          listeningPayout.callback(true);
          listeningPayout.callback = null;
        }
        else
        {
          yield return Balance(listeningPayout.address, (balance, pending) =>
          {
            if (balance != null && balance.Equals(new NanoAmount("0")))
            {
              Unwatch(listeningPayout.address, listeningPayout.watcherId);
              listeningPayout.callback(false);
              listeningPayout.callback = null;
            }
          });
        }
      }
    }

    private NanoPlugin.RPC rpc;
    private NanoWebSocket websocket;
    public NanoWebSocket Websocket
    {
      private get { return websocket; }
      set
      {
        websocket = value;
        SetupFilteredConfirmationMessageWebsocketListener();
      }
    }
    public string rpcURL;

    public string defaultRep = "nano_387tj8fjeo6r35ry5tjppympp8dct4d1ogpis7uaxsw8ywsrgp6shfge7two";
  }
}
