using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;

namespace BoogleChip
{
    internal class Export
    {
        public static string MakePng(KeyValuePair<string, DateTime> file, string folderPath)
        {

            string fileName = file.Key.ToString();
            string fileDate = file.Value.ToString("yyyy-M-dd hhmmss");

            string filePath = folderPath + "\\" + fileName;

            FileInfo fileInfo = new FileInfo(filePath);

            string imageDirectory = folderPath + "\\ExportedArt";
            if (!Directory.Exists(imageDirectory))
            {
                Directory.CreateDirectory(imageDirectory);
            }

            string imageFileName = imageDirectory + "\\" + fileName.Substring(0, fileName.LastIndexOf('.')) + " " + fileDate + ".png";

            //sqlite header
            byte[] sqlHeader = new byte[] { 0x53, 0x51, 0x4c, 0x69, 0x74, 0x65, 0x20, 0x66, 0x6f, 0x72, 0x6d, 0x61, 0x74, 0x20, 0x33 };
            int headerIndex = 0;
            int headerLocation = 0;

            FileStream fileStream = File.Open(filePath, FileMode.Open);

            //Find sqlite header in clip file
            while (true)
            {
                int fileByte = fileStream.ReadByte();

                if (fileByte == -1)
                {
                    break;
                }

                if (fileByte == sqlHeader[headerIndex])
                {
                    headerIndex = headerIndex + 1;

                    if (headerIndex == sqlHeader.Length)
                    {
                        //header found
                        fileStream.Position = headerLocation;

                        string sqliteFileName = filePath.Substring(0, filePath.LastIndexOf('.'));
                        sqliteFileName = sqliteFileName + ".sqlite";

                        FileStream temp = File.OpenWrite(sqliteFileName);
                        fileStream.CopyTo(temp);
                        temp.Close();
                        fileStream.Close();

                        //open sqlite connection
                        SqliteConnectionStringBuilder conString = new SqliteConnectionStringBuilder();
                        conString.DataSource = sqliteFileName;
                        conString.DefaultTimeout = 5000;

                        SqliteConnection dbcon = new SqliteConnection(conString.ToString());

                        try
                        {
                            dbcon.Open();

                            string query = "SELECT imageData From CanvasPreview";
                            using (SqliteCommand cmd = dbcon.CreateCommand())
                            {
                                cmd.CommandText = query;
                                FileStream output = File.OpenWrite(imageFileName);
                                output.Write((System.Byte[])cmd.ExecuteScalar());
                                output.Close();
                                cmd.Dispose();
                            }

                        }
                        catch (SqliteException ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        dbcon.Close();
                        SqliteConnection.ClearAllPools();

                        File.Delete(sqliteFileName);
                        break;
                    }
                }
                else
                {
                    headerLocation = headerLocation + headerIndex + 1;
                    headerIndex = 0;
                }                
            }
            return imageFileName;
        }
    }
}
