using UnityEngine;

namespace Ash
{
    /// <summary>Fixed 2.5D view of the existing XZ gameplay plane; no perspective shrinkage.</summary>
    public static class IsometricView
    {
        // Equal foreshortening of the X, Y and Z axes, with ground axes 30 degrees to horizontal.
        public static Quaternion Rotation => Quaternion.Euler(35.2643897f, -45f, 0);
        public const float Distance = 90f;

        public static void Configure(Camera camera)
        {
            camera.orthographic = true;
            camera.transform.rotation = Rotation;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 300f;
        }

        public static float FitSize(Camera camera, Bounds bounds, Rect area)
        {
            float horizontal = 0, vertical = 0;
            for (int corner = 0; corner < 8; corner++)
            {
                var offset = Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1,
                    (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                horizontal = Mathf.Max(horizontal, Mathf.Abs(Vector3.Dot(offset, camera.transform.right)));
                vertical = Mathf.Max(vertical, Mathf.Abs(Vector3.Dot(offset, camera.transform.up)));
            }
            return 1.08f * Mathf.Max(horizontal / (camera.aspect * area.width), vertical / area.height);
        }

        public static Vector3 Position(Camera camera, Vector3 target, Rect area, float size)
        {
            return target - camera.transform.forward * Distance
                - camera.transform.right * ((area.center.x - .5f) * 2 * size * camera.aspect)
                - camera.transform.up * ((area.center.y - .5f) * 2 * size);
        }
    }
}
