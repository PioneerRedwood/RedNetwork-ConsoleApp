using System;
using System.Collections.Generic;
using System.Net.Http;

namespace RedNetwork
{
    public static class LoginClient
    {
        public static bool TryLogin(string id, string pwd, ref Dictionary<string, string> dict)
        {
            try
            {
                HttpClient client = new HttpClient();
                string uri = "http://localhost:8081/signin/";
                client.BaseAddress = new Uri(uri);

                HttpResponseMessage response = client.GetAsync(uri + $"{id}/{pwd}").Result;
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    // 결과로 {"id":"0"}만 오기 때문에 기본적인 파싱만 진행
                    Console.WriteLine(result);
                    result = result.Replace('{', ' ').Replace('}', ' ').Replace('\"', ' ').Replace('"', ' ').Trim();
                    
                    string[] splited1 = result.Split(':');
                    dict.Add(splited1[0].Trim(), splited1[1].Trim());
                    
                    return true;
                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                    return false;
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
