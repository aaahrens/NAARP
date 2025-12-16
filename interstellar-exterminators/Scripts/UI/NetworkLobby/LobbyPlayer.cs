using Godot;

/// <summary>
/// Represents a single player in the lobby, including their network identity
/// and lobby-specific state such as name, ready flag, and host status.
/// </summary>
public class LobbyPlayer
{
    /// <summary>
    /// Unique network peer identifier assigned by Godot's multiplayer system.
    /// </summary>
    public int PeerId = -1;

    /// <summary>
    /// Display name chosen by the player for the lobby UI.
    /// </summary>
    public string Name = "Player";

    /// <summary>
    /// Indicates whether this player has marked themselves as ready.
    /// </summary>
    public bool IsReady = false;

    /// <summary>
    /// Indicates whether this player is the host / authoritative lobby owner.
    /// </summary>
    public bool IsHost = false;
}