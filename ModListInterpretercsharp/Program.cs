using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Tomlyn;
using Tomlyn.Model;


class Reader
{
    static void Main(string[] pathArg)
    {
        static void OpenUrl(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw new PlatformNotSupportedException("Unknown operating system");
            }
        }
        static bool validateFile(string pathIn)
        {
            if (!System.IO.File.Exists(pathIn))
            {
                Console.WriteLine("File doesn't exist!");
                Console.WriteLine("Press enter to exit...");
                return false;
            }   //nonexistent file check
            else if (!Path.GetExtension(pathIn).Equals(".xml", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Invalid file extension ("+Path.GetExtension(pathIn)+") expected xml!");
                Console.WriteLine("Press enter to exit...");
                return false;
            }   //invalid extension check
            else if(System.IO.File.ReadAllText(pathIn).Length < 1)
            {
                Console.WriteLine("Empty File!");
                Console.WriteLine("Press enter to exit...");
                return false;
            }   // empty file check
            else
            {
                try
                {
                    XDocument.Load(pathIn);
                    return true;
                }   //validation succeeded
                catch
                {
                    Console.WriteLine("Xml invalid or unreadable!");
                    return false;
                }   //invalid xml check
            }
            
        }//file validator
        static bool validateModList(string fileIn)
        {
            XDocument xmlIn = XDocument.Load(fileIn);
            XElement root = xmlIn.Root;
            string strRoot = root.Name.LocalName;
            var subElements = root.Elements().ToList();
            if (root.Name.LocalName != "mods")
            {
                Console.WriteLine("Invalid root: "+strRoot+" (expected mods!)");
                return false;   
            }   //check root to see if its in line with baro modlists, should be mods
            else
            {
                if (subElements.Count < 2)
                {
                    Console.WriteLine("Modlist is empty!");
                    Console.WriteLine("Press enter to exit...");
                    return false;
                }   //empty check
                else if (subElements[0].Name.LocalName == "Workshop" || subElements[0].Name.LocalName == "Local")
                {
                    Console.WriteLine("Malformed modlist!");
                    Console.WriteLine("Press enter to quit...");
                    return false;
                }
                else
                {
                    for (int i = 1; i < subElements.Count; i++)
                        if (subElements[i].Name == "Workshop")
                        {
                            {
                                var attributes = subElements[i].Attributes().ToList();
                                if (attributes.Count < 2)
                                {
                                    Console.WriteLine("Malformed modlist!");
                                    Console.WriteLine("Press enter to quit...");
                                    return false;
                                }
                                else if (attributes[0].Name != "name" || attributes[1].Name != "id")
                                {
                                    Console.WriteLine("Malformed modlist!");
                                    Console.WriteLine("Press enter to quit...");
                                    return false;
                                }
                            }
                        }
                        else if (subElements[i].Name == "Local")
                        {
                            var attributes = subElements[i].Attributes().ToList();
                            if (attributes.Count != 1)
                            {
                                Console.WriteLine("Malformed modlist!");
                                Console.WriteLine("Press enter to quit...");
                                return false;
                            }
                            else if (attributes[0].Name != "name")
                            {
                                Console.WriteLine("Malformed modlist!");
                                Console.WriteLine("Press enter to quit...");
                                return false;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Malformed modlist!");
                            Console.WriteLine("Press enter to quit...");
                            return false;
                        }
                }
                return true;
            }
        }//xml validator

        Console.Title = "Barotrauma Modlist Manager";
        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName!;
        string exeDirectory = Path.GetDirectoryName(exePath)!;
        string configPath = Path.Combine(exeDirectory, "config.toml");  //get path to config file


        string baroPath = "";
        if (!File.Exists(configPath))   //check if config exists
        {
            var toml = @"BarotraumaPath = """"
            # The input above needs to be escaped (\->\\)";
            var model = Toml.ToModel(toml);
            File.WriteAllText(configPath, Toml.FromModel(model));
            string configContent = File.ReadAllText(configPath);
            Console.WriteLine("Created config.toml\nPlease fill in 'BarotraumaPath' and restart to use the file selector");
        }
        else
        {
            string configContent = File.ReadAllText(configPath);
            var configData = Toml.Parse(configContent).ToModel();
            baroPath = @configData["BarotraumaPath"]?.ToString();
            baroPath = @Path.Combine(baroPath.Replace('/', Path.DirectorySeparatorChar), "ModLists");
            Console.WriteLine("Path: " + baroPath);
        }
        string loadedFile = "";
        if (pathArg.Length < 1)
        {
            Console.WriteLine("No input given");
            Console.WriteLine("Checking Barotrauma's directory...");
            if (!string.IsNullOrEmpty(baroPath))
            {
                List<string> modLists = Directory.GetFiles(baroPath).ToList();
                if (modLists.Count > 0)
                {
                    List<string> xmlModLists = new List<string>();
                    for (int i = 0; i < modLists.Count; i++)
                    {
                        if (!Path.GetExtension(modLists[i]).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            modLists.RemoveAt(i);
                        }
                        else
                        {
                            xmlModLists.Add(modLists[i]);
                        }
                    }
                    for (int i = 0; i < xmlModLists.Count; i++)
                    {
                        int fileNum = i + 1;
                        Console.WriteLine("(" + fileNum + ") - " + Path.GetFileNameWithoutExtension(xmlModLists[i]));
                    }
                    string userFile = Console.ReadLine();
                    if (!string.IsNullOrEmpty(userFile))
                    {
                        try
                        {
                            int userIndex = int.Parse(userFile);
                            userIndex = userIndex - 1;
                            if (userIndex < 0)
                            {
                                userIndex = 0;
                            }
                            else if (userIndex > xmlModLists.Count)
                            {
                                userIndex = xmlModLists.Count;
                            }
                            loadedFile = xmlModLists[userIndex];
                            Console.WriteLine("Loaded: " + loadedFile);
                        }
                        catch
                        {
                            Console.WriteLine("Invalid input!");
                            return;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Modlists folder is empty!");
                    return;
                }
            }
            else if (!Directory.Exists(baroPath))
            {
                Console.WriteLine("Barotrauma directory doesn't exist!");
            }
            else
            {
                Console.WriteLine("Empty Barotrauma path in " + configPath + " Please input a path or input a file to continue!");
            }
        }
        else
        {
            loadedFile = pathArg[0];
        }
        if (validateFile(loadedFile))
        {
            Console.WriteLine("Passed file check, checking xml.");
            if (validateModList(loadedFile))
            {
                XDocument xmlIn = XDocument.Load(loadedFile);
                XElement root = xmlIn.Root;
                var subElements = root.Elements().ToList();
                Console.WriteLine("File is a valid Barotrauma modlist.");
                Console.WriteLine("Choose an action.");
                Console.WriteLine("(1) Save mod links to file.");
                Console.WriteLine("(2) Open all mod links in browser.");
                string userInput = Console.ReadLine();
                if (userInput == null || userInput.Length < 1)
                {
                    Console.WriteLine("Please input something.");
                }
                else if (userInput == "1")
                {
                    string output = "";
                    output += "Core package: " + subElements[0].Name + "\n\n";
                    subElements.RemoveAt(0);
                    for (int i = 0; i < subElements.Count; i++)
                    {
                        var attributes = subElements[i].Attributes().ToList();
                        if (subElements[i].Name == "Workshop")
                        {
                            Console.WriteLine("Adding workshop mod: " + attributes[0].Value + " (id: " + attributes[1].Value + ")");
                            output += attributes[0].Value + "\n";
                            output += "https://steamcommunity.com/sharedfiles/filedetails/?id=" + attributes[1].Value + "\n\n";
                        }
                        else
                        {
                            Console.WriteLine("Adding local mod: " + attributes[0].Value);
                            output += attributes[0].Value + "\n\n";
                        }
                    }
                    Console.WriteLine("Done!");
                    Console.WriteLine("Input a path to where the file should be saved");
                    Console.WriteLine("(Leave blank to use where the original modlist is stored)");
                    string currentdir = AppContext.BaseDirectory;
                    string userPath = Console.ReadLine();
                    string savePath = "";
                    if (string.IsNullOrEmpty(userPath))
                    {
                        Console.WriteLine("Using default");
                        if (!Path.EndsInDirectorySeparator(currentdir))
                        {
                            currentdir += Path.DirectorySeparatorChar;
                            userPath = currentdir;
                        }
                    }
                    else if (!Path.EndsInDirectorySeparator(userPath))
                    {
                        userPath += Path.DirectorySeparatorChar;
                    }
                    else if (!Directory.Exists(userPath))
                    {

                        Console.WriteLine("Directory doesn't exist!");
                        Console.WriteLine("Using default");
                        if (!Path.EndsInDirectorySeparator(currentdir))
                        {
                            currentdir += Path.DirectorySeparatorChar;
                            userPath = currentdir;
                        }
                    }
                    Console.WriteLine("Using path " + userPath);
                    Console.WriteLine("Input a file name.");
                    Console.WriteLine("(Leave blank to use default (Modlist.txt)");
                    string userName = Console.ReadLine();
                    if (string.IsNullOrEmpty(userName))
                    {
                        Console.WriteLine("Using default");
                        userName = "Modlist";
                    }
                    else if (Path.HasExtension(userName))
                    {
                        userName = Path.ChangeExtension(userName, "");
                    }
                    else
                    {
                        foreach (char c in Path.GetInvalidFileNameChars())
                        {
                            if (userName.Contains(c))
                            {
                                Console.WriteLine("Removing invalid character in file name! (" + c + ")");
                                userName = userName.Replace(c, '-');
                            }
                        }
                    }
                    Console.WriteLine("Using name: " + userName);
                    userName += ".txt";
                    savePath = Path.Combine(userPath, userName);
                    Console.WriteLine("Saving to " + savePath);
                    File.WriteAllText(savePath, output);
                    Console.WriteLine("Saved!");
                    Console.WriteLine("Press enter to exit...");
                    Console.ReadLine();
                }
                else if (userInput == "2")
                {
                    subElements.RemoveAt(0);
                    for (int i = 0; i < subElements.Count; i++)
                    {
                        if (subElements[i].Name == "Workshop")
                        {
                            string url = "";
                            var attributes = subElements[i].Attributes().ToList();
                            Console.WriteLine("Opening " + attributes[0].Value + " (id: " + attributes[1].Value + ")");
                            url += "https://steamcommunity.com/sharedfiles/filedetails/?id=" + attributes[1].Value;
                            OpenUrl(url);
                        }
                        else
                        {
                            var attributes = subElements[i].Attributes().ToList();
                            Console.WriteLine("Skipping local mod " + attributes[0].Value);
                        }
                    }
                    Console.WriteLine("Done!");
                    Console.WriteLine("Press enter to exit...");
                    Console.ReadLine();
                }
                else if (userInput == "3")
                {
                    
                }
                else if (userInput == "4")
                {
                    
                }
                else
                {
                    Console.WriteLine("Invalid input!");
                }
            } 
            else
            {
                Console.ReadLine();
            }
        }
        else
        {
            Console.ReadLine();
        }
    }
}