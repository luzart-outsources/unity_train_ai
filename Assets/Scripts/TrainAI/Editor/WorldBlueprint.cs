using System;
using System.Collections.Generic;

namespace TrainAI.Editor
{
    [Serializable] public class WorldBlueprint
    {
        public List<DayBlueprint> days = new();
        public List<NPCBlueprint> npcs = new();
        public List<AreaBlueprint> areas = new();
        public List<SubjectBlueprint> subjects = new();
    }

    [Serializable] public class DayBlueprint
    {
        public int day;
        public string weekday;
        public List<QuestBlueprint> quests = new();
    }

    [Serializable] public class QuestBlueprint
    {
        public string type;
        public string area;
        public string start;
        public string deadline;
        public string title;
        public string subscene;
        public string quizSet;
        public string confirmText;
    }

    [Serializable] public class NPCBlueprint
    {
        public string id;
        public string displayName;
        public VectorBlueprint pos;
        public string movement;
        public string dialogue;
        public List<ScheduleBlueprintEntry> schedule = new();
    }

    [Serializable] public class ScheduleBlueprintEntry
    {
        public string time;
        public string area;
    }

    [Serializable] public class AreaBlueprint
    {
        public string id;
        public VectorBlueprint pos;
        public VectorBlueprint size;
        public string scene;
        public string sub;
    }

    [Serializable] public class VectorBlueprint
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable] public class SubjectBlueprint
    {
        public string id;
        public string displayName;
        public int lessons = 1;
        public int maxPerLesson = 40;
    }
}
