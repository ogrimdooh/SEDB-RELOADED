using Newtonsoft.Json;
using SEDiscordBridge.Storage;
using System;
using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.Bank
{
    public class BankTransaction
    {

        [XmlElement]
        public string OperationDateValue { get; set; }

        [XmlElement]
        public BankTransactionType OperationType { get; set; }

        [XmlElement]
        public ulong Value { get; set; }

        [XmlElement]
        public ulong ReferenceValue { get; set; }

        [XmlElement]
        public string Name { get; set; }

        [XmlElement]
        public string Description { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public DateTime? OperationDate
        {
            get
            {
                if (DateTime.TryParseExact(OperationDateValue, SEDBStorage.DATE_FORMAT, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
                return null;
            }
            set
            {
                OperationDateValue = value?.ToString(SEDBStorage.DATE_FORMAT);
            }
        }

    }

}
