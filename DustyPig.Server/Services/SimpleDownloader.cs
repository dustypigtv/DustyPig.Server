//using System;
//using System.IO;
//using System.Net.Http;
//using System.Threading;
//using System.Threading.Tasks;

//namespace DustyPig.Server.Services
//{
//    public static class SimpleDownloader
//    {
//        static HttpClient _httpClient = new HttpClient();

//        static async Task<HttpResponseMessage> GetResponseAsync(string url, CancellationToken cancellationToken)
//        {
//            var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
//            response.EnsureSuccessStatusCode();
//            return response;
//        }

//        static async Task<HttpResponseMessage> GetResponseAsync(Uri uri, CancellationToken cancellationToken)
//        {
//            var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
//            response.EnsureSuccessStatusCode();
//            return response;
//        }


//        static FileStream CreateFile(string filename)
//        {
//            Directory.CreateDirectory(Path.GetDirectoryName(filename));
//            return new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
//        }



//        public static async Task DownloadFileAsync(string url, string filename, CancellationToken cancellationToken = default)
//        {
//            using var response = await GetResponseAsync(url, cancellationToken).ConfigureAwait(false);
//            using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

//            using var fileStream = CreateFile(filename);
//            await content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
//        }

//        public static async Task DownloadFileAsync(Uri uri, string filename, CancellationToken cancellationToken = default)
//        {
//            using var response = await GetResponseAsync(uri, cancellationToken).ConfigureAwait(false);
//            using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

//            using var fileStream = CreateFile(filename);
//            await content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
//        }




//        public static async Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken = default)
//        {
//            using var response = await GetResponseAsync(url, cancellationToken).ConfigureAwait(false);
//            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
//        }

//        public static async Task<string> DownloadStringAsync(Uri uri, CancellationToken cancellationToken = default)
//        {
//            using var response = await GetResponseAsync(uri, cancellationToken).ConfigureAwait(false);
//            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
//        }




//        public static async Task<byte[]> DownloadDataAsync(string url, CancellationToken cancellationToken = default)
//        {
//            using var response = await GetResponseAsync(url, cancellationToken).ConfigureAwait(false);
//            return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
//        }

//        public static async Task<byte[]> DownloadDataAsync(Uri uri, CancellationToken cancellationToken = default)
//        {
//            using var response = await GetResponseAsync(uri, cancellationToken).ConfigureAwait(false);
//            return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
//        }
//    }
//}
