using SpotifyAPI.Web;
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

    private const string scopes = "user-read-private user-read-email";

    string verifier, challenge;

    // Start is called before the first frame update
    void Start()
    {
        // Generates a secure random verifier of length 100 and its challenge
        (verifier, challenge) = PKCEUtil.GenerateCodes();

        var loginRequest = new LoginRequest(
            new Uri("http://localhost:5000/callback"),
            client_id,
            LoginRequest.ResponseType.Code
        )
        {
            CodeChallengeMethod = "S256",
            CodeChallenge = challenge,
            Scope = new[] { Scopes.PlaylistReadPrivate, Scopes.PlaylistReadCollaborative }
        };

        var uri = loginRequest.ToUri();
        Task.Run(() => ListenForCallback());
        Application.OpenURL(uri.ToString());
    }

    private async void ListenForCallback()
    {
        HttpListener httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:5000/callback/");
        httpListener.Start();

        HttpListenerContext context = httpListener.GetContext(); //waits for response
        HttpListenerRequest request = context.Request;
        string code = request.QueryString["code"];
        httpListener.Stop();
        await GetCallback(code);
    }

    // This method should be called from your web-server when the user visits "http://localhost:5000/callback"
    public async Task GetCallback(string code)
    {
        // Note that we use the verifier calculated above!
        PKCETokenRequest req = new PKCETokenRequest(client_id, code, new Uri("http://localhost:5000"), verifier);
        Debug.Log("1");
        var initialResponse = await new OAuthClient().RequestToken(req);
        Debug.Log("2"); //TODO: Understand why execution never gets here. It waits forever
        var spotify = new SpotifyClient(initialResponse.AccessToken);
        // Also important for later: response.RefreshToken
    }

    private void OnDestroy()
    {
        
    }
}
