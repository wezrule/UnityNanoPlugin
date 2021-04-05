using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using Blake2Core;
using Chaos.NaCl;
using System.IO;
using UnityEngine;
using QRCoder;
using QRCoder.Unity;

namespace NanoPlugin
{
  public static class NanoUtils
  {
    private static Dictionary<char, string> nano_addressEncoding;
    private static Dictionary<string, char> nano_addressDecoding;

    public static byte[] GeneratePrivateKey()
    {
      RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
      byte[] randomNumber = new byte[32];
      rngCsp.GetBytes(randomNumber);
      return randomNumber;
    }

    /**
     * Checks if the raw string is valid.
     *
     * @param rawStr the raw value
     */
    public static bool ValidateRaw(string rawStr)
    {
      BigInteger raw;
      if (BigInteger.TryParse(rawStr, out raw))
      {
        return raw <= NanoAmount.MAX_VALUE.getAsRaw() && raw >= 0;
      }

      return false;
    }

    /**
     * Checks if the nano string is valid.
     *
     * @param nanoStr the raw value
     */
    public static bool ValidateNano(string nano)
    {
      if (nano.Length > 40 || nano.Length == 0)
      {
        return false;
      }

      // Check that it contains only 0-9 digits and at most 1 decimal
      var error = false;
      var num_decimal_points = 0;
      var decimal_point_index = -1;
      for (var i = 0; i < nano.Length; ++i)
      {
        var c = nano[i];
        if (!Char.IsDigit(c))
        {
          if (c.Equals('.') || c.Equals(','))
          {
            decimal_point_index = i;
            ++num_decimal_points;
          }
          else
          {
            error = true;
            break;
          }
        }
      }

      if (error || num_decimal_points > 1)
      {
        return false;
      }

      if (decimal_point_index == -1)
      {
        // There is no decimal and it contains only digits
        var as_int = Int32.Parse(nano);
        return as_int <= 340282366;  // This is the maximum amount of Nano there is
      }
      else
      {
        // Split the string
        var arr = nano.Split('.');
        if (arr.Length != 2 || arr[0].Length > 9)
        {
          arr = nano.Split(',');
          if (arr.Length != 2 || arr[0].Length > 9)
          {
            return false;
          }
        }

        // These could be empty text
        var integer_part = arr[0];
        var fraction_part = arr[1];

        if (!String.IsNullOrWhiteSpace(integer_part))
        {
          var as_int = Int32.Parse(integer_part);
          error = as_int > 340282366;  // This is the maximum amount of Nano there is
        }

        if (!error)
        {
          if (!String.IsNullOrWhiteSpace(fraction_part))
          {
            var max = BigInteger.Parse("920938463463374607431768211456");
            var num = BigInteger.Parse(fraction_part);
            error = num > max;
          }
        }
      }

      return !error;
    }
    public static string NanoToRaw(string str)
    {
      // Remove decimal point (if exists) and add necessary trailing 0s to form exact raw number
      var decimalPoint = -1;
      for (int i = 0; i < str.Length; ++i)
      {
        var c = str[i];
        if (c.Equals('.') || str[i].Equals(','))
        {
          decimalPoint = i;
          break;
        }
      }
      var num_zeroes_to_add = 30;
      if (decimalPoint != -1)
      {
        str = str.Remove(decimalPoint, 1);
        num_zeroes_to_add -= (str.Length - decimalPoint);
      }

      var raw = str + new string('0', num_zeroes_to_add);

      // Remove leading zeroes
      var start_index = 0;
      foreach (var c in raw)
      {
        if (c.Equals('0'))
        {
          ++start_index;
        }
        else
        {
          break;
        }
      }

      // Remove leading zeroes
      return raw.Substring(start_index);
    }
    public static string RawToNano(string str)
    {
      // Insert a decimal 30 decimal places from the right
      if (str.Length <= 30)
      {
        str = "0." + new String('0', 30 - str.Length) + str;
      }
      else
      {
        var decimal_index = str.Length - 30;
        str = str.Insert(decimal_index, ".");
      }

      var index = 0;

      var newStr = String.Copy(str);
      foreach (var c in newStr.Reverse())
      {
        if (!c.Equals('0'))
        {
          if (c.Equals('.'))
          {
            --index;
          }
          break;
        }
        ++index;
      }

      return str.Substring(0, str.Length - index);
    }

