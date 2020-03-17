using UnityEngine;

namespace CharacterController2k
{
    // Stuck info and logic used by the OpenCharacterController.
    public class StuckInfo
    {
        // For keeping track of the character's position, to determine when the character gets stuck.
        Vector3? m_StuckPosition;

        // Count how long the character is in the same position.
        int m_StuckPositionCount;

        // If character's position does not change by more than this amount then we assume the character is stuck.
        const float k_StuckDistance = 0.001f;

        // If character collided this number of times during the movement loop then test if character is stuck by examining the position
        const int k_HitCountForStuck = 6;

        // Assume character is stuck if the position is the same for longer than this number of loop iterations
        const int k_MaxStuckPositionCount = 1;

        /// <summary>
        /// Is the character stuck in the current move loop iteration?
        /// </summary>
        public bool isStuck { get; set; }

        /// <summary>
        /// Count the number of collisions during movement, to determine when the character gets stuck.
        /// </summary>
        public int hitCount { get; set; }


        /// <summary>
        /// Called when the move loop starts.
        /// </summary>
        public void OnMoveLoop()
        {
            hitCount = 0;
            m_StuckPositionCount = 0;
            m_StuckPosition = null;
            isStuck = false;
        }

        /// <summary>
        /// Is the character stuck during the movement loop (e.g. bouncing between 2 or more colliders)?
        /// </summary>
        /// <param name="characterPosition">The character's position.</param>
        /// <param name="currentMoveVector">Current move vector.</param>
        /// <param name="originalMoveVector">Original move vector.</param>
        /// <returns></returns>
        public bool UpdateStuck(Vector3 characterPosition, Vector3 currentMoveVector,
                                Vector3 originalMoveVector)
        {
            // First test
            if (!isStuck)
            {
                // From Quake2: "if velocity is against the original velocity, stop dead to avoid tiny occilations in sloping corners"
                if (currentMoveVector.sqrMagnitude.NotEqualToZero() &&
                    Vector3.Dot(currentMoveVector, originalMoveVector) <= 0.0f)
                {
                    isStuck = true;
                }
            }

            // Second test
            if (!isStuck)
            {
                // Test if collided and while position remains the same
                if (hitCount < k_HitCountForStuck)
                {
                    return false;
                }

                if (m_StuckPosition == null)
                {
                    m_StuckPosition = characterPosition;
                }
                else if (Vector3.Distance(m_StuckPosition.Value, characterPosition) <= k_StuckDistance)
                {
                    m_StuckPositionCount++;
                    if (m_StuckPositionCount > k_MaxStuckPositionCount)
                    {
                        isStuck = true;
                    }
                }
                else
                {
                    m_StuckPositionCount = 0;
                    m_StuckPosition = null;
                }
            }

            if (isStuck)
            {
                isStuck = false;
                hitCount = 0;
                m_StuckPositionCount = 0;
                m_StuckPosition = null;

                return true;
            }

            return false;
        }
    }
}