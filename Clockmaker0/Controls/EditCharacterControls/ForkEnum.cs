namespace Clockmaker0.Controls.EditCharacterControls;

/// <summary>
/// Enum to encapsualte the various forking modes
/// </summary>
public enum ForkEnum
{
    /// <summary>
    /// No value declared, abort fork
    /// </summary>
    None = 0,
    /// <summary>
    /// The user is responsible for recreating any jinxes that may belong on this character
    /// </summary>
    Manual = 1,
    /// <summary>
    /// The forked character will create a copy of every jinx that points to it before forking
    /// </summary>
    SwapOwner = 2,
    /// <summary>
    /// Fork any characters necessary to maintain ownership of all jinxe
    /// </summary>
    RecurseFork = 3,
    /// <summary>
    /// There are no jinxes to fork in the first place
    /// </summary>
    Empty = 4,
}