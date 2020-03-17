// CharacterController2k is based on Unity's OpenCharacterController, modified by vis2k, all rights reserved.
//
// -------------------------------------------------------------------------------------------------------------
// Original License from: https://github.com/Unity-Technologies/Standard-Assets-Characters:
// Licensed under the Unity Companion License for Unity-dependent projects--see Unity Companion License.
// Unless expressly provided otherwise, the Software under this license is made available strictly on an “AS IS” BASIS WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED. Please review the license for details on these and other terms and conditions.
//
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterController2k
{
    // <summary>
    // Open character controller. Handles the movement of a character, by using a capsule for movement and collision detection.
    // Note: The capsule is always upright. It ignores rotation.
    // </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class OpenCharacterController : MonoBehaviour
    {
        // Fired on collision with colliders in the world
        public event Action<CollisionInfo> collision;

        [Header("Player Root")]
        [FormerlySerializedAs("m_PlayerRootTransform"), Tooltip("The root bone in the avatar.")]
        public Transform playerRootTransform;

        [FormerlySerializedAs("m_RootTransformOffset"), Tooltip("The root transform will be positioned at this offset.")]
        public Vector3 rootTransformOffset = Vector3.zero;

        [Header("Collision")]
        [FormerlySerializedAs("m_SlopeLimit"), Tooltip("Limits the collider to only climb slopes that are less steep (in degrees) than the indicated value.")]
        public float slopeLimit = 45.0f;

        [SerializeField, Tooltip("The character will step up a stair only if it is closer to the ground than the indicated value. " +
                 "This should not be greater than the Character Controller’s height or it will generate an error. " +
                 "Generally this should be kept as small as possible.")]
        float m_StepOffset = 0.3f;

        [SerializeField, Tooltip(
            "Two colliders can penetrate each other as deep as their Skin Width. Larger Skin Widths reduce jitter. " +
            "Low Skin Width can cause the character to get stuck. A good setting is to make this value 10% of the Radius.")]
        float m_SkinWidth = 0.08f;

        [SerializeField, Tooltip(
            "If the character tries to move below the indicated value, it will not move at all. This can be used to reduce jitter. " +
            "In most situations this value should be left at 0.")]
#pragma warning disable CS0649 // Field is never assigned to
        float m_MinMoveDistance;
#pragma warning restore CS0649 // Field is never assigned to

        [SerializeField, Tooltip("Distance to test beneath the character when doing the grounded test. Increase if controller.isGrounded doesn't give the correct results or switches between true/false a lot.")]
        float m_GroundedTestDistance = 0.002f; // 0.001f isn't enough for big BoxColliders like uSurvival's Floor, even though it would work for MeshColliders.

        [SerializeField, Tooltip("This will offset the Capsule Collider in world space, and won’t affect how the Character pivots. " +
                 "Ideally, x and z should be zero to avoid rotating into another collider.")]
        Vector3 m_Center;

        [SerializeField, Tooltip("Length of the Capsule Collider’s radius. This is essentially the width of the collider.")]
        float m_Radius = 0.5f;

        [SerializeField, Tooltip("The Character’s Capsule Collider height. It should be at least double the radius.")]
        float m_Height = 2.0f;

        [SerializeField, Tooltip("Layers to test against for collisions.")]
        LayerMask m_CollisionLayerMask = ~0; // ~0 sets it to Everything

        [SerializeField, Tooltip("Is the character controlled by a local human? If true then more calculations are done for more " +
                 "accurate movement.")]
        bool m_IsLocalHuman = true;

        [SerializeField, Tooltip("Can character slide vertically when touching the ceiling? (For example, if ceiling is sloped.)")]
        bool m_SlideAlongCeiling = true;

        [SerializeField, Tooltip("Should the character slow down against walls?")]
        bool m_SlowAgainstWalls = false;

        [SerializeField, Range(0.0f, 90.0f), Tooltip("The minimal angle from which the character will start slowing down on walls.")]
        float m_MinSlowAgainstWallsAngle = 10.0f;

        [SerializeField, Tooltip("The desired interaction that cast calls should make against triggers")]
        QueryTriggerInteraction m_TriggerQuery = QueryTriggerInteraction.Ignore;

        [Header("Sliding")]
        [SerializeField, Tooltip("Should the character slide down slopes when their angle is more than the slope limit?")]
        bool m_SlideDownSlopes = true;

        [SerializeField, Tooltip("The maximum speed that the character can slide downwards")]
        float m_SlideMaxSpeed = 10.0f;

        [SerializeField, Tooltip("Gravity scale to apply when sliding down slopes.")]
        float m_SlideGravityScale = 1.0f;

        [SerializeField, Tooltip("The time (in seconds) after initiating a slide classified as a slide start. Used to disable jumping.")]
        float m_SlideStartTime = 0.25f;

        // Max slope limit.
        const float k_MaxSlopeLimit = 90.0f;

        // Max slope angle on which character can slide down automatically.
        const float k_MaxSlopeSlideAngle = 90.0f;

        // Distance to test for ground when sliding down slopes.
        const float k_SlideDownSlopeTestDistance = 1.0f;

        // Slight delay before we stop sliding down slopes. To handle cases where sliding test fails for a few frames.
        const float k_StopSlideDownSlopeDelay = 0.5f;

        // Distance to push away from slopes when sliding down them.
        const float k_PushAwayFromSlopeDistance = 0.001f;

        // Minimum distance to use when checking ahead for steep slopes, when checking if it's safe to do the step offset.
        const float k_MinCheckSteepSlopeAheadDistance = 0.2f;

        // Min skin width.
        const float k_MinSkinWidth = 0.0001f;

        // The maximum move iterations. Mainly used as a fail safe to prevent an infinite loop.
        const int k_MaxMoveIterations = 20;

        // Stick to the ground if it is less than this distance from the character.
        const float k_MaxStickToGroundDownDistance = 1.0f;

        // Min distance to test for the ground when sticking to the ground.
        const float k_MinStickToGroundDownDistance = 0.01f;

        // Max colliders to use in the overlap methods.
        const int k_MaxOverlapColliders = 10;

        // Offset to use when moving to a collision point, to try to prevent overlapping the colliders
        const float k_CollisionOffset = 0.001f;

        // Minimum distance to move. This minimizes small penetrations and inaccurate casts (e.g. into the floor)
        const float k_MinMoveDistance = 0.0001f;

        // Minimum sqr distance to move. This minimizes small penetrations and inaccurate casts (e.g. into the floor)
        const float k_MinMoveSqrDistance = k_MinMoveDistance * k_MinMoveDistance;

        // Minimum step offset height to move (if character has a step offset).
        const float k_MinStepOffsetHeight = k_MinMoveDistance;

        // Small value to test if the movement vector is small.
        const float k_SmallMoveVector = 1e-6f;

        // If angle between raycast and capsule/sphere cast normal is less than this then use the raycast normal, which is more accurate.
        const float k_MaxAngleToUseRaycastNormal = 5.0f;

        // Scale the capsule/sphere hit distance when doing the additional raycast to get a more accurate normal
        const float k_RaycastScaleDistance = 2.0f;

        // Slope check ahead is clamped by the distance moved multiplied by this scale.
        const float k_SlopeCheckDistanceMultiplier = 5.0f;

        // The capsule collider.
        CapsuleCollider m_CapsuleCollider;

        // The position at the start of the movement.
        Vector3 m_StartPosition;

        // Movement vectors used in the move loop.
        List<MoveVector> m_MoveVectors = new List<MoveVector>();

        // Next index in the moveVectors list.
        int m_NextMoveVectorIndex;

        // Surface normal of the last collision while moving down.
        Vector3? m_DownCollisionNormal;

        // Stuck info.
        StuckInfo m_StuckInfo = new StuckInfo();

        // The collision info when hitting colliders.
        Dictionary<Collider, CollisionInfo> m_CollisionInfoDictionary = new Dictionary<Collider, CollisionInfo>();

        // Slight delay before stopping the sliding down slopes.
        float m_DelayStopSlidingDownSlopeTime;

        // Pending resize info to set when it is safe to do so.
        readonly ResizeInfo m_PendingResize = new ResizeInfo();

        // Collider array used for UnityEngine.Physics.OverlapCapsuleNonAlloc in GetPenetrationInfo
        readonly Collider[] m_PenetrationInfoColliders = new Collider[k_MaxOverlapColliders];

        // Velocity of the last movement. It's the new position minus the old position.
        Vector3 m_Velocity;

        // Factor used to perform a slow down against the walls.
        float m_InvRescaleFactor;

        // How long has character been sliding down a steep slope? (Zero means not busy sliding.)
        float m_SlidingDownSlopeTime;

        // Default center of the capsule (e.g. for resetting it).
        Vector3 m_DefaultCenter;

        // Used to offset movement raycast when determining if a slope is travesable.
        float m_SlopeMovementOffset;

        // Is character busy sliding down a steep slope?
        public bool isSlidingDownSlope { get { return m_SlidingDownSlopeTime > 0.0f; } }

        // The capsule center with scaling and rotation applied.
        Vector3 transformedCenter { get { return transform.TransformVector(m_Center); } }

        // The capsule height with the relevant scaling applied (e.g. if object scale is not 1,1,1)
        float scaledHeight { get { return m_Height * transform.lossyScale.y; } }

        // Is the character on the ground? This is updated during Move or SetPosition.
        public bool isGrounded { get; private set; }

        // Collision flags from the last move.
        public CollisionFlags collisionFlags { get; private set; }

        // Default height of the capsule (e.g. for resetting it).
        public float defaultHeight { get; private set; }

        // Is the character able to be slowed down by walls?
        public bool slowAgainstWalls { get { return m_SlowAgainstWalls; } }

        // Is the character sliding and has been sliding less than slideDownTimeUntilJumAllowed
        public bool startedSlide { get { return isSlidingDownSlope && m_SlidingDownSlopeTime <= m_SlideStartTime; } }

        // The capsule radius with the relevant scaling applied (e.g. if object scale is not 1,1,1)
        public float scaledRadius
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                float maxScale = Mathf.Max(Mathf.Max(scale.x, scale.y), scale.z);
                return m_Radius * maxScale;
            }
        }

        // Is the character controlled by a local human? If true then more calculations are done for more accurate movement.
        public bool IsLocalHuman { get { return m_IsLocalHuman; } }

        // vis2k: add old character controller compatibility
        public Vector3 velocity => m_Velocity;
        public Vector3 center => m_Center;
        public float height => m_Height;
        public float radius => m_Radius;
        public Bounds bounds => m_CapsuleCollider.bounds;

        // Initialise the capsule and rigidbody, and set the root position.
        void Awake()
        {
            InitCapsuleColliderAndRigidbody();

            SetRootToOffset();

            m_InvRescaleFactor = 1 / Mathf.Cos(m_MinSlowAgainstWallsAngle * Mathf.Deg2Rad);
            m_SlopeMovementOffset =  m_StepOffset / Mathf.Tan(slopeLimit * Mathf.Deg2Rad);
        }

        // Set the root position.
        void LateUpdate()
        {
            SetRootToOffset();
        }

        // Update sliding down slopes, and changes to the capsule's height and center.
        void Update()
        {
            UpdateSlideDownSlopes();
            UpdatePendingHeightAndCenter();
        }

