using BDTHPlugin.Interface.Windows;
using BDTHPlugin.Services;

using Dalamud.Interface.Windowing;

namespace BDTHPlugin.Interface
{
  public class PluginUI
  {
    private readonly WindowSystem Windows = new("BDTH");
    
    private readonly Gizmo Gizmo = new();

    public readonly MainWindow Main;
    public readonly DebugWindow Debug = new();
    public readonly GroupWindow Group;
    public readonly FurnitureList Furniture = new();

    public PluginUI()
    {
      Main = new MainWindow(Gizmo);
      Windows.AddWindow(Main);
      Windows.AddWindow(Debug);
      Windows.AddWindow(Furniture);

      Group = new GroupWindow(Plugin.GetGroups());
      Windows.AddWindow(Group);
    }

    public void Draw()
    {
      Gizmo.Draw();
      Windows.Draw();
    }
  }
}
