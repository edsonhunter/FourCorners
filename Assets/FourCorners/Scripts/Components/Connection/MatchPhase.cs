namespace FourCorners.Scripts.Components.Connection
{
    /// <summary>
    /// Tracks the current phase of the match lifecycle.
    /// The server is the sole authority. Clients learn about phase changes via RPCs.
    /// </summary>
    public enum MatchPhase : byte
    {
        WaitingForPlayers = 0,
        Lobby             = 1,
        Active            = 2
    }
}
