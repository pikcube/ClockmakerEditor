namespace Clockmaker0.Data;



/// <summary>
/// Represents a control that needs to unsubscribe when it is going to be unloaded
/// </summary>
public interface IDelete
{
    /// <summary>
    /// Called when a control is to be destroyed so it can unsubscribe from any events
    /// </summary>
    public void Delete();
}