using System.Collections.Generic;
using Engine.DI;
using Engine.Messages;
using Engine.Physics;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Constants;
using Features.Bots.Providers;
using Features.Bots.Systems;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.Combat.Components;
using Features.Combat.Messages;
using Features.KillCam.Utils;
using Features.PlayerFeature.Components;
using Features.Tracers.Configs;
using Features.Tracers.Utils;
using Features.ViewmodelFeature.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;
using EntityId = Scellecs.Morpeh.EntityId;

namespace Features.KillCam.Systems
{
    /// <summary>
    /// The kill cam (Docs/08 §Kill cam): when the local player dies, replays the last
    /// <see cref="Window"/> seconds through the killer's eyes — every other body as a ghost of the
    /// bot model, the shots as tracers — then holds the last frame until the respawn.
    /// </summary>
    /// <remarks>
    /// A view, listed after the viewmodel so it writes the camera last. It reads
    /// <see cref="KillCamStorage"/> (recorded by <see cref="KillCamRecordSystem"/>), writes the
    /// camera, the weapon socket's active flag, live bots' renderers (hidden during the replay) and
    /// its own ghost objects. Records the shots itself: they arrive as <see cref="ShotTraced"/> in
    /// Update.
    /// </remarks>
    public sealed class KillCamSystem : ISystem
    {
        /// <summary>Seconds replayed before the death.</summary>
        public const float Window = 3f;

        /// <summary>Seconds the replay runs on past the death.</summary>
        public const float Tail = 0.5f;

        private const float MoveDamping = 0.1f;

        [Inject] private Stash<Dead> _dead;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<ViewmodelLink> _viewmodelLink;
        [Inject] private Stash<BotViewLink> _botViewLink;

        [Inject] private SubscriberQueue<ShotTraced> _shotTraced;
        [Inject] private KillCamStorage _storage;
        [Inject] private PhysicsWorld _physics;
        [Inject] private CharacterConfig _characterConfig;
        [Inject] private TracerConfig _tracerConfig;
        [Inject] private BotsConfig _botsConfig;
        [Inject] private BotViewFactory _botViews;

        public World World { get; set; }

        private readonly Dictionary<EntityId, Ghost> _ghosts = new Dictionary<EntityId, Ghost>();
        private readonly List<Renderer> _hidden = new List<Renderer>();

        private Filter _filter;
        private Filter _bots;
        private TracerPool _tracers;

        private bool _active;
        private bool _replaying;
        private Transform _camera;
        private Transform _socket;
        private Vector3 _localPosition;
        private Quaternion _localRotation;
        private EntityId _killer;
        private int _startTick;
        private int _endTick;
        private float _time;
        private float _lastTick;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Player>()
                .With<CharacterMotion>()
                .With<ViewmodelLink>()
                .Build();

            _bots = World.Filter
                .With<BotViewLink>()
                .Build();

            _tracers = new TracerPool(_tracerConfig, "KillCamTracers");
        }

        public void Dispose()
        {
            ClearGhosts();
            _tracers?.Dispose();
        }

        public void OnUpdate(float deltaTime)
        {
            RecordShots();

            var player = _filter.FirstOrDefault();
            if (player == null || !_dead.Has(player))
            {
                Release();
                return;
            }

            var rig = _viewmodelLink.Get(player).Rig;
            if (rig == null || rig.Camera == null)
            {
                return;
            }

            if (!_active)
            {
                Begin(player, rig.Camera.transform, rig.Socket);
            }

            if (_replaying)
            {
                Replay(deltaTime);
            }
            else
            {
                var feet = _characterMotion.Get(player).Position;
                var eye = KillCamMath.Overhead(feet);
                _camera.SetPositionAndRotation(eye, KillCamMath.LookAt(eye, feet));
            }

            _tracers.Update(deltaTime);
        }

        private void RecordShots()
        {
            if (!_storage.Recording)
            {
                return;
            }

            foreach (var shot in _shotTraced)
            {
                _storage.AddShot(new ShotSample
                {
                    Tick = _storage.LatestTick,
                    Shooter = shot.Shooter,
                    Origin = shot.Origin,
                    End = shot.End,
                });
            }
        }

        private void Begin(Entity player, Transform camera, Transform socket)
        {
            _active = true;
            _camera = camera;
            _socket = socket;
            _localPosition = camera.localPosition;
            _localRotation = camera.localRotation;
            if (_socket != null)
            {
                _socket.gameObject.SetActive(false);
            }

            _killer = _dead.Get(player).Killer;
            _endTick = _storage.LatestTick;
            _startTick = math.max(_storage.OldestTick, _endTick - (int)math.round(Window * _physics.TickRate));
            _replaying = _killer != player.ID && _storage.TryGetFrame(_endTick, out var last) &&
                         KillCamMath.TryFind(last, _killer, out _);

            if (!_replaying)
            {
                return;
            }

            _storage.Recording = false;
            _storage.Playing = true;
            _time = 0f;
            _lastTick = _startTick - 1;
            HideLiveBots();
        }

