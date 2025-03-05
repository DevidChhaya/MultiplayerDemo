using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages Fusion lobby operations, including creating or joining rooms, handling player connections, and managing callbacks.
/// </summary>
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    /// <summary>
    /// Singleton instance of the FusionLobbyManager.
    /// </summary>
    public static NetworkManager Instance;

    [Tooltip("Player prefab to instantiate in the game.")]
    public NetworkPrefabRef playerPrefab;

    public NetworkRunner networkRunner;

    /// <summary>
    /// Dictionary to store player references and their associated names.
    /// </summary>
    private Dictionary<PlayerRef, string> players = new Dictionary<PlayerRef, string>();

    /// <summary>
    /// List of available rooms in the lobby.
    /// </summary>
    public List<SessionInfo> availableRooms = new List<SessionInfo>();

    /// <summary>
    /// Action invoked when the session list is updated.
    /// </summary>
    public static Action SessionListUpdateAction;

    public static Action<string> ShutdownAction;

    public static Action LeaveRoomAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        networkRunner = gameObject.AddComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;
        networkRunner.AddCallbacks(this);
    }

    private void Start()
    {
        UIControllerLobbyScene.OnCreateOrJoinRoom += CreateOrJoinRoom;
        LeaveRoomAction += LeaveRoom;

        JoinLobbyAsync();
    }

    /// <summary>
    /// Asynchronously joins the shared lobby.
    /// </summary>
    public async void JoinLobbyAsync()
    {
        var result = await networkRunner.JoinSessionLobby(SessionLobby.Shared, "default");

        if (result.Ok)
        {
            Debug.Log("Successfully joined the lobby.");
        }
        else
        {
            Debug.LogError($"Failed to join the lobby: {result.ShutdownReason}");
        }
    }

    private void OnDestroy()
    {
        UIControllerLobbyScene.OnCreateOrJoinRoom -= CreateOrJoinRoom;
        LeaveRoomAction -= LeaveRoom;
    }

    /// <summary>
    /// Leaves the current Fusion room and transitions to another state.
    /// </summary>
    public void LeaveRoom()
    {
        if (networkRunner != null && networkRunner.IsRunning)
        {
            Debug.Log("Leaving the Fusion room...");
            foreach (var playerObject in networkRunner.ActivePlayers)
            {
                if (networkRunner.GetPlayerObject(playerObject) != null)
                {
                    networkRunner.Despawn(networkRunner.GetPlayerObject(playerObject));
                }
            }
            networkRunner.Shutdown();

        }
        else
        {
            Debug.LogWarning("Cannot leave the room. NetworkRunner is not active.");
        }
        // Optionally transition to the main menu or another scene.
        SceneManager.LoadScene("LobbyScene");
    }

    /// <summary>
    /// Initiates the creation or joining of a room.
    /// </summary>
    /// <param name="roomName">The name of the room to create or join.</param>
    private void CreateOrJoinRoom(string roomName)
    {
        Debug.Log($"Creating room: {roomName}");
        CreateRoomAsync(roomName);
    }

    /// <summary>
    /// Asynchronously creates or joins a room with the specified name.
    /// </summary>
    /// <param name="roomName">The name of the room.</param>
    public async void CreateRoomAsync(string roomName)
    {
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
            networkRunner.ProvideInput = true;
            networkRunner.AddCallbacks(this);
        }

        var startGameArgs = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            PlayerCount = 2,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            MatchmakingMode = Fusion.Photon.Realtime.MatchmakingMode.FillRoom
        };

        var result = await networkRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            //UIController.instance.Close();
            LoadGameScene();
            Debug.Log($"Joined or created room: {startGameArgs.SessionName}");
        }
        else
        {
            Debug.LogError($"Failed to start game: {result.ShutdownReason}");
        }
    }

    /// <summary>
    /// Loads the game scene after joining a room.
    /// </summary>
    private void LoadGameScene()
    {
        //networkRunner.LoadScene("GameScene");
        if (networkRunner.IsSharedModeMasterClient)
        {
            networkRunner.LoadScene("GameScene");
        }
    }


    #region All Callbacks

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("OnConnectedToServer: Connected to server.");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.Log($"OnConnectFailed: Failed to connect. Address: {remoteAddress}, Reason: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        Debug.Log($"OnConnectRequest: Connect request received from {request.RemoteAddress}");
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        Debug.Log("OnCustomAuthenticationResponse: Custom authentication response received.");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"OnDisconnectedFromServer: Disconnected from server. Reason: {reason}");
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("OnHostMigration: Host migration detected.");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (runner.LocalPlayer == null) return;

        NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);
        if (playerObject != null)
        {
            NetworkInputData data = new NetworkInputData
            {
                Horizontal = Input.GetAxis("Horizontal"),
                Vertical = Input.GetAxis("Vertical"),
                Jump = Input.GetButton("Jump"),
                Crouch = Input.GetKeyDown(KeyCode.LeftControl),
                Sprint = Input.GetButton("Fire3")
            };

            input.Set(data);
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        Debug.Log($"OnInputMissing: Input missing for Player {player.PlayerId}");
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        Debug.Log($"OnObjectEnterAOI: Object {obj.name} entered AOI of Player {player.PlayerId}");
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        Debug.Log($"OnObjectExitAOI: Object {obj.name} exited AOI of Player {player.PlayerId}");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        players[player] = $"Player {player.PlayerId}";
        Debug.Log($"OnPlayerJoined: Player {player.PlayerId} joined.");
        foreach (var item in players)
        {
            Debug.Log($"Player Key: {item.Key}, Player Value: {item.Value}");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        players.Remove(player);
        Debug.Log($"OnPlayerLeft: Player {player.PlayerId} left.");
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        Debug.Log($"OnReliableDataProgress: Player {player.PlayerId}, Key {key}, Progress: {progress}");
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        Debug.Log($"OnReliableDataReceived: Data received from Player {player.PlayerId}, Key: {key}");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("OnSceneLoadDone: Scene load completed.");

        Debug.Log($"Scene {SceneManager.GetActiveScene().name} loaded.");

        // Spawn the player only in the GameScene
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            if (runner.GetPlayerObject(runner.LocalPlayer) == null)
            {
                Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-5, 5), 0, UnityEngine.Random.Range(-5, 5));
                NetworkObject networkObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, runner.LocalPlayer);
                runner.SetPlayerObject(runner.LocalPlayer, networkObject);
            }
            else
            {
                Debug.LogWarning("Player object already exists. No need to respawn.");
            }
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("OnSceneLoadStart: Scene load started.");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"OnSessionListUpdated: Session list count: {sessionList.Count}");
        foreach (var session in sessionList)
        {
            Debug.Log($"Session: {session.Name}, PlayerCount: {session.PlayerCount}");
        }
        availableRooms = sessionList;

        SessionListUpdateAction?.Invoke();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"OnShutdown: Runner shutdown. Reason: {shutdownReason}");
        if(shutdownReason != ShutdownReason.Ok)
        {
            ShutdownAction?.Invoke(shutdownReason.ToString());
        }
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        Debug.Log("OnUserSimulationMessage: Simulation message received.");
    }

    #endregion
}
