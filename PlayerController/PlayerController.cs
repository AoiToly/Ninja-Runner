using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaRunner
{
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        public Vector3 Velocity { get; private set; }
        public FrameInput Input { get; private set; }
        public bool JumpingThisFrame { get; private set; }
        public bool LandingThisFrame { get; private set; }
        public Vector3 RawMovement { get; private set; }
        public bool Grounded => colDown;

        private MoveData moveData;
        private MoveFSM moveFSM;
        private bool isEnabled = false;

        private void Start()
        {
            
        }

        private void Update()
        {
            if(UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                Initialize();
                EnableMove();
            }
            if (!isEnabled) return;

            // Calculate velocity
            Velocity = (transform.position - moveData.LastPosition) / Time.deltaTime;
            moveData.LastPosition = transform.position;

            GatherInput();
            RunCollisionChecks();

            moveFSM.Update();
            JumpJudgement();
            HitWallAdjustment();
            MoveCharacter();
        }

        #region public functions

        public void Initialize()
        {
            isEnabled = false;
            moveData = new MoveData();
            moveData.Initialize(this);
            moveFSM = new MoveFSM();
            moveFSM.FSMInitialization(moveData);
            // Collisions
            moveData.TimeLeftGrounded = timeLeftGrounded;
            // Walk
            moveData.Acceleration = acceleration;
            moveData.MoveClamp = moveClamp;
            moveData.Deacceleration = deacceleration;
            moveData.ApexBonus = apexBonus;
            // Gravity
            moveData.FallClamp = fallClamp;
            moveData.MinFallSpeed = minFallSpeed;
            moveData.MaxFallSpeed = maxFallSpeed;
            // Jump
            moveData.JumpHeight = jumpHeight;
            moveData.JumpApexThreshold = jumpApexThreshold;
            moveData.CoyoteTimeThreshold = coyoteTimeThreshold;
            moveData.JumpBuffer = jumpBuffer;
            moveData.JumpEndEarlyGravityModifier = jumpEndEarlyGravityModifier;
        }

        public void EnableMove()
        {
            isEnabled = true;
            moveFSM.EnableMove();
        }

        public void Pause()
        {
            isEnabled = false;
            moveFSM.Pause();
        }

        public void Resume()
        {
            isEnabled = true;
            moveFSM.Resume();
        }

        public void Clear()
        {
            Pause();
            Initialize();
        }

        #endregion

        #region Gather Input

        private void GatherInput()
        {
            Input = new FrameInput
            {
                JumpDown = UnityEngine.Input.GetButtonDown("Jump"),
                JumpUp = UnityEngine.Input.GetButtonUp("Jump"),
                X = UnityEngine.Input.GetAxisRaw("Horizontal"),
                Y = UnityEngine.Input.GetAxisRaw("Vertical")
            };
            if (Input.JumpDown)
            {
                moveData.LastJumpPressed = Time.time;
            }
        }

        #endregion

        #region Collisions

        [Header("COLLISION")] [SerializeField] private Bounds characterBounds;
        [SerializeField] private LayerMask groundLayer;
        /// <summary>
        /// the count of the rays per side
        /// </summary>
        [SerializeField] private int detectorCount = 3;
        [SerializeField] private float detectionRayLength = 0.1f;
        [SerializeField] [Range(0.1f, 0.3f)] private float rayBuffer = 0.1f; // Prevents side detectors hitting the ground

        private RayRange raysUp, raysRight, raysDown, raysLeft;
        private bool colUp, colRight, colDown, colLeft;

        private float timeLeftGrounded;

        // We use these raycast checks for pre-collision information
        private void RunCollisionChecks()
        {
            // Generate ray ranges. 
            CalculateRayRanged();

            // Ground
            LandingThisFrame = false;
            var groundedCheck = RunDetection(raysDown);
            if (colDown && !groundedCheck) timeLeftGrounded = Time.time; // Only trigger when first leaving
            else if (!colDown && groundedCheck)
            {
                moveData.CoyoteUsable = true; // Only trigger when first touching
                LandingThisFrame = true;
            }

            colDown = groundedCheck;

            // The rest
            colUp = RunDetection(raysUp);
            colLeft = RunDetection(raysLeft);
            colRight = RunDetection(raysRight);

            bool RunDetection(RayRange range)
            {
                return EvaluateRayPositions(range).Any(point => Physics2D.Raycast(point, range.Dir, detectionRayLength, groundLayer));
            }
        }

        private void CalculateRayRanged()
        {
            // This is crying out for some kind of refactor. 
            var b = new Bounds(transform.position + characterBounds.center, characterBounds.size);

            raysDown = new RayRange(b.min.x + rayBuffer, b.min.y, b.max.x - rayBuffer, b.min.y, Vector2.down);
            raysUp = new RayRange(b.min.x + rayBuffer, b.max.y, b.max.x - rayBuffer, b.max.y, Vector2.up);
            raysLeft = new RayRange(b.min.x, b.min.y + rayBuffer, b.min.x, b.max.y - rayBuffer, Vector2.left);
            raysRight = new RayRange(b.max.x, b.min.y + rayBuffer, b.max.x, b.max.y - rayBuffer, Vector2.right);
        }


        private IEnumerable<Vector2> EvaluateRayPositions(RayRange range)
        {
            for (var i = 0; i < detectorCount; i++)
            {
                var t = (float)i / (detectorCount - 1);
                yield return Vector2.Lerp(range.Start, range.End, t);
            }
        }

        private void OnDrawGizmos()
        {
            // Bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + characterBounds.center, characterBounds.size);

            // Rays
            if (!Application.isPlaying)
            {
                CalculateRayRanged();
                Gizmos.color = Color.blue;
                foreach (var range in new List<RayRange> { raysUp, raysRight, raysDown, raysLeft })
                {
                    foreach (var point in EvaluateRayPositions(range))
                    {
                        Gizmos.DrawRay(point, range.Dir * detectionRayLength);
                    }
                }
            }

            if (!Application.isPlaying) return;

            // Draw the future position. Handy for visualizing gravity
            Gizmos.color = Color.red;
            var move = new Vector3(moveData.CurrentHorizontalSpeed, moveData.CurrentVerticalSpeed) * Time.deltaTime;
            Gizmos.DrawWireCube(transform.position + characterBounds.center + move, characterBounds.size);
        }

        #endregion

        #region Walk

        [Header("WALKING")]
        [SerializeField] private float acceleration = 90;
        [SerializeField] private float moveClamp = 13;
        [SerializeField] private float deacceleration = 60f;
        [SerializeField] private float apexBonus = 2;

        #endregion

        #region Gravity

        [Header("GRAVITY")]
        [SerializeField] private float fallClamp = -40f;
        [SerializeField] private float minFallSpeed = 80f;
        [SerializeField] private float maxFallSpeed = 120f;
        private float _fallSpeed;

        #endregion

        #region Jump

        [Header("JUMPING")]
        [SerializeField] private float jumpHeight = 30;
        [SerializeField] private float jumpApexThreshold = 10f;
        [SerializeField] private float coyoteTimeThreshold = 0.1f;
        [SerializeField] private float jumpBuffer = 0.1f;
        [SerializeField] private float jumpEndEarlyGravityModifier = 3;

        private void JumpJudgement()
        {
            // Jump if: grounded or within coyote threshold || sufficient jump buffer
            if (Input.JumpDown && moveData.CanUseCoyote || moveData.HasBufferedJump)
            {
                JumpingThisFrame = true;
            }
            else
            {
                JumpingThisFrame = false;
            }
        }

        #endregion

        #region Move

        private void HitWallAdjustment()
        {
            if (moveData.CurrentHorizontalSpeed > 0 && colRight || moveData.CurrentHorizontalSpeed < 0 && colLeft)
            {
                // Don't walk through walls
                moveData.CurrentHorizontalSpeed = 0;
            }
            if (colDown && moveData.CurrentVerticalSpeed < 0)
            {
                // Move out of the ground
                moveData.CurrentVerticalSpeed = 0;
            }
            if (colUp && moveData.CurrentVerticalSpeed > 0)
            {
                moveData.CurrentVerticalSpeed = 0;
            }
        }

        [Header("MOVE")]
        [SerializeField, Tooltip("Raising this value increases collision accuracy at the cost of performance.")]
        private int _freeColliderIterations = 10;

        // We cast our bounds before moving to avoid future collisions
        private void MoveCharacter()
        {
            var pos = transform.position + characterBounds.center;
            RawMovement = new Vector3(moveData.CurrentHorizontalSpeed, moveData.CurrentVerticalSpeed); // Used externally
            var move = RawMovement * Time.deltaTime;
            var furthestPoint = pos + move;

            // check furthest movement. If nothing hit, move and don't do extra checks
            var hit = Physics2D.OverlapBox(furthestPoint, characterBounds.size, 0, groundLayer);
            if (!hit)
            {
                transform.position += move;
                return;
            }

            // otherwise increment away from current pos; see what closest position we can move to
            var positionToMoveTo = transform.position;
            for (int i = 1; i < _freeColliderIterations; i++)
            {
                // increment to check all but furthestPoint - we did that already
                var t = (float)i / _freeColliderIterations;
                var posToTry = Vector2.Lerp(pos, furthestPoint, t);

                if (Physics2D.OverlapBox(posToTry, characterBounds.size, 0, groundLayer))
                {
                    transform.position = positionToMoveTo;

                    // We've landed on a corner or hit our head on a ledge. Nudge the player gently
                    if (i == 1)
                    {
                        if (moveData.CurrentVerticalSpeed < 0) moveData.CurrentVerticalSpeed = 0;
                        var dir = transform.position - hit.transform.position;
                        transform.position += dir.normalized * move.magnitude;
                    }

                    return;
                }

                positionToMoveTo = posToTry;
            }
        }

        #endregion
    }
}