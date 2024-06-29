using UnityEngine;

namespace Azure.BaseFramework
{
    public class IndestructibleSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                //Debug.Log("do not destroy call");

                Instance = this as T;
                DontDestroyOnLoad(this);
                OnAwake();
            }
            else
            {
                //Debug.Log("destroy call");
                Destroy(this.gameObject);
            }
        }

        public virtual void OnAwake()
        {

        }
    }
}