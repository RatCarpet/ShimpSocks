using Newtonsoft.Json;
using System;
using System.Data;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using Tweetinvi.Exceptions;

namespace BoogleChip
{
    internal class Program
    {
        static async Task Main(string[] args)
        {     
            //Boogle
            string userId = Environment.UserName;
            string folderPath = @"C:\Users\" + userId + @"\Desktop\ArtInProgress";
            string logFilePath = folderPath + "\\BoogleChipLogFile.json";
            string keyFilePath = folderPath + "\\BoogleChipKeyFile.json";           
            
            Dictionary<string, DateTime> currentFiles = new Dictionary<string, DateTime>();
            Dictionary<string, DateTime> unchangedFiles = new Dictionary<string, DateTime>();
            Dictionary<string, DateTime> changedFiles = new Dictionary<string, DateTime>();

            //grab all .clib files from art directory
            foreach (string f in Directory.GetFiles(folderPath))
            {
                if (Path.GetExtension(f) == ".clip")
                {
                    FileInfo fileInfo = new FileInfo(f);
                    currentFiles.Add(fileInfo.Name, fileInfo.LastWriteTime);
                }
            }

            //If log file does not exist create it and populate with current
            if (!File.Exists(logFilePath))
            {
                File.Create(logFilePath).Close();
                string jsonInitial = JsonConvert.SerializeObject(currentFiles, Formatting.Indented);
                File.WriteAllText(logFilePath, jsonInitial);
                changedFiles = currentFiles;
            }
            else
            {
                //Read old log and write to the "existing" dictionary
                string getOldLog = File.ReadAllText(logFilePath);
                unchangedFiles = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(getOldLog);

                //compare existing log to current log 
                changedFiles = currentFiles.Except(unchangedFiles).ToDictionary(x => x.Key, y => y.Value);
                Dictionary<string, DateTime> newLog = changedFiles.Concat(unchangedFiles.Where(x => !changedFiles.Keys.Contains(x.Key))).ToDictionary(x => x.Key, y => y.Value);
                //Write new log
                string setJson = JsonConvert.SerializeObject(newLog);
                File.WriteAllText(logFilePath, setJson);
            }
              
            //Export png
            List<string> imageFileNames = new List<string>();
            foreach (KeyValuePair<string, DateTime> file in changedFiles)
            {
                imageFileNames.Add(Export.MakePng(file, folderPath));
            }

            //Grab keys from keyFile
            TwitterKeys twitterKeys = new TwitterKeys();
            if (!File.Exists(keyFilePath))
            {
                throw new FileNotFoundException(keyFilePath);
            }
            else
            {
                string getKeys = File.ReadAllText(keyFilePath);
                twitterKeys = JsonConvert.DeserializeObject<TwitterKeys>(getKeys);                
            }            
            
            //post to twitter
            foreach (string fileName in imageFileNames)
            {
                FileInfo fileInfo = new FileInfo(fileName);
                string tweetText = "WIP " + fileInfo.Name;
                byte[] image = File.ReadAllBytes(fileName);

                try
                {
                    TwitterClient userClient = new TwitterClient(twitterKeys.oauth_consumer_key, twitterKeys.oauth_consumer_secret, twitterKeys.oauth_token, twitterKeys.oauth_secret);
                    IMedia uploadImage = await userClient.Upload.UploadTweetImageAsync(image);
                    ITweet TweetImage = await userClient.Tweets.PublishTweetAsync(new PublishTweetParameters(tweetText) { Medias = { uploadImage } });
                }
                catch (TwitterException e)
                {
                    Console.WriteLine(e.ToString());
                }                
            }
            Environment.Exit(0);
        }

        public class TwitterKeys
        {
            public string oauth_consumer_key { get; set; }
            public string oauth_consumer_secret { get; set; }
            public string oauth_token { get; set; }
            public string oauth_secret { get; set; }
        }
    }
}
