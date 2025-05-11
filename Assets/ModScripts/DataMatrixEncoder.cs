using System.Collections.Generic;
using System.Linq;



public class DataMatrixEncoder
{
    private readonly bool[] _dataMatrix;
    private readonly bool[] _entireDataMatrix;

    public DataMatrixEncoder(bool[] dataMatrix, bool[] entireDataMatrix)
    {
        _dataMatrix = dataMatrix;
        _entireDataMatrix = entireDataMatrix;
    }

    enum HexDirection
    {
        Up,
        Up_Right,
        Down_Right,
        Down,
        Down_Left,
        Up_Left
    }

    enum HexPosition
    {
        Top,
        Top_Left,
        Top_Right,
        Center,
        Bottom_Left,
        Bottom_Right,
        Bottom
    }

    public List<List<string>> Logged = new List<List<string>>();


    private static readonly HexPosition[][] hexGrid = new[]
    {
        new[] { 6, 1, 2, 3, 1, 2 },
        new[] { 4, 0, 3, 4, 0, 5 },
        new[] { 5, 4, 0, 5, 3, 0 },
        new[] { 0, 2, 5, 6, 4, 1 },
        new[] { 1, 3, 6, 1, 2, 6 },
        new[] { 2, 6, 1, 2, 6, 3 },
        new[] { 3, 5, 4, 0, 5, 4 }
    }.Select(x => x.Select(y => (HexPosition)y).ToArray()).ToArray();

    private HexPosition currentPosition = HexPosition.Center;

    public bool[] EncodeDataMatrix(string sn)
    {
        var alpha = "-ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var convertedDirections = sn.Select(x => (char.IsLetter(x) ? alpha.IndexOf(x) : int.Parse(x.ToString())) % 6).Select(x => (HexDirection)x).ToArray();
        var convertedBools = sn.Select(x => (char.IsLetter(x) ? alpha.IndexOf(x) : int.Parse(x.ToString())) % 2 == 0).ToArray();

        var encoded = _dataMatrix.ToArray();
        var encodedQuadrants = GenerateQuadrants(encoded);

        var loggedDirections = new List<string>();
        var trackedDirections = new List<HexPosition>();

        Logged.Add(new List<string> { "Full Data Matrix:", LogFullDataMatrix(_entireDataMatrix) });
        Logged.Add(new List<string> { "Before modifying grid:", LogEntireGrid(encoded) });

        for (int i = 0; i < 6; i++)
        {
            loggedDirections.Add($"Going {convertedDirections[i].ToString().Replace('_', '-')} from {currentPosition.ToString().Replace('_', '-')} to {hexGrid[(int)currentPosition][(int)convertedDirections[i]].ToString().Replace('_', '-')}");
            currentPosition = hexGrid[(int)currentPosition][(int)convertedDirections[i]];
            trackedDirections.Add(currentPosition);
        }

        for (int i = trackedDirections.Count - 1; i >= 0; i--)
        {
            var ixA = (char.IsLetter(sn[i]) ? alpha.IndexOf(sn[i]) : int.Parse(sn[i].ToString())) % 4;
            var ixB = (char.IsLetter(sn[(i + 1) % 6]) ? alpha.IndexOf(sn[(i + 1) % 6]) : int.Parse(sn[(i + 1) % 6].ToString())) % 4;

            var list = new List<string>();

            EncodeMethod[] methods =
            {
                new EncodeFlip(true, convertedBools[i]),
                new EncodeFlip(false, convertedBools[i]),
                new EncodeSwap(ixA, ixB),
                null,
                new EncodeRotate(convertedBools[i]),
                new EncodeShift(true, convertedBools[i]),
                new EncodeShift(false, convertedBools[i])
            };

            if (ixA == ixB)
                ixB = (ixB + 1) % 4;

            list.Add($"{loggedDirections[i]}: {methods[(int)trackedDirections[i]].Name ?? "Nothing"}");

            switch (trackedDirections[i])
            {
                case HexPosition.Top:
                    list.Add($"Before flipping quadrant {ixA + 1}:");
                    list.Add(LogQuadrant(encodedQuadrants[ixA]));
                    encodedQuadrants[ixA] = methods[(int)trackedDirections[i]].EncodeGrid(encodedQuadrants[ixA]);
                    encoded = QuadrantsToFull(encodedQuadrants);
                    list.Add($"After flipping quadrant {ixA + 1}:");
                    list.Add(LogQuadrant(encodedQuadrants[ixA]));
                    break;
                case HexPosition.Top_Left:
                    list.Add("Before flipping grid:");
                    list.Add(LogEntireGrid(encoded));
                    encoded = methods[(int)trackedDirections[i]].EncodeGrid(encoded);
                    encodedQuadrants = GenerateQuadrants(encoded);
                    list.Add("After flipping grid:");
                    list.Add(LogEntireGrid(encoded));
                    break;
                case HexPosition.Top_Right:
                    list.Add($"Before swapping quadrant {ixA + 1} and quadrant {ixB + 1}:");
                    list.Add($"Quadrant {ixA + 1}:");
                    list.Add(LogQuadrant(encodedQuadrants[ixA]));
                    list.Add($"Quadrant {ixB + 1}:");
                    list.Add(LogQuadrant(encodedQuadrants[ixB]));
                    encodedQuadrants = methods[(int)trackedDirections[i]].EncodeQuadrants(encodedQuadrants);
                    encoded = QuadrantsToFull(encodedQuadrants);
                    list.Add($"After swapping quadrant {ixA + 1} and quadrant {ixB + 1}:");
                    list.Add($"Quadrant {ixA + 1}:");
                    list.Add(LogQuadrant(encodedQuadrants[ixA]));
                    list.Add($"Quadrant {ixB + 1}:");
                    list.Add(LogQuadrant(encodedQuadrants[ixB]));
                    break;
                case HexPosition.Center:
                    list.Add("Do nothing to the grid/quadrant");
                    break;
                case HexPosition.Bottom_Left:
                    list.Add("Before rotating quadrants:");
                    list.Add(LogEntireGrid(encoded));
                    encodedQuadrants = methods[(int)trackedDirections[i]].EncodeQuadrants(encodedQuadrants);
                    encoded = QuadrantsToFull(encodedQuadrants);
                    list.Add("After rotating quadrants:");
                    list.Add(LogEntireGrid(encoded));
                    break;
                case HexPosition.Bottom_Right:
                    list.Add("Before shifting row:");
                    list.Add(LogEntireGrid(encoded));
                    encoded = methods[(int)trackedDirections[i]].EncodeGrid(encoded);
                    encodedQuadrants = GenerateQuadrants(encoded);
                    list.Add("After shifting row:");
                    list.Add(LogEntireGrid(encoded));
                    break;
                case HexPosition.Bottom:
                    list.Add("Before shifting column:");
                    list.Add(LogEntireGrid(encoded));
                    encoded = methods[(int)trackedDirections[i]].EncodeGrid(encoded);
                    encodedQuadrants = GenerateQuadrants(encoded);
                    list.Add("After shifting column:");
                    list.Add(LogEntireGrid(encoded));
                    break;
            }

            Logged.Add(list);
        }

        Logged.Add(new List<string> { "Final Grid:", LogEntireGrid(encoded) });

        return encoded;
    }

