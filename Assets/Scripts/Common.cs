﻿using UnityEngine;

public static class Common
{
    public static class Tags
    {
        public static string Player = "Player";
        public static string MapChunk = "MapChunk";
        public static string Shop = "Shop";
        public static string Interactable = "Interactable";
        public static string Enemy = "Enemy";
        public static string Buffer = "Buffer";
        public static string Caster = "Caster";
    }

    public static class LayerNames
    {
        public static string Actors = "Actors";
        public static string Ground = "Ground";
        public static string Interactable = "Interactable";
    }

    public static class LayerMasks
    {
        public static int Actors = LayerMask.GetMask(LayerNames.Actors);
        public static int Ground = LayerMask.GetMask(LayerNames.Ground);
        public static int Interactable = LayerMask.GetMask(LayerNames.Interactable);
        public static int ActorsOrGround = LayerMask.GetMask(LayerNames.Ground, LayerNames.Actors);
    }

    public static class Input
    {
        public static string VerticalAxis = "Vertical";
        public static string HorizontalAxis = "Horizontal";
        public static string UseButton1 = "Use";
        public static string UseButton2 = "Use2";
        public static string UltButton = "Ult";
    }

    public static class BaseLevelNames
    {
        public static string MainMenu = "Scenes/MainMenu";
    }

    public static class SoundParameters
    {
        // You should set those in AudioMixer by exposing

        public static string MusicVolume = "MusicVolume";
        public static string SoundVolume = "SoundVolume";
    }
}