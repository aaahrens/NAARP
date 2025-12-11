using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages lobby-specific state such as the list of players, 
/// readiness, and whether the game can start.
/// </summary>
public partial class NetworkLobby : Node
{
    /// <summary>
    /// Snapshot of the current lobby state for this process.
    /// </summary>
    public LobbyState LobbyState { get; private set; } = new LobbyState();

    /// <summary>
    /// Event raised whenever the lobby state changes. Subscribers such as
    /// UI scripts should refresh themselves when this event fires.
    /// </summary>
    public event Action<LobbyState> LobbyUpdated;

    /// <summary>
    /// Internal mapping of peer IDs to their corresponding lobby players.
    /// This is treated as the authoritative source of truth used to build State.
    /// </summary>
    private readonly Dictionary<int, LobbyPlayer> playersByPeer = new();

    /// <summary>
    /// Tracks whether the host player has been added to the lobby.
    /// </summary>
    private bool hostAdded = false;

    public override void _Ready()
    {
        // Listen to peer events directly; this keeps NetworkManager decoupled.
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
    }

    public override void _Process(double delta)
    {
        // On the server we need to add the host to the lobby once the multiplayer peer is ready.
        if (!hostAdded && Multiplayer.IsServer() && Multiplayer.MultiplayerPeer != null)
        {
            int hostPeerId = Multiplayer.GetUniqueId();
            AddPlayer(hostPeerId, isHost: true);
            hostAdded = true;
        }
    }

    /// <summary>
    /// Handles notification that a new peer has joined.
    /// Only the server mutates the authoritative player list;
    /// clients will receive snapshots via RPC.
    /// </summary>
    /// <param name="id">Peer ID of the connected client.</param>
    private void OnPeerConnected(long id)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        int peerId = (int)id;

