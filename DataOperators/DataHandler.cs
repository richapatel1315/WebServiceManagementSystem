using Azure.Data;
using Azure.UI;
using BestHTTP.JSON;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI;
using static ZXing.QrCode.Internal.Mode;

namespace Azure.BaseFramework
{
    public class DataHandler : Singleton<DataHandler>
    {
      
        #region Classes&Variables

        public AllLocationData location;
        #endregion
        #region Unity_Methods
   
        #endregion
        #region Request_Methods
        public void GetByAllLocation()
        {
            LoaderController.Instance.showLoader();
            Services.Get(ServicesData.API_getAllLocationByCompany + "?CompanyID=" + SavedDataHandler.Instance._saveData.companyId, callbackLocation, true, false, false);
        }
           #endregion
        #region callbacks
        void callbackLocation(string data)
        {
            location = JsonUtility.FromJson<AllLocationData>("{\"locations\":" + data + "}");
            UIController.Instance.ShowNextScreen(ScreenType.Home, .2f);
            UIController.Instance.HideScreen(ScreenType.Auth);
            Events.OnLocation(data);

            LoaderController.Instance.HideLoader();
        }
        #endregion
    }
}