        private void Replay(float deltaTime)
        {
            _time += deltaTime;
            var tick = math.min(_startTick + _time * _physics.TickRate, _endTick + Tail * _physics.TickRate);
            var at = math.min(tick, _endTick);
            var index = (int)math.floor(at);
            var t = at - index;

            _storage.TryGetFrame(index, out var from);
            if (!_storage.TryGetFrame(math.min(index + 1, _endTick), out var to))
            {
                to = from;
            }

            if (from == null)
            {
                return;
            }

            if (KillCamMath.TrySample(from, to, _killer, t, out var eyes))
            {
                var eye = eyes.Position + new float3(0f, _characterConfig.EyeHeight, 0f);
                _camera.SetPositionAndRotation(eye, KillCamMath.EyeRotation(eyes.Yaw, eyes.Pitch));
            }

            PlaceGhosts(from, to, t, deltaTime);
            ReplayShots(tick);

            if (tick >= _endTick + Tail * _physics.TickRate)
            {
                // Hold the last frame until the respawn; the death screen takes over.
                _storage.Playing = false;
            }

            _lastTick = tick;
        }

        private void PlaceGhosts(List<BodySample> from, List<BodySample> to, float t, float deltaTime)
        {
            for (var i = 0; i < from.Count; i++)
            {
                var id = from[i].Id;
                if (id == _killer || !KillCamMath.TrySample(from, to, id, t, out var body))
                {
                    continue;
                }

                if (!_ghosts.TryGetValue(id, out var ghost) && !TryMakeGhost(id, body, out ghost))
                {
                    continue;
                }

                if (ghost.Model != null)
                {
                    ghost.Model.SetPositionAndRotation(body.Position, Quaternion.Euler(0f, math.degrees(body.Yaw), 0f));
                }

                var animator = ghost.Animator;
                if (animator != null)
                {
                    var forward = new float2(math.sin(body.Yaw), math.cos(body.Yaw));
                    var right = new float2(math.cos(body.Yaw), -math.sin(body.Yaw));
                    animator.SetFloat(BotAnimatorParams.MoveX, math.dot(body.Velocity.xz, right), MoveDamping, deltaTime);
                    animator.SetFloat(BotAnimatorParams.MoveZ, math.dot(body.Velocity.xz, forward), MoveDamping, deltaTime);
                    animator.SetBool(BotAnimatorParams.Grounded, body.Grounded);
                    if (body.Dead && !ghost.WasDead)
                    {
                        var variant = (int)(math.hash(new int2(id.GetHashCode(), 7)) % BotAnimatorParams.DeathVariantCount);
                        animator.SetInteger(BotAnimatorParams.DeathVariant, variant);
                    }

                    animator.SetBool(BotAnimatorParams.Dead, body.Dead);
                }

                ghost.WasDead = body.Dead;
                _ghosts[id] = ghost;
            }
        }

        private void ReplayShots(float tick)
        {
            var shots = _storage.Shots;
            for (var i = 0; i < shots.Count; i++)
            {
                var shot = shots[i];
                if (shot.Tick > _lastTick && shot.Tick <= tick && shot.Tick <= _endTick)
                {
                    _tracers.Spawn(shot.Origin, shot.End, shot.Shooter == _killer);
                }
            }
        }

        private bool TryMakeGhost(EntityId id, BodySample body, out Ghost ghost)
        {
            ghost = default;
            var rotation = Quaternion.Euler(0f, math.degrees(body.Yaw), 0f);
            if (!_botViews.TryBuild(_botsConfig.ViewPrefab, body.Position, rotation, out var root, out var view))
            {
                return false;
            }

            root.name = "KillCamGhost";
            ghost = new Ghost
            {
                Root = root.gameObject,
                Model = view.Model,
                Animator = view.Animator,
            };
            _ghosts.Add(id, ghost);
            return true;
        }

        private void HideLiveBots()
        {
            _hidden.Clear();
            foreach (var entity in _bots)
            {
                var view = _botViewLink.Get(entity).View;
                if (view == null)
                {
                    continue;
                }

                foreach (var renderer in view.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.enabled)
                    {
                        renderer.enabled = false;
                        _hidden.Add(renderer);
                    }
                }
            }
        }

        private void Release()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _replaying = false;
            _storage.Recording = true;
            _storage.Playing = false;

            if (_camera != null)
            {
                _camera.localPosition = _localPosition;
                _camera.localRotation = _localRotation;
            }

            if (_socket != null)
            {
                _socket.gameObject.SetActive(true);
            }

            for (var i = 0; i < _hidden.Count; i++)
            {
                if (_hidden[i] != null)
                {
                    _hidden[i].enabled = true;
                }
            }

            _hidden.Clear();
            ClearGhosts();
            _tracers.Clear();
            _camera = null;
            _socket = null;
        }

        private void ClearGhosts()
        {
            foreach (var ghost in _ghosts.Values)
            {
                if (ghost.Root != null)
                {
                    Object.Destroy(ghost.Root);
                }
            }

            _ghosts.Clear();
        }

        private struct Ghost
        {
            public GameObject Root;
            public Transform Model;
            public Animator Animator;
            public bool WasDead;
        }
    }
}
