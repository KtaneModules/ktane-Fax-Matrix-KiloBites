using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Random;

public class NonogramPuzzle
{
    public Sprite DataMatrix;

    private int[] generatedNumbers;

    private NonogramGenerator _generator;

    public bool[] GetFullClusters() => Reduce(false).Select(x => x == Color.black).ToArray();

    public bool[] GetPuzzleClusters() => Reduce(true).Select(x => x == Color.black).ToArray();

    private Color[] Reduce(bool useForPuzzle)
    {

        var exclude = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 23, 35, 47, 59, 71, 83, 95, 107, 119, 131, 143, 142, 141, 140, 139, 138, 137, 136, 135, 134, 133, 132, 120, 108, 96, 84, 72, 60, 48, 36, 24, 12 };

        return (useForPuzzle ? Enumerable.Range(0, 144).Where(x => !exclude.Contains(x)) : Enumerable.Range(0, 144)).Select(x => DataMatrix.texture.GetPixel(x % 12, 11 - (x / 12))).ToArray();
    }

    public void Generate(out List<string> horizClues, out List<string> vertClues)
    {
        var q = new Queue<int[]>(Enumerable.Range(0, 20).Select(x => Enumerable.Range(0, 7).Select(_ => Range(0, 10)).ToArray()));


        
        while (q.Count > 0)
        {
            var nums = q.Dequeue();
            Debug.Log(nums.Join(""));
            DataMatrix = DataMatrixGenerator.GenerateDataMatrix(nums.Join(""));
            _generator = new NonogramGenerator(GetPuzzleClusters());

            if (_generator.IsUnique())
            {
                horizClues = _generator.PrintedHorizClues;
                vertClues = _generator.PrintedVertClues;
                return;
            }
        }
        

        throw new Exception("Cannot generator nonogram.");
    }
}
