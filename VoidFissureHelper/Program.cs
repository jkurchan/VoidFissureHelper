using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace Tesseract.ConsoleDemo
{
    internal class Program
    {
        private static TesseractEngine engine;
        private const string ITEMS_FILE_NAME = "warframe_items.reavacwel";
        private const string DROPS_FILE_NAME = "item.png";
        private static List<WarframeItem> Items;

        [STAThread]
        public static void Main(string[] args)
        {
            Console.Title = "Void Fissure Farm Helper";

            engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            GetAllItems();
            LoadItemsToMemory();
            Console.WriteLine("Program startup completed!");
            
            while(true)
            {
                Console.WriteLine("\nAwaiting screenshots.");
                Console.WriteLine("Press [Esc] to exit.\n");
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Escape) break;

                if (SaveClipboardToFile())
                {
                    string processedString = ProcessImage();
                    List<WarframeItem> foundItems = FindItems(processedString);
                    PrintItemInfo(foundItems);
                }
            }

            engine.Dispose();
        }

        private static bool SaveClipboardToFile()
        {
            Console.WriteLine("Copying image from clipboard.");
            try
            {
                Clipboard.GetImage().Save(DROPS_FILE_NAME, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine("Image copied.");

                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("No image found in clipboard!");
                return false;
            }
        }

        private static void GetAllItems()
        {
            string url = @"http://warframe.market/api/get_all_items_v2";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                File.WriteAllText(ITEMS_FILE_NAME, reader.ReadToEnd());
                Console.WriteLine("Saved items to warframe_items.reavacwel file.");
            }
        }

        private static void LoadItemsToMemory()
        {
            string json = File.ReadAllText(ITEMS_FILE_NAME);
            Items = JsonConvert.DeserializeObject<List<WarframeItem>>(json);
            Console.WriteLine("Loaded items to memory.");
        }

        private static string ProcessImage()
        {
            var testImagePath = "./" + DROPS_FILE_NAME;
            var result = string.Empty;

            try
            {
                Console.WriteLine("Processing image.");
                using (var img = Pix.LoadFromFile(testImagePath))
                using (var page = engine.Process(img))
                {
                    var text = page.GetText();
                    using (var iter = page.GetIterator())
                    {
                        iter.Begin();
                        do
                        {
                            do
                            {
                                do
                                {
                                    do
                                    {
                                        result += " " + iter.GetText(PageIteratorLevel.Word);
                                    } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));
                                } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                            } while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                        } while (iter.Next(PageIteratorLevel.Block));

                        Console.WriteLine("Image processing finished.");
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.WriteLine("Unexpected Error: " + e.Message);
                Console.WriteLine("Details: ");
                Console.WriteLine(e.ToString());
            }

            return result;
        }

        private static List<WarframeItem> FindItems(string imageProcessResult)
        {
            Console.WriteLine("\nLooking for matching items");
            List<WarframeItem> foundItems = new List<WarframeItem>();
            foreach (WarframeItem item in Items)
            {
                if (imageProcessResult.Contains(item.Name.ToUpper()))
                    foundItems.Add(item);
            }
            return foundItems;
        }

        private static void PrintItemInfo(List<WarframeItem> items)
        {
            int highestWorth = 0;
            string name = string.Empty;

            if (items.Count == 0)
            {
                Console.WriteLine("No items found :<");
                return;
            }

            Console.WriteLine("Found " + items.Count + " matching items:");

            foreach(WarframeItem item in items)
            {
                string result = string.Empty;
                string url = string.Format("http://warframe.market/api/get_orders/{0}/{1}", item.Type, item.Name);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                    result = reader.ReadToEnd();

                ItemResponse itemResponse = JsonConvert.DeserializeObject<ItemResponse>(result);
                Console.WriteLine(String.Format("Item: {0} ({1})", item.Name, item.Type));

                int lowestPrice = PrintItemWorth(itemResponse);
                if(lowestPrice > highestWorth)
                {
                    highestWorth = lowestPrice;
                    name = item.Name;
                }

                Console.WriteLine("\n");
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(name);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" is worth the most ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("platinum");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(".");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(name);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" is worth the most ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("ducats");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(". (maybe lol)");
        }

        private static int PrintItemWorth(ItemResponse item)
        {
            List<User> sellers = item.Reponse.Sellers.Where(o => o.IsIngame == true).OrderBy(o => o.Price).ToList();
            List<int> values = new List<int>();

            foreach(User u in sellers)
                if (!values.Contains(u.Price) && values.Count <= 3)
                    values.Add(u.Price);

            Console.Write("Prices: ");

            foreach (int value in values)
                Console.Write(value + "p ");
            
            return values.First();
        }
    }
}