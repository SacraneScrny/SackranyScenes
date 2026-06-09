using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;

namespace SackranyScenes
{
    /// <summary>
    /// Base class for a scene transition. Put this (or a subclass) on the root of a
    /// prefab, register that prefab in the single <see cref="SceneTransitionLibrary"/>,
    /// and implement the two stages with your own animations.
    ///
    /// Lifecycle for one transition:
    ///   1. The prefab is instantiated and kept across the scene swap.
    ///   2. <see cref="PlayOut"/> is awaited — hide / cover the current scene.
    ///   3. The scenes are swapped (next loaded, current unloaded).
    ///   4. <see cref="PlayIn"/> is awaited — reveal the next scene.
    ///   5. The instance is destroyed.
    /// </summary>
    public abstract class SceneTransition : MonoBehaviour
    {
        /// <summary>Stage one: cover / hide the current scene. Awaited before the swap.</summary>
        public abstract UniTask PlayOut(CancellationToken cancellationToken = default);

        /// <summary>Stage two: reveal the next scene. Awaited after the swap.</summary>
        public abstract UniTask PlayIn(CancellationToken cancellationToken = default);
    }
}
