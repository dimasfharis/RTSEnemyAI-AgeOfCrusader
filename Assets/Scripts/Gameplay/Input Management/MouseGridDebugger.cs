using UnityEngine;
using UnityEngine.Tilemaps;

namespace RTS.Gameplay.InputManagement
{
    public class MouseGridDebugger : MonoBehaviour
    {
        public Tilemap tilemap;
        public Vector3Int currentGridPosition;

        #region Unity Lifecycle

        private void Update()
        {
            currentGridPosition = GetMouseWorldToCellPos();
        }

        #endregion

        #region Helpers

        private Vector3Int GetMouseWorldToCellPos()
        {
            if (tilemap != null)
            {
                // Take world position of the mouse
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0f;

                // Convert to grid coordinate (cell)
                Vector3Int cellPos = tilemap.WorldToCell(mouseWorldPos);

                return cellPos;
            }
            else
            {
                return Vector3Int.zero;
            }
        }

        #endregion
    }
}