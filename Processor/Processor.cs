using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using System.Globalization;

namespace Processor
{
    public class Processor
    {
        public void Process(string path)
        {
            var tetelek = Parse(path);

            if (!tetelek.Any())
            {
                return;
            }

            using (var sw = new StreamWriter("tetelek.csv", false))
            {
                sw.WriteLine("SzamlaSorszama;Tol;Ig;BruttoAr");
                foreach (var item in tetelek.OrderBy(i => i.SzamlaSorszam))
                {
                    sw.WriteLine($"\"{item.SzamlaSorszam}\";\"{item.IdoszakTol?.ToShortDateString()}\";\"{item.IdoszakIg?.ToShortDateString()}\";{item.BruttoAr}");
                }
            }

            // Nem jó így? Tételenként kellene megcsinálni
            var minDatum = tetelek.Where(i => i.IdoszakTol != null).Select(i => i.IdoszakTol.Value).DefaultIfEmpty().Min();

            var periodData = new List<PeriodData>();

            var honap = new DateTime(minDatum.Year, minDatum.Month, 1);
            while (true)
            {
                var hoVege = honap.AddMonths(1).AddDays(-1);
                var haviTetelek = tetelek
                    .Where(i => 
                        (i.IdoszakTol >= honap && i.IdoszakTol <= hoVege) ||
                        (i.IdoszakIg >= honap && i.IdoszakIg <= hoVege) );

                foreach (var tetel in haviTetelek)
                {
                    if (tetel.BruttoAr == 0)
                    {
                        continue;
                    }

                    DateTime kezdete;
                    if (tetel.IdoszakTol <= honap)
                    {
                        kezdete = honap;
                    }
                    else
                    {
                        kezdete = tetel.IdoszakTol.Value;
                    }

                    DateTime vege;
                    if (tetel.IdoszakIg == null)
                    {
                        vege = hoVege;
                    }
                    else if (tetel.IdoszakIg >= hoVege)
                    {
                        vege = hoVege;
                    }
                    else
                    {
                        vege = tetel.IdoszakIg.Value;
                    }

                    var tetelTol = tetel.IdoszakTol.Value;
                    var tetekIg = tetel.IdoszakIg ?? tetel.IdoszakTol.Value.AddMonths(1).AddDays(-1);
                    var tetelPeriod = (tetekIg - tetelTol).Days + 1;

                    var monthlyPeriod = (vege - kezdete).Days + 1;

                    var amount = Math.Round((tetel.BruttoAr / tetelPeriod) * monthlyPeriod, 0, MidpointRounding.AwayFromZero);

                    var data = new PeriodData
                    {
                        InvoiceNumber = tetel.SzamlaSorszam,
                        PeriodFrom = kezdete,
                        PeriodTo = vege,
                        Amount = amount
                    };

                    periodData.Add(data);
                }

                if (!haviTetelek.Any())
                {
                    break;
                }

                honap = honap.AddMonths(1);
            }

            using (var sw = new StreamWriter("tetelek2.csv", false))
            {
                sw.WriteLine("SzamlaSorszama;Tol;Ig;BruttoAr");
                foreach (var item in periodData.OrderBy(i => i.InvoiceNumber).ThenBy(i => i.PeriodFrom))
                {
                    sw.WriteLine($"\"{item.InvoiceNumber}\";\"{item.PeriodFrom.ToShortDateString()}\";\"{item.PeriodTo.ToShortDateString()}\";{item.Amount}");
                }
            }

            var tmp = periodData.GroupBy(i => new { i.PeriodFrom, i.PeriodTo }).OrderBy(i => i.Key.PeriodTo).ThenBy(i => i.Key.PeriodTo);
            foreach (var period in tmp)
            {
                Console.WriteLine($"Periódus: {period.Key.PeriodFrom.ToShortDateString()} - {period.Key.PeriodTo.ToShortDateString()}");
                var amount = period.Sum(i => i.Amount);
                Console.WriteLine($"\tÖsszeg: {amount}");

                var invoiceGroups = period.GroupBy(i => i.InvoiceNumber).OrderBy(i => i.Key);
                foreach (var invoice in invoiceGroups)
                {
                    Console.WriteLine($"\t\tSzámla: {invoice.Key}");
                    var invoiceAmount = invoice.Sum(i => i.Amount);
                    Console.WriteLine($"\t\t\tÖsszeg: {invoiceAmount}");
                }
            }

        }

