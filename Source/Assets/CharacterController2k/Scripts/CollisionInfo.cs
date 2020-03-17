using UnityEngine;

namespace CharacterController2k
{
    /// <summary>
    /// Collision info used by the OpenCharacterController and sent to the OnOpenCharacterControllerHit message.
    /// </summary>
    public struct CollisionInfo
    {
        // The collider that was hit by the controller.
        readonly Collider m_Collider;

        // The controller that hit the collider.
        readonly OpenCharacterController m_Controller;

        // The game object that was hit by the controller.
        readonly GameObject m_GameObject;

        // The direction the character Controller was moving in when the collision occured.
        readonly Vector3 m_MoveDirection;

        // How far the character has travelled until it hit the collider.
        readonly float m_MoveLength;

        // The normal of the surface we collided with in world space.
        readonly Vector3 m_Normal;

        // The impact point in world space.
        readonly Vector3 m_Point;

        // The rigidbody that was hit by the controller.
        readonly Rigidbody m_Rigidbody;

        // The transform that was hit by the controller.
        readonly Transform m_Transform;

        /// <summary>
        /// Gets the <see cref="Collider"/> associated with the collision
        /// </summary>
        public Collider collider { get { return m_Collider; } }

        /// <summary>
        /// Gets the <see cref="OpenCharacterController"/> associated with the collision
        /// </summary>
        public OpenCharacterController controller { get { return m_Controller; } }

        /// <summary>
        /// Gets the <see cref="GameObject"/> associated with the collision
        /// </summary>
        public GameObject gameObject { get { return m_GameObject; } }

        /// <summary>
        /// Gets the move direction associated with the collision
        /// </summary>
        public Vector3 moveDirection { get { return m_MoveDirection; } }

        /// <summary>
        /// Gets the length of the move associated with the collision
        /// </summary>
        public float moveLength { get { return m_MoveLength; } }

        /// <summary>
        /// Gets the normal of the collision
        /// </summary>
        public Vector3 normal { get { return m_Normal; } }

        /// <summary>
        /// Gets the point of the collision
        /// </summary>
        public Vector3 point { get { return m_Point; } }

        /// <summary>
        /// Gets the <see cref="Rigidbody"/> associated with the collision
        /// </summary>
        public Rigidbody rigidbody { get { return m_Rigidbody; } }

        /// <summary>
        /// Gets the <see cref="Transform"/> associated with the collision
        /// </summary>
        public Transform transform { get { return m_Transform; } }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="openCharacterController">The character controller that hit.</param>
        /// <param name="hitInfo">The hit info.</param>
        /// <param name="directionMoved">Direction moved when collision occured.</param>
        /// <param name="distanceMoved">How far the character has travelled until it hit the collider.</param>
        public CollisionInfo(OpenCharacterController openCharacterController,
                             RaycastHit hitInfo,
                             Vector3 directionMoved,
                             float distanceMoved)
        {
            m_Collider = hitInfo.collider;
            m_Controller = openCharacterController;
            m_GameObject = hitInfo.collider.gameObject;
            m_MoveDirection = directionMoved;
            m_MoveLength = distanceMoved;
            m_Normal = hitInfo.normal;
            m_Point = hitInfo.point;
            m_Rigidbody = hitInfo.rigidbody;
            m_Transform = hitInfo.transform;
        }
    }
}