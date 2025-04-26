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

    private int _lastDigitSN;

    public NonogramPuzzle(int lastDigitSN)
    {
        _lastDigitSN = lastDigitSN;
    }

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
            _generator = new NonogramGenerator(XORPattern(GetPuzzleClusters().ToArray()));

            if (_generator.IsUnique())
            {
                horizClues = _generator.PrintedHorizClues;
                vertClues = _generator.PrintedVertClues;
                return;
            }
        }
        

        throw new Exception("Cannot generator nonogram.");
    }

    private bool[] XORPattern(bool[] dataMatrix)
    {
        var patterns = new[]
        {
            new[]
            {
                "..xx..xx..",
                "..xxxxxx..",
                "...xxxx...",
                Enumerable.Repeat("....xx....", 4).Join(""),
                "...xxxx...",
                "..xxxxxx..",
                "..xx..xx.."
            }.Join(""),
            new[]
            {
                "..xxxxx...",
                ".x.....x..",
                "x..xxx..x.",
                "x.x.x.x..x",
                Enumerable.Repeat("x.x..x.x.x", 2).Join(""),
                "x..xx.x..x",
                ".x...x..x.",
                "..xxx..x..",
                "......x..."
            }.Join(""),
            new[]
            {
                Enumerable.Repeat("........xx", 3).Join(""),
                Enumerable.Repeat(".....xxxxx", 4).Join(""),
                "......xxxx",
                ".......xxx",
                "........xx"
            }.Join(""),
            new[]
            {
                "xxxx..xxxx",
                "x...xx...x",
                "xxxx..xxxx",
                Enumerable.Repeat("xx.x..x.xx", 2).Join(""),
                "x.x....x.x",
                ".xxxxxxxx.",
                "..xxxxxx..",
                "...x..x...",
                "....xx...."
            }.Join(""),
            new[]
            {
                "...xxxx...",
                "..xxxxxx..",
                Enumerable.Repeat('x', 10).Join(""),
                Enumerable.Repeat("x...xx...x", 2).Join(""),
                Enumerable.Repeat(Enumerable.Repeat('x', 10).Join(""), 2).Join(""),
                "xxx.xx.xxx",
                Enumerable.Repeat(".xx.xx.xx.", 2).Join("")
            }.Join(""),
            new[]
            {
                "....xx....",
                "...x..x...",
                "..x....x..",
                ".x.xxxx.x.",
                "x...xx...x",
                ".xxxxxxxx.",
                ".x..xx..x.",
                "..x.xx.x..",
                "....xx....",
                ".xxxxxxxx."
            }.Join(""),
            new[]
            {
                "....xx....",
                "...xxxx...",
                "..xxxxxx..",
                ".xxxxxxxx.",
                Enumerable.Repeat(Enumerable.Repeat('x', 10).Join(""), 3).Join(""),
                ".xx.xx.xx.",
                "....xx....",
                "..xxxxxx.."
            }.Join(""),
            new[]
            {
                string.Empty,
                "..x....x..",
                Enumerable.Repeat(".x.x..x.x.", 2).Join(""),
                "..x....x..",
                string.Empty,
                Enumerable.Range(0, 4).Select(x => x % 2 == 0 ? "..xxxxxx.." : "..x....x..").Join("")
            }.Join(""),
            new[]
            {
                Enumerable.Repeat('x', 10).Join(""),
                "x........x",
                "x.xxxxxx.x",
                Enumerable.Repeat("x.x....x.x", 4).Join(""),
                "x.xxxxxx.x",
                Enumerable.Repeat('x', 10).Join("")
            }.Join(""),
            new[]
            {
                ".xxxxxxxx.",
                Enumerable.Repeat("x..x..x..x", 5).Join(""),
                ".xxxxxxxx.",
                Enumerable.Repeat("....xx....", 3).Join("")
            }.Join("")
        }.Select(x => x.Select(y => y == 'x').ToArray()).ToArray();

        var selectedPattern = patterns[_lastDigitSN];

        return Enumerable.Range(0, 100).Select(x => dataMatrix[x] ^ selectedPattern[x]).ToArray();
    }
}
