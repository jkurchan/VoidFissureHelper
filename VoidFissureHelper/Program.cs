using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Tesseract.ConsoleDemo
{
    internal class Program
    {
        private static TesseractEngine engine;
        private const string WINDOW_TITLE = "Void Fissure Farm Helper";
        private const string ITEMS_FILE_NAME = "warframe_items.reavacwel";
        private const string DROPS_FILE_NAME = "item.png";
        private static List<WarframeItem> Items;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        [STAThread]
        public static void Main(string[] args)
        {
            Console.Title = WINDOW_TITLE;

            engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            LoadItems();
            Console.WriteLine("Program startup completed!");
            Console.WriteLine("Awaiting screenshots [PrintScreen]");

            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
            engine.Dispose();
        }

        private static bool SaveClipboardToFile()
        {
            Console.WriteLine("\nCopying image from clipboard.");
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

        private static void LoadItems()
        {
            if (File.Exists(ITEMS_FILE_NAME))
            {
                string json = File.ReadAllText(ITEMS_FILE_NAME);
                Items = JsonConvert.DeserializeObject<List<WarframeItem>>(json);
            }
            else
            {
                string url = @"http://warframe.market/api/get_all_items_v2";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    Items = JsonConvert.DeserializeObject<List<WarframeItem>>(result);
                }

                url = @"http://warframe.wikia.com/wiki/Ducats";
                request = (HttpWebRequest)WebRequest.Create(url);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                {
                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.Load(stream);
                    foreach(HtmlNode table in doc.DocumentNode.SelectNodes("//table"))
                    {
                        foreach(HtmlNode row in table.SelectNodes("tr"))
                        {
                            HtmlNodeCollection cells = row.SelectNodes("th|td");
                            if(cells.Count == 2)
                            {
                                string itemName = cells[0].InnerText.Trim(new char[] { ' ', '*' });
                                string itemValue = cells[1].InnerText.Trim(new char[] { ' ', '*', '\r', '\n' });
                                
                                foreach(WarframeItem item in Items)
                                    if (itemName.Contains(item.Name))
                                        item.Ducats = int.Parse(itemValue);
                            }
                        }
                    }
                }

                string json = JsonConvert.SerializeObject(Items);
                File.WriteAllText(ITEMS_FILE_NAME, json);
            }

            Console.WriteLine("\nLoaded items to memory.");
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
            int highestPlatWorth = 0;
            int highestDucatWorth = 0;
            string highestPlatName = string.Empty;
            string highestDucatName = string.Empty;

            if (items.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No items found :<");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Found " + items.Count + " matching items:\n");
            Console.ForegroundColor = ConsoleColor.Gray;

            foreach (WarframeItem item in items)
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

                int lowestPrice = PrintItemWorth(itemResponse, item.Ducats);
                if(lowestPrice > highestPlatWorth)
                {
                    highestPlatWorth = lowestPrice;
                    highestPlatName = item.Name;
                }

                if(item.Ducats > highestDucatWorth)
                {
                    highestDucatWorth = item.Ducats;
                    highestDucatName = item.Name;
                }

                Console.WriteLine("\n");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(highestPlatName);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" is worth the most ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("platinum (" + highestPlatWorth + ")");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(".");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(highestDucatName);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" is worth the most ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("ducats (" + highestDucatWorth + ")");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(".");
        }

        private static int PrintItemWorth(ItemResponse item, int ducats)
        {
            List<User> sellers = item.Reponse.Sellers.Where(o => o.IsIngame == true).OrderBy(o => o.Price).ToList();
            List<int> values = new List<int>();

            foreach(User u in sellers)
                if (!values.Contains(u.Price) && values.Count <= 3)
                    values.Add(u.Price);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Platinum: ");
            Console.ForegroundColor = ConsoleColor.Gray;

            foreach (int value in values)
                Console.Write(value + "p ");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\nDucats: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(ducats);
            
            return values.First();
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
        
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {

            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == 44)
                    if (SaveClipboardToFile())
                    {
                        string processedString = ProcessImage();
                        List<WarframeItem> foundItems = FindItems(processedString);
                        PrintItemInfo(foundItems);
                        BringToForeground();
                    }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static void BringToForeground()
        {
            IntPtr handle = FindWindowByCaption(IntPtr.Zero, WINDOW_TITLE);
            if (handle == IntPtr.Zero)
            {
                Console.WriteLine("Can't find main window.");
                return;
            }

            SetForegroundWindow(handle);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);
    }
}