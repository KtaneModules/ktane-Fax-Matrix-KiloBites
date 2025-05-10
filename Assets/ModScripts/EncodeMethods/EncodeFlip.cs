using System;
using System.Collections.Generic;
using System.Linq;

public class EncodeFlip : EncodeMethod
{
    private readonly bool _isQuadrant;
    private readonly bool _even;

    public EncodeFlip(bool isQuadrant, bool even)
    {
        _isQuadrant = isQuadrant;
        _even = even;
    }

    public override string Name => $"Flip {(_isQuadrant ? "Quadrant" : "Grid")}";

    public override bool[][] EncodeQuadrants(bool[][] quadrants) => null;

    public override bool[] EncodeGrid(bool[] grid)
    {
        var cap = _isQuadrant ? 5 : 10;

        var groups = Enumerable.Range(0, cap).Select(x => Enumerable.Range(0, cap).Select(y => cap * x + y).ToArray()).ToArray();

        var converted = Enumerable.Range(0, cap).Select(_ => new int[cap]).ToArray();

        for (int i = 0; i < cap; i++)
            for (int j = 0; j < cap; j++)
            {
                if (_even)
                    converted[cap - 1 - i][j] = groups[i][j];
                else
                    converted[i][cap - 1 - j] = groups[i][j];
            }

        return converted.SelectMany(x => x.Select(y => grid[y])).ToArray();
    }
}

