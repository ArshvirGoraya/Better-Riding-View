// Project:         BetterRidingView for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2024 Arshvir Goraya
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Arshvir Goraya
// Origin Date:     August 31 2024
// Source Code:     https://github.com/ArshvirGoraya/Better-Riding-View

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.UserInterface;
using System.Collections.Generic;
using System;

namespace BetterRidingViewMod
{
    public class BetterRidingView : MonoBehaviour
    {
        public static bool dynamic_horse_positioning = true;
        public static float horse_center_position = 0;
        public static float horse_center_angle = 0; // -15
        public static float horse_down_position = 0;
        public static float horse_down_angle = 0; // 40
        public static float horse_horizontal_position = 0;
        float camera_angle_x = 0;
        float normalized_angle_x = 0;
        float horse_texture_offset_y = 0;
////////////////////////////////////////////////////////////////////////////////
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

            // * Get mod settings on change!
            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();
        }
        // * Raised when user changes mod settings.
        static void LoadSettings(ModSettings modSettings, ModSettingsChange change){
            dynamic_horse_positioning = modSettings.GetBool("DynamicHorsePositioning", "DynamicHorsePositioning");
            horse_center_position = modSettings.GetFloat("CenterPositioning", "HorseCenterPosition");
            horse_center_angle = modSettings.GetFloat("CenterPositioning", "HorseCenterAngle");
            horse_down_position = modSettings.GetFloat("DownPositioning", "HorseDownPosition");
            horse_down_angle = modSettings.GetFloat("DownPositioning", "HorseDownAngle");
            horse_horizontal_position = modSettings.GetFloat("HorizontalPositioning", "HorseHorizontalPosition");
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
        public float NormalizeTo180Angle(float angle)
        {
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        private void Update(){
            if (!GameManager.Instance.StateManager.GameInProgress){
                return;
            }
            if (GameManager.Instance.TransportManager.IsOnFoot){
                return;
            }
            if (!dynamic_horse_positioning){
                horse_texture_offset_y = horse_center_position;
                return;
            }
            camera_angle_x = -NormalizeTo180Angle(GameManager.Instance.MainCamera.transform.eulerAngles.x);
            camera_angle_x = Mathf.Clamp(camera_angle_x, horse_center_angle, horse_down_angle);
            normalized_angle_x = NormalizeValue(camera_angle_x, horse_center_angle, horse_down_angle);
            horse_texture_offset_y = GetValueFromNormalize(normalized_angle_x, horse_center_position, horse_down_position);
        }

        // * Mimics the OnGUI method inside of TransportManager.cs, with some additions to allow dynamic horse positioning.
        void OnGUI(){
            if (!GameManager.Instance.StateManager.GameInProgress){
                return;
            }
            if (GameManager.Instance.TransportManager.IsOnFoot){
                return;
            }
            if (GameManager.Instance.TransportManager.RidingTexture.texture != null){
                if (DaggerfallUI.Instance.CustomScreenRect != null){ 
                    screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
                }
                else{
                    screenRect = new Rect(0, 0, Screen.width, Screen.height);
                }
                
                if (Event.current.type.Equals(EventType.Repaint)){
                // if (Event.current.type.Equals(EventType.Repaint) && !GameManager.IsGamePaused)
                    GUI.depth = 2;
                    float horseScaleY = (float)screenRect.height / nativeScreenHeight;
                    float horseScaleX = horseScaleY * TransportManager.ScaleFactorX;
                    float horseOffsetHeight = 0;
                    if (DaggerfallUI.Instance.DaggerfallHUD != null &&
                        DaggerfallUnity.Settings.LargeHUD &&
                        DaggerfallUnity.Settings.LargeHUDOffsetHorse)
                    {
                        horseOffsetHeight = (int)DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ScreenHeight;
                    }
////////////////////////////////////////////////////////////////////////////////
                    float horseOffsetWidth = 0;
                    horseOffsetHeight += horse_texture_offset_y;
                    horseOffsetWidth += horse_horizontal_position;
                    Rect pos = new Rect(
                        screenRect.x + screenRect.width / 2f - (GameManager.Instance.TransportManager.RidingTexture.width * horseScaleX) / 2f + horseOffsetWidth,
                        screenRect.y + screenRect.height - (GameManager.Instance.TransportManager.RidingTexture.height * horseScaleY) - horseOffsetHeight,
                        GameManager.Instance.TransportManager.RidingTexture.width * horseScaleX,
                        GameManager.Instance.TransportManager.RidingTexture.height * horseScaleY)
                    ;
////////////////////////////////////////////////////////////////////////////////
                    DaggerfallUI.DrawTexture(pos, GameManager.Instance.TransportManager.RidingTexture.texture, ScaleMode.StretchToFill, true, GameManager.Instance.TransportManager.Tint);
                }
            }
        }
    }
}
