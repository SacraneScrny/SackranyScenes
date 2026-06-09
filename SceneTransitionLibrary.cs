using System;
using System.Collections.Generic;

using UnityEngine;

namespace SackranyScenes
{
    /// <summary>
    /// Single, project-wide registry of transition prefabs. It lives in a
    /// <c>Resources</c> folder so it can be loaded at runtime, and there should be
    /// exactly one instance. Create it via <c>Sackrany/Scenes/Create Transition Library</c>.
    ///
    /// Prefabs may live anywhere (including inside this package); only their reference
    /// is stored here. Names registered here drive the generated <c>GameTransitions</c>
    /// constants.
    /// </summary>
    public class SceneTransitionLibrary : ScriptableObject
    {
        // Path passed to Resources.Load (relative to a Resources folder, no extension).
        public const string ResourcePath = "SceneTransitionLibrary";

        [Serializable]
        public class Entry
        {
            public string Name;
            public SceneTransition Prefab;
        }

        [SerializeField] List<Entry> _transitions = new();

        [Tooltip("Transition used by Load/Reload when none is passed explicitly. Empty means no transition.")]
        [SerializeField] string _defaultTransition;

        public IReadOnlyList<Entry> Transitions => _transitions;
        public string DefaultTransition => _defaultTransition;

        public SceneTransition Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            foreach (var entry in _transitions)
            {
                if (entry != null && entry.Name == name)
                    return entry.Prefab;
            }

            return null;
        }
    }
}
