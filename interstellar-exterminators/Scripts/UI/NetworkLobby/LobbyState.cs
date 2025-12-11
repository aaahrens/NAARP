using System.Collections.Generic;

/// <summary>
/// Represents a full snapshot of the lobby state on the client, including all players,
/// host information, and basic flags controlling lobby behavior.
/// </summary>
public class LobbyState
{
    /// <summary>
    /// Indicates whether the local player is the host of the lobby.
    /// </summary>
    public bool LocalIsHost;

    /// <summary>
    /// Indicates whether the local player is currently marked as ready.
    /// </summary>
    public bool LocalIsReady;

    /// <summary>
    /// Indicates whether the lobby conditions allow the game to be started
    /// (for example, all required players are ready).
    /// </summary>
    public bool CanStartGame;

    /// <summary>
    /// List of all players currently present in the lobby.
    /// </summary>
    public List<LobbyPlayer> Players { get; } = new List<LobbyPlayer>();
}