using System;
using System.Collections.Generic;
using Engine.Configs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Engine.Runtime;
using Scellecs.Morpeh;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode.Features
{
    /// <summary>
    /// A headless world wired the way Bootstrap wires it, for driving a handful of systems by hand:
    /// add the configs and systems under test, then <see cref="Tick"/>.
    /// </summary>
    public sealed class EcsTestWorld : IDisposable
    {
        private readonly List<Object> _owned = new List<Object>();
        private readonly SystemsGroup _group;
        private bool _initialized;

        public EcsTestWorld(params Config[] configs)
        {
            World = World.Create();
            Time = new TimeService();
            Container = new Container();

            var catalog = new ConfigCatalog(configs);
            var bus = new MessageBus();
            Container.Register(catalog);
            Container.Register(World);
            Container.Register(bus);
            Container.Register(Time);
            Container.AddResolver(new ConfigResolver(catalog));
            Container.AddResolver(new StashResolver(World));
            Container.AddResolver(new MessagePipeResolver(bus));

            _group = World.CreateSystemsGroup();
            _owned.AddRange(configs);
        }

        public World World { get; }
        public Container Container { get; }
        public TimeService Time { get; }

        /// <summary>Builds and adds a system. Systems run in the order they are added.</summary>
        public T Add<T>() where T : class, ISystem
        {
            var system = Container.New<T>();
            _group.AddSystem(system);
            return system;
        }

        /// <summary>Registers an initializer (storage/service) under its own type.</summary>
        public T AddInitializer<T>(T initializer) where T : class, IInitializer
        {
            Container.Register(initializer);
            _group.AddInitializer(initializer);
            return initializer;
        }

        public void Tick(float deltaTime)
        {
            if (!_initialized)
            {
                _group.Initialize();
                _initialized = true;
            }

            _group.Update(deltaTime);
        }

        public void Tick(float deltaTime, int count)
        {
            for (var i = 0; i < count; i++)
            {
                Tick(deltaTime);
            }
        }

        public ref T Get<T>(Entity entity) where T : struct, IComponent
        {
            return ref World.GetStash<T>().Get(entity);
        }

        public bool Has<T>(Entity entity) where T : struct, IComponent
        {
            return World.GetStash<T>().Has(entity);
        }

        public void Set<T>(Entity entity, T value) where T : struct, IComponent
        {
            World.GetStash<T>().Set(entity, value);
        }

        /// <summary>A GameObject destroyed with the world.</summary>
        public GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            _owned.Add(gameObject);
            return gameObject;
        }

        public void Dispose()
        {
            World.Dispose();

            for (var i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                {
                    Object.DestroyImmediate(_owned[i]);
                }
            }
        }
    }
}
