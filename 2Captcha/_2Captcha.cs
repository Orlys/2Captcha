﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
#pragma warning disable 649

namespace _2Captcha
{
#if NETSTANDARD2_0
        [Serializable]
#endif
    internal struct _2CaptchaResultInternal
    {
        public bool Status;
        public string Request;
    }
     

    public class _2Captcha
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public _2Captcha(string apiKey, HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            _apiKey = apiKey;
        }
         
        private const string API_URL = "https://2captcha.com";
        private const string RES_API_URL = API_URL + "/res.php";
        private const string IN_API_URL = API_URL + "/in.php";
          
        public async Task<_2CaptchaResult> Report(string solveId, bool isBad = false)
        {
            var getData = new Dictionary<string, string>
            {
                { "key", _apiKey },
                { "action", isBad ? "reportbad" : "reportgood" },
                { "id", solveId },
                { "json", "1"}
            };

            var inResponse = await _httpClient.PostAsync(RES_API_URL, new FormUrlEncodedContent(getData));
            var inJson = await inResponse.Content.ReadAsStringAsync();

            var @in = JsonConvert.DeserializeObject<_2CaptchaResultInternal>(inJson);

            return new _2CaptchaResult(@in.Status, @in.Request, solveId);
        }

        public async Task<_2CaptchaResult> GetBalance()
        {
            var getData = new Dictionary<string, string>
            {
                { "key", _apiKey },
                { "action", "getbalance" },
                { "json", "1" }
            };

            var inResponse = await _httpClient.PostAsync(RES_API_URL, new FormUrlEncodedContent(getData));
            var inJson = await inResponse.Content.ReadAsStringAsync();

            var @in = JsonConvert.DeserializeObject<_2CaptchaResultInternal>(inJson);

            return new _2CaptchaResult(@in.Status, @in.Request, null);
        }
        
        

        private async Task<_2CaptchaResult> Solve(string method, int delaySeconds, params KeyValuePair<string, string>[] args)
        {
            IDictionary<string, string> postData = new Dictionary<string, string>
            {
                { "key", _apiKey },
                { "method", method },
                { "json", "1" }
            };
            
            foreach (var arg in args)
            {
                postData.Add(arg);
            }

            var inResponse = await _httpClient.PostAsync(IN_API_URL, new FormUrlEncodedContent(postData));
            var inJson = await inResponse.Content.ReadAsStringAsync();

            var @in = JsonConvert.DeserializeObject<_2CaptchaResultInternal>(inJson);
            if (!@in.Status)
            {
                return new _2CaptchaResult(false, @in.Request, null);
            }
            
            await Task.Delay(delaySeconds * 1000);
            return await GetResponse(@in.Request);
        }

        private async Task<_2CaptchaResult> GetResponse(string solveId)
        {
            var apiKeySafe = Uri.EscapeUriString(_apiKey);

            while (true)
            {
                var resJson = await _httpClient.GetStringAsync(RES_API_URL + $"?key={apiKeySafe}&id={solveId}&action=get&json=1");

                var res = JsonConvert.DeserializeObject<_2CaptchaResultInternal>(resJson);
                if (!res.Status)
                {
                    if (res.Request == "CAPCHA_NOT_READY")
                    {
                        await Task.Delay(5 * 1000);
                        continue;
                    }
                    else
                    {
                        return new _2CaptchaResult(false, res.Request, solveId);
                    }
                }

                return new _2CaptchaResult(true, res.Request, solveId);
            }
        }

        

        public async Task<_2CaptchaResult> SolveImage(string imageBase64)
        {
            return await Solve("base64", 5, new KeyValuePair<string, string>("body", imageBase64));
        }

        public async Task<_2CaptchaResult> SolveQuestion(string question)
        {
            return await Solve("textcaptcha", 5,
                new KeyValuePair<string, string>("textcaptcha", question));
        }

        public async Task<_2CaptchaResult> SolveHCaptcha(string siteKey, string pageUrl)
        {
             return await Solve("hcaptcha", 10,
                new KeyValuePair<string, string>("sitekey", siteKey),
                new KeyValuePair<string, string>("pageurl", pageUrl));
        }

