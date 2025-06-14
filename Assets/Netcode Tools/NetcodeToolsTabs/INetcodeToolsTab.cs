using UnityEngine;

public interface INetcodeToolsTab
{
    string TabName { get; }
    string TabTooltip { get; }
    void SetupContent();
    void DrawContent();
}
