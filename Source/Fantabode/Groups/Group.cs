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
    public readonly IReadOnlyList<Vector3> StartPositions;
    public readonly IReadOnlyList<Vector3> StartRotations;
    public IReadOnlyList<Vector3>? EndPositions { get; private set; }
    public IReadOnlyList<Vector3>? EndRotations { get; private set; }

    public Group(
      PivotMode pivot,
      IReadOnlyList<ulong> itemIds,
      IReadOnlyList<Matrix4x4> localFromPivot,
      in Matrix4x4 pivotWorld,
      IReadOnlyList<Vector3> startPositions,
      IReadOnlyList<Vector3> startRotations)
    {
      if (itemIds.Count != localFromPivot.Count ||
          itemIds.Count != startPositions.Count ||
          itemIds.Count != startRotations.Count)
        throw new ArgumentException("Mismatched lists");
      Pivot = pivot;
      ItemIds = itemIds;
      LocalFromPivot = localFromPivot;
      PivotWorld = pivotWorld;
      StartPositions = startPositions;
      StartRotations = startRotations;
    }

    public void SetEndPositions(IReadOnlyList<Vector3> end)
      => EndPositions = end;

    public void SetEndRotations(IReadOnlyList<Vector3> end)
      => EndRotations = end;
  }
}
