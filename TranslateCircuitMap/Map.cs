using Microsoft.Z3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace TranslateCircuitMap
{
    class Map
    {
        private static readonly Dictionary<Rgba32, Tile> Colors = new Dictionary<Rgba32, Tile> {
            { new Rgba32(255, 255, 255), Tile.None          },
            { new Rgba32(  0, 255,   0), Tile.Lamp          },
            { new Rgba32(255,   0,   0), Tile.Lever         },
            { new Rgba32( 20,  20,  20), Tile.Wire          },
            { new Rgba32( 70,  70,  70), Tile.WireCorner    },
            { new Rgba32( 40,  40,  40), Tile.WireJunctionT },
            { new Rgba32( 60,  60,  60), Tile.WireCrossover },
            { new Rgba32(  0,   0, 110), Tile.GateAnd       },
            { new Rgba32(  0,   0, 140), Tile.GateNot       },
            { new Rgba32(  0,   0, 170), Tile.GateOr        },
            { new Rgba32(  0,   0, 200), Tile.GateXor       },
            { new Rgba32(180,   0,   0), Tile.Clamp         }
        };

        public Context Context { get; }

        private Image<Rgba32> Source { get; }

        private ImageFrame<Rgba32> Frame { get; }

        public ReadOnlyCollection<Component> Components { get; }

        public int Width => Frame.Width;

        public int Height => Frame.Height;

        public int ClampCount { get; private set; }

        private Dictionary<ValueTuple<int, int>, ClampDirection> DegradedJunctions { get; }

        public Map(string path)
        {
            Context = new Context();
            Source = Image.Load(path);
            Frame = Source.Frames.RootFrame;
            Components = ExtractComponents().ToList().AsReadOnly();
            DegradedJunctions = new Dictionary<ValueTuple<int, int>, ClampDirection>();
        }

        public void SaveStateAsPng(string path)
        {
            using (var stream = File.OpenWrite(path))
            {
                Source.SaveAsPng(stream);
            }
        }

        public Tile this[int x, int y]
        {
            get
            {
                return Colors[Frame[x, y]];
            }
        }

        private IEnumerable<Component> ExtractComponents()
        {
            int sourceCount = 0;
            for (int x = 0; x < Width; ++x)
            {
                for (int y = 0; y < Height; ++y)
                {
                    switch (this[x, y])
                    {
                        case Tile.GateAnd:
                            yield return new MultiInputGate(x, y, Context.MkAnd);
                            break;

                        case Tile.GateNot:
                            yield return new SingleInputGate(x, y, Context.MkNot);
                            break;

                        case Tile.GateOr:
                            yield return new MultiInputGate(x, y, Context.MkOr);
                            break;

                        case Tile.GateXor:
                            yield return new MultiInputGate(x, y, Context.MkXor);
                            break;

                        case Tile.Lamp:
                            yield return new Drain(x, y);
                            break;

                        case Tile.Lever:
                            yield return new Source(x, y, Context.MkBoolConst($"S{++sourceCount:D3}"));
                            break;
                    }
                }
            }
        }

        public void ClampDown(int x, int y)
        {
            var current = this[x, y];
            if (current == Tile.WireCrossover)
            {
                Frame[x, y] = new Rgba32(20, 20, 20);
            }
            else
            {
                Frame[x, y] = new Rgba32(180, 0, 0);

                if (current == Tile.WireJunctionT)
                {
                    DegradedJunctions.Remove((x, y));
                }
            }
            ++ClampCount;
        }

        public void DegradeJunction(int x, int y, ClampDirection direction)
        {
            DegradedJunctions[(x, y)] = direction;
        }

        public ClampDirection GetJunctionClampDirection(int x, int y)
        {
            if (DegradedJunctions.TryGetValue((x, y), out ClampDirection value))
            {
                return value;
            }
            else
            {
                return ClampDirection.None;
            }
        }
    }
}
