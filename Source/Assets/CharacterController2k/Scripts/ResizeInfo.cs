using UnityEngine;

namespace CharacterController2k
{
    // Resize info for OpenCharacterController (e.g. delayed resizing until it is safe to resize).
    public class ResizeInfo
    {
        // Intervals (seconds) in which to check if the capsule's height/center must be changed.
        const float k_PendingUpdateIntervals = 1.0f;

        // Height to set.
        public float? height { get; private set; }

        // Center to set.
        public Vector3? center { get; private set; }

        // Time.time when the height must be set.
        public float? heightTime { get; private set; }

        // Time.time when the center must be set.
        public float? centerTime { get; private set; }

        // Set the pending height.
        public void SetHeight(float newHeight)
        {
            height = newHeight;
            if (heightTime == null)
            {
                heightTime = Time.time + k_PendingUpdateIntervals;
            }
        }

        // Set the pending center.
        public void SetCenter(Vector3 newCenter)
        {
            center = newCenter;
            if (centerTime == null)
            {
                centerTime = Time.time + k_PendingUpdateIntervals;
            }
        }

        // Set the pending height and center.
        public void SetHeightAndCenter(float newHeight, Vector3 newCenter)
        {
            SetHeight(newHeight);
            SetCenter(newCenter);
        }

        // Cancel the pending height.
        public void CancelHeight()
        {
            height = null;
            heightTime = null;
        }

        // Cancel the pending center.
        public void CancelCenter()
        {
            center = null;
            centerTime = null;
        }

        // Cancel the pending height and center.
        public void CancelHeightAndCenter()
        {
            CancelHeight();
            CancelCenter();
        }

        // Clear the timers.
        public void ClearTimers()
        {
            heightTime = null;
            centerTime = null;
        }
    }
}