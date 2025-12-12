using Godot;
using System;

/// <summary>
/// Simple main menu for starting different types of multiplayer sessions.
/// Provides controls to start a dedicated server, host a game, or connect as a client.
/// Once a session is started, this menu can optionally transition to a lobby scene
/// so that the existing NetworkLobby and NetworkLobbyMenu can be exercised.
/// </summary>
public partial class MainMenu : Control
{
    /// <summary>
    /// Path to the lobby scene that should be loaded after starting
    /// a host or client session. This can be overridden in the inspector.
    /// </summary>
    [Export]
    public string LobbyScenePath { get; set; }

    /// <summary>
    /// Reference to the Button used to start a dedicated server.
    /// </summary>
    [Export]
    private Button startDedicatedButton;

    /// <summary>
    /// Reference to the Button used to start a listen-server host session.
    /// </summary>
    [Export]
    private Button hostButton;

    /// <summary>
    /// Reference to the Button used to connect to an existing server as a client.
    /// </summary>
    [Export]
    private Button connectButton;

    /// <summary>
    /// Reference to the LineEdit used to input the server address.
    /// </summary>
    [Export]
    private LineEdit addressLineEdit;

    /// <summary>
    /// Reference to the LineEdit used to input the port number.
    /// </summary>
    [Export]
    private LineEdit portLineEdit;

    /// <summary>
    /// Reference to the NetworkManager responsible for starting and managing
    /// multiplayer sessions. This is resolved from the scene tree.
    /// </summary>
    private NetworkManager networkManager;

    /// <summary>
    /// Called by Godot when the node is added to the scene tree.
    /// Initializes references, wires up button callbacks, and locates the NetworkManager.
    /// </summary>
    public override void _Ready()
    {
        startDedicatedButton.Pressed += OnStartDedicatedPressed;
        hostButton.Pressed += OnHostPressed;
        connectButton.Pressed += OnConnectPressed;

        // Provide reasonable defaults for quick testing.
        if (addressLineEdit != null && string.IsNullOrWhiteSpace(addressLineEdit.Text))
        {
            addressLineEdit.Text = "127.0.0.1";
        }

        if (portLineEdit != null && string.IsNullOrWhiteSpace(portLineEdit.Text))
        {
            portLineEdit.Text = "7777";
        }

        // Locate the NetworkManager anywhere in the scene tree.
        networkManager = this.FindInEntireSceneTreeOfType<NetworkManager>();

        if (networkManager == null)
        {
            GD.PushError("MainMenu: Could not find NetworkManager in scene tree. Multiplayer buttons will not function.");
            startDedicatedButton.Disabled = true;
            hostButton.Disabled = true;
            connectButton.Disabled = true;
        }
    }

    /// <summary>
    /// Handles clicks on the "Start Dedicated Server" button.
    /// Starts a dedicated server using the configured port and leaves the menu in place.
    /// </summary>
    private void OnStartDedicatedPressed()
    {
        if (networkManager == null)
        {
            return;
        }

        int port = GetPortOrDefault();
        networkManager.StartDedicatedServer(port);

        GD.Print($"MainMenu: Dedicated server started on port {port}.");
        // For a dedicated server, we typically do not change scenes on this process.
    }

    /// <summary>
    /// Handles clicks on the "Host Game" button.
    /// Starts a listen server (server + local player) and transitions to the lobby scene.
    /// </summary>
    private void OnHostPressed()
    {
        if (networkManager == null)
        {
            return;
        }

        int port = GetPortOrDefault();
        networkManager.StartHost(port);

        GD.Print($"MainMenu: Host started on port {port}.");

        if (!string.IsNullOrEmpty(LobbyScenePath))
        {
            GetTree().ChangeSceneToFile(LobbyScenePath);
        }
    }

    /// <summary>
    /// Handles clicks on the "Connect" button.
    /// Connects this process as a client to the specified server address and port,
    /// and then transitions to the lobby scene.
    /// </summary>
    private void OnConnectPressed()
    {
        if (networkManager == null)
        {
            return;
        }

        string address = GetAddressOrDefault();
        int port = GetPortOrDefault();

        networkManager.StartClient(address, port);

        GD.Print($"MainMenu: Connecting to {address}:{port}...");

        if (!string.IsNullOrEmpty(LobbyScenePath))
        {
            GetTree().ChangeSceneToFile(LobbyScenePath);
        }
    }

    /// <summary>
    /// Retrieves the currently configured port from the UI, or a default value if
    /// the field is invalid or missing. Logs a warning when fallback occurs.
    /// </summary>
    /// <returns>Valid port number between 1 and 65535.</returns>
    private int GetPortOrDefault()
    {
        const int defaultPort = 7777;

        if (portLineEdit == null)
        {
            return defaultPort;
        }

        if (int.TryParse(portLineEdit.Text, out int port) && port > 0 && port <= 65535)
        {
            return port;
        }

        GD.PushWarning($"MainMenu: Invalid port '{portLineEdit.Text}', falling back to {defaultPort}.");
        portLineEdit.Text = defaultPort.ToString();
        return defaultPort;
    }

    /// <summary>
    /// Retrieves the currently configured server address from the UI, or a default
    /// of localhost if the field is invalid or missing.
    /// </summary>
    /// <returns>Server address string to use for client connections.</returns>
    private string GetAddressOrDefault()
    {
        const string defaultAddress = "127.0.0.1";

        if (addressLineEdit == null)
        {
            return defaultAddress;
        }

        string text = addressLineEdit.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            addressLineEdit.Text = defaultAddress;
            return defaultAddress;
        }

        return text.Trim();
    }
}