    public static string GetBlockTypeStr(BlockType blockType)
    {
      return Enum.GetName(typeof(BlockType), blockType);
    }

    static byte[] sha256(string text)
    {
      using (SHA256 mySHA256 = SHA256.Create())
      {
        return mySHA256.ComputeHash(Encoding.UTF8.GetBytes(text));
      }
    }

    static byte[] iv = new byte[] { 190, 110, 198, 210, 15, 18, 163, 94, 139, 191, 100, 72, 154, 120, 105, 119 };

    static string Encrypt(string plainSeed, string password)
    {
      if (plainSeed.Length == 0)
      {
        return "";
      }

      var key = sha256(password);

      // Create a new instance of the Aes
      // class.  This generates a new key and initialization
      // vector (IV).
      using (Aes myAes = Aes.Create())
      {
        myAes.Key = key;
        myAes.IV = iv;
        // Encrypt the string to an array of bytes.
        return ByteArrayToHexString(EncryptStringToBytes_Aes(plainSeed, myAes.Key, myAes.IV));
      }
    }
    static string Decrypt(string cipherSeed, string password)
    {
      var key = sha256(password);
      return DecryptStringFromBytes_Aes(HexStringToByteArray(cipherSeed), key, iv);
    }

    static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
    {
      // Check arguments.
      if (plainText == null || plainText.Length <= 0)
        throw new ArgumentNullException("plainText");
      if (Key == null || Key.Length <= 0)
        throw new ArgumentNullException("Key");
      if (IV == null || IV.Length <= 0)
        throw new ArgumentNullException("IV");
      byte[] encrypted;

      // Create an Aes object
      // with the specified key and IV.
      using (Aes aesAlg = Aes.Create())
      {
        aesAlg.Key = Key;
        aesAlg.IV = IV;

        // Create an encryptor to perform the stream transform.
        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        // Create the streams used for encryption.
        using (MemoryStream msEncrypt = new MemoryStream())
        {
          using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
          {
            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
            {
              //Write all data to the stream.
              swEncrypt.Write(plainText);
            }
            encrypted = msEncrypt.ToArray();
          }
        }
      }

      // Return the encrypted bytes from the memory stream.
      return encrypted;
    }

    static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
    {
      // Check arguments.
      if (cipherText == null || cipherText.Length <= 0)
        throw new ArgumentNullException("cipherText");
      if (Key == null || Key.Length <= 0)
        throw new ArgumentNullException("Key");
      if (IV == null || IV.Length <= 0)
        throw new ArgumentNullException("IV");

      // Declare the string used to hold
      // the decrypted text.
      string plaintext = null;

      // Create an Aes object
      // with the specified key and IV.
      using (Aes aesAlg = Aes.Create())
      {
        aesAlg.Key = Key;
        aesAlg.IV = IV;

        // Create a decryptor to perform the stream transform.
        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        // Create the streams used for decryption.
        try
        {
          using (MemoryStream msDecrypt = new MemoryStream(cipherText))
          {
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            {
              using (StreamReader srDecrypt = new StreamReader(csDecrypt))
              {
                // Read the decrypted bytes from the decrypting stream
                // and place them in a string.
                plaintext = srDecrypt.ReadToEnd();
              }
            }
          }
        }
        catch (Exception)
        {
          plaintext = String.Empty;
        }
      }

      return plaintext;
    }

