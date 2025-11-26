using System.Collections.Generic;
using UnityEngine;

namespace TurfSystem
{
    [DisallowMultipleComponent]
    public class TurfEdgeVisual : MonoBehaviour
    {
        [Tooltip("6 child GameObjects representing the 6 hex edges (E, SE, SW, W, NW, NE)")]
        [SerializeField] private List<GameObject> edgeSections = new List<GameObject>();

        // For testing in Editor
        [SerializeField][Range(0, 63)] private int debugEdgeMask = 0;

        private void OnValidate()
        {
            SetEdgeMask(debugEdgeMask);
        }

        public void SetEdgeMask(int edgeMask)
        {
            if (edgeSections.Count < 6)
            {
                Debug.LogWarning($"TurfEdgeVisual on {name} needs 6 edge sections!", this);
                return;
            }

            edgeSections[0].SetActive((edgeMask & 0x01) != 0); // E
            edgeSections[1].SetActive((edgeMask & 0x02) != 0); // SE
            edgeSections[2].SetActive((edgeMask & 0x04) != 0); // SW
            edgeSections[3].SetActive((edgeMask & 0x08) != 0); // W
            edgeSections[4].SetActive((edgeMask & 0x10) != 0); // NW
            edgeSections[5].SetActive((edgeMask & 0x20) != 0); // NE
        }
    }
}