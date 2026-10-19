using UnityEngine;

namespace UnityExtensions
{
    public static class Extensions
    {
        public static Vector3 GetTopCenter(this SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer == null)
                return Vector3.zero;

            return spriteRenderer.bounds.center + Vector3.up * spriteRenderer.bounds.extents.y;
        }

        public static CD.FaceDir Reverse(this CD.FaceDir direction)
        {
            return direction switch
            {
                CD.FaceDir.Up => CD.FaceDir.Down,
                CD.FaceDir.Down => CD.FaceDir.Up,
                CD.FaceDir.Left => CD.FaceDir.Right,
                CD.FaceDir.Right => CD.FaceDir.Left,
                _ => direction
            };
        }

        public static void EnsureComponent<T>(this GameObject gameObject, ref T component, bool addIfMissing = false) where T : Component
        {
            if (component != null)
                return;

            component = gameObject.GetComponent<T>();
            if (component == null && addIfMissing)
                component = gameObject.AddComponent<T>();
            else if (component == null)
                Debug.LogError($"Component of type {typeof(T).Name} is missing on GameObject '{gameObject.name}' and was not added.");
        }
    }
}