    static Texture2D GenerateQRCodeTexture(int pixelsPerModule, string qrString, int margin)
    {
      QRCodeGenerator qrGenerator = new QRCodeGenerator();
      QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.Q);
      UnityQRCode qrCode = new UnityQRCode(qrCodeData);
      Texture2D qrCodeAsTexture2D = qrCode.GetGraphic(pixelsPerModule);
      return qrCodeAsTexture2D;
    }

    public static Texture2D GenerateQRCodeTextureOnlyAccount(int pixelsPerModule, string account, int margin)
    {
      string qrString = "nano:" + account;
      return GenerateQRCodeTexture(pixelsPerModule, qrString, margin);
    }

    public static Texture2D GenerateQRCodeTextureWithPrivateKey(int pixelsPerModule, string privateKey, int margin)
    {
      string qrString = privateKey.ToUpper();
      return GenerateQRCodeTexture(pixelsPerModule, qrString, margin);
    }

    public static Texture2D GenerateQRCodeTextureWithAmount(
      int pixelsPerModule, string account, string amount, int margin)
    {
      string qrString = "nano:" + account;
      if (amount != "")
      {
        qrString += "?amount=" + amount;
      }
      return GenerateQRCodeTexture(pixelsPerModule, qrString, margin);
    }

    static NanoUtils()
    {
      nano_addressEncoding = new Dictionary<char, string>();
      nano_addressDecoding = new Dictionary<string, char>();


      var i = 0;
      foreach (var validAddressChar in "13456789abcdefghijkmnopqrstuwxyz")
      {
        nano_addressEncoding[validAddressChar] = Convert.ToString(i, 2).PadLeft(5, '0');
        nano_addressDecoding[Convert.ToString(i, 2).PadLeft(5, '0')] = validAddressChar;
        i++;
      }
    }

    public static byte[] HexStringToByteArray(string hex)
    {
      return Enumerable.Range(0, hex.Length)
                           .Where(x => x % 2 == 0)
                           .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                           .ToArray();
    }

    public static string ByteArrayToHexString(byte[] bytes)
    {
      var hex = new StringBuilder();
      for (int j = 0; j < bytes.Length; j++)
      {
        hex.AppendFormat("{0:X2}", bytes[j]);
      }
      return hex.ToString();
    }

    public static string PublicKeyToAddress(byte[] publicKey)
    {
      var address = "nano_" + NanoEncode(publicKey);

      var blake = Blake2B.Create(new Blake2BConfig() { OutputSizeInBytes = 5 });
      blake.Init();
      blake.Update(publicKey);
      var checksumBytes = blake.Finish();

      address += NanoEncode(checksumBytes.Reverse().ToArray(), false);

      return address;
    }

    public static string PublicKeyToAddress(string publicKey)
    {
      return PublicKeyToAddress(HexStringToByteArray(publicKey));
    }

    private static string NanoEncode(byte[] bytes, bool padZeros = true)
    {
      var binaryString = padZeros ? "0000" : "";
      for (int i = 0; i < bytes.Length; i++)
      {
        binaryString += Convert.ToString(bytes[i], 2).PadLeft(8, '0');
      }

      var result = "";

      for (int i = 0; i < binaryString.Length; i += 5)
      {
        result += nano_addressDecoding[binaryString.Substring(i, 5)];
      }

      return result;
    }

    public static string PrivateKeyToAddress(byte[] privateKey)
    {
      return PublicKeyToAddress(Ed25519.PublicKeyFromSeed(privateKey));
    }

    public static string PrivateKeyToAddress(string privateKey)
    {
      return PrivateKeyToAddress(HexStringToByteArray(privateKey));
    }

    public static string PrivateKeyToPublicKeyHexString(byte[] privateKey)
    {
      return ByteArrayToHexString(Ed25519.PublicKeyFromSeed(privateKey));
    }

    public static string PrivateKeyToPublicKeyHexString(string privateKey)
    {
      return ByteArrayToHexString(Ed25519.PublicKeyFromSeed(HexStringToByteArray (privateKey)));
    }

