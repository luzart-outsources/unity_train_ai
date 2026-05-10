using UnityEngine;

namespace Luzart.NewBase
{
    public class SelectSwitchGameObject : SelectSwitch
    {
        public GroupGameObject[] obSelects;

        public override void Select(int index)
        {
            int length = obSelects.Length;
            for (int i = 0; i < length; i++)
            {
                SetActiveGroup(obSelects[i], false);
            }
            if (index >= length)
            {
                return;
            }
            SetActiveGroup(obSelects[index], true);
        }
        private void SetActiveObject(GameObject ob, bool status)
        {
            if (ob != null)
            {
                ob.SetActive(status);
            }
        }
        private void SetActiveGroup(GroupGameObject group, bool status)
        {
            if (group != null && group.obGroups != null && group.obGroups.Length > 0)
            {
                int length = group.obGroups.Length;
                for (int i = 0; i < length; i++)
                {
                    SetActiveObject(group.obGroups[i], status);
                }
            }
        }

        [System.Serializable]
        public class GroupGameObject
        {
            public GameObject[] obGroups;
        }
    }
}
