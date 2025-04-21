using System;
using System.Collections.Generic;
using System.Linq;

public class NonogramGenerator
{
    private bool[] _dataMatrixClusters;

    public NonogramGenerator(bool[] dataMatrixClusters)
    {
        _dataMatrixClusters = dataMatrixClusters;
    }

    private string[] _rowClues = new string[10], columnClues = new string[10];
}
