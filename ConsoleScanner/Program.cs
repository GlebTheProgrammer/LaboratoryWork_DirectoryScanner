using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleScanner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            byte threadCount = 2;

            // List of our 
            List<Entity> entities = new List<Entity>();

            // Get head directory for proceeding
            string directoryPath = @"C:\Users\Gleb\Desktop\Main Projects\Bank-Chat-Bot";
            DirectoryInfo headDirectory = new DirectoryInfo(directoryPath);

            entities.Add(CreateEntityFromDirectory(headDirectory, true));

            // Get files and directoriesin the head folder
            var files_HeadDirectory = headDirectory.GetFiles();
            var directories_HeadDirectory = headDirectory.GetDirectories();
            // Dont include hidden files
            var filesAndDirectories_HeadDirectory = headDirectory.GetFileSystemInfos();//.
                      //Where(f => !f.Attributes.ToString().Contains("Hidden")).ToList();

            var fileNames_HeadDirectory = new List<string>();
            foreach (var file in files_HeadDirectory)
                fileNames_HeadDirectory.Add(file.Name);

            var timer = new Stopwatch();
            timer.Start();
            // Check every file and directory in head directory
            GetDirectoryIerarchy(entities, fileNames_HeadDirectory, filesAndDirectories_HeadDirectory);
            timer.Stop();


            int startIndex = 0;
            string str = "";
            PrintEntities(entities, ref startIndex, ref str, null);

            Console.WriteLine($"\n\nTime spent: {(float)timer.ElapsedMilliseconds / 1000} s");
            Console.ReadLine();
        }

        static void GetDirectoryIerarchy(List<Entity> entities, List<string> fileNames_HeadDirectory, FileSystemInfo[] filesAndDirectories_HeadDirectory)
        {
            foreach (var item in filesAndDirectories_HeadDirectory)
            {
                
                if (fileNames_HeadDirectory.Contains(item.Name))
                    entities.Add(CreateEntityFromFile((FileInfo)item));
                else
                    ProceedDirectory(entities, (DirectoryInfo)item);

            }
        }

        static async Task<long> GetDirectorySize(DirectoryInfo dir)
        {
            long size = 0;

            FileInfo[] files = dir.GetFiles();
            DirectoryInfo[] directories = dir.GetDirectories();

            foreach (var file in files)
                size += file.Length;

            foreach (var directory in directories)
                size += await GetDirectorySize(directory);

            return size;
        }

        static Entity CreateEntityFromFile(FileInfo file)
        {
            return new Entity
            {
                Name = file.Name,
                Type = file.Extension == ".txt" ? EntityType.TextFile : EntityType.File,
                SubDirecory = file.Directory,
                Size = file.Length,
                Persantage = (100 * (float)file.Length / GetDirectorySize(file.Directory).Result).ToString() + "%"
            };
        }

        static Entity CreateEntityFromDirectory(DirectoryInfo dir, bool isHeadDirectory = false)
        {
            return new Entity
            {
                Name = dir.Name,
                Type = EntityType.Directory,
                SubDirecory = isHeadDirectory ? null : dir.Parent,
                Size = GetDirectorySize(dir).Result,
                Persantage = isHeadDirectory ? String.Empty : (100 * (float)GetDirectorySize(dir).Result / GetDirectorySize(dir.Parent).Result).ToString() + "%"
            };
        }

        static void ProceedDirectory(List<Entity> entities, DirectoryInfo dir)
        {
            entities.Add(CreateEntityFromDirectory(dir));

            // Get files and directories in the sub folder
            var files_SubDirectory = dir.GetFiles();
            var directories_SubDirectory = dir.GetDirectories();

            // Dont include hidden files
            var filesAndDirectories_SubDirectory = dir.GetFileSystemInfos();//.
                      //Where(f => !f.Attributes.ToString().Contains("Hidden")).ToList();

            var fileNames_SubDirectory = new List<string>();
            foreach (var file in files_SubDirectory)
                fileNames_SubDirectory.Add(file.Name);

            // Check every file and directory in head directory
            foreach (var item in filesAndDirectories_SubDirectory)
            {
                if (fileNames_SubDirectory.Contains(item.Name))
                    entities.Add(CreateEntityFromFile((FileInfo)item));
                else
                    ProceedDirectory(entities, (DirectoryInfo)item);

            }
        }

        static void PrintEntities(List<Entity> entities, ref int index, ref string indent, DirectoryInfo subDir)
        {
            while (index <= entities.Count - 1)
            {
                if (index == 0 || entities[index].SubDirecory.FullName == subDir?.FullName)
                {
                    string extension = entities[index].Type == EntityType.File ? "(file)" : entities[index].Type == EntityType.Directory ? "(dir)" : "(txt)";
                    string persantage = entities[index].Persantage == String.Empty ? "" : $", {entities[index].Persantage}";
                    Console.WriteLine(indent + extension + $" {entities[index].Name} ({entities[index].Size} байт{persantage})");

                    index += 1;

                    continue;
                }
                else
                {
                    if (subDir == null || entities[index].SubDirecory.FullName.Contains(entities[index - 1].SubDirecory.FullName))
                    {
                        indent += "\t";
                        subDir = entities[index].SubDirecory;
                        //PrintEntities(entities, ref index, ref indent, entities[index].SubDirecory);
                        continue;
                    }
                    else
                    {
                        subDir = subDir.Parent;
                        indent = indent.Length >= 2 ? indent.Remove(0, 1) : indent;
                        continue;
                    }
                }
            }

            while (index <= entities.Count - 1)
            {
                if (entities[index].SubDirecory.Name == subDir?.Name)
                {
                    PrintEntities(entities, ref index, ref indent, subDir);
                }
                    //break;
                //PrintEntities(entities, ref index, ref indent, subDir);
                else
                {
                    if (subDir == null || entities[index].SubDirecory.FullName.Contains(entities[index-1].SubDirecory.FullName))
                    {
                        indent += "\t";
                        PrintEntities(entities, ref index, ref indent, entities[index].SubDirecory);
                    }
                    else
                    {
                        subDir = subDir.Parent;
                        indent = indent.Length >= 2 ? indent.Remove(0, 2) : indent;
                        return;
                    }
                }
            }
            //indent = indent.Length >= 2 ? indent.Remove(0, 2) : indent;
        }
    }

    public enum EntityType
    {
        Directory = 1,
        File = 2,
        TextFile = 3
    }

    public class Entity
    {
        public string Name { get; set; } // Имя
        public EntityType Type { get; set; } // Тип сущности
        public DirectoryInfo SubDirecory { get; set; } // Каталог, в котором содержится сущность (null для головной)
        public long Size { get; set; } // размер (в байтах)
        public string Persantage { get; set; } // размер (в процентах от всего содержимого каталога)

        public Entity() { }

    }

}
