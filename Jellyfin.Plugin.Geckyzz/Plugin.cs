using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Geckyzz;

/// <summary>
/// The Geckyzz plugin.
/// </summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="xmlSerializer">The XML serializer.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
    }

    /// <inheritdoc />
    public override string Name => "geckyzz";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("36bc6c0a-f85b-46c8-ae04-0d67a454d217");

    /// <inheritdoc />
    public override string Description => "Adds links to anime pages on geckyzz.my.id.";
}
