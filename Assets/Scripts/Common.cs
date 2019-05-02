using UnityEngine;

namespace Assets.Scripts
{
    public static class Common
    {
        /*
        public static class Tags
        {
            public static string Player = "Player";
            public static string Hamster = "Hamster";
            public static string DrumArea = "DrumArea";
            public static string Killer = "Killer";
            public static string MainCamera = "MainCamera";
        }

        public static class Layers
        {
            public static string Environment = "Environment";
            public static int EnvironmentMask = LayerMask.GetMask(Environment);
        }
        
        public static class Controls
        {
            public static int LeftMouseButton = 0;
            public static int RightMouseButton = 1;
            public static int MiddleMouseButton = 2;
            public static string HorizontalMovementAxis = "Horizontal";
            public static string VerticalMovementAxis = "Vertical";
        }

        public static class Patterns
        {
            public static string Move = "Move";
            public static string MoveNearest = "MoveNearest";
            public static string Restart = "Restart";
        }
        */
        public static class BaseLevelNames
        {
            public static string MainMenu = "Scenes/MainMenu";
            public static string Intro = "Scenes/Levels/Intro";
        }

        public static class SoundParameters
        {
            public static string MusicVolume = "MusicVolume";
            public static string SoundVolume = "SoundVolume";
        }
    }
}