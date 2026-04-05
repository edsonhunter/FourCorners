using Unity.NetCode;

namespace FourCorners.Scripts.Components.Request
{
    public struct GoInGameRequest : IRpcCommand
    {
        // 0-3 for the 4 corners of the map
        public int RequestedTeamIndex;
    }
}
