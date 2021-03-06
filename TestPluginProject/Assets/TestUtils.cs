using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NanoPlugin;

public class TestUtils : MonoBehaviour
{
  // Start is called before the first frame update
  void Start()
  {
    // Validate nano/raw functions
    if (!NanoUtils.ValidateNano("100.123"))
    {
      Debug.Log("Error validating nano");
      return;
    }

    if (!NanoUtils.ValidateNano("100,123"))
    {
      Debug.Log("Error validating nano");
      return;
    }

    if (!NanoUtils.ValidateNano("340282366.920938463463374607431768211455"))
    {
      Debug.Log("Error validating nano");
      return;
    }

    if (!NanoUtils.ValidateNano("0.1231231"))
    {
      Debug.Log("Error validating nano");
      return;
    }

    if (!NanoUtils.ValidateNano(".1223"))
    {
      Debug.Log("Error validating nano");
      return;
    }

    if (!NanoUtils.ValidateNano(",1223"))
    {
      Debug.Log("Error validating nano");
      return;
    }

    if (NanoUtils.ValidateNano(".122.3"))
    {
      Debug.Log("Error validating nano, 2 decimal points");
      return;
    }

    if (!NanoUtils.ValidateRaw("100"))
    {
      Debug.Log("Error validating raw");
      return;
    }

    if (!NanoUtils.ValidateRaw("340282366920938463463374607431768211455"))
    {
      Debug.Log("Error validating raw");
      return;
    }

    if (!NanoUtils.ValidateRaw("0001"))
    {
      Debug.Log("Error validating raw");
      return;
    }

    if (NanoUtils.ValidateRaw("100.123"))
    {
      Debug.Log("Error validating raw");
      return;
    }

    if (NanoUtils.ValidateRaw("100@123"))
    {
      Debug.Log("Error validating raw");
      return;
    }

    if (NanoUtils.ValidateRaw("1111111111111111111111111111111111111111111111111111111111"))
    {
      Debug.Log("Error validating raw");
      return;
    }

    // 1 above max raw
    if (NanoUtils.ValidateRaw("340282366920938463463374607431768211456"))
    {
      Debug.Log("Error validating raw");
      return;
    }

    // Raw to nano
    var raw = "10000000000000000000000000000000";
    var nano = NanoUtils.RawToNano(raw);
    if (!nano.Equals("10.0"))
    {
      Debug.Log("Error converting raw to nano");
      return;
    }

    raw = "1000000000000000000000000000000";
    nano = NanoUtils.RawToNano(raw);
    if (!nano.Equals("1.0"))
    {
      Debug.Log("Error converting raw to nano");
      return;
    }

    raw = "100000000000000000000000000000";
    nano = NanoUtils.RawToNano(raw);
    if (!nano.Equals("0.1"))
    {
      Debug.Log("Error converting raw to nano");
      return;
    }

    raw = "10000000000000000000000000000";
    nano = NanoUtils.RawToNano(raw);
    if (!nano.Equals("0.01"))
    {
      Debug.Log("Error converting raw to nano");
      return;
    }

    raw = "100000000000000000000000000";
    nano = NanoUtils.RawToNano(raw);
    if (!nano.Equals("0.0001"))
    {
      Debug.Log("Error converting raw to nano");
      return;
    }

    raw = "100";
    nano = NanoUtils.RawToNano(raw);
    if (!nano.Equals("0.0000000000000000000000000001"))
    {
      Debug.Log("Error converting raw to nano");
      return;
    }

    raw = "1";
    nano = NanoUtils.RawToNano(raw);
    if (!nano.Equals("0.000000000000000000000000000001"))
    {
      Debug.Log("Error converting raw to nano");
      return;
    }

    // Nano to raw
    nano = "10.0";
    raw = NanoUtils.NanoToRaw(nano);
    if (!raw.Equals("10000000000000000000000000000000"))
    {
      Debug.Log("Error converting nano to raw");
      return;
    }

    nano = "10";
    raw = NanoUtils.NanoToRaw(nano);
    if (!raw.Equals("10000000000000000000000000000000"))
    {
      Debug.Log("Error converting nano to raw");
      return;
    }

    nano = "1.0";
    raw = NanoUtils.NanoToRaw(nano);
    if (!raw.Equals("1000000000000000000000000000000"))
    {
      Debug.Log("Error converting nano to raw");
      return;
    }

    nano = "0.1";
    raw = NanoUtils.NanoToRaw(nano);
    if (!raw.Equals("100000000000000000000000000000"))
    {
      Debug.Log("Error converting nano to raw");
      return;
    }

    nano = ".1";
    raw = NanoUtils.NanoToRaw(nano);
    if (!raw.Equals("100000000000000000000000000000"))
    {
      Debug.Log("Error converting nano to raw");
      return;
    }

    nano = "00000.1";
    raw = NanoUtils.NanoToRaw(nano);
    if (!raw.Equals("100000000000000000000000000000"))
    {
      Debug.Log("Error converting nano to raw");
      return;
    }

    nano = "0.01";
    raw = NanoUtils.NanoToRaw(nano);
    if (!raw.Equals("10000000000000000000000000000"))
    {
      Debug.Log("Error converting nano to raw");
      return;
    }

    nano = "0.000000000000000000000000000001";
    raw = NanoUtils.NanoToRaw(nano);
    if (!raw.Equals("1"))
    {
      Debug.Log("Error converting nano to raw");
      return;
    }

    // Test localization of using a comma
    nano = "0,01";
    raw = NanoUtils.NanoToRaw(nano);
    if (!raw.Equals("10000000000000000000000000000"))
    {
      Debug.Log("Error converting nano to raw");
      return;
    }

    // Test NanoAmount
    if (!((new NanoAmount("1") + new NanoAmount("2")).Equals(new NanoAmount("3")))) {
      Debug.Log("Error with adding");
      return;
    }

    if (!((new NanoAmount("2") - new NanoAmount("1")).Equals(new NanoAmount("1")))) {
      Debug.Log("Error with subtracting");
      return;
    }

    if (!(new NanoAmount("3000") > new NanoAmount("2000")))
    {
      Debug.Log("Error with greater");
      return;
    }

    if ((new NanoAmount("2000") > new NanoAmount("2000")))
    {
      Debug.Log("Error with greater");
      return;
    }

    if ((new NanoAmount("1999") > new NanoAmount("2000")))
    {
      Debug.Log("Error with greater");
      return;
    }

    if (!(new NanoAmount("3000") >= new NanoAmount("2000")))
    {
      Debug.Log("Error with greater or equal");
      return;
    }

    if (!(new NanoAmount("2000") >= new NanoAmount("2000")))
    {
      Debug.Log("Error with greater or equal");
      return;
    }

    if ((new NanoAmount("1999") > new NanoAmount("2000")))
    {
      Debug.Log("Error with greater or equal");
      return;
    }

    var amount = new NanoAmount(100);

    byte[] bytes = NanoUtils.HexStringToByteArray("E989DE925A4EDEE45447158557AD1409450315491F147F4AAA8F37DCA355354A");

    byte[] b = NanoUtils.AddressToPublicKeyByteArray("nano_3kqdiqmqiojr1aqqj51aq8bzz5jtwnkmhb38qwf3ppngo8uhhzkdkn7up7rp");
    string s1 = NanoUtils.SignHash("E989DE925A4EDEE45447158557AD1409450315491F147F4AAA8F37DCA355354A", bytes);
    string s2 = NanoUtils.PublicKeyToAddress(bytes);
    string s3 = NanoUtils.ByteArrayToHexString(bytes);

    var prvKey = NanoUtils.GeneratePrivateKey();
    var password = "cheese_cake" + new Random();
    var filename = "privateKey1.nano";

    NanoUtils.SavePrivateKey(NanoUtils.ByteArrayToHexString(prvKey), filename, password);
    var originalPrivateKey = NanoUtils.ByteArrayToHexString(prvKey);
    var extractedPrivateKey = NanoUtils.LoadPrivateKey(filename, password);
    if (!originalPrivateKey.Equals(extractedPrivateKey))
    {
      Debug.Log("Error decrypting privateKey");
      return;
    }

    Debug.Log("Successfully tested Utils");
  }

  // Update is called once per frame
  void Update()
  {

  }
}
