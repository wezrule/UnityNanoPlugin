using NanoPluginLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class NanoDemo : MonoBehaviour
{
  // UI elements
  public Button CreateSeedUI;
  public Button NextSeedUI;
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

  public string seed;
  public string address;

  public string arcadeSeed;
  public string watcherSeed;

  public NanoAmount currentBalance = new NanoAmount(0);

  private string password = "wezrule";

  private string rep = "nano_387tj8fjeo6r35ry5tjppympp8dct4d1ogpis7uaxsw8ywsrgp6shfge7two";

  void Start()
  {
    // Initialize RPC & Websocket
    nanoManager = gameObject.AddComponent<NanoManager>();
    nanoManager.rpcURL = "http://95.216.164.23:28103"; // "http://127.0.0.1:28103"; // 
    nanoManager.defaultRep = "nano_387tj8fjeo6r35ry5tjppympp8dct4d1ogpis7uaxsw8ywsrgp6shfge7two";

    websocket = gameObject.AddComponent<NanoWebSocket>();
    websocket.url = "ws://95.216.164.23:28104";  //"ws://127.0.0.1:28104";
    nanoManager.Websocket = websocket;

    Debug.Log("Seed files located at: " + Path.Combine(Application.persistentDataPath, "Nano"));

    nanoManager.AddOnWebsocketConnectListener((bool isError, bool isReconnect) =>
   {
     // Called when the connection is successfully opened (or failed), it will automatically keep trying to connect if there is a failure
     if (!isError)
     {
       nanoManager.ListenForPaymentWaitConfirmation(NanoUtils.PublicKeyFromPrivateKey(arcadeSeed).Address, new NanoAmount("1000000000000000000000000"), true, (error) =>
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
    CreateSeedUI.onClick.AddListener(OnClickCreateSeed);
    NextSeedUI.onClick.AddListener(OnClickNextSeed);
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

    // Update QR codes for arcade
    arcadeSeed = NanoUtils.ByteArrayToHex(NanoUtils.CreateSeed());

    var numRawPayToPlay = "1000000000000000000000000";
    var qrCodePayAsTexture2D = NanoUtils.GenerateQRCodeTextureWithAmount(50, NanoUtils.PublicKeyFromPrivateKey(arcadeSeed).Address, numRawPayToPlay, 50);
    QRCodePayArcadeUI.sprite = Sprite.Create(qrCodePayAsTexture2D, new Rect(0.0f, 0.0f, qrCodePayAsTexture2D.width, qrCodePayAsTexture2D.height), new Vector2(0.5f, 0.5f), 100.0f);

    watcherSeed = NanoUtils.ByteArrayToHex(NanoUtils.CreateSeed());

    OnClickNextSeed();
  }

  IEnumerator PaidArcadeHandler()
  {
    // Recieve this block

    List<PendingBlock> pendingBlocks = null;
    var arcadeAddress = NanoUtils.PublicKeyFromPrivateKey(arcadeSeed).Address;
    yield return nanoManager.PendingBlocks(arcadeAddress, (responsePendingBlocks) =>
    {
      pendingBlocks = responsePendingBlocks;
    });

    // Just get the first one as it will have the highest amount
    if (pendingBlocks != null && pendingBlocks.Count > 0)
    {
      // Returns the one with the highest amount
      var pendingBlock = pendingBlocks[0];
      yield return nanoManager.ReceiveWaitConf(arcadeAddress, pendingBlock, arcadeSeed, (error, hash) =>
      {
        if (!error)
        {
          var qrCodePayoutAsTexture2D = NanoUtils.GenerateQRCodeTextureWithPrivateKey(50, arcadeSeed, 50);
          QRCodePayoutArcadeUI.sprite = Sprite.Create(qrCodePayoutAsTexture2D, new Rect(0.0f, 0.0f, qrCodePayoutAsTexture2D.width, qrCodePayoutAsTexture2D.height), new Vector2(0.5f, 0.5f), 100.0f);
          QRCodePayArcadeUI.sprite = null;

          // Start the payout listener
          var expirySecs = 30;
          nanoManager.ListenForPayoutWaitConfirmation(arcadeAddress, expirySecs, (errorPayout) =>
          {
            if (!errorPayout)
            {
              PayoutArcadeUI.text = "Extracted seed";
            }
            else
            {
              PayoutArcadeUI.text = "Seed expired";
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

  void SeedChanged()
  {
    // Update the public key text element
    address = NanoUtils.PublicKeyFromPrivateKey(seed).Address;
    PublicKeyUI.text = address;

    // Update QRcode for topping up funds
    var qrCodeAsTexture2D = NanoUtils.GenerateQRCodeTextureOnlyAccount(50, address, 50);
    QRCodeTopUpUI.sprite = Sprite.Create(qrCodeAsTexture2D, new Rect(0.0f, 0.0f, qrCodeAsTexture2D.width, qrCodeAsTexture2D.height), new Vector2(0.5f, 0.5f), 100.0f);
  }

  void OnClickCreateSeed()
  {
    seed = NanoUtils.ByteArrayToHex(NanoUtils.CreateSeed());
    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
    var filename = "encrypted_seed_" + (int)t.TotalSeconds + ".nano";

    // Save the seed to disk
    NanoUtils.SaveSeed(seed, filename, password);
    SeedChanged();
  }

  // Loop through all the local seed files, and display any which can be decrypted with the default password
  private int seedIndex = 0;
  void OnClickNextSeed()
  {
    var seedFiles = NanoUtils.GetSeedFiles();
    if (seedFiles.Length > 0)
    {
      var seedFile = seedFiles[seedIndex];
      seed = NanoUtils.GetPlainSeed(seedFile, password);
      if (!String.IsNullOrEmpty(seed))
      {
        SeedChanged();
      }
      ++seedIndex;
      if (seedIndex >= seedFiles.Length - 0)
      {
        seedIndex = 0;
      }
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
    yield return nanoManager.AccountFrontier(address, (hash) =>
    {
      previous = hash;
    });

    // Create the block to send
    var newBalance = currentBalance - new NanoAmount(System.Numerics.BigInteger.Parse("1000000000000000000000000"));
    var block = nanoManager.CreateBlock(address, NanoUtils.HexStringToByteArray(seed), newBalance, NanoUtils.PublicKeyFromPrivateKey(watcherSeed).Key, previous, rep, LastWorkUI.text);
    yield return nanoManager.Process(block, BlockType.send, (hash) =>
   {
     if (hash != null)
     {
       LastWorkUI.text = ""; // Clear it
     }

     Debug.Log(hash);
   });
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
    yield return nanoManager.AccountFrontier(address, (hash) =>
    {
      previous = hash;
    });

    List<PendingBlock> pendingBlocks = null;
    yield return nanoManager.PendingBlocks(address, (responsePendingBlocks) =>
    {
      pendingBlocks = responsePendingBlocks;
    });

    // Just get the first one as it will have the highest amount
    var pendingBlock = pendingBlocks[0];

    // Create the block to receive
    var newBalance = currentBalance + pendingBlock.amount;
    var block = nanoManager.CreateBlock(address, NanoUtils.HexStringToByteArray(seed), newBalance, pendingBlock.source, previous, rep, LastWorkUI.text);
    yield return nanoManager.Process(block, previous == null ? BlockType.open : BlockType.receive, (hash) =>
    {
      if (hash != null)
      {
        LastWorkUI.text = ""; // Clear it
      }

      Debug.Log(hash);
    });
  }

  void OnClickSend()
  {
    StartCoroutine(SendHandler());
  }

  private IEnumerator SendHandler()
  {
    yield return nanoManager.Send(address, NanoUtils.PublicKeyFromPrivateKey(NanoUtils.HexStringToByteArray(watcherSeed)).Address, rep, new NanoAmount("1000000000000000000000000"), seed, (error, hash) =>
    {
      Debug.Log("Send wait confirmed!!");
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
      yield return nanoManager.Receive(address, pendingBlock, seed, (error, callback) =>
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
    yield return nanoManager.SendWaitConf(address, NanoUtils.PublicKeyFromPrivateKey(NanoUtils.HexStringToByteArray(watcherSeed)).Address, rep, new NanoAmount("1000000000000000000000000"), seed, (error, hash) =>
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
      yield return nanoManager.ReceiveWaitConf(address, pendingBlock, seed, (error, hash) =>
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
    nanoManager.AutomatePocketing(address, seed, (block) =>
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
    lastWatcherId = nanoManager.Watch(NanoUtils.PublicKeyFromPrivateKey(NanoUtils.HexStringToByteArray(watcherSeed)).Address, (watcherInfo) =>
    {
      WatchedUI.text = watcherInfo.hash;
    });
  }

  void OnClickUnwatch()
  {
    nanoManager.Unwatch(NanoUtils.PublicKeyFromPrivateKey(NanoUtils.HexStringToByteArray(watcherSeed)).Address, lastWatcherId);
  }

  // Update is called once per frame
  void Update()
  {
    StartCoroutine(UpdatePendingAndBalance());
  }

  // Every 2 seconds poll for some things in case the websocket missed them
  private float updateTimer = 2;
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

  private NanoWebSocket websocket;
  private NanoManager nanoManager;
}
