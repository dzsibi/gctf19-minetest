using Microsoft.Z3;
using System;
using System.Linq;
using System.Threading;

namespace TranslateCircuitMap
{
    class Program
    {
        static bool IsIrrelevant(Tile c)
        {
            switch (c)
            {
                case Tile.None:
                case Tile.Clamp:
                case Tile.Lever:
                    return true;

                default:
                    return false;
            }
        }

        static bool? CanEnterFromSide(Map map, int x, int y, int direction)
        {
            switch (map[x, y])
            {
                case Tile.WireJunctionT:
                    if (IsIrrelevant(map[x, y + 1]))
                    {
                        return true;
                    }
                    else
                    {
                        return null;
                    }

                case Tile.Wire:
                    if (IsIrrelevant(map[x, y + 1]) || IsIrrelevant(map[x, y - 1]))
                    {
                        return true;
                    }
                    else
                    {
                        for (int i = x + direction; i < map.Width && i >= 0; i++)
                        {
                            var current = map[i, y];
                            if (IsIrrelevant(current))
                            {
                                return false;
                            }
                            else if (current != Tile.Wire || current != Tile.WireCrossover)
                            {
                                return null;
                            }
                        }
                        return false;
                    }

                case Tile.WireCorner:
                    if (IsIrrelevant(map[x + direction, y]))
                    {
                        return true;
                    }
                    else
                    {
                        return null;
                    }

                case Tile.WireCrossover:
                case Tile.GateAnd:
                case Tile.GateOr:
                case Tile.GateXor:
                    return true;

                case Tile.Lamp:
                case Tile.GateNot:
                case Tile.None:
                case Tile.Clamp:
                    return false;

                default:
                    throw new NotImplementedException("Unknown or unsupported component");
            }
        }

        static bool? CanEnterFromBottom(Map map, int x, int y)
        {
            switch (map[x, y])
            {
                case Tile.WireJunctionT:
                    if (IsIrrelevant(map[x, y - 1]) || IsIrrelevant(map[x - 1, y]) || IsIrrelevant(map[x + 1, y]))
                    {
                        return true;
                    }
                    else
                    {
                        return null;
                    }

                case Tile.Wire:
                    if (IsIrrelevant(map[x - 1, y]) || IsIrrelevant(map[x + 1, y]))
                    {
                        return true;
                    }
                    else
                    {
                        for (int i = y - 1; i >= 0; i--)
                        {
                            var current = map[x, i];
                            if (IsIrrelevant(current))
                            {
                                return false;
                            }
                            else if (current != Tile.Wire || current != Tile.WireCrossover)
                            {
                                return null;
                            }
                        }
                        return null;
                    }

                case Tile.WireCorner:
                    if (IsIrrelevant(map[x, y - 1]))
                    {
                        return true;
                    }
                    else
                    {
                        return null;
                    }

                case Tile.WireCrossover:
                case Tile.Lamp:
                case Tile.GateNot:
                    return true;

                case Tile.None:
                case Tile.Clamp:
                case Tile.GateAnd:
                case Tile.GateOr:
                case Tile.GateXor:
                    return false;

                default:
                    throw new NotImplementedException("Unknown or unsupported component");
            }
        }

