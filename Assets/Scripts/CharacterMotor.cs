using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[AddComponentMenu("Character/Character Motor")]
public class CharacterMotor : MonoBehaviour
{
    public bool canControl = true;
    public float boostMults = 1;
    public float maxForwardSpeed = 5.0f;
    public float maxSidewaysSpeed = 5.0f;
    public float maxBackwardsSpeed = 5.0f;
    private bool useFixedUpdate = true;

    public AnimationCurve slopeSpeedMultiplier =
        new AnimationCurve(new Keyframe(-90, 1), new Keyframe(0, 1), new Keyframe(90, 0));

    public float maxGroundAcceleration = 30.0f;
    public float maxAirAcceleration = 20.0f;

    public float gravity = 10.0f;
    public float maxFallSpeed = 20.0f;
    
    [NonSerialized] public Vector3 inputMoveDirection	= Vector3.zero;

    [NonSerialized] public bool inputJump = false;

    class CharacterMotorMovement
    {
        [NonSerialized] public CollisionFlags collisionFlags;
        [NonSerialized] public Vector3 velocity;
        [NonSerialized] public Vector3 frameVelocity = Vector3.zero;
        [NonSerialized] public Vector3 hitPoint = Vector3.zero;
        [NonSerialized] public Vector3 lastHitPoint = new Vector3(Mathf.Infinity, 0, 0);
    }

    CharacterMotorMovement movement = new CharacterMotorMovement();

    enum MovementTransferOnJump
    {
        None,
        InitTransfer,
        PermaTransfer,
        PermaLocked
    }

    class CharacterMotorJumping
    {
        public bool enable = true;
        public float baseHeight = 1.6f;
        public float extraHeight = 1.6f;
        public float perpAmount = 2.0f;
        public float steepPerpAmount = 1.5f;

        [NonSerialized] public bool jumping = false;
        [NonSerialized] public bool holdingJumpButton = false;
        [NonSerialized] public float lastStartTime = 0.0f;
        [NonSerialized] public float lastButtonDownTime = -100f;
        [NonSerialized] public Vector3 jumpDir = Vector3.up;
        public bool enabled;
    }

    private CharacterMotorJumping jumping = new CharacterMotorJumping();

    class CharacterMotorMovingPlatform
    {
        public bool enabled = true;

        public MovementTransferOnJump movementTransfer = MovementTransferOnJump.PermaTransfer;

        [NonSerialized] public Transform hitPlatform;
        [NonSerialized] public Transform activePlatform;
        [NonSerialized] public Vector3 activeLocalPoint;
        [NonSerialized] public Vector3 activeGlobalPoint;
        [NonSerialized] public Quaternion activeLocalRotation;
        [NonSerialized] public Quaternion activeGlobalRotation;
        [NonSerialized] public Matrix4x4 lastMatrix;
        [NonSerialized] public Vector3 platformVelocity;
        [NonSerialized] public bool newPlatform;
    }

    CharacterMotorMovingPlatform movingPlatform = new CharacterMotorMovingPlatform();

    class CharacterMotorSliding
    {
        public bool enabled = true;
        public float slidingSpeed = 15f;
        public float sidewaysControl = 1.0f;
        public float speedControl = 0.4f;
    }

    CharacterMotorSliding sliding = new CharacterMotorSliding();

    [NonSerialized] public bool grounded = true;
    [NonSerialized] public Vector3 groundNormal = Vector3.zero;
    Vector3 lastGroundNormal = Vector3.zero;
    Transform tr;
    public CharacterController controller;

    void Awake()
    {
        controller = gameObject.GetComponent<CharacterController>();
        tr = transform;
    }

