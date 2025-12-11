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
    /// NodePath to the VBoxContainer that will hold instantiated PlayerRow controls.
    /// This container should be the parent of all player list entries in the lobby UI.
    /// </summary>
    [Export]
    public NodePath PlayerRowsContainerPath { get; set; }

    /// <summary>
    /// NodePath to the Label used to display the local player's role
    /// (e.g. "You are Host" or "You are Player").
    /// </summary>
    [Export]
    public NodePath RoleLabelPath { get; set; }

    /// <summary>
    /// NodePath to the "Ready" Button which toggles the local player's ready state.
    /// </summary>
    [Export]
    public NodePath ReadyButtonPath { get; set; }

    /// <summary>
    /// NodePath to the "Start" Button which allows the host to start the game.
    /// </summary>
    [Export]
    public NodePath StartButtonPath { get; set; }

    /// <summary>
    /// NodePath to the "Leave" Button which allows the local player to exit the lobby.
    /// </summary>
    [Export]
    public NodePath LeaveButtonPath { get; set; }

    /// <summary>
    /// Reference to the VBoxContainer that contains all PlayerRow instances.
    /// </summary>
    private VBoxContainer playerRowsContainer;

    /// <summary>
    /// Reference to the Label displaying the local player's role text.
    /// </summary>
    private Label roleLabel;

    /// <summary>
    /// Reference to the Button used to toggle the local player's ready state.
    /// </summary>
    private Button readyButton;

    /// <summary>
    /// Reference to the Button used to request starting the game as the host.
    /// </summary>
    private Button startButton;

    /// <summary>
    /// Reference to the Button used to leave and exit the lobby.
    /// </summary>
    private Button leaveButton;

    /// <summary>
    /// PackedScene used to instantiate new PlayerRow controls for the player list.
    /// </summary>
    private PackedScene playerRowScene;

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
        playerRowsContainer = GetNode<VBoxContainer>(PlayerRowsContainerPath);
        roleLabel = GetNode<Label>(RoleLabelPath);
        readyButton = GetNode<Button>(ReadyButtonPath);
        startButton = GetNode<Button>(StartButtonPath);
        leaveButton = GetNode<Button>(LeaveButtonPath);

        playerRowScene = GD.Load<PackedScene>("res://UI/PlayerRow.tscn");

        readyButton.Pressed += OnReadyPressed;
        startButton.Pressed += OnStartPressed;
        leaveButton.Pressed += OnLeavePressed;

        networkLobby = this.FindInEntireSceneTreeOfType<NetworkLobby>();
        if (networkLobby == null)
        {
            GD.PushError("NetworkLobbyMenu: Could not find NetworkLobby; lobby UI will not update.");
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
            PlayerRow rowInstance = playerRowScene.Instantiate<PlayerRow>();
            rowInstance.SetData(player.PeerId, player.Name, player.IsReady, player.IsHost);
            playerRowsContainer.AddChild(rowInstance);
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
        //TODO: Implement leaving the lobby properly
        // We need to create main scene to bail out to.
        //GetTree().ChangeSceneToFile("res://UI/MainMenu.tscn");
    }
}
