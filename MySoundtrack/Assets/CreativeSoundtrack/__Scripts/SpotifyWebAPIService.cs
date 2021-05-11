using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class SpotifyWebAPIService : MonoBehaviour
{
    private const string client_id = "221b1ed7900947d3a412f455a9003fd9"; // Your client id
    private const string client_secret = "13e74dfa5a3446849d91e0c664ba9b3a"; // Your secret
    private const string redirect_uri = "http://localhost:8888/callback"; // Your redirect uri

    private EmbedIOAuthServer _server;

    public async Task StartStuff()
    {
        _server = new EmbedIOAuthServer(new Uri(redirect_uri), 5000);
        await _server.Start();

        _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
        _server.ErrorReceived += OnErrorReceived;

        var request = new LoginRequest(_server.BaseUri, client_id, LoginRequest.ResponseType.Code)
        {
            Scope = new List<string> { Scopes.UserReadEmail }
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

        var spotify = new SpotifyClient(tokenResponse.AccessToken);
        // do calls with Spotify and save token?
    }

    private async Task OnErrorReceived(object sender, string error, string state)
    {
        Console.WriteLine($"Aborting authorization, error received: {error}");
        await _server.Stop();
    }


    void Start()
    {
        StartStuff();
    }

    private void OnDestroy()
    {

    }
}
