using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaRunner
{

    public enum PlayerState
    {
        Ground,
        Jump,
        Fall,
        None
    }

    public class MoveFSM
    {
        bool isEnabled = false;
        MoveState preState = null;
        Dictionary<PlayerState, MoveState> stateDic = null;
        MoveData moveData = null;

        public void FSMInitialization(MoveData data)
        {
            stateDic = new Dictionary<PlayerState, MoveState>();
            stateDic.Add(PlayerState.Ground, new WalkState(data));
            stateDic.Add(PlayerState.Jump, new JumpState(data));
            stateDic.Add(PlayerState.Fall, new FallState(data));
            moveData = data;
            preState = stateDic[PlayerState.Ground];
            isEnabled = false;
        }

        public void Update()
        {
            if (isEnabled && preState != null)
            {
                PlayerState targetStateType = preState.Reason();
                if (targetStateType != PlayerState.None)
                {
                    SwitchStatus(targetStateType);
                }
                preState.Update();
            }
        }

        public void SwitchStatus(PlayerState mst)
        {
            preState.OnEnd();
            preState = stateDic[mst];
            preState.OnEnter();
        }

        public void EnableMove()
        {
            isEnabled = true;
        }

        public void Pause()
        {
            isEnabled = false;
            preState.Pause();
        }

        public void Resume()
        {
            isEnabled = true;
            preState.Resume();
        }
    }
}