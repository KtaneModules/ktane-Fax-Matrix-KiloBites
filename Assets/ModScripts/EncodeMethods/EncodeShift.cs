using System;
using System.Collections.Generic;
using System.Linq;

public class EncodeShift : EncodeMethod
{
    private readonly bool _isRow;
    private readonly bool _even;

    public EncodeShift(bool isRow, bool even)
    {
        _isRow = isRow;
        _even = even;
    }



    public override string Name => $"Shift {(_isRow ? "Row" : "Column")}";

    public override bool[][] EncodeQuadrants(bool[][] quadrants) => null;

    public override bool[] EncodeGrid(bool[] grid)
    {
        var groups = Enumerable.Range(0, 10).Select(x => Enumerable.Range(0, 10).Select(y => 10 * x + y).ToArray()).ToArray();

        var shifts = Enumerable.Range(0, 10).ToArray();

        for (int i = 0; i < 10; i++)
        {
            if (_isRow)
                shifts[i] = (_even ? (shifts[i] - 1 + 10) : (shifts[i] + 1)) % 10;
            else
                shifts[i] = (_even ? (shifts[i] + 1) : (shifts[i] - 1 + 10)) % 10;
        }

        var converted = Enumerable.Range(0, 10).Select(_ => new int[10]).ToArray();

        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 10; j++)
                converted[i][j] = _isRow ? groups[shifts[i]][j] : groups[i][shifts[j]];

        return converted.SelectMany(x => x.Select(y => grid[y])).ToArray();
    }

}
