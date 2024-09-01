﻿// Project:         BetterRidingView for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2024 Arshvir Goraya
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Arshvir Goraya
// Origin Date:     August 31 2024
// Source Code:     https://github.com/ArshvirGoraya/Better-Riding-View

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop;

namespace BetterRidingViewMod
{
    public class BetterRidingView : MonoBehaviour
    {
        public float straight_angle_offset = 0; // 15
        public float up_angle_offset = 0; // 40

        const float STRAIGHT_ANGLE = 0;
        const float UP_ANGLE = 90;

        const float STRAIGHT_HORSE_POS = 0;
        const float UP_HORSE_POS = 300; // 293

        float camera_angle_x = 0;
        float normalized_angle_x = 0;

        float horse_texture_offset = 0;

        private static Mod mod;
        Rect screenRect;
        readonly float nativeScreenHeight = 200;
        
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<BetterRidingView>();
            mod.IsReady = true;
        }

        private void Start()
        {
            GameManager.Instance.TransportManager.DrawHorse = false;
        }

        public static float NormalizeValue(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }

        public static float GetValueFromNormalize(float normalized_value, float min, float max)
        {
            return min + normalized_value * (max - min);
        }

    float NormalizeTo180Angle(float angle)
    {
        if (angle > 180)
            angle -= 360;
        return angle;
    }

        private void Update(){
            camera_angle_x = NormalizeTo180Angle(GameManager.Instance.MainCamera.transform.eulerAngles.x);
            if (camera_angle_x > 0) {camera_angle_x = 0;}
            Debug.Log($"camera_angle_x: {camera_angle_x}");

            normalized_angle_x = NormalizeValue(camera_angle_x, STRAIGHT_ANGLE + straight_angle_offset, -(UP_ANGLE + up_angle_offset));
            horse_texture_offset = GetValueFromNormalize(normalized_angle_x, STRAIGHT_HORSE_POS, UP_HORSE_POS);
        }
    
        void OnGUI()
            {                
                if (DaggerfallUI.Instance.CustomScreenRect != null)
                    screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
                else
                    screenRect = new Rect(0, 0, Screen.width, Screen.height);

                if (Event.current.type.Equals(EventType.Repaint) && !GameManager.IsGamePaused)
                {
                    if ((GameManager.Instance.TransportManager.TransportMode == TransportModes.Horse || GameManager.Instance.TransportManager.TransportMode == TransportModes.Cart) && GameManager.Instance.TransportManager.RidingTexture.texture != null)
                    {
                        // Draw horse texture behind other HUD elements & weapons.
                        GUI.depth = 2;
                        // Get horse texture scaling factor. (base on height to avoid aspect ratio issues like fat horses)
                        float horseScaleY = (float)screenRect.height / nativeScreenHeight;
                        float horseScaleX = horseScaleY * TransportManager.ScaleFactorX;

                        // Allow horse to be offset when large HUD enabled
                        // This is enabled by default to match classic but can be toggled for either docked/undocked large HUD
                        float horseOffsetHeight = 0;
                        if (DaggerfallUI.Instance.DaggerfallHUD != null &&
                            DaggerfallUnity.Settings.LargeHUD &&
                            DaggerfallUnity.Settings.LargeHUDOffsetHorse)
                        {
                            horseOffsetHeight = (int)DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ScreenHeight;
                        }

                        horseOffsetHeight -= horse_texture_offset;

                        // Calculate position for horse texture and draw it.
                        Rect pos = new Rect(
                                        screenRect.x + screenRect.width / 2f - (GameManager.Instance.TransportManager.RidingTexture.width * horseScaleX) / 2f,
                                        screenRect.y + screenRect.height - (GameManager.Instance.TransportManager.RidingTexture.height * horseScaleY) - horseOffsetHeight,
                                        GameManager.Instance.TransportManager.RidingTexture.width * horseScaleX,
                                        GameManager.Instance.TransportManager.RidingTexture.height * horseScaleY);
                        DaggerfallUI.DrawTexture(pos, GameManager.Instance.TransportManager.RidingTexture.texture, ScaleMode.StretchToFill, true, GameManager.Instance.TransportManager.Tint);
                    }
                }
            }
    
    
    }
}
