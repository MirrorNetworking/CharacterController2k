using UnityEngine;

namespace CharacterController2k
{
    // Collision info used by the OpenCharacterController and sent to the OnOpenCharacterControllerHit message.
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

        // Gets the Collider associated with the collision
        public Collider collider { get { return m_Collider; } }

        // Gets the OpenCharacterController associated with the collision
        public OpenCharacterController controller { get { return m_Controller; } }

        // Gets the GameObject associated with the collision
        public GameObject gameObject { get { return m_GameObject; } }

        // Gets the move direction associated with the collision
        public Vector3 moveDirection { get { return m_MoveDirection; } }

        // Gets the length of the move associated with the collision
        public float moveLength { get { return m_MoveLength; } }

        // Gets the normal of the collision
        public Vector3 normal { get { return m_Normal; } }

        // Gets the point of the collision
        public Vector3 point { get { return m_Point; } }

        // Gets the Rigidbody associated with the collision
        public Rigidbody rigidbody { get { return m_Rigidbody; } }

        // Gets the Transform associated with the collision
        public Transform transform { get { return m_Transform; } }

        // Constructor
        // openCharacterController: The character controller that hit.
        // hitInfo: The hit info.
        // directionMoved: Direction moved when collision occured.
        // distanceMoved: How far the character has travelled until it hit the collider.
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