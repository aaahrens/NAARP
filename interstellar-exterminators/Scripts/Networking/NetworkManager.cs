using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages multiplayer session lifecycle including server/host/client startup,
/// peer connection handling, and spawning networked player entities.
/// </summary>
public partial class NetworkManager : Node
{
    /// <summary>
    /// Scene used to instantiate player entities for connected peers.
    /// </summary>
    [Export]
    public PackedScene PlayerScene;

    /// <summary>
    /// High-level manager responsible for maintaining lobby state.
    /// NetworkManager notifies this manager of connection events and 
    /// ready-state changes, and the LobbyManager issues updates for the UI.
    /// </summary>
    public NetworkLobby NetworkLobby { get; private set; } = new NetworkLobby();

    private ENetMultiplayerPeer peer;
    private readonly Dictionary<int, Node3D> playersByPeer = new();

    /// <summary>
    /// Initializes multiplayer signal bindings and prepares the manager
    /// to react to connection and disconnection events.
    /// </summary>
    public override void _Ready()
    {
        var mp = Multiplayer;

        mp.PeerConnected += OnPeerConnected;
        mp.PeerDisconnected += OnPeerDisconnected;
        mp.ConnectedToServer += OnConnectedToServer;
        mp.ConnectionFailed += OnConnectionFailed;
        mp.ServerDisconnected += OnServerDisconnected;

        GD.Print("NetworkManager ready.");

        //Test code to force an initialization to being a host.
        if (!Engine.IsEditorHint())
        {
            GD.Print("Auto-starting host for test...");
            StartHost();
        }
    }

    /// <summary>
    /// Starts a multiplayer session where this process acts as both
    /// the server and a local player (listen server / host).
    /// </summary>
    public void StartHost(int port = 7777, int maxClients = 16)
    {
        GD.Print("Starting host...");

        peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(port, maxClients);

        if (error != Error.Ok)
        {
            GD.PushError($"Failed to start host on port {port}: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"Host started on port {port}");

        // Host process is peer 1 by ENet convention
        SpawnPlayerForPeer(1);
        NetworkLobby.AddPlayer(1, isHost: true);
        NetworkLobby.UpdateLocalFlags(Multiplayer.GetUniqueId());
    }

    /// <summary>
    /// Starts a multiplayer session where this process acts as a
    /// dedicated server with no local player.
    /// </summary>
    public void StartDedicatedServer(int port = 7777, int maxClients = 16)
    {
        GD.Print("Starting dedicated server...");

        peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(port, maxClients);

        if (error != Error.Ok)
        {
            GD.PushError($"Failed to start server on port {port}: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"Dedicated server started on port {port}");
    }

    /// <summary>
    /// Connects this process to a remote multiplayer server as a client.
    /// </summary>
    public void StartClient(string address = "127.0.0.1", int port = 7777)
    {
        GD.Print($"Connecting to {address}:{port}...");

        peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(address, port);

        if (error != Error.Ok)
        {
            GD.PushError($"Failed to connect to {address}:{port}: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
    }

    /// <summary>
    /// Handles notification that a new peer has joined the multiplayer session.
    /// On the server, this results in spawning a player entity for that peer.
    /// </summary>
    private void OnPeerConnected(long id)
    {
        GD.Print($"Peer connected: {id}");

        if (!Multiplayer.IsServer())
            return;

        SpawnPlayerForPeer((int)id);
        NetworkLobby.AddPlayer((int)id, isHost: false);
        NetworkLobby.UpdateLocalFlags(Multiplayer.GetUniqueId());
    }

    /// <summary>
    /// Handles notification that a peer has disconnected from the session.
    /// Cleans up any associated player entity on the server.
    /// </summary>
    private void OnPeerDisconnected(long id)
    {
        GD.Print($"Peer disconnected: {id}");

        if (!Multiplayer.IsServer())
            return;

        int peerId = (int)id;

        if (playersByPeer.TryGetValue(peerId, out var player))
        {
            player.QueueFree();
            playersByPeer.Remove(peerId);
        }

        NetworkLobby.RemovePlayer((int)id);
        NetworkLobby.UpdateLocalFlags(Multiplayer.GetUniqueId());
    }

    /// <summary>
    /// Handles confirmation that this client has successfully connected
    /// to a remote server.
    /// </summary>
    private void OnConnectedToServer()
    {
        GD.Print("Connected to server.");
    }

    /// <summary>
    /// Handles failure to establish a connection to a remote server.
    /// </summary>
    private void OnConnectionFailed()
    {
        GD.PushError("Connection failed.");
    }

    /// <summary>
    /// Handles unexpected disconnection from the server after a successful connection.
    /// </summary>
    private void OnServerDisconnected()
    {
        GD.Print("Disconnected from server.");
    }

    /// <summary>
    /// Creates and registers a new player entity associated with the given peer ID,
    /// and delegates authority configuration to the player's internal
    /// NetworkAuthoritySetup component.
    /// </summary>
    private void SpawnPlayerForPeer(int peerId)
    {
        if (PlayerScene == null)
        {
            GD.PushError("NetworkManager: PlayerScene is not assigned.");
            return;
        }

        GD.Print($"Spawning player for peer {peerId}.");

        var player = PlayerScene.Instantiate<Node3D>();

        GetTree().CurrentScene.AddChild(player);
        playersByPeer[peerId] = player;

        var authoritySetup = player.FindChildOfType<NetworkAuthority>();

        if (authoritySetup == null)
        {
            GD.PushError("NetworkManager: Player instance is missing NetworkAuthoritySetup.");
            return;
        }

        authoritySetup.SetupAuthority(peerId);
    }
}
