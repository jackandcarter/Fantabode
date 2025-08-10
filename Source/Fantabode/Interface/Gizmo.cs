using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Fantabode.Services;
using Fantabode.Groups;

namespace Fantabode.Interface
{
  public class Gizmo
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Configuration Configuration => Plugin.GetConfiguration();
    private static GroupService Groups => Plugin.GetGroups();

    private static unsafe bool CanEdit => Configuration.UseGizmo && Memory.CanEditItem() && Memory.HousingStructure->ActiveItem != null;

    public ImGuizmoMode Mode = ImGuizmoMode.Local;

    private Vector3 translate;
    private Vector3 rotation;
    private Vector3 scale = Vector3.One;

    private Matrix4x4 matrix = Matrix4x4.Identity;

    private ImGuiIOPtr Io;
    private Vector2 Wp;

    public void Draw()
    {
      if (!CanEdit)
        return;

      ImGuiHelpers.ForceNextWindowMainViewport();
      ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));

      ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

      const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs;
      if (!ImGui.Begin("FantabodeGizmo", windowFlags))
        return;

      Io = ImGui.GetIO();
      ImGui.SetWindowSize(Io.DisplaySize);

      Wp = ImGui.GetWindowPos();

      try
      {
        DrawGizmo(Wp, new Vector2(Io.DisplaySize.X, Io.DisplaySize.Y));
      }
      finally
      {
        ImGui.PopStyleVar();
        ImGui.End();
      }
    }

    private unsafe void DrawGizmo(Vector2 pos, Vector2 size)
    {
      ImGuizmo.BeginFrame();

      var cam = Memory.Camera->RenderCamera;
      var view = Memory.Camera->ViewMatrix;
      var proj = cam->ProjectionMatrix;

      var far = cam->FarPlane;
      var near = cam->NearPlane;
      var clip = far / (far - near);

      proj.M43 = -(clip * near);
      proj.M33 = -((far + near) / (far - near));
      view.M44 = 1.0f;

      ImGuizmo.SetDrawlist();

      ImGuizmo.Enable(Memory.HousingStructure->Rotating);
      ImGuizmo.SetID((int)ImGui.GetID("Fantabode"));
      ImGuizmo.SetOrthographic(false);

      ImGuizmo.SetRect(pos.X, pos.Y, size.X, size.Y);

      ComposeMatrix();

      var snap = Configuration.DoSnap ? new(Configuration.Drag, Configuration.Drag, Configuration.Drag) : Vector3.Zero;

      if (Manipulate(ref view.M11, ref proj.M11, ImGuizmoOperation.Translate, Mode, ref matrix.M11, ref snap.X))
        WriteMatrix();

      ImGuizmo.SetID(-1);
    }

    private void ComposeMatrix()
    {
      try
      {
        if (Groups.ApplyGizmoToGroup && Groups.Current != null)
        {
          var pivot = Groups.PreviewPivotWorld ?? Groups.Current.PivotWorld;
          translate = pivot.Translation;
          var sy = -pivot.M13; var y = (float)System.Math.Asin(System.Math.Clamp(sy, -1f, 1f));
          var x = (float)System.Math.Atan2(pivot.M23, pivot.M33);
          var z = (float)System.Math.Atan2(pivot.M12, pivot.M11);
          rotation = new Vector3(x, y, z) * (180f/(float)System.Math.PI);
          ImGuizmo.RecomposeMatrixFromComponents(ref translate.X, ref rotation.X, ref scale.X, ref matrix.M11);
          return;
        }
        translate = Memory.ReadPosition();
        rotation = Memory.ReadRotation();
        ImGuizmo.RecomposeMatrixFromComponents(ref translate.X, ref rotation.X, ref scale.X, ref matrix.M11);
      }
      catch { }
    }

    private void WriteMatrix()
    {
      ImGuizmo.DecomposeMatrixToComponents(ref matrix.M11, ref translate.X, ref rotation.X, ref scale.X);
      if (Groups.ApplyGizmoToGroup && Groups.Current != null)
      {
        var rx = rotation.X * (float)System.Math.PI/180f;
        var ry = rotation.Y * (float)System.Math.PI/180f;
        var rz = rotation.Z * (float)System.Math.PI/180f;
        var cx=(float)System.Math.Cos(rx); var sx=(float)System.Math.Sin(rx);
        var cy=(float)System.Math.Cos(ry); var sy=(float)System.Math.Sin(ry);
        var cz=(float)System.Math.Cos(rz); var sz=(float)System.Math.Sin(rz);
        var m = System.Numerics.Matrix4x4.Identity;
        m.M11 = cy*cz; m.M12 = cy*sz; m.M13 = -sy;
        m.M21 = sx*sy*cz - cx*sz; m.M22 = sx*sy*sz + cx*cz; m.M23 = sx*cy;
        m.M31 = cx*sy*cz + sx*sz; m.M32 = cx*sy*sz - sx*cz; m.M33 = cx*cy;
        m.Translation = translate;
        Groups.SetPreviewPivotWorld(m);
        Groups.Preview();
        return;
      }
      Memory.WritePosition(translate);
    }

    private unsafe bool Manipulate(ref float view, ref float proj, ImGuizmoOperation op, ImGuizmoMode mode, ref float matrix, ref float snap)
    {
      fixed (float* native_view = &view)
      {
        fixed (float* native_proj = &proj)
        {
          fixed (float* native_matrix = &matrix)
          {
            fixed (float* native_snap = &snap)
            {
              return ImGuizmo.Manipulate(
                native_view,
                native_proj,
                op,
                mode,
                native_matrix,
                null,
                native_snap,
                null,
                null
              ) != false;
            }
          }
        }
      }
    }
  }
}
