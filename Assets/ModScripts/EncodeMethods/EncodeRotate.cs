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

        var rotated = new[] { 0, 1, 3, 2 };

        for (int i = 0; i < 4; i++)
            rotated[i] = (_even ? (rotated[i] - 1 + 4) : (rotated[i] + 1)) % 4;

        return rotated.Select(x => modified[x]).ToArray();
    }
}