    private void UpdateFunction()
    {
        var velocity = movement.velocity;
        velocity = ApplyInputVelocityChange(velocity);
        velocity = ApplyGravityAndJumping(velocity);

        var moveDistance = Vector3.zero;
        if (MoveWithPlatform())
        {
            var newGlobalPoint = movingPlatform.activePlatform.TransformPoint(movingPlatform.activeLocalPoint);
            moveDistance = (newGlobalPoint - movingPlatform.activeGlobalPoint);
            if (moveDistance != Vector3.zero)
                controller.Move(moveDistance);

            var newGlobalRotation = movingPlatform.activePlatform.rotation * movingPlatform.activeLocalRotation;
            var rotationDiff = newGlobalRotation * Quaternion.Inverse(movingPlatform.activeGlobalRotation);

            var yRotation = rotationDiff.eulerAngles.y;
            if (yRotation != 0)
            {
                tr.Rotate(0, yRotation, 0);
            }
        }

        var lastPosition = tr.position;
        var currentMovementOffset = velocity * Time.deltaTime;

        var pushDownOffset = Mathf.Max(controller.stepOffset,
            new Vector3(currentMovementOffset.x, 0, currentMovementOffset.z).magnitude);
        if (grounded)
            currentMovementOffset -= pushDownOffset * Vector3.up;

        movingPlatform.hitPlatform = null;
        groundNormal = Vector3.zero;

        movement.collisionFlags = controller.Move(currentMovementOffset);

        movement.lastHitPoint = movement.hitPoint;
        lastGroundNormal = groundNormal;

        if (movingPlatform.enabled && movingPlatform.activePlatform != movingPlatform.hitPlatform)
        {
            if (movingPlatform.hitPlatform != null)
            {
                movingPlatform.activePlatform = movingPlatform.hitPlatform;
                movingPlatform.lastMatrix = movingPlatform.hitPlatform.localToWorldMatrix;
                movingPlatform.newPlatform = true;
            }
        }

        var oldHVelocity = new Vector3(velocity.x, 0, velocity.z);
        movement.velocity = (tr.position - lastPosition) / Time.deltaTime;
        var newHVelocity = new Vector3(movement.velocity.x, 0, movement.velocity.z);

        if (oldHVelocity == Vector3.zero)
        {
            movement.velocity = new Vector3(0, movement.velocity.y, 0);
        }
        else
        {
            var projectedNewVelocity = Vector3.Dot(newHVelocity, oldHVelocity) / oldHVelocity.sqrMagnitude;
            movement.velocity = oldHVelocity * Mathf.Clamp01(projectedNewVelocity) + movement.velocity.y * Vector3.up;
        }

        if (movement.velocity.y < velocity.y - 0.001)
        {
            if (movement.velocity.y < 0)
            {
                movement.velocity.y = velocity.y;
            }
            else
            {
                jumping.holdingJumpButton = false;
            }
        }

        if (grounded && !IsGroundedTest())
        {
            grounded = false;

            if (movingPlatform.enabled &&
                (movingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
                 movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)
               )
            {
                movement.frameVelocity = movingPlatform.platformVelocity;
                movement.velocity += movingPlatform.platformVelocity;
            }

            SendMessage("OnFall", SendMessageOptions.DontRequireReceiver);
            tr.position += pushDownOffset * Vector3.up;
        }
        else if (!grounded && IsGroundedTest())
        {
            grounded = true;
            jumping.jumping = false;
            SubtractNewPlatformVelocity();

            SendMessage("OnLand", SendMessageOptions.DontRequireReceiver);
        }

        if (MoveWithPlatform())
        {
            movingPlatform.activeGlobalPoint = tr.position +
                                               Vector3.up * (controller.center.y - (controller.height * 0.5f) +
                                                             controller.radius);
            movingPlatform.activeLocalPoint =
                movingPlatform.activePlatform.InverseTransformPoint(movingPlatform.activeGlobalPoint);

            movingPlatform.activeGlobalRotation = tr.rotation;
            movingPlatform.activeLocalRotation = Quaternion.Inverse(movingPlatform.activePlatform.rotation) *
                                                 movingPlatform.activeGlobalRotation;
        }
    }

