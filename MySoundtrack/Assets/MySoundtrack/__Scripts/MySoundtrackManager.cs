using SpotifyAPI.Web;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MySoundtrack_Service;

public class MySoundtrackManager : Singleton<MySoundtrackManager>
{
    [Tooltip("Backup playlists are used in cases where the player doesn't have enough liked songs on his account. These playlists should be public.")]
    public string[] BackupPlaylists;

    private List<FullTrack> m_tracks = new List<FullTrack>();

    [HideInInspector]
    public int currentAreaPlaying = -1;

    List<Tuple<Vector3, Action>> initializationActions = new List<Tuple<Vector3, Action>>();

    private const int SONG_ABOUT_TO_END_TOLERANCE = 4000; //milliseconds
    private const int SONG_LIMIT_TO_USE_BACKUP_PLAYLISTS = 150;

    private Vector3 startingPlayerPos = Vector3.zero;

    private MySoundtrackService mss;

    #region Initialization

    private void Start()
    {
        startingPlayerPos = Vector3.zero;

        if(mss == null)
        {
            mss = new MySoundtrackService();
        }

        Thread initThread = new Thread(Initialize);
        initThread.Start();
    }

    private void Connect()
    {
        mss.Connect();
    }

    public async Task GetAllUserTracks()
    {
        print("getting user songs");
        m_tracks = await mss.GetAllUserSavedTracks();
        if (m_tracks == null || m_tracks.Count < SONG_LIMIT_TO_USE_BACKUP_PLAYLISTS)
        {
            await UseBackupPlaylistsInstead();
        }
        Debug.Log("Got user songs!");
    }

    private async Task UseBackupPlaylistsInstead()
    {
        if(BackupPlaylists.Length == 0)
        {
            await UseFeaturedPlaylistsInstead();
        } 
        else
        {
            m_tracks.AddRange(await mss.GetPlaylistsTracks(BackupPlaylists));
        }
    }

    private async Task UseFeaturedPlaylistsInstead()
    {
        m_tracks.AddRange(await mss.GetFeaturedPlaylistsSongs());
    }

    private void Initialize()
    {
        Connect();

        mss.InitializedSpotify += async () =>
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
            Thread.Sleep(500);
        }
    }

    private void OrderInitializationsToCloserToPlayer()
    {
        initializationActions.OrderBy(x => Vector3.Distance(x.Item1, startingPlayerPos));
    }
    #endregion

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
            var af = mss.GetSeveralAudioFeatures(sublist).Result;
            audioFeatures.AddRange(af);
        }

        List<Tuple<FullTrack, float>> trackGrades = new List<Tuple<FullTrack, float>>();
        int count = 0;
        foreach (var f in audioFeatures)
        {
            //smaller grades are better (less different from the parameters chosen)
            float grade = 100f;
            if(f != null)
            {
                grade = (Math.Abs(f.Energy - energy) + Math.Abs(f.Valence - valence));
            }

            trackGrades.Add(new Tuple<FullTrack, float>(m_tracks[count], grade));
            count++;
        }
        trackGrades.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        return (trackGrades.Select(x => x.Item1).ToList());
    }

    public async void PlayTrack(FullTrack track, Action songAboutToEndCallback)
    {
        Debug.Log("Playing " + track.Name);
        await mss.PlayTrack(track);

        StopAllCoroutines();
        StartCoroutine(StartWatchForSongEnd(songAboutToEndCallback, track.DurationMs));
    }

    public void PlaySongsOfVibe(float energy, float valence)
    {
        var rankedTracks = GetBestSongsFor(energy, valence, 10);
        int rand = UnityEngine.Random.Range(0, 9);
        PlayTrack(rankedTracks[rand], () => PlaySongsOfVibe(energy, valence));
    }

    public void PausePlayback()
    {
        mss.PausePlayback();
    }

    public void PlayPlayback()
    {
        mss.PlayPlayback();
    }

    private void OnApplicationQuit()
    {
        
    }
}