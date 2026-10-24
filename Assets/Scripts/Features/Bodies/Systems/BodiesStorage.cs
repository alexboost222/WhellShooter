using System.Collections.Generic;
using Features.Bodies.Enums;
using PhysR;
using Scellecs.Morpeh;

namespace Features.Bodies.Systems
{
    /// <summary>
    /// What Bodies has created in the native world: one record per body or character, keyed by
    /// entity id, so <see cref="PhysicsReleaseSystem"/> can find natives whose entity went away
    /// without ever holding an <see cref="Entity"/> across ticks. Also the shape cache.
    /// </summary>
    /// <remarks>
    /// The native world itself is an engine service (<c>PhysicsWorld</c>, disposed by Bootstrap),
    /// so nothing here frees native memory: on shutdown the whole world goes at once.
    /// </remarks>
    public sealed class BodiesStorage : IInitializer
    {
        private readonly List<Record> _records = new List<Record>();
        private readonly Dictionary<ShapeKey, ShapeId> _shapes = new Dictionary<ShapeKey, ShapeId>();

        public World World { get; set; }

        /// <summary>Live native bodies and characters Bodies created.</summary>
        public int Count
        {
            get { return _records.Count; }
        }

        public void OnAwake()
        {
        }

        public void Dispose()
        {
            _records.Clear();
            _shapes.Clear();
        }

        internal Record this[int index]
        {
            get { return _records[index]; }
        }

        internal void Track(EntityId entity, ulong handle, bool character)
        {
            _records.Add(new Record(entity, handle, character));
        }

        /// <summary>Swap-remove; callers walk the list backwards.</summary>
        internal void RemoveAt(int index)
        {
            var last = _records.Count - 1;
            _records[index] = _records[last];
            _records.RemoveAt(last);
        }

        /// <summary>Shapes are configuration in phys-r and never freed, so identical ones are shared.</summary>
        internal ShapeId GetShape(PhysWorld world, BodyShape shape, FPVector3 size)
        {
            var key = new ShapeKey(shape, size.X.Raw, size.Y.Raw, size.Z.Raw);
            if (_shapes.TryGetValue(key, out var id))
            {
                return id;
            }

            switch (shape)
            {
                case BodyShape.Sphere:
                    id = world.AddSphere(size.X);
                    break;
                case BodyShape.Capsule:
                    id = world.AddCapsule(size.Y, size.X);
                    break;
                default:
                    id = world.AddBox(size);
                    break;
            }

            _shapes.Add(key, id);
            return id;
        }

        internal readonly struct Record
        {
            public readonly EntityId Entity;
            public readonly ulong Handle;
            public readonly bool Character;

            public Record(EntityId entity, ulong handle, bool character)
            {
                Entity = entity;
                Handle = handle;
                Character = character;
            }
        }

        private readonly struct ShapeKey : System.IEquatable<ShapeKey>
        {
            private readonly BodyShape _shape;
            private readonly long _x;
            private readonly long _y;
            private readonly long _z;

            public ShapeKey(BodyShape shape, long x, long y, long z)
            {
                _shape = shape;
                _x = x;
                _y = y;
                _z = z;
            }

            public bool Equals(ShapeKey other)
            {
                return _shape == other._shape && _x == other._x && _y == other._y && _z == other._z;
            }

            public override bool Equals(object obj)
            {
                return obj is ShapeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return System.HashCode.Combine((int)_shape, _x, _y, _z);
            }
        }
    }
}
