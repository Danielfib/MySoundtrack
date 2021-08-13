using SpotifyAPI.Web;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MySoundtrackService;

public class MySoundtrackManager : Singleton<MySoundtrackManager>
{
    [Tooltip("Backup playlists are used in cases where the player doesn't have enough liked songs on his account. These playlists should be public.")]
    public string[] BackupPlaylists;

    private List<FullTrack> m_tracks = new List<FullTrack>();

    [HideInInspector]
    public int currentAreaPlaying = -1;

    List<Tuple<Vector3, Action>> initializationActions = new List<Tuple<Vector3, Action>>();

    private const int SONG_ABOUT_TO_END_TOLERANCE = 4000; //milliseconds

    private Vector3 startingPlayerPos = Vector3.zero;

    private SpotifyWebAPIService service;

    private const int MAX_FEATURED_PLAYLISTS = 5, MAX_TRACKS_PER_FEATURED_PLAYLIST = 90;

    private void Start()
    {
        startingPlayerPos = Vector3.zero; //TODO
        //startingPlayerPos = GameObject.FindGameObjectWithTag("Player").transform.position;

        Thread initThread = new Thread(Initialize);
        initThread.Start();

        if(service == null)
        {
            service = new SpotifyWebAPIService();
        }
    }

    private void Connect()
    {
        service.Connect();
    }

    public async Task GetAllUserTracks()
    {
        print("getting user songs");
        //m_tracks = await service.GetAllUserSavedTracks();
        //if (m_tracks == null || m_tracks.Count == 0)
        //{
        //    await UseBackupPlaylists();
        //}
        //await UseFeaturedPlaylists();
        await UseBackupPlaylists();
        Debug.Log("Got user songs!");
    }

    private async Task UseBackupPlaylists()
    {
        if(BackupPlaylists.Length == 0)
        {
            //throw new Exception("Player doesn't have enough songs and you have not chosen BackupPlaylists at MySoundtrackManager!");
            await UseFeaturedPlaylists();
        } 
        else
        {
            m_tracks.AddRange(await GetPlaylistsTracks(BackupPlaylists));
        }
    }

    private async Task UseFeaturedPlaylists()
    {
        FeaturedPlaylistsRequest req = new FeaturedPlaylistsRequest();
        req.Limit = MAX_FEATURED_PLAYLISTS;

        var fPlaylists = await service.Spotify.Browse.GetFeaturedPlaylists(req);
        string[] lista = new string[Math.Min(MAX_FEATURED_PLAYLISTS, fPlaylists.Playlists.Items.Count)];
        for(var i = 0; i < fPlaylists.Playlists.Items.Count; i++)
        {
            lista[i] = fPlaylists.Playlists.Items[i].Id;
        }

        m_tracks.AddRange(await GetPlaylistsTracks(lista, true, MAX_TRACKS_PER_FEATURED_PLAYLIST));
    }

    private async Task<List<FullTrack>> GetPlaylistsTracks(string[] playlistsIds, bool useLimit = false, int limit = 10000)
    {
        List<FullTrack> tracks = new List<FullTrack>();

        foreach (var playlist in playlistsIds)
        {
            string id = playlist;
            if (playlist.Contains(':') && !playlist.Contains("https"))
            {
                var substringStart = playlist.LastIndexOf(':');
                id = playlist.Substring(substringStart + 1, (playlist.Length - 1) - substringStart);
            } else if (playlist.Contains('/'))
            {
                var substringStart = playlist.LastIndexOf('/');
                var substringEnd = playlist.LastIndexOf('?') - 1;
                id = playlist.Substring(substringStart + 1, substringEnd - substringStart);
            }

            PlaylistGetItemsRequest req = new PlaylistGetItemsRequest(PlaylistGetItemsRequest.AdditionalTypes.Track);
            if (useLimit) req.Limit = limit;

            var playlistItems = await service.Spotify.Playlists.GetItems(id, req);
            foreach (var playlistTrack in playlistItems.Items)
            {
                if (playlistTrack.Track is FullTrack)
                {
                    tracks.Add(playlistTrack.Track as FullTrack);
                }
            }
        }

        return tracks;
    }

    private void Initialize()
    {
        Connect();

        service.InitializedSpotify += async () =>
        {
            print("initialized spotify");
            await GetAllUserTracks();
            OrderInitializationsToCloserToPlayer();
            InvokeInitializationActions();
        };
    }

    private void InvokeInitializationActions()
    {
        foreach (var initializationAction in initializationActions.Select(x => x.Item2))
        {
            initializationAction.Invoke();
            Thread.Sleep(5000);
        }
    }

    private void OrderInitializationsToCloserToPlayer()
    {
        initializationActions.OrderBy(x => Vector3.Distance(x.Item1, startingPlayerPos));
    }

    private IEnumerator StartWatchForSongEnd(Action callback, int timeToEnd)
    {
        yield return new WaitForSeconds((timeToEnd - SONG_ABOUT_TO_END_TOLERANCE) / 1000f);
        callback.Invoke();
    }

    public void AddInitilizationAction(Tuple<Vector3, Action> a)
    {
        initializationActions.Add(a);
    }

    public bool EnteredNewArea(int areaId)
    {
        if (currentAreaPlaying != areaId && m_tracks != null)
        {
            currentAreaPlaying = areaId;
            return true;
        }
        return false;
    }

    public List<FullTrack> GetBestSongsFor(float energy, float valence, int howMany)
    {
        return SortByFeatures(energy, valence).GetRange(0, Mathf.Min(howMany, m_tracks.Count));
    }

    public List<FullTrack> SortByFeatures(float energy, float valence)
    {
        var audioFeatures = new List<TrackAudioFeatures>();

        List<string> tracksIds = m_tracks.Select(x => x.Id).ToList();
        for (var i = 0; i < tracksIds.Count; i += 100)
        {
            int howMany = Math.Min(100, tracksIds.Count - i - 1);
            var sublist = tracksIds.GetRange(i, howMany);
            var af = service.GetSeveralAudioFeatures(sublist).Result;
            audioFeatures.AddRange(af);
        }

        List<Tuple<FullTrack, float>> trackGrades = new List<Tuple<FullTrack, float>>();
        int count = 0;
        foreach (var f in audioFeatures)
        {
            //smaller grades are better (less different from the parameters chosen)
            float grade = (Math.Abs(f.Energy - energy) +
                           Math.Abs(f.Valence - valence));

            trackGrades.Add(new Tuple<FullTrack, float>(m_tracks[count], grade));
            count++;
        }
        trackGrades.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        return (trackGrades.Select(x => x.Item1).ToList());
    }

    public async void PlayTrack(FullTrack track, Action songAboutToEndCallback)
    {
        Debug.Log("Playing " + track.Name);
        await service.PlayTrack(track);

        StopAllCoroutines();
        StartCoroutine(StartWatchForSongEnd(songAboutToEndCallback, track.DurationMs));
    }

    public void PausePlayback()
    {
        service.PausePlayback();
    }

    public void PlayPlayback()
    {
        service.PlayPlayback();
    }

    private void OnApplicationQuit()
    {
        
    }
}