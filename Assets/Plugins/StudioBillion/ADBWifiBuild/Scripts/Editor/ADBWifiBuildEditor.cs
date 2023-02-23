#if UNITY_EDITOR && UNITY_ANDROID
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Net;
using System.Net.Sockets;

public class ADBWifiBuildEditor : EditorWindow
{
    class Device
    {
        public string deviceID = "";
        public string deviceIP = "";
        public Device(string id)
        {
            deviceID = id;
            deviceIP = "";
        }
        public Device(string id, string ip)
        {
            deviceID = id;
            deviceIP = ip;
        }
    }
    private Texture2D m_Logo = null;
    static bool DeviceLayout = true;
    static List<Device> Devices = new List<Device>();
    static string pcIPAddress = "";
    static string IPMask = "";

    [MenuItem("StudioBillion/ADB Wifi Build")]
    public static void Init()
    {
        ADBWifiBuildEditor wifiBuildEditor = (ADBWifiBuildEditor)GetWindow(typeof(ADBWifiBuildEditor), true, "ADB Wifi Build Configuration");
        wifiBuildEditor.m_Logo = Resources.Load("SB_Logo", typeof(Texture2D)) as Texture2D;
        wifiBuildEditor.Show();
        IpUpdate();

        ADBCommand("tcpip 5555");
        wifiBuildEditor.GetConnectedDeviceIDs();

    }

    static void IpUpdate()
    {
        pcIPAddress = GetLocalIPAddress();
        IPMask = pcIPAddress.Substring(0, 8);
        //Debug.Log(pcIPAddress);
        //Debug.Log(IPMask);
    }

    static string GetLocalIPAddress()
    {
        if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            EditorUtility.DisplayDialog("StudioBillion ADB WIFI Connection", "No network adapters with an IPv4 address in the system!", "OK");
            return "";
        }
        else
        {
            EditorUtility.DisplayDialog("StudioBillion ADB WIFI Connection", "No network adapters with an IPv4 address in the system!", "OK");
            return "";
        }
    }

    void OnGUI()
    {
        #region LOGO
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(m_Logo))
        {
            Application.OpenURL("https://assetstore.unity.com/publishers/51907");
        }
        GUILayout.EndHorizontal();
        #endregion

        GUILayout.Space(40);
        if (DeviceLayout)
        {
            int devCount = Devices.Count;
            GUILayout.BeginHorizontal();

            #region ID Column
            GUILayout.BeginVertical();

            GUILayout.Label("Device ID");
            for (int i = 0; i < devCount; i++)
            {
                GUILayout.Space(20);
                GUILayout.Label(Devices[i].deviceID);
            }
            GUILayout.EndVertical();
            #endregion

            #region IP Column

            GUILayout.BeginVertical();
            GUILayout.Label("Device IP");
            for (int i = 0; i < devCount; i++)
            {
                GUILayout.Space(20);
                GUILayout.Label(Devices[i].deviceIP);
            }
            GUILayout.EndVertical();
            #endregion

            #region Buttons Column
            GUILayout.BeginVertical();
            GUILayout.Label("");
            for (int i = 0; i < devCount; i++)
            {
                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                if (Devices[i].deviceIP != "")
                {
                    if (ADBCommand("devices -l").Contains(Devices[i].deviceIP))
                    {
                        if (GUILayout.Button("Disconnect from Device"))
                        {
                            ADBCommand("kill-server");
                        }
                        if (GUILayout.Button("Delete and Disconnect from Device"))
                        {
                            ADBCommand("kill-server");
                            Devices.Remove(Devices[i]);
                            return;
                        }
                    }
                    else if (IPMask != "" && Devices[i].deviceIP.Contains(IPMask))//mask 192.168
                    {
                        if (GUILayout.Button("Connect to Device"))
                        {
                            ConnectViaWifi(Devices[i]);
                        }
                        if (GUILayout.Button("X"))
                        {
                            Devices.Remove(Devices[i]);
                            return;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Try To Find IP Addres"))
                        {
                            TryToFindIPAddress(Devices[i]);
                        }
                        if (GUILayout.Button("X"))
                        {
                            Devices.Remove(Devices[i]);
                            return;
                        }
                    }
                }
                else
                {
                    if (Devices[i].deviceID == "")
                    {
                        Devices.Remove(Devices[i]);
                        return;
                    }
                    else
                    {
                        if (GUILayout.Button("Try To Find IP Addres"))
                        {
                            TryToFindIPAddress(Devices[i]);

                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            #endregion

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Devices.Count == 0)
            {
                GUILayout.Space(40);
                GUILayout.Label("There is no device.");
                GUILayout.Space(40);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(40);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Devices List"))
            {
                GetConnectedDeviceIDs();
            }
            GUILayout.EndHorizontal();
        }
    }
    void ConnectViaWifi(Device device)
    {
        string result = ADBCommand("connect " + device.deviceIP);
        EditorUtility.DisplayDialog("StudioBillion ADB WIFI Connection", result, "OK");
    }

    static string ADBCommand(string arguments)
    {
        var p = new Process();
        p.StartInfo.FileName = UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath + "/platform-tools/adb.exe";
        p.StartInfo.Arguments = arguments;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        return p.StandardOutput.ReadToEnd();
    }

    void GetConnectedDeviceIDs()
    {
        string devices = ADBCommand("devices");
        string[] sepDevices = devices.Split('\n');

        for (int i = 1; i < sepDevices.Length - 1; i++)
        {

            string[] temp = sepDevices[i].Split('\t');

            if (isAlreadySaved(temp[0]) || temp[0] == "\r")
                continue;

            Device dev = new Device(temp[0], temp[1]);
            if (dev.deviceIP != "device")
                TryToFindIPAddress(dev);
            if (!isAlreadyWifiConnected(dev.deviceIP))
                Devices.Add(dev);

            if (sepDevices[i + 1].Length < 3)
                return;
        }
    }
    bool isAlreadySaved(string DeviceID)
    {
        bool result = false;
        for (int i = 0; i < Devices.Count; i++)
        {
            result = (Devices[i].deviceID == DeviceID);
            if (result)
                return result;
        }
        return result;
    }
    bool isAlreadyWifiConnected(string DeviceIP)
    {
        for (int i = 0; i < Devices.Count; i++)
        {
            if (Devices[i].deviceIP.Contains(DeviceIP))
                return true;
        }
        return false;
    }


    static void TryToFindIPAddress(Device device)
    {
        try
        {
            if (IPMask == "")
                IpUpdate();
            if (IPMask == "")
                return;
            string ipWlan = ADBCommand("-s " + device.deviceID + " shell ip addr show wlan0");
            string[] lines = ipWlan.Split('\n');
            string ipAddress = (lines[2].Substring(lines[2].IndexOf(IPMask), 15)).Split('/')[0];
            //return ipAddress;
            device.deviceIP = ipAddress;
        }
        catch
        {
            EditorUtility.DisplayDialog("StudioBillion ADB WIFI Connection", "(1) Please Connect Your Device via USB,\n(2) Connect Your Device and PC to the same Wireless Network\n\nand try again!", "OK");
        }
    }
}
#endif