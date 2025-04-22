using System;
using System.Collections.Generic;
using System.Linq;

public class NonogramGenerator
{
    private bool[] _dataMatrixClusters;


    private List<int>[] hintsHoriz = new List<int>[10].ToArray();
    private List<int>[] hintsVert = new List<int>[10].ToArray();

    private List<bool[]>[] rowCombinations = new List<bool[]>[10], colCombinations = new List<bool[]>[10];

    public List<string> PrintedHorizClues, PrintedVertClues;

    public bool IsUnique() => solutions == 1;

    public NonogramGenerator(bool[] dataMatrixClusters)
    {
        _dataMatrixClusters = dataMatrixClusters;
        GeneratePuzzle();
    }

    void GeneratePuzzle()
    {
        for (int i = 0; i < 10; i++)
        {
            hintsHoriz[i] = new List<int>();
            var count = 0;

            for (int j = 0; j < 10; j++)
            {
                if (_dataMatrixClusters[i * 10 + j])
                    count++;

                else if (count > 0)
                {
                    hintsHoriz[i].Add(count);
                    count = 0;
                }
            }

            if (count > 0)
                hintsHoriz[i].Add(count);
        }

        for (int i = 0; i < 10; i++)
        {
            hintsVert[i] = new List<int>();
            var count = 0;

            for (int j = 0; j < 10; j++)
            {
                if (_dataMatrixClusters[i + j * 10])
                    count++;

                else if (count > 0)
                {
                    hintsVert[i].Add(count);
                    count = 0;
                }
            }

            if (count > 0)
                hintsVert[i].Add(count);
        }

        PrintedHorizClues = hintsHoriz.Select(x => x.Join()).ToList();
        PrintedVertClues = hintsVert.Select(x => x.Join()).ToList();

        FindPossibleRowCombinations();
        FindPossibleColCombinations();
        Solve(0);
    }

    void FindPossibleRowCombinations()
    {
        for (int i = 0; i < 10; i++)
        {
            rowCombinations[i] = new List<bool[]>();

            var positions = new int[hintsHoriz[i].Count];

            RecurseRow(0, 0, i, positions);
        }
    }

    void FindPossibleColCombinations()
    {
        for (int i = 0; i < 10; i++)
        {
            colCombinations[i] = new List<bool[]>();

            var positions = new int[hintsVert[i].Count];

            RecurseCol(0, 0, i, positions);
        }
    }

    void RecurseRow(int depth, int min, int ix, int[] pos)
    {
        if (depth == pos.Length)
        {
            var filledRow = new bool[10];

            for (int i = 0; i < pos.Length; i++)
                for (int j = 0; j < hintsHoriz[ix][i]; j++)
                    filledRow[pos[i] + j] = true;

            rowCombinations[ix].Add(filledRow);
            return;
        }

        var length = hintsHoriz[ix][depth];

        for (int i = min; i < 10 - length + 1; i++)
        {
            pos[depth] = i;
            RecurseRow(depth + 1, i + length + 1, ix, pos);
        }
    }

    void RecurseCol(int depth, int min, int ix, int[] pos)
    {
        if (depth == pos.Length)
        {
            var filledCol = new bool[10];

            for (int i = 0; i < pos.Length; i++)
                for (int j = 0; j < hintsVert[ix][i]; j++)
                    filledCol[pos[i] + j] = true;

            colCombinations[ix].Add(filledCol);
            return;
        }

        var length = hintsVert[ix][depth];

        for (int i = min; i < 10 - length + 1; i++)
        {
            pos[depth] = i;
            RecurseCol(depth + 1, i + length + 1, ix, pos);
        }
    }

    bool[] partialSolution = new bool[100];
    int solutions = 0;

    void Solve(int depth)
    {
        if (depth == 10)
        {
            solutions++;
            return;
        }

        for (int i = 0; i < rowCombinations[depth].Count; i++)
        {
            for (int j = 0; j < 10; j++)
                partialSolution[depth * 10 + j] = rowCombinations[depth][i][j];

            var partialValid = true;

            for (int col = 0; col < 10; col++)
            {
                var validCol = false;

                for (int colCombination = 0; colCombination < colCombinations[col].Count; colCombination++)
                {
                    var validComb = true;

                    for (int row = 0; row < depth + 1; row++)
                        if (partialSolution[row * 10 + col] ^ colCombinations[col][colCombination][row])
                        {
                            validComb = false;
                            break;
                        }

                    if (validComb)
                    {
                        validCol = true;
                        break;
                    }
                }

                if (!validCol)
                {
                    partialValid = false;
                    break;
                }
            }

            if (partialValid)
                Solve(depth + 1);
        }

    }
}
