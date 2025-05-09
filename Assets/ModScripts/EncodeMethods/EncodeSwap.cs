using System;
using System.Collections.Generic;
using System.Linq;

public class EncodeSwap : EncodeMethod
{
    private readonly int _ixA;
    private readonly int _ixB;

    public override string Name => "Swap";

    public EncodeSwap(int ixA, int ixB)
    {
        _ixA = ixA;
        _ixB = ixB;
    }

    public override bool[] EncodeGrid(bool[] grid) => null;

    public override bool[][] EncodeQuadrants(bool[][] quadrants)
    {
        var modified = quadrants.ToArray();

        var temp = modified[_ixA].ToArray();
        var swap = modified[_ixB].ToArray();

        modified[_ixA] = swap;
        modified[_ixB] = temp;

        return modified;
    }
}