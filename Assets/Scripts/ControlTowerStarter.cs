using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ControlTowerStarter : MonoBehaviour
{
    [Tooltip("Bir kere basılınca ikinci kez çalışmasın")]
    public bool singleUse = true;

    private bool used;

    private void OnMouseUpAsButton()
    {
        if (used && singleUse) return;

        // 1) Önce MultiGridManager varsa onu kullan
        var mgm = MultiGridManager.Instance;
        if (mgm != null && mgm.allAirplanes != null && mgm.allAirplanes.Count > 0)
        {
            Debug.Log($"[Tower] MultiGridManager ile {mgm.allAirplanes.Count} uçak başlatılıyor.");
            mgm.StartAllAirplanes(); // listedekilere start verir
            used = true;
            return;
        }

        // 2) Liste boşsa sahnedeki tüm uçakları bul ve başlat
        var planes = FindObjectsOfType<AirplaneController>(true).ToList();
        if (planes.Count > 0)
        {
            Debug.Log($"[Tower] Sahneden {planes.Count} uçak bulundu, doğrudan başlatılıyor.");
            foreach (var p in planes) p.StartRolling();
            used = true;
        }
        else
        {
            Debug.LogWarning("[Tower] Uçak bulunamadı! Uçak prefablerında AirplaneController olduğundan ve aktif olduğundan emin ol.");
        }
    }
}
