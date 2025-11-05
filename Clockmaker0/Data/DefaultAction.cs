namespace Clockmaker0.Data;

/// <summary>
/// Default actions for the PreviewScriptCharacterActionButton
/// </summary>
public enum DefaultAction
{
    /// <summary>
    /// The user has not selected a default action, and the app will make an informed guess
    /// </summary>
    None = 0,
    /// <summary>
    /// The user has requested the control be created as part of the same window
    /// </summary>
    OpenInCurrentWindow = 1,
    /// <summary>
    /// The user has requested the control be created in a new window
    /// </summary>
    OpenInNewWindow = 2,
    /// <summary>
    /// The user has requested the control be deleted
    /// </summary>
    DeleteItem = 3,
}