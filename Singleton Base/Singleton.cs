using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Azure.BaseFramework
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        private void Awake()
        {
            if(Instance==null)
            {

            Instance = this as T;
            OnAwake();
            }
        }

        public virtual void OnAwake()
        {

        }
    }
}