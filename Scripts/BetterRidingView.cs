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
using __ExternalAssets;
using System.Runtime.InteropServices.WindowsRuntime;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallConnect.Utility;

namespace BetterRidingViewMod
{
    public class BetterRidingView : MonoBehaviour
    {
        // * Vertical Camera Positioning:
        public static bool horse_vertical_positioning = true;
        public static float horse_center_position = 0;
        public static float horse_center_angle = 0;
        public static float horse_down_position = 0;
        public static float horse_down_angle = 0;
        // * Dynamic Jumping
        public static bool dynamic_horse_jumping = true;
        public static float max_horse_jump_height = 0;
        public static float horse_jump_up_time = 0;
        public static float horse_jump_down_time = 0;
        public static int easing_up_function_num = 0;
        public static int easing_down_function_num = 0;
        public enum HorseTweenType
        {
            None,
            TweenUp,
            TweenDown,
        }
        public HorseTweenType horse_tween_type = HorseTweenType.None;
        public float current_tween_value = 0;
        float tween_elapsed_time = 0;
        public bool on_ground = true;
        // * Horizontal Camera Positioning:
        public static float horizontal_lerp_strength = 0.1f;
        public static float horse_horizontal_position_target = 0;
        public static float horse_horizontal_position = 0;
        GameObject gameObjectPlayerAdvanced = null;
        public float previous_camera_y_angle = 0;
        public bool previous_on_foot = true;
        // * Camera
        float camera_angle_x = 0;
        float normalized_angle_x = 0;
        public static float horse_texture_offset_y = 0;
////////////////////////////////////////////////////////////////////////////////
        private static Mod mod;
        Rect screenRect;
        readonly float nativeScreenHeight = 200;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams){            
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<BetterRidingView>();
            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();
            mod.IsReady = true;
        }
        // * Raised when user changes mod settings.
        static void LoadSettings(ModSettings modSettings, ModSettingsChange change){
            // * Vertical Camera Positioning:
            horse_vertical_positioning = modSettings.GetBool("VerticalPositioning", "VerticalPositioning");
            horse_center_position = (float)modSettings.GetInt("VerticalPositioning", "HorseCenterPosition");
            horse_center_angle = modSettings.GetFloat("VerticalPositioning", "HorseCenterAngle");
            horse_down_position = (float)modSettings.GetInt("VerticalPositioning", "HorseDownPosition");
            horse_down_angle = modSettings.GetFloat("VerticalPositioning", "HorseDownAngle");
            // * Jumping:
            dynamic_horse_jumping = modSettings.GetBool("DynamicJumping", "DynamicHorseJumping");
            max_horse_jump_height = modSettings.GetFloat("DynamicJumping", "MaxJumpHeight");
            horse_jump_up_time = modSettings.GetFloat("DynamicJumping", "JumpUpTime");
            horse_jump_down_time = modSettings.GetFloat("DynamicJumping", "JumpDownTime");
            easing_up_function_num = modSettings.GetInt("DynamicJumping", "JumpUpEasing");
            easing_down_function_num = modSettings.GetInt("DynamicJumping", "JumpDownEasing");
            // * Horizontal Camera Positioning:
            horse_horizontal_position_target = (float)modSettings.GetInt("HorizontalPositioning", "HorseHorizontalPosition");
            horizontal_lerp_strength = (float)modSettings.GetInt("HorizontalPositioning", "LerpStrength") / 100; // * Make into float.
            if (horizontal_lerp_strength < 1){
                horse_horizontal_position = horse_horizontal_position_target;
            }
        }
        private void Start(){
            GameManager.Instance.TransportManager.DrawHorse = false;
            previous_on_foot = GameManager.Instance.TransportManager.IsOnFoot;
            gameObjectPlayerAdvanced = GameObject.Find("PlayerAdvanced");
            StreamingWorld.OnTeleportToCoordinates += Teleported;
        }
        private void Teleported(DFPosition worldPos){
            Debug.Log($"teleported on horse: {worldPos}");
            // horse_horizontal_position = horse_horizontal_position_target;
            // if (!GameManager.Instance.TransportManager.IsOnFoot){
            //     Debug.Log($"teleported on horse: {worldPos}");
            //     horse_horizontal_position = horse_horizontal_position_target;
            // }
        }
////////////////////////////////////////////////////////////////////////////////
        public static float NormalizeValue(float value, float min, float max){
            return (value - min) / (max - min);
        }
        public static float GetValueFromNormalize(float normalized_value, float min, float max){
            return min + normalized_value * (max - min);
        }
        public float NormalizeTo180Angle(float angle){
            if (angle > 180)
                angle -= 360;
            return angle;
        }
        public float GetYRotationDifference(){
            float diff = previous_camera_y_angle - gameObjectPlayerAdvanced.transform.eulerAngles.y;
            if (diff > 180f){diff -= 360f;}
            else if (diff < -180f){diff += 360f;}
            return diff;
        }
        public void EnteredRiding(){
            previous_camera_y_angle = gameObjectPlayerAdvanced.transform.eulerAngles.y;
            horse_horizontal_position = horse_horizontal_position_target;
        }
////////////////////////////////////////////////////////////////////////////////
        public float GetHorseTextureOffset(){
            camera_angle_x = -NormalizeTo180Angle(GameManager.Instance.MainCamera.transform.eulerAngles.x);
            camera_angle_x = Mathf.Clamp(camera_angle_x, horse_center_angle, horse_down_angle);
            normalized_angle_x = NormalizeValue(camera_angle_x, horse_center_angle, horse_down_angle);
            return GetValueFromNormalize(normalized_angle_x, horse_center_position, horse_down_position);
        }
        float IncrementTweenUp(){
            float start_val = GetHorseTextureOffset();
            float target_val = Mathf.Min(horse_center_position, start_val + max_horse_jump_height);
            if (tween_elapsed_time > horse_jump_up_time) tween_elapsed_time = horse_jump_up_time;
            current_tween_value = GetValueFromNormalize(
                BetterRidingViewEasing.Interpolate(NormalizeValue(tween_elapsed_time, 0, horse_jump_up_time), easing_up_function_num),
                start_val,
                target_val
            );
            return Mathf.Min(horse_center_position, current_tween_value);
        }
        float IncrementTweenDown(){
            float start_val = current_tween_value;
            float target_val = GetHorseTextureOffset();
            if (tween_elapsed_time > horse_jump_down_time) tween_elapsed_time = horse_jump_down_time;
            current_tween_value = GetValueFromNormalize(
                BetterRidingViewEasing.Interpolate(NormalizeValue(tween_elapsed_time, 0, horse_jump_down_time), easing_down_function_num),
                start_val,
                target_val
            );
            return Mathf.Min(horse_center_position, current_tween_value);
        }
////////////////////////////////////////////////////////////////////////////////
        private void LateUpdate(){
            if (!GameManager.Instance.StateManager.GameInProgress || GameManager.IsGamePaused){
                return;
            }
            if (previous_on_foot && !GameManager.Instance.TransportManager.IsOnFoot){
                EnteredRiding();
            }
            previous_on_foot = GameManager.Instance.TransportManager.IsOnFoot;
            if (GameManager.Instance.TransportManager.IsOnFoot){
                return;
            }
            if (!horse_vertical_positioning){
                horse_texture_offset_y = horse_center_position;
                return;
            }
            if (dynamic_horse_jumping){
                if (on_ground){
                    if ((GameManager.Instance.AcrobatMotor.Jumping || GameManager.Instance.AcrobatMotor.Falling)){
                        // * Just Jumped
                        horse_tween_type = HorseTweenType.TweenUp;
                        on_ground = false;
                        tween_elapsed_time = 0;
                    }
                }else{
                    if (!(GameManager.Instance.AcrobatMotor.Jumping || GameManager.Instance.AcrobatMotor.Falling)){
                        // * Landed from Jump.
                        horse_tween_type = HorseTweenType.TweenDown;
                        on_ground = true;
                        tween_elapsed_time = 0;
                    }
                }
            }
            
            if (horse_tween_type == HorseTweenType.None){
                horse_texture_offset_y = GetHorseTextureOffset();
            }
            else{
                tween_elapsed_time += Time.deltaTime;
                if (horse_tween_type == HorseTweenType.TweenUp){
                    horse_texture_offset_y = IncrementTweenUp();
                }else{
                    horse_texture_offset_y = IncrementTweenDown();
                    // * End jump easing easing.
                    if (tween_elapsed_time >= horse_jump_down_time){
                        horse_tween_type = HorseTweenType.None;
                    }
                }
            }
            // * Dynamic Horizontal Positioning
            if (horizontal_lerp_strength < 1){
                float camera_horizontal_diff = GetYRotationDifference();
                previous_camera_y_angle = gameObjectPlayerAdvanced.transform.eulerAngles.y;
                horse_horizontal_position += camera_horizontal_diff;
                horse_horizontal_position = Mathf.Lerp(
                    horse_horizontal_position,
                    horse_horizontal_position_target,
                    horizontal_lerp_strength
                );
            }

        }
        // * Mimics the OnGUI method inside of TransportManager.cs, with some additions to allow dynamic horse positioning.
        void OnGUI(){
            if (!GameManager.Instance.StateManager.GameInProgress){
                return;
            }
            if (Event.current.type.Equals(EventType.Repaint)){
                if (GameManager.Instance.TransportManager.IsOnFoot){
                    return;
                }
                if (GameManager.Instance.TransportManager.RidingTexture.texture != null){
                    if (DaggerfallUI.Instance.CustomScreenRect != null){ screenRect = DaggerfallUI.Instance.CustomScreenRect.Value; }
                    else{ screenRect = new Rect(0, 0, Screen.width, Screen.height); }
                    GUI.depth = 2;
////////////////////////////////////////////////////////////////////////////////
                    float horseScaleY = (float)screenRect.height / nativeScreenHeight;
                    float horseScaleX = horseScaleY * TransportManager.ScaleFactorX;
                    float horseOffsetHeight = 0;
                    if (DaggerfallUI.Instance.DaggerfallHUD != null &&
                        DaggerfallUnity.Settings.LargeHUD &&
                        DaggerfallUnity.Settings.LargeHUDOffsetHorse){
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
                        GameManager.Instance.TransportManager.RidingTexture.height * horseScaleY
                    );
////////////////////////////////////////////////////////////////////////////////
                    DaggerfallUI.DrawTexture(pos, GameManager.Instance.TransportManager.RidingTexture.texture, ScaleMode.StretchToFill, true, GameManager.Instance.TransportManager.Tint);    
                }
            }
        }
    }
}