    void FixedUpdate()
    {
        if (movingPlatform.enabled)
        {
            if (movingPlatform.activePlatform != null)
            {
                if (!movingPlatform.newPlatform)
                {
                    movingPlatform.platformVelocity = (
                        movingPlatform.activePlatform.localToWorldMatrix.MultiplyPoint3x4(movingPlatform
                            .activeLocalPoint)
                        - movingPlatform.lastMatrix.MultiplyPoint3x4(movingPlatform.activeLocalPoint)
                    ) / Time.deltaTime;
                }

                movingPlatform.lastMatrix = movingPlatform.activePlatform.localToWorldMatrix;
                movingPlatform.newPlatform = false;
            }
            else
            {
                movingPlatform.platformVelocity = Vector3.zero;
            }
        }

        if (useFixedUpdate)
            UpdateFunction();
    }

    void Update()
    {
        if (!useFixedUpdate)
            UpdateFunction();
    }

    Vector3 ApplyInputVelocityChange(Vector3 velocity)
    {
        if (!canControl)
            inputMoveDirection = Vector3.zero;

        Vector3 desiredVelocity;
        if (grounded && TooSteep())
        {
            desiredVelocity = new Vector3(groundNormal.x, 0, groundNormal.z).normalized;
            var projectedMoveDir = Vector3.Project(inputMoveDirection, desiredVelocity);
            desiredVelocity = desiredVelocity + projectedMoveDir * sliding.speedControl +
                              (inputMoveDirection - projectedMoveDir) * sliding.sidewaysControl;
            desiredVelocity *= sliding.slidingSpeed;
        }
        else
            desiredVelocity = GetDesiredHorizontalVelocity();

        if (movingPlatform.enabled && movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)
        {
            desiredVelocity += movement.frameVelocity;
            desiredVelocity.y = 0;
        }

        if (grounded)
            desiredVelocity = AdjustGroundVelocityToNormal(desiredVelocity, groundNormal);
        else
            velocity.y = 0;

        var maxVelocityChange = GetMaxAcceleration(grounded) * Time.deltaTime;
        var velocityChangeVector = (desiredVelocity - velocity);
        if (velocityChangeVector.sqrMagnitude > maxVelocityChange * maxVelocityChange)
        {
            velocityChangeVector = velocityChangeVector.normalized * maxVelocityChange;
        }

        if (grounded || canControl)
            velocity += velocityChangeVector;

        if (grounded)
        {
            velocity.y = Mathf.Min(velocity.y, 0);
        }

        return velocity;
    }

    Vector3 ApplyGravityAndJumping(Vector3 velocity)
    {
        if (!inputJump || !canControl)
        {
            jumping.holdingJumpButton = false;
            jumping.lastButtonDownTime = -100;
        }

        if (inputJump && jumping.lastButtonDownTime < 0 && canControl)
            jumping.lastButtonDownTime = Time.time;

        if (grounded)
            velocity.y = Mathf.Min(0, velocity.y) - gravity * Time.deltaTime;
        else
        {
            velocity.y = movement.velocity.y - gravity * Time.deltaTime * 2;

            if (jumping.jumping && jumping.holdingJumpButton)
            {
                if (Time.time < jumping.lastStartTime +
                    jumping.extraHeight / CalculateJumpVerticalSpeed(jumping.baseHeight))
                {
                    velocity += jumping.jumpDir * gravity * Time.deltaTime;
                }
            }

            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        }

        if (grounded)
        {
            if (jumping.enabled && canControl && (Time.time - jumping.lastButtonDownTime < 0.2))
            {
                grounded = false;
                jumping.jumping = true;
                jumping.lastStartTime = Time.time;
                jumping.lastButtonDownTime = -100;
                jumping.holdingJumpButton = true;

                if (TooSteep())
                    jumping.jumpDir = Vector3.Slerp(Vector3.up, groundNormal, jumping.steepPerpAmount);
                else
                    jumping.jumpDir = Vector3.Slerp(Vector3.up, groundNormal, jumping.perpAmount);
                velocity.y = 0;
                velocity += jumping.jumpDir * CalculateJumpVerticalSpeed(jumping.baseHeight);

                if (movingPlatform.enabled &&
                    (movingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
                     movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)
                   )
                {
                    movement.frameVelocity = movingPlatform.platformVelocity;
                    velocity += movingPlatform.platformVelocity;
                }

                SendMessage("OnJump", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                jumping.holdingJumpButton = false;
            }
        }

        return velocity;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0 && hit.normal.y > groundNormal.y && hit.moveDirection.y < 0)
        {
            if ((hit.point - movement.lastHitPoint).sqrMagnitude > 0.001 || lastGroundNormal == Vector3.zero)
                groundNormal = hit.normal;
            else
                groundNormal = lastGroundNormal;

            movingPlatform.hitPlatform = hit.collider.transform;
            movement.hitPoint = hit.point;
            movement.frameVelocity = Vector3.zero;
        }
    }
    
