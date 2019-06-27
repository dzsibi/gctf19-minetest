using System;

namespace TranslateCircuitMap
{
    [Flags]
    enum ClampDirection
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4
    }
}
