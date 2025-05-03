using System.Collections.Generic;
using System.Linq;

public class DataMatrixEncoder
{
    private bool[] _dataMatrix;
    private bool[] _entireDataMatrix;

    public List<List<string>> Logs = new List<List<string>>();

    public DataMatrixEncoder(bool[] dataMatrix, bool[] entireDataMatrix)
    {
        _dataMatrix = dataMatrix;
        _entireDataMatrix = entireDataMatrix;
    }

    private enum HexDirections
    {
        Up,
        Up_Right,
        Down_Right,
        Down,
        Down_Left,
        Up_Left
    }

    private static readonly string[] hexPositions = { "Top", "Top Left", "Top Right", "Center", "Bottom Left", "Bottom Right", "Bottom" };

    private enum EncodeMethod
    {
        XOR,
        XNOR,
        Swap,
        Nothing,
        Rotate,
        ShiftRow,
        ShiftCol
    }

    private static readonly EncodeMethod[][] hexGrid = new[]
    {
        new[] { 6, 1, 2, 3, 1, 2 },
        new[] { 4, 0, 3, 4, 0, 5 },
        new[] { 5, 4, 0, 5, 3, 0 },
        new[] { 0, 2, 5, 6, 4, 1 },
        new[] { 1, 3, 6, 1, 2, 6 },
        new[] { 2, 6, 1, 2, 6, 3 },
        new[] { 3, 5, 4, 0, 5, 4 }
    }.Select(x => x.Select(y => (EncodeMethod)y).ToArray()).ToArray();

    private EncodeMethod currentPosition = EncodeMethod.Nothing;

