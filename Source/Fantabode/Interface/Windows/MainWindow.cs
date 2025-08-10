using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;

using Fantabode.Interface.Components;
using Fantabode.Services;
using Fantabode.Groups;
using HousingManager = FFXIVClientStructs.FFXIV.Client.Game.HousingManager;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Fantabode.Interface.Windows
{
  public class MainWindow : Window
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Configuration Configuration => Plugin.GetConfiguration();
    private static GroupService Groups => Plugin.GetGroups();

    private static readonly Vector4 RED_COLOR = new(1, 0, 0, 1);

    private readonly Gizmo Gizmo;
    private readonly ItemControls ItemControls = new();
    private readonly Dictionary<ulong, bool> _checked = new();
    private Group.PivotMode pivot = Group.PivotMode.SelectionCenter;

    public bool Reset;

    public MainWindow(Gizmo gizmo) : base(
      "Fantabode##Fantabode",
      ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize |
      ImGuiWindowFlags.AlwaysAutoResize
    )
    {
      Gizmo = gizmo;
    }

    public override void PreDraw()
    {
      if (Reset)
      {
        Reset = false;
        ImGui.SetNextWindowPos(new Vector2(69, 69), ImGuiCond.Always);
      }
    }

    public unsafe override void Draw()
    {
      if (ImGui.BeginTabBar("fantabode-main"))
      {
        if (ImGui.BeginTabItem("Controls"))
        {
          DrawControls();
          ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Groups"))
        {
          DrawGroups();
          ImGui.EndTabItem();
        }
        ImGui.EndTabBar();
      }
    }

    private unsafe void DrawControls()
    {
      ImGui.BeginGroup();

      var mgr = HousingManager.Instance();
      if (mgr != null)
      {
        var plot = mgr->GetCurrentPlot();
        ImGui.Text($"Ward: {mgr->GetCurrentWard()}, Division: {mgr->GetCurrentDivision()}, Plot: {plot}, Room: {mgr->GetCurrentRoom()}");

        if (ImGui.IsItemHovered() && plot > 0 && mgr->OutdoorTerritory != null)
        {
          var outdoor = mgr->OutdoorTerritory;
          var index = plot - 1;
          var detailPtr = (PlotDetail*)((byte*)outdoor + 0x96B8);
          var detail = detailPtr[index];
          ImGui.BeginTooltip();
          ImGui.Text($"Owner: {detail.OwnerType}");
          ImGui.Text($"Size: {detail.Size}");
          var icon = outdoor->GetPlotIcon((byte)index);
          Plugin.DrawIcon((ushort)icon, new Vector2(24, 24));
          ImGui.EndTooltip();
        }
      }
      var hasPerms = mgr != null && mgr->HasHousePermissions();
      if (!hasPerms)
        DrawError("No housing permissions");
      ImGui.BeginDisabled(!hasPerms);

        var placeAnywhere = Configuration.PlaceAnywhere;
        if (ImGui.Checkbox("Place Anywhere", ref placeAnywhere))
        {
          if (hasPerms)
          {
            Memory.SetPlaceAnywhere(placeAnywhere);
            Configuration.PlaceAnywhere = placeAnywhere;
            Configuration.Save();
          }
          else
          {
            Plugin.Chat.PrintError("No housing permissions");
          }
        }
        DrawTooltip("Allows the placement of objects without limitation from the game engine.");

      ImGui.SameLine();

      var useGizmo = Configuration.UseGizmo;
      if (ImGui.Checkbox("Gizmo", ref useGizmo))
      {
        Configuration.UseGizmo = useGizmo;
        Configuration.Save();
      }
      DrawTooltip("Displays a movement gizmo on the selected item to allow for in-game movement on all axis.");

      ImGui.SameLine();

      var doSnap = Configuration.DoSnap;
      if (ImGui.Checkbox("Snap", ref doSnap))
      {
        Configuration.DoSnap = doSnap;
        Configuration.Save();
      }
      DrawTooltip("Enables snapping of gizmo movement based on the drag value set below.");

      ImGui.SameLine();
      if (ImGuiComponents.IconButton(1, Gizmo.Mode == ImGuizmoMode.Local ? Dalamud.Interface.FontAwesomeIcon.ArrowsAlt : Dalamud.Interface.FontAwesomeIcon.Globe))
        Gizmo.Mode = Gizmo.Mode == ImGuizmoMode.Local ? ImGuizmoMode.World : ImGuizmoMode.Local;

      DrawTooltip(
      [
        $"Mode: {(Gizmo.Mode == ImGuizmoMode.Local ? "Local" : "World")}",
        "Changes gizmo mode between local and world movement.",
      ]);

      ImGui.Separator();

      if (Memory.HousingStructure->Mode == HousingLayoutMode.None)
        DrawError("Enter housing mode to get started");
      else if (PluginMemory.GamepadMode)
        DrawError("Does not support Gamepad");
      else if (Memory.HousingStructure->ActiveItem == null || Memory.HousingStructure->Mode != HousingLayoutMode.Rotate)
      {
        DrawError("Select a housing item in Rotate mode");
        ImGuiComponents.HelpMarker("Are you doing everything right? Try using the /fantabode debug command and report this issue in Discord!");
      }
      else
        ItemControls.Draw();

      ImGui.Separator();

      var drag = Configuration.Drag;
      if (ImGui.InputFloat("drag", ref drag, 0.05f))
      {
        drag = Math.Min(Math.Max(0.001f, drag), 10f);
        Configuration.Drag = drag;
        Configuration.Save();
      }
      DrawTooltip("Sets the amount to change when dragging the controls, also influences the gizmo snap feature.");

      var dummyHousingGoods = PluginMemory.HousingGoods != null && PluginMemory.HousingGoods.IsVisible;
      var dummyInventory = Memory.InventoryVisible;

      if (ImGui.Checkbox("Display in-game list", ref dummyHousingGoods))
      {
        Memory.ShowFurnishingList(dummyHousingGoods);

        Configuration.DisplayFurnishingList = dummyHousingGoods;
        Configuration.Save();
      }
      ImGui.SameLine();

      if (ImGui.Checkbox("Display inventory", ref dummyInventory))
      {
        Memory.ShowInventory(dummyInventory);

        Configuration.DisplayInventory = dummyInventory;
        Configuration.Save();
      }

      if (ImGui.Button("Open Furnishing List"))
        Plugin.CommandManager.ProcessCommand("/fantabode list");
      DrawTooltip(
      [
        "Opens a furnishing list that you can use to sort by distance and click to select objects.",
        "NOTE: Does not currently work outdoors!",
      ]);
      ImGui.SameLine();
      if (ImGui.Button("Apply Group"))
        Groups.StartApply();
      ImGui.SameLine();
      if (ImGui.Button("Cancel Preview"))
        Groups.Clear();

      var applyToGroup = Groups.ApplyGizmoToGroup;
      if (ImGui.Checkbox("Gizmo → Group", ref applyToGroup))
        Groups.ApplyGizmoToGroup = applyToGroup;

      var autoVisible = Configuration.AutoVisible;
      if (ImGui.Checkbox("Auto Open", ref autoVisible))
      {
        Configuration.AutoVisible = autoVisible;
        Configuration.Save();
      }
      ImGui.EndDisabled();
    }

    private unsafe void DrawGroups()
    {
      ImGui.Text("Pivot:"); ImGui.SameLine();
      if (ImGui.RadioButton("First", pivot == Group.PivotMode.FirstItem)) pivot = Group.PivotMode.FirstItem;
      ImGui.SameLine();
      if (ImGui.RadioButton("Center", pivot == Group.PivotMode.SelectionCenter)) pivot = Group.PivotMode.SelectionCenter;
      ImGui.SameLine();
      if (ImGui.RadioButton("Bounds", pivot == Group.PivotMode.BoundingBox)) pivot = Group.PivotMode.BoundingBox;

      if (Plugin.ClientState.LocalPlayer == null)
        return;

      var playerPos = Plugin.ClientState.LocalPlayer.Position;
      if (!Memory.GetFurnishings(out var items, playerPos, Plugin.GetConfiguration().SortByDistance))
      {
        ImGui.Text("No furnishings found.");
      }
      else
      {
        if (_checked.Count == 0 && Groups.Current != null)
          foreach (var id in Groups.Current.ItemIds)
            _checked[id] = true;

        if (ImGui.BeginChild("grp_table", new Vector2(0, 300), true))
        {
          if (ImGui.BeginTable("grp", 3))
          {
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 22);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 0);
            ImGui.TableSetupColumn("Sel", ImGuiTableColumnFlags.WidthFixed, 36);
            for (int i = 0; i < items.Count; i++)
            {
              var itemId = (ulong)items[i].Item;
              if (!_checked.ContainsKey(itemId))
                _checked[itemId] = Groups.Current?.ItemIds.Contains(itemId) ?? false;

              ImGui.TableNextRow();
              ImGui.TableNextColumn();
              ImGui.TextUnformatted((i + 1).ToString());

              ImGui.TableNextColumn();
              var name = ""; ushort icon = 0;
              if (Plugin.TryGetYardObject(items[i].HousingRowId, out var yard)) { name = yard.Item.Value.Name.ToString(); icon = yard.Item.Value.Icon; }
              if (Plugin.TryGetFurnishing(items[i].HousingRowId, out var furn)) { name = furn.Item.Value.Name.ToString(); icon = furn.Item.Value.Icon; }
              if (icon != 0) { Plugin.DrawIcon(icon, new Vector2(18,18)); ImGui.SameLine(); }
              ImGui.TextUnformatted(name == string.Empty ? $"(Row {items[i].HousingRowId})" : name);

              ImGui.TableNextColumn();
              var flag = _checked[itemId];
              if (ImGui.Checkbox($"##sel{i}", ref flag)) _checked[itemId] = flag;
            }
            ImGui.EndTable();
          }
          ImGui.EndChild();
        }
      }

      if (ImGui.Button("Create/Update group"))
      {
        var ids = new List<ulong>();
        foreach (var kv in _checked) if (kv.Value) ids.Add(kv.Key);
        Groups.CaptureFromSelection(ids, pivot);
        Groups.ApplyGizmoToGroup = true;
      }
      ImGui.SameLine();
      if (ImGui.Button("Clear"))
      {
        Groups.Clear();
        _checked.Clear();
      }
      if (ImGui.Button("Apply Group")) Groups.StartApply();
      ImGui.SameLine();
      if (ImGui.Button("Cancel Preview")) Groups.Clear();
      var apply = Groups.ApplyGizmoToGroup;
      if (ImGui.Checkbox("Gizmo → Group", ref apply)) Groups.ApplyGizmoToGroup = apply;

      var status = Groups.Current == null ? "none" : "active";
      var count = Groups.Current?.ItemIds.Count ?? 0;
      ImGui.Text($"Current group: {status} ({count} items)");
    }

    private static void DrawTooltip(string[] text)
    {
      if (ImGui.IsItemHovered())
      {
        ImGui.BeginTooltip();
        foreach (var t in text)
          ImGui.Text(t);
        ImGui.EndTooltip();
      }
    }

    private static void DrawTooltip(string text)
    {
      DrawTooltip([text]);
    }

    private void DrawError(string text)
    {
      ImGui.PushStyleColor(ImGuiCol.Text, RED_COLOR);
      ImGui.Text(text);
      ImGui.PopStyleColor();
    }
  }
}
