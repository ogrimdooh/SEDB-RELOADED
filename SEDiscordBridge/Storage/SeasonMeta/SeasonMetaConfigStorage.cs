using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using SEDiscordBridge.Patches;
using SEDiscordBridge.Storage.Base;
using SEDiscordBridge.Storage.Registry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Library.Utils;
using VRage.ObjectBuilders;

namespace SEDiscordBridge.Storage.SeasonMeta
{

    public class SeasonMetaConfigStorage : BaseStorage
    {

        private const int CURRENT_VERSION = 1;
        private const string FILE_NAME = "SEDB.SeasonMeta.Config.Storage.xml";
        private const string JSON_FILE_NAME = "SEDB.SeasonMeta.Config.Storage.json";
        private const bool USE_JSON = true;

        private static SeasonMetaConfigStorage _instance;
        public static SeasonMetaConfigStorage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        private static bool Validate(SeasonMetaConfigStorage settings)
        {
            var res = true;
            return res;
        }

        private static SeasonMetaConfigStorage Upgrade(SeasonMetaConfigStorage settings)
        {

            return settings;
        }

        public static SeasonMetaConfigStorage Load()
        {
            _instance = Load(USE_JSON, FILE_NAME, JSON_FILE_NAME, CURRENT_VERSION, Validate, () => { return new SeasonMetaConfigStorage(); }, Upgrade);
            return _instance;
        }

        public static void Save()
        {
            try
            {
                Save(Instance, USE_JSON, FILE_NAME, JSON_FILE_NAME);
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(SeasonMetaConfigStorage), e);
            }
        }

        [XmlElement]
        public bool Enabled { get; set; } = true;

        [XmlElement]
        public string ActiveConfiguration { get; set; }

        [XmlElement]
        public bool FirstLoad { get; set; } = true;

        [XmlElement]
        public SeasonMetaChatMessagesIds ChatMessagesIds { get; set; } = new SeasonMetaChatMessagesIds();

        [XmlArray("Categories"), XmlArrayItem("Category", typeof(SeasonMetaCategory))]
        public List<SeasonMetaCategory> Categories { get; set; } = new List<SeasonMetaCategory>();

        [XmlArray("Configurations"), XmlArrayItem("Configuration", typeof(SeasonMetaConfiguration))]
        public List<SeasonMetaConfiguration> Configurations { get; set; } = new List<SeasonMetaConfiguration>();

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
            return id.TypeId == typeof(MyObjectBuilder_ConsumableItem) && !ignoredSubTypes.Contains(id.SubtypeName) ||
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
                // Set active configuration and result
                ActiveConfiguration = configuration.Id;
                // Load dummy result
                SeasonMetaResultStorage.Instance.LoadInitialData(configuration);
            }
        }

    }

}
