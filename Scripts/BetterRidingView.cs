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
using static DaggerfallWorkshop.Game.PlayerEnterExit;
using System.Reflection;

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
        public static float random_jump_height_range_value = 1;
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
        // * Teleport Fix:
        public bool enable_horse_horizontal_positioning = true;
        // * Randomized Horse Jumping:
        public float max_horse_random_jump_height = 0;
        // * Public Variables for Mod Support: 
        public static bool better_riding_view_draw_horse = true;
        // * Eye of the Beholder Compatbility:
        // ModManager.Instance.GetModFromGUID("2942ea8c-dbd4-42af-bdf9-8199d2f4a0aa");
        Component eye_of_the_beholder_component;
        bool eye_of_the_beholder_current_offset;
        bool eye_of_the_beholder_previous_offset;
        FieldInfo eye_of_the_beholder_offset;
        GameObject eye_of_the_beholder;
        // * Roleplay and Realism Compatibility:
        GameObject roleplay_and_realism;
        private int previous_texture_index = -1;
        public ImageData riding_texture;
        public Texture2D riding_horse_texture = null;

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
            mod.MessageReceiver = MessageReceiver;
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
            random_jump_height_range_value = modSettings.GetFloat("DynamicJumping", "RandomJumpHeightRange");
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

        private static void MessageReceiver(string message, object data, DFModMessageCallback callBack){
            if (message == "DrawHorse"){
                better_riding_view_draw_horse = true;
            } else if (message == "StopDrawHorse"){
                better_riding_view_draw_horse = false;
            }
        }

        private void Start(){
            GameManager.Instance.TransportManager.DrawHorse = false;
            previous_on_foot = GameManager.Instance.TransportManager.IsOnFoot;
            gameObjectPlayerAdvanced = GameObject.Find("PlayerAdvanced");
            StreamingWorld.OnTeleportToCoordinates += Teleported;
            PlayerEnterExit.OnTransitionExterior += ExteriorTransition;

            // * Eye of the Beholder Compatbility:
            eye_of_the_beholder = GameObject.Find("Eye Of The Beholder");
            if (eye_of_the_beholder != null){
                Component[] components = eye_of_the_beholder.GetComponents<Component>();
                foreach (Component component in components){
                    Type type = component.GetType();
                    if (type.ToString() == "EyeOfTheBeholder"){
                        eye_of_the_beholder_component = component; 
                        FieldInfo fieldInfo = type.GetField("offset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fieldInfo != null){
                            eye_of_the_beholder_offset = fieldInfo;
                            eye_of_the_beholder_previous_offset = (bool) eye_of_the_beholder_offset.GetValue(eye_of_the_beholder_component);
                            eye_of_the_beholder_current_offset = eye_of_the_beholder_previous_offset;
                        }
                    }
                }
            }

            // * Roleplay and Realism Compatibility:
            roleplay_and_realism = GameObject.Find("RoleplayRealism");
            riding_texture = GameManager.Instance.TransportManager.RidingTexture;
            // FieldInfo propertyInf = GameManager.Instance.TransportManager.GetType().GetField("ridingTexures", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // // PropertyInfo propertyInf = rtype.GetProperty("ridingTexures", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Debug.Log($"propertyInf: {propertyInf}");
            // ImageData[] ridingTextureInstance = (ImageData[]) propertyInf.GetValue(GameManager.Instance.TransportManager);
            // Debug.Log($"property object: {ridingTextureInstance}");
            // ImageData[] bruh = ridingTextureInstance;
            // propertyInf.SetValue(GameManager.Instance.TransportManager, new ImageData[4]);
            // Debug.Log($"-> before: riding_horse_texture value: {bruh != null}");
            // riding_texture = GameManager.Instance.TransportManager.RidingTexture;

            // FieldInfo nestedFieldInfo = ridingTextureInstance.GetType().GetField("texture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Debug.Log($"nestedFieldInfo: {nestedFieldInfo}");

            // Debug.Log($"-> initial: riding_horse_texture value: {riding_horse_texture != null}");
            // riding_horse_texture = (Texture2D) nestedFieldInfo.GetValue(GameManager.Instance.TransportManager.RidingTexture);
            // riding_horse_texture = (Texture2D) nestedFieldInfo.GetValue(ridingTextureInstance);
            // object bruh = nestedFieldInfo.GetValue(ridingTextureInstance);
            // Debug.Log($"-> before: riding_horse_texture value: {bruh != null}");

            // Debug.Log($"-> after null: riding_horse_texture value: {riding_horse_texture.GetPixels().Length}");

            // nestedFieldInfo.SetValue(ridingTextureInstance, null);
            // Debug.Log($"-> riding_horse_texture value: {riding_horse_texture}");
            

            // ImageData ridingTextureInstance = (ImageData) propertyInf.GetValue(rtype);
            // Texture2D horse_texture_clone = ridingTextureInstance.texture;
            // ridingTextureInstance.texture = null;

            
            // Debug.Log($"propertyInf: {propertyInf}");
            // Debug.Log($"riding_texture: {riding_texture}");
            // propertyInf.SetValue(GameManager.Instance.TransportManager, null);
            // Debug.Log($"riding_texture after setting to null: {riding_texture}");
            
            // GameManager.Instance.TransportManager.RidingTexture = null;
            // PropertyInfo propertyInf = TransportManager.GetProperty("RidingTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        private void Teleported(DFPosition worldPos){
            // * If teleported triggered AFTER camera rotation, then can just to like ExteriorTransition() function instead.
            HorseWhipFix();
        }
        private void ExteriorTransition(TransitionEventArgs args){
            // * This is needed to be compatible with RememberTransportMode Mod, but it might be good to have regardless.
            previous_camera_y_angle = gameObjectPlayerAdvanced.transform.eulerAngles.y;
            horse_horizontal_position = horse_horizontal_position_target;
        }
        private void HorseWhipFix(){
            if (horizontal_lerp_strength >= 1 || GameManager.Instance.TransportManager.IsOnFoot){ return; }
            enable_horse_horizontal_positioning = false; // * disables horizontal positioning for a second.
            Invoke(nameof(EnableHorizontalPositioning), 0.5f); // * If there is a event for after player is fully telported (rotated), use that instead.
            previous_camera_y_angle = gameObjectPlayerAdvanced.transform.eulerAngles.y;
            horse_horizontal_position = horse_horizontal_position_target;
        }
        private void EnableHorizontalPositioning(){
            enable_horse_horizontal_positioning = true;
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
            if (horizontal_lerp_strength >= 1){ return; }
            previous_camera_y_angle = gameObjectPlayerAdvanced.transform.eulerAngles.y;
            horse_horizontal_position = horse_horizontal_position_target;
        }
