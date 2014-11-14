using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using HipChat.Net;
using HipChat.Net.Http;
using Timer = System.Timers.Timer;

namespace HipchatSpotify
{
    internal class Program
    {
        private Timer _timer;
        private readonly HipChatClient _hipchatClient;
        private string _currentSong;
        private readonly SpotifyAPI _spotifyClient;

        private static void Main(string[] args)
        {
            var p = new Program();
            p.Run();
            Console.ReadKey();
        }

        public Program()
        {
            _hipchatClient = new HipChatClient(new ApiConnection(new Credentials("TODO:MAKE_CONFIG_VALUE")));

            var spotifyOAuthToken = SpotifyAPI.GetOAuth();
            _spotifyClient = new SpotifyAPI(spotifyOAuthToken, "127.0.0.1");

            // ReSharper disable once UnusedVariable
            var cfid = _spotifyClient.CFID;
        }

        public void Run()
        {
            Ping(null, null);
            StartTimer();
        }

        private void StartTimer()
        {
            _timer = new Timer(5000)
            {
                Enabled = true,
                AutoReset = true
            };
            _timer.Elapsed += Ping;
            _timer.Start();
        }

        private void Ping(object sender, ElapsedEventArgs e)
        {
            var currentStatus = _spotifyClient.Status;

            if (currentStatus.track != null)
            {
                UpdateCurrentSong(
                    string.Format(
                        "Now listening to {0} - {1} from the album '{2}'",
                        currentStatus.track.track_resource.name,
                        currentStatus.track.artist_resource.name,
                        currentStatus.track.album_resource.name));
            }
        }

        private void UpdateCurrentSong(string text)
        {
            if (_currentSong != null && _currentSong == text)
            {
                return;
            }

            _currentSong = text;
            Console.WriteLine(_currentSong);
            _hipchatClient.Rooms.SendNotificationAsync("934725", _currentSong, false).Wait();
        }
    }
}