using System;
using System.Collections.Generic;
using UnityEngine;

// add leniency for half circle and solo dp checks
public class DirectionalMovement : MonoBehaviour
{
    private static int buffer_frames = 25;

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
        UNKNOWN, NONE
    }

    private enum input_types
    {
        QCIRCLEFOR,
        QCIRCLEBACK,
        HCIRCLEFOR,
        HCIRCLEBACK,
        DPFOR,
        DPBACK,
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
        bool forward; // is the input forwards or backwards?
        bool guaranteed_dp; // is the input guaranteed to be a dp if these conditions are met?

        // the requried directions for an given directional input
        // minus4 is the earliest input, minus1 is the second to last input (compared to curr_type)
        directionals minus1_target;
        directionals minus2_target;
        directionals minus3_target;
        directionals minus4_target;

        // alternate options for targets 3 and 4 
        directionals alt3_inbetween = directionals.NONE;
        directionals alt4_target = directionals.NONE;

        directionals curr_type = calc_directionals(inputs[index]); // the reference input
        if (curr_type == directionals.EAST) // could be QCF, HCF, DPF
        {
            minus4_target = directionals.WEST;
            minus3_target = directionals.SOUTHWEST;
            minus2_target = directionals.SOUTH;
            minus1_target = directionals.SOUTHEAST;

            alt4_target = directionals.EAST;
            alt3_inbetween = directionals.SOUTHEAST;

            forward = true;
            guaranteed_dp = false;
        }
        else if (curr_type == directionals.WEST) // could be QCB, HCB, DPB
        {
            minus4_target = directionals.EAST;
            minus3_target = directionals.SOUTHEAST;
            minus2_target = directionals.SOUTH;
            minus1_target = directionals.SOUTHWEST;

            alt4_target = directionals.WEST;
            alt3_inbetween = directionals.SOUTHWEST;

            forward = false;
            guaranteed_dp = false;
        }
        else if (curr_type == directionals.SOUTHEAST) // could be DPF
        {
            minus4_target = directionals.NONE;
            minus3_target = directionals.NONE;
            minus2_target = directionals.EAST;
            minus1_target = directionals.SOUTH;

            forward = true;
            guaranteed_dp = true;
        }
        else if (curr_type == directionals.SOUTHWEST) // could be DPB
        {
            minus4_target = directionals.NONE;
            minus3_target = directionals.NONE;
            minus2_target = directionals.WEST;
            minus1_target = directionals.SOUTH;

            forward = false;
            guaranteed_dp = true;
        }
        else // can't be a directional input
        {
            return input_types.NONE;
        }
        
        for (int i = index - 1; i >= 1; i--)
        {
            directionals test = calc_directionals(inputs[i]);
            if (test == minus2_target)
            {
                for (int j = i - 1; i >= 0; i--)
                {
                    directionals test2 = calc_directionals(inputs[i]);
                    if (test2 ==  minus4_target)
                    {
                        return forward ? input_types.HCIRCLEFOR : input_types.HCIRCLEBACK;
                    }
                    else if (test2 != minus3_target && test2 != minus2_target)
                    {
                        break;
                    }
                }
            }
            else if (test != minus1_target && test != curr_type)
            {
                break;
            }
        }
        
        if (calc_directionals(inputs[index - 1]) != minus1_target)
        {
            return input_types.NONE;
        }

        for (int j = index - 1; j >= 0; j--)
        {
            if (guaranteed_dp)
            {
                directionals test = calc_directionals(inputs[j]);
                if (test == minus2_target)
                {
                    return forward ? input_types.DPFOR : input_types.DPBACK;
                }
                else if (test != minus1_target && test != curr_type)
                {
                    break;
                }
            }
            
            if (calc_directionals(inputs[j]) != minus2_target)
            {
                continue;
            }
            
            // a quarter circle has been inputted
            // check if there are any inputs before inputs[j]
            // if so, a half circle or dp may have been inputted
            if (j >= 2)
            {
                for (int k = j - 1; k >= 1; k--)
                {
                    if (calc_directionals(inputs[k]) != minus3_target)
                    {
                        continue;
                    }

                    for (int l = k - 1; l >= 0; l--)
                    {
                        if (calc_directionals(inputs[l]) == minus4_target)
                        {
                            return forward ? input_types.HCIRCLEFOR : input_types.HCIRCLEBACK;
                        }
                    }
                }
            }

            if (j >= 1)
            {
                for (int k = j - 1; k >= 0; k--)
                {
                    directionals test = calc_directionals(inputs[k]);

                    if (test == alt4_target)
                    {
                        return forward ? input_types.DPFOR : input_types.DPBACK;
                    }
                    else if (test != minus2_target && test != alt3_inbetween)
                    {
                        break;
                    }
                }
            }

            return forward ? input_types.QCIRCLEFOR : input_types.QCIRCLEBACK;
        }

        return input_types.NONE;
    }

    private input_types check_inputs(List<Vector2> inputs)
    {
        for (int i = inputs.Count - 1; i >= 1; i--)
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