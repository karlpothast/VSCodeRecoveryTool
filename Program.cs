using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VSCodeRecoveryTool 
{
  internal class Program
  {
      static void Main(string[] args)
      {
        Restore restore = new Restore();
        restore.Run();
      }
  }
    public class Entry
    {
        public String? id { get; set; }
        public Int64? timestamp { get; set; }
    }

    public class JSONFileRoot
    {
        public Int32? version { get; set; }
        public String? resource { get; set; }
        public List<Entry>? entries { get; set; }
    }

  public class Restore
  {
      public void cw(String? text)
      {
        Console.WriteLine(text);
      }

      public void Run()
      {
        var configurationBuilder = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        IConfiguration configuration = configurationBuilder.Build();
        String? VSCodeHistoryDirectory = configuration["AppSettings:VSCodeHistoryDirectory"];
        String? VSCodeRecoveryDirectory = configuration["AppSettings:VSCodeRecoveryDirectory"];

        if (VSCodeRecoveryDirectory != null)
        {
          try
          {
            Int32 restoredFileCount = 0;
            if (Directory.Exists(VSCodeRecoveryDirectory)) 
            {
              // Uncomment the Directory.Delete and the Directory.CreateDirectory lines to allow recovery directory to be fully refreshed - make sure to configure appsettings.json correctly first
              // Directory.Delete(VSCodeRecoveryDirectory, true); 
            }
            // Directory.CreateDirectory(VSCodeRecoveryDirectory);
    
            if (VSCodeHistoryDirectory != null)
            {
              var vsCodeHistoryDirectoryInfo = new DirectoryInfo(VSCodeHistoryDirectory);
              var vsCodeHistoryDirectories = vsCodeHistoryDirectoryInfo.EnumerateDirectories()
                                  .OrderBy(d => d.LastWriteTime)
                                  .Select(d => d.Name)
                                  .ToList();

              foreach (var snapshotDirectory in vsCodeHistoryDirectories)
              {
                String snapshotDirectoryFullPath = VSCodeHistoryDirectory + "/" + snapshotDirectory;
                var snapshotDirectoryInfo = new DirectoryInfo(snapshotDirectoryFullPath);

                var snapshotFiles = snapshotDirectoryInfo.EnumerateFiles()
                                  .OrderBy(f => f.LastWriteTime)
                                  .Select(f => f.Name)
                                  .ToList();

                Int32 fileCount = 0;
                
                JSONFileRoot? entriesJSONFile;

                foreach (var snapshotFile in snapshotFiles)
                {
                  fileCount+=1;
                  var snapshotFileInfo = new FileInfo(snapshotFile);
                  
                  if (fileCount == 1) 
                  {
                    //export the latest file
                    var jsonFile = snapshotDirectoryFullPath + "/entries.json";
                    String? originalFilePath = "";
                    
                    if (File.Exists(jsonFile)) 
                    { 
                      using (StreamReader reader = new StreamReader(jsonFile))
                      {
                          String jsonFileString = reader.ReadToEnd();
                          if (jsonFileString != null)
                          {
                            entriesJSONFile = JsonConvert.DeserializeObject<JSONFileRoot>(jsonFileString);
                            if (entriesJSONFile != null && entriesJSONFile.entries != null)
                            {
                              List<Entry>? entries = entriesJSONFile.entries;  
                              String? resource = entriesJSONFile.resource;
                              if (resource != null) { originalFilePath = resource.Replace("file://",""); }
                              String? fileId = "";
                              String? fileTimestamp = "";
                              String? snapshotFileFullPath = "";
                              String? restoreToFilePath = "";
                              String? dirCheckString = "";
                              String? originalFileName = "";
                              String? restoreToFullPath = "";

                              if (entries != null)
                              {
                                Int32 i = 0;
                                List<Entry>? entriesOrderedByDateDescList = entries.OrderByDescending(x => x.timestamp).ToList();
                                foreach (Entry? orderedEntry in entriesOrderedByDateDescList)
                                {
                                  restoreToFilePath = "";
                                  snapshotFileFullPath = "";
                                  if (orderedEntry != null && orderedEntry.timestamp != null)
                                  {
                                    if (i == 0) //get first in descending by date list to restore latest file
                                    {
                                      restoreToFilePath = VSCodeRecoveryDirectory + originalFilePath;
                                      fileId = orderedEntry.id;
                                      fileTimestamp = orderedEntry.timestamp.ToString();
                                      originalFileName = "";
                                      restoreToFullPath = "";

                                      //restore original file to full path
                                      snapshotFileFullPath = snapshotDirectoryFullPath + "/" + fileId;

                                      String[] restorePathArray = restoreToFilePath.Split('/');
                                      originalFileName = restorePathArray[restorePathArray.Length - 1];
              
                                      Int32 j = 0;
                                      foreach (var dir in restorePathArray)
                                      {
                                        if (dir != "")
                                        {
                                          if (j == restorePathArray.Length - 1) //end of path array or filename
                                          {
                                            restoreToFullPath = dirCheckString + "/" + dir;
                                            if (File.Exists(restoreToFullPath)) 
                                            { 
                                              // Uncomment the File.Delete and the File.Copy lines to allow recovery directory to be fully refreshed - make sure to configure appsettings.json correctly first
                                              // File.Delete(restoreToFullPath); 
                                            }
                                            // File.Copy(snapshotFileFullPath, restoreToFullPath); restoredFileCount += 1;                                           
                                          }
                                          else
                                          {
                                            dirCheckString += "/" + dir;
                                            if (!Directory.Exists(dirCheckString))
                                            {
                                              Directory.CreateDirectory(dirCheckString);
                                            }
                                          }
                                        }
                                        j+=1;
                                      }
                                    }
                                  }
                                  i+=1;
                                }
                              }
                            }     
                          }
                      }
                    }
                  }
                }
              }
            }
            cw(restoredFileCount.ToString() + " files restored to \n"+VSCodeRecoveryDirectory);
          }
          catch (IOException ioex) 
          {
             cw("IO Exception : " + ioex.Message);
          }
          catch (Exception ex)
          {
             cw("Exception : " + ex.Message); 
          }
        }
      }
  }
}
