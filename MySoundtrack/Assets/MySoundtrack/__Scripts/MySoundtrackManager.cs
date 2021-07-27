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
    private List<FullTrack> m_tracks = null;

    [HideInInspector]
    public int currentAreaPlaying = -1;

    List<Tuple<Vector3, Action>> initializationActions = new List<Tuple<Vector3, Action>>();

    private const int SONG_ABOUT_TO_END_TOLERANCE = 4000; //milliseconds

    private Vector3 startingPlayerPos = Vector3.zero;

    private SpotifyWebAPIService service;

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
        m_tracks = await service.GetAllUserSavedTracks();
        Debug.Log("Got user songs!");
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