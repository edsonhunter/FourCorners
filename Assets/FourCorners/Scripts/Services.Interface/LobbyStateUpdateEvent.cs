namespace FourCorners.Scripts.Services.Interface
{
    /// <summary>
    /// Payload sent via ISystemBridgeService.OnLobbyStateUpdate.
    /// UI (e.g. ConnectionScreenUI) subscribes to this event to:
    ///  - Update the displayed player count.
    ///  - Show/hide the Start button for the host.
    /// </summary>
    public struct LobbyStateUpdateEvent
    {
        /// <summary>True if this client is the designated lobby host.</summary>
        public bool IsHost;

        /// <summary>Number of players currently accepted by the server.</summary>
        public int PlayerCount;
    }
}
