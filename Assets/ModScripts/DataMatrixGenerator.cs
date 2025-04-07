using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZXing;
using ZXing.Datamatrix;

public class DataMatrixGenerator
{
    public Sprite GenerateDataMatrix(string message)
    {
        IBarcodeWriter writer = new BarcodeWriter
        {
            Format = BarcodeFormat.DATA_MATRIX,
            Options = new DatamatrixEncodingOptions
            {
                Height = 256,
                Width = 256
            }
        };

        var dm = writer.Write(message);
        var encoded = new Texture2D(256, 256);

        encoded.SetPixels32(dm);
        encoded.Apply();

        var converted = Sprite.Create(encoded, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));

        converted.texture.filterMode = FilterMode.Point;

        return converted;
    }
}