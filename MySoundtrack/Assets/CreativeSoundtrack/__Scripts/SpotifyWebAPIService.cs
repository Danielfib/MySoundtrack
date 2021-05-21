using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class SpotifyWebAPIService : Singleton<SpotifyWebAPIService>
{
    private const string client_id = "221b1ed7900947d3a412f455a9003fd9"; // Your client id
    private const string client_secret = "13e74dfa5a3446849d91e0c664ba9b3a"; // Your secret
    private const string redirect_uri = "http://localhost:5000/callback"; // Your redirect uri

    private EmbedIOAuthServer _server;
    public Action InitializedSpotify;
    private SpotifyClient _spotify;
    public SpotifyClient Spotify { get { return _spotify; } set { _spotify = value; } }

    public async Task StartStuff()
    {
        _server = new EmbedIOAuthServer(new Uri(redirect_uri), 5000);
        await _server.Start();

        _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
        _server.ErrorReceived += OnErrorReceived;

        var request = new LoginRequest(_server.BaseUri, client_id, LoginRequest.ResponseType.Code)
        {
            Scope = new List<string> {
                Scopes.PlaylistReadCollaborative,
                Scopes.PlaylistReadPrivate,
                Scopes.UserLibraryRead,
                Scopes.UserModifyPlaybackState,
                Scopes.UserReadPlaybackPosition,
                Scopes.UserReadPlaybackState,
                Scopes.AppRemoteControl,
                Scopes.Streaming,
                Scopes.PlaylistModifyPrivate,
                Scopes.PlaylistModifyPublic,
                Scopes.UgcImageUpload,
                Scopes.UserFollowModify,
                Scopes.UserFollowRead,
                Scopes.UserLibraryModify,
                Scopes.UserReadCurrentlyPlaying,
                Scopes.UserReadEmail,
                Scopes.UserReadPrivate,
                Scopes.UserReadRecentlyPlayed,
                Scopes.UserTopRead
            }
        };
        BrowserUtil.Open(request.ToUri());
    }

    private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
    {
        await _server.Stop();

        var config = SpotifyClientConfig.CreateDefault();
        var tokenResponse = await new OAuthClient(config).RequestToken(
          new AuthorizationCodeTokenRequest(
            client_id, client_secret, response.Code, new Uri(redirect_uri)
          )
        );

        Spotify = new SpotifyClient(tokenResponse.AccessToken);
        InitializedSpotify.Invoke();
        // do calls with Spotify and save token?
    }

    private async Task OnErrorReceived(object sender, string error, string state)
    {
        Console.WriteLine($"Aborting authorization, error received: {error}");
        await _server.Stop();
    }

    public async Task PlayTrack(FullTrack track)
    {
        CurrentlyPlayingContext player = await Spotify.Player.GetCurrentPlayback();
        if (!player.IsPlaying)
        {
            await Spotify.Player.ResumePlayback();
        }
        await Spotify.Player.AddToQueue(new PlayerAddToQueueRequest(track.Uri));
        await Spotify.Player.SkipNext();
    }

    public async Task<List<FullTrack>> GetUserSavedTracks(int howMany, int offset = 0)
    {
        LibraryTracksRequest req = new LibraryTracksRequest();
        req.Limit = howMany;
        req.Offset = offset;
        var result = await Spotify.Library.GetTracks(req);
        List<FullTrack> tracks = new List<FullTrack>();
        if (result.Items.Count > 0)
        {
            tracks = result.Items.Select(x => x.Track).ToList();
        }
        return tracks;
    }

    public async Task<List<FullTrack>> GetAllUserSavedTracks()
    {
        List<FullTrack> tracks = new List<FullTrack>();

        int offset = 0;
        var tracksToAdd = await GetUserSavedTracks(50, offset);
        while (tracksToAdd.Count > 0)
        {
            tracks.AddRange(tracksToAdd);
            offset += 50;
            tracksToAdd = await GetUserSavedTracks(50, offset);
        }
        return tracks;
    }

    public async Task<int> GetPlaybackTimeToEnd()
    {
        var pb = await Spotify.Player.GetCurrentPlayback();
        FullTrack currentPlayingTrack = pb.Item as FullTrack;
        return currentPlayingTrack.DurationMs - pb.ProgressMs;
    }

    public async Task<List<TrackAudioFeatures>> GetSeveralAudioFeatures(List<string> tracksIds)
    {
        var req = new TracksAudioFeaturesRequest(tracksIds);
        var response = await Spotify.Tracks.GetSeveralAudioFeatures(req);
        return response.AudioFeatures;
    }

    public async void Connect()
    {
        await StartStuff();
        //InitializedSpotify += async () =>
        //{
        //    var oi = await GetAllUserLikedSongs();
        //};
    }

    private void Start()
    {
        //Connect();
    }

    private void OnDestroy()
    {
        //TODO: Maybe use SimpleTrack instead of FullTrack?
    }
}