    private bool[][] GenerateQuadrants(bool[] current)
    {

        var quadrants = Enumerable.Range(0, 4).Select(x => Enumerable.Range(0, 25).Select(y => 5 * (x % 2) + y % 5 + (50 * (x / 2) + 10 * (y / 5))).ToArray()).ToArray();

        return quadrants.Select(x => x.Select(y => current[y]).ToArray()).ToArray();
    }

    private bool[] QuadrantsToFull(bool[][] quadrants)
    {
        var full = new bool[100];

        var quadrantIxes = Enumerable.Range(0, 4).Select(x => Enumerable.Range(0, 25).Select(y => 5 * (x % 2) + y % 5 + (50 * (x / 2) + 10 * (y / 5))).ToArray()).ToArray();

        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 25; j++)
            {
                var ix = quadrantIxes[i][j];
                full[ix] = quadrants[i][j];
            }

        return full;
    }

    private string LogQuadrant(bool[] quadrant) => Enumerable.Range(0, 5).Select(r => Enumerable.Range(0, 5).Select(c => quadrant[5 * r + c] ? 'X' : '.').Join("")).Join("\n");

    private string LogEntireGrid(bool[] grid) => Enumerable.Range(0, 10).Select(r => Enumerable.Range(0, 10).Select(c => grid[10 * r + c] ? 'X' : '.').Join("")).Join("\n");

    private string LogFullDataMatrix(bool[] matrix) => Enumerable.Range(0, 12).Select(r => Enumerable.Range(0, 12).Select(c => matrix[12 * r + c] ? 'X' : '.').Join("")).Join("\n");
}