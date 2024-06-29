using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Azure.BaseFramework
{
    public static class ServicesData
    {

        public const string baseSocketURL = "<URL>";
        public const string localBaseSocketURL = "localhost";
        public const string localSocketPortNum = "8000";

        #region API URLS

        // BASE URL SETUP
        public static bool isLive = false;
        public static string baseAPIURL = "http://192.192.44.4/WCMS/";
        public const string localAPIURL = "localhost";
        public const string localAPIPortNum = "8456";

        public const string APIPostFixURL = "/docs/v1/";
        public const string API_authentication = "api/Auth/Token";
        //    public const string API_getAllLocationByCompany = "api/LocationMaster/GetAllLocationByCompany";
      
        static string finalBaseURL = "";
        public static string authentication = "";

        //MOVE THESE SOMWHERE ELSE!
        static string apiToken = "";
        public static bool isLoggedIn;
        public static string ImagesDirectoryPath = "Images";
        public static string VideosDirectoryPath = "Videos";

        public static int ProductHoldTimer = 2;
        public static string APIAuthToken
        {
            get
            {
                if (!string.IsNullOrEmpty(apiToken)) return apiToken;
                else return apiToken;
            }
            set { apiToken = value; }
        }

        public static void SetupBaseURL()
        {
            finalBaseURL = (isLive ? baseAPIURL : localAPIURL + ":" + localAPIPortNum);// +
            ///APIPostFixURL;
            Debug.Log("URL : " + finalBaseURL);
        }
        public static string FinalizeURL(this string m_url)
        {
            return finalBaseURL + m_url;
        }
        #endregion
    }

    //Enum for All the APIs
    public enum API_TYPE
    {
        None,
        API_authentication,
        API_getAllLocationByCompany
       
    }

    #region HelperFunctions

    public static class Helper
    {
        public static Texture2D textureFromSprite(Sprite sprite)
        {
            if (sprite.rect.width != sprite.texture.width)
            {
                Texture2D newText = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
                Color[] newColors = sprite.texture.GetPixels((int)sprite.textureRect.x,
                                                             (int)sprite.textureRect.y,
                                                             (int)sprite.textureRect.width,
                                                             (int)sprite.textureRect.height);
                newText.SetPixels(newColors);
                newText.Apply();
                return newText;
            }
            else
                return sprite.texture;
        }

    }

    public static class ColorExtensions
    {
        /// <summary>
        /// Convert string to Color (if defined as a static property of Color)
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color ToColor(this string color)
        {
            Color c = Color.white;

            ColorUtility.TryParseHtmlString("#" + color, out c); ;

            return c;
        }
    }

    #endregion

    #region Serializable Class

    [Serializable]
    public class EventListResponse<T> where T : class
    {
        public EventListResponse()
        {
            Data = new List<T>();
        }

        public int StatusCode;
        public List<T> Data;
        public object ErrorMessage;
    }

    [Serializable]
    public class AuthData
    {
        public string access_token;
        public string token_type;
        public int expires_in;
        public string refresh_token;
        public string userName;
        public string companyId;
    }

    [Serializable]
    public class AllLocationData
    {
        public List<Location> locations;
        [Serializable]
        public class Location
        {
            public int Id;
            public string LocationName;
            public string Address;
            public int CompanyId;
        }
    }
    #endregion
}
public enum BundleType
{
    BaseImage,
    SmallImage,
    ThumbnailImage,
    Video,
    Document,
    Bundle,
    Manifest
}
public enum SceneName
{
    main
}
public enum SceneNumber
{
    Sneakers = 12
}