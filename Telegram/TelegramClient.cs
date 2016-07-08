using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PSNBot.Telegram
{
    public class TelegramClient
    {
        private readonly string _baseUri = "https://api.telegram.org/bot{0}/";

        private string _token;

        public TelegramClient(string token)
        {
            _token = token;
        }

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(string.Format(_baseUri, _token));
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private HttpContent CreateHttpContentFromObject(object data)
        {
            if (data == null)
            {
                return new StringContent(string.Empty);
            }

            var raw = JsonConvert.SerializeObject(data);
            var content = new StringContent(raw);

            content.Headers.ContentType.MediaType = "application/json";
            return content;
        }

        private async Task<T> Post<T>(string uri, object query = null)
        {
            using (var c = CreateHttpClient())
            {
                var response = await c.PostAsync(uri, CreateHttpContentFromObject(query));
                var data = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(data);
            }            
        }

        public async Task<Response<User>> GetMe()
        {
            return await Post<Response<User>>("getMe");
        }

        public async Task<Response<Update[]>> GetUpdates(GetUpdatesQuery query)
        {
            return await Post<Response<Update[]>>("getUpdates", query);
        }

        public async Task<Response<Message>> SendMessage(SendMessageQuery query)
        {
            return await Post<Response<Message>>("sendMessage", query);
        }

        public async Task<Response<Message>> SendPhoto(SendPhotoQuery query, byte[] data)
        {
            using (var c = CreateHttpClient())
            {
                using (var content = new MultipartFormDataContent(DateTime.Now.ToString()))
                {
                    var imageContent = new ByteArrayContent(data);
                    imageContent.Headers.ContentType =
                        MediaTypeHeaderValue.Parse("image/png");

                    content.Add(imageContent, "photo", "image.png");
                    var response = await c.PostAsync(string.Format("sendPhoto?chat_id={0}", query.ChatId), content);
                    var raw = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Response<Message>>(raw);
                }
            }
        }
    }
}
