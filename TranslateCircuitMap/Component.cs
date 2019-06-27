using Microsoft.Z3;

namespace TranslateCircuitMap
{
    abstract class Component
    {
        public int X { get; }

        public int Y { get; }

        public abstract bool IsValid { get; }

        public bool Processed { get; set; }

        protected Component(int x, int y)
        {
            X = x;
            Y = y;
        }

        public abstract BoolExpr BuildExpression();
    }
}
