namespace FourCorners.Scripts.Domain.Interface
{
    public interface IMinion
    {
        MinionType Type { get; }
        
        bool IsSameType(IMinion minion);
    }

    public enum MinionType
    {
        Unknown,
        Fire,
        Water,
        Earth,
        Wind
    }
}
