using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaRunner
{

    public abstract class BaseState
    {
        public virtual void OnEnter()
        {

        }

        public virtual void OnEnd()
        {

        }

        public virtual void Update()
        {

        }

        public virtual void Pause()
        {
            OnPause();
        }

        public virtual void Resume()
        {
            OnResume();
        }

        protected virtual void OnPause()
        {

        }

        protected virtual void OnResume()
        {

        }
    }
}