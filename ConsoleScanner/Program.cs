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
            // List of all our entities (files and directories)
            List<Entity> entities = new List<Entity>();

            // Get head directory for proceeding
            string directoryPath = @"C:\Users\Gleb\Desktop\Main Projects\Bank-Chat-Bot";
            DirectoryInfo headDirectory = new DirectoryInfo(directoryPath);

            // Add head directory as an entity
            entities.Add(CreateEntityFromDirectory(headDirectory, isHeadDirectory: true));

            // Get files in the head folder
            var files_HeadDirectory = headDirectory.GetFiles();

            // Get all files (normal and directories). Hidden files included
            var filesAndDirectories_HeadDirectory = headDirectory.GetFileSystemInfos();

            // Create a list with only file names
            var fileNames_HeadDirectory = new List<string>();
            foreach (var file in files_HeadDirectory)
                fileNames_HeadDirectory.Add(file.Name);

            var timer = new Stopwatch();
            // Check every file and directory in head directory
            GetDirectoryIerarchy(entities, fileNames_HeadDirectory, filesAndDirectories_HeadDirectory);

            // Get all system threads available for usage
            ThreadPool.GetAvailableThreads(out int systemThreadsCount, out _);

            // Start timer to count how much time it took to calculate size of files and directories
            timer.Start();

            // Use this code to proceed all files asynchronously
            CalculateSizeOfAllEntities(entities, isAsync: true, numberOfThreadsToProceed: 7, numberOfSystemThreads: systemThreadsCount);

            // Use this code to proceed all files synchronously
            //CalculateSizeOfAllEntities(entities);

            // Stop timer
            timer.Stop();

            // Print all results using console
            int startIndex = 0;
            string str = "";
            PrintEntities(entities, ref startIndex, ref str, null);

            Console.WriteLine($"\n\nTime spent: {(float)timer.ElapsedMilliseconds / 1000} s");
            Console.ReadLine();
        }

        // Method for calculating size of all entities
        static void CalculateSizeOfAllEntities(List<Entity> entities, bool isAsync = false, int numberOfThreadsToProceed = 0, int numberOfSystemThreads = 0)
        {
            if (!isAsync) // If we want to proceed synchronously
            {
                foreach (var entity in entities) // Check every entity
                {
                    if (entity.Type == EntityType.Directory) // If we work with directory
                    {
                        DirectoryInfo dir = (DirectoryInfo)entity.Info; // Use explisit cast from FileSystemInfo into DirectoryInfo
                        entity.Size = GetDirectorySize(dir); // Start methods for calculating directory size and persantage
                        entity.Persantage = entity.SubDirecory == null ? String.Empty : (100 * (float)GetDirectorySize(dir) / GetDirectorySize(dir.Parent)).ToString() + "%";
                    }
                    else // If we work with file
                    {
                        FileInfo file = (FileInfo)entity.Info; // Use explisit cast from FileSystemInfo into FileInfo
                        entity.Size = file.Length; // Calculate file size and persanatge
                        entity.Persantage = (100 * (float)file.Length / GetDirectorySize(file.Directory)).ToString() + "%";
                    }
                }
            }
            else // If we want to proceed asynchronously
            {
                foreach (var entity in entities)  // Check every entity
                {
                    ThreadPool.QueueUserWorkItem(TaskForAnAsyncCalculation, entity); // Add method for calculating size into the ThreadPool

                    while (true) // Method for checking whether we have an open thread
                    {
                        ThreadPool.GetAvailableThreads(out int currentAvailableThreads, out _); // Get all available threads at the moment

                        // If the difference between max threads count and current available threads < number of user number of threads ->
                        // -> continue foreach cycle iteration
                        if (numberOfSystemThreads - currentAvailableThreads < numberOfThreadsToProceed) 
                            break;
                    }
                }
                while (true) // Cycle need to check if we have some working threads at the moment
                {
                    ThreadPool.GetAvailableThreads(out int currentAvailableThreadsCount, out _);
                    if (currentAvailableThreadsCount != numberOfSystemThreads) // If we have some of them working
                        Thread.Sleep(100); // Wait 100 ms and check again (immitation of waiting our unfinished threads) 
                    else
                        break; // If there is no -> do not wait
                }
            }
        }

        // Method to use in the ThreadPool for proceeding calculation asynchroniously
        static void TaskForAnAsyncCalculation(object entityObj)
        {
            Entity entity = (Entity)entityObj; // Explicit cast from object type to the entity
            if (entity.Type == EntityType.Directory) // If we work with directory entity
            {
                DirectoryInfo dir = (DirectoryInfo)entity.Info; // Use explisit cast from FileSystemInfo into DirectoryInfo
                entity.Size = GetDirectorySize(dir); // Start methods for calculating directory size and persantage
                entity.Persantage = entity.SubDirecory == null ? String.Empty : (100 * (float)GetDirectorySize(dir) / GetDirectorySize(dir.Parent)).ToString() + "%";
            }
            else
            {
                FileInfo file = (FileInfo)entity.Info; // Use explisit cast from FileSystemInfo into FileInfo
                entity.Size = file.Length; // Calculate file size and persanatge
                entity.Persantage = (100 * (float)file.Length / GetDirectorySize(file.Directory)).ToString() + "%";
            }
        }

        // Method for working with directories and files in the head directory and proceed through them
        static void GetDirectoryIerarchy(List<Entity> entities, List<string> fileNames_HeadDirectory, FileSystemInfo[] filesAndDirectories_HeadDirectory)
        {
            // Check every file in the directory
            foreach (var item in filesAndDirectories_HeadDirectory)
            {
                if (fileNames_HeadDirectory.Contains(item.Name)) // If we work with file -> Add it as an entity
                    entities.Add(CreateEntityFromFile((FileInfo)item));
                else
                    ProceedDirectory(entities, (DirectoryInfo)item); // If we work with directory -> Start method for proceeding the directory

            }
        }

        // Method for calculating size of directory
        static long GetDirectorySize(DirectoryInfo dir)
        {
            // Set current size as 0
            long size = 0;

            // Get all the files and directories in the selected directory
            FileInfo[] files = dir.GetFiles(); 
            DirectoryInfo[] directories = dir.GetDirectories();

            // Add to the directory size variable, size of the current file
            foreach (var file in files)
                size += file.Length;

            // Add to the directory size variable, size of the current directory
            foreach (var directory in directories)
                size += GetDirectorySize(directory); // Before adding -> calculate the size using method

            // Return the result size
            return size;
        }

        // Method for creating Entity object from FileInfo object using standart constructor 
        static Entity CreateEntityFromFile(FileInfo file)
        {
            return new Entity
            {
                Info = file, // Save FileInfo for future size and persantage calculating
                Name = file.Name,
                Type = file.Extension == ".txt" ? EntityType.TextFile : EntityType.File,
                SubDirecory = file.Directory,
                //Size = file.Length,
                //Persantage = (100 * (float)file.Length / GetDirectorySize(file.Directory)).ToString() + "%"
            };
        }

        // Method for creating Entity object from DirectoryInfo object using standart constructor 
        static Entity CreateEntityFromDirectory(DirectoryInfo dir, bool isHeadDirectory = false)
        {
            return new Entity
            {
                Info = dir, // Save DirectoryInfo for future size and persantage calculating
                Name = dir.Name,
                Type = EntityType.Directory,
                SubDirecory = isHeadDirectory ? null : dir.Parent, // If we work with head directory -> subDirectory is null. Otherwise -> Parent
                //Size = GetDirectorySize(dir),
                //Persantage = isHeadDirectory ? String.Empty : (100 * (float)GetDirectorySize(dir) / GetDirectorySize(dir.Parent)).ToString() + "%"
            };
        }

        // Recursive method for proceeding the normal directories and inside directories aswell
        static void ProceedDirectory(List<Entity> entities, DirectoryInfo dir)
        {
            // Add to the list Entity? created from DirectoryInfo variable
            entities.Add(CreateEntityFromDirectory(dir));

            // Get files in the current folder
            var files_SubDirectory = dir.GetFiles();

            // Get all the files (normal files and directories) in the current directory
            var filesAndDirectories_SubDirectory = dir.GetFileSystemInfos();

            // Get all normal files name and save it into new list
            var fileNames_SubDirectory = new List<string>();
            foreach (var file in files_SubDirectory)
                fileNames_SubDirectory.Add(file.Name);

            // Check every file and directory in current directory
            foreach (var item in filesAndDirectories_SubDirectory)
            {
                // If we work with a normal file -> Save it in the main list
                if (fileNames_SubDirectory.Contains(item.Name))
                    entities.Add(CreateEntityFromFile((FileInfo)item));
                else
                    ProceedDirectory(entities, (DirectoryInfo)item); // If we work with directory -> start ProceedDirectory method
                                                                     // using the recursion with the new (current subDir) parameter

            }
        }

        // Method for printing all the results on the console 
        static void PrintEntities(List<Entity> entities, ref int index, ref string indent, DirectoryInfo subDir)
        {
            // Proceed through all list of entities
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
                    if (subDir == null || entities[index].SubDirecory.FullName.Contains(entities[index - 1].SubDirecory.FullName))
                    {
                        indent += "\t";
                        PrintEntities(entities, ref index, ref indent, entities[index].SubDirecory);
                    }
                    else
                    {
                        indent = indent.Length >= 2 ? indent.Remove(0, 2) : indent;
                        return;
                    }
                }
            }
        }
    }

    // Enum for saving the type of entities
    public enum EntityType
    {
        Directory = 1,
        File = 2,
        TextFile = 3
    }

    // Main entity class (files and directories)
    public class Entity
    {
        public FileSystemInfo Info { get; set; } // Системная информация о сущности
        public string Name { get; set; } // Имя
        public EntityType Type { get; set; } // Тип сущности
        public DirectoryInfo SubDirecory { get; set; } // Каталог, в котором содержится сущность (null для головной)
        public long? Size { get; set; } = null; // размер (в байтах)
        public string Persantage { get; set; } = null; // размер (в процентах от всего содержимого каталога)

        public Entity() { }

    }

}
