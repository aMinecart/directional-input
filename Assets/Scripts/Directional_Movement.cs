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

        float angle = VectorFunctions.get_vector_angle(direction);
        switch (angle)
        {
            case > 337.5f or <= 22.5f:
                return Directionals.EAST;

            case > 22.5f and <= 67.5f:
                return Directionals.NORTHEAST;

            case > 67.5f and <= 112.5f:
                return Directionals.NORTH;

            case > 112.5f and <= 157.5f:
                return Directionals.NORTHWEST;

            case > 157.5f and <= 202.5f:
                return Directionals.WEST;

            case > 202.5f and <= 247.5f:
                return Directionals.SOUTHWEST;
                
            case > 247.5f and <= 292.5f:
                return Directionals.SOUTH;
                
            case > 292.5f and <= 337.5f:
                return Directionals.SOUTHEAST;

            default:
                return Directionals.NONE;
        }
    }

    private (Directionals, Directionals, Directionals, Directionals) get_targets(InputTypes type)
    {
        return type switch
        {
            InputTypes.HCIRCLEFOR => (Directionals.SOUTHEAST, Directionals.SOUTH, Directionals.SOUTHWEST, Directionals.WEST),
            InputTypes.HCIRCLEBACK => (Directionals.SOUTHWEST, Directionals.SOUTH, Directionals.SOUTHEAST, Directionals.EAST),
            InputTypes.DPFOR => (Directionals.SOUTH, Directionals.EAST, Directionals.SOUTHEAST, Directionals.EAST),
            InputTypes.DPBACK => (Directionals.SOUTH, Directionals.WEST, Directionals.SOUTHWEST, Directionals.WEST),
            InputTypes.QCIRCLEFOR => (Directionals.SOUTHEAST, Directionals.SOUTH, Directionals.NULL, Directionals.NULL),
            InputTypes.QCIRCLEBACK => (Directionals.SOUTHWEST, Directionals.SOUTH, Directionals.NULL, Directionals.NULL),
            _ => (Directionals.NULL, Directionals.NULL, Directionals.NULL, Directionals.NULL),
        };
    }

    private bool? find_half_circle(List<Vector2> inputs, int index)
    {
        bool is_forward_input;
        Directionals end_direction = calc_directionals(inputs[index]);

        if (index < 3)
        {
            return null;
        }

        // inputs[index] (last input) must be WEST or EAST
        // the first input must be the opposite
        // otherwise, cannot be a half circle

        if (end_direction == Directionals.EAST)
        {
            is_forward_input = true;
        }
        else if (end_direction == Directionals.WEST)
        {
            is_forward_input = false;
        }
        else
        {
            return null;
        }

        var (minus1_target, minus2_target, minus3_target, minus4_target) = get_targets(is_forward_input ? InputTypes.HCIRCLEFOR : InputTypes.HCIRCLEBACK);

        Directionals minus1_direction = calc_directionals(inputs[index - 1]);
        int start_index = index - 2;

        // any intermediate check for a half circle can be skipped
        // however, only one can be skipped for the input to still be valid

        // this check can be skipped
        if (minus1_direction == minus1_target)
        {
            // skip to the input two inputs before inputs[index]
            while (start_index >= 3 && calc_directionals(inputs[start_index]) == minus1_target)
            {
                start_index--;
            }

            // if the input two inputs before inputs[index] meets the target,
            // skip to the input three inputs before inputs[index]

            // this check can be skipped
            if (calc_directionals(inputs[start_index]) == minus2_target)
            {
                do
                {
                    start_index--;
                } while (start_index >= 2 && calc_directionals(inputs[start_index]) == minus2_target);

                // if the input three inputs before inputs[index] meets the target,
                // skip to the input four inputs before inputs[index]

                // this check can be skipped
                if (calc_directionals(inputs[start_index]) == minus3_target)
                {
                    do
                    {
                        start_index--;
                    } while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus3_target);
                }

                // if the input four (or three, if the previous check was skipped)
                // inputs before inputs[index] meets the target,
                // input is a half circle

                // if all checks passed but there is no input before minus3_target,
                // start_index may be less than 0
                // check that start_index is greater than 0 to prevent an out of bounds error

                // whether the previous check was skipped or not, the final input must be equal to minus4_target
                if (start_index >= 0 && calc_directionals(inputs[start_index]) == minus4_target)
                {
                    return is_forward_input;
                }
            }
            else if (calc_directionals(inputs[start_index]) == minus3_target)
            {
                // minus2_target was skipped, so perform a strict check on remaining inputs

                // skip to the input three inputs before inputs[index]
                do
                {
                    start_index--;
                } while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus3_target);

                // if the input three inputs before inputs[index] meets the target,
                // input is a half circle
                if (calc_directionals(inputs[start_index]) == minus4_target)
                {
                    return is_forward_input;
                }
            }
        }
        else if (minus1_direction == minus2_target)
        {
            // minus1_target was skipped, so perform a strict check on remaining inputs

            // skip to the input two inputs before inputs[index]
            while (start_index >= 2 && calc_directionals(inputs[start_index]) == minus2_target)
            {
                start_index--;
            }

            // if the input two inputs before inputs[index] meets the target,
            // skip to the input three inputs before inputs[index]
            if (calc_directionals(inputs[start_index]) == minus3_target)
            {
                do
                {
                    start_index--;
                } while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus3_target);

                // if the input three inputs before inputs[index] meets the target,
                // input is a half circle
                if (calc_directionals(inputs[start_index]) == minus4_target)
                {
                    return is_forward_input;
                }
            }
        }

        return null;
    }

    private bool? find_half_circle(List<Vector2> inputs, int index, out int start_index)
    {
        bool is_forward_input;
        Directionals end_direction = calc_directionals(inputs[index]);

        if (index < 3)
        {
            start_index = -1;
            return null;
        }

        // inputs[index] (last input) must be WEST or EAST
        // the first input must be the opposite
        // otherwise, cannot be a half circle

        if (end_direction == Directionals.EAST)
        {
            is_forward_input = true;
        }
        else if (end_direction == Directionals.WEST)
        {
            is_forward_input = false;
        }
        else
        {
            start_index = -1;
            return null;
        }

        var (minus1_target, minus2_target, minus3_target, minus4_target) = get_targets(is_forward_input ? InputTypes.HCIRCLEFOR : InputTypes.HCIRCLEBACK);

        Directionals minus1_direction = calc_directionals(inputs[index - 1]);
        start_index = index - 2;

        // any intermediate check for a half circle can be skipped
        // however, only one can be skipped for the input to still be valid
        
        // this check can be skipped
        if (minus1_direction == minus1_target)
        {
            // skip to the input two inputs before inputs[index]
            while (start_index >= 3 && calc_directionals(inputs[start_index]) == minus1_target)
            {
                start_index--;
            }

            // if the input two inputs before inputs[index] meets the target,
            // skip to the input three inputs before inputs[index]

            // this check can be skipped
            if (calc_directionals(inputs[start_index]) == minus2_target)
            {
                do
                {
                    start_index--;
                } while (start_index >= 2 && calc_directionals(inputs[start_index]) == minus2_target);

                // if the input three inputs before inputs[index] meets the target,
                // skip to the input four inputs before inputs[index]

                // this check can be skipped
                if (calc_directionals(inputs[start_index]) == minus3_target)
                {
                    do
                    {
                        start_index--;
                    } while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus3_target);
                }

                // if the input four (or three, if the previous check was skipped)
                // inputs before inputs[index] meets the target,
                // input is a half circle

                // if all checks passed but there is no input before minus3_target,
                // start_index may be less than 0
                // check that start_index is greater than 0 to prevent an out of bounds error

                // whether the previous check was skipped or not, the final input must be equal to minus4_target
                if (start_index >= 0 && calc_directionals(inputs[start_index]) == minus4_target)
                {
                    return is_forward_input;
                }
            }
            else if (calc_directionals(inputs[start_index]) == minus3_target)
            {
                // minus2_target was skipped, so perform a strict check on remaining inputs

                // skip to the input three inputs before inputs[index]
                do
                {
                    start_index--;
                } while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus3_target);
                
                // if the input three inputs before inputs[index] meets the target,
                // input is a half circle
                if (calc_directionals(inputs[start_index]) == minus4_target)
                {
                    return is_forward_input;
                }
            }
        }
        else if (minus1_direction == minus2_target)
        {
            // minus1_target was skipped, so perform a strict check on remaining inputs

            // skip to the input two inputs before inputs[index]
            while (start_index >= 2 && calc_directionals(inputs[start_index]) == minus2_target)
            {
                start_index--;
            }

            // if the input two inputs before inputs[index] meets the target,
            // skip to the input three inputs before inputs[index]
            if (calc_directionals(inputs[start_index]) == minus3_target)
            {
                do
                {
                    start_index--;
                } while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus3_target);

                // if the input three inputs before inputs[index] meets the target,
                // input is a half circle
                if (calc_directionals(inputs[start_index]) == minus4_target)
                {
                    return is_forward_input;
                }
            }
        }

        start_index = -1;
        return null;
    }

    private bool? find_dp(List<Vector2> inputs, int index)
    {
        bool is_forward_input;
        Directionals end_direction = calc_directionals(inputs[index]);

        if (index < 2)
        {
            return null;
        }

        // inputs[index] (last input) must be SOUTHWEST/WEST or SOUTHEAST/EAST
        // the first input must be the opposite
        // otherwise, cannot be a dp

        if (end_direction == Directionals.SOUTHEAST || end_direction == Directionals.EAST)
        {
            is_forward_input = true;
        }
        else if (end_direction == Directionals.SOUTHWEST || end_direction == Directionals.WEST)
        {
            is_forward_input = false;
        }
        else
        {
            return null;
        }

        var (minus1_target, minus2_target, minus3_target, minus4_target) = get_targets(is_forward_input ? InputTypes.DPFOR : InputTypes.DPBACK);

        Directionals minus1_direction = calc_directionals(inputs[index - 1]);
        int start_index = index - 2;


        return null;
    }

    // reminder: clean up the di_test functions' variable initialization
    private InputTypes directional_input_test(List<Vector2> inputs, int index)
    {
        // add return none statement for indices less than 2

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

        Directionals direction1 = calc_directionals(inputs[index - 1]); // the input before input[index]
        if (direction1 == minus2_target)
        {
            // only possible valid inputs are half circles
            // one input (minus1_target) is already missing, so perform a strict check on remaining inputs

            // skip to the input two inputs before inputs[index]
            int start_index = index - 2;
            while (start_index >= 2 && calc_directionals(inputs[start_index]) == minus2_target)
            {
                start_index--;
            }

            // if the input two inputs before inputs[index] meets the target,
            // skip to the input three inputs before inputs[index]
            if (calc_directionals(inputs[start_index]) == minus3_target)
            {
                do
                {
                    start_index--;
                } while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus3_target);

                // if the input three inputs before inputs[index] meets the target, input is a half circle
                if (calc_directionals(inputs[start_index]) == minus4_target)
                {
                    return forward ? InputTypes.HCIRCLEFOR : InputTypes.HCIRCLEBACK;
                }
            }

            /*
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
            */
        }
        else if (direction1 == minus1_target)
        {
            // possible valid inputs are quarter circles, half circles, and "dp"s
            // skip to the input two inputs before inputs[index]
            int start_index = index - 2;
            while (start_index >= 2 && calc_directionals(inputs[start_index]) == minus1_target)
            {
                start_index--;
            }

            if (guaranteed_dp)
            {
                // if the input two inputs before inputs[index] is an optional in-between,
                // skip to the input three inputs before inputs[index]
                if (calc_directionals(inputs[start_index]) == curr_type)
                {
                    do
                    {
                        start_index--;
                    } while (start_index >= 1 && calc_directionals(inputs[start_index]) == curr_type);
                }

                // if the input two (or three) inputs before inputs[index] meets the target, input is a half circle
                if (calc_directionals(inputs[start_index]) == minus2_target)
                {
                    return forward ? InputTypes.DPFOR : InputTypes.DPBACK;
                }

                /*
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
                */

                return InputTypes.NONE;
            }

            // comment out this loop
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

    private InputTypes check_inputs(List<Vector2> inputs)
    {
        for (int i = inputs.Count - 1; i >= 2; i--)
        {
            InputTypes input = directional_input_test(inputs, i);
            if (input != InputTypes.NONE)
            {
                return input;
            }
        }

        return InputTypes.NONE;
    }

    private bool? test(List<Vector2> inputs)
    {
        for (int i = inputs.Count - 1; i >= 0; i--)
        {
            var input = find_half_circle(inputs, i);
            if (input.HasValue)
            {
                return input.Value;
            }
        }

        return null;
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
        
        user_inputs = new List<Vector2>()
        {
            VectorFunctions.make_vector(180),
            VectorFunctions.make_vector(225),
            VectorFunctions.make_vector(270),
            VectorFunctions.make_vector(315),
            VectorFunctions.make_vector(0)
        };

        // debug_vector2_list(user_inputs);
        inputted_directional = check_inputs(user_inputs);
        // print(inputted_directional);

        print(test(user_inputs));
    }
}