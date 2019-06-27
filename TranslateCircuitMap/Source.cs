using Microsoft.Z3;

namespace TranslateCircuitMap
{
    class Source : Component
    {
        public override bool IsValid => true;

        public BoolExpr Value { get; }

        public Source(int x, int y, BoolExpr value)
            : base(x, y)
        {
            Value = value;
        }

        public override BoolExpr BuildExpression()
        {
            return Value;
        }
    }
}
