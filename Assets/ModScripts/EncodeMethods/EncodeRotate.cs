using System;
using System.Collections.Generic;
using System.Linq;

public class EncodeRotate : EncodeMethod
{
    private readonly bool _even;

    public EncodeRotate(bool even)
    {
        _even = even;
    }

    public override string Name => $"Rotating Quadrants {(_even ? "CCW" : "CW")}";

    public override bool[] EncodeGrid(bool[] grid) => null;

    public override bool[][] EncodeQuadrants(bool[][] quadrants)
    {
        var modified = quadrants.ToArray();

        var rotated = _even ? new[] { 1, 3, 0, 2 } : new[] { 2, 0, 3, 1 };

        return rotated.Select(x => modified[x]).ToArray();
    }
}