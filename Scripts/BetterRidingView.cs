// Project:         BetterRidingView for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2024 Arshvir Goraya
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Arshvir Goraya
// Origin Date:     August 31 2024
// Source Code:     https://github.com/ArshvirGoraya/Better-Riding-View

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace BetterRidingViewMod
{
    public class BetterRidingView : MonoBehaviour
    {
        const float STRAIGHT_ANGLE = 360;
        const float UP_ANGLE = 270;

        float camera_angle_x = 0;
        float normalized_angle_x = 0;
        private static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<BetterRidingView>();
            mod.IsReady = true;
        }

        // private void Start()
        // {
            
        // }

        public static float NormalizeValue(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }

        private void Update(){
            camera_angle_x = GameManager.Instance.MainCamera.transform.eulerAngles.x;
            if (camera_angle_x < UP_ANGLE){camera_angle_x = 360;}

            // camera_angle_x = Mathf.Clamp(GameManager.Instance.MainCamera.transform.eulerAngles.x, STRAIGHT_ANGLE, UP_ANGLE);
            // camera_angle_x = Mathf.Max(0, GameManager.Instance.MainCamera.transform.eulerAngles.x);
            // if (camera_angle_x == 0) camera_angle_x = STRAIGHT_ANGLE;

            Debug.Log($"camera_angle_x: {camera_angle_x}");

            normalized_angle_x = NormalizeValue(camera_angle_x, STRAIGHT_ANGLE, UP_ANGLE);

            Debug.Log($"normalized_angle_x: {normalized_angle_x}");
            
        }
    }
}
