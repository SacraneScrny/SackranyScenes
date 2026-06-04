using System;

using Sackrany.ConfigSystem.SackranyConfig;

namespace Sackrany.Scenes.SackranyScenes
{
    [Serializable]
    public class SceneConfig : IConfig
    {
        public string DefaultScene { get; set; } = "SampleScene";
    }
}