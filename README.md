# Multiplayer Character Controller with Photon Fusion

## Overview

This project is a Unity-based multiplayer game using Photon Fusion for real-time networking. It allows two players to join the same room and synchronize their movements (walk, run, and jump) in real-time.

---

## Project Structure

```
/Assets
    /Animations
    /Models
    /Photon
    /Prefabs
    /Scenes
    /Scripts
        /GamePlay
        /Networking
    /Settings
```

### Folder Descriptions
- **Scripts**: Contains all C# scripts for networking and gameplay mechanics.
- **Prefabs**: Prefabs for the player and UI components.
- **Scenes**: Contains the lobby and game scenes.
- **Photon**: Configuration files for Photon Fusion.
- **Models**: 3D models used in the game (e.g., the player model).
- **Animations**: Contains animation files for player actions.

---

## How the Code Works

- **NetworkManager**: Handles room creation, joining, and player connections.
- **FollowCamera**: Controls the player follow camera.
- **ThirdPersonController**: Handles walking, jumping, and animations, along with character movement control using the Character Controller.
- **UIControllerLobbyScene**: Manages all UI elements in the Lobby scene, such as the Create and Join buttons.
- **UIControllerGameScene**: Manages all UI elements in the Game scene.

---

## Setup Instructions

### Prerequisites
- **Unity**: Version `6000.0.24f1`
- **Photon Fusion SDK**: Available via the Unity Asset Store or Photon Fusion website.
- **Photon Account**: Create an account at [Photon Engine](https://www.photonengine.com/) and get an App ID.

### Steps to Run
1. Clone the Git repository:
   ```bash
   git clone <repository-url>
   ```
2. Open the project in Unity.
3. Import the Photon Fusion SDK (if not included).
4. Configure Photon:
   - Navigate to `/Photon/PhotonAppSettings.asset`.
   - Paste your Photon App ID.
5. Build and Run:
   - Add `LobbyScene` and `GameScene` scenes in Build Settings.
   - Build and launch the EXE file.

---

## Controls
- **Movement**: `WASD` or Arrow Keys
- **Run**: Hold `Shift`
- **Jump**: Press `Spacebar`

---
