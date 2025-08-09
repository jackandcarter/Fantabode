using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BDTHPlugin.Groups;

namespace BDTHPlugin.Services
{
  public sealed class GroupService : IGroupService
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Dalamud.Plugin.Services.IChatGui Chat => Plugin.Chat;

    public Group? Current { get; private set; }
    public bool ApplyGizmoToGroup { get; set; }
    public Matrix4x4? PreviewPivotWorld { get; private set; }

    private bool applying = false;
    private int index = 0;
    private int framesUntilNext = 0;
    private readonly List<(ulong id, Matrix4x4 world)> toApply = new();

    public void SetPreviewPivotWorld(in Matrix4x4 m) => PreviewPivotWorld = m;
    public void CaptureFromSelection(IReadOnlyList<ulong> itemIds, Group.PivotMode pivotMode)
    {
      if (itemIds == null || itemIds.Count == 0) { Current = null; PreviewPivotWorld = null; return; }
      var world = itemIds.Select(ReadWorld).ToArray();
      var pivotWorld = pivotMode switch
      {
        Group.PivotMode.FirstItem => world[0],
        Group.PivotMode.SelectionCenter => MakePivotAtAverage(world),
        Group.PivotMode.BoundingBox => MakePivotAtBoundsCenter(world),
        _ => world[0]
      };
      Matrix4x4.Invert(pivotWorld, out var invPivot);
      var locals = world.Select(w => invPivot * w).ToArray();
      Current = new Group(pivotMode, itemIds.ToArray(), locals, pivotWorld);
      PreviewPivotWorld = pivotWorld;
    }
    public void Clear(){ Current=null; PreviewPivotWorld=null; applying=false; toApply.Clear(); }
    public void StartApply()
    {
      if (Current is null || PreviewPivotWorld is null) { Chat.PrintError("[BDTH] No group/preview to apply."); return; }
      toApply.Clear();
      var pivot = PreviewPivotWorld.Value;
      for (int i = 0; i < Current.ItemIds.Count; i++)
      {
        var id = Current.ItemIds[i];
        var w = pivot * Current.LocalFromPivot[i];
        toApply.Add((id, w));
      }
      applying = true; index = 0; framesUntilNext = 0;
      Chat.Print($"[BDTH] Applying group to {toApply.Count} item(s)...");
    }
    public void Update()
    {
      if (!applying) return;
      if (framesUntilNext > 0) { framesUntilNext--; return; }
      if (index >= toApply.Count) { applying=false; Chat.Print("[BDTH] Group apply complete."); return; }
      var (id, w) = toApply[index];
      unsafe
      {
        var item = (HousingItem*)id;
        Memory.SelectHousingItem(item); // renamed helper
        FromMatrixTR(in w, out var p, out var r);
        Memory.WritePosition(p);
        Memory.WriteRotation(r);
      }
      index++; framesUntilNext = 2;
    }
  }
}
