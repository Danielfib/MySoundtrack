using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(SoundtrackArea))]
public class SoundtrackAreaCustomEditor : Editor
{
    public Texture2D battleVibeIcon;
    public Texture2D romanticVibeIcon;
    public Texture2D exploringVibeIcon;
    public Texture2D sadVibeIcon;

    const int ICON_HEIGHT = 40;

    bool showAudioFeatures = false;

    private static readonly string[] _dontIncludeMe = new string[] { "m_Script" };

    SerializedObject obj;
    private void OnEnable()
    {
        if (target)
            obj = new SerializedObject(target);
    }

    public override void OnInspectorGUI()
    {
        SoundtrackArea targetSA = target as SoundtrackArea;
        SerializedProperty tbIntProp = obj.FindProperty("toolbarInt");
        SerializedProperty tbIntAuxProp = obj.FindProperty("toolbarIntAux");
        SerializedProperty colorProp = obj.FindProperty("selectedVibeColor");
        SerializedProperty energyProp = obj.FindProperty("energy");
        SerializedProperty valenceProp = obj.FindProperty("valence");

        int toolbarInt = tbIntProp.intValue;
        int toolbarIntAux = tbIntAuxProp.intValue;

        //hide Script property
        DrawPropertiesExcluding(serializedObject, _dontIncludeMe);

        //source code links
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Source Code"))
            Application.OpenURL("https://github.com/Danielfib/MySoundtrack");
        //if (GUILayout.Button("Open Spotify Dashboard"))
        //    Application.OpenURL("https://developer.spotify.com/dashboard/");
        GUILayout.EndHorizontal();

        GUILayout.Space(20f);

        //Vibe buttons
        GUILayout.Label("Choose area's vibe:", EditorStyles.boldLabel);
        Texture2D[] textures = { battleVibeIcon, romanticVibeIcon, exploringVibeIcon, sadVibeIcon };
        toolbarInt = GUILayout.Toolbar(toolbarInt, textures, GUILayout.Height(ICON_HEIGHT));
        if (toolbarIntAux != toolbarInt)
        {
            toolbarIntAux = toolbarInt;
            switch (toolbarInt)
            {
                case 0:
                    energyProp.floatValue = 1f;
                    valenceProp.floatValue = 0.4f;
                    AssignLabel(targetSA.gameObject, battleVibeIcon);
                    break;
                case 1:
                    energyProp.floatValue = 0.8f;
                    valenceProp.floatValue = 0.8f;
                    AssignLabel(targetSA.gameObject, romanticVibeIcon);
                    break;
                case 2:
                    energyProp.floatValue = 0.5f;
                    valenceProp.floatValue = 0.3f;
                    AssignLabel(targetSA.gameObject, exploringVibeIcon);
                    break;
                case 3:
                    energyProp.floatValue = 0f;
                    valenceProp.floatValue = 0f;
                    AssignLabel(targetSA.gameObject, sadVibeIcon);
                    break;
            }
            tbIntProp.intValue = toolbarInt;
            tbIntAuxProp.intValue = toolbarIntAux;
            colorProp.colorValue = getVibeColor(toolbarInt);
            obj.ApplyModifiedProperties();
        }

        showAudioFeatures = EditorGUILayout.Foldout(showAudioFeatures, "Audio Features", EditorStyles.foldout);

        if (showAudioFeatures)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Energy", EditorStyles.label);
            float sliderEValue = EditorGUILayout.Slider(energyProp.floatValue, 0, 1, GUILayout.Width(160));
            if (sliderEValue != energyProp.floatValue)
            {
                energyProp.floatValue = sliderEValue;
                tbIntProp.intValue = -1;//unchecks toolbar
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Valence", EditorStyles.label);
            float sliderVValue = EditorGUILayout.Slider(valenceProp.floatValue, 0, 1, GUILayout.Width(160));
            if(sliderVValue != valenceProp.floatValue)
            {
                valenceProp.floatValue = sliderVValue;
                tbIntProp.intValue = -1;//unchecks toolbar
            }
            GUILayout.EndHorizontal();
        }
    }

    public void AssignLabel(GameObject g, Texture2D tex = null)
    {
        if (tex == null)
            tex = EditorGUIUtility.IconContent("sv_label_0").image as Texture2D;

        Type editorGUIUtilityType = typeof(EditorGUIUtility);
        BindingFlags bindingFlags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
        object[] args = new object[] { g, tex };
        editorGUIUtilityType.InvokeMember("SetIconForObject", bindingFlags, null, null, args);
    }

    private Color getVibeColor(int vibeSelected)
    {
        Color vibeColor = Color.red;

        switch (vibeSelected)
        {
            case 0: //combat
                vibeColor = new Color(231f / 255f, 228f / 255f, 90f / 255f, 1);
                break;
            case 1: //romance
                vibeColor = new Color(191f / 255f, 98f / 255f, 98f / 255f, 1);
                break;
            case 2: //adventure
                vibeColor = new Color(99f / 255f, 192f / 255f, 183f / 255f, 1);
                break;
            case 3: //sad
                vibeColor = new Color(50f / 255f, 69f / 255f, 151f / 255f, 1);
                break;
        }
        return vibeColor;
    }
}