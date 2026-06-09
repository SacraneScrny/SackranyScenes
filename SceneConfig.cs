using System;

using SackranyConfig;

namespace Sackrany.Scenes.SackranyScenes
{
    [Serializable]
    public class SceneConfig : IConfig
    {
        public string DefaultScene { get; set; } = "SampleScene";
    }
}