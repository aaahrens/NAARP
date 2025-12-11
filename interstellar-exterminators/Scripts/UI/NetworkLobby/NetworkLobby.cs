using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages lobby-specific state such as the list of players, 
/// readiness, and whether the game can start.
/// </summary>
public class NetworkLobby
{
    /// <summary>
    /// Snapshot of the current lobby state for this process.
    /// </summary>
    public LobbyState State { get; private set; } = new LobbyState();

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
    /// Adds a new player to the lobby and rebuilds the lobby state snapshot.
    /// </summary>
    /// <param name="peerId">Unique peer identifier for the new player.</param>
    /// <param name="isHost">Indicates whether this player is the lobby host.</param>
    public void AddPlayer(int peerId, bool isHost)
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
    public void RemovePlayer(int peerId)
    {
        if (playersByPeer.Remove(peerId))
        {
            RebuildState();
        }
    }

    /// <summary>
    /// Sets the ready state for a given player and rebuilds the lobby state snapshot.
    /// </summary>
    /// <param name="peerId">Unique peer identifier of the player being updated.</param>
    /// <param name="ready">New ready state for the player.</param>
    public void SetReady(int peerId, bool ready)
    {
        if (!playersByPeer.TryGetValue(peerId, out LobbyPlayer lobbyPlayer))
        {
            return;
        }

        lobbyPlayer.IsReady = ready;
        RebuildState();
    }

    /// <summary>
    /// Updates local-player specific flags on the lobby state based on the provided local peer ID.
    /// This should be called whenever the local peer changes or when the player list changes.
    /// </summary>
    /// <param name="localPeerId">Peer ID for the local player, or 0 if unknown.</param>
    public void UpdateLocalFlags(int localPeerId)
    {
        bool localIsHost = false;
        bool localIsReady = false;

        if (localPeerId != 0 && playersByPeer.TryGetValue(localPeerId, out LobbyPlayer localPlayer))
        {
            localIsHost = localPlayer.IsHost;
            localIsReady = localPlayer.IsReady;
        }

        State.LocalIsHost = localIsHost;
        State.LocalIsReady = localIsReady;
        State.CanStartGame = CanStartGame(State);

        LobbyUpdated?.Invoke(State);
    }

    /// <summary>
    /// Rebuilds the immutable LobbyState snapshot from the internal player dictionary
    /// and recomputes derived flags such as whether the game can start.
    /// </summary>
    private void RebuildState()
    {
        var newState = new LobbyState();

        foreach (var pair in playersByPeer)
        {
            newState.Players.Add(pair.Value);
        }

        // LocalIsHost / LocalIsReady will be filled in by UpdateLocalFlags.
        State = newState;

        // Note: We do not fire LobbyUpdated here because we are missing local flags.
        // The owner (e.g. NetworkManager) should call UpdateLocalFlags() afterwards.
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
}
