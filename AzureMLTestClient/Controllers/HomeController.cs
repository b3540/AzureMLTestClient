using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;

namespace AzureMLTestClient.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> DoIt(HttpPostedFileBase imageFile)
        {
            var pixels = ConvertTo28x28Pixels(imageFile);

            var scoreRequest = new
            {
                Inputs = new
                {
                    input1 = new
                    {
                        ColumnNames = Enumerable.Range(0, pixels.Length).Select(n => $"f{n}").ToArray(), // "f0","f1","f2",...
                        Values = new[] { pixels.Select(n => n.ToString()).ToArray() } // "0","0","0",...
                    }
                },
                GlobalParameters = new { }
            };

            var apiKey = AppSettings.AzureML.APIKey;
            var baseAddress = AppSettings.AzureML.BaseAddress;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.BaseAddress = new Uri(baseAddress);
                var response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    dynamic result = JObject.Parse(resultJson);
                    return View("Result", result);
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return View("Error", new { response, responseContent });
                }
            }
        }

        private int[] ConvertTo28x28Pixels(HttpPostedFileBase imageFile)
        {
            using (var srcImage = Image.FromStream(imageFile.InputStream))
            using (var adjustedImage = new Bitmap(width: 28, height: 28))
            using (var g = Graphics.FromImage(adjustedImage))
            {
                g.FillRectangle(Brushes.White, 0, 0, adjustedImage.Width, adjustedImage.Height);

                var scale = Math.Min((double)adjustedImage.Width / srcImage.Width, (double)adjustedImage.Height / srcImage.Height);
                var sacledWidth = (int)Math.Round(srcImage.Width * scale);
                var sacledHeight = (int)Math.Round(srcImage.Height * scale);
                var left = (adjustedImage.Width - sacledWidth) / 2;
                var top = (adjustedImage.Height - sacledHeight) / 2;

                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(srcImage, x: left, y: top, width: sacledWidth, height: sacledHeight);

                //var path = Server.MapPath("~/App_Data/test.bmp");
                //adjustedImage.Save(path, ImageFormat.Bmp);

                var pixels = new int[adjustedImage.Width * adjustedImage.Height];
                for (var y = 0; y < adjustedImage.Height; y++)
                {
                    for (var x = 0; x < adjustedImage.Width; x++)
                    {
                        pixels[x + y * adjustedImage.Width] = 255 - (int)Math.Round(255.0 * adjustedImage.GetPixel(x, y).GetBrightness());
                    }
                }

                return pixels;
            }
        }
    }
}