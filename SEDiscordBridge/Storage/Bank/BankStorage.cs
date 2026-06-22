using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.Bank
{
    public class BankStorage
    {

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
