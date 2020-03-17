using UnityEngine;

namespace CharacterController2k
{
    // A vector used by the OpenCharacterController.
    public struct MoveVector
    {
        /// <summary>
        /// The move vector.
        /// Note: This gets used up during the move loop, so will be zero by the end of the loop.
        /// </summary>
        public Vector3 moveVector { get; set; }

        /// <summary>
        /// Can the movement slide along obstacles?
        /// </summary>
        public bool canSlide { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="newMoveVector">The move vector.</param>
        /// <param name="newCanSlide">Can the movement slide along obstacles?</param>
        public MoveVector(Vector3 newMoveVector, bool newCanSlide = true)
            : this()
        {
            moveVector = newMoveVector;
            canSlide = newCanSlide;
        }
    }
}