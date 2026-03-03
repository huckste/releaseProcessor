namespace ReleaseProcessor.Services;

public class Verify
{
    public static List<string> Directories()
    {
        List<string> dirList = [];
        List<string> ptfDirs = [];

        string scriptsDir = "/home/huckste/Scripts/";
        string scriptsArchive = $"{scriptsDir}Archive";
        string scriptsSinglePick = $"{scriptsDir}Single Pick";
        string scriptsFailed = $"{scriptsDir}Failed";
        string scriptsLogs = $"{scriptsDir}Logs";
        string scriptsBuild = $"{scriptsDir}Build";

        string scriptsPTFBuild = $"{scriptsDir}ind-as10/PrintToFile/Build";
        string scriptsPTFComplete = $"{scriptsDir}ind-as10/PrintToFile/Complete";
        string scriptsPTFArchive = $"{scriptsDir}ind-as10/PrintToFile/Archive";
        string scriptsIndfs01 = $"{scriptsDir}indfs01/SinglePick";
        string scriptsPTFDirs = $"{scriptsDir}ind-as10/BARPRN/PTF";

        string ptfParent = $"{scriptsPTFDirs}/PTF0";

        dirList.Add(scriptsDir);
        dirList.Add(scriptsArchive);
        dirList.Add(scriptsSinglePick);
        dirList.Add(scriptsFailed);
        dirList.Add(scriptsLogs);
        dirList.Add(scriptsPTFBuild);
        dirList.Add(scriptsPTFComplete);
        dirList.Add(scriptsPTFArchive);
        dirList.Add(scriptsIndfs01);
        dirList.Add(scriptsBuild);

        for (var i = 1; i <= 5; i++)
        {
            dirList.Add($"{ptfParent}{i}");
            ptfDirs.Add($"{ptfParent}{i}");
        }

        foreach (var dir in dirList)
        {
            Directory.CreateDirectory(dir);
        }

        return ptfDirs;
    }

    public static List<string> ProductionDirs()
    {
        List<string> dirList = [];
        List<string> ptfDirs = [];

        // Create zip of files in PTF child Folders and place in prnProcArchive
        // Create zip of files in printToFileComplete and place in printToFileArchive
        // Move files in printToFileComplete to prnDropLocation
        // delete all files in PTF child Folders
        // delete all files in printToFileComplete

        var singlePickLocation = "C:\\Single Pick";
        var printToFile = "\\IND-AS10\\BARPRN\\PrintToFile";
        var prnProcArchive = "\\IND-AS10\\BARPRN\\prnproc_archive";
        var ptf = "\\IND-AS10\\BARPRN\\PTF\\PTFO";
        var printToFileArchive = $"{printToFile}\\Archive";
        var printToFileBuild = $"{printToFile}\\Build";
        var printToFileComplete = $"{printToFile}\\Complete";
        var prnDropLocation = "\\indfs01\\SinglePick";

        dirList.Add(singlePickLocation);
        dirList.Add(printToFileArchive);
        dirList.Add(printToFileBuild);
        dirList.Add(printToFileComplete);
        dirList.Add(prnDropLocation);
        dirList.Add(prnProcArchive);

        for (var i = 1; i <= 5; i++)
        {
            dirList.Add($"{ptf}{i}");
            ptfDirs.Add($"{ptf}{i}");
        }

        return ptfDirs;
    }

    public static void SinglePickFile()
    {
        string scriptsDir = "/home/huckste/Scripts/";
        string singlePickPath = $"{scriptsDir}Single Pick/SINGLEPICK.POP";

        if (!File.Exists(singlePickPath))
            throw new FileNotFoundException("File not found", singlePickPath);
    }

    public void PTFFlodersCleared() { }

    public void FilesMovedSuccessfully() { }

    public void AllFilesCompletedSuccessfully() { }

    public void FailedFilesFolderIsEmpty() { }
}
