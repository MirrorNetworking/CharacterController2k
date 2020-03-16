// keep track of some meta info like class, account etc.
using UnityEngine;
using CharacterController2k;

public class PlayerAnimation : MonoBehaviour
{
    // fields for all player components to avoid costly GetComponent calls
    [Header("Components")]
    public OpenCharacterController controller;
    public PlayerMovement movement;

    [Header("Animation")]
    public float animationDirectionDampening = 0.05f;
    public float animationTurnDampening = 0.1f;
    Vector3 lastForward;

    // the player as singleton, for easier access from other scripts
    [HideInInspector] public string className = ""; // the prefab name

    void Start()
    {
        lastForward = transform.forward;
    }

    // animation ///////////////////////////////////////////////////////////////

    // Vector.Angle and Quaternion.FromToRotation and Quaternion.Angle all end
    // up clamping the .eulerAngles.y between 0 and 360, so the first overflow
    // angle from 360->0 would result in a negative value (even though we added
    // something to it), causing a rapid twitch between left and right turn
    // animations.
    //
    // the solution is to use the delta quaternion rotation.
    // when turning by 0.5, it is:
    //   0.5 when turning right (0 + angle)
    //   364.6 when turning left (360 - angle)
    // so if we assume that anything >180 is negative then that works great.
    static float AnimationDeltaUnclamped(Vector3 lastForward, Vector3 currentForward)
    {
        Quaternion rotationDelta = Quaternion.FromToRotation(lastForward, currentForward);
        float turnAngle = rotationDelta.eulerAngles.y;
        return turnAngle >= 180 ? turnAngle - 360 : turnAngle;
    }

    void LateUpdate()
    {
        // local velocity (based on rotation) for animations
        Vector3 localVelocity = transform.InverseTransformDirection(controller.velocity);

        // Turn value so that mouse-rotating the character plays some animation
        // instead of only raw rotating the model.
        float turnAngle = AnimationDeltaUnclamped(lastForward, transform.forward);
        lastForward = transform.forward;

        // apply animation parameters to all animators.
        // there might be multiple if we use skinned mesh equipment.
        foreach (Animator animator in GetComponentsInChildren<Animator>())
        {
            animator.SetBool("DEAD", false);
            animator.SetFloat("DirX", localVelocity.x, animationDirectionDampening, Time.deltaTime); // smooth idle<->run transitions
            animator.SetFloat("DirY", localVelocity.y, animationDirectionDampening, Time.deltaTime); // smooth idle<->run transitions
            animator.SetFloat("DirZ", localVelocity.z, animationDirectionDampening, Time.deltaTime); // smooth idle<->run transitions
            animator.SetFloat("LastFallY", movement.lastFall.y);
            animator.SetFloat("Turn", turnAngle, animationTurnDampening, Time.deltaTime); // smooth turn
            animator.SetBool("CROUCHING", movement.state == MoveState.CROUCHING);
            animator.SetBool("CRAWLING", movement.state == MoveState.CRAWLING);
            animator.SetBool("CLIMBING", movement.state == MoveState.CLIMBING);
            animator.SetBool("SWIMMING", movement.state == MoveState.SWIMMING);

            // smoothest way to do climbing-idle is to stop right where we were
            if (movement.state == MoveState.CLIMBING)
                animator.speed = localVelocity.y == 0 ? 0 : 1;
            else
                animator.speed = 1;

            // grounded detection works best via .state
            // -> check AIRBORNE state instead of controller.isGrounded to have some
            //    minimum fall tolerance so we don't play the AIRBORNE animation
            //    while walking down steps etc.
            animator.SetBool("OnGround", movement.state != MoveState.AIRBORNE);
            if (controller.isGrounded) animator.SetFloat("JumpLeg", movement.jumpLeg);

            // upper body layer
            animator.SetBool("UPPERBODY_HANDS", true);
        }
    }
}