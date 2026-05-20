using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace MKSoundModCopy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Ищем путь к игре...");

            string lgc_path_dat = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\Lesta\\GameCenter\\data\\lgc_path.dat";
            if (!File.Exists(lgc_path_dat))
            {
                Console.Write($"Файл {lgc_path_dat} не найден.");
                return;
            }

            string lgc_path = File.ReadAllText(lgc_path_dat);
            if (!Directory.Exists(lgc_path))
            {
                Console.Write($"Путь {lgc_path} не найден.");
                return;
            }

            string lgc_pref = $"{lgc_path}\\preferences.xml";
            if (!File.Exists(lgc_pref))
            {
                Console.Write($"Файл {lgc_pref} не найден.");
                return;
            }

            string GamePath = "";
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(lgc_pref);
                var xnode = xDoc.DocumentElement?.SelectSingleNode("application")?.
                SelectSingleNode("games_manager")?.SelectSingleNode("current_game");
                if (xnode != null)
                {
                    GamePath = xnode.InnerText;
                }
            }

            if (GamePath.Length == 0)
            {
                Console.Write($"Путь к игре не найден.");
                return;
            }

            Console.WriteLine($"Путь к игре: {GamePath}");
            Console.WriteLine("Ищем версию игры...");

            string xml_path = $"{GamePath}\\game_info.xml";
            if (!File.Exists(xml_path))
            {
                Console.WriteLine($"Файл {xml_path} не найден.");
                return;
            }

            string GameVer = "";
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(xml_path);
                var xnode = xDoc.DocumentElement?.SelectSingleNode("game")?.
                    SelectSingleNode("part_versions")?.SelectSingleNode("version")?.
                    Attributes?["installed"]?.InnerText;
                if (xnode != null)
                {
                    Regex reg = new Regex(@"\d+$");
                    Match match = reg.Match(xnode);
                    if (match.Success)
                    {
                        GameVer = match.Value;
                    }
                }
            }

            if (GameVer.Length == 0)
            {
                Console.WriteLine($"Версия игры не найдена.");
                return;
            }
            Console.WriteLine($"Версия игры: {GameVer}.");

            int mods_count = 0;
            string ModsDir = $"{GamePath}\\bin\\{GameVer}\\res_mods\\banks\\mods";
            string exepath = Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString();
            foreach (var Dir in Directory.EnumerateDirectories(exepath))
            {
                string mod_xml = $"{Dir}\\mod.xml";
                if (!File.Exists(mod_xml))
                {
                    //Console.WriteLine($"не найден файл: {mod_xml}.");
                    continue;
                }
                var FilesWem = Directory.EnumerateFiles(Dir, "*.wem");
                if (FilesWem.Count() == 0) { continue; }

                // нашли папку с озвучкой
                var ModDirName = new DirectoryInfo(Dir).Name;
                var DestDir = $"{ModsDir}\\{ModDirName}";
                try
                {
                    Directory.CreateDirectory(DestDir);
                    foreach (var fwem in FilesWem)
                    {
                        File.Copy(fwem, fwem.Replace(Dir, DestDir), true);
                    }
                    File.Copy(mod_xml, mod_xml.Replace(Dir, DestDir), true);
                    Console.WriteLine($"Cкопирован мод из папки {ModDirName}");
                    mods_count++;
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            }
            if (mods_count == 0)
            {
                Console.WriteLine("Папок с модами озвучек не найдено.");
            }
            Console.WriteLine("Для закрытия нажмите любую клавишу...");

            var ctop = Console.CursorTop;
            var cleft = Console.CursorLeft;
            Console.CursorVisible = false;
            int _delay = Int32.Parse(Properties.Resources.AutoCloseTimer) * 10;
            for (int i = _delay; i > 0; i--)
            {
                Console.SetCursorPosition(cleft, ctop);
                Console.Write($"Автозакрытие через {1 + i / 10} сек.");
                Thread.Sleep(100);
                if (Console.KeyAvailable) break;
            }
        }
    }
}
