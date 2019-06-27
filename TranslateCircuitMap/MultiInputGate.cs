using Microsoft.Z3;

namespace TranslateCircuitMap
{
    class MultiInputGate : Component
    {
        public delegate BoolExpr BuilderFunc(BoolExpr[] inputs);

        public BuilderFunc Function { get; }

        public Component InputLeft { get; set; }

        public Component InputRight { get; set; }

        public override bool IsValid => InputLeft != null && InputRight != null;

        public MultiInputGate(int x, int y, BuilderFunc func)
            : base(x, y)
        {
            Function = func;
        }

        public override BoolExpr BuildExpression()
        {
            return Function(new[] { InputLeft.BuildExpression(), InputRight.BuildExpression() });
        }
    }
}
