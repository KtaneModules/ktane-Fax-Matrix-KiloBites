using System;
using System.Collections.Generic;
using System.Linq;

public abstract class EncodeMethod
{
    public abstract string Name { get; }

    public abstract bool[] EncodeGrid(bool[] grid);
    public abstract bool[][] EncodeQuadrants(bool[][] quadrants);
}
