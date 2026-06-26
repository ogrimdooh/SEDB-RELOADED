using System.Collections.Generic;

namespace SEDiscordBridge.Controllers.Rankings
{
    public abstract class BaseRankDefinition
    {

        public abstract string GetId();
        public abstract string GetName();
        public abstract string GetDescription();
        public abstract string GetFooter();
        public abstract string GetIcon();
        public abstract string GetIconForOrder(int order);
        public abstract string GetValueFormated(string icon, string name, float value);
        public abstract List<RankEntry> GetEntries();

    }
}
