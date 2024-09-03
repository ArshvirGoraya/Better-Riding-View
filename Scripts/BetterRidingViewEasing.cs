using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Math = UnityEngine.Mathf;

// Script from: https://github.com/acron0/Easings
// Modified script a little.

namespace BetterRidingViewMod{
    public class BetterRidingViewEasing : MonoBehaviour
    {        
        private const float PI = Math.PI; 
        private const float HALFPI = Math.PI / 2.0f; 

        static public float Interpolate(float p, int functionNum){
            // * functionNum corresponds to the function's position in multiple choice in Mod Settings.
            switch(functionNum){
                default:
                case 0:		    return SineEaseIn(p);
                case 1:		    return SineEaseOut(p);
                case 2:		    return SineEaseInOut(p);
                case 3:		    return QuadraticEaseOut(p);
                case 4:		    return QuadraticEaseIn(p);
                case 5:		    return QuadraticEaseInOut(p);
                case 6:		    return CubicEaseIn(p);
                case 7:		    return CubicEaseOut(p);
                case 8:		    return CubicEaseInOut(p);
                case 9:		    return QuinticEaseIn(p);
                case 10:		return QuinticEaseOut(p);
                case 11:		return QuinticEaseInOut(p);
                case 12:		return CircularEaseIn(p);
                case 13:		return CircularEaseOut(p);
                case 14:		return CircularEaseInOut(p);
                case 15:		return QuarticEaseIn(p);
                case 16:		return QuarticEaseOut(p);
                case 17:		return QuarticEaseInOut(p);
                case 18:		return ExponentialEaseIn(p);
                case 19:		return ExponentialEaseOut(p);
                case 20:	    return ExponentialEaseInOut(p);
                case 21: 		return Linear(p);
            }
        }
        static public float Linear(float p)
        {
            return p;
        }
        // * SINE
        static public float SineEaseIn(float p)
        {
            return Math.Sin((p - 1) * HALFPI) + 1;
        }
        
        static public float SineEaseOut(float p)
        {
            return Math.Sin(p * HALFPI);
        }
        
        static public float SineEaseInOut(float p)
        {
            return 0.5f * (1 - Math.Cos(p * PI));
        }

        // * QUAD
        static public float QuadraticEaseIn(float p)
        {
            return p * p;
        }
        static public float QuadraticEaseOut(float p)
        {
            return -(p * (p - 2));
        }
        
        static public float QuadraticEaseInOut(float p)
        {
            if(p < 0.5f)
            {
                return 2 * p * p;
            }
            else
            {
                return (-2 * p * p) + (4 * p) - 1;
            }
        }

        // * Cubic
        static public float CubicEaseIn(float p)
        {
            return p * p * p;
        }
        static public float CubicEaseOut(float p)
        {
            float f = (p - 1);
            return f * f * f + 1;
        }
        static public float CubicEaseInOut(float p)
        {
            if(p < 0.5f)
            {
                return 4 * p * p * p;
            }
            else
            {
                float f = ((2 * p) - 2);
                return 0.5f * f * f * f + 1;
            }
        }
        // * Quint
        static public float QuinticEaseIn(float p) 
        {
            return p * p * p * p * p;
        }
        static public float QuinticEaseOut(float p) 
        {
            float f = (p - 1);
            return f * f * f * f * f + 1;
        }
        static public float QuinticEaseInOut(float p) 
        {
            if(p < 0.5f)
            {
                return 16 * p * p * p * p * p;
            }
            else
            {
                float f = ((2 * p) - 2);
                return 0.5f * f * f * f * f * f + 1;
            }
        }
        // * Circ
        static public float CircularEaseIn(float p)
        {
            return 1 - Math.Sqrt(1 - (p * p));
        }
        static public float CircularEaseOut(float p)
        {
            return Math.Sqrt((2 - p) * p);
        }
        static public float CircularEaseInOut(float p)
        {
            if(p < 0.5f)
            {
                return 0.5f * (1 - Math.Sqrt(1 - 4 * (p * p)));
            }
            else
            {
                return 0.5f * (Math.Sqrt(-((2 * p) - 3) * ((2 * p) - 1)) + 1);
            }
        }
        // * Quart
        static public float QuarticEaseIn(float p)
        {
            return p * p * p * p;
        }
        static public float QuarticEaseOut(float p)
        {
            float f = (p - 1);
            return f * f * f * (1 - p) + 1;
        }
        static public float QuarticEaseInOut(float p) 
        {
            if(p < 0.5f)
            {
                return 8 * p * p * p * p;
            }
            else
            {
                float f = (p - 1);
                return -8 * f * f * f * f + 1;
            }
        }
        // * Expo
        static public float ExponentialEaseIn(float p)
        {
            return (p == 0.0f) ? p : Math.Pow(2, 10 * (p - 1));
        }
        static public float ExponentialEaseOut(float p)
        {
            return (p == 1.0f) ? p : 1 - Math.Pow(2, -10 * p);
        }
        static public float ExponentialEaseInOut(float p)
        {
            if(p == 0.0 || p == 1.0) return p;
            
            if(p < 0.5f)
            {
                return 0.5f * Math.Pow(2, (20 * p) - 10);
            }
            else
            {
                return -0.5f * Math.Pow(2, (-20 * p) + 10) + 1;
            }
        }
    }
}