using System;
using System.Collections.Generic;
using System.Numerics;

namespace Fantabode.Groups
{
  public sealed class Group
  {
    public enum PivotMode { SelectionCenter, BoundingBox, FirstItem }

    public readonly PivotMode Pivot;
    public readonly IReadOnlyList<ulong> ItemIds; // (ulong)HousingItem* pointers
    public readonly IReadOnlyList<Matrix4x4> LocalFromPivot; // itemLocal = inv(pivotWorld) * itemWorld
    public readonly Matrix4x4 PivotWorld; // capture-time pivot

    public Group(PivotMode pivot, IReadOnlyList<ulong> itemIds, IReadOnlyList<Matrix4x4> localFromPivot, in Matrix4x4 pivotWorld)
    {
      if (itemIds.Count != localFromPivot.Count) throw new ArgumentException("Mismatched lists");
      Pivot = pivot;
      ItemIds = itemIds;
      LocalFromPivot = localFromPivot;
      PivotWorld = pivotWorld;
    }
  }
}
