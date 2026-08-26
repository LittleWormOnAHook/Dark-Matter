using Gaia;
using UnityEngine;

/// <summary>
/// Legacy play-mode loader. TLM RefreshRuntimePlayerLoading now streams the
/// 4 nearest tiles. This component stays on Player_v7 for the Bind menu but
/// does not issue extra load bounds (that was stacking to 6 terrains).
/// </summary>
[DefaultExecutionOrder(50)]
public class PioneerGaiaTerrainFollow : MonoBehaviour
{
    public float loadRange = 1800f;

    void LateUpdate()
    {
        // TLM owns play-mode streaming. Do not call UpdateTerrainLoadState here.
    }
}
