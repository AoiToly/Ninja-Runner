using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaRunner
{
    // to be added: coyote
    public abstract class MoveState : BaseState
    {
        protected MoveData moveData;
        public MoveState(MoveData data)
        {
            moveData = data;
        }
        public abstract PlayerState Reason();

        protected void CalculateWalk()
        {
            if (moveData.Input.X != 0)
            {
                // Set horizontal move speed
                moveData.CurrentHorizontalSpeed += moveData.Input.X * moveData.Acceleration * Time.deltaTime;

                // clamped by max frame movement
                moveData.CurrentHorizontalSpeed = Mathf.Clamp(moveData.CurrentHorizontalSpeed, -moveData.MoveClamp, moveData.MoveClamp);

                // Apply bonus at the apex of a jump
                var apexBonus = Mathf.Sign(moveData.Input.X) * moveData.ApexBonus * moveData.ApexPoint;
                moveData.CurrentHorizontalSpeed += apexBonus * Time.deltaTime;
            }
            else
            {
                // No input. Let's slow the character down
                moveData.CurrentHorizontalSpeed = Mathf.MoveTowards(moveData.CurrentHorizontalSpeed, 0, moveData.Deacceleration * Time.deltaTime);
            }
        }

        protected void CalculateGravity()
        {
            if (!moveData.Grounded)
            {
                // Add downward force while ascending if we ended the jump early
                var fallSpeed = moveData.EndedJumpEarly && moveData.CurrentVerticalSpeed > 0 ? moveData.FallSpeed * moveData.JumpEndEarlyGravityModifier : moveData.FallSpeed;

                // Fall
                moveData.CurrentVerticalSpeed -= fallSpeed * Time.deltaTime;

                // Clamp
                if (moveData.CurrentVerticalSpeed < moveData.FallClamp) moveData.CurrentVerticalSpeed = moveData.FallClamp;
            }
        }

        protected void CalculateJumpApex()
        {
            if (!moveData.Grounded)
            {
                // Gets stronger the closer to the top of the jump
                moveData.ApexPoint = Mathf.InverseLerp(moveData.JumpApexThreshold, 0, Mathf.Abs(moveData.Velocity.y));
                moveData.FallSpeed = Mathf.Lerp(moveData.MinFallSpeed, moveData.MaxFallSpeed, moveData.ApexPoint);
            }
            else
            {
                moveData.ApexPoint = 0;
            }
        }

        private void CalculateJump()
        {
            // Jump if: grounded or within coyote threshold || sufficient jump buffer
            if (moveData.Input.JumpDown && moveData.CanUseCoyote || moveData.HasBufferedJump)
            {
                moveData.CurrentVerticalSpeed = moveData.JumpHeight;
                moveData.EndedJumpEarly = false;
                moveData.CoyoteUsable = false;
                moveData.TimeLeftGrounded = float.MinValue;

                // End the jump early if button released
                if (!moveData.Grounded && moveData.Input.JumpUp && !moveData.EndedJumpEarly && moveData.Velocity.y > 0)
                {
                    // _currentVerticalSpeed = 0;
                    moveData.EndedJumpEarly = true;
                }
            }
        }
    }

    public class WalkState : MoveState
    {
        public WalkState(MoveData data) : base(data) { }

        public override void Update()
        {
            CalculateWalk();
        }

        public override PlayerState Reason()
        {
            if (moveData.Input.JumpDown && moveData.CanUseCoyote || moveData.HasBufferedJump)
            {
                return PlayerState.Jump;
            }
            if(!moveData.Grounded)
            {
                return PlayerState.Fall;
            }
            return PlayerState.None;
        }
    }

    public class JumpState : MoveState
    {
        public JumpState(MoveData data) : base(data) { }

        public override void Update()
        {
            CalculateWalk();
            CalculateJumpApex();
            CalculateGravity();
            
        }

        public override void OnEnter()
        {
            moveData.CurrentVerticalSpeed = moveData.JumpHeight;
            moveData.EndedJumpEarly = false;
            moveData.CoyoteUsable = false;
            moveData.TimeLeftGrounded = float.MinValue;
        }

        public override PlayerState Reason()
        {
            // End the jump early if button released
            if (!moveData.Grounded && moveData.Input.JumpUp && !moveData.EndedJumpEarly && moveData.Velocity.y > 0)
            {
                // _currentVerticalSpeed = 0;
                moveData.EndedJumpEarly = true;
                return PlayerState.Fall;
            }
            if (!moveData.Grounded && moveData.Velocity.y < 0)
            {
                return PlayerState.Fall;
            }
            return PlayerState.None;
        }
    }

    public class FallState : MoveState
    {
        public FallState(MoveData data) : base(data) { }

        public override void Update()
        {
            CalculateWalk();
            CalculateJumpApex();
            CalculateGravity();
            
        }

        public override void OnEnter()
        {

        }

        public override PlayerState Reason()
        {
            // End the jump early if button released
            if (moveData.Grounded)
            {
                return PlayerState.Ground;
            }
            return PlayerState.None;
        }
    }
}