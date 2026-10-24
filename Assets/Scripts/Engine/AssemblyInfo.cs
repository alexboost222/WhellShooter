using System.Runtime.CompilerServices;

// Engine keeps its internals internal; the tests get to see them rather than those types being
// made public just to be reachable.
[assembly: InternalsVisibleTo("Engine.Tests")]
[assembly: InternalsVisibleTo("Game.Tests")]
