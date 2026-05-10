using System.Collections.Generic;
using TrainAI.Configs;

namespace TrainAI.Systems.Quest
{
    public class QuestRunnerRegistry
    {
        private readonly Dictionary<QuestType, IQuestRunner> _map = new Dictionary<QuestType, IQuestRunner>();

        public void Register(QuestType type, IQuestRunner runner)
        {
            _map[type] = runner;
        }

        public IQuestRunner Get(QuestType type)
        {
            if (_map.TryGetValue(type, out var r)) return r;
            return null;
        }

        public bool Has(QuestType type) => _map.ContainsKey(type);
    }
}
