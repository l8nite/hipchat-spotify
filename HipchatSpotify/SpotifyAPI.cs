﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;

namespace HipchatSpotify
{
    public class SpotifyAPI
    {
        private readonly string _oauth;
        private readonly string _host;
        private readonly WebClient wc = new WebClient();
        private string _cfid;
        private string _uri = string.Empty;
        private int _wait = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyAPI"/> class. 
        /// Initializes a new SpotifyAPI object which can be used to recieve
        /// </summary>
        /// <param name="OAuth">
        /// Use 
        /// <seealso cref="SpotifyAPI.GetOAuth()"/>
        /// to get this, Or specify your own
        /// </param>
        /// <param name="Host">
        /// Most of the time 127.0.0.1, or for lulz use something like my-awesome-program.spotilocal.com
        /// </param>
        public SpotifyAPI(string OAuth, string Host)
        {
            _oauth = OAuth;
            _host = Host;

            // emulate the embed code [NEEDED]
            wc.Headers.Add("Origin", "https://embed.spotify.com");
            wc.Headers.Add("Referer", "https://embed.spotify.com/?uri=spotify:track:5Zp4SWOpbuOdnsxLqwgutt");
        }

        /// <summary>
        /// Gets the current Unix Timestamp
        /// Mostly for internal use
        /// </summary>
        public int TimeStamp
        {
            get { return Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds); }
        }

        /// <summary>
        /// Gets the 'CFID', a unique identifier for the current session.
        /// Note: It's required to get the CFID before making any other calls
        /// </summary>
        public Responses.CFID CFID
        {
            get
            {
                string a = recv("simplecsrf/token.json");
                List<Responses.CFID> d = (List<Responses.CFID>)JsonConvert.DeserializeObject(a, typeof(List<Responses.CFID>));
                _cfid = d[0].token;
                return d[0];
            }
        }

        /// <summary>
        /// Used by SpotifyAPI.Play to play Spotify URI's
        /// Change this URI and then call SpotifyAPI.Play
        /// </summary>
        public string URI
        {
            get { return _uri; }
            set { _uri = value; }
        }

        /// <summary>
        /// Plays a certain URI and returns the status afterwards
        /// Change SpotifyAPI.URI into the needed uri!
        /// </summary>
        public Responses.Status Play
        {
            get
            {
                string a = recv("remote/play.json?uri=" + URI, true, true, -1);
                List<Responses.Status> d = (List<Responses.Status>)JsonConvert.DeserializeObject(a, typeof(List<Responses.Status>));
                return d[0];
            }
        }

        /// <summary>
        /// Resume Spotify playback and return the status afterwards 
        /// </summary>
        public Responses.Status Resume
        {
            get
            {
                string a = recv("remote/pause.json?pause=false", true, true, -1);
                List<Responses.Status> d = (List<Responses.Status>)JsonConvert.DeserializeObject(a, typeof(List<Responses.Status>));
                return d[0];
            }
        }

        /// <summary>
        /// Pause Spotify playback and return the status afterwards
        /// </summary>
        public Responses.Status Pause
        {
            get
            {
                string a = recv("remote/pause.json?pause=true", true, true, -1);
                List<Responses.Status> d = (List<Responses.Status>)JsonConvert.DeserializeObject(a, typeof(List<Responses.Status>));
                return d[0];
            }
        }

        /// <summary>
        /// Returns the current track info.
        /// Change <seealso cref="Wait"/> into the amount of waiting time before it will return
        /// When the current track info changes it will return before elapsing the amount of seconds in <seealso cref="Wait"/>
        /// (look at the project site for more information if you do not understand this)
        /// </summary>
        public Responses.Status Status
        {
            get
            {
                string a = recv("remote/status.json", true, true, _wait);
                List<Responses.Status> d = (List<Responses.Status>)JsonConvert.DeserializeObject(a, typeof(List<Responses.Status>));
                return d[0];
            }
        }

        /// <summary>
        /// Please see <seealso cref="Status"/> for more information
        /// </summary>
        public int Wait
        {
            get { return _wait; }
            set { _wait = value; }
        }

        /// <summary>
        /// Recieves client version information.
        /// Doesn't require a OAuth/CFID
        /// </summary>
        public Responses.ClientVersion ClientVersion
        {
            get
            {
                string a = recv("service/version.json?service=remote");
                List<Responses.ClientVersion> d =
                    (List<Responses.ClientVersion>)JsonConvert.DeserializeObject(a, typeof(List<Responses.ClientVersion>));
                return d[0];
            }
        }

        /// <summary>
        /// Recieves a OAuth key from the Spotify site
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetOAuth()
        {
            var raw = new WebClient().DownloadString("https://open.spotify.com/token");
            var token = JsonConvert.DeserializeObject<Responses.Token>(raw);

            return token.t;
        }

        /// <summary>
        /// Get a link to the 640x640 cover art image of a spotify album
        /// </summary>
        /// <param name="uri">
        /// The Spotify album URI
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string getArt(string uri)
        {
            try
            {
                string raw =
                    new WebClient().DownloadString("http://open.spotify.com/album/" + uri.Split(new[] { ":" }, StringSplitOptions.None)[2]);
                raw = raw.Replace("\t", string.Empty);
                
                string[] lines = raw.Split(new[] { "\n" }, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    if (line.StartsWith("<meta property=\"og:image\""))
                    {
                        string[] l = line.Split(new[] { "/" }, StringSplitOptions.None);
                        return "http://o.scdn.co/640/" + l[4].Replace("\"", string.Empty).Replace(">", string.Empty);
                    }
                }
            }
            catch
            {
                return string.Empty;
            }

            return string.Empty;
        }

        private string recv(string request)
        {
            return recv(request, false, false, -1);
        }

        private string recv(string request, bool oauth, bool cfid)
        {
            return recv(request, oauth, cfid, -1);
        }

        private string recv(string request, bool oauth, bool cfid, int wait)
        {
            string parameters = "?&ref=&cors=&_=" + TimeStamp;
            if (request.Contains("?"))
            {
                parameters = parameters.Substring(1);
            }

            if (oauth)
            {
                parameters += "&oauth=" + _oauth;
            }

            if (cfid)
            {
                parameters += "&csrf=" + _cfid;
            }

            if (wait != -1)
            {
                parameters += "&returnafter=" + wait;
                parameters += "&returnon=login%2Clogout%2Cplay%2Cpause%2Cerror%2Cap";
            }

            string a = "http://" + _host + ":4380/" + request + parameters;
            string derp = string.Empty;
            try
            {
                derp = wc.DownloadString(a);
                derp = "[ " + derp + " ]";
            }
            catch (Exception z)
            {
                // perhaps spotifywebhelper isn't started (happens sometimes)
                if (Process.GetProcessesByName("SpotifyWebHelper").Length < 1)
                {
                    try
                    {
                        Process.Start(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Spotify\\Data\\SpotifyWebHelper.exe");
                    }
                    catch (Exception dd)
                    {
                        throw new Exception(
                            "Could not launch SpotifyWebHelper. Your installation of Spotify might be corrupt or you might not have Spotify installed", 
                            dd);
                    }

                    return recv(request, oauth, cfid);
                }
                    
                    // spotifywebhelper is running but we still can't connect, wtf?!
                throw new Exception("Unable to connect to SpotifyWebHelper", z);
            }

            return derp;
        }
    }
}