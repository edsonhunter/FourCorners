namespace ElementLogicFail.Scripts.Domain.Interface
{
    public interface IElement
    {
        ElementType Type { get; }
        
        bool IsSameType(IElement element);
    }

    public enum ElementType
    {
        Unknown,
        Fire,
        Water,
        Earth,
        Wind
    }
}