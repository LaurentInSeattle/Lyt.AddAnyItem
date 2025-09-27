#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Lyt.FileSystem;
#pragma warning restore IDE0130 

using System.Diagnostics;

// TODO: When on .Net 10, use library 

public static class FileSystemExtensions
{
    public static DriveInfo? DriveInfo(this string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            string root = directoryInfo.Root.ToString();
            if (root.EndsWith('\\'))
            {
                string driveName = root.Replace("\\", string.Empty);
                return new DriveInfo(driveName);
            }
        }

        return null;
    }

    public static long AvailableFreeSpace(this string folderPath)
    {
        if (DriveInfo(folderPath) is DriveInfo driveInfo)
        {
            return driveInfo.AvailableFreeSpace;
        }

        return 0;
    }


    // Enumerates files 
    public static List<string> EnumerateFiles(
        this string folderPath,
        EnumerationOptions enumerationOptions,
        string extension = "*",
        string filter = "")
    {
        try
        {
            if (Directory.Exists(folderPath))
            {
                var files =
                    Directory.EnumerateFiles(folderPath, extension, enumerationOptions);
                var list = new List<string>(16);
                foreach (string file in files)
                {
                    if ((!string.IsNullOrWhiteSpace(filter)) &&
                        (!file.Contains(filter, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }

                    var fileInfo = new FileInfo(file);
                    list.Add(fileInfo.FullName);
                }

                return list;
            }
            else
            {
                Debug.WriteLine("No such directory.");
                return [];
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            return [];
        }
    }

    public static List<string> EnumerateDirectories(this string folderPath)
    {
        try
        {
            if (Directory.Exists(folderPath))
            {

                var directories = Directory.EnumerateDirectories(folderPath);
                var list = new List<string>(16);
                foreach (string directory in directories)
                {
                    var directoryInfo = new DirectoryInfo(directory);
                    list.Add(directoryInfo.FullName);
                }

                return list;
            }
            else
            {
                Debug.WriteLine("No such directory.");
                return [];
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            return [];
        }
    }

    public static string? ChangeFileExtension(this string path, string newExtension)
    {
        try
        {
            if (File.Exists(path))
            {
                string? newPath = Path.ChangeExtension(path, newExtension);
                if (!string.IsNullOrWhiteSpace(newPath))
                {
                    File.Move(path, newPath);
                    return newPath;
                }
                else
                {
                    Debug.WriteLine("Failed to swap extension");
                }
            }
            else
            {
                Debug.WriteLine("No such file.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            if (Debugger.IsAttached) { Debugger.Break(); }
            return ex.Message;
        }

        return string.Empty;
    }
}