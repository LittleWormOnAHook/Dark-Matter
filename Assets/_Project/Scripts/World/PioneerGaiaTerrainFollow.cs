using Gaia;
using UnityEngine;

/// <summary>
/// Play-mode stand-in for Gaia's missing TerrainLoader: keeps tiles loaded around this transform.
/// Range is meters. 2500 covers about 3 of the 2048m DM Genesis tiles.
/// </summary>
[DefaultExecutionOrder(50)]
public class PioneerGaiaTerrainFollow : MonoBehaviour
{
    public float loadRange = 2500f;

    void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (TerrainLoaderManager.Instance == null || !GaiaUtils.HasDynamicLoadedTerrains())
        {
            return;
        }

        Vector3 pos = transform.position;
        Vector3Double center = new Vector3Double(pos.x, pos.y, pos.z);
        Vector3Double size = new Vector3Double(loadRange * 2f, loadRange * 2f, loadRange * 2f);
        BoundsDouble regular = new BoundsDouble(center, size);
        TerrainLoaderManager.Instance.UpdateTerrainLoadState(regular, null, gameObject);
    }
}
