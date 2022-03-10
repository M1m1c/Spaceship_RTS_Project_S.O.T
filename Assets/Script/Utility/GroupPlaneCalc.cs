using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GroupPlaneCalc
{
   public static Vector3 GetGroupPlane(Dictionary<int,SelectableEntity> selectionGroup)
    {
        var averageX = 0f;
        var averageY = 0f;
        var averageZ = 0f;

        foreach (var pair in selectionGroup)
        {
            var pos = pair.Value.transform.position;
            averageX += pos.x;
            averageY += pos.y;
            averageZ += pos.z;
        }

        averageX = averageX / (float)selectionGroup.Count;
        averageY = averageY / (float)selectionGroup.Count;
        averageZ = averageZ / (float)selectionGroup.Count;

        return new Vector3(averageX, averageY, averageZ);
   }

    public static float GetAverageYPos(Dictionary<int, SelectableEntity> selectionGroup)
    {
        var averageY = 0f;

        foreach (var pair in selectionGroup)
        {
            var pos = pair.Value.transform.position;
            averageY += pos.y;
        }
        averageY = averageY / (float)selectionGroup.Count;

        return averageY;
    }
}

