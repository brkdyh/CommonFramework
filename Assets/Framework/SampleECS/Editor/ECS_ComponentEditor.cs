using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ECS_ComponentEditor
{
    public Dictionary<string, Dictionary<string, bool>> componentFoldValue = new Dictionary<string, Dictionary<string, bool>>();

    public bool GetFoldoutValue(string cp_guid, string fold_id)
    {
        if (!componentFoldValue.ContainsKey(cp_guid))
            componentFoldValue.Add(cp_guid, new Dictionary<string, bool>());

        var fold_map = componentFoldValue[cp_guid];
        if (!fold_map.ContainsKey(fold_id))
            fold_map.Add(fold_id, false);

        return fold_map[fold_id];
    }

    public void SetFoldoutValue(string cp_guid, string fold_id, bool value)
    {
        if (!componentFoldValue.ContainsKey(cp_guid))
            componentFoldValue.Add(cp_guid, new Dictionary<string, bool>());

        var fold_map = componentFoldValue[cp_guid];
        if (!fold_map.ContainsKey(fold_id))
            fold_map.Add(fold_id, value);

        fold_map[fold_id] = value;
    }
}