        public async Task<_2CaptchaResult> SolveReCaptchaV2(string googleSiteKey, string pageUrl, bool invisible = false)
        {
            return await Solve("userrecaptcha", 10,
                new KeyValuePair<string, string>("googlekey", googleSiteKey),
                new KeyValuePair<string, string>("pageurl", pageUrl),
                new KeyValuePair<string, string>("invisible", invisible ? "1" : "0"));
        }

        public async Task<_2CaptchaResult> SolveReCaptchaV3(string googleSiteKey, string pageUrl, string action = "verify", double minScore = 0.4)
        {
            return await Solve("userrecaptcha", 10,
                new KeyValuePair<string, string>("googlekey", googleSiteKey),
                new KeyValuePair<string, string>("pageurl", pageUrl),
                new KeyValuePair<string, string>("action", action),
                new KeyValuePair<string, string>("version", "v3"),
                new KeyValuePair<string, string>("min_score", minScore.ToString(CultureInfo.InvariantCulture)));
        }
        

        public async Task<_2CaptchaResult> SolveClickCaptcha(string imageBase64, string task)
        {
            return await Solve("base64", 5,
                new KeyValuePair<string, string>("coordinatescaptcha", "1"),
                new KeyValuePair<string, string>("body", imageBase64),
                new KeyValuePair<string, string>("textinstructions", task));
        }

        

        public async Task<_2CaptchaResult> SolveFunCaptcha(string funCaptchaPublicKey, string pageUrl, bool noJavaScript = false)
        {
            return await Solve("funcaptcha", 10,
                new KeyValuePair<string, string>("publickey", funCaptchaPublicKey),
                new KeyValuePair<string, string>("pageurl", pageUrl),
                new KeyValuePair<string, string>("nojs", noJavaScript ? "1" : "0"));
        }

        public async Task<_2CaptchaResult> SolveKeyCaptcha(string userId, string sessionId, string webServerSign, string webServerSign2, string pageUrl)
        {
            return await Solve("keycaptcha", 15,
                new KeyValuePair<string, string>("s_s_c_user_id", userId),
                new KeyValuePair<string, string>("s_s_c_session_id", sessionId),
                new KeyValuePair<string, string>("s_s_c_web_server_sign", webServerSign),
                new KeyValuePair<string, string>("s_s_c_web_server_sign2", webServerSign2),
                new KeyValuePair<string, string>("pageurl", pageUrl));
        }

        #region MultipartForm
        private async Task<_2CaptchaResult> Solve(string method, int delaySeconds, MultipartFormDataContent httpContent)
        {
            httpContent.Add(new StringContent(_apiKey), "key");
            httpContent.Add(new StringContent(method), "method");
            httpContent.Add(new StringContent("1"), "json");

            var inResponse = await _httpClient.PostAsync(IN_API_URL, httpContent);
            var inJson = await inResponse.Content.ReadAsStringAsync();

            var @in = JsonConvert.DeserializeObject<_2CaptchaResultInternal>(inJson);
            if (!@in.Status)
            {
                return new _2CaptchaResult(false, @in.Request, null);
            }

            await Task.Delay(delaySeconds * 1000);
            return await GetResponse(@in.Request);
        }
        public async Task<_2CaptchaResult> SolveImage(Stream imageStream)
        {
            var httpContent = new MultipartFormDataContent
            {
                { new StreamContent(imageStream), "file" }
            };

            return await Solve("post", 5, httpContent);
        }
        public async Task<_2CaptchaResult> SolveClickCaptcha(Stream imageStream, string task)
        {
            var httpContent = new MultipartFormDataContent
            {
                { new StringContent("1"), "coordinatescaptcha" },
                { new StreamContent(imageStream), "file" },
                { new StringContent(task), "textinstructions" }
            };

            return await Solve("post", 5, httpContent);
        }
        public async Task<_2CaptchaResult> SolveRotateCaptcha(Stream[] imageStreams, string rotateAngle)
        {
            var httpContent = new MultipartFormDataContent
            {
                { new StringContent(rotateAngle), "angle" }
            };

            for (var i = 0; i < imageStreams.Length; i++)
            {
                httpContent.Add(new StreamContent(imageStreams[i]), "file_" + (i + 1));
            }

            return await Solve("rotatecaptcha", 5, httpContent);
        }
        #endregion
    }
}
