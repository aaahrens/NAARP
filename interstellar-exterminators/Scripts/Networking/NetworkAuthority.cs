using Godot;

/// <summary>
/// Configures multiplayer authority for an entity by assigning
/// client authority to input nodes and server authority to all
/// other nodes in the entity subtree.
/// </summary>
public partial class NetworkAuthority : Node
{
    /// <summary>
    /// Nodes responsible for producing player input.
    /// These nodes will be assigned authority for the controlling peer,
    /// allowing them to emit input intent.
    /// </summary>
    [Export]
    public Node[] ClientAuthorityNodes = new Node[0];

    /// <summary>
    /// The peer ID representing the authoritative server.
    /// In ENet-based multiplayer this is conventionally peer ID 1.
    /// </summary>
    public const int ServerPeerId = 1;

    /// <summary>
    /// The peer ID of the client that will be granted authority.
    /// </summary>
    public int PeerId { get; private set; }

    /// <summary>
    /// Assigns multiplayer client ownership for the specified client authority nodes while giving everything else server ownership.
    /// </summary>
    public void SetupAuthority(int controllingPeerId)
    {
        PeerId = controllingPeerId;

        if (!Multiplayer.IsServer())
        {
            GD.PushWarning(
                "NetworkAuthoritySetup.SetupAuthority was invoked on a non-server peer."
            );
            return;
        }

        Node authorityRoot = FindAuthorityRoot();
        if (authorityRoot == null)
        {
            GD.PushError("NetworkAuthoritySetup: Authority root is null.");
            return;
        }

        authorityRoot.SetMultiplayerAuthority(ServerPeerId, true);
        if (ClientAuthorityNodes == null)
            return;

        foreach (var node in ClientAuthorityNodes)
        {
            if (node == null)
                continue;

            node.SetMultiplayerAuthority(controllingPeerId, true);
        }
    }

    /// <summary>
    /// Determines the root node of the entity whose authority is being configured
    /// by walking upward to the highest ancestor beneath the SceneTree root.
    /// </summary>
    private Node FindAuthorityRoot()
    {
        Node current = this;

        // SceneTree root is the top-level Viewport/Window.
        Node treeRoot = GetTree().Root;

        // Walk upward until the parent is the SceneTree root,
        // or there is no parent left.
        while (current.GetParent() != null &&
               current.GetParent() != treeRoot)
        {
            current = current.GetParent();
        }

        return current;
    }
}