    public static string AddressToPublicKeyHexString(string address)
    {
      return ByteArrayToHexString(AddressToPublicKeyByteArray(address));
    }

    public static byte[] AddressToPublicKeyByteArray(string address)
    {
      // Check length is valid
      if (address.Length != 65)
      {
        return null;
      }

      // Address must begin with nano_
      if (!address.Substring(0, 5).Equals("nano_"))
      {
        return null;
      }

      // Remove nano_
      var publicKeyPart = address.Substring(5, address.Length - 8);

      var binaryString = "";
      for (int i = 0; i < publicKeyPart.Length; i++)
      {
        // Decode each character into string representation of it's binary parts
        binaryString += nano_addressEncoding[publicKeyPart[i]];
      }

      // Remove leading 4 0s
      binaryString = binaryString.Substring(4);

      // Convert to bytes
      var pk = new byte[32];
      for (int i = 0; i < 32; i++)
      {
        // for each byte, read the bits from the binary string
        var b = Convert.ToByte(binaryString.Substring(i * 8, 8), 2);
        pk[i] = b;
      }
      return pk;
    }

    public static string HashStateBlock(string accountAddress, string previousHash, string balance, string representativeAccount, string link)
    {
      var representativePublicKey = AddressToPublicKeyByteArray(representativeAccount);
      var accountPublicKey = AddressToPublicKeyByteArray(accountAddress);
      var previousBytes = HexStringToByteArray(previousHash);

      var balanceHex = BigInteger.Parse(balance).ToString("X");
      if (balanceHex.Length % 2 == 1)
      {
        balanceHex = "0" + balanceHex;
      }
      byte[] balanceBytes = HexStringToByteArray(balanceHex.PadLeft(32, '0'));
      var linkBytes = HexStringToByteArray(link);
      var preamble = HexStringToByteArray(new string('0', 63) + '6');

      var blake = Blake2B.Create(new Blake2BConfig() { OutputSizeInBytes = 32 });

      blake.Init();
      blake.Update(preamble); // Preamble

      blake.Update(accountPublicKey);
      blake.Update(previousBytes);
      blake.Update(representativePublicKey);
      blake.Update(balanceBytes);
      blake.Update(linkBytes);

      var hashBytes = blake.Finish();
      return ByteArrayToHexString(hashBytes);
    }

    public static string SignHash(string hash, byte[] privateKey)
    {
      var signature = Ed25519.Sign(HexStringToByteArray(hash), Ed25519.ExpandedPrivateKeyFromSeed(privateKey));
      return ByteArrayToHexString(signature);
    }

    // Encrypts the private key with the password and saved to this file
    public static void SavePrivateKey(string plainSeed, string seedFilename, string password)
    {
      var cypherText = Encrypt(plainSeed, password);

      string destination = Path.Combine(Application.persistentDataPath, "Nano", seedFilename);
      System.IO.FileInfo file = new System.IO.FileInfo(destination);
      file.Directory.Create(); // If the directory already exists, this method does nothing.
      File.WriteAllText(file.FullName, cypherText);
    }

    // Loads the file and decrypts the encrypted private key with the password
    public static string LoadPrivateKey(string seedFilename, string password)
    {
      string file = Path.Combine(Application.persistentDataPath, "Nano", seedFilename);

      if (File.Exists(file))
      {
        var text = File.ReadAllText(file);
        return Decrypt(text, password);
      }
      else
      {
        Debug.LogError("File not found");
        return "";
      }
    }

    public static string[] GetPrivateKeyFiles()
    {
      // Loop through all files in data path
      string directory = Path.Combine(Application.persistentDataPath, "Nano");
      if (Directory.Exists(directory))
      {
        return Directory.EnumerateFiles(directory).Select(Path.GetFileName).ToArray();
      }
      else
      {
        return new string[0];
      }
    }
  }

  public enum BlockType
  {
    receive,
    send,
    epoch,
    open,
    change
  }
}
