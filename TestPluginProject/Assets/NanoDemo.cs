using NanoPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class NanoDemo : MonoBehaviour
{
  // UI elements
  public Button CreatePrivateKeyUI;
  public Button NextPrivateKeyUI;
  public Button GenerateWorkUI;
  public Button SendNeedsWorkUI;
  public Button ReceiveNeedsWorkUI;
  public Button SendUI;
  public Button ReceiveUI;
  public Button SendWaitConfUI;
  public Button ReceiveWaitConfUI;
  public Button AutomatePocketingUI;
  public Button UnautomatePocketingUI;
  public Button ListenAllConfirmationsUI;
  public Button UnlistenAllConfirmationsUI;
  public Button WatchUI;
  public Button UnwatchUI;

  // QRCode
  public Image QRCodeTopUpUI;
  public Image QRCodePayArcadeUI;
  public Image QRCodePayoutArcadeUI;

  // Text elements
  public Text LastWorkUI;
  public Text PublicKeyUI;
  public Text BalanceUI;
  public Text PendingBalanceUI;
  public Text PayArcadeUI;
  public Text PayoutArcadeUI;
  public Text WatchedUI;

  public Text WebsockConfirmationResponseUI;

  public string privateKey;
  public string address;

  public string arcadePrivateKey;
  public string watcherPrivateKey;

  public NanoAmount currentBalance = new NanoAmount(0);

  private string password = "wezrule";
  private string defaultRep = "nano_387tj8fjeo6r35ry5tjppympp8dct4d1ogpis7uaxsw8ywsrgp6shfge7two";

  void Start()
  {
    // Initialize RPC & Websocket
    nanoManager = gameObject.AddComponent<NanoManager>();
    nanoManager.rpcURL = "http://95.216.164.23:28103"; // Update this url to point to your JSON-RPC server
    nanoManager.defaultRep = defaultRep;

    nanoWebsocket = gameObject.AddComponent<NanoWebSocket>();
    nanoWebsocket.url = "ws://95.216.164.23:28104"; // Update this url to point to your websocket server
    nanoManager.Websocket = nanoWebsocket;

    Debug.Log("Private key files located at: " + Path.Combine(Application.persistentDataPath, "Nano"));

    // Update QR codes for arcade
    arcadePrivateKey = NanoUtils.ByteArrayToHexString(NanoUtils.GeneratePrivateKey());

    nanoManager.AddOnWebsocketConnectListener((bool isError, bool isReconnect) =>
   {
     // Called when the connection is successfully opened (or failed), it will automatically keep trying to connect if there is a failure
     if (!isError)
     {
       nanoManager.ListenForPaymentWaitConfirmation(NanoUtils.PrivateKeyToAddress(arcadePrivateKey), new NanoAmount("1000000000000000000000000"), true, (error) =>
       {
         if (!error)
         {
           PayArcadeUI.text = "Paid!";
           // Wait until the account has a balance
           StartCoroutine(PaidArcadeHandler());
         }
         else
         {
           Debug.Log("Error with payment");
         }
       });

       Debug.Log("Successfully connected to websocket!!");
     }
     else
     {
       Debug.Log("Failed to connect to websocket!!");
     }
   });

    nanoManager.AddConfirmationListener((websocketConfirmationResponse) =>
    {
      Debug.Log("Confirmation received");
      string output = "";
      var block = websocketConfirmationResponse.message.block;
      output += "type: " + block.type + "\n";
      output += "account: " + block.account + "\n";
      output += "previous: " + block.previous + "\n";
      output += "representative: " + block.representative + "\n";
      output += "balance: " + block.balance + "\n";
      output += "link: " + block.link + "\n";
      output += "link_as_account: " + block.link_as_account + "\n";
      output += "signature: " + block.signature + "\n";
      output += "work: " + block.work + "\n";
      output += "subtype: " + block.subtype;
      WebsockConfirmationResponseUI.text = output;
    });

    nanoManager.AddFilteredConfirmationListener((websocketConfirmationResponse) =>
    {
      Debug.Log("Confirmation received");
    });

    // Add event listeners for all the buttons
    CreatePrivateKeyUI.onClick.AddListener(OnClickCreatePrivateKey);
    NextPrivateKeyUI.onClick.AddListener(OnClickNextPrivateKey);
    GenerateWorkUI.onClick.AddListener(OnClickGenerateWork);
    SendNeedsWorkUI.onClick.AddListener(OnClickSendNeedsWork);
    ReceiveNeedsWorkUI.onClick.AddListener(OnClickReceiveNeedsWork);
    SendUI.onClick.AddListener(OnClickSend);
    ReceiveUI.onClick.AddListener(OnClickReceive);
    SendWaitConfUI.onClick.AddListener(OnClickSendWaitConf);
    ReceiveWaitConfUI.onClick.AddListener(OnClickReceiveWaitConf);
    AutomatePocketingUI.onClick.AddListener(OnClickAutomatePocketing);
    UnautomatePocketingUI.onClick.AddListener(OnClickUnautomatePocketing);
    ListenAllConfirmationsUI.onClick.AddListener(OnClickListenAllConfirmations);
    UnlistenAllConfirmationsUI.onClick.AddListener(OnClickUnlistenAllConfirmations);
    WatchUI.onClick.AddListener(OnClickWatch);
    UnwatchUI.onClick.AddListener(OnClickUnwatch);

    var numRawPayToPlay = "1000000000000000000000000";
    var qrCodePayAsTexture2D = NanoUtils.GenerateQRCodeTextureWithAmount(250, NanoUtils.PrivateKeyToAddress(arcadePrivateKey), numRawPayToPlay, 50);
    QRCodePayArcadeUI.sprite = Sprite.Create(qrCodePayAsTexture2D, new Rect(0.0f, 0.0f, qrCodePayAsTexture2D.width, qrCodePayAsTexture2D.height), new Vector2(0.5f, 0.5f), 100.0f);

    watcherPrivateKey = NanoUtils.ByteArrayToHexString(NanoUtils.GeneratePrivateKey());

    OnClickNextPrivateKey();
  }

  IEnumerator PaidArcadeHandler()
  {
    // Recieve this block

    List<PendingBlock> pendingBlocks = null;
    var arcadeAddress = NanoUtils.PrivateKeyToAddress(arcadePrivateKey);
    yield return nanoManager.PendingBlocks(arcadeAddress, (responsePendingBlocks) =>
    {
      pendingBlocks = responsePendingBlocks;
    });

    // Just get the first one as it will have the highest amount
    if (pendingBlocks != null && pendingBlocks.Count > 0)
    {
      // Returns the one with the highest amount
      var pendingBlock = pendingBlocks[0];
      yield return nanoManager.ReceiveWaitConf(arcadeAddress, pendingBlock, arcadePrivateKey, (error, hash) =>
      {
        if (!error)
        {
          var qrCodePayoutAsTexture2D = NanoUtils.GenerateQRCodeTextureWithPrivateKey(50, arcadePrivateKey, 50);
          QRCodePayoutArcadeUI.sprite = Sprite.Create(qrCodePayoutAsTexture2D, new Rect(0.0f, 0.0f, qrCodePayoutAsTexture2D.width, qrCodePayoutAsTexture2D.height), new Vector2(0.5f, 0.5f), 100.0f);
          QRCodePayArcadeUI.sprite = null;

          // Start the payout listener
          var expirySecs = 30;
          nanoManager.ListenForPayoutWaitConfirmation(arcadeAddress, expirySecs, (errorPayout) =>
          {
            if (!errorPayout)
            {
              PayoutArcadeUI.text = "Extracted privateKey";
            }
            else
            {
              PayoutArcadeUI.text = "PrivateKey expired";
              Debug.Log("Did not retrieve the payout fast enough");
            }
            QRCodePayoutArcadeUI.sprite = null;
          });
        }
        else
        {
          Debug.Log("Error with ReceiveWaitConf");
        }
      });
    }
  }

  void PrivateKeyChanged()
  {
    // Update the public key text element
    address = NanoUtils.PrivateKeyToAddress(privateKey);
    PublicKeyUI.text = address;

    // Update QRcode for topping up funds
    var qrCodeAsTexture2D = NanoUtils.GenerateQRCodeTextureOnlyAccount(50, address, 50);
    QRCodeTopUpUI.sprite = Sprite.Create(qrCodeAsTexture2D, new Rect(0.0f, 0.0f, qrCodeAsTexture2D.width, qrCodeAsTexture2D.height), new Vector2(0.5f, 0.5f), 100.0f);
  }

  void OnClickCreatePrivateKey()
  {
    privateKey = NanoUtils.ByteArrayToHexString(NanoUtils.GeneratePrivateKey());
    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
    var filename = "encrypted_privateKey_" + (int)t.TotalSeconds + ".nano";

    // Save the private key to disk
    NanoUtils.SavePrivateKey(privateKey, filename, password);
    PrivateKeyChanged();
    LastWorkUI.text = ""; // Clear last work
  }

  // Loop through all the local private key files, and display any which can be decrypted with the default password
  private int privateKeyIndex = 0;
  void OnClickNextPrivateKey()
  {
    var privateKeyFiles = NanoUtils.GetPrivateKeyFiles();
    if (privateKeyFiles.Length > 0)
    {
      var privateKeyFile = privateKeyFiles[privateKeyIndex];
      privateKey = NanoUtils.LoadPrivateKey(privateKeyFile, password);
      if (!String.IsNullOrEmpty(privateKey))
      {
        PrivateKeyChanged();
      }
      ++privateKeyIndex;
      if (privateKeyIndex >= privateKeyFiles.Length - 1)
      {
        privateKeyIndex = 0;
      }
    }
    if (privateKeyFiles.Length > 1)
    {
      LastWorkUI.text = ""; // Clear some fields
      BalanceUI.text = "";
      PendingBalanceUI.text = "";
    }
  }

  void OnClickGenerateWork()
  {
    StartCoroutine(GenerateWorkHandler());
  }

  private IEnumerator GenerateWorkHandler()
  {
    // First we get the frontier
    string previous = null;
    yield return nanoManager.AccountFrontier(address, (hash) =>
   {
     previous = hash;
   });

    // If this is an open block, then we want to generate work for the account public key
    yield return nanoManager.WorkGenerate(address, previous, (work) =>
 {
   LastWorkUI.text = work;
 }
    );
  }

  void OnClickSendNeedsWork()
  {
    StartCoroutine(SendNeedsWorkHandler());
  }

  // Send to yourself
  private IEnumerator SendNeedsWorkHandler()
  {
    if (LastWorkUI.text.Equals(""))
    {
      yield break;
    }

    // First we get the frontier
    string previous = null;
    string rep = null;
    yield return nanoManager.AccountInfo(address, (accountInfo) =>
    {
      previous = accountInfo.frontier;
      rep = accountInfo.representative;
    });

    if (previous != null)
    {
      // Create the block to send
      var newBalance = currentBalance - new NanoAmount(System.Numerics.BigInteger.Parse("1000000000000000000000000"));
      var block = nanoManager.CreateBlock(address, NanoUtils.HexStringToByteArray(privateKey), newBalance, NanoUtils.PrivateKeyToPublicKeyHexString(watcherPrivateKey), previous, rep, LastWorkUI.text);
      yield return nanoManager.Process(block, BlockType.send, (hash) =>
     {
       if (hash != null)
       {
         LastWorkUI.text = ""; // Clear it
       }

       Debug.Log(hash);
     });
    }
    else
    {
      Debug.Log("Account does not exist yet");
    }
  }

  void OnClickReceiveNeedsWork()
  {
    StartCoroutine(ReceiveNeedsWorkHandler());
  }

  private IEnumerator ReceiveNeedsWorkHandler()
  {
    if (LastWorkUI.text.Equals(""))
    {
      yield break;
    }

    // First we get the frontier
    string previous = null;
    string rep = null;
    yield return nanoManager.AccountInfo(address, (accountInfo) =>
    {
      previous = accountInfo.frontier;
      rep = accountInfo.representative;
    });

    List<PendingBlock> pendingBlocks = null;
    yield return nanoManager.PendingBlocks(address, (responsePendingBlocks) =>
    {
      pendingBlocks = responsePendingBlocks;
    });

    if (pendingBlocks.Count != 0)
    {
      // Just get the first one as it will have the highest amount
      var pendingBlock = pendingBlocks[0];

      // Create the block to receive
      var newBalance = currentBalance + pendingBlock.amount;
      var block = nanoManager.CreateBlock(address, NanoUtils.HexStringToByteArray(privateKey), newBalance, pendingBlock.source, previous, rep != null ? rep : defaultRep, LastWorkUI.text);
      yield return nanoManager.Process(block, previous == null ? BlockType.open : BlockType.receive, (hash) =>
      {
        if (hash != null)
        {
          LastWorkUI.text = ""; // Clear it
        }

        Debug.Log(hash);
      });
    }
    else
    {
      Debug.Log("There are no pending blocks to receive");
    }
  }

  void OnClickSend()
  {
    StartCoroutine(SendHandler());
  }

  private IEnumerator SendHandler()
  {
    yield return nanoManager.Send(NanoUtils.PrivateKeyToAddress(watcherPrivateKey), new NanoAmount("1000000000000000000000000"), privateKey, (error, hash) =>
    {
      if (!error)
      {
        Debug.Log("Send confirmed!!");
      }
      else
      {
        Debug.Log("Error with Send");
      }
    });
  }

  void OnClickReceive()
  {
    StartCoroutine(ReceiveHandler());
  }

  private IEnumerator ReceiveHandler()
  {
    List<PendingBlock> pendingBlocks = null;
    yield return nanoManager.PendingBlocks(address, (responsePendingBlocks) =>
    {
      pendingBlocks = responsePendingBlocks;
    });

    // Just get the first one as it will have the highest amount
    if (pendingBlocks != null && pendingBlocks.Count > 0)
    {
      var pendingBlock = pendingBlocks[0];
      yield return nanoManager.Receive(address, pendingBlock, privateKey, (error, callback) =>
      {
      });
    }
    else
    {
      Debug.Log("No pending blocks");
    }
  }

  void OnClickSendWaitConf()
  {
    StartCoroutine(SendWaitConfHandler());
  }

  IEnumerator SendWaitConfHandler()
  {
    var amount = new NanoAmount(NanoUtils.NanoToRaw("0.000001"));
    yield return nanoManager.SendWaitConf(NanoUtils.PrivateKeyToAddress(watcherPrivateKey), amount, privateKey, (error, hash) =>
    {
      if (!error)
      {
        Debug.Log("Send wait confirmed!!");
      }
      else
      {
        Debug.Log("Error with SendWaitConf");
      }
    });
  }

  void OnClickReceiveWaitConf()
  {
    StartCoroutine(ReceiveWaitConfHandler());
  }

  IEnumerator ReceiveWaitConfHandler()
  {
    List<PendingBlock> pendingBlocks = null;
    yield return nanoManager.PendingBlocks(address, (responsePendingBlocks) =>
    {
      pendingBlocks = responsePendingBlocks;
    });

    // Just get the first one as it will have the highest amount
    if (pendingBlocks != null && pendingBlocks.Count > 0)
    {
      var pendingBlock = pendingBlocks[0];
      yield return nanoManager.ReceiveWaitConf(address, pendingBlock, privateKey, (error, hash) =>
    {
      if (!error)
      {
        Debug.Log("Receive wait confirmed!!");
      }
      else
      {
        Debug.Log("Error with ReceiveWaitConf");
      }
    });
    }
    else
    {
      Debug.Log("No pending blocks");
    }
  }

  void OnClickAutomatePocketing()
  {
    nanoManager.AutomatePocketing(address, privateKey, (block) =>
    {
      Debug.Log("Automatically pocketed");
    });
  }

  void OnClickUnautomatePocketing()
  {
    nanoManager.UnautomatePocketing(address);
  }

  void OnClickListenAllConfirmations()
  {
    nanoManager.ListenAllConfirmations();
  }

  void OnClickUnlistenAllConfirmations()
  {
    nanoManager.UnlistenAllConfirmations();
  }

  private int lastWatcherId = 0;
  void OnClickWatch()
  {
    lastWatcherId = nanoManager.Watch(NanoUtils.PrivateKeyToAddress(watcherPrivateKey), (watcherInfo) =>
    {
      WatchedUI.text = watcherInfo.hash;
    });
  }

  void OnClickUnwatch()
  {
    nanoManager.Unwatch(NanoUtils.PrivateKeyToAddress(watcherPrivateKey), lastWatcherId);
  }

  // Update is called once per frame
  void Update()
  {
    StartCoroutine(UpdatePendingAndBalance());
  }

  // Every 1 second poll for some things in case the websocket missed them
  private float updateTimer = 1;
  private float time = 0;
  IEnumerator UpdatePendingAndBalance()
  {
    time += Time.deltaTime;
    if (time >= updateTimer)
    {
      yield return nanoManager.Balance(address, (balance, pending) =>
      {
        if (balance != null)
        {
          BalanceUI.text = balance.getAsRaw().ToString();
          PendingBalanceUI.text = pending.getAsRaw().ToString();
          currentBalance = balance;
        }
      });

      time = 0;
    }
  }

  private NanoWebSocket nanoWebsocket;
  private NanoManager nanoManager;
}
