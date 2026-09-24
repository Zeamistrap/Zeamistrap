namespace Bloxstrap.Utility
{
    internal static class Http
    {
        private static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json)
                ?? throw new JsonException("The API returned a null JSON document.");
        }

        /// <summary>
        /// Gets and deserializes a JSON API response to the specified object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException"></exception>
        public static async Task<T> GetJson<T>(Uri url)
        {
            using var request = await App.HttpClient.GetAsync(url);

            request.EnsureSuccessStatusCode();

            string json = await request.Content.ReadAsStringAsync();
            
            return Deserialize<T>(json);
        }

        public static async Task<T> SendJson<T>(HttpRequestMessage requestMessage)
        {
            using var request = await App.HttpClient.SendAsync(requestMessage);

            request.EnsureSuccessStatusCode();

            string json = await request.Content.ReadAsStringAsync();

            return Deserialize<T>(json);
        }

        public static async Task<T> AuthGetJson<T>(Uri url)
        {
            using var request = await App.Cookies.AuthGet(url);

            request.EnsureSuccessStatusCode();

            string json = await request.Content.ReadAsStringAsync();

            return Deserialize<T>(json);
        }

        public static async Task<T> AuthSendJson<T>(HttpRequestMessage requestMessage)
        {
            HttpContent content = requestMessage.Content!;

            using var request = await App.Cookies.AuthPost(requestMessage.RequestUri, content);

            request.EnsureSuccessStatusCode();

            string json = await request.Content.ReadAsStringAsync();

            return Deserialize<T>(json);
        }
    }
}
