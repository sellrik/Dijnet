using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace dijnet
{
    public class DijnetClient
    {
        private HttpClient httpClient; // TODO: dispose?
        private int waitSeconds;
        private string baseUrl;

        public DijnetClient(int waitSeconds = 2)
        {
            this.waitSeconds = waitSeconds;
            this.baseUrl = "https://www.dijnet.hu/";

            httpClient = new HttpClient();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private void Sleep()
        {
            if (waitSeconds > 0)
            {
                Thread.Sleep(waitSeconds * 1000);
            }
        }

        private string GetUrl(string subUrl)
        {
            return $"{baseUrl}{subUrl}";
        }

        public void Login()
        {
            Sleep();

            var url = GetUrl("ekonto/login/login_check_ajax");

            var formData = new List<KeyValuePair<string, string>>();
            formData.Add(new KeyValuePair<string, string>("username", "nemethb_"));
            formData.Add(new KeyValuePair<string, string>("password", "oyZ8zDF7D2faOtFMKcfb"));
            var content = new FormUrlEncodedContent(formData);

            var response = httpClient.PostAsync(url, content).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result.Trim();

            var response2 = httpClient.GetStringAsync("https://www.dijnet.hu/ekonto/control/main").Result;
        }

        public void SzamlaSearch()
        {
            Sleep();

            var url = GetUrl("ekonto/control/szamla_search");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };

            var response = httpClient.SendAsync(request).Result;
           
            var responseContent = response.Content.ReadAsStringAsync().Result.Trim();
        }

        public int  SzamlaSearchSubmit()
        {
            Sleep();

            var url = GetUrl("ekonto/control/szamla_search_submit");

            var formData = new List<KeyValuePair<string, string>>();
            formData.Add(new KeyValuePair<string, string>("vfw_form", "szamla_search_submit"));
            formData.Add(new KeyValuePair<string, string>("vfw_coll", "szamla_search_params"));
            formData.Add(new KeyValuePair<string, string>("szlaszolgnev", null));
            formData.Add(new KeyValuePair<string, string>("regszolgid", null));
            formData.Add(new KeyValuePair<string, string>("datumtol", null));
            formData.Add(new KeyValuePair<string, string>("datumig", null));
            var content = new FormUrlEncodedContent(formData);

            var response = httpClient.PostAsync(url, content).Result;

            var responseContent = response.Content.ReadAsStringAsync().Result.Trim();

            var regex = new Regex("\\<tr id=\"r_[0-9]*\"");
            var matches = regex.Matches(responseContent).Count();
            return matches;
        }

        public void DownloadAll()
        {
            Login();

            SzamlaSearch();

            var nuberOfInvoices = SzamlaSearchSubmit();
            Console.WriteLine($"Számlák szám: {nuberOfInvoices}");

            for (int i = 0; i < nuberOfInvoices; i++)
            {
                Console.WriteLine($"Számla feldolgozása: {i + 1}");

                try
                {
                    SzamlaSelect(i);
                    SzamlaLetolt();
                    DownloadPdf();
                    DownloadXml();
                    SzamlaList();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"A számla letöltése/feldolgozása sikertelen: {ex.Message}");
                }
            }
        }

        public void SzamlaSelect(int rowId)
        {
            Sleep();

            var url = GetUrl("ekonto/control/szamla_select?vfw_coll=szamla_list&vfw_rowid={rowId}&exp=K");

            var response = httpClient.GetAsync(url).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            if (responseContent.Contains("Program hiba"))
            {
                throw new Exception("A számla nem található");
            }
        }

        public void SzamlaLetolt()
        {
            Sleep();

            var url = GetUrl("ekonto/control/szamla_letolt");
            var response = httpClient.GetStringAsync(url).Result;
        }

        public void DownloadPdf()
        {
            Sleep();

            var url = GetUrl("ekonto/control/szamla_pdf");

            var response = httpClient.GetAsync(url).Result;
            if(!response.Content.Headers.TryGetValues("Content-Disposition", out var contentDispositon))
            {
                Console.WriteLine("Pdf fájl nem található");
                return;
            }

            var fileName = contentDispositon.ToList().First().Replace("attachment; filename=", "");

            var responseStream = response.Content.ReadAsStreamAsync().Result;
            var memoryStream = new MemoryStream();
            responseStream.CopyTo(memoryStream);
            File.WriteAllBytes(fileName, memoryStream.GetBuffer());
        }

        public void DownloadXml()
        {
            Sleep();

            var url = GetUrl("ekonto/control/szamla_xml");

            var response = httpClient.GetAsync(url).Result;
            if (!response.Content.Headers.TryGetValues("Content-Disposition", out var contentDispositon))
            {
                Console.WriteLine("Xml fájl nem található");
                return;
            }

            var fileName = contentDispositon.ToList().First().Replace("attachment; filename=", "");

            var responseStream = response.Content.ReadAsStreamAsync().Result;
            var memoryStream = new MemoryStream();
            responseStream.CopyTo(memoryStream);
            File.WriteAllBytes(fileName, memoryStream.GetBuffer());
        }

        public void SzamlaList()
        {
            Sleep();

            var url = GetUrl("ekonto/control/szamla_list");

            var response = httpClient.GetStringAsync(url).Result;
        }


    }
}
