using System.Collections.Generic;
using Features.Tracers.Configs;
using Unity.Mathematics;
using UnityEngine;

namespace Features.Tracers.Utils
{
    /// <summary>
    /// Streaks flying from a start point to an end point, drawn with pooled
    /// <see cref="LineRenderer"/>s (Docs/08 §Tracers). Owned by whichever view draws shots — the
    /// live tracers, the kill cam's replay — each with its own pool.
    /// </summary>
    public sealed class TracerPool
    {
        private readonly TracerConfig _config;
        private readonly string _name;
        private readonly List<Tracer> _live = new List<Tracer>();
        private readonly Stack<LineRenderer> _free = new Stack<LineRenderer>();
        private GameObject _root;

        public TracerPool(TracerConfig config, string name)
        {
            _config = config;
            _name = name;
        }

        /// <summary>
        /// Starts a streak for a shot from <paramref name="eye"/> to <paramref name="end"/>, offset
        /// from the eye to roughly where the weapon is (<see cref="TracerConfig.MuzzleOffset"/>;
        /// <paramref name="sideways"/> uses its x, for a first-person shooter).
        /// </summary>
        public void Spawn(float3 eye, float3 end, bool sideways)
        {
            var aim = end - eye;
            if (math.lengthsq(aim) < 1e-6f)
            {
                return;
            }

            var forward = math.normalize(aim);
            var right = math.normalizesafe(math.cross(new float3(0f, 1f, 0f), forward), new float3(1f, 0f, 0f));
            var up = math.cross(forward, right);
            var offset = (float3)_config.MuzzleOffset;
            var from = eye + right * (sideways ? offset.x : 0f) + up * offset.y + forward * offset.z;

            var path = end - from;
            var distance = math.length(path);
            if (distance < 0.05f)
            {
                return;
            }

            var line = Take();
            line.SetPosition(0, from);
            line.SetPosition(1, from);
            line.enabled = true;

            _live.Add(new Tracer
            {
                Line = line,
                From = from,
                Direction = path / distance,
                Distance = distance,
            });
        }

        public void Update(float deltaTime)
        {
            for (var i = _live.Count - 1; i >= 0; i--)
            {
                var tracer = _live[i];
                tracer.Travelled += _config.Speed * deltaTime;

                var head = math.min(tracer.Travelled, tracer.Distance);
                var tail = math.max(0f, tracer.Travelled - _config.Length);
                if (tail >= tracer.Distance)
                {
                    Free(tracer.Line);
                    _live.RemoveAt(i);
                    continue;
                }

                tracer.Line.SetPosition(0, tracer.From + tracer.Direction * tail);
                tracer.Line.SetPosition(1, tracer.From + tracer.Direction * head);
                _live[i] = tracer;
            }
        }

        /// <summary>Stops every live streak.</summary>
        public void Clear()
        {
            for (var i = 0; i < _live.Count; i++)
            {
                Free(_live[i].Line);
            }

            _live.Clear();
        }

        public void Dispose()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            _root = null;
            _live.Clear();
            _free.Clear();
        }

        private void Free(LineRenderer line)
        {
            if (line == null)
            {
                return;
            }

            line.enabled = false;
            _free.Push(line);
        }

        private LineRenderer Take()
        {
            while (_free.Count > 0)
            {
                var pooled = _free.Pop();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            if (_root == null)
            {
                _root = new GameObject(_name);
            }

            var go = new GameObject("Tracer");
            go.transform.SetParent(_root.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.sharedMaterial = _config.Material;
            line.widthMultiplier = _config.Width;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            var color = _config.Color;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(color.a, 1f) });
            line.colorGradient = gradient;
            return line;
        }

        private struct Tracer
        {
            public LineRenderer Line;
            public float3 From;
            public float3 Direction;
            public float Distance;
            public float Travelled;
        }
    }
}
