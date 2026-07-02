using UnityEngine;

namespace RTS.Gameplay.CameraManagement
{
    public class CameraController : MonoBehaviour
    {
        [Header("Pan Settings")]
        public float panSpeed = 10f;
        public float keyboardPanSpeed = 20f;
        public float panBorderThickness = 10f;

        [Header("Zoom Settings")]
        public float zoomSpeed = 5f;
        public float minZoom = 5f;
        public float maxZoom = 40f;

        [Header("Drag Settings")]
        public float dragSpeed = 1f;
        private Vector3 dragOrigin;

        [Header("Bounds")]
        public bool limitBounds = true;
        public Vector2 minBounds = new Vector2(0, 0);
        public Vector2 maxBounds = new Vector2(100, 100);

        Camera cam;

        void Start()
        {
            cam = Camera.main;
        }

        void Update()
        {
            HandleKeyboardPan();
            HandleMouseDragPan();
            HandleZoom();
            ClampCameraPosition();
        }

        void HandleKeyboardPan()
        {
            Vector3 pos = transform.position;

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
                pos.y += keyboardPanSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
                pos.y -= keyboardPanSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                pos.x -= keyboardPanSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                pos.x += keyboardPanSpeed * Time.deltaTime;

            transform.position = pos;
        }

        void HandleMouseDragPan()
        {
            // When middle mouse clicked first time, mouse start position
            // recorded on dragOrigin
            if (Input.GetMouseButtonDown(2))    // Middle mouse button first time
            {
                dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            }

            // While middle mouse being held, script calculate difference
            // between start position and recent position
            // then camera being moved according to the difference
            if (Input.GetMouseButton(2))        // Middle mouse being held
            {
                Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
                transform.position += difference;
            }
        }

        void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            // scroll > 0, means zoom in
            // scroll < 0, means zoom out
            if (scroll != 0)
            {
                float newSize = cam.orthographicSize - scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            }
        }

        void ClampCameraPosition()
        {
            /*if (!limitBounds) return;

            float camHeight = cam.orthographicSize;
            float camWidth = cam.aspect * camHeight;

            float minX = minBounds.x + camWidth;
            float maxX = minBounds.x - camWidth;
            float minY = minBounds.y + camHeight;
            float maxY = minBounds.y - camHeight;

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;*/
        }
    }
}