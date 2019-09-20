using System;
using System.ComponentModel;
using BattleTech;
using Newtonsoft.Json;

namespace ModTek
{
    public class ModExtract
    {
        [JsonConstructor]
        public ModExtract(string path, string target)
        {
            Path = path;
            Target = target;
        }

        [JsonProperty(Required = Required.Always)]
        public string Path { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Target { get; set; }
    }
}
