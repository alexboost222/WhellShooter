using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Benchmarks.Physics
{
    /// <summary>
    /// Unity's built-in PhysX, stepped by hand in its own local physics scene so nothing else in
    /// the editor shares its broadphase.
    /// </summary>
    /// <remarks>
    /// Characters are <see cref="CharacterController"/>s moved before each simulate, the closest
    /// stock equivalent of phys-r's kinematic character. Rays are <c>PhysicsScene.Raycast</c> one
    /// by one, or a <see cref="RaycastCommand"/> batch on the job system.
    /// </remarks>
    public sealed class PhysXBackend : IPhysicsBackend
    {
        private const float CharacterGravity = 20f;
        private const float WalkSpeed = 6f;
        private const float JumpSpeed = 7f;

        private readonly List<Rigidbody> _props = new List<Rigidbody>();
        private readonly List<CharacterController> _characters = new List<CharacterController>();
        private readonly List<float> _verticalSpeeds = new List<float>();

        private Scene _scene;
        private PhysicsScene _physics;
        private BenchmarkScene _source;
        private NativeArray<RaycastCommand> _commands;
        private NativeArray<RaycastHit> _hits;

        public string Name
        {
            get { return "PhysX"; }
        }

        public void Build(BenchmarkScene scene)
        {
            _source = scene;
            _scene = SceneManager.CreateScene("PhysXBenchmark " + scene.Name,
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _physics = _scene.GetPhysicsScene();

            var root = new GameObject("Level");
            SceneManager.MoveGameObjectToScene(root, _scene);

            for (var i = 0; i < scene.Statics.Count; i++)
            {
                var box = scene.Statics[i];
                var go = new GameObject("Static");
                go.transform.SetParent(root.transform, false);
                go.transform.SetPositionAndRotation(box.Center,
                    Quaternion.AngleAxis(box.Yaw * Mathf.Rad2Deg, Vector3.up));
                go.AddComponent<BoxCollider>().size = box.HalfExtents * 2f;
            }

            for (var i = 0; i < scene.Props.Count; i++)
            {
                var go = new GameObject("Prop");
                go.transform.SetParent(root.transform, false);
                go.transform.position = scene.Props[i];
                go.AddComponent<BoxCollider>().size = Vector3.one * (BenchmarkScene.PropHalf * 2f);
                var body = go.AddComponent<Rigidbody>();
                body.mass = BenchmarkScene.PropMass;
                if (!scene.Sleep)
                {
                    body.sleepThreshold = 0f;
                }

                _props.Add(body);
            }

            for (var i = 0; i < scene.Characters.Count; i++)
            {
                var go = new GameObject("Character");
                go.transform.SetParent(root.transform, false);
                go.transform.position = scene.Characters[i];
                var controller = go.AddComponent<CharacterController>();
                controller.radius = 0.4f;
                controller.height = 1.8f;
                controller.stepOffset = 0.35f;
                controller.slopeLimit = 45f;
                controller.skinWidth = 0.04f;
                _characters.Add(controller);
                _verticalSpeeds.Add(0f);
            }

            _commands = new NativeArray<RaycastCommand>(Mathf.Max(1, scene.RaysPerTick), Allocator.Persistent);
            _hits = new NativeArray<RaycastHit>(Mathf.Max(1, scene.RaysPerTick), Allocator.Persistent);

            // Transforms were written after the colliders were added.
            UnityEngine.Physics.SyncTransforms();
        }

        public void Step(int tick)
        {
            for (var i = 0; i < _characters.Count; i++)
            {
                BenchmarkScene.CharacterInput(i, tick, out var yaw, out var jump);
                var controller = _characters[i];
                var vertical = _verticalSpeeds[i];
                if (controller.isGrounded)
                {
                    vertical = jump ? JumpSpeed : -1f;
                }
                else
                {
                    vertical -= CharacterGravity * BenchmarkScene.Dt;
                }

                var velocity = new Vector3(Mathf.Sin(yaw) * WalkSpeed, vertical, Mathf.Cos(yaw) * WalkSpeed);
                controller.Move(velocity * BenchmarkScene.Dt);
                _verticalSpeeds[i] = vertical;
            }

            _physics.Simulate(BenchmarkScene.Dt);
        }

        public int CastRays(int tick, bool batched)
        {
            var count = _source.RaysPerTick;
            var hits = 0;
            if (!batched)
            {
                for (var i = 0; i < count; i++)
                {
                    BenchmarkScene.Ray(i, tick, out var origin, out var direction);
                    if (_physics.Raycast(origin, direction, out _, BenchmarkScene.RayLength))
                    {
                        hits++;
                    }
                }

                return hits;
            }

            for (var i = 0; i < count; i++)
            {
                BenchmarkScene.Ray(i, tick, out var origin, out var direction);
                _commands[i] = new RaycastCommand(_physics, origin, direction, QueryParameters.Default,
                    BenchmarkScene.RayLength);
            }

            RaycastCommand.ScheduleBatch(_commands.GetSubArray(0, count), _hits.GetSubArray(0, count), 16)
                .Complete();

            for (var i = 0; i < count; i++)
            {
                if (_hits[i].colliderInstanceID != 0)
                {
                    hits++;
                }
            }

            return hits;
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
                sum += _props[i].position.y;
            }

            return sum / _props.Count;
        }

        public int SleepingProps()
        {
            var sleeping = 0;
            for (var i = 0; i < _props.Count; i++)
            {
                if (_props[i].IsSleeping())
                {
                    sleeping++;
                }
            }

            return sleeping;
        }

        public void Dispose()
        {
            if (_commands.IsCreated)
            {
                _commands.Dispose();
            }

            if (_hits.IsCreated)
            {
                _hits.Dispose();
            }

            if (_scene.IsValid())
            {
                SceneManager.UnloadSceneAsync(_scene);
            }

            _props.Clear();
            _characters.Clear();
            _verticalSpeeds.Clear();
        }
    }
}
