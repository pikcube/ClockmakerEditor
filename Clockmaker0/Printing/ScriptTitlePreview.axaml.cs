using Avalonia.Controls;
using Avalonia.Media;
using Clockmaker0.Data;
using JetBrains.Annotations;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Printing;

/// <summary>
/// Script item for the script's title
/// </summary>
public partial class ScriptTitlePreview : UserControl
{
    private MutableBotcScript LoadedScript { get; set; }
    private MutableMeta LoadedMeta { get; set; }
    private ScriptImageLoader Loader { get; set; }


    /// <inheritdoc />
    [UsedImplicitly]
    public ScriptTitlePreview() : this(BotcScript.Default.ToMutable(), ScriptImageLoader.Default)
    {
    }

    /// <summary>
    /// Load the script title preview
    /// </summary>
    /// <param name="script">The script</param>
    /// <param name="loader">The image loader</param>
    public ScriptTitlePreview(MutableBotcScript script, ScriptImageLoader loader)
    {
        InitializeComponent();
        LoadedScript = script;
        LoadedMeta = script.Meta;
        Loader = loader;

        ScriptTitleBlock.Text = $"{LoadedMeta.Name} by {LoadedMeta.Author}";
        ScriptTitleBlock.Foreground = new SolidColorBrush(new Color(255, 92, 31, 34));
    }




}