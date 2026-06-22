using SEDiscordBridge.Storage.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using VRageMath;

namespace SEDiscordBridge.Storage.SeasonMeta
{
    public class SeasonMetaResultStorage : BaseStorage
    {

        private const int CURRENT_VERSION = 1;
        private const string FILE_NAME = "SEDB.SeasonMeta.Result.Storage.xml";
        private const string JSON_FILE_NAME = "SEDB.SeasonMeta.Result.Storage.json";
        private const bool USE_JSON = true;

        private static SeasonMetaResultStorage _instance;
        public static SeasonMetaResultStorage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        private static bool Validate(SeasonMetaResultStorage settings)
        {
            var res = true;
            return res;
        }

        private static SeasonMetaResultStorage Upgrade(SeasonMetaResultStorage settings)
        {

            return settings;
        }

        public static SeasonMetaResultStorage Load()
        {
            _instance = Load(USE_JSON, FILE_NAME, JSON_FILE_NAME, CURRENT_VERSION, Validate, () => { return new SeasonMetaResultStorage(); }, Upgrade);
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
                Logging.Instance.LogError(typeof(SeasonMetaResultStorage), e);
            }
        }

        [XmlElement]
        public string ActiveResult { get; set; }

        [XmlArray("Results"), XmlArrayItem("Result", typeof(SeasonMetaResult))]
        public List<SeasonMetaResult> Results { get; set; } = new List<SeasonMetaResult>();

        public SeasonMetaResult GetActiveResult()
        {
            return Results.FirstOrDefault(x => x.Id == ActiveResult);
        }

        public Dictionary<string, Vector2> GetActiveResultProgress()
        {
            var result = GetActiveResult();
            if (result == null) return new Dictionary<string, Vector2>();
            var configuration = SeasonMetaConfigStorage.Instance.GetActiveConfiguration();
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
            var configuration = SeasonMetaConfigStorage.Instance.GetActiveConfiguration();
            var result = GetActiveResult();
            var lastCheckpoint = (result.LastCheckpoint ?? result.SeasonStart).Value;
            var nextCheckpoint = lastCheckpoint.AddHours(configuration.HoursBetweenCheckpoints);
            return nextCheckpoint - DateTime.Now;
        }

        public TimeSpan GetTimeToNextSeason()
        {
            var configuration = SeasonMetaConfigStorage.Instance.GetActiveConfiguration();
            var result = GetActiveResult();
            var seasonStart = result.SeasonStart.Value;
            var nextSeason = seasonStart.AddHours(configuration.HoursBetweenCheckpoints * configuration.TotalCheckpoints);
            return nextSeason - DateTime.Now;
        }

        public void LoadInitialData(SeasonMetaConfiguration configuration)
        {
            // Load a dummy result for testing
            var result = new SeasonMetaResult()
            {
                Id = "SEASON_META_RESULT_TEST",
                TargetConfiguration = configuration.Id,
                Entries = configuration.Entries.Select(x => new SeasonSimpleMetaEntry()
                {
                    CategoryId = x.CategoryId,
                    Amount = 0
                }).ToList()
            };
            Results.Add(result);
            ActiveResult = result.Id;
        }

    }

}