        private List<SzamlaTetel> Parse(string path)
        {
            var files = Directory.GetFiles(path, "*.xml");

            var osszesSzamlaTetel = new List<SzamlaTetel>();

            foreach (var filePath in files)
            {
                var fileName = new FileInfo(filePath).Name;
                Console.WriteLine(fileName);

                var fileContent = File.ReadAllText(filePath, Encoding.GetEncoding("iso-8859-2"));
                var doc = new XmlDocument();
                doc.LoadXml(fileContent);

                try
                {
                    List<SzamlaTetel> szamlaTetelek;

                    if (IsFormat1(doc))
                    {
                        Console.WriteLine("1-es formátum");
                        szamlaTetelek = ParseFormat1(doc);
                    }
                    else if (IsFormat2(doc))
                    {
                        Console.WriteLine("2-es formátum");
                        szamlaTetelek = ParseFormat2(doc);
                    }
                    else
                    {
                        Console.WriteLine("Ismeretlen formátum");
                        continue;
                    }

                    osszesSzamlaTetel.AddRange(szamlaTetelek);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Számla feldolgozása sikertelen: {ex.Message}");
                }
            }

            return osszesSzamlaTetel;
        }

        private List<SzamlaTetel> ParseFormat1(XmlDocument doc)
        {
            // TODO: több számla?
            var tetelek = new List<SzamlaTetel>();
            var tetelNodes = doc.SelectNodes("//tetelek/tetel");

            var sorszamNode = doc.SelectSingleNode("//szamlainfo//sorszam");
            var sorszam = sorszamNode.InnerText.Trim();

            foreach (XmlNode xmlTetel in tetelNodes)
            {
                var id = int.Parse(xmlTetel.Attributes["id"].Value, CultureInfo.InvariantCulture);
                if (id % 10 != 0)
                {
                    continue;
                }

                var tetel = new SzamlaTetel
                {
                    SzamlaSorszam = sorszam
                };

                var bruttoArNode = xmlTetel.SelectSingleNode(".//bruttoar");
                var bruttoAr = int.Parse(bruttoArNode.InnerText.Replace(" ", "").Trim());
                tetel.BruttoAr = bruttoAr;

                var nettoArNode = xmlTetel.SelectSingleNode(".//nettoar");
                if (nettoArNode != null)
                {
                    var nettoAr = int.Parse(nettoArNode.InnerText.Replace(" ", "").Trim());
                    tetel.NettoAr = nettoAr;
                }
                else
                {
                    // Kerekítés?
                    continue;
                    //tetel.NettoAr = tetel.BruttoAr;
                    //tetel.NettoAr = 0;
                }

                var afakulcsNode = xmlTetel.SelectSingleNode(".//afakulcs");
                if (afakulcsNode != null)
                {
                    if (afakulcsNode.InnerText.Trim() != "M")
                    {
                        var afakulcs = int.Parse(afakulcsNode.InnerText.Replace(" ", "").Trim(), CultureInfo.InvariantCulture);
                        tetel.Afakulcs = afakulcs / 100.0;
                    }
                }

                var idoszakTolNode = xmlTetel.SelectSingleNode(".//idoszak/tol");
                if (idoszakTolNode != null) // pl. kerekítés
                {
                    var idoszakTol = DateTime.Parse(idoszakTolNode.InnerText, CultureInfo.InvariantCulture);
                    tetel.IdoszakTol = idoszakTol;
                }

                var idoszakIgNode = xmlTetel.SelectSingleNode(".//idoszak/ig");
                if (idoszakIgNode != null)
                {
                    var idoszakIg = DateTime.Parse(idoszakIgNode.InnerText, CultureInfo.InvariantCulture);
                    tetel.IdoszakIg = idoszakIg;
                }

                tetelek.Add(tetel);
            }

            return tetelek;
        }

