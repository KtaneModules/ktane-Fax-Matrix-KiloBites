using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NonogramPuzzle
{
    private Sprite _dataMatrix;

    public NonogramPuzzle(Sprite dataMatrix)
    {
        _dataMatrix = dataMatrix;
    }

    public bool[] GetFullClusters() => Reduce(false).Select(x => x == Color.black).ToArray();

    public bool[] GetPuzzleClusters() => Reduce(true).Select(x => x == Color.black).ToArray();

    private Color[] Reduce(bool useForPuzzle)
    {
        var _2dConvert = Enumerable.Range(0, 144).Select(x => new Vector2Int(256 * x % 12, 256 * x / 12)).ToArray();

        var exclude = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 23, 35, 47, 59, 71, 83, 95, 107, 119, 131, 143, 142, 141, 140, 139, 138, 137, 136, 135, 134, 133, 132, 120, 108, 96, 84, 72, 60, 48, 36, 24, 12 };

        return (useForPuzzle ? Enumerable.Range(0, 144).Where(x => !exclude.Contains(x)) : Enumerable.Range(0, 144)).Reverse().Select(x => _dataMatrix.texture.GetPixel(_2dConvert[x].x, _2dConvert[x].y)).ToArray();
    }
}
