using System.Net.Http;
using System.Threading.Tasks;

namespace xtofs.httpscript
{
    internal static class HttpMessageExtensions
    {
        public static async Task<string> ShowAsync(this HttpRequestMessage message)
        {
            var content = new HttpMessageContent(message);
            return await content.ReadAsStringAsync();
        }

        public static async Task<string> ShowAsync(this HttpResponseMessage message, int? contentLimit)
        {
            var content = await message.Content.ReadAsStringAsync();
            if (contentLimit.HasValue && content.Length > contentLimit)
            {
                message.Content = new StringContent(content.Substring(0, contentLimit.Value) + "<<<content truncated>>>");
            }

            var messageContent = new HttpMessageContent(message);
            return await messageContent.ReadAsStringAsync();
        }
    }
}