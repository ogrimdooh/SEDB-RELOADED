using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRageMath;

namespace SEDiscordBridge.Patches
{

    public class SeasonMetaStorage
    {

        [XmlElement]
        public bool Enabled { get; set; } = true;

        [XmlElement]
        public string ActiveConfiguration { get; set; }

        [XmlElement]
        public string ActiveResult { get; set; }

        [XmlElement]
        public bool FirstLoad { get; set; } = true;

        [XmlElement]
        public SeasonMetaChatMessagesIds ChatMessagesIds { get; set; } = new SeasonMetaChatMessagesIds();

        [XmlArray("Categories"), XmlArrayItem("Category", typeof(SeasonMetaCategory))]
        public List<SeasonMetaCategory> Categories { get; set; } = new List<SeasonMetaCategory>();

        [XmlArray("Configurations"), XmlArrayItem("Configuration", typeof(SeasonMetaConfiguration))]
        public List<SeasonMetaConfiguration> Configurations { get; set; } = new List<SeasonMetaConfiguration>();

        [XmlArray("Results"), XmlArrayItem("Result", typeof(SeasonMetaResult))]
        public List<SeasonMetaResult> Results { get; set; } = new List<SeasonMetaResult>();

        public SeasonMetaConfiguration GetActiveConfiguration()
        {
            return Configurations.FirstOrDefault(x => x.Id == ActiveConfiguration);
        }

        public List<StorageDefinitionId> GetValidItensIds()
        {
            var lista = new List<StorageDefinitionId>();
            var activeConfiguration = GetActiveConfiguration();
            if (activeConfiguration != null)
            {
                foreach (var entry in activeConfiguration.Entries)
                {
                    var category = GetCategoryById(entry.CategoryId);
                    if (category != null)
                    {
                        lista.AddRange(category.Items.Select(x => x.Id));
                    }
                }
            }
            return lista;
        }

        public string GetItemCategoryById(MyDefinitionId itemId)
        {
            var activeConfiguration = GetActiveConfiguration();
            if (activeConfiguration != null)
            {
                foreach (var entry in activeConfiguration.Entries)
                {
                    var category = GetCategoryById(entry.CategoryId);
                    if (category != null)
                    {
                        if (category.Items.Any(x=>x.Id.ToMyDefinitionId() == itemId))
                        {
                            return category.Id;
                        }
                    }
                }
            }
            return null;
        }

        public SeasonMetaResult GetActiveResult()
        {
            return Results.FirstOrDefault(x => x.Id == ActiveResult);
        }

        public Dictionary<string, Vector2> GetActiveResultProgress()
        {
            var result = GetActiveResult();
            if (result == null) return new Dictionary<string, Vector2>();
            var configuration = GetActiveConfiguration();
            if (configuration == null) return new Dictionary<string, Vector2>();
            var progress = new Dictionary<string, Vector2>();
            foreach (var entry in result.Entries)
            {
                var configEntry = configuration.Entries.FirstOrDefault(x => x.CategoryId == entry.CategoryId);
                if (configEntry != null && configEntry.Amount > 0)
                {
                    progress[entry.CategoryId] = new Vector2((float)entry.Amount / configEntry.Amount, configEntry.Weight);
                }
                else
                {
                    progress[entry.CategoryId] = new Vector2(0f, 0f);
                }
            }
            return progress;
        }

        public float GetCurrentProgress()
        {
            var progress = GetActiveResultProgress();
            if (progress.Count == 0) return 0f;
            var allValues = new List<float>();
            foreach (var item in progress.Keys)
            {
                for (int i = 0; i < progress[item].Y; i++)
                {
                    allValues.Add(progress[item].X);
                }
            }
            return allValues.Average();
        }

        public TimeSpan GetTimeToNextCheckpoint()
        {
            var configuration = GetActiveConfiguration();
            var result = GetActiveResult();
            var lastCheckpoint = (result.LastCheckpoint ?? result.SeasonStart).Value;
            var nextCheckpoint = lastCheckpoint.AddHours(configuration.HoursBetweenCheckpoints);
            return nextCheckpoint - DateTime.Now;
        }

