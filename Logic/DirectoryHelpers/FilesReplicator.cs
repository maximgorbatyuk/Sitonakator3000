﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Logic.Arhivator;

namespace Logic.DirectoryHelpers
{
    public class FilesReplicator
    {
        private readonly string _sourceDirectory;

        private readonly string _targetDirectory;

        public IReadOnlyCollection<string> DirectoryNamesToIgnore { private get; set; }

        public FilesReplicator(string sourceDirectory, string targetDirectory)
        {
            _sourceDirectory = sourceDirectory;
            _targetDirectory = targetDirectory;
        }

        public async Task CopyAllAsync()
        {
            CheckSourceDirectoryForContent();

            IReadOnlyCollection<FileCopyInfo> filesToCopy = GetAll();

            var tasks = new List<Task>();
            foreach (FileCopyInfo fileCopyInfo in filesToCopy)
            {
                tasks.Add(fileCopyInfo.CopyAsync());
            }

            await Task.Run(() =>
            {
                Task.WaitAll(tasks.ToArray());
            });
        }

        public async Task CopyAllAsync(Action<AsyncActionResult> copyFinishedCallback)
        {
            if (copyFinishedCallback == null)
                throw new ArgumentNullException(paramName: nameof(copyFinishedCallback));

            try
            {
                await CopyAllAsync();

                copyFinishedCallback(AsyncActionResult.Success());
            }
            catch (Exception ex)
            {
                copyFinishedCallback(AsyncActionResult.Fail(ex));
            }
        }

        public async Task RemoveAllContentAsync(string directoryPath)
        {
            await Task.Run(() =>
            {
                var directoryInfo = new DirectoryInfo(directoryPath);

                // https://stackoverflow.com/a/1288747
                foreach (FileInfo file in directoryInfo.EnumerateFiles())
                    file.Delete();

                IEnumerable<DirectoryInfo> dirsToRemove = GetSubDirectoriesWithoutIngored(directoryInfo);

                foreach (DirectoryInfo dir in dirsToRemove)
                    dir.Delete(true);
            });
        }

        private IReadOnlyCollection<FileCopyInfo> GetAll()
        {
            var result = new List<FileCopyInfo>();
            PerformDeepCopyScanning(_sourceDirectory, _targetDirectory, ref result);

            return result;
        }

        private void CheckSourceDirectoryForContent()
        {
            var sourceDirInfo = new DirectoryInfo(_sourceDirectory);

            if (!sourceDirInfo.EnumerateFiles().Any() || !GetSubDirectoriesWithoutIngored(sourceDirInfo).Any())
                throw new InvalidOperationException($@"В папке-источнике {_sourceDirectory} не обнаружено никаких файлов или папок");
        }

        // https://social.msdn.microsoft.com/Forums/vstudio/en-US/dab86e37-a25b-4bdb-9552-7e6c7ed509c7/how-to-copy-files-and-directories-recursively?forum=csharpgeneral
        private void PerformDeepCopyScanning(string sourceDirectory, string destinationDirectory, ref List<FileCopyInfo> results)
        {
            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            var sourceDir = new DirectoryInfo(sourceDirectory);
            var targetDir = new DirectoryInfo(destinationDirectory);

            FileInfo[] fileInfos = sourceDir.GetFiles();

            foreach (FileInfo fileInfo in fileInfos)
                results.Add(new FileCopyInfo(fileInfo, targetDir));

            IEnumerable<DirectoryInfo> directories = GetSubDirectoriesWithoutIngored(sourceDir);

            foreach (DirectoryInfo dir in directories)
            {
                DirectoryInfo subDirectory = targetDir.CreateSubdirectory(dir.Name);
                PerformDeepCopyScanning(dir.FullName, subDirectory.FullName, ref results);
            }
        }

        private IEnumerable<DirectoryInfo> GetSubDirectoriesWithoutIngored(DirectoryInfo sourceDir)
        {
            var directories = sourceDir.EnumerateDirectories();
            if (DirectoryNamesToIgnore?.Count > 0)
                directories = directories.Where(x => !DirectoryNamesToIgnore.Contains(x.Name));

            return directories;
        }
    }
}