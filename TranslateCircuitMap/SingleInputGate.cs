using Microsoft.Z3;

namespace TranslateCircuitMap
{
    class SingleInputGate : Component
    {
        public delegate BoolExpr BuilderFunc(BoolExpr input);

        public BuilderFunc Function { get; }

        public Component Input { get; set; }

        public override bool IsValid => Input != null;

        public SingleInputGate(int x, int y, BuilderFunc func)
            : base(x, y)
        {
            Function = func;
        }

        public override BoolExpr BuildExpression()
        {
            return Function(Input.BuildExpression());
        }
    }
}