    public bool[] EncodeDataMatrix(string sn)
    {
        var alpha = "-ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var convertedDirections = sn.Select(x => (char.IsLetter(x) ? alpha.IndexOf(x) : int.Parse(x.ToString())) % 6).Select(x => (HexDirections)x).ToArray();
        var convertedBools = sn.Select(x => (char.IsLetter(x) ? alpha.IndexOf(x) : int.Parse(x.ToString())) % 2 == 0).ToArray();

        var encoded = _dataMatrix.ToArray();
        var encodedQuadrants = GenerateQuadrants(encoded);
        var methods = new List<EncodeMethod>();
        var loggedPositions = new List<string>();
        var finishedLogs = new List<List<string>>();

        var before = new List<string> { "Before encoding:", "Full Data Matrx:" };
        before.AddRange(LogFullDataMatrix(_entireDataMatrix));
        before.Add("10x10:");
        before.AddRange(LogEntireGrid(_dataMatrix));
        Logs.Add(before);

        for (int i = 0; i < convertedDirections.Length; i++)
        {
            loggedPositions.Add($"Going {convertedDirections[i].ToString().Replace('_', ' ')} from {hexPositions[(int)currentPosition]} to {hexPositions[(int)hexGrid[(int)currentPosition][(int)convertedDirections[i]]]}:");
            currentPosition = hexGrid[(int)currentPosition][(int)convertedDirections[i]];
            methods.Add(currentPosition);
        }

        for (int i = methods.Count - 1; i >= 0; i--)
        {
            var ixA = (char.IsLetter(sn[i]) ? alpha.IndexOf(sn[i]) : int.Parse(sn[i].ToString())) % 4;
            var ixB = (char.IsLetter(sn[(i + 1) % 6]) ? alpha.IndexOf(sn[(i + 1) % 6]) : int.Parse(sn[(i + 1) % 6].ToString())) % 4;

            var logs = new List<string> { loggedPositions[i] };

            if (ixA == ixB)
            {
                ixB++;
                ixB %= 4;
            }

            switch (methods[i])
            {
                case EncodeMethod.XOR:
                    logs.Add($"XORing quadrant {ixA + 1} with quadrant {ixB + 1}");
                    logs.Add($"Quadrant {ixA + 1}:");
                    logs.AddRange(LogQuadrant(encodedQuadrants[ixA]));
                    logs.Add($"Quadrant {ixB + 1}:");
                    logs.AddRange(LogQuadrant(encodedQuadrants[ixB]));
                    encodedQuadrants[ixA] = XOR(encodedQuadrants[ixA], encodedQuadrants[ixB]);
                    encoded = QuadrantsToFull(encodedQuadrants);
                    logs.Add($"After XORing quadrant {ixA + 1} with quadrant {ixB + 1}:");
                    logs.AddRange(LogQuadrant(encodedQuadrants[ixA]));
                    break;
                case EncodeMethod.XNOR:
                    logs.Add($"XNORing quadrant {ixA + 1} with quadrant {ixB + 1}");
                    logs.Add($"Quadrant {ixA + 1}:");
                    logs.AddRange(LogQuadrant(encodedQuadrants[ixA]));
                    logs.Add($"Quadrant {ixB + 1}:");
                    logs.AddRange(LogQuadrant(encodedQuadrants[ixB]));
                    encodedQuadrants[ixA] = XNOR(encodedQuadrants[ixA], encodedQuadrants[ixB]);
                    encoded = QuadrantsToFull(encodedQuadrants);
                    logs.Add($"After XNORing quadrant {ixA + 1} with quadrant {ixB + 1}:");
                    logs.AddRange(LogQuadrant(encodedQuadrants[ixA]));
                    break;
                case EncodeMethod.Swap:
                    logs.Add($"Swapping quadrant {ixA + 1} with quadrant {ixB + 1}");
                    logs.Add($"Quadrant {ixA + 1}:");
                    logs.AddRange(LogQuadrant(encodedQuadrants[ixA]));
                    logs.Add($"Quadrant {ixB + 1}:");
                    logs.AddRange(LogQuadrant(encodedQuadrants[ixB]));
                    encodedQuadrants = Swap(ixA, ixB, encodedQuadrants);
                    logs.Add($"After swapping quadrant {ixA + 1} with quadrant {ixB + 1}:");
                    logs.Add($"Quadrant {ixA + 1}:");
                    logs.AddRange(LogQuadrant(encodedQuadrants[ixA]));
                    logs.Add($"Quadrant {ixB + 1}:");
                    logs.AddRange(LogQuadrant(encodedQuadrants[ixB]));
                    encoded = QuadrantsToFull(encodedQuadrants);
                    break;
                case EncodeMethod.Nothing:
                    logs.Add("Do nothing to the grid/quadrants");
                    break;
                case EncodeMethod.Rotate:
                    logs.Add($"Rotating the quadrants {(convertedBools[i] ? "Counterclockwise" : "Clockwise")}");
                    logs.Add("Before rotating the quadrants:");
                    logs.AddRange(LogEntireGrid(encoded));
                    encodedQuadrants = Rotate(convertedBools[i], encodedQuadrants);
                    encoded = QuadrantsToFull(encodedQuadrants);
                    break;
                case EncodeMethod.ShiftRow:
                    logs.Add($"Shifting row {(convertedBools[i] ? "down" : "up")} by one");
                    logs.Add("Before shifting:");
                    logs.AddRange(LogEntireGrid(encoded));
                    encoded = Shift(true, convertedBools[i], encoded);
                    encodedQuadrants = GenerateQuadrants(encoded);
                    break;
                case EncodeMethod.ShiftCol:
                    logs.Add($"Shifting column {(convertedBools[i] ? "left" : "right")} by one");
                    logs.Add("Before shifting:");
                    logs.AddRange(LogEntireGrid(encoded));
                    encoded = Shift(false, convertedBools[i], encoded);
                    encodedQuadrants = GenerateQuadrants(encoded);
                    break;
            }

            logs.Add("After modifying:");
            logs.AddRange(LogEntireGrid(encoded));

            finishedLogs.Add(logs);
        }

        finishedLogs.Reverse();

        Logs.AddRange(finishedLogs);

        var finalLog = new List<string> { "Final encoded grid:" };
        finalLog.AddRange(LogEntireGrid(encoded));

        Logs.Add(finalLog);

        return encoded;
    }

    private bool[][] GenerateQuadrants(bool[] current)
    {
        var quadrants = new[]
        {
            new int[25] { 0, 1, 2, 3, 4, 10, 11, 12, 13, 14, 20, 21, 22, 23, 24, 30, 31, 32, 33, 34, 40, 41, 42, 43, 44 }, // TL
            new int[25] { 5, 6, 7, 8, 9, 15 ,16, 17, 18, 19, 25, 26, 27, 28, 29, 35, 36, 37, 38, 39, 45, 46, 47, 48, 49 }, // TR
            new int[25] { 55, 56, 57, 58, 59, 65, 66, 67, 68, 69, 75, 76, 77, 78, 79, 85, 86, 87, 88, 89, 95, 96, 97, 98, 99 }, // BR
            new int[25] { 50, 51, 52, 53, 54, 60, 61, 62, 63, 64, 70, 71, 72, 73, 74, 80, 81, 82, 83, 84, 90, 91, 92, 93, 94 } // BL
        };

        return quadrants.Select(x => x.Select(y => current[y]).ToArray()).ToArray();
    }

