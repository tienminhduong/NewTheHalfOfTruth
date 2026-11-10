using System.Runtime.InteropServices;
using UnityEngine;

public class ClipboardManager : MonoBehaviour
{
    // Import the native JavaScript function from our .jslib file
    [DllImport("__Internal")]
    private static extern void CopyToClipboard(string text);

    /// <summary>
    /// Copies the provided text to the user's system clipboard.
    /// </summary>
    public void CopyText(string textToCopy)
    {
        // Always check if we are running in WebGL before calling an __Internal plugin
        #if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboard(textToCopy);
        #else
            // Fallback for the Unity Editor or other platforms (Standalone PC, Mobile, etc.)
            GUIUtility.systemCopyBuffer = textToCopy;
            Debug.Log($"[Fallback] Copied to clipboard: {textToCopy}");
        #endif
    }
}
