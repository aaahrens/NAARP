using Godot;

/// <summary>
/// Represents a single player's row in the lobby UI.
/// </summary>
public partial class PlayerRow : Node
{
    /// <summary>
    /// Label displaying the player's name.
    /// </summary>
    private Label nameLabel;

    /// <summary>
    /// Label displaying the player's ready status.
    /// </summary>
    private Label readyLabel;

    /// <summary>
    /// Label displaying the player's role (Host/Client).
    /// </summary>
    private Label roleLabel;

    public int PeerId { get; private set; }

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("NameLabel");
        readyLabel = GetNode<Label>("ReadyLabel");
        roleLabel = GetNode<Label>("RoleLabel");
    }

    /// <summary>
    /// Sets the data for this player row.
    /// </summary>
    /// <param name="peerId"> PeerId of the player represented by this row. </param>
    /// <param name="name"> The name of the player represented by this row.</param>
    /// <param name="ready"> Is the player represented by this row ready?</param>
    /// <param name="isHost"> Is the player represented by this row the host?</param>
    public void SetData(int peerId, string name, bool ready, bool isHost)
    {
        PeerId = peerId;
        nameLabel.Text = name;
        readyLabel.Text = ready ? "Ready" : "Not Ready";
        roleLabel.Text = isHost ? "Host" : "Client";
    }
}