        public TimeSpan GetTimeToNextSeason()
        {
            var configuration = GetActiveConfiguration();
            var result = GetActiveResult();
            var seasonStart = result.SeasonStart.Value;
            var nextSeason = seasonStart.AddHours(configuration.HoursBetweenCheckpoints * configuration.TotalCheckpoints);
            return nextSeason - DateTime.Now;
        }

        public SeasonMetaCategory GetCategoryById(string id)
        {
            return Categories.FirstOrDefault(x => x.Id == id);
        }

        private MyObjectBuilderType[] GetPhysicalItemFilter()
        {
            return new MyObjectBuilderType[] { typeof(MyObjectBuilder_TreeObject), typeof(MyObjectBuilder_Package) };
        }

        private MyDefinitionId[] GetFuelIds()
        {
            return new MyDefinitionId[] {
                new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Uranium"),
            };
        }

        private bool IsFuel(MyDefinitionId id)
        {
            return GetFuelIds().Contains(id);
        }

        private bool IsRawResource(MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_Ore);
        }

        private bool IsRefinedResource(MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_Ingot);
        }

        private bool IsAssembledComponent(MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_Component);
        }

        private bool IsAmmo(MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_AmmoMagazine);
        }

        private bool IsWeaponOrTool(MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_PhysicalGunObject);
        }

        private bool IsSurvivalResource(MyDefinitionId id)
        {
            var ignoredSubTypes = new string[] { "RadiationKit", "Powerkit", "Medkit" };
            return (id.TypeId == typeof(MyObjectBuilder_ConsumableItem) && !ignoredSubTypes.Contains(id.SubtypeName)) ||
                id.TypeId == typeof(MyObjectBuilder_SeedItem);
        }

        private SeasonMetaEntryType GetEntryType(MyDefinitionId id)
        {
            if (IsFuel(id))
                return SeasonMetaEntryType.Fuel;
            if (IsRawResource(id))
                return SeasonMetaEntryType.RawResource;
            if (IsRefinedResource(id))
                return SeasonMetaEntryType.RefinedResource;
            if (IsAssembledComponent(id))
                return SeasonMetaEntryType.AssembledComponent;
            if (IsAmmo(id))
                return SeasonMetaEntryType.Ammo;
            if (IsWeaponOrTool(id))
                return SeasonMetaEntryType.WeaponAndTool;
            if (IsSurvivalResource(id))
                return SeasonMetaEntryType.SurvivalResource;
            return SeasonMetaEntryType.Others;
        }

        public void LoadInitialData()
        {
            if (FirstLoad)
            {
                FirstLoad = false;
                // Load Categories
                var ignoredTypes = GetPhysicalItemFilter();
                var list = MyDefinitionManager.Static.GetPhysicalItemDefinitions().Where(x => !ignoredTypes.Contains(x.Id.TypeId)).OrderBy(x => x.DisplayNameText).ToArray();
                var listByType = list.GroupBy(x => GetEntryType(x.Id)).ToDictionary(x => x.Key, x => x.ToList());
                foreach (var gType in listByType.Keys)
                {
                    var group = listByType[gType];
                    var category = new SeasonMetaCategory()
                    {
                        Id = $"SEASON_META_CATEGORY_{gType.ToString().ToUpper()}",
                        Name = gType.ToString(),
                        Type = gType,
                        Items = group.Select(x => new SeasonMetaCategoryValidItem() { Id = new StorageDefinitionId(x.Id), Weight = 1 }).ToList()
                    };
                    Categories.Add(category);
                }
                // Load a dummy configuration and result for testing
                var configuration = new SeasonMetaConfiguration()
                {
                    Id = "SEASON_META_CONFIG_TEST",
                    Name = "Test",
                    Entries = Categories.Select(x => new SeasonMetaEntry()
                    {
                        CategoryId = x.Id,
                        Amount = MyRandom.Instance.NextLong()
                    }).ToList()
                };
                Configurations.Add(configuration);
                // Load a dummy result for testing
                var result = new SeasonMetaResult()
                {
                    Id = "SEASON_META_RESULT_TEST",
                    TargetConfiguration = configuration.Id,
                    Entries = configuration.Entries.Select(x => new SeasonMetaEntry()
                    {
                        CategoryId = x.CategoryId,
                        Amount = 0
                    }).ToList()
                };
                Results.Add(result);
                // Set active configuration and result
                ActiveConfiguration = configuration.Id;
                ActiveResult = result.Id;
            }
        }

    }

}
