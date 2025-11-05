namespace Clockmaker0.Data;

/// <summary>
/// Defines a control that has a distinct lock and unlock method instead of using isenabled to disable editting.
/// </summary>
public interface ILock
{
    /// <summary>
    /// Puts the control in read only mode
    /// </summary>
    public void Lock();
    /// <summary>
    /// Enables Editting
    /// </summary>
    public void Unlock();
}