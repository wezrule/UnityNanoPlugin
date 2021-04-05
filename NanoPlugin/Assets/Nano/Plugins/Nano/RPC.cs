using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NanoPlugin
{
  public class RPC
  {
    public string url;

    public RPC(string url)
    {
      this.url = url;
    }

    public IEnumerator MakeRequest(UnityWebRequest webRequest, byte[] body, Action<string> callback)
    {
      webRequest.uploadHandler = new UploadHandlerRaw(body);
      webRequest.downloadHandler = new DownloadHandlerBuffer();
      webRequest.SetRequestHeader("Content-Type", "application/json");
      webRequest.SetRequestHeader("Accepts", "application/json");

      yield return webRequest.SendWebRequest();

      if (webRequest.isNetworkError || webRequest.isHttpError)
      {
        Debug.Log(webRequest.error);
      }
      else
      {
        callback(webRequest.downloadHandler.text);
      }
    }

    public IEnumerator AccountBalance(string account, Action<string> callback)
    {
      using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
      {
        var request = new AccountBalanceRequest();
        request.account = account;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        yield return MakeRequest(webRequest, bodyRaw, callback);
      }
    }

    public IEnumerator WorkGenerate(string hash, Action<string> callback)
    {
      using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
      {
        var request = new WorkGenerateRequest();
        request.hash = hash;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        yield return MakeRequest(webRequest, bodyRaw, callback);
      }
    }

    public IEnumerator AccountInfo(string account, Action<string> callback)
    {
      using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
      {
        var request = new AccountInfoRequest();
        request.account = account;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        yield return MakeRequest(webRequest, bodyRaw, callback);
      }
    }

    public IEnumerator BlockInfo(string hash, Action<string> callback)
    {
      using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
      {
        var request = new BlockInfoRequest();
        request.hash = hash;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        yield return MakeRequest(webRequest, bodyRaw, callback);
      }
    }

    public IEnumerator PendingBlocks(string account, Action<string> callback)
    {
      using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
      {
        var request = new PendingRequest();
        request.account = account;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        yield return MakeRequest(webRequest, bodyRaw, callback);
      }
    }

    public IEnumerator Process(Block block, string subtype, Action<string> callback)
    {
      using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
      {
        var request = new ProcessRequest();
        request.block = block;
        request.subtype = subtype;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        yield return MakeRequest(webRequest, bodyRaw, callback);
      }
    }
  }

  [Serializable]
  public class AccountBalanceRequest
  {
    public string action = "account_balance";
    public bool include_confirmed = true;
    public string account;
  }

  [Serializable]
  public class AccountBalanceResponse
  {
    public string balance;
    public string pending;
  }

  [Serializable]
  public class WorkGenerateRequest
  {
    public string action = "work_generate";
    public string hash;
  }

  [Serializable]
  public class WorkGenerateResponse
  {
    public string work;
    public string difficulty;
    public string multiplier;
    public string hash;
  }

  [Serializable]
  public class PendingRequest
  {
    public string action = "pending";
    public string account;
    public int count = 5;
    public bool sorting = true;
    public bool amount = true;
    public bool include_only_confirmed = true;
  }

  [Serializable]
  public class PendingResponse
  {
    public List<string> blocks;
  }

  [Serializable]
  public class BlockInfoRequest
  {
    public string action = "block_info";
    public bool json_block = true;
    public string hash;
  }

  [Serializable]
  public class BlockInfoResponse
  {
    public string block_account;
    public string amount;
    public string balance;
    public string height;
    public string local_timestamp;
    public string confirmed;
    public Block contents;
    public string subtype;
  }

  [Serializable]
  public class AccountInfoRequest
  {
    public string action = "account_info";
    public string account;
    public bool representative = true;
  }

  [Serializable]
  public class AccountInfoResponse
  {
    public string frontier;
    public string open_block;
    public string representative_block;
    public string representative;
    public string balance;
    public string modified_timestamp;
    public string block_count;
    public string confirmation_height;
    public string confirmation_height_frontier;
    public string account_version;
  }

  [Serializable]
  public class Block
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
  }

  [Serializable]
  public class ProcessRequest
  {
    public string action = "process";
    public string json_block = "true";
    public string subtype;
    public Block block;
  }

  [Serializable]
  public class ProcessResponse
  {
    public string hash;
  }
}
