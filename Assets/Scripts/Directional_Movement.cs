using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class DirectionalMovement : MonoBehaviour
{
    private static int buffer_frames = 13;

    private static readonly Vector2 northwest = Vector2.ClampMagnitude(new Vector2(-1, 1), 1);
    private static readonly Vector2 northeast = Vector2.ClampMagnitude(new Vector2(1, 1), 1);
    private static readonly Vector2 southwest = Vector2.ClampMagnitude(new Vector2(-1, -1), 1);
    private static readonly Vector2 southeast = Vector2.ClampMagnitude(new Vector2(1, -1), 1);

    [SerializeField] private InputReader input_reader;

    private Vector2 move_direction;
    private List<Vector2> user_inputs = new List<Vector2>(buffer_frames + 1);
    private input_types inputted_directional;

    private enum directionals
    {
        NORTH, SOUTH, WEST, EAST,
        NORTHWEST, NORTHEAST, SOUTHWEST, SOUTHEAST,
        UNKNOWN
    }

    private enum input_types
    {
        QCIRCLEFOR,
        QCIRCLEBACK,
        HCIRCLEFOR,
        HCIRCLEBACK,
        NONE
    }

    // update to use range checks instead of equalities for stick compatibility
    // i.e case Vector2 v when v.x < 0.2 && v.y > 0: return directionals.NORTH;
    private directionals calc_directionals(Vector2 direction)
    {
        switch (direction)
        {
            case Vector2 v when v == Vector2.up: return directionals.NORTH;
            case Vector2 v when v == Vector2.down: return directionals.SOUTH;
            case Vector2 v when v == Vector2.left: return directionals.WEST;
            case Vector2 v when v == Vector2.right: return directionals.EAST;
            case Vector2 v when v == northwest: return directionals.NORTHWEST;
            case Vector2 v when v == northeast: return directionals.NORTHEAST;
            case Vector2 v when v == southwest: return directionals.SOUTHWEST;
            case Vector2 v when v == southeast: return directionals.SOUTHEAST;
            default: return directionals.UNKNOWN;
        }
    }

    private input_types test_input_for_direction(List<Vector2> inputs, int index)
    {
        /*
            curr_type is the type of the Vector2 input in inputs currently being checked
            *_test variables represent the type of input needed for a directional input
            in relation to inputs[index]
        */

        bool forward;

        directionals prev_test;
        directionals after_test;
        directionals before1_test;
        directionals before2_test;

        directionals curr_type = calc_directionals(inputs[index]);
        if (curr_type == directionals.SOUTHEAST)
        {
            before2_test = directionals.WEST;
            before1_test = directionals.SOUTHWEST;
            prev_test = directionals.SOUTH;
            after_test = directionals.EAST;

            forward = true;
        }
        else if (curr_type == directionals.SOUTHWEST)
        {
            before2_test = directionals.EAST;
            before1_test = directionals.SOUTHEAST;
            prev_test = directionals.SOUTH;
            after_test = directionals.WEST;

            forward = false;
        }
        else
        {
            return input_types.NONE;
        }

        if (calc_directionals(inputs[index + 1]) != after_test)
        {
            return input_types.NONE;
        }

        for (int j = index - 1; j >= 0; j--)
        {
            if (calc_directionals(inputs[j]) != prev_test)
            {
                continue;
            }

            // a quarter circle has been inputted
            // check if there are 2 or more inputs before inputs[j]
            // if so, a half circle may have been input
            if (j >= 2)
            {
                for (int k = j - 1; k >= 1; k--)
                {
                    if (calc_directionals(inputs[k]) != before1_test)
                    {
                        continue;
                    }

                    for (int l = k - 1; l >= 0; l--)
                    {
                        if (calc_directionals(inputs[l]) == before2_test)
                        {
                            return forward ? input_types.HCIRCLEFOR : input_types.HCIRCLEBACK;
                        }
                    }
                }
            }

            return forward ? input_types.QCIRCLEFOR : input_types.QCIRCLEBACK;
        }

        return input_types.NONE;
    }

    private input_types check_inputs(List<Vector2> inputs)
    {
        for (int i = inputs.Count - 2; i >= 1; i--)
        {
            input_types input = test_input_for_direction(inputs, i);
            if (input != input_types.NONE)
            {
                return input;
            }
        }

        return input_types.NONE;
    }

    private void debug_vector2_list(List<Vector2> list)
    {
        string test = "";
        foreach (Vector2 vector in list)
        {
            test += vector.ToString() + " ";
        }

        print(test);
    }

    // Start is called before the first frame update
    void Start()
    {
        input_reader.MoveEvent += handleMove;
    }

    private void handleMove(Vector2 dir)
    {
        move_direction = dir;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        user_inputs.Add(move_direction);
        if (user_inputs.Count > buffer_frames)
        {
            user_inputs.RemoveAt(0);
        }

        // debug_vector2_list(user_inputs);
        inputted_directional = check_inputs(user_inputs);
        print(inputted_directional);
    }
}