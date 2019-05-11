﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace FMBot.Bot
{
    
    public static class FMBotUtil
    {
        #region Database Functions

        public static class DBase
        {
            #region User Settings


            public static async Task<IGuildUser> ConvertIDToGuildUser(IGuild guild, ulong id)
            {
                IReadOnlyCollection<IGuildUser> users = await guild.GetUsersAsync().ConfigureAwait(false);

                foreach (IGuildUser user in users)
                {
                    if (user.Id == id)
                    {
                        return user;
                    }
                }

                return null;
            }

            #endregion




            #region Friend List Settings

            public static bool FriendsExists(string id)
            {
                return File.Exists(GlobalVars.CacheFolder + id + "-friends.txt");
            }

            public static int AddFriendsEntry(string id, params string[] friendlist)
            {
                if (!FriendsExists(id))
                {
                    File.Create(GlobalVars.CacheFolder + id + "-friends.txt").Dispose();
                    File.SetAttributes(GlobalVars.CacheFolder + id + "-friends.txt", FileAttributes.Normal);
                }

                string[] friends = File.ReadAllLines(GlobalVars.CacheFolder + id + "-friends.txt");

                int listcount = friendlist.Length;

                List<string> list = new List<string>(friends);

                foreach (string friend in friendlist)
                {
                    if (!friends.Contains(friend))
                    {
                        list.Add(friend);
                    }
                    else
                    {
                        listcount--;
                        continue;
                    }
                }

                friends = list.ToArray();

                File.WriteAllLines(GlobalVars.CacheFolder + id + "-friends.txt", friends);
                File.SetAttributes(GlobalVars.CacheFolder + id + "-friends.txt", FileAttributes.Normal);

                return listcount;
            }

            public static int RemoveFriendsEntry(string id, params string[] friendlist)
            {
                if (!FriendsExists(id))
                {
                    return 0;
                }

                string[] friends = File.ReadAllLines(GlobalVars.CacheFolder + id + "-friends.txt");
                string[] friendsLower = friends.Select(s => s.ToLowerInvariant()).ToArray();

                int listcount = friendlist.Length;

                List<string> list = new List<string>(friends);


                foreach (string friend in friendlist)
                {
                    if (friendsLower.Contains(friend.ToLower()))
                    {
                        list.RemoveAll(n => n.Equals(friend, StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        listcount--;
                        continue;
                    }
                }

                friends = list.ToArray();

                File.WriteAllLines(GlobalVars.CacheFolder + id + "-friends.txt", friends);
                File.SetAttributes(GlobalVars.CacheFolder + id + "-friends.txt", FileAttributes.Normal);

                return listcount;
            }

            public static string[] GetFriendsForID(string id)
            {
                string[] lines = File.ReadAllLines(GlobalVars.CacheFolder + id + "-friends.txt");
                return lines;
            }

            #endregion

            #region Global Settings

            public static int GetIntForModeName(string mode)
            {
                if (mode.Equals("embedmini"))
                {
                    return 0;
                }
                else if (mode.Equals("embedfull"))
                {
                    return 1;
                }
                else if (mode.Equals("textfull"))
                {
                    return 2;
                }
                else if (mode.Equals("textmini"))
                {
                    return 3;
                }
                else if (mode.Equals("userdefined"))
                {
                    return 4;
                }
                else
                {
                    return 4;
                }
            }

            public static string GetNameForModeInt(int mode, bool isservercmd = false)
            {
                if (mode == 0)
                {
                    return "embedmini";
                }
                else if (mode == 1)
                {
                    return "embedfull";
                }
                else if (mode == 2)
                {
                    return "textfull";
                }
                else if (mode == 3)
                {
                    return "textmini";
                }
                else if ((mode > 3 || mode < 0) && isservercmd)
                {
                    return "userdefined";
                }
                else
                {
                    return "NULL";
                }
            }

            #endregion
        }

        #endregion

        #region Configuration Data

        public static class JsonCfg
        {
            // this structure will hold data from config.json
            public struct ConfigJson
            {
#pragma warning disable RCS1170 // Use read-only auto-implemented property.
                [JsonProperty("token")]
                public string Token { get; private set; }

                [JsonProperty("fmkey")]
                public string FMKey { get; private set; }

                [JsonProperty("fmsecret")]
                public string FMSecret { get; private set; }

                [JsonProperty("prefix")]
                public string CommandPrefix { get; private set; }

                [JsonProperty("baseserver")]
                public string BaseServer { get; private set; }

                [JsonProperty("announcementchannel")]
                public string AnnouncementChannel { get; private set; }

                [JsonProperty("featuredchannel")]
                public string FeaturedChannel { get; private set; }

                [JsonProperty("botowner")]
                public string BotOwner { get; private set; }

                [JsonProperty("timerinit")]
                public string TimerInit { get; private set; }

                [JsonProperty("timerrepeat")]
                public string TimerRepeat { get; private set; }

                [JsonProperty("spotifykey")]
                public string SpotifyKey { get; private set; }

                [JsonProperty("spotifysecret")]
                public string SpotifySecret { get; private set; }

                [JsonProperty("exceptionchannel")]
                public string ExceptionChannel { get; private set; }

                [JsonProperty("cooldown")]
                public string Cooldown { get; private set; }

                [JsonProperty("nummessages")]
                public string NumMessages { get; private set; }

                [JsonProperty("inbetweentime")]
                public string InBetweenTime { get; private set; }

                [JsonProperty("derpikey")]
                public string DerpiKey { get; private set; }
#pragma warning restore RCS1170 // Use read-only auto-implemented property.
            }

            public static async Task<ConfigJson> GetJSONDataAsync()
            {
                // first, let's load our configuration file
                await GlobalVars.Log(new LogMessage(LogSeverity.Info, "JsonCfg", "Loading Configuration")).ConfigureAwait(false);
                string json = "";
                using (FileStream fs = File.OpenRead(GlobalVars.ConfigFileName))
                using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    json = await sr.ReadToEndAsync().ConfigureAwait(false);
                }

                // next, let's load the values from that file
                // to our client's configuration
                return JsonConvert.DeserializeObject<ConfigJson>(json);
            }

            public static ConfigJson GetJSONData()
            {
                // first, let's load our configuration file
                GlobalVars.Log(new LogMessage(LogSeverity.Info, "JsonCfg", "Loading Configuration"));
                string json = "";
                using (FileStream fs = File.OpenRead(GlobalVars.ConfigFileName))
                using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    json = sr.ReadToEnd();
                }

                // next, let's load the values from that file
                // to our client's configuration
                return JsonConvert.DeserializeObject<ConfigJson>(json);
            }
        }

        #endregion

        #region Exception Reporter

        public static class ExceptionReporter
        {
            public static async void ReportException(DiscordSocketClient client = null, Exception e = null)
            {
                JsonCfg.ConfigJson cfgjson = await JsonCfg.GetJSONDataAsync().ConfigureAwait(false);

                try
                {
                    ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                    ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                    SocketGuild guild = client.GetGuild(BroadcastServerID);
                    SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                    EmbedBuilder builder = new EmbedBuilder();
                    builder.AddField("Exception:", e.Message + "\nSource:\n" + e.Source + "\nStack Trace:\n" + e.StackTrace);

                    await channel.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    try
                    {
                        ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                        ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                        SocketGuild guild = client.GetGuild(BroadcastServerID);
                        SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                        await channel.SendMessageAsync("Exception: " + e.Message + "\n\nSource:\n" + e.Source + "\n\nStack Trace:\n" + e.StackTrace).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", "Unable to connect to the server/channel to report error. Look in the log.txt in the FMBot folder to see it.")).ConfigureAwait(false);
                    }
                }

                await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", e.Message, e), true).ConfigureAwait(false);
            }

            public static async void ReportShardedException(DiscordShardedClient client = null, Exception e = null)
            {
                JsonCfg.ConfigJson cfgjson = await JsonCfg.GetJSONDataAsync().ConfigureAwait(false);

                try
                {
                    ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                    ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                    SocketGuild guild = client.GetGuild(BroadcastServerID);
                    SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                    EmbedBuilder builder = new EmbedBuilder();
                    builder.AddField("Exception:", e.Message + "\nSource:\n" + e.Source + "\nStack Trace:\n" + e.StackTrace);

                    await channel.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    try
                    {
                        ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                        ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                        SocketGuild guild = client.GetGuild(BroadcastServerID);
                        SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                        await channel.SendMessageAsync("Exception: " + e.Message + "\n\nSource:\n" + e.Source + "\n\nStack Trace:\n" + e.StackTrace).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", "Unable to connect to the server/channel to report error. Look in the log.txt in the FMBot folder to see it.")).ConfigureAwait(false);
                    }
                }

                await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", e.Message, e), true).ConfigureAwait(false);
            }

            public static async void ReportStringAsException(DiscordShardedClient client, string e)
            {
                JsonCfg.ConfigJson cfgjson = await JsonCfg.GetJSONDataAsync().ConfigureAwait(false);

                try
                {
                    ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                    ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                    SocketGuild guild = client.GetGuild(BroadcastServerID);
                    SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                    EmbedBuilder builder = new EmbedBuilder();
                    builder.AddField("Exception:", e);

                    await channel.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    try
                    {
                        ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                        ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                        SocketGuild guild = client.GetGuild(BroadcastServerID);
                        SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                        await channel.SendMessageAsync("Exception: " + e).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", "Unable to connect to the server/channel to report error. Look in the log.txt in the FMBot folder to see it.")).ConfigureAwait(false);
                    }
                }

                await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", e), true).ConfigureAwait(false);
            }
        }

        #endregion

        #region Global Variables

        public static class GlobalVars
        {
            public static Dictionary<string, string> CensoredAlbums = new Dictionary<string, string>()
            {
                {"Death Grips", "No Love Deep Web"},
                {"ミドリ(Midori)", "あらためまして、はじめまして、ミドリです。(aratamemashite hajimemashite midori desu)"},
                {"Midori", "ratamemashite hajimemashite midori desu"},
                {"ミドリ", "あらためまして、はじめまして、ミドリです。"},
                {"ミドリ", "あらためまして、はじめまして、ミドリです"},
            };
            public static string ConfigFileName = "config.json";
            public static string BasePath = AppDomain.CurrentDomain.BaseDirectory;
            public static string CacheFolder = BasePath + "cache/";
            public static string CoversFolder = BasePath + "covers/";
            public static string FeaturedUserID = "";
            public static int MessageLength = 2000;
            public static int CommandExecutions;
            public static int CommandExecutions_Servers;
            public static int CommandExecutions_DMs;
            public static Hashtable charts = new Hashtable();

            private static bool IsUserInDM;

            public static string GetChartFileName(ulong id)
            {
                return CacheFolder + id.ToString() + "-chart.png";
            }

            public static async Task<MemoryStream> GetChartStreamAsync(ulong id)
            {
                MemoryStream dest = new MemoryStream();
                string fileName = GetChartFileName(id);
                Bitmap chartBitmap = (Bitmap)charts[fileName];
                chartBitmap.Save(dest, System.Drawing.Imaging.ImageFormat.Png);
                dest.Position = 0;

                return dest;
            }

            public static TimeSpan SystemUpTime()
            {
                ManagementObject mo = new ManagementObject(@"\\.\root\cimv2:Win32_OperatingSystem=@");
                DateTime lastBootUp = ManagementDateTimeConverter.ToDateTime(mo["LastBootUpTime"].ToString());
                return DateTime.Now.ToUniversalTime() - lastBootUp.ToUniversalTime();
            }

            public static Task Log(LogMessage arg, bool nowrite = false)
            {
                if (!nowrite)
                {
                    Console.WriteLine(arg);
                }

                NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

                logger.Info(arg);

                return Task.CompletedTask;
            }

            public static string GetLine(string filePath, int line)
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    for (int i = 1; i < line; i++)
                    {
                        sr.ReadLine();
                    }

                    return sr.ReadLine();
                }
            }

            public static string MultiLine(params string[] args)
            {
                return string.Join(Environment.NewLine, args);
            }

            public static Bitmap Combine(List<Bitmap> images, bool vertical = false)
            {
                //read all images into memory
                Bitmap finalImage = null;

                try
                {
                    int width = 0;
                    int height = 0;

                    foreach (Bitmap image in images.ToArray())
                    {
                        //create a Bitmap from the file and add it to the list
                        Bitmap bitmap = image;

                        //update the size of the final bitmap
                        if (vertical)
                        {
                            width = bitmap.Width > width ? bitmap.Width : width;
                            height += bitmap.Height;
                        }
                        else
                        {
                            width += bitmap.Width;
                            height = bitmap.Height > height ? bitmap.Height : height;
                        }

                        images.Add(bitmap);
                    }

                    //create a bitmap to hold the combined image
                    finalImage = new Bitmap(width, height);

                    //get a graphics object from the image so we can draw on it
                    using (Graphics g = Graphics.FromImage(finalImage))
                    {
                        //set background color
                        g.Clear(System.Drawing.Color.Black);

                        //go through each image and draw it on the final image
                        int offset = 0;
                        foreach (Bitmap image in images)
                        {
                            if (vertical)
                            {
                                g.DrawImage(image, new Rectangle(0, offset, image.Width, image.Height));
                                offset += image.Height;
                            }
                            else
                            {
                                g.DrawImage(image, new Rectangle(offset, 0, image.Width, image.Height));
                                offset += image.Width;
                            }
                        }
                    }

                    return finalImage;
                }
                catch (Exception)
                {
                    if (finalImage != null)
                    {
                        finalImage.Dispose();
                    }

                    throw;
                }
                finally
                {
                    //clean up memory
                    foreach (Bitmap image in images)
                    {
                        image.Dispose();
                    }
                }
            }

            public static List<List<Bitmap>> splitBitmapList(List<Bitmap> locations, int nSize)
            {
                List<List<Bitmap>> list = new List<List<Bitmap>>();

                for (int i = 0; i < locations.Count; i += nSize)
                {
                    list.Add(locations.GetRange(i, Math.Min(nSize, locations.Count - i)));
                }

                return list;
            }


            public static async void CheckIfDMBool(ICommandContext Context)
            {
                IDMChannel dm = await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);

                if (dm == null)
                {
                    IsUserInDM = false;
                }

                IsUserInDM = Context.Channel.Name == dm.Name;
            }

            public static bool GetDMBool()
            {
                return IsUserInDM;
            }

            public static IUser CheckIfDM(IUser user, ICommandContext Context)
            {
                CheckIfDMBool(Context);

                if (IsUserInDM)
                {
                    return user ?? Context.Message.Author;
                }
                else
                {
                    return (IGuildUser)user ?? (IGuildUser)Context.Message.Author;
                }
            }

            public static string GetNameString(IUser DiscordUser, ICommandContext Context)
            {
                if (IsUserInDM)
                {
                    return DiscordUser.Username;
                }
                else
                {
                    IGuildUser GuildUser = (IGuildUser)DiscordUser;

                    if (string.IsNullOrWhiteSpace(GuildUser.Nickname))
                    {
                        return GuildUser.Username;
                    }
                    else
                    {
                        return GuildUser.Nickname;
                    }
                }
            }


            public static void ClearReadOnly(DirectoryInfo parentDirectory)
            {
                if (parentDirectory != null)
                {
                    parentDirectory.Attributes = FileAttributes.Normal;
                    foreach (FileInfo fi in parentDirectory.GetFiles())
                    {
                        fi.Attributes = FileAttributes.Normal;
                    }
                    foreach (DirectoryInfo di in parentDirectory.GetDirectories())
                    {
                        ClearReadOnly(di);
                    }
                }
            }
        }

        #endregion
    }
}