#if UNITY_EDITOR
        // Validate the capsule.
        void OnValidate()
        {
            Vector3 position = transform.position;
            ValidateCapsule(false, ref position);
            transform.position = position;
            SetRootToOffset();

            m_InvRescaleFactor = 1 / Mathf.Cos(m_MinSlowAgainstWallsAngle * Mathf.Deg2Rad);
        }

        // Draws the debug Gizmos
        void OnDrawGizmosSelected()
        {
            // Foot position
            Gizmos.color = Color.cyan;
            Vector3 footPosition = GetFootWorldPosition(transform.position);
            Gizmos.DrawLine(footPosition + Vector3.left * scaledRadius,
                            footPosition + Vector3.right * scaledRadius);
            Gizmos.DrawLine(footPosition + Vector3.back * scaledRadius,
                            footPosition + Vector3.forward * scaledRadius);

            // Top of head
            Vector3 headPosition = transform.position + transformedCenter + Vector3.up * (scaledHeight / 2.0f + m_SkinWidth);
            Gizmos.DrawLine(headPosition + Vector3.left * scaledRadius,
                            headPosition + Vector3.right * scaledRadius);
            Gizmos.DrawLine(headPosition + Vector3.back * scaledRadius,
                            headPosition + Vector3.forward * scaledRadius);

            // Center position
            Vector3 centerPosition = transform.position + transformedCenter;
            Gizmos.DrawLine(centerPosition + Vector3.left * scaledRadius,
                            centerPosition + Vector3.right * scaledRadius);
            Gizmos.DrawLine(centerPosition + Vector3.back * scaledRadius,
                            centerPosition + Vector3.forward * scaledRadius);
        }
