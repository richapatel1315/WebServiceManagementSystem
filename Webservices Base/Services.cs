using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Data;
using Azure.UI;
using Newtonsoft.Json;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Azure.BaseFramework
{
    public class KVPList<TKey, TValue> : List<KeyValuePair<TKey, TValue>>
    {
        public void Add(TKey Key, TValue Value)
        {
            base.Add(new KeyValuePair<TKey, TValue>(Key, Value));
        }
    }

    public class APIRequest
    {
        public METHOD RequestMethod;
        public string URL;
        public KVPList<string, string> data;

        public KVPList<string, byte[]> rawdata;
        public Action<string> OnServiceCallBack;
        public bool shouldAuthorize = true;
        public bool withTimeOut = false;

        public APIRequest(METHOD requestMethod, string requestURL, Action<string> requestCallBack, KVPList<string, string> requestData = null, KVPList<string, byte[]> requestRawdata = null, bool shouldRequestAuthorize = true, bool requestwithTimeOut = false)
        {
            RequestMethod = requestMethod;
            URL = requestURL;
            OnServiceCallBack = requestCallBack;
            data = requestData;
            rawdata = requestRawdata;
            shouldAuthorize = shouldRequestAuthorize;
            withTimeOut = requestwithTimeOut;
        }

    }

    public class RequestScheduler
    {

        List<APIRequest> RequestQueue;
        public bool isRequestActive;
        bool ActiveScheduler;

        public RequestScheduler()
        {
            RequestQueue = new List<APIRequest>();

            ActiveScheduler = true;
            RequestsHandler();
        }

        public void KillRequestSchedular()
        {
            ActiveScheduler = false;
        }

        public bool IsRequeastPresentInQueue(string URL)
        {

            foreach (var req in RequestQueue)
            {
                if (req.URL.Contains(URL))
                {
                    return true;
                }
            }

            return false;
        }

        public async void RequestsHandler()
        {
            do
            {

                if (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    if (RequestQueue.Count > 0 && !isRequestActive)
                    {
                        SendRequest(RequestQueue[0]);
                    }
                }


                //Debug.Log("Scheduler Active");
                await Task.Delay(10);

            } while (ActiveScheduler);
        }

        public void SendRequest(APIRequest request)
        {
            //Debug.Log("<color='magenta'>Sending Request " + request.URL + "</color>");


            isRequestActive = true;

            switch (request.RequestMethod)
            {
                case METHOD.GET:

                    Services.Get(request.URL, (response) =>
                    {

                        RequestQueue.RemoveAt(0);
                        isRequestActive = false;

                        //Debug.Log("<color='magenta'>Completed Request : " + request.URL + "</color>");

                        if (Services.IsValidJson(response))
                        {
                            request.OnServiceCallBack(response);
                        }
                        else
                        {
                            AddRequest(request);
                        }
                    }, request.shouldAuthorize, request.withTimeOut, false, true);


                    break;
                case METHOD.POST:
                    {
                        Services.Post(request.URL, request.data, (response) =>
                        {
                            RequestQueue.RemoveAt(0);
                            isRequestActive = false;

                            //Debug.Log("<color='magenta'>Completed Request : " + request.URL + "</color>");

                            if (Services.IsValidJson(response))
                            {
                                request.OnServiceCallBack(response);
                            }
                            else
                            {
                                AddRequest(request);
                            }

                        }, request.shouldAuthorize, false, true);

                        break;
                    }
                    //case METHOD.POST_RAW:
                    //    break;
                    //case METHOD.POST_MIX:
                    //    break;
            }
        }

        public void AddRequest(APIRequest request)
        {
            APIRequest lookreq = RequestQueue.Find(x => x.URL.Equals(request.URL));

            if (lookreq != null)
            {
                RequestQueue.Remove(lookreq);
                //Debug.Log("<color='magenta'>Removed Request : " + request.URL + "</color>");
            }

            RequestQueue.Add(request);
            //Debug.Log("<color='magenta'>Added Request : " + request.URL + "</color>");
            //Debug.Log("<color='green'>Queue count : " + RequestQueue.Count + "</color>");
        }
    }

    public enum METHOD
    {
        GET,
        POST,
        POST_RAW,
        POST_MIX,
    }

    public class Services : IndestructibleSingleton<Services>
    {
        public static RequestScheduler APIRequestScheduler;

        static bool isSchedulerImplemented = true;
        public requiredData requiredData;
        public Data.ScenesData sceneData;

        public override void OnAwake()
        {
            ServicesData.isLive = true;
            ServicesData.SetupBaseURL();
            //DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            APIRequestScheduler = new RequestScheduler();
        }

        private void OnDestroy()
        {
            if (APIRequestScheduler != null)
                APIRequestScheduler.KillRequestSchedular();
        }
        public void SetAuthentication(string auth)
        {
            ServicesData.APIAuthToken = auth;
            Debug.Log(ServicesData.APIAuthToken);
        }
        #region POST METHOD

        public static async void Post(string postURL, KVPList<string, string> data, Action<string> OnServiceCallBack,
     bool shouldAuthorize = true, bool isToBeAddedToScheduler = true, bool isFromScheduler = false)
        {
            if (!HasInternetConnection())
                return;
            if (isToBeAddedToScheduler && isSchedulerImplemented)
            {
                APIRequestScheduler.AddRequest(new APIRequest(METHOD.POST, postURL, OnServiceCallBack, data, null, shouldAuthorize));
                return;
            }

            postURL = postURL.FinalizeURL();

            Debug.Log("<color='blue'> URL :: " + postURL + "</color>");


            WWWForm formData = new WWWForm();

            string m_body = "";

            for (int count = 0; count < data.Count; count++)
            {
                m_body = m_body + data[count].Key + ":" + data[count].Value + "\n";
                formData.AddField(data[count].Key, data[count].Value.ToString());
            }

            Debug.Log("Body " + "\n" + m_body + "auth : " + SavedDataHandler.Instance._saveData.auth_Token);

            UnityWebRequest request = UnityWebRequest.Post(postURL, formData);

            if (shouldAuthorize)
            {
                request.SetRequestHeader("Authorization", SavedDataHandler.Instance._saveData.auth_Token );
            }
            //    request.SetRequestHeader("content-type", "application/json");
            request.timeout = 25;


            await request.SendWebRequest();
            Debug.Log("<color=green> Received Data :" + postURL + " : " + request.downloadHandler.text + "</color>");
            JSONNode node = JSON.Parse(request.downloadHandler.text);
            string authData = node["Data"].ToString();
            string errorMsg = node["ErrorMessage"].ToString();
            switch (request.responseCode)
            {
                case 200:
                    if (IsValidJson(authData))
                    {
                        OnServiceCallBack(authData);
                    }
                    break;
                case 400: //invalid request 
                    CommonPopUp.Instance.DisplayMessagePanel(errorMsg);
                    break;
                case 500: //server error
                    break;

                case 401: //invalid authantication 
                    CommonPopUp.Instance.DisplayMessagePanel(errorMsg/*, () =>
                    {
                        DataHandler.Instance.DoAuthenticateNotValid(async () =>
                        {
                            request.Dispose();
                            Post(postURL, data, (resp) =>
                            {
                            });
                        });
                    }*/);
                    break;

                default:
                    CommonPopUp.Instance.DisplayMessagePanel(errorMsg);
                    break;
            }

        }
        public static async void PostAuth(string postURL, KVPList<string, string> data, Action<string> OnServiceCallBack,
    bool shouldAuthorize = true, bool isToBeAddedToScheduler = true, bool isFromScheduler = false)
        {
            if (!HasInternetConnection())
                return;
            if (isToBeAddedToScheduler && isSchedulerImplemented)
            {
                APIRequestScheduler.AddRequest(new APIRequest(METHOD.POST, postURL, OnServiceCallBack, data, null, shouldAuthorize));
                return;
            }

            postURL = postURL.FinalizeURL();

            Debug.Log("<color='blue'> URL :: " + postURL + "</color>");


            WWWForm formData = new WWWForm();

            string m_body = "";

            for (int count = 0; count < data.Count; count++)
            {
                m_body = m_body + data[count].Key + ":" + data[count].Value + "\n";
                formData.AddField(data[count].Key, data[count].Value.ToString());
            }

            Debug.Log("Body " + "\n" + m_body + "auth : " + SavedDataHandler.Instance._saveData.auth_Token);

            UnityWebRequest request = UnityWebRequest.Post(postURL, formData);

            if (shouldAuthorize)
            {
                request.SetRequestHeader("Authorization", SavedDataHandler.Instance._saveData.auth_Token);
            }
            //    request.SetRequestHeader("content-type", "application/json");
            request.timeout = 25;


            await request.SendWebRequest();
            switch (request.result)
            {
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:

                    Debug.LogError("API ERROR : " + request.error);

                    if (isFromScheduler)
                    {
                        OnServiceCallBack(request.downloadHandler.text);
                    }
                    break;

                case UnityWebRequest.Result.Success:

                    if (IsValidJson(request.downloadHandler.text))
                    {
                        Debug.Log("<color=green>" + request.downloadHandler.text + "</color>");
                        OnServiceCallBack(request.downloadHandler.text);
                    }
                    break;
            }

        }
        #endregion

        #region GET METHOD

        public static async void Get(string getURL, Action<string> OnServiceCallBack,
            bool shouldAuthorize = true, bool withTimeOut = false, bool isToBeAddedToScheduler = true, bool isFromScheduler = false)
        {
            if (!HasInternetConnection())
                return;
            if (isToBeAddedToScheduler && isSchedulerImplemented)
            {
                APIRequestScheduler.AddRequest(new APIRequest(METHOD.GET, getURL, OnServiceCallBack, null, null, shouldAuthorize, withTimeOut));
                return;
            }

            getURL = getURL.FinalizeURL();

            Debug.Log("URL : " + getURL);

            UnityWebRequest request = UnityWebRequest.Get(getURL);

            if (withTimeOut)
            {
                request.timeout = 5;
            }
            if (shouldAuthorize)
            {
                request.SetRequestHeader("Authorization", SavedDataHandler.Instance._saveData.auth_Token);
            }

            await request.SendWebRequest();
  
            Debug.Log("<color=green>" + request.downloadHandler.text + "</color>");
            Debug.Log("<color=green>" + request.responseCode + "</color>");

            JSONNode node = JSON.Parse(request.downloadHandler.text);
            string data = node["Data"].ToString();
            string errorMsg = node["ErrorMessage"].ToString();
            switch (request.responseCode)
            {
                case 200:
                    if (IsValidJson(data))
                    {
                        OnServiceCallBack(data);
                    }
                    break;
                case 400: //invalid request 
                    CommonPopUp.Instance.DisplayMessagePanel(errorMsg);
                    break;
                case 500: //server error
                    break;

                case 401: //invalid authantication 
                    CommonPopUp.Instance.DisplayMessagePanel(errorMsg/*, () =>
                    {
                        DataHandler.Instance.DoAuthenticateNotValid(() =>
                        {
                            request.Dispose();
                            request = UnityWebRequest.Get(getURL);
                            Debug.Log("Get Url : " + getURL);
                            if (withTimeOut)
                                request.timeout = 5;
                            if (shouldAuthorize)
                                request.SetRequestHeader("Authorization", SavedDataHandler.Instance._saveData.auth_Token);
                            request.SendWebRequest();
                        });
                    }*/);
                    break;

                default:
                    CommonPopUp.Instance.DisplayMessagePanel(errorMsg);
                    break;
            }
        }


        #region IMAGE_UPLOAD

        public static async void UploadImage(string PostURL, KVPList<string, byte[]> data, Action<string> OnServiceCallBack,
            bool shouldAuthorize = true)
        {
            PostURL = PostURL.FinalizeURL();

            Debug.Log("<color='blue'> URL :: " + PostURL + "</color>");


            WWWForm formData = new WWWForm();


            for (int count = 0; count < data.Count; count++)
            {
                formData.AddBinaryData(data[count].Key, data[count].Value);
            }


            UnityWebRequest request = UnityWebRequest.Post(PostURL, formData);

            if (shouldAuthorize)
            {
                request.SetRequestHeader("Authorization", ServicesData.APIAuthToken);
            }

            request.timeout = 10;

            await request.SendWebRequest();


            switch (request.result)
            {
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:

                    Debug.LogError("API ERROR : " + request.downloadHandler.text);

                    break;

                case UnityWebRequest.Result.Success:

                    if (IsValidJson(request.downloadHandler.text))
                    {
                        Debug.Log("<color=green>" + request.downloadHandler.text + "</color>");
                        OnServiceCallBack(request.downloadHandler.text);
                    }
                    break;
            }
        }


        #endregion


        public static async Task DownloadThisImage(string imgUrl, Action<Texture2D> downloadedTexture)
        {
            //  imgUrl=ServicesData.baseAPIURL + imgUrl;
            string imgFileName = Path.GetFileNameWithoutExtension(imgUrl);

            string imgFolder = Path.Combine(Application.persistentDataPath, ServicesData.ImagesDirectoryPath + "/");
            if (!Directory.Exists(imgFolder)) Directory.CreateDirectory(imgFolder);

            bool imgExists = false;
            if (File.Exists(imgFolder + imgFileName))
            {
                Debug.Log("FILE");
                imgExists = true;
                imgUrl = "file://" + imgFolder + imgFileName;
            }
            Debug.Log("img url : " + imgUrl);
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(imgUrl);

            await www.SendWebRequest();

            switch (www.result)
            {
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                    Debug.LogError(www.url + " - " + www.error);
                    downloadedTexture?.Invoke(null);
                    break;
                case UnityWebRequest.Result.Success:
                    Texture myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    downloadedTexture.Invoke(myTexture as Texture2D);
                    if (!imgExists)
                    {
                        File.WriteAllBytes(imgFolder + imgFileName, www.downloadHandler.data);
                    }
                    break;
            }
        }

        public static async Task DownloadThisVideo(string videoUrl, Action<String> vurl)
        {
            string videoFileName = Path.GetFileName(videoUrl);

            string videoFolder = Path.Combine(Application.persistentDataPath, ServicesData.VideosDirectoryPath + "/");
            if (!Directory.Exists(videoFolder)) Directory.CreateDirectory(videoFolder);

            bool videoExists = false;
            if (File.Exists(videoFolder + videoFileName))
            {
                Debug.Log("FILE");
                videoExists = true;
                videoUrl = "file://" + videoFolder + videoFileName;
                vurl.Invoke(videoUrl as String);
            }
            else
            {
                Debug.Log("video url : " + videoUrl);
                UnityWebRequest www = UnityWebRequest.Get(videoUrl);

                await www.SendWebRequest();

                switch (www.result)
                {
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                    case UnityWebRequest.Result.ConnectionError:
                        Debug.LogError(www.url + " - " + www.error);
                        vurl.Invoke(null);
                        break;
                    case UnityWebRequest.Result.Success:
                        if (!videoExists)
                        {
                            File.WriteAllBytes(videoFolder + videoFileName, www.downloadHandler.data);
                        }
                        vurl.Invoke(videoUrl as String);
                        break;
                }
            }
        }
        #endregion

        #region MixedData

        public static async void PostMixedData(string PostURL, KVPList<string, string> data, KVPList<string, byte[]> rawdata,
            Action<string> OnServiceCallBack,
            bool shouldAuthorize = true)
        {
            PostURL = PostURL.FinalizeURL();

            Debug.Log("<color='blue'> URL :: " + PostURL + "</color>");


            WWWForm formData = new WWWForm();

            string m_body = "";

            for (int count = 0; count < data.Count; count++)
            {
                m_body = m_body + data[count].Key + ":" + data[count].Value + "\n";
                formData.AddField(data[count].Key, data[count].Value.ToString());
            }

            for (int count = 0; count < rawdata.Count; count++)
            {
                m_body = m_body + rawdata[count].Key + ":" + rawdata[count].Value + "\n";
                formData.AddBinaryData(rawdata[count].Key, rawdata[count].Value);
            }

            Debug.Log("Body " + "\n" + m_body);

            UnityWebRequest request = UnityWebRequest.Post(PostURL, formData);

            if (shouldAuthorize)
            {
                request.SetRequestHeader("Authorization", ServicesData.APIAuthToken);
            }

            request.timeout = 10;

            await request.SendWebRequest();


            switch (request.result)
            {
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                    Debug.LogError("API ERROR : " + request.downloadHandler.text);
                    break;

                case UnityWebRequest.Result.Success:

                    if (IsValidJson(request.downloadHandler.text))
                    {
                        Debug.Log("<color=green>" + request.downloadHandler.text + "</color>");
                        OnServiceCallBack(request.downloadHandler.text);
                    }
                    break;
            }

        }

        #endregion


        #region Utilities
        public static bool HasInternetConnection()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                CommonPopUp.Instance.DisplayMessagePanel("Internet Connectivity");
                return false;
            }
            return true;
        }
        public static bool IsValidJson(string strInput)
        {
            strInput = strInput.Trim();
            if (!string.IsNullOrEmpty(strInput)) //For array
            {
                try
                {
                    var obj = JSON.Parse(strInput);
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

    }
    public static class MyExtension
    {
        public static string ConvertToCommaSeparatedValue(this double amount)
        {
            string amt = amount.ToString("###,###,##0.00");
            return amt;
        }

        public static string ConvertListToString<T>(this List<T> lst)
        {
            string convertedStr = string.Join(",", lst);
            return convertedStr;
        }
    }
    [Serializable]
    public class requiredData
    {
     //   public int companyId;
     //   public int locationId;
     //   public string screenId;
     //   public string screenNum;
     //   public int catId;
     //   public int brandId;
     //   public int productId;
     //   public int SessionId;
     //   public int customerId;
     //   public int updatedProductId;
     //   public int productQuantity;
     //   public string SessionToken;
     //   public DateTime SessionIdealTime;
     //   public bool isCartDetail;

    }
}