        bool isHost = false;
        AddPlayer(peerId, isHost);
    }

    /// <summary>
    /// Handles notification that a peer has disconnected.
    /// Only the server mutates the authoritative player list;
    /// clients will receive snapshots via RPC.
    /// </summary>
    /// <param name="id">Peer ID of the disconnected client.</param>
    private void OnPeerDisconnected(long id)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        int peerId = (int)id;
        RemovePlayer(peerId);
    }

    /// <summary>
    /// Adds a new player to the lobby and rebuilds the lobby state snapshot.
    /// </summary>
    /// <param name="peerId">Unique peer identifier for the new player.</param>
    /// <param name="isHost">Indicates whether this player is the lobby host.</param>
    private void AddPlayer(int peerId, bool isHost)
    {
        var lobbyPlayer = new LobbyPlayer
        {
            PeerId = peerId,
            Name = $"Player {peerId}",
            IsReady = false,
            IsHost = isHost
        };

        playersByPeer[peerId] = lobbyPlayer;
        RebuildState();
    }

    /// <summary>
    /// Removes a player from the lobby and rebuilds the lobby state snapshot.
    /// </summary>
    /// <param name="peerId">Unique peer identifier of the player leaving the lobby.</param>
    private void RemovePlayer(int peerId)
    {
        if (playersByPeer.Remove(peerId))
        {
            RebuildState();
        }
    }

    /// <summary>
    /// Sets the ready state for the local player. On the server this directly
    /// updates the authoritative lobby data. On a client this is forwarded
    /// to the server via RPC.
    /// </summary>
    /// <param name="ready">Whether the local player should be marked as ready.</param>
    public void SetReady_Local(bool ready)
    {
        if (Multiplayer.IsServer())
        {
            int localPeerId = Multiplayer.GetUniqueId();
            SetReady_Server(localPeerId, ready);
        }
        else
        {
            Rpc(nameof(RpcSetReady), ready);
        }
    }

    /// <summary>
    /// RPC endpoint invoked by clients to request that their ready state be updated.
    /// </summary>
    /// <param name="ready">Whether the requesting player should be marked as ready.</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcSetReady(bool ready)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        int senderPeerId = Multiplayer.GetRemoteSenderId();
        SetReady_Server(senderPeerId, ready);
    }

    /// <summary>
    /// Applies a ready state change to the authoritative lobby data on the server
    /// and rebuilds the lobby state snapshot.
    /// </summary>
    /// <param name="peerId">Peer ID of the player whose ready state is changing.</param>
    /// <param name="ready">New ready state for the player.</param>
    private void SetReady_Server(int peerId, bool ready)
    {
        if (!playersByPeer.TryGetValue(peerId, out LobbyPlayer lobbyPlayer))
        {
            return;
        }

        lobbyPlayer.IsReady = ready;
        RebuildState();
    }

    /// <summary>
    /// Rebuilds the LobbyState snapshot from the authoritative lobby player dictionary
    /// and notifies any listeners via the LobbyUpdated event.
    /// This is called whenever player membership or ready state changes.
    /// If running on the server, the updated snapshot is also broadcast to all clients
    /// so their local lobby views stay in sync.
    /// </summary>
    private void RebuildState()
    {
        // Build a new snapshot from the authoritative playersByPeer dictionary.
        var newState = new LobbyState();

        foreach (KeyValuePair<int, LobbyPlayer> pair in playersByPeer)
        {
            LobbyPlayer sourcePlayer = pair.Value;

            // Create a shallow copy so UI code cannot mutate the authoritative objects.
            var snapshotPlayer = new LobbyPlayer
            {
                PeerId = sourcePlayer.PeerId,
                Name = sourcePlayer.Name,
                IsReady = sourcePlayer.IsReady,
                IsHost = sourcePlayer.IsHost
            };

            newState.Players.Add(snapshotPlayer);
        }

        // Determine the local peer ID for this process, if any.
        int localPeerId = Multiplayer.MultiplayerPeer != null
            ? Multiplayer.GetUniqueId()
            : 0;

        if (localPeerId != 0 && playersByPeer.TryGetValue(localPeerId, out LobbyPlayer localPlayer))
        {
            newState.LocalIsHost = localPlayer.IsHost;
            newState.LocalIsReady = localPlayer.IsReady;
        }
        else
        {
            newState.LocalIsHost = false;
            newState.LocalIsReady = false;
        }

        newState.CanStartGame = CanStartGame(newState);

        // Replace the current snapshot and notify listeners on this process.
        LobbyState = newState;
        LobbyUpdated?.Invoke(LobbyState);

        // If this instance is the server, also push the updated snapshot to all clients.
        if (Multiplayer.IsServer())
        {
            BroadcastLobbyStateToClients();
        }
    }

    /// <summary>
    /// Constructs a serializable representation of the current lobby player dictionary
    /// and sends it to all connected clients via RPC so they can rebuild their local state.
    /// Only called on the server.
    /// </summary>
    private void BroadcastLobbyStateToClients()
    {
        var playersArray = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (KeyValuePair<int, LobbyPlayer> pair in playersByPeer)
        {
            LobbyPlayer player = pair.Value;

            var playerDict = new Godot.Collections.Dictionary
        {
            { "PeerId", player.PeerId },
            { "Name", player.Name },
            { "IsReady", player.IsReady },
            { "IsHost", player.IsHost }
        };

            playersArray.Add(playerDict);
        }

        // Send the snapshot to all peers; the server will also receive this,
        // but the handler early-exits on the server side.
        Rpc(nameof(RpcApplyLobbySnapshot), playersArray);
    }

    /// <summary>
    /// RPC endpoint used by the server to deliver a lobby snapshot to clients.
    /// Reconstructs the authoritative lobbyPlayersByPeer dictionary on the client
    /// and then rebuilds the LobbyState snapshot locally.
    /// </summary>
    /// <param name="players">
    /// Array of dictionaries representing each player in the lobby, including
    /// peer ID, name, ready flag, and host status.
    /// </param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcApplyLobbySnapshot(Godot.Collections.Array<Godot.Collections.Dictionary> players)
    {
        // The server already has the authoritative state; no need to apply the snapshot there.
        if (Multiplayer.IsServer())
        {
            return;
        }

        playersByPeer.Clear();

        foreach (Godot.Collections.Dictionary playerDict in players)
        {
            // Godot may box integer values as long when passing through Variant.
            int peerId = (int)(long)playerDict["PeerId"];
            string name = (string)playerDict["Name"];
            bool isReady = (bool)playerDict["IsReady"];
            bool isHost = (bool)playerDict["IsHost"];

            var lobbyPlayer = new LobbyPlayer
            {
                PeerId = peerId,
                Name = name,
                IsReady = isReady,
                IsHost = isHost
            };

            playersByPeer[peerId] = lobbyPlayer;
        }

        // Rebuild the local snapshot and notify any listeners (e.g. UI).
        RebuildState();
    }

    /// <summary>
    /// Determines if the game is ready to start.
    /// </summary>
    /// <param name="state">Lobby state snapshot to evaluate.</param>
    /// <returns>True if the game can start; otherwise false.</returns>
    private bool CanStartGame(LobbyState state)
    {
        return state.Players.Count > 0 &&
               state.Players.TrueForAll(p => p.IsReady);
    }

    /// <summary>
    /// Requests that the game start based on the current lobby state.
    /// On a client, this sends an RPC to the server, and on the server
    /// it validates and, if valid, transitions into the gameplay scene.
    /// </summary>
    public void RequestStartGame()
    {
        if (Multiplayer.IsServer())
        {
            TryStartGameOnServer();
        }
        else
        {
            Rpc(nameof(RpcRequestStartGame));
        }
    }

    /// <summary>
    /// RPC endpoint for clients to request that the server attempt to start the game.
    /// Only the current host is allowed to trigger the start.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcRequestStartGame()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        int senderPeerId = Multiplayer.GetRemoteSenderId();

        if (!playersByPeer.TryGetValue(senderPeerId, out LobbyPlayer lobbyPlayer) || !lobbyPlayer.IsHost)
        {
            GD.Print("NetworkLobby: Non-host attempted to start the game; ignoring.");
            return;
        }

        TryStartGameOnServer();
    }

    /// <summary>
    /// Validates the current lobby state and, if all conditions are met,
    /// transitions the session into the gameplay scene for all peers.
    /// </summary>
    private void TryStartGameOnServer()
    {
        if (!LobbyState.CanStartGame)
        {
            GD.Print("NetworkLobby: Cannot start game; not all players are ready.");
            return;
        }

        GD.Print("NetworkLobby: Starting game...");

        // TODO: Replace with your actual gameplay scene path.
        string gameScenePath = "res://Scenes/Game.tscn";

        GetTree().ChangeSceneToFile(gameScenePath);
    }

}