////////////////////////////////////////////////////////////////////////////////
        public float GetMaxHorseJumpHeight(){
            if (random_jump_height_range_value == 1f){
                return max_horse_jump_height;
            }
            float working_center_position = GetValueFromNormalize(random_jump_height_range_value, horse_center_position, max_horse_jump_height); // * Min Value
            float random_jump_height = GetValueFromNormalize(
                UnityEngine.Random.Range(0f, 1f),
                working_center_position, 
                max_horse_jump_height
            );
            return (float) Math.Ceiling(random_jump_height);
        }
        public float GetHorseTextureOffset(){
            camera_angle_x = -NormalizeTo180Angle(GameManager.Instance.MainCamera.transform.eulerAngles.x);
            camera_angle_x = Mathf.Clamp(camera_angle_x, horse_center_angle, horse_down_angle);
            normalized_angle_x = NormalizeValue(camera_angle_x, horse_center_angle, horse_down_angle);
            return GetValueFromNormalize(normalized_angle_x, horse_center_position, horse_down_position);
        }
        float IncrementTweenUp(float max_jump_height){
            float start_val = GetHorseTextureOffset();
            float target_val = Mathf.Min(horse_center_position, start_val + max_jump_height);
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
        private void Update(){
            if (!GameManager.Instance.StateManager.GameInProgress || GameManager.IsGamePaused || GameManager.Instance.TransportManager.IsOnFoot){
                return;
            }
            // // * Roleplay and Realism Compatibility:
            // if (previous_texture_index != GameManager.Instance.TransportManager.FrameIndex){
            //     previous_texture_index = GameManager.Instance.TransportManager.FrameIndex;
            //     Debug.Log($"horse texture changed");
            //     // * Make ridingTexture null:
            // }
            
            // ImageData horse_image_data = (ImageData) GameManager.Instance.TransportManager.GetType().GetField("ridingTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameManager.Instance.TransportManager);
            // GameManager.Instance.TransportManager.GetType().GetField("ridingTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameManager.Instance.TransportManager).GetType().GetField("texture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField).SetValue(
            //     GameManager.Instance.TransportManager.GetType().GetField("ridingTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameManager.Instance.TransportManager), 
            //     null);
            // Debug.Log($"texture is null: {GameManager.Instance.TransportManager.RidingTexture.texture == null}");
            
            // Texture2D horse_image_data_texture = (Texture2D) 
            // horse_image_data.texture = null;

            // FieldInfo horse_image_data_field = GameManager.Instance.TransportManager.GetType().GetField("ridingTexture");
            // var horse_image_data_instance = horse_image_data_field.GetValue(GameManager.Instance.TransportManager);

            // ImageData horse_image_data = (ImageData) GameManager.Instance.TransportManager.GetType().GetField("ridingTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameManager.Instance.TransportManager);
            // Texture2D horse_image_data_texture = (Texture2D) horse_image_data.GetType().GetField("texture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(horse_image_data);

            // if (horse_image_data_texture == null){
            //     Debug.Log($"horse_image_data_texture: null");
            // }else{
            //     Debug.Log($"horse_image_data_texture: {horse_image_data_texture}");
            //     riding_texture.texture = horse_image_data_texture;
            // }
            // // 
            // try {
            //     horse_image_data.GetType().GetField("texture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(horse_image_data, null);
            // } catch(Exception ex) {
            //     Debug.Log($"Failed to set horse texture to null with exception: {ex}");
            // }

            // Nullable<ImageData> temp_riding_texture = (Nullable<ImageData>) GameManager.Instance.TransportManager.GetType().GetField("ridingTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameManager.Instance.TransportManager);
            // if (temp_riding_texture != null){
            //     ImageData temp_riding_texture_r = (ImageData) temp_riding_texture;
            //     if (!temp_riding_texture_r.filename.Equals("")){
            //         riding_texture = temp_riding_texture_r;
            //         Debug.Log($"riding_texture set to: {riding_texture.filename}");
            //     }
                
            // }
            // try {
            //     GameManager.Instance.TransportManager.GetType().GetField("ridingTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GameManager.Instance.TransportManager, null);
            // }catch(Exception ex){
            //     Debug.Log($"Failed to set ridingTexture to null with exception: {ex}");
            // }
        }
        private void LateUpdate(){
            if (!GameManager.Instance.StateManager.GameInProgress || GameManager.IsGamePaused){
                return;
            }
            // * Eye of the Beholder Compatibility: 
            // TODO: If can listen for an event from the mod, that would be better than this.
            if (eye_of_the_beholder != null){
                if ((bool) eye_of_the_beholder_offset.GetValue(eye_of_the_beholder_component)){
                    better_riding_view_draw_horse = false;
                }else{
                    better_riding_view_draw_horse = true;
                    GameManager.Instance.TransportManager.DrawHorse = false;
                }
            }
            // * Roleplay and Realism Compatibility:
            if (roleplay_and_realism != null){
                if (GameManager.Instance.TransportManager.RidingTexture.texture != null){
                    riding_texture = GameManager.Instance.TransportManager.RidingTexture;
                }
                try {
                    GameManager.Instance.TransportManager.GetType().GetField("ridingTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GameManager.Instance.TransportManager, null);
                }catch(Exception ex){
                    Debug.Log($"Failed to set ridingTexture to null with exception: {ex}");
                }
            }
            // * If On Horse:
            if (previous_on_foot && !GameManager.Instance.TransportManager.IsOnFoot){
                EnteredRiding();
            }
            previous_on_foot = GameManager.Instance.TransportManager.IsOnFoot;
            if (GameManager.Instance.TransportManager.IsOnFoot){
                return;
            }
            // * Dynamic Horizontal Positioning
            if (enable_horse_horizontal_positioning){
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
            }else{
                previous_camera_y_angle = gameObjectPlayerAdvanced.transform.eulerAngles.y;
            }
            // * If Have Dynamic Vertical Positioning:
            if (!horse_vertical_positioning){
                horse_texture_offset_y = horse_center_position;
                return;
            }
            // * Dynamic Jumping:
            if (dynamic_horse_jumping){
                if (on_ground){
                    if ((GameManager.Instance.AcrobatMotor.Jumping || GameManager.Instance.AcrobatMotor.Falling)){
                        // * Just Jumped
                        max_horse_random_jump_height = GetMaxHorseJumpHeight();
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
            // * Jump Tweening:
            if (horse_tween_type == HorseTweenType.None){
                horse_texture_offset_y = GetHorseTextureOffset();
            }
            else{
                tween_elapsed_time += Time.deltaTime;
                if (horse_tween_type == HorseTweenType.TweenUp){
                    horse_texture_offset_y = IncrementTweenUp(max_horse_random_jump_height);
                }else{
                    horse_texture_offset_y = IncrementTweenDown();
                    // * End Jump Tweening:
                    if (tween_elapsed_time >= horse_jump_down_time){
                        horse_tween_type = HorseTweenType.None;
                    }
                }
            }
        }
        // * Mimics the OnGUI method inside of TransportManager.cs, with some additions to allow dynamic horse positioning.
        void OnGUI(){
            if (!GameManager.Instance.StateManager.GameInProgress || better_riding_view_draw_horse == false){ return; }
            if (Event.current.type.Equals(EventType.Repaint)){
                if (GameManager.Instance.TransportManager.IsOnFoot){
                    return;
                }
                if (riding_texture.texture != null){
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
                        screenRect.x + screenRect.width / 2f - (riding_texture.width * horseScaleX) / 2f + horseOffsetWidth,
                        screenRect.y + screenRect.height - (riding_texture.height * horseScaleY) - horseOffsetHeight,
                        riding_texture.width * horseScaleX,
                        riding_texture.height * horseScaleY
                    );
////////////////////////////////////////////////////////////////////////////////
                    DaggerfallUI.DrawTexture(pos, riding_texture.texture, ScaleMode.StretchToFill, true, GameManager.Instance.TransportManager.Tint);
                }
            }
        }
    }
}
