using System.Collections.Generic;
using PhysR;
using UnityEngine;

namespace Benchmarks.Physics
{
    /// <summary>
    /// phys-r (deterministic fixed-point Rust engine) through its raw <see cref="PhysWorld"/>
    /// binding — no MonoBehaviour components, so the numbers are the engine plus the P/Invoke hop.
    /// </summary>
    public sealed class PhysRBackend : IPhysicsBackend
    {
        private readonly uint _threads;
        private readonly List<BodyHandle> _props = new List<BodyHandle>();
        private readonly List<CharacterHandle> _characters = new List<CharacterHandle>();

        private PhysWorld _world;
        private BenchmarkScene _source;
        private RayQuery[] _rays;
        private RayHit[] _hits;

        /// <param name="threads">Worker threads; 0 = automatic (up to 4), 1 = single-threaded.</param>
        public PhysRBackend(uint threads)
        {
            _threads = threads;
        }

        public string Name
        {
            get { return _threads == 0 ? "phys-r (auto threads)" : "phys-r (" + _threads + " thread)"; }
        }

        public void Build(BenchmarkScene scene)
        {
            _source = scene;

            var settings = WorldSettings.Default;
            settings.Dt = FP.FromRatio(1, 60);
            settings.EnableSleep = scene.Sleep ? 1u : 0u;
            settings.Threads = _threads;
            _world = new PhysWorld(settings);

            var shapes = new Dictionary<Vector3, ShapeId>();
            for (var i = 0; i < scene.Statics.Count; i++)
            {
                var box = scene.Statics[i];
                if (!shapes.TryGetValue(box.HalfExtents, out var shape))
                {
                    shape = _world.AddBox(ToFP(box.HalfExtents));
                    shapes.Add(box.HalfExtents, shape);
                }

                var desc = BodyDesc.Default;
                desc.Kind = BodyKind.Static;
                desc.Shape = shape;
                desc.Position = ToFP(box.Center);
                desc.Rotation = FPQuaternion.FromYaw(FP.FromFloat(box.Yaw));
                _world.CreateBody(desc);
            }

            var propShape = _world.AddBox(ToFP(Vector3.one * BenchmarkScene.PropHalf));
            for (var i = 0; i < scene.Props.Count; i++)
            {
                var desc = BodyDesc.Default;
                desc.Kind = BodyKind.Dynamic;
                desc.Shape = propShape;
                desc.Position = ToFP(scene.Props[i]);
                desc.Rotation = FPQuaternion.Identity;
                desc.Mass = FP.FromFloat(BenchmarkScene.PropMass);
                _props.Add(_world.CreateBody(desc));
            }

            for (var i = 0; i < scene.Characters.Count; i++)
            {
                var desc = CharacterDesc.Default;
                desc.Position = ToFP(scene.Characters[i]);
                _characters.Add(_world.CreateCharacter(desc));
            }

            _rays = new RayQuery[Mathf.Max(1, scene.RaysPerTick)];
            _hits = new RayHit[_rays.Length];
        }

        public void Step(int tick)
        {
            for (var i = 0; i < _characters.Count; i++)
            {
                BenchmarkScene.CharacterInput(i, tick, out var yaw, out var jump);
                var input = new CharacterInput
                {
                    MoveZ = FP.One,
                    Yaw = ToFP(yaw),
                    Buttons = jump ? CharacterButtons.Jump : CharacterButtons.None,
                };
                _world.SetInput(_characters[i], input);
            }

            _world.Step();
        }

        public int CastRays(int tick, bool batched)
        {
            var count = _source.RaysPerTick;
            var length = ToFP(BenchmarkScene.RayLength);
            var hits = 0;
            if (!batched)
            {
                for (var i = 0; i < count; i++)
                {
                    BenchmarkScene.Ray(i, tick, out var origin, out var direction);
                    if (_world.Raycast(ToFP(origin), ToFP(direction), length, out _))
                    {
                        hits++;
                    }
                }

                return hits;
            }

            for (var i = 0; i < count; i++)
            {
                BenchmarkScene.Ray(i, tick, out var origin, out var direction);
                _rays[i] = new RayQuery(ToFP(origin), ToFP(direction), length, QueryFilter.All);
            }

            return _world.RaycastBatch(_rays, _hits, count);
        }

        public float AveragePropHeight()
        {
            if (_props.Count == 0)
            {
                return 0f;
            }

            var sum = 0f;
            for (var i = 0; i < _props.Count; i++)
            {
                sum += _world.GetState(_props[i]).Position.Y.ToFloat();
            }

            return sum / _props.Count;
        }

        public int SleepingProps()
        {
            var sleeping = 0;
            for (var i = 0; i < _props.Count; i++)
            {
                if (_world.GetState(_props[i]).Sleeping != 0)
                {
                    sleeping++;
                }
            }

            return sleeping;
        }

        public void Dispose()
        {
            _world?.Dispose();
            _world = null;
            _props.Clear();
            _characters.Clear();
        }

        private static FPVector3 ToFP(Vector3 v)
        {
            return new FPVector3(ToFP(v.x), ToFP(v.y), ToFP(v.z));
        }

        /// <summary>
        /// Managed float → Q40.24. <see cref="FP.FromFloat"/> is a P/Invoke per component (exact
        /// across platforms, which a benchmark doesn't need); in a real game ray origins and inputs
        /// are already FP, so the conversion cost is kept out of the measurement.
        /// </summary>
        private static FP ToFP(float v)
        {
            return FP.FromRaw((long)System.Math.Round(v * (double)FP.OneRaw));
        }
    }
}
