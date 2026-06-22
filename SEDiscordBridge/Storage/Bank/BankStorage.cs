using SEDiscordBridge.Storage.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.Bank
{
    public class BankStorage : BaseStorage
    {

        private const int CURRENT_VERSION = 1;
        private const string FILE_NAME = "SEDB.Bank.Storage.xml";
        private const string JSON_FILE_NAME = "SEDB.Bank.Storage.json";
        private const bool USE_JSON = true;

        private static BankStorage _instance;
        public static BankStorage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        private static bool Validate(BankStorage settings)
        {
            var res = true;
            return res;
        }

        private static BankStorage Upgrade(BankStorage settings)
        {

            return settings;
        }

        public static BankStorage Load()
        {
            _instance = Load(USE_JSON, FILE_NAME, JSON_FILE_NAME, CURRENT_VERSION, Validate, () => { return new BankStorage(); }, Upgrade);
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
                Logging.Instance.LogError(typeof(BankStorage), e);
            }
        }

        [XmlElement]
        public bool Enabled { get; set; } = true;

        [XmlElement]
        public ulong StartMsgId { get; set; }

        [XmlElement]
        public ulong MinOperationValue { get; set; } = 1000;

        [XmlElement]
        public float WithdrawFactor { get; set; } = 0.5f;

        [XmlElement]
        public float DepositFactor { get; set; } = 0.5f;

        [XmlArray("Accounts"), XmlArrayItem("Account", typeof(BankTransaction))]
        public List<BankAccount> Accounts { get; set; } = new List<BankAccount>();

        public HashSet<ulong> GetAllMessagesIds()
        {
            return new HashSet<ulong>() { StartMsgId };
        }

        public bool UserHasAccount(ulong userId)
        {
            return Accounts.Any(x => x.UserId == userId);
        }

        public BankAccount GetBankAccount(ulong userId)
        {
            return Accounts.FirstOrDefault(x => x.UserId == userId);
        }

        public BankAccount CreateBankAccount(ulong userId, ulong steamId)
        {
            BankAccount bankAccount = new BankAccount()
            {
                UserId = userId,
                SteamId = steamId,
                Balance = 0,
            };
            Accounts.Add(bankAccount);
            return bankAccount;
        }

    }

}
