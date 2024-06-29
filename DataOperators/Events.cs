using Azure.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Azure.UI;
using JetBrains.Annotations;

namespace Azure.BaseFramework
{
    public class Events : MonoBehaviour
    {
        public static event Action<string> LocationUpdate = delegate { };
             public static void OnLocation(string data)
        {
            LocationUpdate(data);
        }
     }
}
