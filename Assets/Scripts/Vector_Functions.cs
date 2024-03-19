using System;
using System.Collections.Generic;
using UnityEngine;

public class VectorFunctions
{
    public static Vector2 make_vector(int degrees)
    {
        return new Vector2((float)Math.Cos(degrees * Mathf.Deg2Rad), (float)Math.Sin(degrees * Mathf.Deg2Rad));
    }
    
    public static float get_vector_angle(Vector2 vector)
    {
        float angle = (float)Math.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        return (angle < 0 ? angle + 360 : angle);
    }

    public void debug_vector2_list(List<Vector2> list)
    {
        string test = "";
        foreach (Vector2 vector in list)
        {
            test += vector.ToString() + " ";
        }

        Debug.Log(test);
    }
}
