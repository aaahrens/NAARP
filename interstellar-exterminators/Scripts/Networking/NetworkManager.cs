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
    /// Path to the gameplay scene that should be loaded when the match starts.
    /// </summary>
    [Export]
    public string GameScenePath { get; set; } = "res://Scenes/Game.tscn";

    private ENetMultiplayerPeer peer;
    private readonly Dictionary<int, Node3D> playersByPeer = new();

    private bool isDedicatedServer = false;

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

        isDedicatedServer = false;

        GD.Print($"Host started on port {port}");
    }

    /// <summary>
    /// Starts a multiplayer session where this process acts as a
    /// dedicated server with no local player.
    /// </summary>
    public void StartDedicatedServer(int port = 7777, int maxClients = 16)
    {
        GD.Print("Starting dedicated server...");

        isDedicatedServer = true;

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

    /// <summary>
    /// Called on clients to load the gameplay scene when the server starts the match.
    /// The server changes its own scene locally and will skip this RPC.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void RpcLoadGameScene(string scenePath)
    {
        // Only clients should react here; server already changed scene.
        if (Multiplayer.IsServer())
            return;

        GD.Print($"NetworkManager (client): Loading game scene '{scenePath}'.");

        var tree = GetTree();
        if (tree == null)
        {
            GD.PushError("NetworkManager: SceneTree is null; cannot change scene on client.");
            return;
        }

        var error = tree.ChangeSceneToFile(scenePath);
        if (error != Error.Ok)
        {
            GD.PushError($"NetworkManager (client): Failed to change scene to '{scenePath}': {error}");
        }
    }

    /// <summary>
    /// Performs the full game start sequence on the server.
    /// </summary>
    public void StartGame()
    {
        if (!Multiplayer.IsServer())
        {
            GD.PushError("NetworkManager.StartGame should only be called on the server.");
            return;
        }

        if (string.IsNullOrWhiteSpace(GameScenePath))
        {
            GD.PushError("NetworkManager: GameScenePath is not set; cannot start game.");
            return;
        }

        GD.Print("NetworkManager: Starting game...");

        Error error = GetTree().ChangeSceneToFile(GameScenePath);
        if(error != Error.Ok)
        {
            GD.PushError($"NetworkManager: Failed to change scene to '{GameScenePath}': {error}");
            return;
        }
        Rpc(nameof(RpcLoadGameScene), GameScenePath);

        foreach (int peerId in Multiplayer.GetPeers())
        {
            SpawnPlayerForPeer(peerId);
        }

        if (!isDedicatedServer)
        {
            int localPeerId = Multiplayer.GetUniqueId();

            if (localPeerId != 0 && !playersByPeer.ContainsKey(localPeerId))
                SpawnPlayerForPeer(localPeerId);
        }
    }
}
