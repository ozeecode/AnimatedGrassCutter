#if UNITY_EDITOR && UNITY_ANDROID
using System.Diagnostics;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;
using Debug = UnityEngine.Debug;
[InitializeOnLoad]
public class ADBScreenRecorderEditor : EditorWindow
{
#region Parameters
    public struct ADBRecording
    {
        public RecordState recordState; // State that if obile phone is recording a screen video
        public string recordName; // MP4 file name
        public string recordPath; // MP4 file location that exported on current machine
    }
    public ADBRecording recording;
    public enum RecordState { Recording, Stopped }

    public static string sdkPath; // SDK Path as string that come from Unity Editor Installed (as Module) or System Environment Path (from external)
    public static bool isADBFound = false; // Check if pc has ADB installed
    public Process p; // Cached terminal process to use out of scope
#endregion
#region Defaults
    [MenuItem("StudioBillion/ADB Screen Recorder", false, 102)]
    public static void ShowWindow()
    {
        sdkPath = UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath + "/platform-tools/adb";
        if (string.IsNullOrEmpty(UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath))
        {
            if (System.Environment.GetEnvironmentVariable("PATH").Contains("platform-tools"))
            {
                foreach (string path in System.Environment.GetEnvironmentVariable("PATH").Split(';'))
                    if (path.Contains("platform-tools"))
                        sdkPath = path + "/adb";
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "ADB not installed, install ADB and try again", "OK");
                isADBFound = true;
            }
        }
        Debug.Log("SDK Path: " + sdkPath);
        if (!string.IsNullOrEmpty(sdkPath))
        {
            ADBScreenRecorderEditor window = ((ADBScreenRecorderEditor)GetWindow(typeof(ADBScreenRecorderEditor), true, "ADB Screen Recorder"));

            { // Initialize recording struct's default values
                window.recording.recordName = "";
                window.recording.recordPath = "";
                window.recording.recordState = RecordState.Stopped;
            }
            window.minSize = new Vector2(350, 450);

            { // Set current window to the center of resolution
                var position = window.position;
                position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
                window.position = position;
                window.Show();
            }
        }
    }
    void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        EditorGUI.BeginDisabledGroup(recording.recordState == RecordState.Recording && !isADBFound);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(
            new GUIContent("Start Record", (Resources.Load("ADBScreenRecord_Start") as Texture2D)),
            GUILayout.MaxHeight(100),
            GUILayout.MinHeight(20),
            GUILayout.MaxWidth(300),
            GUILayout.MinWidth(100)))
            StartButton();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
        GUILayout.Space(50);
        EditorGUI.BeginDisabledGroup(recording.recordState == RecordState.Stopped && !isADBFound);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(
            new GUIContent("Stop Record", (Resources.Load("ADBScreenRecord_Stop") as Texture2D)),
            GUILayout.MaxHeight(100),
            GUILayout.MinHeight(20),
            GUILayout.MaxWidth(300),
            GUILayout.MinWidth(100)))
            StopButton();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
        GUILayout.Space(50);
        EditorGUI.BeginDisabledGroup(recording.recordPath.Length == 0 || recording.recordState == RecordState.Recording && !isADBFound);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(
            new GUIContent("  Check and Download \n On Computer", (Resources.Load("ADBScreenRecord_Download") as Texture2D)),
            GUILayout.MaxHeight(100),
            GUILayout.MinHeight(20),
            GUILayout.MaxWidth(300),
            GUILayout.MinWidth(100)))
            CheckFileExist();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }
#endregion
#region Buttons
    public void StartButton() // On Start Button Click
    {
        if (recording.recordState == RecordState.Stopped)
        {
            if (ADBCommand(sdkPath, "devices -l").Contains("product:"))
            {
                new Thread(StartRecording).Start();
                Debug.Log("Recording Started");
            }
            else
                EditorUtility.DisplayDialog("Record", "No Connected Device", "OK");
        }
        else
            EditorUtility.DisplayDialog("Record", "You Are Already Recording", "OK");
    }
    public void StopButton() // Kill process and Stop Recording
    {
        Debug.Log("Recording Stopped");
        if (p != null)
        {
            p.Kill();
            p = null;
        }
        else
            EditorUtility.DisplayDialog("Record", "If You Want Stop Record You Must Start it First :)", "OK");
    }
    public void CloseWindow()
    {
        Close();
    }
#endregion
#region Utility Functions
    public void StartRecording() // Send Record Command via terminal and wait until get killed
    {
        string storagePath = ADBCommand(sdkPath, "shell echo $EXTERNAL_STORAGE").Trim() + "/";
        recording.recordState = RecordState.Recording;
        recording.recordName = System.DateTime.Now.ToLongDateString().Replace(" ", "_").Replace(",", "") + "-" + System.DateTime.Now.ToLongTimeString().Replace(":", "_").Replace(" ", "_") + ".mp4";
        recording.recordPath = storagePath + recording.recordName;

        p = new Process()
        {
            EnableRaisingEvents = true,
            StartInfo = new ProcessStartInfo()
            {
                FileName = sdkPath,
                Arguments = "shell screenrecord " + recording.recordPath,
                RedirectStandardOutput = false,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            }
        };
        p.Exited += P_Exited;
        p.Start();
        p.WaitForExit(int.MaxValue);
    }

    private void P_Exited(object sender, System.EventArgs e) // On Record Command Kill
    {
        recording.recordState = RecordState.Stopped;
    }

    string ADBCommand(string fileName, string arguments) // General Methods for Send Command to cmd
    {
        Process p = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        p.Start();
        return p.StandardOutput.ReadToEnd();
    }

    public void CheckFileExist() // Check File Exist in Android Phone
    {
        if (recording.recordPath.Length != 0 && recording.recordState == RecordState.Stopped)
        {
            if (ADBCommand(sdkPath, "shell ls " + recording.recordPath) != "")
                DownloadOnComputer(EditorUtility.OpenFolderPanel("Record", "", ""));
            else
                EditorUtility.DisplayDialog("Record", "Record Doesn't Saved Yet, Please Try Again", "OK");
        }
        else
            EditorUtility.DisplayDialog("Record", "First, You Need Record :)", "OK");
    }

    public void DownloadOnComputer(string savePath) // send command cmd for Copy file from phone to PC
    {
        ADBCommand(sdkPath, "pull " + recording.recordPath + " " + savePath);
        if (EditorUtility.DisplayDialog("Record", "Should Play Video or Open Folder ? ", "Open Video", "Open Folder"))
        {
            Process p = new Process();
#if UNITY_EDITOR_OSX
            p.StartInfo.FileName = "open";
            p.StartInfo.Arguments = savePath + "/" + recording.recordName;
#else
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = @savePath.Replace("/", "\\") + @"\" + recording.recordName.Replace("/", "\\");
#endif
            p.Start();
        }
        else
        {
            Process p = new Process();

#if UNITY_EDITOR_OSX
            p.StartInfo.FileName = "open";
            p.StartInfo.Arguments = savePath;
#else
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = "/select," + "\"" + @savePath.Replace("/", "\\") + @"\" + recording.recordName.Replace("/", "\\") + "\"";
#endif
            p.Start();
        }
    }
#endregion
}
#endif