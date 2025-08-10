using Fantabode.Interface.Windows;
using Dalamud.Interface.Windowing;

namespace Fantabode.Interface
{
  public class PluginUI
  {
    private const string WindowSystemId = "Fantabode";
    private readonly WindowSystem Windows = new(WindowSystemId);
    
    private readonly Gizmo Gizmo = new();

    public readonly MainWindow Main;
    public readonly DebugWindow Debug = new();
    public readonly FurnitureList Furniture = new();

    public PluginUI()
    {
      Main = new MainWindow(Gizmo);
      Windows.AddWindow(Main);
      Windows.AddWindow(Debug);
      Windows.AddWindow(Furniture);
    }

    public void Draw()
    {
      Gizmo.Draw();
      Windows.Draw();
    }
  }
}
