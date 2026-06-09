using System;

using SackranyConfig;

namespace SackranyScenes
{
    [Serializable]
    public class SceneConfig : IConfig
    {
        public string DefaultScene { get; set; } = "SampleScene";
    }
}