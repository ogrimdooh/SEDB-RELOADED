using System.Collections.Generic;
using System.Xml.Serialization;

namespace SEDiscordBridge.Patches
{
    public class BankAccount
    {

        [XmlElement]
        public ulong SteamId { get; set; }

        [XmlElement]
        public ulong UserId { get; set; }

        [XmlElement]
        public ulong Balance { get; set; }

        [XmlArray("Transactions"), XmlArrayItem("Transaction", typeof(BankTransaction))]
        public List<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();

        public bool DoFee(ulong value, string name, string description)
        {
            if (Balance >= value)
            {
                Transactions.Add(new BankTransaction()
                {
                    OperationDate = System.DateTime.Now,
                    OperationType = BankTransactionType.Fee,
                    Value = value,
                    Name = name,
                    Description = description
                });
                Balance -= value;
                return true;
            }
            return false;
        }

        public bool DoDeposit(ulong value, ulong referenceValue)
        {
            Transactions.Add(new BankTransaction() { 
                OperationDate = System.DateTime.Now,
                OperationType = BankTransactionType.Deposit,
                Value = value,
                ReferenceValue = referenceValue
            });
            Balance += value;
            return true;
        }

        public bool DoWithdraw(ulong value, ulong referenceValue)
        {
            if (Balance >= value)
            {
                Transactions.Add(new BankTransaction()
                {
                    OperationDate = System.DateTime.Now,
                    OperationType = BankTransactionType.Withdraw,
                    Value = value,
                    ReferenceValue = referenceValue
                });
                Balance -= value;
                return true;
            }
            return false;
        }

    }

}
