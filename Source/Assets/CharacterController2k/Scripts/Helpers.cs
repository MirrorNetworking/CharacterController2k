using UnityEngine;

namespace Controller2k
{
    public static class Helpers
    {
        public static Vector3 GetTopSphereWorldPositionSimulated(Transform transform, Vector3 center, float height, float scaledRadius)
        {
            float scaledHeight = height * transform.lossyScale.y;
            Vector3 sphereOffsetY = Vector3.up * (scaledHeight / 2.0f - scaledRadius);
            Vector3 transformedCenter = transform.TransformVector(center);
            return transform.position + transformedCenter + sphereOffsetY;
        }

        public static Vector3 GetBottomSphereWorldPositionSimulated(Transform transform, Vector3 center, float height, float scaledRadius)
        {
            float scaledHeight = height * transform.lossyScale.y;
            Vector3 sphereOffsetY = Vector3.up * (scaledHeight / 2.0f - scaledRadius);
            Vector3 transformedCenter = transform.TransformVector(center);
            return transform.position + transformedCenter - sphereOffsetY;
        }
    }
}