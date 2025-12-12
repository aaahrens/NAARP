using Godot;
using System;

/// <summary>
/// Represents the main lobby menu UI. 
/// Responsible for displaying connected players and lobby actions such as 
/// readying up, starting the game, and leaving the lobby.
/// </summary>
public partial class NetworkLobbyMenu : Node
{
    /// <summary>
    /// Reference to the VBoxContainer that contains all PlayerRow instances.
    /// </summary>
    [Export]
    private VBoxContainer playerRowsContainer;

    /// <summary>
    /// Reference to the Label displaying the local player's role text.
    /// </summary>
    [Export]
    private Label roleLabel;

    /// <summary>
    /// Reference to the Button used to toggle the local player's ready state.
    /// </summary>
    [Export]
    private Button readyButton;

    /// <summary>
    /// Reference to the Button used to request starting the game as the host.
    /// </summary>
    [Export]
    private Button startButton;

    /// <summary>
    /// Reference to the Button used to leave and exit the lobby.
    /// </summary>
    [Export]
    private Button leaveButton;

    /// <summary>
    /// Control used to create each individual player row in the lobby UI.
    /// </summary>
    [Export]
    private PackedScene playerRowScene;

    /// <summary>
    /// The main menu scene.
    /// </summary>
    [Export]
    private PackedScene mainMenuScene;

    /// <summary>
    /// Cached reference to the autoloaded LobbyManager node.
    /// </summary>
    private NetworkLobby networkLobby;

    /// <summary>
    /// Called by Godot when the node is added to the scene tree.
    /// Initializes references to child nodes, wires up button events,
    /// and performs any necessary initial UI setup.
    /// </summary>
    public override void _Ready()
    {
        readyButton.Pressed += OnReadyPressed;
        startButton.Pressed += OnStartPressed;
        leaveButton.Pressed += OnLeavePressed;

        networkLobby = this.FindInEntireSceneTreeOfType<NetworkLobby>();
        if (networkLobby == null)
        {
            GD.PushError("NetworkLobbyMenu: Could not find NetworkLobby; lobby UI will not update.");
            return;
        }

        if (playerRowScene == null)
        {
            GD.PushError("NetworkLobbyMenu: playerRowScene is not assigned.");
            return;
        }

        networkLobby.LobbyUpdated += OnLobbyUpdated;
        OnLobbyUpdated(networkLobby.LobbyState);
    }

    /// <summary>
    /// Updates the entire lobby UI based on the provided lobby state.
    /// This should be called whenever the authoritative lobby data changes.
    /// </summary>
    /// <param name="lobbyState">
    /// The current lobby state snapshot containing players, host information,
    /// and flags such as whether the game can be started.
    /// </param>
    public void OnLobbyUpdated(LobbyState lobbyState)
    {
        if (lobbyState == null)
        {
            return;
        }

        UpdateRoleLabel(lobbyState);
        UpdatePlayersPanel(lobbyState);
        UpdateButtons(lobbyState);
    }

    /// <summary>
    /// Updates the role label text to reflect whether the local player is the host or a regular player.
    /// </summary>
    /// <param name="lobbyState">The current lobby state snapshot.</param>
    private void UpdateRoleLabel(LobbyState lobbyState)
    {
        roleLabel.Text = lobbyState.LocalIsHost ? "You are Host" : "You are Player";
    }

    /// <summary>
    /// Rebuilds the player list UI from the current set of players in the lobby state.
    /// Existing PlayerRow instances are cleared and recreated.
    /// </summary>
    /// <param name="lobbyState">The current lobby state snapshot.</param>
    private void UpdatePlayersPanel(LobbyState lobbyState)
    {
        // Clear existing rows
        foreach (Node child in playerRowsContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Recreate from lobby players
        foreach (LobbyPlayer player in lobbyState.Players)
        {
            Node rowNode = playerRowScene.Instantiate();
            playerRowsContainer.AddChild(rowNode);

            var row = rowNode as PlayerRow;
            if (row == null)
            {
                GD.PushError($"NetworkLobbyMenu: Instantiated PlayerRow scene does not have a PlayerRow script on its root (got {rowNode.GetType().Name}).");
                continue;
            }

            row.SetData(player.PeerId, player.Name, player.IsReady, player.IsHost);
        }
    }

    /// <summary>
    /// Updates the interactive state and text of the lobby action buttons
    /// based on the current lobby state (e.g. local ready status and host status).
    /// </summary>
    /// <param name="lobbyState">The current lobby state snapshot.</param>
    private void UpdateButtons(LobbyState lobbyState)
    {
        readyButton.Text = lobbyState.LocalIsReady ? "Unready" : "Ready";

        startButton.Visible = lobbyState.LocalIsHost;
        startButton.Disabled = !lobbyState.CanStartGame;
    }

    /// <summary>
    /// Handles clicks on the Ready button.
    /// Forwards a request to the network layer to toggle the local player's ready state.
    /// </summary>
    private void OnReadyPressed()
    {
        if (networkLobby == null)
        {
            return;
        }

        LobbyState lobbyState = networkLobby.LobbyState;
        bool newReady = !lobbyState.LocalIsReady;

        networkLobby.SetReady_Local(newReady);
    }

    /// <summary>
    /// Handles clicks on the Start button.
    /// Forwards a request to the network layer to attempt to start the game as the host.
    /// </summary>
    private void OnStartPressed()
    {
        if (networkLobby == null)
        {
            return;
        }

        networkLobby.RequestStartGame();
    }

    /// <summary>
    /// Handles clicks on the Leave button.
    /// Forwards a request to leave the lobby and then changes back to the main menu scene.
    /// </summary>
    private void OnLeavePressed()
    {
        if (mainMenuScene == null)
        {
            GD.PushError("NetworkLobbyMenu: mainMenuScene is not set; cannot leave lobby.");
            return;
        }

        // If we're the sever shut down everyones clients.
        if (Multiplayer.IsServer())
        {
            Multiplayer.MultiplayerPeer?.Close();
            Multiplayer.MultiplayerPeer = null;

            GetTree().ChangeSceneToPacked(mainMenuScene);
            return;
        }

        // If we're a client, just disconnect and return to main menu.
        Multiplayer.MultiplayerPeer?.Close();
        Multiplayer.MultiplayerPeer = null;

        GetTree().ChangeSceneToPacked(mainMenuScene);
    }
}