        private List<SzamlaTetel> ParseFormat2(XmlDocument doc)
        {
            // TODO: több számla?
            var tetelek = new List<SzamlaTetel>();
            var tetelNodes = doc.SelectNodes("/szamlak/szamla/termek_szolgaltatas_tetelek");

            var sorszamNode = doc.SelectSingleNode("//fejlec/szlasorszam");
            var sorszam = sorszamNode.InnerText.Trim(); 

            foreach (XmlNode xmlTetel in tetelNodes)
            {
                var tetel = new SzamlaTetel
                {
                    SzamlaSorszam = sorszam
                };

                var bruttoArNode = xmlTetel.SelectSingleNode(".//bruttoar");
                if (bruttoArNode != null)
                {
                    var bruttoAr = decimal.Parse(bruttoArNode.InnerText.Replace(" ", "").Trim(), CultureInfo.InvariantCulture);
                    tetel.BruttoAr = bruttoAr;
                }
                else
                {
                    var nettoArNode = xmlTetel.SelectSingleNode(".//nettoar");
                    var nettoAr = decimal.Parse(nettoArNode.InnerText.Replace(" ", "").Trim(), CultureInfo.InvariantCulture);

                    var adokulcsNode = xmlTetel.SelectSingleNode(".//adokulcs");
                    var adokulcs = double.Parse(adokulcsNode.InnerText.Replace(" ", "").Trim(), CultureInfo.InvariantCulture);
                    var veglegesAdokulcs = 1 + ( adokulcs / 100.0);

                    var bruttoAr = (decimal)Math.Round(nettoAr * (decimal)veglegesAdokulcs, 0, MidpointRounding.AwayFromZero);
                    tetel.BruttoAr = bruttoAr;
                }

                // TODO: a kerekítés nem teljesen jó, van kulön kerekítési korrekció is (osszesites/kerekites

                var idoszakNode = xmlTetel.SelectSingleNode(".//idoszak");
                if (idoszakNode != null)
                {
                    var split = idoszakNode.InnerText.Split("-");

                    if(split.Length == 0 || split.Length > 2)
                    {
                        Console.WriteLine("Ismeretlen időszak");
                    }

                    if (split.Length > 0)
                    {
                        var idoszakTol = DateTime.Parse(split[0], CultureInfo.InvariantCulture);
                        tetel.IdoszakTol = idoszakTol;
                    }

                    if(split.Length == 2)
                    {
                        var idoszakIg = DateTime.Parse(split[1], CultureInfo.InvariantCulture);
                        tetel.IdoszakIg = idoszakIg;
                    }
                }
                else
                {
                    Console.WriteLine("Ismeretlen időszak");
                }

                var idoszakTolNode = xmlTetel.SelectSingleNode(".//idoszak/tol");
                if (idoszakTolNode != null) // pl. kerekítés
                {
                    var idoszakTol = DateTime.Parse(idoszakTolNode.InnerText, CultureInfo.InvariantCulture);
                    tetel.IdoszakTol = idoszakTol;
                }

                var idoszakIgNode = xmlTetel.SelectSingleNode(".//idoszak/ig");
                if (idoszakIgNode != null)
                {
                    var idoszakIg = DateTime.Parse(idoszakIgNode.InnerText, CultureInfo.InvariantCulture);
                    tetel.IdoszakIg = idoszakIg;
                }

                tetelek.Add(tetel);
            }

            return tetelek;
        }

        private bool IsFormat1(XmlDocument doc)
        {
            var node = doc.SelectSingleNode("/content/szamla");
            if (node != null)
            {
                return true;
            }

            return false;
        }

        private bool IsFormat2(XmlDocument doc)
        {
            var node = doc.SelectSingleNode("/szamlak/szamla");
            if (node != null)
            {
                return true;
            }

            return false;
        }
    }
}
