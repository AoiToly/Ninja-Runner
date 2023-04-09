using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaRunner
{

    public class MoveData
    {
        #region IPlayerController
        IPlayerController Player = null;
        public Vector3 Velocity => Player.Velocity;
        public FrameInput Input => Player.Input;
        public bool JumpingThisFrame { get { return Player.JumpingThisFrame; } }
        public bool LandingThisFrame => Player.LandingThisFrame;
        public Vector3 RawMovement => Player.RawMovement;
        public bool Grounded => Player.Grounded;
        #endregion

        public Vector3 LastPosition { get; set; }
        public float CurrentHorizontalSpeed { get; set; }
        public float CurrentVerticalSpeed { get; set; }

        // Collisions
        public float TimeLeftGrounded { get; set; }

        // Walk
        public float Acceleration { get; set; }
        public float MoveClamp { get; set; }
        public float Deacceleration { get; set; }
        public float ApexBonus { get; set; }

        // Gravity
        public float FallClamp { get; set; }
        public float MinFallSpeed { get; set; }
        public float MaxFallSpeed { get; set; }
        public float FallSpeed { get; set; }

        // Jump
        public float JumpHeight { get; set; }
        public float JumpApexThreshold { get; set; }
        public float CoyoteTimeThreshold { get; set; }
        public float JumpBuffer { get; set; }
        public float JumpEndEarlyGravityModifier { get; set; }
        public bool CoyoteUsable { get; set; }
        public bool EndedJumpEarly { get; set; } = true;
        public float ApexPoint { get; set; } // Becomes 1 at the apex of a jump
        public float LastJumpPressed { get; set; }
        public bool CanUseCoyote => CoyoteUsable && !Grounded && TimeLeftGrounded + CoyoteTimeThreshold > Time.time;
        public bool HasBufferedJump => Grounded && LastJumpPressed + JumpBuffer > Time.time;

        public void Initialize(IPlayerController player)
        {
            Player = player;
        }
    }
}