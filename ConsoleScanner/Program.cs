using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ConsoleScanner
{
    internal class Program
    {
        static void Main(string[] args)
        {
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
            var filesAndDirectories_HeadDirectory = headDirectory.GetFileSystemInfos();

            var fileNames_HeadDirectory = new List<string>();
            foreach (var file in files_HeadDirectory)
                fileNames_HeadDirectory.Add(file.Name);

            var timer = new Stopwatch();
            // Check every file and directory in head directory
            GetDirectoryIerarchy(entities, fileNames_HeadDirectory, filesAndDirectories_HeadDirectory);


            ThreadPool.GetAvailableThreads(out int systemThreadsCount, out _);

            timer.Start();
            CalculateSizeOfAllEntities(entities, isAsync: true, numberOfThreadsToProceed: 1, numberOfSystemThreads: systemThreadsCount);
            CalculateSizeOfAllEntities(entities);
            timer.Stop();


            int startIndex = 0;
            string str = "";
            PrintEntities(entities, ref startIndex, ref str, null);

            Console.WriteLine($"\n\nTime spent: {(float)timer.ElapsedMilliseconds / 1000} s");
            Console.ReadLine();
        }

        static void CalculateSizeOfAllEntities(List<Entity> entities, bool isAsync = false, int numberOfThreadsToProceed = 0, int numberOfSystemThreads = 0)
        {
            if (!isAsync)
            {
                foreach (var entity in entities)
                {
                    if (entity.Type == EntityType.Directory)
                    {
                        DirectoryInfo dir = (DirectoryInfo)entity.Info;
                        entity.Size = GetDirectorySize(dir);
                        entity.Persantage = entity.SubDirecory == null ? String.Empty : (100 * (float)GetDirectorySize(dir) / GetDirectorySize(dir.Parent)).ToString() + "%";
                    }
                    else
                    {
                        FileInfo file = (FileInfo)entity.Info;
                        entity.Size = file.Length;
                        entity.Persantage = (100 * (float)file.Length / GetDirectorySize(file.Directory)).ToString() + "%";
                    }
                }
            }
            else
            {
                foreach (var entity in entities)
                {
                    ThreadPool.QueueUserWorkItem(TaskForAnAsyncCalculation, entity);

                    while (true)
                    {
                        ThreadPool.GetAvailableThreads(out int currentAvailableThreads, out _);
                        if (numberOfSystemThreads - currentAvailableThreads < numberOfThreadsToProceed)
                            break;
                    }
                }
                while (true)
                {
                    ThreadPool.GetAvailableThreads(out int currentAvailableThreadsCount, out _);
                    if (currentAvailableThreadsCount != numberOfSystemThreads)
                        Thread.Sleep(100);
                    else
                        break;
                }
            }
        }

        static void TaskForAnAsyncCalculation(object entityObj)
        {
            Entity entity = (Entity)entityObj;
            if (entity.Type == EntityType.Directory)
            {
                DirectoryInfo dir = (DirectoryInfo)entity.Info;
                entity.Size = GetDirectorySize(dir);
                entity.Persantage = entity.SubDirecory == null ? String.Empty : (100 * (float)GetDirectorySize(dir) / GetDirectorySize(dir.Parent)).ToString() + "%";
            }
            else
            {
                FileInfo file = (FileInfo)entity.Info;
                entity.Size = file.Length;
                entity.Persantage = (100 * (float)file.Length / GetDirectorySize(file.Directory)).ToString() + "%";
            }
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

        static long GetDirectorySize(DirectoryInfo dir)
        {
            long size = 0;

            FileInfo[] files = dir.GetFiles();
            DirectoryInfo[] directories = dir.GetDirectories();

            foreach (var file in files)
                size += file.Length;

            foreach (var directory in directories)
                size += GetDirectorySize(directory);

            return size;
        }

        static Entity CreateEntityFromFile(FileInfo file)
        {
            return new Entity
            {
                Info = file,
                Name = file.Name,
                Type = file.Extension == ".txt" ? EntityType.TextFile : EntityType.File,
                SubDirecory = file.Directory,
                //Size = file.Length,
                //Persantage = (100 * (float)file.Length / GetDirectorySize(file.Directory)).ToString() + "%"
            };
        }

        static Entity CreateEntityFromDirectory(DirectoryInfo dir, bool isHeadDirectory = false)
        {
            return new Entity
            {
                Info = dir,
                Name = dir.Name,
                Type = EntityType.Directory,
                SubDirecory = isHeadDirectory ? null : dir.Parent,
                //Size = GetDirectorySize(dir),
                //Persantage = isHeadDirectory ? String.Empty : (100 * (float)GetDirectorySize(dir) / GetDirectorySize(dir.Parent)).ToString() + "%"
            };
        }

        static void ProceedDirectory(List<Entity> entities, DirectoryInfo dir)
        {
            entities.Add(CreateEntityFromDirectory(dir));

            // Get files and directories in the sub folder
            var files_SubDirectory = dir.GetFiles();
            var directories_SubDirectory = dir.GetDirectories();

            // Dont include hidden files
            var filesAndDirectories_SubDirectory = dir.GetFileSystemInfos();

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
        public FileSystemInfo Info { get; set; }
        public string Name { get; set; } // Имя
        public EntityType Type { get; set; } // Тип сущности
        public DirectoryInfo SubDirecory { get; set; } // Каталог, в котором содержится сущность (null для головной)
        public long? Size { get; set; } = null; // размер (в байтах)
        public string Persantage { get; set; } = null; // размер (в процентах от всего содержимого каталога)

        public Entity() { }

    }

}
