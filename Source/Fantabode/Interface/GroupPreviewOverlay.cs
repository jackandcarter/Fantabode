using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Fantabode.Services;

namespace Fantabode.Interface
{
  public sealed class GroupPreviewOverlay
  {
    private static GroupService Groups => Plugin.GetGroups();

    public void Draw(in Matrix4x4 view, in Matrix4x4 proj, in Vector2 pos, in Vector2 size)
    {
      if (!Groups.ApplyGizmoToGroup || Groups.Current is null)
        return;
      var bounds = Groups.PreviewBounds;
      if (bounds is null)
        return;
      var (min, max) = bounds.Value;

      var vp = view * proj;
      Span<Vector2> corners2d = stackalloc Vector2[8];
      Span<Vector3> corners = stackalloc Vector3[8]
      {
        new(min.X, min.Y, min.Z),
        new(max.X, min.Y, min.Z),
        new(max.X, max.Y, min.Z),
        new(min.X, max.Y, min.Z),
        new(min.X, min.Y, max.Z),
        new(max.X, min.Y, max.Z),
        new(max.X, max.Y, max.Z),
        new(min.X, max.Y, max.Z)
      };
      for (int i = 0; i < 8; i++)
      {
        if (!WorldToScreen(corners[i], vp, pos, size, out corners2d[i]))
          return;
      }
      var drawList = ImGui.GetWindowDrawList();
      uint col = ImGui.GetColorU32(new Vector4(1f, 1f, 0f, 1f));
      ReadOnlySpan<int> edges = stackalloc int[]
      {
        0, 1, 1, 2, 2, 3, 3, 0,
        4, 5, 5, 6, 6, 7, 7, 4,
        0, 4, 1, 5, 2, 6, 3, 7,
      };
      for (int i = 0; i < edges.Length; i += 2)
        drawList.AddLine(corners2d[edges[i]], corners2d[edges[i + 1]], col);
    }

    private static bool WorldToScreen(in Vector3 world, in Matrix4x4 vp, in Vector2 pos, in Vector2 size, out Vector2 screen)
    {
      var clip = Vector4.Transform(new Vector4(world, 1f), vp);
      if (clip.W <= 0f)
      {
        screen = Vector2.Zero;
        return false;
      }
      var ndc = clip / clip.W;
      screen = new Vector2(
        pos.X + (ndc.X * 0.5f + 0.5f) * size.X,
        pos.Y + (0.5f - ndc.Y * 0.5f) * size.Y);
      return true;
    }
  }
}