#endif

        // Move the character. This function does not apply any gravity.
        //   moveVector: Move along this vector.
        // CollisionFlags is the summary of collisions that occurred during the Move.
        public CollisionFlags Move(Vector3 moveVector)
        {
            MoveInternal(moveVector, true);
            return collisionFlags;
        }

        // Set the position of the character.
        //   position: Position to set.
        //   updateGrounded: Update the grounded state? This uses a cast, so only set it to true if you need it.
        public void SetPosition(Vector3 position, bool updateGrounded)
        {
            transform.position = position;

            if (updateGrounded)
            {
                UpdateGrounded(CollisionFlags.None);
            }
        }

        // Compute the minimal translation required to separate the character from the collider.
        //   positionOffset: Position offset to add to the capsule collider's position.
        //   collider: The collider to test.
        //   colliderPosition: Position of the collider.
        //   colliderRotation: Rotation of the collider.
        //   direction: Direction along which the translation required to separate the colliders apart is minimal.
        //   distance: The distance along direction that is required to separate the colliders apart.
        //   includeSkinWidth: Include the skin width in the test?
        //   currentPosition: Position of the character
        // True if found penetration.
        bool ComputePenetration(Vector3 positionOffset,
                                       Collider collider, Vector3 colliderPosition, Quaternion colliderRotation,
                                       out Vector3 direction, out float distance,
                                       bool includeSkinWidth, Vector3 currentPosition)
        {
            if (collider == m_CapsuleCollider)
            {
                // Ignore self
                direction = Vector3.one;
                distance = 0.0f;
                return false;
            }

            if (includeSkinWidth)
            {
                m_CapsuleCollider.radius = m_Radius + m_SkinWidth;
                m_CapsuleCollider.height = m_Height + (m_SkinWidth * 2.0f);
            }

            // Note: Physics.ComputePenetration does not always return values when the colliders overlap.
            bool result = Physics.ComputePenetration(m_CapsuleCollider,
                                                     currentPosition + positionOffset,
                                                     Quaternion.identity,
                                                     collider, colliderPosition, colliderRotation,
                                                     out direction, out distance);
            if (includeSkinWidth)
            {
                m_CapsuleCollider.radius = m_Radius;
                m_CapsuleCollider.height = m_Height;
            }

            return result;
        }

        // Check for collision below the character, using a ray or sphere cast.
        //   distance: Distance to check.
        //   hitInfo: Get the hit info.
        //   offsetPosition: Position offset. If we want to do a cast relative to the character's current position.
        //   useSphereCast: Use a sphere cast? If false then use a ray cast.
        //   useSecondSphereCast: The second cast includes the skin width. Ideally only needed for human controlled player, for more accuracy.
        //   adjustPositionSlightly: Adjust position slightly up, in case it's already inside an obstacle.
        //   currentPosition: Position of the character
        // True if collision occurred.
        public bool CheckCollisionBelow(float distance, out RaycastHit hitInfo, Vector3 currentPosition,
                                        Vector3 offsetPosition,
                                        bool useSphereCast = false,
                                        bool useSecondSphereCast = false,
                                        bool adjustPositionSlightly = false)
        {
            bool didCollide = false;
            float extraDistance = adjustPositionSlightly ? k_CollisionOffset : 0.0f;
            if (!useSphereCast)
            {
#if UNITY_EDITOR
                Vector3 start = GetFootWorldPosition(currentPosition) + offsetPosition + Vector3.up * extraDistance;
                Debug.DrawLine(start, start + Vector3.down * (distance + extraDistance), Color.red);
#endif
                if (Physics.Raycast(GetFootWorldPosition(currentPosition) + offsetPosition + Vector3.up * extraDistance,
                                    Vector3.down,
                                    out hitInfo,
                                    distance + extraDistance,
                                    GetCollisionLayerMask(),
                                    m_TriggerQuery))
                {
                    didCollide = true;
                    hitInfo.distance = Mathf.Max(0.0f, hitInfo.distance - extraDistance);
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.DrawRay(currentPosition, Vector3.down, Color.red); // Center

                Debug.DrawRay(currentPosition +  new Vector3(scaledRadius, 0.0f), Vector3.down, Color.blue);
                Debug.DrawRay(currentPosition +  new Vector3(-scaledRadius, 0.0f), Vector3.down, Color.blue);
                Debug.DrawRay(currentPosition +  new Vector3(0.0f, 0.0f, scaledRadius), Vector3.down, Color.blue);
                Debug.DrawRay(currentPosition +  new Vector3(0.0f, 0.0f, -scaledRadius), Vector3.down, Color.blue);
#endif
                if (SmallSphereCast(Vector3.down,
                                    GetSkinWidth() + distance,
                                    out hitInfo,
                                    offsetPosition,
                                    true, currentPosition))
                {
                    didCollide = true;
                    hitInfo.distance = Mathf.Max(0.0f, hitInfo.distance - GetSkinWidth());
                }

                if (!didCollide && useSecondSphereCast)
                {
                    if (BigSphereCast(Vector3.down,
                                      distance + extraDistance, currentPosition,
                                      out hitInfo,
                                      offsetPosition + Vector3.up * extraDistance,
                                      true))
                    {
                        didCollide = true;
                        hitInfo.distance = Mathf.Max(0.0f, hitInfo.distance - extraDistance);
                    }
                }
            }

            return didCollide;
        }

        // Get the skin width.
        public float GetSkinWidth()
        {
            return m_SkinWidth;
        }

        // Get the minimum move sqr distance.
        float GetMinMoveSqrDistance()
        {
            return m_MinMoveDistance * m_MinMoveDistance;
        }

        // Set the capsule's height and center.
        //   newHeight: The new height.
        //   newCenter: The new center.
        //   checkForPenetration: Check for collision, and then de-penetrate if there's collision?
        //   updateGrounded: Update the grounded state? This uses a cast, so only set it to true if you need it.
        // Returns the height that was set, which may be different to newHeight because of validation.
        public float SetHeightAndCenter(float newHeight, Vector3 newCenter, bool checkForPenetration,
                                        bool updateGrounded)
        {
            float oldHeight = m_Height;
            Vector3 oldCenter = m_Center;
            Vector3 oldPosition = transform.position;
            var cancelPending = true;
            Vector3 virtualPosition = oldPosition;

            SetCenter(newCenter, false, false);
            SetHeight(newHeight, false, false, false);

            if (checkForPenetration)
            {
                if (Depenetrate(ref virtualPosition))
                {
                    // Inside colliders?
                    if (CheckCapsule(virtualPosition))
                    {
                        // Wait until it is safe to resize
                        cancelPending = false;
                        m_PendingResize.SetHeightAndCenter(newHeight, newCenter);
                        // Restore data
                        m_Height = oldHeight;
                        m_Center = oldCenter;
                        transform.position = oldPosition;
                        ValidateCapsule(true, ref virtualPosition);
                    }
                }
            }

            if (cancelPending)
            {
                m_PendingResize.CancelHeightAndCenter();
            }

            if (updateGrounded)
            {
                UpdateGrounded(CollisionFlags.None);
            }

            transform.position = virtualPosition;
            return m_Height;
        }

        // Reset the capsule's height and center to the default values.
        //   checkForPenetration: Check for collision, and then de-penetrate if there's collision?
        //   updateGrounded: Update the grounded state? This uses a cast, so only set it to true if you need it.
        // Returns the reset height.
        public float ResetHeightAndCenter(bool checkForPenetration, bool updateGrounded)
        {
            return SetHeightAndCenter(defaultHeight, m_DefaultCenter, checkForPenetration, updateGrounded);
        }

        // Get the capsule's center (local).
        public Vector3 GetCenter()
        {
            return m_Center;
        }

        // Set the capsule's center (local).
        //   newCenter: The new center.
        //   checkForPenetration: Check for collision, and then de-penetrate if there's collision?
        //   updateGrounded: Update the grounded state? This uses a cast, so only set it to true if you need it.
        public void SetCenter(Vector3 newCenter, bool checkForPenetration, bool updateGrounded)
        {
            Vector3 oldCenter = m_Center;
            Vector3 oldPosition = transform.position;
            bool cancelPending = true;
            Vector3 virtualPosition = oldPosition;

            m_Center = newCenter;
            ValidateCapsule(true, ref virtualPosition);

            if (checkForPenetration)
            {
                if (Depenetrate(ref virtualPosition))
                {
                    // Inside colliders?
                    if (CheckCapsule(virtualPosition))
                    {
                        // Wait until it is safe to resize
                        cancelPending = false;
                        m_PendingResize.SetCenter(newCenter);
                        // Restore data
                        m_Center = oldCenter;
                        transform.position = oldPosition;
                        ValidateCapsule(true, ref virtualPosition);
                    }
                }
            }

            if (cancelPending)
            {
                m_PendingResize.CancelCenter();
            }

            if (updateGrounded)
            {
                UpdateGrounded(CollisionFlags.None);
            }

            transform.position = virtualPosition;
        }

        // Reset the capsule's center to the default value.
        //   checkForPenetration: Check for collision, and then de-penetrate if there's collision?
        //   updateGrounded: Update the grounded state? This uses a cast, so only set it to true if you need it.
        public void ResetCenter(bool checkForPenetration, bool updateGrounded)
        {
            SetCenter(m_DefaultCenter, checkForPenetration, updateGrounded);
        }

        // Get the capsule's height (local).
        public float GetHeight()
        {
            return m_Height;
        }

        // Validate the capsule's height. (It must be at least double the radius size.)
        // The valid height.
        public float ValidateHeight(float newHeight)
        {
            return Mathf.Clamp(newHeight, m_Radius * 2.0f, float.MaxValue);
        }

        // Set the capsule's height (local). Minimum limit is double the capsule radius size.
        // Call CanSetHeight if you want to test if height can change, e.g. when changing from crouch to stand.
        //   newHeight: The new height.
        //   preserveFootPosition: Adjust the capsule's center to preserve the foot position?
        //   checkForPenetration: Check for collision, and then de-penetrate if there's collision?
        //   updateGrounded: Update the grounded state? This uses a cast, so only set it to true if you need it.
        // Returns the height that was set, which may be different to newHeight because of validation.
        public float SetHeight(float newHeight, bool preserveFootPosition, bool checkForPenetration,
                               bool updateGrounded)
        {
            // vis2k fix:
            // IMPORTANT: adjust height BEFORE ever calculating the center.
            //            previously it was adjusted AFTER calculating the center.
            //            so the center would NOT EXACTLY be the center anymore
            //            if the height was adjusted.
            //            => causing all future center calculations to be wrong.
            //            => causing center.y to increase every time
            //            => causing the character to float in the air over time
            //            see also: https://github.com/vis2k/uMMORPG/issues/36
            newHeight = ValidateHeight(newHeight);

            Vector3 virtualPosition = transform.position;
            bool changeCenter = preserveFootPosition;
            Vector3 newCenter = changeCenter ? CalculateCenterWithSameFootPosition(newHeight) : m_Center;
            if (Mathf.Approximately(m_Height, newHeight))
            {
                // Height remains the same
                m_PendingResize.CancelHeight();
                if (changeCenter)
                {
                    SetCenter(newCenter, checkForPenetration, updateGrounded);
                }

                return m_Height;
            }

            float oldHeight = m_Height;
            Vector3 oldCenter = m_Center;
            Vector3 oldPosition = transform.position;
            bool cancelPending = true;

            if (changeCenter)
            {
                m_Center = newCenter;
            }

            m_Height = newHeight;
            ValidateCapsule(true, ref virtualPosition);

            if (checkForPenetration)
            {
                if (Depenetrate(ref virtualPosition))
                {
                    // Inside colliders?
                    if (CheckCapsule(virtualPosition))
                    {
                        // Wait until it is safe to resize
                        cancelPending = false;
                        if (changeCenter)
                        {
                            m_PendingResize.SetHeightAndCenter(newHeight, newCenter);
                        }
                        else
                        {
                            m_PendingResize.SetHeight(newHeight);
                        }

                        // Restore data
                        m_Height = oldHeight;
                        if (changeCenter)
                        {
                            m_Center = oldCenter;
                        }

                        transform.position = oldPosition;
                        ValidateCapsule(true, ref virtualPosition);
                    }
                }
            }

            if (cancelPending)
            {
                if (changeCenter)
                {
                    m_PendingResize.CancelHeightAndCenter();
                }
                else
                {
                    m_PendingResize.CancelHeight();
                }
            }

            if (updateGrounded)
            {
                UpdateGrounded(CollisionFlags.None);
            }

            transform.position = virtualPosition;
            return m_Height;
        }

        // vis2k: add missing CanSetHeight function & helper functions
        // Collider array used for UnityEngine.Physics.OverlapCapsuleNonAlloc in GetPenetrationInfo
        static Vector3 GetTopSphereWorldPositionSimulated(Transform transform, Vector3 center, float height, float scaledRadius)
        {
            float scaledHeight = height * transform.lossyScale.y;
            Vector3 sphereOffsetY = Vector3.up * (scaledHeight / 2.0f - scaledRadius);
            Vector3 transformedCenter = transform.TransformVector(center);
            return transform.position + transformedCenter + sphereOffsetY;
        }
        static Vector3 GetBottomSphereWorldPositionSimulated(Transform transform, Vector3 center, float height, float scaledRadius)
        {
            float scaledHeight = height * transform.lossyScale.y;
            Vector3 sphereOffsetY = Vector3.up * (scaledHeight / 2.0f - scaledRadius);
            Vector3 transformedCenter = transform.TransformVector(center);
            return transform.position + transformedCenter - sphereOffsetY;
        }
        readonly Collider[] m_OverlapCapsuleColliders = new Collider[k_MaxOverlapColliders];
        public bool CanSetHeight(float newHeight, bool preserveFootPosition)
        {
            // vis2k fix:
            // IMPORTANT: adjust height BEFORE ever calculating the center.
            //            previously it was adjusted AFTER calculating the center.
            //            so the center would NOT EXACTLY be the center anymore
            //            if the height was adjusted.
            //            => causing all future center calculations to be wrong.
            //            => causing center.y to increase every time
            //            => causing the character to float in the air over time
            //            see also: https://github.com/vis2k/uMMORPG/issues/36
            newHeight = ValidateHeight(newHeight);

            // calculate the new capsule center & height
            bool changeCenter = preserveFootPosition;
            Vector3 newCenter = changeCenter ? CalculateCenterWithSameFootPosition(newHeight) : m_Center;
            if (Mathf.Approximately(m_Height, newHeight))
            {
                // Height remains the same
                return true;
            }

            // debug draw
            Debug.DrawLine(
                GetTopSphereWorldPositionSimulated(transform, newCenter, newHeight, scaledRadius),
                GetBottomSphereWorldPositionSimulated(transform, newCenter, newHeight, scaledRadius),
                Color.yellow,
                3f
            );

            // check the overlap capsule
            int hits = UnityEngine.Physics.OverlapCapsuleNonAlloc(
                GetTopSphereWorldPositionSimulated(transform, newCenter, newHeight, scaledRadius),
                GetBottomSphereWorldPositionSimulated(transform, newCenter, newHeight, scaledRadius),
                radius,
                m_OverlapCapsuleColliders,
                GetCollisionLayerMask(),
                m_TriggerQuery);

            for (int i = 0; i < hits; ++i)
            {
                // a collider that is not self?
                Collider col = m_OverlapCapsuleColliders[i];
                if (col != m_CapsuleCollider)
                {
                    return false;
                }
            }

            // no overlaps
            return true;
        }

        // Reset the capsule's height to the default value.
        //   preserveFootPosition: Adjust the capsule's center to preserve the foot position?
        //   checkForPenetration: Check for collision, and then de-penetrate if there's collision?
        //   updateGrounded: Update the grounded state? This uses a cast, so only set it to true if you need it.
        // Returns the reset height.
        public float ResetHeight(bool preserveFootPosition, bool checkForPenetration, bool updateGrounded)
        {
            return SetHeight(defaultHeight, preserveFootPosition, checkForPenetration, updateGrounded);
        }

        // Get the layers to test for collision.
        public LayerMask GetCollisionLayerMask()
        {
            return m_CollisionLayerMask;
        }

        // Get the foot world position.
        public Vector3 GetFootWorldPosition()
        {
            return transform.position + transformedCenter + (Vector3.down * (scaledHeight / 2.0f + m_SkinWidth));
        }

        // Get the foot world position.
        Vector3 GetFootWorldPosition(Vector3 position)
        {
            return position + transformedCenter + (Vector3.down * (scaledHeight / 2.0f + m_SkinWidth));
        }

        // Get the top sphere's world position.
        Vector3 GetTopSphereWorldPosition(Vector3 position)
        {
            Vector3 sphereOffsetY = Vector3.up * (scaledHeight / 2.0f - scaledRadius);
            return position + transformedCenter + sphereOffsetY;
        }

        // Get the bottom sphere's world position.
        Vector3 GetBottomSphereWorldPosition(Vector3 position)
        {
            Vector3 sphereOffsetY = Vector3.up * (scaledHeight / 2.0f - scaledRadius);
            return position + transformedCenter - sphereOffsetY;
        }

        // Initialize the capsule collider and the rigidbody
        void InitCapsuleColliderAndRigidbody()
        {
            GameObject go = transform.gameObject;
            m_CapsuleCollider = go.GetComponent<CapsuleCollider>();

            // Copy settings to the capsule collider
            m_CapsuleCollider.center = m_Center;
            m_CapsuleCollider.radius = m_Radius;
            m_CapsuleCollider.height = m_Height;

            // Ensure that the rigidbody is kinematic and does not use gravity
            Rigidbody rigidbody = go.GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            defaultHeight = m_Height;
            m_DefaultCenter = m_Center;
        }

        // Call this when the capsule's values change.
        //      updateCapsuleCollider: Update the capsule collider's values (e.g. center, height, radius)?
        //      currentPosition: position of the character
        //      checkForPenetration: Check for collision, and then de-penetrate if there's collision?
        //      updateGrounded: Update the grounded state? This uses a cast, so only set it to true if you need it.
        void ValidateCapsule(bool updateCapsuleCollider,
                             ref Vector3 currentPosition,
                             bool checkForPenetration = false,
                             bool updateGrounded = false)
        {
            slopeLimit = Mathf.Clamp(slopeLimit, 0.0f, k_MaxSlopeLimit);
            m_SkinWidth = Mathf.Clamp(m_SkinWidth, k_MinSkinWidth, float.MaxValue);
            float oldHeight = m_Height;
            m_Height = ValidateHeight(m_Height);

            if (m_CapsuleCollider != null)
            {
                if (updateCapsuleCollider)
                {
                    // Copy settings to the capsule collider
                    m_CapsuleCollider.center = m_Center;
                    m_CapsuleCollider.radius = m_Radius;
                    m_CapsuleCollider.height = m_Height;
                }
                else if (!Mathf.Approximately(m_Height, oldHeight))
                {
                    // Height changed
                    m_CapsuleCollider.height = m_Height;
                }
            }

            if (checkForPenetration)
            {
                Depenetrate(ref currentPosition);
            }

            if (updateGrounded)
            {
                UpdateGrounded(CollisionFlags.None);
            }
        }

        // Calculate a new center if the height changes and preserve the foot position.
        //      newHeight: New height
        Vector3 CalculateCenterWithSameFootPosition(float newHeight)
        {
            float localFootY = m_Center.y - (m_Height / 2.0f + m_SkinWidth);
            float newCenterY = localFootY + (newHeight / 2.0f + m_SkinWidth);
            return new Vector3(m_Center.x, newCenterY, m_Center.z);
        }

        // Moves the characters.
        //      moveVector: Move vector
        //      slideWhenMovingDown: Slide against obstacles when moving down? (e.g. we don't want to slide when applying gravity while the character is grounded)
        //      forceTryStickToGround: Force try to stick to ground? Only used if character is grounded before moving.
        //      doNotStepOffset: Do not try to perform the step offset?
        void MoveInternal(Vector3 moveVector, bool slideWhenMovingDown,
                                            bool forceTryStickToGround = false,
                                            bool doNotStepOffset = false)
        {
            bool wasGrounded = isGrounded;
            Vector3 moveVectorNoY = new Vector3(moveVector.x, 0.0f, moveVector.z);
            bool tryToStickToGround = wasGrounded && (forceTryStickToGround || (moveVector.y <= 0.0f && moveVectorNoY.sqrMagnitude.NotEqualToZero()));

            m_StartPosition = transform.position;

            collisionFlags = CollisionFlags.None;
            m_CollisionInfoDictionary.Clear();
            m_DownCollisionNormal = null;

            // Stop sliding down slopes when character jumps
            if (moveVector.y > 0.0f && isSlidingDownSlope)
            {
                StopSlideDownSlopes();
            }

            // Do the move loop
            MoveLoop(moveVector, tryToStickToGround, slideWhenMovingDown, doNotStepOffset);

            bool doDownCast = tryToStickToGround || moveVector.y <= 0.0f;
            UpdateGrounded(collisionFlags, doDownCast);

            // vis2k: fix velocity
            // set velocity, which is direction * speed. we don't have speed,
            // but we do have elapsed time
            m_Velocity = (transform.position - m_StartPosition) / Time.deltaTime;

            BroadcastCollisionEvent();
        }

        // Send hit messages.
        void BroadcastCollisionEvent()
        {
            if (collision == null || m_CollisionInfoDictionary == null || m_CollisionInfoDictionary.Count <= 0)
            {
                return;
            }

            foreach (KeyValuePair<Collider, CollisionInfo> kvp in m_CollisionInfoDictionary)
            {
                collision(kvp.Value);
            }
        }

        // Determine if the character is grounded.
        //      movedCollisionFlags: Moved collision flags of the current move. Set to None if not moving.
        //      doDownCast: Do a down cast? We want to avoid this when the character is jumping upwards.
        void UpdateGrounded(CollisionFlags movedCollisionFlags, bool doDownCast = true)
        {
            if ((movedCollisionFlags & CollisionFlags.CollidedBelow) != 0)
            {
                isGrounded = true;
            }
            else if (doDownCast)
            {
                isGrounded = CheckCollisionBelow(m_GroundedTestDistance,
                                                 out RaycastHit hitInfo,
                                                 transform.position,
                                                 Vector3.zero,
                                                 true,
                                                 m_IsLocalHuman,
                                                 m_IsLocalHuman);
            }
            else
            {
                isGrounded = false;
            }
        }

        // Movement loop. Keep moving until completely blocked by obstacles, or we reached the desired position/distance.
        //      moveVector: The move vector.
        //      tryToStickToGround: Try to stick to the ground?
        //      slideWhenMovingDown: Slide against obstacles when moving down? (e.g. we don't want to slide when applying gravity while the charcter is grounded)
        //      doNotStepOffset: Do not try to perform the step offset?
        void MoveLoop(Vector3 moveVector, bool tryToStickToGround, bool slideWhenMovingDown, bool doNotStepOffset)
        {
            m_MoveVectors.Clear();
            m_NextMoveVectorIndex = 0;

            // Split the move vector into horizontal and vertical components.
            SplitMoveVector(moveVector, slideWhenMovingDown, doNotStepOffset);
            MoveVector remainingMoveVector = m_MoveVectors[m_NextMoveVectorIndex];
            m_NextMoveVectorIndex++;

            bool didTryToStickToGround = false;
            m_StuckInfo.OnMoveLoop();
            Vector3 virtualPosition = transform.position;

            // The loop
            for (int i = 0; i < k_MaxMoveIterations; i++)
            {
                Vector3 refMoveVector = remainingMoveVector.moveVector;
                bool collided = MoveMajorStep(ref refMoveVector, remainingMoveVector.canSlide, didTryToStickToGround, ref virtualPosition);

                remainingMoveVector.moveVector = refMoveVector;

                // Character stuck?
                if (m_StuckInfo.UpdateStuck(virtualPosition, remainingMoveVector.moveVector, moveVector))
                {
                    // Stop current move loop vector
                    remainingMoveVector = new MoveVector(Vector3.zero);
                }
                else if (!m_IsLocalHuman && collided)
                {
                    // Only slide once for non-human controlled characters
                    remainingMoveVector.canSlide = false;
                }

                // Not collided OR vector used up (i.e. vector is zero)?
                if (!collided || remainingMoveVector.moveVector.sqrMagnitude.IsEqualToZero())
                {
                    // Are there remaining movement vectors?
                    if (m_NextMoveVectorIndex < m_MoveVectors.Count)
                    {
                        remainingMoveVector = m_MoveVectors[m_NextMoveVectorIndex];
                        m_NextMoveVectorIndex++;
                    }
                    else
                    {
                        if (!tryToStickToGround || didTryToStickToGround)
                        {
                            break;
                        }

                        // Try to stick to the ground
                        didTryToStickToGround = true;
                        if (!CanStickToGround(moveVector, out remainingMoveVector))
                        {
                            break;
                        }
                    }
                }

#if UNITY_EDITOR
                if (i == k_MaxMoveIterations - 1)
                {
                    Debug.LogWarning(name + " reached MaxMoveInterations(" + k_MaxMoveIterations + "): remainingVector=" + remainingMoveVector + " moveVector=" + moveVector + " hitCount=" + m_StuckInfo.hitCount);
                }
#endif
            }

            transform.position = virtualPosition;
        }

        // A single movement major step. Returns true when there is collision.
        //      moveVector: The move vector.
        //      canSlide: Can slide against obstacles?
        //      tryGrounding: Try grounding the player?
        //      currentPosition: position of the character
        bool MoveMajorStep(ref Vector3 moveVector, bool canSlide, bool tryGrounding, ref Vector3 currentPosition)
        {
            Vector3 direction = moveVector.normalized;
            float distance = moveVector.magnitude;
            RaycastHit bigRadiusHitInfo;
            RaycastHit smallRadiusHitInfo;
            bool smallRadiusHit;
            bool bigRadiusHit;

            if (!CapsuleCast(direction, distance, currentPosition,
                             out smallRadiusHit, out bigRadiusHit,
                             out smallRadiusHitInfo, out bigRadiusHitInfo,
                             Vector3.zero))
            {
                // No collision, so move to the position
                MovePosition(moveVector, null, null, ref currentPosition);

                // Check for penetration
                float penetrationDistance;
                Vector3 penetrationDirection;
                if (GetPenetrationInfo(out penetrationDistance, out penetrationDirection, currentPosition))
                {
                    // Push away from obstacles
                    MovePosition(penetrationDirection * penetrationDistance, null, null, ref currentPosition);
                }

                // Stop current move loop vector
                moveVector = Vector3.zero;

                return false;
            }

            // Did the big radius not hit an obstacle?
            if (!bigRadiusHit)
            {
                // The small radius hit an obstacle, so character is inside an obstacle
                MoveAwayFromObstacle(ref moveVector, ref smallRadiusHitInfo,
                                     direction, distance,
                                     canSlide,
                                     tryGrounding,
                                     true, ref currentPosition);

                return true;
            }

            // Use the nearest collision point (e.g. to handle cases where 2 or more colliders' edges meet)
            if (smallRadiusHit && smallRadiusHitInfo.distance < bigRadiusHitInfo.distance)
            {
                MoveAwayFromObstacle(ref moveVector, ref smallRadiusHitInfo,
                                     direction, distance,
                                     canSlide,
                                     tryGrounding,
                                     true, ref currentPosition);
                return true;
            }

            MoveAwayFromObstacle(ref moveVector, ref bigRadiusHitInfo,
                                 direction, distance,
                                 canSlide,
                                 tryGrounding,
                                 false, ref currentPosition);

            return true;
        }

        // Can the character perform a step offset?
        //      moveVector: Horizontal movement vector.
        bool CanStepOffset(Vector3 moveVector)
        {
            float moveVectorMagnitude = moveVector.magnitude;
            Vector3 position = transform.position;
            RaycastHit hitInfo;

            // Only step up if there's an obstacle at the character's feet (e.g. do not step when only character's head collides)
            if (!SmallSphereCast(moveVector, moveVectorMagnitude, out hitInfo, Vector3.zero, true, position) &&
                !BigSphereCast(moveVector, moveVectorMagnitude, position, out hitInfo, Vector3.zero, true))
            {
                return false;
            }

            float upDistance = Mathf.Max(m_StepOffset, k_MinStepOffsetHeight);

            // We only step over obstacles if we can partially fit on it (i.e. fit the capsule's radius)
            Vector3 horizontal = moveVector * scaledRadius;
            float horizontalSize = horizontal.magnitude;
            horizontal.Normalize();

            // Any obstacles ahead (after we moved up)?
            Vector3 up = Vector3.up * upDistance;
            if (SmallCapsuleCast(horizontal, GetSkinWidth() + horizontalSize, out hitInfo, up, position) ||
                BigCapsuleCast(horizontal, horizontalSize, out hitInfo, up, position))
            {
                return false;
            }

            return !CheckSteepSlopeAhead(moveVector);
        }

        // Returns true if there's a steep slope ahead.
        //      moveVector: The movement vector.
        //      alsoCheckForStepOffset: Do a second test where the step offset will move the player to?
        bool CheckSteepSlopeAhead(Vector3 moveVector, bool alsoCheckForStepOffset = true)
        {
            Vector3 direction = moveVector.normalized;
            float distance = moveVector.magnitude;

            if (CheckSteepSlopAhead(direction, distance, Vector3.zero))
            {
                return true;
            }

            // Only need to do the second test for human controlled character
            if (!alsoCheckForStepOffset || !m_IsLocalHuman)
            {
                return false;
            }

            // Check above where the step offset will move the player to
            return CheckSteepSlopAhead(direction,
                                       Mathf.Max(distance, k_MinCheckSteepSlopeAheadDistance),
                                       Vector3.up * m_StepOffset);
        }

        // Returns true if there's a steep slope ahead.
        bool CheckSteepSlopAhead(Vector3 direction, float distance, Vector3 offsetPosition)
        {
            RaycastHit bigRadiusHitInfo;
            RaycastHit smallRadiusHitInfo;
            bool smallRadiusHit;
            bool bigRadiusHit;

            if (!CapsuleCast(direction, distance, transform.position,
                             out smallRadiusHit, out bigRadiusHit,
                             out smallRadiusHitInfo, out bigRadiusHitInfo,
                             offsetPosition))
            {
                // No collision
                return false;
            }

            RaycastHit hitInfoCapsule = (!bigRadiusHit || (smallRadiusHit && smallRadiusHitInfo.distance < bigRadiusHitInfo.distance)) ?
                                        smallRadiusHitInfo :
                                        bigRadiusHitInfo;

            RaycastHit hitInfoRay;
            Vector3 rayOrigin = transform.position + transformedCenter + offsetPosition;

            float offset = Mathf.Clamp(m_SlopeMovementOffset, 0.0f, distance * k_SlopeCheckDistanceMultiplier);
            Vector3 rayDirection = (hitInfoCapsule.point + direction * offset) - rayOrigin;

            // Raycast returns a more accurate normal than SphereCast/CapsuleCast
            if (Physics.Raycast(rayOrigin,
                                rayDirection,
                                out hitInfoRay,
                                rayDirection.magnitude * k_RaycastScaleDistance,
                                GetCollisionLayerMask(),
                                m_TriggerQuery) &&
                hitInfoRay.collider == hitInfoCapsule.collider)
            {
                hitInfoCapsule = hitInfoRay;
            }
            else
            {
                return false;
            }

            float slopeAngle = Vector3.Angle(Vector3.up, hitInfoCapsule.normal);
            bool slopeIsSteep = slopeAngle > slopeLimit &&
                                slopeAngle < k_MaxSlopeLimit &&
                                Vector3.Dot(direction, hitInfoCapsule.normal) < 0.0f;

            return slopeIsSteep;
        }

        // Split the move vector into horizontal and vertical components. The results are added to the moveVectors list.
        //      moveVector: The move vector.
        //      slideWhenMovingDown: Slide against obstacles when moving down? (e.g. we don't want to slide when applying gravity while the character is grounded)
        //      doNotStepOffset: Do not try to perform the step offset?
        void SplitMoveVector(Vector3 moveVector, bool slideWhenMovingDown, bool doNotStepOffset)
        {
            Vector3 horizontal = new Vector3(moveVector.x, 0.0f, moveVector.z);
            Vector3 vertical = new Vector3(0.0f, moveVector.y, 0.0f);
            bool horizontalIsAlmostZero = IsMoveVectorAlmostZero(horizontal);
            float tempStepOffset = m_StepOffset;
            bool doStepOffset = isGrounded &&
                                !doNotStepOffset &&
                                !Mathf.Approximately(tempStepOffset, 0.0f) &&
                                !horizontalIsAlmostZero;

            // Note: Vector is split in this order: up, horizontal, down

            if (vertical.y > 0.0f)
            {
                // Up
                if (horizontal.x.NotEqualToZero() || horizontal.z.NotEqualToZero())
                {
                    // Move up then horizontal
                    AddMoveVector(vertical, m_SlideAlongCeiling);
                    AddMoveVector(horizontal);
                }
                else
                {
                    // Move up
                    AddMoveVector(vertical, m_SlideAlongCeiling);
                }
            }
            else if (vertical.y < 0.0f)
            {
                // Down
                if (horizontal.x.NotEqualToZero() || horizontal.z.NotEqualToZero())
                {
                    if (doStepOffset && CanStepOffset(horizontal))
                    {
                        // Move up, horizontal then down
                        AddMoveVector(Vector3.up * tempStepOffset, false);
                        AddMoveVector(horizontal);
                        if (slideWhenMovingDown)
                        {
                            AddMoveVector(vertical);
                            AddMoveVector(Vector3.down * tempStepOffset);
                        }
                        else
                        {
                            AddMoveVector(vertical + Vector3.down * tempStepOffset);
                        }
                    }
                    else
                    {
                        // Move horizontal then down
                        AddMoveVector(horizontal);
                        AddMoveVector(vertical, slideWhenMovingDown);
                    }
                }
                else
                {
                    // Move down
                    AddMoveVector(vertical, slideWhenMovingDown);
                }
            }
            else
            {
                // Horizontal
                if (doStepOffset && CanStepOffset(horizontal))
                {
                    // Move up, horizontal then down
                    AddMoveVector(Vector3.up * tempStepOffset, false);
                    AddMoveVector(horizontal);
                    AddMoveVector(Vector3.down * tempStepOffset);
                }
                else
                {
                    // Move horizontal
                    AddMoveVector(horizontal);
                }
            }
        }

        // Add the movement vector to the moveVectors list.
        //      moveVector: Move vector to add.
        //      canSlide: Can the movement slide along obstacles?
        void AddMoveVector(Vector3 moveVector, bool canSlide = true)
        {
            m_MoveVectors.Add(new MoveVector(moveVector, canSlide));
        }

        // Is the movement vector almost zero (i.e. very small)?
        bool IsMoveVectorAlmostZero(Vector3 moveVector)
        {
            return (Mathf.Abs(moveVector.x) > k_SmallMoveVector ||
                    Mathf.Abs(moveVector.y) > k_SmallMoveVector ||
                    Mathf.Abs(moveVector.z) > k_SmallMoveVector) ? false : true;
        }

        // Test if character can stick to the ground, and set the down vector if so.
        //      moveVector: The original movement vector.
        //      getDownVector: Get the down vector.
        bool CanStickToGround(Vector3 moveVector, out MoveVector getDownVector)
        {
            Vector3 moveVectorNoY = new Vector3(moveVector.x, 0.0f, moveVector.z);
            float downDistance = Mathf.Max(moveVectorNoY.magnitude, k_MinStickToGroundDownDistance);
            if (moveVector.y < 0.0f)
            {
                downDistance = Mathf.Max(downDistance, Mathf.Abs(moveVector.y));
            }

            if (downDistance <= k_MaxStickToGroundDownDistance)
            {
                getDownVector = new MoveVector(Vector3.down * downDistance, false);
                return true;
            }

            getDownVector = new MoveVector(Vector3.zero);
            return false;
        }

        // Do two capsule casts. One excluding the capsule's skin width and one including the skin width.
        //      direction: Direction to cast
        //      distance: Distance to cast
        //      currentPosition: position of the character
        //      smallRadiusHit: Did hit, excluding the skin width?
        //      bigRadiusHit: Did hit, including the skin width?
        //      smallRadiusHitInfo: Hit info for cast excluding the skin width.
        //      bigRadiusHitInfo: Hit info for cast including the skin width.
        //      offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
        bool CapsuleCast(Vector3 direction, float distance, Vector3 currentPosition,
                                 out bool smallRadiusHit, out bool bigRadiusHit,
                                 out RaycastHit smallRadiusHitInfo, out RaycastHit bigRadiusHitInfo,
                                 Vector3 offsetPosition)
        {
            // Exclude the skin width in the test
            smallRadiusHit = SmallCapsuleCast(direction, distance, out smallRadiusHitInfo, offsetPosition, currentPosition);

            // Include the skin width in the test
            bigRadiusHit = BigCapsuleCast(direction, distance, out bigRadiusHitInfo, offsetPosition, currentPosition);

            return smallRadiusHit || bigRadiusHit;
        }

        // Do a capsule cast, excluding the skin width.
        //      direction: Direction to cast.
        //      distance: Distance to cast.
        //      smallRadiusHitInfo: Hit info.
        //      offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
        //      currentPosition: position of the character
        bool SmallCapsuleCast(Vector3 direction, float distance,
                              out RaycastHit smallRadiusHitInfo,
                              Vector3 offsetPosition, Vector3 currentPosition)
        {
            // Cast further than the distance we need, to try to take into account small edge cases (e.g. Casts fail
            // when moving almost parallel to an obstacle for small distances).
            float extraDistance = scaledRadius;

            if (Physics.CapsuleCast(GetTopSphereWorldPosition(currentPosition) + offsetPosition,
                                    GetBottomSphereWorldPosition(currentPosition) + offsetPosition,
                                    scaledRadius,
                                    direction,
                                    out smallRadiusHitInfo,
                                    distance + extraDistance,
                                    GetCollisionLayerMask(),
                                    m_TriggerQuery))
            {
                return smallRadiusHitInfo.distance <= distance;
            }

            return false;
        }

        // Do a capsule cast, includes the skin width.
        //      direction: Direction to cast.
        //      distance: Distance to cast.
        //      bigRadiusHitInfo: Hit info.
        //      offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
        //      currentPosition: position of the character
        bool BigCapsuleCast(Vector3 direction, float distance,
                                    out RaycastHit bigRadiusHitInfo,
                                    Vector3 offsetPosition, Vector3 currentPosition)
        {
            // Cast further than the distance we need, to try to take into account small edge cases (e.g. Casts fail
            // when moving almost parallel to an obstacle for small distances).
            float extraDistance = scaledRadius + GetSkinWidth();

            if (Physics.CapsuleCast(GetTopSphereWorldPosition(currentPosition) + offsetPosition,
                                    GetBottomSphereWorldPosition(currentPosition) + offsetPosition,
                                    scaledRadius + GetSkinWidth(),
                                    direction,
                                    out bigRadiusHitInfo,
                                    distance + extraDistance,
                                    GetCollisionLayerMask(),
                                    m_TriggerQuery))
            {
                return bigRadiusHitInfo.distance <= distance;
            }

            return false;
        }

        // Do a sphere cast, excludes the skin width. Sphere position is at the top or bottom of the capsule.
        //      direction: Direction to cast.
        //      distance: Distance to cast.
        //      smallRadiusHitInfo: Hit info.
        //      offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
        //      useBottomSphere: Use the sphere at the bottom of the capsule? If false then use the top sphere.
        //      currentPosition: position of the character
        bool SmallSphereCast(Vector3 direction, float distance,
                                     out RaycastHit smallRadiusHitInfo,
                                     Vector3 offsetPosition,
                                     bool useBottomSphere, Vector3 currentPosition)
        {
            // Cast further than the distance we need, to try to take into account small edge cases (e.g. Casts fail
            // when moving almost parallel to an obstacle for small distances).
            float extraDistance = scaledRadius;

            Vector3 spherePosition = useBottomSphere ? GetBottomSphereWorldPosition(currentPosition) + offsetPosition
                                                     : GetTopSphereWorldPosition(currentPosition) + offsetPosition;
            if (Physics.SphereCast(spherePosition,
                                   scaledRadius,
                                   direction,
                                   out smallRadiusHitInfo,
                                   distance + extraDistance,
                                   GetCollisionLayerMask(),
                                   m_TriggerQuery))
            {
                return smallRadiusHitInfo.distance <= distance;
            }

            return false;
        }

        // Do a sphere cast, including the skin width. Sphere position is at the top or bottom of the capsule.
        //      direction: Direction to cast.
        //      distance: Distance to cast.
        //      currentPosition: position of the character
        //      bigRadiusHitInfo: Hit info.
        //      offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
        //      useBottomSphere: Use the sphere at the bottom of the capsule? If false then use the top sphere.
        bool BigSphereCast(Vector3 direction, float distance, Vector3 currentPosition,
                                   out RaycastHit bigRadiusHitInfo,
                                   Vector3 offsetPosition,
                                   bool useBottomSphere)
        {
            // Cast further than the distance we need, to try to take into account small edge cases (e.g. Casts fail
            // when moving almost parallel to an obstacle for small distances).
            float extraDistance = scaledRadius + GetSkinWidth();

            Vector3 spherePosition = useBottomSphere ? GetBottomSphereWorldPosition(currentPosition) + offsetPosition
                                                     : GetTopSphereWorldPosition(currentPosition) + offsetPosition;
            if (Physics.SphereCast(spherePosition,
                                   scaledRadius + GetSkinWidth(),
                                   direction,
                                   out bigRadiusHitInfo,
                                   distance + extraDistance,
                                   GetCollisionLayerMask(),
                                   m_TriggerQuery))
            {
                return bigRadiusHitInfo.distance <= distance;
            }

            return false;
        }

        // Called when a capsule cast detected an obstacle. Move away from the obstacle and slide against it if needed.
        //      moveVector: The movement vector.
        //      hitInfoCapsule: Hit info of the capsule cast collision.
        //      direction: Direction of the cast.
        //      distance: Distance of the cast.
        //      canSlide: Can slide against obstacles?
        //      tryGrounding: Try grounding the player?
        //      hitSmallCapsule: Did the collision occur with the small capsule (i.e. no skin width)?
        //      currentPosition: position of the character
        void MoveAwayFromObstacle(ref Vector3 moveVector, ref RaycastHit hitInfoCapsule,
                                          Vector3 direction, float distance,
                                          bool canSlide,
                                          bool tryGrounding,
                                          bool hitSmallCapsule, ref Vector3 currentPosition)
        {
            // IMPORTANT: This method must set moveVector.

            // When the small capsule hit then stop skinWidth away from obstacles
            float collisionOffset = hitSmallCapsule ? GetSkinWidth() : k_CollisionOffset;

            float hitDistance = Mathf.Max(hitInfoCapsule.distance - collisionOffset, 0.0f);
            // Note: remainingDistance is more accurate is we use hitDistance, but using hitInfoCapsule.distance gives a tiny
            // bit of dampening when sliding along obstacles
            float remainingDistance = Mathf.Max(distance - hitInfoCapsule.distance, 0.0f);

            // Move to the collision point
            MovePosition(direction * hitDistance, direction, hitInfoCapsule, ref currentPosition);

            Vector3 hitNormal;
            RaycastHit hitInfoRay;
            Vector3 rayOrigin = currentPosition + transformedCenter;
            Vector3 rayDirection = hitInfoCapsule.point - rayOrigin;

            // Raycast returns a more accurate normal than SphereCast/CapsuleCast
            // Using angle <= k_MaxAngleToUseRaycastNormal gives a curve when collision is near an edge.
            if (Physics.Raycast(rayOrigin,
                                rayDirection,
                                out hitInfoRay,
                                rayDirection.magnitude * k_RaycastScaleDistance,
                                GetCollisionLayerMask(),
                                m_TriggerQuery) &&
                hitInfoRay.collider == hitInfoCapsule.collider &&
                Vector3.Angle(hitInfoCapsule.normal, hitInfoRay.normal) <= k_MaxAngleToUseRaycastNormal)
            {
                hitNormal = hitInfoRay.normal;
            }
            else
            {
                hitNormal = hitInfoCapsule.normal;
            }

            float penetrationDistance;
            Vector3 penetrationDirection;

            if (GetPenetrationInfo(out penetrationDistance, out penetrationDirection, currentPosition, true, null, hitInfoCapsule))
            {
                // Push away from the obstacle
                MovePosition(penetrationDirection * penetrationDistance, null, null, ref currentPosition);
            }

            var slopeIsSteep = false;
            if (tryGrounding || m_StuckInfo.isStuck)
            {
                // No further movement when grounding the character, or the character is stuck
                canSlide = false;
            }
            else if (moveVector.x.NotEqualToZero() || moveVector.z.NotEqualToZero())
            {
                // Test if character is trying to walk up a steep slope
                float slopeAngle = Vector3.Angle(Vector3.up, hitNormal);
                slopeIsSteep = slopeAngle > slopeLimit && slopeAngle < k_MaxSlopeLimit && Vector3.Dot(direction, hitNormal) < 0.0f;
            }

            // Set moveVector
            if (canSlide && remainingDistance > 0.0f)
            {
                Vector3 slideNormal = hitNormal;

                if (slopeIsSteep && slideNormal.y > 0.0f)
                {
                    // Do not move up the slope
                    slideNormal.y = 0.0f;
                    slideNormal.Normalize();
                }

                // Vector to slide along the obstacle
                Vector3 project = Vector3.Cross(direction, slideNormal);
                project = Vector3.Cross(slideNormal, project);

                if (slopeIsSteep && project.y > 0.0f)
                {
                    // Do not move up the slope
                    project.y = 0.0f;
                }

                project.Normalize();

                // Slide along the obstacle
                bool isWallSlowingDown = m_SlowAgainstWalls && m_MinSlowAgainstWallsAngle < 90.0f;

                if (isWallSlowingDown)
                {
                    // Cosine of angle between the movement direction and the tangent is equivalent to the sin of
                    // the angle between the movement direction and the normal, which is the sliding component of
                    // our movement.
                    float cosine = Vector3.Dot(project, direction);
                    float slowDownFactor = Mathf.Clamp01(cosine * m_InvRescaleFactor);

                    moveVector = project * (remainingDistance * slowDownFactor);
                }
                else
                {
                    // No slow down, keep the same speed even against walls.
                    moveVector = project * remainingDistance;
                }
            }
            else
            {
                // Stop current move loop vector
                moveVector = Vector3.zero;
            }

            if (direction.y < 0.0f && Mathf.Approximately(direction.x, 0.0f) && Mathf.Approximately(direction.z, 0.0f))
            {
                // This is used by the sliding down slopes
                m_DownCollisionNormal = hitNormal;
            }
        }

        // Check for collision penetration, then try to de-penetrate if there is collision.
        bool Depenetrate(ref Vector3 currentPosition)
        {
            float distance;
            Vector3 direction;
            if (GetPenetrationInfo(out distance, out direction, currentPosition))
            {
                MovePosition(direction * distance, null, null, ref currentPosition);
                return true;
            }

            return false;
        }

        // Get direction and distance to move out of the obstacle.
        //      getDistance: Get distance to move out of the obstacle.
        //      getDirection: Get direction to move out of the obstacle.
        //      currentPosition: position of the character
        //      includeSkinWidth: Include the skin width in the test?
        //      offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
        //      hitInfo: The hit info.
        bool GetPenetrationInfo(out float getDistance, out Vector3 getDirection,
                                Vector3 currentPosition,
                                bool includeSkinWidth = true,
                                Vector3? offsetPosition = null,
                                RaycastHit? hitInfo = null)
        {
            getDistance = 0.0f;
            getDirection = Vector3.zero;

            Vector3 offset = offsetPosition != null ? offsetPosition.Value : Vector3.zero;
            float tempSkinWidth = includeSkinWidth ? GetSkinWidth() : 0.0f;
            int overlapCount = Physics.OverlapCapsuleNonAlloc(GetTopSphereWorldPosition(currentPosition) + offset,
                                                              GetBottomSphereWorldPosition(currentPosition) + offset,
                                                              scaledRadius + tempSkinWidth,
                                                              m_PenetrationInfoColliders,
                                                              GetCollisionLayerMask(),
                                                              m_TriggerQuery);
            if (overlapCount <= 0 || m_PenetrationInfoColliders.Length <= 0)
            {
                return false;
            }

            bool result = false;
            Vector3 localPos = Vector3.zero;
            for (int i = 0; i < overlapCount; i++)
            {
                Collider collider = m_PenetrationInfoColliders[i];
                if (collider == null)
                {
                    break;
                }

                Vector3 direction;
                float distance;
                var colliderTransform = collider.transform;
                if (ComputePenetration(offset,
                                       collider, colliderTransform.position, colliderTransform.rotation,
                                       out direction, out distance, includeSkinWidth, currentPosition))
                {
                    localPos += direction * (distance + k_CollisionOffset);
                    result = true;
                }
                else if (hitInfo != null && hitInfo.Value.collider == collider)
                {
                    // We can use the hit normal to push away from the collider, because CapsuleCast generally returns a normal
                    // that pushes away from the collider.
                    localPos += hitInfo.Value.normal * k_CollisionOffset;
                    result = true;
                }
            }

            if (result)
            {
                getDistance = localPos.magnitude;
                getDirection = localPos.normalized;
            }

            return result;
        }

        // Check if any colliders overlap the capsule.
        //      includeSkinWidth: Include the skin width in the test?
        //      offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
        bool CheckCapsule(Vector3 currentPosition, bool includeSkinWidth = true,
                                  Vector3? offsetPosition = null)
        {
            Vector3 offset = offsetPosition != null ? offsetPosition.Value : Vector3.zero;
            float tempSkinWidth = includeSkinWidth ? GetSkinWidth() : 0.0f;
            return Physics.CheckCapsule(GetTopSphereWorldPosition(currentPosition) + offset,
                                        GetBottomSphereWorldPosition(currentPosition) + offset,
                                        scaledRadius + tempSkinWidth,
                                        GetCollisionLayerMask(),
                                        m_TriggerQuery);
        }

        // Move the capsule position.
        //      moveVector: Move vector.
        //      collideDirection: Direction we encountered collision. Null if no collision.
        //      hitInfo: Hit info of the collision. Null if no collision.
        //      currentPosition: position of the character
        void MovePosition(Vector3 moveVector, Vector3? collideDirection, RaycastHit? hitInfo, ref Vector3 currentPosition)
        {
            if (moveVector.sqrMagnitude.NotEqualToZero())
            {
                currentPosition += moveVector;
            }

            if (collideDirection != null && hitInfo != null)
            {
                UpdateCollisionInfo(collideDirection.Value, hitInfo.Value, currentPosition);
            }
        }

        // Update the collision flags and info.
        //      direction: The direction moved.
        //      hitInfo: The hit info of the collision.
        //      currentPosition: position of the character
        void UpdateCollisionInfo(Vector3 direction, RaycastHit? hitInfo, Vector3 currentPosition)
        {
            if (direction.x.NotEqualToZero() || direction.z.NotEqualToZero())
            {
                collisionFlags |= CollisionFlags.Sides;
            }

            if (direction.y > 0.0f)
            {
                collisionFlags |= CollisionFlags.CollidedAbove;
            }
            else if (direction.y < 0.0f)
            {
                collisionFlags |= CollisionFlags.CollidedBelow;
            }

            m_StuckInfo.hitCount++;

            if (hitInfo != null)
            {
                Collider collider = hitInfo.Value.collider;

                // We only care about the first collision with a collider
                if (!m_CollisionInfoDictionary.ContainsKey(collider))
                {
                    Vector3 moved = currentPosition - m_StartPosition;
                    CollisionInfo newCollisionInfo = new CollisionInfo(this, hitInfo.Value, direction, moved.magnitude);
                    m_CollisionInfoDictionary.Add(collider, newCollisionInfo);
                }
            }
        }

        // Stop auto-slide down steep slopes.
        void StopSlideDownSlopes()
        {
            m_SlidingDownSlopeTime = 0.0f;
        }

        // Auto-slide down steep slopes.
        void UpdateSlideDownSlopes()
        {
            float deltaTime = Time.deltaTime;
            if (!UpdateSlideDownSlopesInternal(deltaTime))
            {
                if (isSlidingDownSlope)
                {
                    m_SlidingDownSlopeTime += deltaTime;
                    m_DelayStopSlidingDownSlopeTime += deltaTime;

                    // Slight delay before we stop sliding down slopes. To handle cases where sliding test fails for a few frames.
                    if (m_DelayStopSlidingDownSlopeTime > k_StopSlideDownSlopeDelay)
                    {
                        StopSlideDownSlopes();
                    }
                }
                else
                {
                    StopSlideDownSlopes();
                }
            }
            else
            {
                m_DelayStopSlidingDownSlopeTime = 0.0f;
            }
        }

        // Auto-slide down steep slopes.
        bool UpdateSlideDownSlopesInternal(float dt)
        {
            if (!m_SlideDownSlopes || !isGrounded)
            {
                return false;
            }

            Vector3 hitNormal;

            // Collided downwards during the last slide movement?
            if (isSlidingDownSlope && m_DownCollisionNormal != null)
            {
                hitNormal = m_DownCollisionNormal.Value;
            }
            else
            {
                RaycastHit hitInfoSphere;
                if (!SmallSphereCast(Vector3.down,
                                     GetSkinWidth() + k_SlideDownSlopeTestDistance,
                                     out hitInfoSphere,
                                     Vector3.zero,
                                     true, transform.position))
                {
                    return false;
                }

                RaycastHit hitInfoRay;
                Vector3 rayOrigin = GetBottomSphereWorldPosition(transform.position);
                Vector3 rayDirection = hitInfoSphere.point - rayOrigin;

                // Raycast returns a more accurate normal than SphereCast/CapsuleCast
                if (Physics.Raycast(rayOrigin,
                                    rayDirection,
                                    out hitInfoRay,
                                    rayDirection.magnitude * k_RaycastScaleDistance,
                                    GetCollisionLayerMask(),
                                    m_TriggerQuery) &&
                    hitInfoRay.collider == hitInfoSphere.collider)
                {
                    hitNormal = hitInfoRay.normal;
                }
                else
                {
                    hitNormal = hitInfoSphere.normal;
                }
            }

            float slopeAngle = Vector3.Angle(Vector3.up, hitNormal);
            bool slopeIsSteep = slopeAngle > slopeLimit;
            if (!slopeIsSteep || slopeAngle >= k_MaxSlopeSlideAngle)
            {
                return false;
            }

            bool didSlide = true;
            m_SlidingDownSlopeTime += dt;

            // Pro tip: Here you can also use the friction of the physics material of the slope, to adjust the slide speed.

            // Speed increases as slope angle increases
            float slideSpeedScale = Mathf.Clamp01(slopeAngle / k_MaxSlopeSlideAngle);

            // Apply gravity and slide along the obstacle
            float gravity = Mathf.Abs(Physics.gravity.y) * m_SlideGravityScale * slideSpeedScale;
            float verticalVelocity = Mathf.Clamp(gravity * m_SlidingDownSlopeTime, 0.0f, Mathf.Abs(m_SlideMaxSpeed));
            Vector3 moveVector = new Vector3(0.0f, -verticalVelocity, 0.0f) * dt;

            // Push slightly away from the slope
            Vector3 push = new Vector3(hitNormal.x, 0.0f, hitNormal.z).normalized * k_PushAwayFromSlopeDistance;
            moveVector = new Vector3(push.x, moveVector.y, push.z);

            // Preserve collision flags and velocity. Because user expects them to only be set when manually calling Move/SimpleMove.
            CollisionFlags oldCollisionFlags = collisionFlags;
            Vector3 oldVelocity = m_Velocity;

            MoveInternal(moveVector, true, true, true);
            if ((collisionFlags & CollisionFlags.CollidedSides) != 0)
            {
                // Stop sliding when hit something on the side
                didSlide = false;
            }

            collisionFlags = oldCollisionFlags;
            m_Velocity = oldVelocity;

            return didSlide;
        }

        // Update pending height and center when it is safe.
        void UpdatePendingHeightAndCenter()
        {
            if (m_PendingResize.heightTime == null && m_PendingResize.centerTime == null)
            {
                return;
            }

            // Use smallest time
            float time = m_PendingResize.heightTime != null ? m_PendingResize.heightTime.Value : float.MaxValue;
            time = Mathf.Min(time, m_PendingResize.centerTime != null ? m_PendingResize.centerTime.Value : float.MaxValue);
            if (time > Time.time)
            {
                return;
            }

            m_PendingResize.ClearTimers();

            if (m_PendingResize.height != null && m_PendingResize.center != null)
            {
                SetHeightAndCenter(m_PendingResize.height.Value, m_PendingResize.center.Value, true, false);
            }
            else if (m_PendingResize.height != null)
            {
                SetHeight(m_PendingResize.height.Value, false, true, false);
            }
            else if (m_PendingResize.center != null)
            {
                SetCenter(m_PendingResize.center.Value, true, false);
            }
        }

        // Sets the playerRootTransform's localPosition to the rootTransformOffset
        void SetRootToOffset()
        {
            if (playerRootTransform != null)
            {
                playerRootTransform.localPosition = rootTransformOffset;
            }
        }
    }
}