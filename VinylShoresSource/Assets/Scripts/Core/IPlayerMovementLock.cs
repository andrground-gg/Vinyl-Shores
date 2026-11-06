namespace VinylShores.Core
{
    /// <summary>
    /// Implemented by any player controller that must release movement while a
    /// dialogue (or another modal interaction) is active.
    /// </summary>
    public interface IPlayerMovementLock
    {
        void SetMovementEnabled(bool enabled);
    }
}
