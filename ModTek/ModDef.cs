using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ModTek
{
    public class ModDef
    {
        // this path will be set at runtime by ModTek
        [JsonIgnore]
        public string Directory { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        // informational
        public string Description { get; set; }
        public string Author { get; set; }
        public string Website { get; set; }
        public string Contact { get; set; }

        // versioning
        public string Version { get; set; }
        public DateTime? PackagedOn { get; set; }
        public string BattleTechVersionMin { get; set; }
        public string BattleTechVersionMax { get; set; }
        public string BattleTechVersion { get; set; }

        // this will abort loading by ModTek if set to false
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        // load order and requirements
        public HashSet<string> DependsOn { get; set; } = new HashSet<string>();
        public HashSet<string> ConflictsWith { get; set; } = new HashSet<string>();
        public HashSet<string> OptionallyDependsOn { get; set; } = new HashSet<string>();

        [DefaultValue(false)]
        public bool IgnoreLoadFailure { get; set; }

        // adding and running code
        [JsonIgnore]
        public Assembly Assembly { get; set; }
        public string DLL { get; set; }
        public string DLLEntryPoint { get; set; }

        [DefaultValue(false)]
        public bool EnableAssemblyVersionCheck { get; set; } = false;

        // changing implicit loading behavior
        [DefaultValue(true)]
        public bool LoadImplicitManifest { get; set; } = true;

        // custom resources types that will be passed into FinishedLoading method
        public HashSet<string> CustomResourceTypes { get; set; } = new HashSet<string>();

        // manifest, for including any kind of things to add to the game's manifest
        public List<ModEntry> Manifest { get; set; } = new List<ModEntry>();

        public List<ModExtract> Extracts { get; set; } = new List<ModExtract>();

        // remove these entries by ID from the game
        public List<string> RemoveManifestEntries { get; set; } = new List<string>();

        // a settings file to be nice to our users and have a known place for settings
        // these will be different depending on the mod obviously
        public JObject Settings { get; set; } = new JObject();

        /// <summary>
        /// Creates a ModDef from a path to a mod.json
        /// </summary>
        public static ModDef CreateFromPath(string path)
        {
            var modDef = JsonConvert.DeserializeObject<ModDef>(File.ReadAllText(path));
            modDef.Directory = Path.GetDirectoryName(path);
            return modDef;
        }

        /// <summary>
        /// Creates a ModDef from a string
        /// </summary>
        public static ModDef CreateFromString(string data, string path)
        {
            var modDef = JsonConvert.DeserializeObject<ModDef>(data);
            modDef.Directory = Path.GetDirectoryName(path);
            return modDef;
        }

        /// <summary>
        /// Checks if all dependencies are present in param loaded
        /// </summary>
        public bool AreDependenciesResolved(IEnumerable<string> loaded)
        {
            return DependsOn.Count == 0 || DependsOn.Intersect(loaded).Count() == DependsOn.Count;
        }

        /// <summary>
        /// Checks against provided list of mods to see if any of them conflict
        /// </summary>
        public bool HasConflicts(IEnumerable<string> otherMods)
        {
            return ConflictsWith.Intersect(otherMods).Any();
        }

        /// <summary>
        /// Checks to see if this ModDef should load, providing a reason if shouldn't load
        /// </summary>
        public bool ShouldTryLoad(List<string> alreadyTryLoadMods, out string reason)
        {
            if (!Enabled)
            {
                reason = "it is disabled";
                IgnoreLoadFailure = true;
                return false;
            }

            if (alreadyTryLoadMods.Contains(Name))
            {
                reason = $"ModTek already loaded with the same name. Skipping load from {ModTek.GetRelativePath(ModTek.ModsDirectory, Directory)}.";
                return false;
            }

            // check game version vs. specific version or against min/max
            if (!string.IsNullOrEmpty(BattleTechVersion) && !VersionInfo.ProductVersion.StartsWith(BattleTechVersion))
            {
                reason = $"it specifies a game version and this isn't it ({BattleTechVersion} vs. game {VersionInfo.ProductVersion})";
                return false;
            }

            var btgVersion = new Version(VersionInfo.ProductVersion);
            if (!string.IsNullOrEmpty(BattleTechVersionMin) && btgVersion < new Version(BattleTechVersionMin))
            {
                reason = $"it doesn't match the min version set in the mod.json ({BattleTechVersionMin} vs. game {VersionInfo.ProductVersion})";
                return false;
            }

            if (!string.IsNullOrEmpty(BattleTechVersionMax) && btgVersion > new Version(BattleTechVersionMax))
            {
                reason = $"it doesn't match the max version set in the mod.json ({BattleTechVersionMax} vs. game {VersionInfo.ProductVersion})";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
