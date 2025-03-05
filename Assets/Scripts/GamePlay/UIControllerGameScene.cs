using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

/// <summary>
/// Controls the UI elements in the game scene, such as the leave room button
/// and the warning panel. Also manages event subscriptions related to network actions.
/// </summary>
public class UIControllerGameScene : MonoBehaviour
{
    [Header("Leave Room")]
    [Tooltip("Button to leave the current room.")]
    [SerializeField] private Button leaveRoomBtn;

    [Header("Warning")]
    [Tooltip("Panel to display warning messages.")]
    public GameObject warningPanel;

    [Tooltip("Text field to display warning messages.")]
    [SerializeField] private TMP_Text warningTxt;

    [Tooltip("Button to acknowledge and close the warning panel.")]
    [SerializeField] private Button warningBtn;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Called when the object is being destroyed
    private void OnDestroy()
    {
        Destroy();
    }

    /// <summary>
    /// Initializes the UI by setting up button listeners and subscribing to necessary events.
    /// </summary>
    void Init()
    {
        // Subscribe to network shutdown events to display warnings
        NetworkManager.ShutdownAction += WarningMessage;

        // Set up the leave room button's action
        if (leaveRoomBtn != null)
        {
            leaveRoomBtn.onClick.AddListener(() => NetworkManager.LeaveRoomAction?.Invoke());
        }

        // Set up the warning button's action
        if (warningBtn != null)
        {
            warningBtn.onClick.AddListener(OnClickWarningMessage);
        }
    }

    /// <summary>
    /// Cleans up event subscriptions and removes button listeners to avoid memory leaks.
    /// </summary>
    void Destroy()
    {
        // Unsubscribe from network shutdown events
        NetworkManager.ShutdownAction -= WarningMessage;

        // Remove all listeners from the leave room button
        if (leaveRoomBtn != null)
        {
            leaveRoomBtn.onClick.RemoveAllListeners();
        }

        // Remove all listeners from the warning button
        if (warningBtn != null)
        {
            warningBtn.onClick.RemoveAllListeners();
        }
    }

    #region Buttons

    /// <summary>
    /// Handles the click event for the warning button. Hides the warning panel
    /// and triggers the action to leave the room.
    /// </summary>
    public void OnClickWarningMessage()
    {
        warningPanel.SetActive(false);
        NetworkManager.LeaveRoomAction?.Invoke();
    }

    #endregion

    /// <summary>
    /// Displays a warning message in the warning panel.
    /// </summary>
    /// <param name="warningMsg">The warning message to display.</param>
    public void WarningMessage(string warningMsg)
    {
        warningPanel.SetActive(true);
        warningTxt.text = warningMsg;
    }
}
