using Sandbox.ModAPI;
using System;
using VRage.Utils;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using static System.BitStreamExtensions;
using System.Windows.Documents;

namespace SEDiscordBridge.Patches
{
    public abstract class BaseStorage
    {

        protected delegate bool BaseSettings_Validade<T>(T settings) where T : BaseStorage;
        protected delegate T BaseSettings_Upgrade<T>(T settings) where T : BaseStorage;
        protected delegate T BaseSettings_Create<T>() where T : BaseStorage;

        [XmlElement]
        public int Version { get; set; }

        [XmlIgnore]
        public bool Modified { get; set; }

        [XmlIgnore]
        public bool CheckModified { get; set; } = false;

        protected static T Load<T>(string fileName, int currentVersion, BaseSettings_Validade<T> validade, BaseSettings_Create<T> create, BaseSettings_Upgrade<T> upgrade, bool json = false, bool createIfNotFound = true) where T : BaseStorage
        {
            T settings = null;
            try
            {
                string storageFile = Path.Combine(VRage.FileSystem.MyFileSystem.UserDataPath, fileName);
                if (File.Exists(storageFile))
                {
                    try
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        using (FileStream stream = File.OpenRead(storageFile))
                        {
                            settings = serializer.Deserialize(stream) as T;
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.Instance.LogError(typeof(BaseStorage), e);
                    }
                }
                if (settings != null)
                {
                    var adjusted = false;
                    if (settings.Version < currentVersion)
                    {
                        Logging.Instance.LogInfo(typeof(BaseStorage), $"{fileName}: Storage have old version: {settings.Version} update to {currentVersion}");
                        settings = upgrade(settings);
                        adjusted = true;
                        settings.Version = currentVersion;
                    }
                    adjusted = adjusted || !validade(settings);
                    if (adjusted) 
                        Save<T>(settings, fileName);
                }
                else if (createIfNotFound)
                {
                    settings = create();
                    settings.Version = currentVersion;
                    validade(settings);
                    Save<T>(settings, fileName);
                }
                settings?.OnAfterLoad();
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(BaseStorage), e);
            }
            return settings;
        }

        protected virtual void OnAfterLoad()
        {

        }

        protected static void Save<T>(T settings, string fileName) where T : BaseStorage
        {
            if (settings.CheckModified && !settings.Modified)
                return;

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            string configFile = Path.Combine(VRage.FileSystem.MyFileSystem.UserDataPath, fileName);
            using (StreamWriter stream = new StreamWriter(configFile, false, Encoding.UTF8))
            {
                serializer.Serialize(stream, settings);
            }

            if (settings.CheckModified)
                settings.Modified = false;
        }

    }

}
