using Engine.DI;
using Features.Map.Systems;
using Features.Navigation.Utils;
using Scellecs.Morpeh;

namespace Features.Navigation.Systems
{
    /// <summary>
    /// Keeps <see cref="NavStorage"/> in step with <see cref="MapStorage"/>: a new map version means
    /// a new grid, on the same tick the map was generated.
    /// </summary>
    public sealed class NavGridBuildSystem : ISystem
    {
        [Inject] private MapStorage _mapStorage;
        [Inject] private NavStorage _navStorage;

        public World World { get; set; }

        private int _builtVersion;

        public void OnAwake()
        {
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            if (_mapStorage.Version == _builtVersion)
            {
                return;
            }

            _builtVersion = _mapStorage.Version;
            var layout = _mapStorage.Layout;
            _navStorage.Set(layout != null ? new NavGrid(layout) : null);
        }
    }
}
