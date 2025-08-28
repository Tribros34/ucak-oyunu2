using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PreFlightStore
{
    [System.Serializable]
    public class PlaneSnap
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector2Int gridCell;
        public int dataRotation;
    }

    private static List<PlaneSnap> _snaps;
    private static bool _pendingRestore;

    public static bool HasPendingRestore => _pendingRestore;

    public static void Capture(List<AirplaneController> planes)
    {
        _snaps = new List<PlaneSnap>(planes.Count);
        foreach (var p in planes)
        {
            if (!p) continue;
            var ad = p.GetComponent<AirplaneData>();

            var snap = new PlaneSnap
            {
                position = p.transform.position,
                rotation = p.transform.rotation,
                gridCell = MultiGridManager.Instance != null
                           ? MultiGridManager.Instance.GetGridPosition(p.transform.position)
                           : Vector2Int.zero,
                dataRotation = ad != null ? ad.rotation : Mathf.RoundToInt(p.transform.eulerAngles.z)
            };
            _snaps.Add(snap);
        }
    }

    public static void RequestSceneReloadToPreFlight(float delaySeconds = 0f)
    {
        if (_snaps == null || _snaps.Count == 0) return;
        _pendingRestore = true;

        if (delaySeconds <= 0f)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            // Delay’li yükleme (animasyon bitmesini beklemek istersen)
            var runner = new GameObject("[PreFlightRunner]").AddComponent<MonoRunner>();
            runner.Run(ReloadAfter(delaySeconds));
        }
    }

    private static System.Collections.IEnumerator ReloadAfter(float t)
    {
        yield return new WaitForSeconds(t);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Sahne yeniden yüklenince MultiGridManager.Awake() içinden çağrılacak
    public static void ApplyRestore(MultiGridManager mgm)
    {
        if (!_pendingRestore || _snaps == null) return;

        // Mevcut işgalleri sıfırla
        foreach (var ac in new List<AirplaneController>(mgm.allAirplanes))
            if (ac) Object.Destroy(ac.gameObject);
        mgm.allAirplanes.Clear();

        foreach (var s in _snaps)
        {
            // Sahnedeki uçak prefabını bilmiyorsan, mevcut bir “Airplane” template’ini kopyalayabilirsin.
            // Projende her uçak zaten sahnede olduğundan çoğu projede template gerekmez;
            // burada basitçe boş GO oluşturup gerekli komponentleri ekleyelim:
            var go = new GameObject("Airplane");
            var ad = go.AddComponent<AirplaneData>();
            var ac = go.AddComponent<AirplaneController>();
            go.AddComponent<SpriteRenderer>(); // kendi sprite’ını atarsın
            go.AddComponent<BoxCollider2D>().isTrigger = true;

            // Transform ve data
            ad.rotation = s.dataRotation;
            var world = mgm.GetWorldPosition(s.gridCell.x, s.gridCell.y);
            go.transform.SetPositionAndRotation(new Vector3(world.x, world.y, 0f), Quaternion.Euler(0, 0, s.dataRotation));

            // Grid işgali ve liste
            mgm.Occupy(ad, s.gridCell, ad.rotation);
            mgm.allAirplanes.Add(ac);

            ac.SyncMoveDirectionFromRotation();
        }

        _pendingRestore = false;
    }

    // Küçük yardımcı: delay’li coroutine çalıştırmak için görünmez runner
    private class MonoRunner : MonoBehaviour
    {
        public void Run(System.Collections.IEnumerator co) => StartCoroutine(co);
    }
}
