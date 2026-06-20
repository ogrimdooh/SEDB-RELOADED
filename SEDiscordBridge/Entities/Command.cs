using System.Xml.Serialization;

namespace SEDiscordBridge.Entities
{

    public class Command
    {

        [XmlElement]
        public ulong Sender { get; set; }

        [XmlElement]
        public string[] Content { get; set; }

        [XmlElement]
        public byte[] Data { get; set; }

        public Command() { }

        public Command(ulong sender, params string[] content)
        {
            this.Sender = sender;
            this.Content = content;
        }

    }

}