using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Fusion;
using System;

/// <summary>
/// Handles the user interface for the lobby scene in a multiplayer game.
/// This includes functionalities like creating rooms, joining rooms, setting a username,
/// and managing warning messages or errors. The script interacts with a NetworkManager to 
/// fetch available rooms and manage room-related actions.
/// </summary>
public class UIControllerLobbyScene : MonoBehaviour
{
    public static UIControllerLobbyScene instance;

    [Header("Main Panel Buttons")]
    [Tooltip("Button to open the 'Create Room' panel.")]
    [SerializeField] private Button createRoomPanelBtn;

    [Tooltip("Button to open the 'Join Room' panel.")]
    [SerializeField] private Button joinRoomBtn;

    [Space]
    [Tooltip("Main panel containing the initial options.")]
    public GameObject mainPanel;

    [Tooltip("Panel for creating a new room.")]
    public GameObject createRoomPanel;

    [Tooltip("Panel for joining an existing room.")]
    public GameObject joinRoomPanel;

    [Header("Create Room")]
    [Tooltip("Input field for entering the room name.")]
    [SerializeField] private TMP_InputField createRoomInputField;

    [Tooltip("Button to create a new room with the specified name.")]
    [SerializeField] private Button createRoomBtn;

    [Header("Join Room")]
    [Tooltip("Prefab used to display available rooms.")]
    [SerializeField] private GameObject roomPrefab;

    [Tooltip("Parent object to hold room prefabs.")]
    [SerializeField] private Transform roomPrefabPanel;

    [Header("Warning")]
    [Tooltip("Panel for displaying warning messages.")]
    public GameObject warningPanel;

    [Tooltip("Text field to display the warning message.")]
    [SerializeField] private TMP_Text warningTxt;

    [Tooltip("Button to close the warning panel.")]
    [SerializeField] private Button warningBtn;

    [Header("User Name")]
    [Tooltip("Panel for entering the user's name.")]
    public GameObject userNamePanel;

    [Tooltip("Input field for entering the username.")]
    [SerializeField] private TMP_InputField userNameInputfield;

    [Tooltip("Warning text displayed if the username field is empty.")]
    [SerializeField] private GameObject warningUserNameTxt;

    [Tooltip("Button to save the username.")]
    [SerializeField] private Button usernameSaveBtn;

    // Delegates and events
    public delegate void LobbyAction(string roomName);
    public delegate void SimpleAction();
    public static event LobbyAction OnCreateOrJoinRoom;

    // List to manage room UI objects
    private List<GameObject> roomList = new List<GameObject>();

    private const string username = "UserName";

    /// <summary>
    /// Unity Start method to initialize the singleton and set up event listeners.
    /// </summary>
    void Start()
    {
        instance = this;
        Init();
    }

    /// <summary>
    /// Sets up button listeners and subscribes to session list updates.
    /// </summary>
    void Init()
    {
        if (createRoomPanelBtn != null)
        {
            createRoomPanelBtn.onClick.AddListener(OnClickCreateRoomPanelButton);
        }

        if (joinRoomBtn != null)
        {
            joinRoomBtn.onClick.AddListener(OnClickJoinRoomPanelButton);
        }

        if (warningBtn != null)
        {
            warningBtn.onClick.AddListener(OnClickWarningMessage);
        }

        if (usernameSaveBtn != null)
        {
            usernameSaveBtn.onClick.AddListener(OnClickSetUserName);
        }

        if (createRoomBtn != null)
        {
            createRoomBtn.onClick.AddListener(() => {
                OnCreateOrJoinRoom?.Invoke(createRoomInputField.text);
                createRoomInputField.text = "";
            });
        }

        userNameInputfield.onValueChanged.AddListener(OnUserNameInputNoteEmpty);

        userNameInputfield.text = PlayerPrefs.GetString(username);
        if (!PlayerPrefs.HasKey(username))
        {
            userNamePanel.SetActive(true);
            string name = $"Player {UnityEngine.Random.Range(10, 100)}";
            userNameInputfield.text = name;
        }

        NetworkManager.SessionListUpdateAction += RoomList;
        NetworkManager.ShutdownAction += WarningMessage;
    }

