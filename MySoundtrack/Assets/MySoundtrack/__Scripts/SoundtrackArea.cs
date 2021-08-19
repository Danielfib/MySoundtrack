﻿using SpotifyAPI.Web;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class SoundtrackArea : MonoBehaviour
{
    List<FullTrack> tracks = new List<FullTrack>();
    private int currentTrackId = 0;
    private bool isInitialized;

    [HideInInspector]
    public int id;

    [HideInInspector, SerializeField]
    private float energy, valence;

    private const int HOW_MANY_SONGS_TO_GET = 15;

    #region Editor
    [HideInInspector, SerializeField]
    public Color selectedVibeColor;
    [HideInInspector, SerializeField]
    public int toolbarIntAux = -1, toolbarInt = 0;

    private void OnDrawGizmos()
    {
        Gizmos.color = selectedVibeColor;
        Gizmos.matrix = transform.localToWorldMatrix;

        BoxCollider bc = GetComponent<BoxCollider>();
        if (!bc) return;
        Gizmos.DrawWireCube(bc.center, bc.size);
    }

    public void SetAudioFeatures(float energy, float valence)
    {
        this.energy = energy;
        this.valence = valence;
    }
    #endregion

    private void Start()
    {
        id = gameObject.GetInstanceID();
        MySoundtrackManager.Instance.AddInitilizationAction(new Tuple<Vector3, Action>(transform.position, () =>
        {
            new Thread(() =>
            {
                tracks.AddRange(MySoundtrackManager.Instance.GetBestSongsFor(energy, valence, HOW_MANY_SONGS_TO_GET));
                print("InitializedArea");
                isInitialized = true;
            }).Start();
        }));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerEntered();
        }
    }

    public void PlayerEntered()
    {
        if (!isInitialized)
        {
            print("Area not yet initialized, give it a moment!");
            return;
        }

        if (MySoundtrackManager.Instance.EnteredNewArea(id))
        {
            var rdmTop10 = UnityEngine.Random.Range(0, Mathf.Min(10, tracks.Count));
            MySoundtrackManager.Instance.PlayTrack(tracks[rdmTop10], PlayNextSong);
        }
    }

    //called when current sont is about to end
    public void PlayNextSong()
    {
        int randomIndex = Mathf.RoundToInt(UnityEngine.Random.Range(0, tracks.Count - 1));
        if (tracks.Count > 1)
        {
            while (randomIndex == currentTrackId)
            {
                randomIndex = Mathf.RoundToInt(UnityEngine.Random.Range(0, tracks.Count - 1));
            }
        }

        var nextSong = tracks[randomIndex];
        MySoundtrackManager.Instance.PlayTrack(nextSong, PlayNextSong);
        currentTrackId = randomIndex;
    }
}