    private IEnumerable SubtractNewPlatformVelocity () {
        if (movingPlatform.enabled &&
            (movingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
             movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)
           ) {
            if (movingPlatform.newPlatform) {
                var platform	= movingPlatform.activePlatform;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                if (grounded && platform == movingPlatform.activePlatform)
                    yield return 1;
            }
            movement.velocity -= movingPlatform.platformVelocity;
        }
    }
    
    private bool MoveWithPlatform()
    {
        return movingPlatform.enabled
               && (grounded || movingPlatform.movementTransfer == MovementTransferOnJump.PermaLocked)
               && movingPlatform.activePlatform != null;
    }

    private Vector3 GetDesiredHorizontalVelocity()
    {
        var desiredLocalDirection = tr.InverseTransformDirection(inputMoveDirection);
        var maxSpeed = MaxSpeedInDirection(desiredLocalDirection);
        if (grounded)
        {
            var movementSlopeAngle = Mathf.Asin(movement.velocity.normalized.y) * Mathf.Rad2Deg;
            maxSpeed *= slopeSpeedMultiplier.Evaluate(movementSlopeAngle);
        }

        return tr.TransformDirection(desiredLocalDirection * maxSpeed);
    }

    private Vector3 AdjustGroundVelocityToNormal(Vector3 hVelocity, Vector3 groundNormal)
    {
        var sideways = Vector3.Cross(Vector3.up, hVelocity);
        return Vector3.Cross(sideways, groundNormal).normalized * hVelocity.magnitude;
    }
    
    private bool IsGroundedTest()
    {
        return groundNormal.y > 0.01;
    }
    
    float GetMaxAcceleration(bool grounded)
    {
        return grounded ? maxGroundAcceleration : maxAirAcceleration;
    }
    
    float CalculateJumpVerticalSpeed (float targetJumpHeight)
    {
        // From the jump height and gravity we deduce the upwards speed 
        // for the character to reach at the apex.
        return Mathf.Sqrt(2 * targetJumpHeight * gravity);
    }

    bool IsJumping()
    {
        return jumping.jumping;
    }

    bool IsSliding()
    {
        return (grounded && sliding.enabled && TooSteep());
    }

    bool IsTouchingCeiling()
    {
        return (movement.collisionFlags & CollisionFlags.CollidedAbove) != 0;
    }

    bool IsGrounded()
    {
        return grounded;
    }

    bool TooSteep()
    {
        return (groundNormal.y <= Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad));
    }

    Vector3 GetDirection()
    {
        return inputMoveDirection;
    }

    void SetControllable(bool controllable)
    {
        canControl = controllable;
    }

    float MaxSpeedInDirection(Vector3 desiredMovementDirection)
    {
        if (desiredMovementDirection == Vector3.zero)
            return 0;
        else
        {
            var zAxisEllipseMultiplier =
                (desiredMovementDirection.z > 0 ? maxForwardSpeed * boostMults : maxBackwardsSpeed * boostMults) /
                maxSidewaysSpeed * boostMults;
            var temp = new Vector3(desiredMovementDirection.x, 0, desiredMovementDirection.z / zAxisEllipseMultiplier)
                .normalized;
            var length = new Vector3(temp.x, 0, temp.z * zAxisEllipseMultiplier).magnitude * maxSidewaysSpeed *
                         boostMults;
            return length;
        }
    }

    void SetVelocity(Vector3 velocity)
    {
        grounded = false;
        movement.velocity = velocity;
        movement.frameVelocity = Vector3.zero;
        SendMessage("OnExternalVelocity");
    }
}