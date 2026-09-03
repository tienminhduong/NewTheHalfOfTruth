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
    }
}