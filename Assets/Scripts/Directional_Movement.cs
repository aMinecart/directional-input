using System;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalMovement : MonoBehaviour
{
    private static readonly int buffer_frames = 25;

    [SerializeField] private InputReader input_reader;

    private Vector2 move_direction;
    private List<Vector2> user_inputs = new List<Vector2>(buffer_frames + 1);
    private InputTypes inputted_directional;

    private enum Directionals
    {
        NORTH, SOUTH, WEST, EAST,
        NORTHWEST, NORTHEAST, SOUTHWEST, SOUTHEAST,
        NONE, NULL
    }

    private enum InputTypes
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
    // i.e case Vector2 v when v.x < 0.2 && v.y > 0: return Directionals.NORTH;
    private Directionals calc_directionals(Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            return Directionals.NONE;
        }

        double angle = VectorFunctions.get_vector_angle(direction);
        while (angle > 360)
        {
            angle -= 360;
        }

        while (angle < 0)
        {
            angle += 360;
        }

        switch (angle)
        {
            case > 337.5 or <= 22.5:
                return Directionals.EAST;

            case > 22.5 and <= 67.5:
                return Directionals.NORTHEAST;

            case > 67.5 and <= 112.5:
                return Directionals.NORTH;

            case > 112.5 and <= 157.5:
                return Directionals.NORTHWEST;

            case > 157.5 and <= 202.5:
                return Directionals.WEST;

            case > 202.5 and <= 247.5:
                return Directionals.SOUTHWEST;
                
            case > 247.5 and <= 292.5:
                return Directionals.SOUTH;
                
            case > 292.5 and <= 337.5:
                return Directionals.SOUTHEAST;

            default:
                return Directionals.NONE;
        }
    }

    // reminder: clean up the di_test functions' variable initialization
    private InputTypes directional_input_test(List<Vector2> inputs, int index)
    {
        bool forward; // is the input forwards or backwards?
        bool guaranteed_dp; // is the input guaranteed to be a dp if these conditions are met?

        // the requried directions for an given directional input
        // minus4 is the earliest direction inputted, minus1 is the second to last direction (compared to curr_type)
        Directionals minus1_target;
        Directionals minus2_target;
        Directionals minus3_target;
        Directionals minus4_target;

        // alternate options for targets 3 and 4 
        Directionals alt3_inbetween = Directionals.NULL;
        Directionals alt4_target = Directionals.NULL;

        Directionals curr_type = calc_directionals(inputs[index]); // the end of the input, if it exists
        
        if (curr_type == Directionals.EAST) // could be QCF, HCF, DPF
        {
            minus4_target = Directionals.WEST;
            minus3_target = Directionals.SOUTHWEST;
            minus2_target = Directionals.SOUTH;
            minus1_target = Directionals.SOUTHEAST;

            alt4_target = Directionals.EAST;
            alt3_inbetween = Directionals.SOUTHEAST;

            forward = true;
            guaranteed_dp = false;
        }
        else if (curr_type == Directionals.WEST) // could be QCB, HCB, DPB
        {
            minus4_target = Directionals.EAST;
            minus3_target = Directionals.SOUTHEAST;
            minus2_target = Directionals.SOUTH;
            minus1_target = Directionals.SOUTHWEST;

            alt4_target = Directionals.WEST;
            alt3_inbetween = Directionals.SOUTHWEST;

            forward = false;
            guaranteed_dp = false;
        }
        else if (curr_type == Directionals.SOUTHEAST) // could be DPF
        {
            minus4_target = Directionals.NULL;
            minus3_target = Directionals.NULL;
            minus2_target = Directionals.EAST;
            minus1_target = Directionals.SOUTH;

            forward = true;
            guaranteed_dp = true;
        }
        else if (curr_type == Directionals.SOUTHWEST) // could be DPB
        {
            minus4_target = Directionals.NULL;
            minus3_target = Directionals.NULL;
            minus2_target = Directionals.WEST;
            minus1_target = Directionals.SOUTH;

            forward = false;
            guaranteed_dp = true;
        }
        else // inputs[index] can't be the end of a directional input
        {
            return InputTypes.NONE;
        }

        Directionals direction1 = calc_directionals(inputs[index - 1]);
        if (direction1 == minus2_target)
        {
            // only possible valid inputs are half circles
            for (int i = index - 2; i >= 0; i--)
            {
                Directionals direction2 = calc_directionals(inputs[i]);

                if (direction2 == minus4_target)
                {
                    return forward ? InputTypes.HCIRCLEFOR : InputTypes.HCIRCLEBACK;
                }
                else if (direction2 == minus3_target || direction2 == minus2_target)
                {
                    continue;
                }

                break;
            }
        }
        else if (direction1 == minus1_target)
        {
            // possible valid inputs are quarter circles, half circles, and "dp"s
            if (guaranteed_dp)
            {
                for (int i = index - 2; i >= 0; i--)
                {
                    Directionals direction2 = calc_directionals(inputs[i]);

                    if (direction2 == minus2_target)
                    {
                        return forward ? InputTypes.DPFOR : InputTypes.DPBACK;
                    }
                    else if (direction2 == minus1_target || direction2 == curr_type)
                    {
                        continue;
                    }

                    break;
                }
            }

            for (int i = index - 2; i >= 0; i--)
            {
                Directionals direction2 = calc_directionals(inputs[i]);

                // if true, have not reached new direction yet
                if (direction2 == minus1_target)
                {
                    continue;
                }
                // if true, reached unexpected new direction
                // input[index] is not the end of a valid output
                else if (direction2 != minus2_target)
                {
                    return InputTypes.NONE;
                }

                // input is at least a quarter circle, could still be a half circle or dp
                // check for half circle
                for (int j = i - 1; j >= 0; j--)
                {
                    Directionals direction3 = calc_directionals(inputs[j]);

                    if (direction3 == minus4_target)
                    {
                        return forward ? InputTypes.HCIRCLEFOR : InputTypes.HCIRCLEBACK;
                    }
                    else if (direction3 == minus3_target || direction3 == minus2_target)
                    {
                        continue;
                    }

                    break;
                }

                // check for dp
                for (int j = i - 1; j >= 0; j--)
                {
                    Directionals direction3 = calc_directionals(inputs[j]);

                    if (direction3 == alt4_target)
                    {
                        return forward ? InputTypes.DPFOR : InputTypes.DPBACK;
                    }
                    else if (direction3 == alt3_inbetween || direction3 == minus2_target)
                    {
                        continue;
                    }

                    break;
                }

                // quarter circle has no valid inputs attached to it
                return forward ? InputTypes.QCIRCLEFOR : InputTypes.QCIRCLEBACK;
            }
        }

        // end of function reached, no valid input ending at index found in inputs
        return InputTypes.NONE;
    }
    
    // reminder: clean up harsh_di_test's calculation of input
    private InputTypes harsh_directional_input_test(List<Vector2> inputs, int index)
    {
        bool forward; // is the input forwards or backwards?
        bool guaranteed_dp; // is the input guaranteed to be a dp if these conditions are met?

        // the requried directions for an given directional input
        // minus4 is the earliest direction inputted, minus1 is the second to last direction (compared to curr_type)
        Directionals minus1_target;
        Directionals minus2_target;
        Directionals minus3_target;
        Directionals minus4_target;

        // alternate options for targets 3 and 4 
        Directionals alt3_inbetween = Directionals.NULL;
        Directionals alt4_target = Directionals.NULL;

        Directionals curr_type = calc_directionals(inputs[index]); // the end of the input, if it exists
        if (curr_type == Directionals.EAST) // could be QCF, HCF, DPF
        {
            minus4_target = Directionals.WEST;
            minus3_target = Directionals.SOUTHWEST;
            minus2_target = Directionals.SOUTH;
            minus1_target = Directionals.SOUTHEAST;

            alt4_target = Directionals.EAST;
            alt3_inbetween = Directionals.SOUTHEAST;

            forward = true;
            guaranteed_dp = false;
        }
        else if (curr_type == Directionals.WEST) // could be QCB, HCB, DPB
        {
            minus4_target = Directionals.EAST;
            minus3_target = Directionals.SOUTHEAST;
            minus2_target = Directionals.SOUTH;
            minus1_target = Directionals.SOUTHWEST;

            alt4_target = Directionals.WEST;
            alt3_inbetween = Directionals.SOUTHWEST;

            forward = false;
            guaranteed_dp = false;
        }
        else if (curr_type == Directionals.SOUTHEAST) // could be DPF
        {
            minus4_target = Directionals.NULL;
            minus3_target = Directionals.NULL;
            minus2_target = Directionals.EAST;
            minus1_target = Directionals.SOUTH;

            forward = true;
            guaranteed_dp = true;
        }
        else if (curr_type == Directionals.SOUTHWEST) // could be DPB
        {
            minus4_target = Directionals.NULL;
            minus3_target = Directionals.NULL;
            minus2_target = Directionals.WEST;
            minus1_target = Directionals.SOUTH;

            forward = false;
            guaranteed_dp = true;
        }
        else // can't be a directional input
        {
            return InputTypes.NONE;
        }

        if (calc_directionals(inputs[index - 1]) != minus1_target)
        {
            return InputTypes.NONE;
        }

        for (int j = index - 2; j >= 0; j--)
        {
            if (calc_directionals(inputs[j]) != minus2_target)
            {
                continue;
            }

            if (guaranteed_dp)
            {
                return forward ? InputTypes.DPFOR : InputTypes.DPBACK;
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
                            return forward ? InputTypes.HCIRCLEFOR : InputTypes.HCIRCLEBACK;
                        }
                    }
                }
            }

            if (j >= 1)
            {
                for (int k = j - 1; k >= 0; k--)
                {
                    Directionals test = calc_directionals(inputs[k]);

                    if (test == alt4_target)
                    {
                        return forward ? InputTypes.DPFOR : InputTypes.DPBACK;
                    }
                    else if (test != minus2_target && test != alt3_inbetween)
                    {
                        break;
                    }
                }
            }

            return forward ? InputTypes.QCIRCLEFOR : InputTypes.QCIRCLEBACK;
        }
        
        return InputTypes.NONE;
    }

    private InputTypes check_inputs(List<Vector2> inputs, bool lenient)
    {
        for (int i = inputs.Count - 1; i >= 1; i--)
        {
            InputTypes input = lenient ? directional_input_test(inputs, i) : harsh_directional_input_test(inputs, i);
            if (input != InputTypes.NONE)
            {
                return input;
            }
        }

        return InputTypes.NONE;
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
        inputted_directional = check_inputs(user_inputs, true);
        print(inputted_directional);
    }
}