using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Satori.Utilities;

/// <summary>
/// Workaround for Resharper error.  This can be reverted when this is fixed in Resharper.
/// </summary>
/// <remarks>https://youtrack.jetbrains.com/issue/RSRP-494352/Support-rendermode-directive</remarks>
public sealed class RenderModeInteractiveServer : RenderModeAttribute
{
    public override IComponentRenderMode Mode => (IComponentRenderMode)RenderMode.InteractiveServer;
}