    /// <summary>
    /// Removes all listeners from buttons.
    /// </summary>
    void Destroy()
    {
        if (createRoomPanelBtn != null)
        {
            createRoomPanelBtn.onClick.RemoveAllListeners();
        }

        if (joinRoomBtn != null)
        {
            joinRoomBtn.onClick.RemoveAllListeners();
        }

        if (warningBtn != null)
        {
            warningBtn.onClick.RemoveAllListeners();
        }

        if (usernameSaveBtn != null)
        {
            usernameSaveBtn.onClick.RemoveAllListeners();
        }

        if (createRoomBtn != null)
        {
            createRoomBtn.onClick.RemoveAllListeners();
        }

        NetworkManager.SessionListUpdateAction -= RoomList;
        NetworkManager.ShutdownAction -= WarningMessage;
    }

    /// <summary>
    /// Unity OnDestroy method to clean up listeners and resources.
    /// </summary>
    private void OnDestroy()
    {
        Destroy();
    }

    #region Buttons

    /// <summary>
    /// Opens the 'Create Room' panel and hides the 'Join Room' panel.
    /// </summary>
    void OnClickCreateRoomPanelButton()
    {
        createRoomPanel.SetActive(true);
        joinRoomPanel.SetActive(false);
    }

    /// <summary>
    /// Opens the 'Join Room' panel, fetches the room list, and hides the 'Create Room' panel.
    /// </summary>
    void OnClickJoinRoomPanelButton()
    {
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(true);
        RoomList();
    }

    /// <summary>
    /// Updates the room list UI by clearing the old list and populating it with available rooms.
    /// </summary>
    void RoomList()
    {
        if (!joinRoomPanel.activeSelf) return;
        if (roomList != null && roomList.Count > 0)
        {
            foreach (var room in roomList)
            {
                Destroy(room);
            }
            roomList.Clear();
        }

        Debug.Log($"Available rooms: {NetworkManager.Instance.availableRooms.Count}");

        if (NetworkManager.Instance.availableRooms == null || NetworkManager.Instance.availableRooms.Count <= 0)
        {
            NetworkManager.Instance.JoinLobbyAsync();
        }

        foreach (var sessionInfo in NetworkManager.Instance.availableRooms)
        {
            if (sessionInfo.PlayerCount == sessionInfo.MaxPlayers)
                continue;

            var roomObject = Instantiate(roomPrefab, roomPrefabPanel);
            roomObject.transform.GetChild(0).GetComponent<TMP_Text>().text = sessionInfo.Name;
            roomObject.transform.GetChild(1).GetComponent<TMP_Text>().text = $"{sessionInfo.PlayerCount}/{sessionInfo.MaxPlayers}";
            roomObject.GetComponent<Button>().onClick.AddListener(() => OnCreateOrJoinRoom?.Invoke(sessionInfo.Name));
            roomList.Add(roomObject);
        }
    }

    void OnUserNameInputNoteEmpty(string text)
    {
        warningUserNameTxt.SetActive(false);
    }

    public void OnClickWarningMessage()
    {
        warningPanel.SetActive(false);
        NetworkManager.LeaveRoomAction?.Invoke();
    }

    public void OnClickSetUserName()
    {
        if (string.IsNullOrEmpty(userNameInputfield.text))
        {
            warningUserNameTxt.SetActive(true);
        }
        else
        {
            PlayerPrefs.SetString(username, userNameInputfield.text);
            userNamePanel.SetActive(false);
        }
    }

    #endregion

    /// <summary>
    /// Closes all panels and hides the UI.
    /// </summary>
    public void Close()
    {
        mainPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
    }

    public void WarningMessage(string warningMsg)
    {
        warningPanel.SetActive(true);
        warningTxt.text = warningMsg;
    }
}
