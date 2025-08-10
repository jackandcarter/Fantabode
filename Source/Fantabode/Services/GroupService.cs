namespace Fantabode.Services
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Numerics;
  using Fantabode.Groups;

  public interface IGroupService
  {
    Group? Current { get; }
    bool ApplyGizmoToGroup { get; set; }
    Matrix4x4? PreviewPivotWorld { get; }
    (Vector3 min, Vector3 max)? PreviewBounds { get; }
    ulong? SelectedId { get; }
    void SetPreviewPivotWorld(in Matrix4x4 m);
    void Preview();
    void CaptureFromSelection(IReadOnlyList<ulong> itemIds, Group.PivotMode pivotMode);
    void Clear();
    void StartApply();
    void SelectGroupItem(ulong id);
    void Update();
  }

  public sealed class GroupService : IGroupService
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Dalamud.Plugin.Services.IChatGui Chat => Plugin.Chat;
    private const string Prefix = "[Fantabode]";

    public Group? Current { get; private set; }
    public bool ApplyGizmoToGroup { get; set; }
    public Matrix4x4? PreviewPivotWorld { get; private set; }
    public (Vector3 min, Vector3 max)? PreviewBounds { get; private set; }
    public ulong? SelectedId { get; private set; }

    private bool applying = false;
    private int applyIndex = 0;
    private int framesUntilNext = 0;
    private readonly List<(ulong id, Matrix4x4 world)> queue = new();
    private Matrix4x4[]? previewWorlds;

    public void SetPreviewPivotWorld(in Matrix4x4 m)
    {
      PreviewPivotWorld = m;
      if (Current is null) return;
      var count = Current.ItemIds.Count;
      previewWorlds = new Matrix4x4[count];
      var ends = new Vector3[count];
      var min = new Vector3(float.MaxValue);
      var max = new Vector3(float.MinValue);
      for (int i = 0; i < count; i++)
      {
        var world = m * Current.LocalFromPivot[i];
        previewWorlds[i] = world;
        var p = world.Translation;
        ends[i] = p;
        min = Vector3.Min(min, p);
        max = Vector3.Max(max, p);
      }
      PreviewBounds = (min, max);
      Current.SetEndPositions(ends);
    }

    public void CaptureFromSelection(IReadOnlyList<ulong> itemIds, Group.PivotMode pivotMode)
    {
      if (itemIds == null || itemIds.Count == 0)
      { Current = null; PreviewPivotWorld = null; Chat.PrintError($"{Prefix} No items checked."); return; }

      var mats = itemIds.Select(ReadWorld).ToArray();
      var startPositions = mats.Select(m => m.Translation).ToArray();
      var pivot = pivotMode switch
      {
        Group.PivotMode.FirstItem => mats[0],
        Group.PivotMode.SelectionCenter => MakePivotAtAverage(mats),
        Group.PivotMode.BoundingBox => MakePivotAtBoundsCenter(mats),
        _ => mats[0]
      };

      Matrix4x4.Invert(pivot, out var inv);
      var locals = mats.Select(w => inv * w).ToArray();

      Current = new Group(pivotMode, itemIds.ToArray(), locals, pivot, startPositions);
      SetPreviewPivotWorld(pivot);
      Chat.Print($"{Prefix} Group captured: {itemIds.Count} item(s). Pivot: {pivotMode}");
    }

    public void Clear()
    {
      Current = null;
      PreviewPivotWorld = null;
      SelectedId = null;
      applying = false;
      queue.Clear();
      previewWorlds = null;
      PreviewBounds = null;
      Chat.Print($"{Prefix} Group cleared.");
    }

    public void StartApply()
    {
      if (Current is null || PreviewPivotWorld is null)
      { Chat.PrintError($"{Prefix} No group/preview to apply."); return; }

      queue.Clear();
      var pivot = PreviewPivotWorld.Value;
      var ends = new Vector3[Current.ItemIds.Count];
      for (int i = 0; i < Current.ItemIds.Count; i++)
      {
        var world = pivot * Current.LocalFromPivot[i];
        queue.Add((Current.ItemIds[i], world));
        ends[i] = world.Translation;
      }
      Current.SetEndPositions(ends);
      previewWorlds = null;
      PreviewBounds = null;

      applying = true; applyIndex = 0; framesUntilNext = 0;
      Chat.Print($"{Prefix} Applying group to {queue.Count} item(s)...");
    }

    public void SelectGroupItem(ulong id)
    {
      SelectedId = id;
      PreviewPivotWorld = ReadWorld(id);
      unsafe { Memory.SelectHousingItem((HousingItem*)id); }
    }

    public void Update()
    {
      if (!applying) return;
      if (framesUntilNext > 0) { framesUntilNext--; return; }

      if (applyIndex >= queue.Count)
      { applying = false; Chat.Print($"{Prefix} Group apply complete."); return; }

      var (id, w) = queue[applyIndex];
      unsafe
      {
        var item = (HousingItem*)id;
        Memory.SelectHousingItem(item); // helper on PluginMemory
        FromMatrix(in w, out var p, out var r);
        Memory.WritePosition(p);
        Memory.WriteRotation(r);
      }
      applyIndex++;
      framesUntilNext = 2; // small delay so the game accepts the change
    }

    public void Preview()
    {
      if (Current is null || previewWorlds is null)
        return;
      unsafe
      {
        var original = Memory.HousingStructure->ActiveItem;
        for (int i = 0; i < Current.ItemIds.Count; i++)
        {
          var world = previewWorlds[i];
          var item = (HousingItem*)Current.ItemIds[i];
          Memory.SelectHousingItem(item);
          FromMatrix(in world, out var p, out var r);
          Memory.WritePosition(p);
          Memory.WriteRotation(r);
        }
        if (original != null)
          Memory.SelectHousingItem(original);
      }
    }

    // -------- helpers --------
    private static Matrix4x4 TR(in Vector3 pos, in Vector3 eulerDeg)
    {
      var rx = eulerDeg.X * (float)Math.PI/180f;
      var ry = eulerDeg.Y * (float)Math.PI/180f;
      var rz = eulerDeg.Z * (float)Math.PI/180f;
      var cx=(float)Math.Cos(rx); var sx=(float)Math.Sin(rx);
      var cy=(float)Math.Cos(ry); var sy=(float)Math.Sin(ry);
      var cz=(float)Math.Cos(rz); var sz=(float)Math.Sin(rz);
      var m = Matrix4x4.Identity;
      m.M11 = cy*cz; m.M12 = cy*sz; m.M13 = -sy;
      m.M21 = sx*sy*cz - cx*sz; m.M22 = sx*sy*sz + cx*cz; m.M23 = sx*cy;
      m.M31 = cx*sy*cz + sx*sz; m.M32 = cx*sy*sz - sx*cz; m.M33 = cx*cy;
      m.Translation = pos; return m;
    }

    private static void FromMatrix(in Matrix4x4 m, out Vector3 pos, out Vector3 eulerDeg)
    {
      pos = m.Translation;
      var sy = -m.M13;
      var y = (float)Math.Asin(Math.Clamp(sy, -1f, 1f));
      var x = (float)Math.Atan2(m.M23, m.M33);
      var z = (float)Math.Atan2(m.M12, m.M11);
      eulerDeg = new Vector3(x, y, z) * (180f/(float)Math.PI);
    }

    private static Matrix4x4 MakePivotAtAverage(IReadOnlyList<Matrix4x4> mats)
    {
      var pos = Vector3.Zero;
      for (int i=0;i<mats.Count;i++) pos += mats[i].Translation;
      pos /= mats.Count;
      var baseOri = mats[0]; baseOri.Translation = pos; return baseOri;
    }

    private static Matrix4x4 MakePivotAtBoundsCenter(IReadOnlyList<Matrix4x4> mats)
    {
      var min = new Vector3(float.MaxValue); var max = new Vector3(float.MinValue);
      foreach (var m in mats) { var p = m.Translation; min = Vector3.Min(min,p); max = Vector3.Max(max,p); }
      var pos = (min+max)*0.5f; var baseOri = mats[0]; baseOri.Translation = pos; return baseOri;
    }

    private static Matrix4x4 ReadWorld(ulong id)
    {
      unsafe
      {
        var item = (HousingItem*)id;
        var p = item->Position;
        var r = Util.FromQ(item->Rotation);
        return TR(p, r);
      }
    }
  }
}