        static bool TraceComponent(Map map, Component component, int x, int y, int sourceX, int sourceY)
        {
            var propagated = true;
            switch (map[x, y])
            {
                case Tile.Wire:
                case Tile.WireCrossover:
                    propagated = TraceComponent(map, component, 2 * x - sourceX, 2 * y - sourceY, x, y);
                    break;

                case Tile.WireCorner when sourceY == y:
                    propagated = TraceComponent(map, component, x, y - 1, x, y);
                    break;

                case Tile.WireCorner:
                    var cornerLeft = CanEnterFromSide(map, x + 1, y, 1);
                    var cornerRight = CanEnterFromSide(map, x - 1, y, -1);
                    if (cornerLeft == cornerRight)
                    {
                        propagated = false;
                    }
                    else if (cornerLeft ?? (cornerRight.HasValue && !cornerRight.Value))
                    {
                        propagated = TraceComponent(map, component, x + 1, y, x, y);
                    }
                    else
                    {
                        propagated = TraceComponent(map, component, x - 1, y, x, y);
                    }
                    break;

                case Tile.WireJunctionT:
                    var junctionLeft = CanEnterFromSide(map, x + 1, y, 1);
                    var junctionRight = CanEnterFromSide(map, x - 1, y, -1);
                    var junctionBottom = CanEnterFromBottom(map, x, y - 1);
                    var clampDirection = map.GetJunctionClampDirection(x, y);

                    int trueCount = 0;
                    int falseCount = 0;
                    if (junctionLeft == true)    ++trueCount;
                    if (junctionLeft == false)   ++falseCount;
                    if (junctionRight == true)   ++trueCount;
                    if (junctionRight == false)  ++falseCount;
                    if (junctionBottom == true)  ++trueCount;
                    if (junctionBottom == false) ++falseCount;

                    bool hasEnough = false;
                    bool defaultValue = false;
                    if (falseCount == 1 || (falseCount == 2 && clampDirection != ClampDirection.None))
                    {
                        hasEnough = true;
                        defaultValue = true;
                    }
                    else if (trueCount == 2)
                    {
                        hasEnough = true;
                        defaultValue = false;
                    }

                    if (hasEnough)
                    {
                        int propagationCount = 0;
                        if (!clampDirection.HasFlag(ClampDirection.Up) && (junctionBottom ?? defaultValue))
                        {
                            var result = TraceComponent(map, component, x, y - 1, x, y);
                            if (result)
                            {
                                ++propagationCount;
                                map.DegradeJunction(x, y, ClampDirection.Up);
                            }
                        }
                        if (!clampDirection.HasFlag(ClampDirection.Right) && (junctionLeft ?? defaultValue))
                        {
                            var result = TraceComponent(map, component, x + 1, y, x, y);
                            if (result)
                            {
                                ++propagationCount;
                                map.DegradeJunction(x, y, ClampDirection.Right);
                            }
                        }
                        if (!clampDirection.HasFlag(ClampDirection.Left) && (junctionRight ?? defaultValue))
                        {
                            var result = TraceComponent(map, component, x - 1, y, x, y);
                            if (result)
                            {
                                ++propagationCount;
                                map.DegradeJunction(x, y, ClampDirection.Left);
                            }
                        }
                        var expectedCount = clampDirection != ClampDirection.None ? 1 : 2;
                        if (propagationCount < expectedCount)
                        {
                            propagated = false;
                        }
                    }
                    else
                    {
                        propagated = false;
                    }
                    break;

                case Tile.GateAnd:
                case Tile.GateOr:
                case Tile.GateXor:
                    var target = map.Components.OfType<MultiInputGate>().Single(e => e.X == x && e.Y == y);
                    if (x > sourceX)
                    {
                        target.InputRight = component;
                    }
                    else
                    {
                        target.InputLeft = component;
                    }
                    return true;

                case Tile.Lamp:
                case Tile.GateNot:
                    map.Components.OfType<SingleInputGate>().Single(e => e.X == x && e.Y == y).Input = component;
                    return true;

                case Tile.Clamp:
                    return true;

                default:
                    throw new NotImplementedException("Unknown or unsupported component");
            }
            if (propagated)
            {
                map.ClampDown(x, y);
            }
            return propagated;
        }

        static void Worker(object parameter)
        {
            var map = new Map((string)parameter);

            int lastClampCount = 0;
            // int iterationCount = 0;
            while (map.Components.Any(e => !e.IsValid))
            {
                foreach (var component in map.Components.Where(e => e.IsValid && !e.Processed).ToArray())
                {
                    component.Processed = TraceComponent(map, component, component.X, component.Y - 1, component.X, component.Y);
                }

                if (map.ClampCount == lastClampCount)
                {
                    throw new Exception("Iteration is stuck");
                }
                else
                {
                    lastClampCount = map.ClampCount;
                    // map.SaveStateAsPng($"Iteration{++iterationCount:D3}.png");
                }
            }

            var solver = map.Context.MkSimpleSolver();
            var drain = map.Components.OfType<Drain>().Single();
            solver.Assert(drain.BuildExpression());
            if (solver.Check() != Status.SATISFIABLE)
            {
                throw new Exception("Equation unsatisfiable");
            }

            var combination = String.Concat(solver.Model.Consts.OrderBy(e => e.Key.Name.ToString()).Select(e => e.Value.BoolValue == Z3_lbool.Z3_L_TRUE ? "1" : "0"));
            Console.WriteLine("CTF{" + combination + "}");
        }

        static void Main(string[] args)
        {
            var thread = new Thread(Worker, 256 * 1024 * 1024);
            thread.Start(args[0]);
            thread.Join();
        }
    }
}
