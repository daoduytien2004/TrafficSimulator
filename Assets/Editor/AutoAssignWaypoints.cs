using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using FCG;

public class AutoAssignWaypoints
{
    [MenuItem("Tools/Auto Assign Waypoints cho tất cả BlockingCar")]
    static void AutoAssign()
    {
        BlockingCar[] allCars = Object.FindObjectsOfType<BlockingCar>();
        FCGWaypointsContainer[] allWays = Object.FindObjectsOfType<FCGWaypointsContainer>();

        if (allWays.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy FCGWaypointsContainer nào trong scene!\nHãy đảm bảo scene đang có waypoint của FCG.", "OK");
            return;
        }

        int assigned = 0;
        int skipped = 0;
        int failed = 0;

        foreach (var bc in allCars)
        {
            TrafficCar tc = bc.GetComponent<TrafficCar>();
            if (tc == null) continue;

            if (tc.atualWay != null)
            {
                skipped++;
                continue;
            }

            FCGWaypointsContainer best = FindBest(bc.transform, allWays);

            if (best != null)
            {
                tc.atualWay = best.transform;
                EditorUtility.SetDirty(tc);
                assigned++;
            }
            else
            {
                Debug.LogWarning($"[AutoAssign] Không tìm thấy waypoint phù hợp: {bc.name}");
                failed++;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Hoàn thành!",
            $"Đã gán:       {assigned} xe\n" +
            $"Bỏ qua:      {skipped} xe (đã có waypoint)\n" +
            $"Thất bại:     {failed} xe (không tìm thấy waypoint)\n\n" +
            "Nhớ nhấn Ctrl+S để lưu scene!",
            "OK"
        );
    }

    static FCGWaypointsContainer FindBest(Transform car, FCGWaypointsContainer[] allWays)
    {
        float minDist = float.MaxValue;
        FCGWaypointsContainer best = null;

        foreach (var cw in allWays)
        {
            if (cw.waypoints == null || cw.waypoints.Count < 2) continue;

            Vector3 pathDir = (cw.waypoints[cw.waypoints.Count - 1].position
                             - cw.waypoints[0].position).normalized;

            float alignment = Vector3.Dot(car.forward, pathDir);
            if (alignment < 0.3f) continue;

            foreach (var wp in cw.waypoints)
            {
                float dist = Vector3.Distance(car.position, wp.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    best = cw;
                }
            }
        }

        return best;
    }
}