    private bool[] QuadrantsToFull(bool[][] quadrants)
    {
        var full = new bool[100];

        var converted = quadrants.SelectMany(x => x.Select(y => y)).ToArray();

        var quadrantIxes = new[]
{
            new int[25] { 0, 1, 2, 3, 4, 10, 11, 12, 13, 14, 20, 21, 22, 23, 24, 30, 31, 32, 33, 34, 40, 41, 42, 43, 44 }, // TL
            new int[25] { 5, 6, 7, 8, 9, 15 ,16, 17, 18, 19, 25, 26, 27, 28, 29, 35, 36, 37, 38, 39, 45, 46, 47, 48, 49 }, // TR
            new int[25] { 55, 56, 57, 58, 59, 65, 66, 67, 68, 69, 75, 76, 77, 78, 79, 85, 86, 87, 88, 89, 95, 96, 97, 98, 99 }, // BR
            new int[25] { 50, 51, 52, 53, 54, 60, 61, 62, 63, 64, 70, 71, 72, 73, 74, 80, 81, 82, 83, 84, 90, 91, 92, 93, 94 } // BL
        };

        foreach (var quadrant in quadrantIxes)
            for (int i = 0; i < quadrant.Length; i++)
            {
                var ix = quadrant[i];
                full[ix] = converted[i];
            }

        return full;
    }

    private bool[] Shift(bool isRow, bool even, bool[] current)
    {
        var region = Enumerable.Range(0, 10).Select(x => Enumerable.Range(0, 10).Select(y => isRow ? y + x * 10 : y * 10 + x).ToArray()).ToArray();

        var shifted = Enumerable.Range(0, 10).ToArray();

        for (int i = 0; i < 10; i++)
        {
            if (even)
            {
                if (isRow)
                    shifted[i] = (shifted[i] + 1) % 10;
                else
                    shifted[i] = (shifted[i] - 1 + 10) % 10;
            }
            else
            {
                if (isRow)
                    shifted[i] = (shifted[i] - 1 + 10) % 10;
                else
                    shifted[i] = (shifted[i] + 1) % 10;
            }

            shifted[i] %= 10;
        }

        return shifted.SelectMany(x => region[x].Select(y => current[y])).ToArray();
    }

    private bool[][] Swap(int a, int b, bool[][] current)
    {
        var modified = current.ToArray();

        var temp = modified[a];
        var swap = modified[b];

        modified[a] = swap;
        modified[b] = temp;

        return modified;
    }

    private bool[] XOR(bool[] a, bool[] b)
    {
        var output = new bool[25];

        for (int i = 0; i < output.Length; i++)
            output[i] = a[i] ^ b[i];

        return output;
    }

    private bool[] XNOR(bool[] a, bool[] b)
    {
        var output = new bool[25];

        for (int i = 0; i < output.Length; i++)
            output[i] = a[i] == b[i];

        return output;
    }

    private bool[][] Rotate(bool even, bool[][] current)
    {
        var modified = current.ToArray();

        var rotated = Enumerable.Range(0, 4).ToArray();

        for (int i = 0; i < 4; i++)
        {
            if (even)
                rotated[i] = (rotated[i] - 1 + 4) % 4;
            else
                rotated[i] = (rotated[i] + 1) % 4;
        }

        return rotated.Select(x => modified[x]).ToArray();
    }

    private string[] LogQuadrant(bool[] quadrant)
    {
        var final = Enumerable.Range(0, 5).Select(x => Enumerable.Range(0, 5).Select(y => quadrant[y + x * 5] ? '▣' : '□').Join("")).Join("\n");

        return final.Split('\n');
    }

    private string[] LogEntireGrid(bool[] grid)
    {
        var final = Enumerable.Range(0, 10).Select(x => Enumerable.Range(0, 10).Select(y => grid[y + x * 10] ? '▣' : '□').Join("")).Join("\n");

        return final.Split('\n');
    }

    private string[] LogFullDataMatrix(bool[] matrix)
    {
        var final = Enumerable.Range(0, 12).Select(x => Enumerable.Range(0, 12).Select(y => matrix[y + x * 12] ? '▣' : '□').Join("")).Join("\n");

        return final.Split('\n');
    }
}