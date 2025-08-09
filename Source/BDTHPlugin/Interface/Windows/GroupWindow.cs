using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using BDTHPlugin.Groups;
using BDTHPlugin.Services;

namespace BDTHPlugin.Interface.Windows
{
  public class GroupWindow : Window
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private readonly IGroupService Groups;

    private readonly Dictionary<ulong, bool> selected = new();
    private Group.PivotMode pivot = Group.PivotMode.SelectionCenter;

    public GroupWindow(IGroupService groups) : base("BDTH Group")
    {
      Groups = groups;
      SizeConstraints = new WindowSizeConstraints { MinimumSize = new(320, 200), MaximumSize = new(900, 1200) };
    }

    public override void PreDraw()
    {
      IsOpen &= Memory.IsHousingOpen();
    }

    public unsafe override void Draw()
    {
      if (!Memory.IsHousingOpen()) { ImGui.Text("Open housing first."); return; }

      ImGui.Text("Select furnishings to include in the group.");
      ImGui.Separator();

      if (Plugin.ClientState.LocalPlayer == null)
        return;

      var playerPos = Plugin.ClientState.LocalPlayer.Position;
      if (!Memory.GetFurnishings(out var items, playerPos, Plugin.GetConfiguration().SortByDistance))
      {
        ImGui.Text("No furnishings found.");
        return;
      }

      if (ImGui.BeginTable("grp", 3))
      {
        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 22);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 0);
        ImGui.TableSetupColumn("Sel", ImGuiTableColumnFlags.WidthFixed, 36);
        for (int i = 0; i < items.Count; i++)
        {
          var itemId = (ulong)items[i].Item;
          if (!selected.ContainsKey(itemId)) selected[itemId] = false;

          ImGui.TableNextRow();
          ImGui.TableNextColumn();
          ImGui.TextUnformatted((i+1).ToString());

          ImGui.TableNextColumn();
          var name = ""; ushort icon = 0;
          if (Plugin.TryGetYardObject(items[i].HousingRowId, out var yard)) { name = yard.Item.Value.Name.ToString(); icon = yard.Item.Value.Icon; }
          if (Plugin.TryGetFurnishing(items[i].HousingRowId, out var furn)) { name = furn.Item.Value.Name.ToString(); icon = furn.Item.Value.Icon; }
          if (icon != 0) { Plugin.DrawIcon(icon, new Vector2(18,18)); ImGui.SameLine(); }
          ImGui.TextUnformatted(name == string.Empty ? $"(Row {items[i].HousingRowId})" : name);

          ImGui.TableNextColumn();
          var flag = selected[itemId];
          if (ImGui.Checkbox($"##sel{i}", ref flag)) selected[itemId] = flag;
        }
        ImGui.EndTable();
      }

      ImGui.Separator();

      ImGui.Text("Pivot:"); ImGui.SameLine();
      if (ImGui.RadioButton("First", pivot == Group.PivotMode.FirstItem)) pivot = Group.PivotMode.FirstItem; ImGui.SameLine();
      if (ImGui.RadioButton("Center", pivot == Group.PivotMode.SelectionCenter)) pivot = Group.PivotMode.SelectionCenter; ImGui.SameLine();
      if (ImGui.RadioButton("Bounds", pivot == Group.PivotMode.BoundingBox)) pivot = Group.PivotMode.BoundingBox;

      if (ImGuiComponents.IconButton("grp-capture", Dalamud.Interface.FontAwesomeIcon.ObjectGroup))
      {
        var ids = new List<ulong>();
        foreach (var kv in selected) if (kv.Value) ids.Add(kv.Key);
        Groups.CaptureFromSelection(ids, pivot);
      }
      if (ImGui.IsItemHovered()) ImGui.SetTooltip("Create/Update group from checked items");
      ImGui.SameLine();

      var apply = Groups.ApplyGizmoToGroup;
      if (ImGui.Checkbox("Apply Gizmo to Group", ref apply)) Groups.ApplyGizmoToGroup = apply;
      ImGui.SameLine();

      if (ImGuiComponents.IconButton("grp-clear", Dalamud.Interface.FontAwesomeIcon.Trash))
      {
        Groups.Clear();
        selected.Clear();
      }
      if (ImGui.IsItemHovered()) ImGui.SetTooltip("Clear current group");
    }
  }
}
