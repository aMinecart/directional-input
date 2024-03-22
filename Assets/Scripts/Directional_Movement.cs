using System;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalMovement : MonoBehaviour
{
    private static readonly int buffer_frames = 13;
    private static readonly int max_input_length = 25; // idea for default is 10

    [SerializeField] private InputReader input_reader;

    private Vector2 move_direction;
    private List<Vector2> user_inputs = new List<Vector2>();
    private InputTypes inputted_directional;
    private List<Directionals> test_list = new List<Directionals>();

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
            InputTypes.DPFOR => (Directionals.SOUTH, Directionals.SOUTHEAST, Directionals.EAST, Directionals.NULL),
            InputTypes.DPBACK => (Directionals.SOUTH, Directionals.SOUTHWEST, Directionals.WEST, Directionals.NULL),
            InputTypes.QCIRCLEFOR => (Directionals.SOUTHEAST, Directionals.SOUTH, Directionals.NULL, Directionals.NULL),
            InputTypes.QCIRCLEBACK => (Directionals.SOUTHWEST, Directionals.SOUTH, Directionals.NULL, Directionals.NULL),
            _ => (Directionals.NULL, Directionals.NULL, Directionals.NULL, Directionals.NULL),
        };
    }

    private bool? find_half_circle(List<Vector2> inputs, int index)
    {
        // half circle requries a minimum of 4 inputs
        // if less than three values are before index, cannot be a half
        if (index < 3)
        {
            return null;
        }

        // inputs[index] (last input) must be WEST or EAST
        // the first input must be the opposite
        // otherwise, cannot be a half circle

        bool is_forward_input;
        Directionals end_direction = calc_directionals(inputs[index]);

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

        var (minus1_target, minus2_target, minus3_target, start_target) = get_targets(is_forward_input ? InputTypes.HCIRCLEFOR : InputTypes.HCIRCLEBACK);

        Directionals minus1_direction = calc_directionals(inputs[index - 1]);
        int start_index = index - 2;

        // any intermediate check for a half circle can be skipped
        // however, only one can be skipped for the input to still be valid

        // this check can be skipped
        if (minus1_direction == minus1_target)
        {
            // skip to the input two inputs before inputs[index]
            while (start_index >= 2 && calc_directionals(inputs[start_index]) == minus1_target)
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
                } while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus2_target);

                // if the input three inputs before inputs[index] meets the target,
                // skip to the input four inputs before inputs[index]

                // this check can be skipped
                while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus3_target)
                {
                    start_index--;
                }

                // if the input four (or three, if the previous check was skipped)
                // inputs before inputs[index] meets the target,
                // input is a half circle

                // whether the previous check was skipped or not, the final input must be equal to start_target
                if (calc_directionals(inputs[start_index]) == start_target && index - start_index < max_input_length)
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
                if (calc_directionals(inputs[start_index]) == start_target && index - start_index < max_input_length)
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
                if (calc_directionals(inputs[start_index]) == start_target && index - start_index < max_input_length)
                {
                    return is_forward_input;
                }
            }
        }

        // end of function reached
        // no valid half circle input starting at index found in inputs
        return null;
    }

    private bool? find_half_circle(List<Vector2> inputs, int index, out int start_index)
    {
        // half circle requries a minimum of 4 inputs
        // if less than three values are before index, cannot be a half
        if (index < 3)
        {
            start_index = -1;
            return null;
        }

        // inputs[index] (last input) must be WEST or EAST
        // the first input must be the opposite
        // otherwise, cannot be a half circle

        bool is_forward_input;
        Directionals end_direction = calc_directionals(inputs[index]);

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

        var (minus1_target, minus2_target, minus3_target, start_target) = get_targets(is_forward_input ? InputTypes.HCIRCLEFOR : InputTypes.HCIRCLEBACK);

        Directionals minus1_direction = calc_directionals(inputs[index - 1]);
        start_index = index - 2;

        // any intermediate check for a half circle can be skipped
        // however, only one can be skipped for the input to still be valid

        // this check can be skipped
        if (minus1_direction == minus1_target)
        {
            // skip to the input two inputs before inputs[index]
            while (start_index >= 2 && calc_directionals(inputs[start_index]) == minus1_target)
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
                } while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus2_target);

                // if the input three inputs before inputs[index] meets the target,
                // skip to the input four inputs before inputs[index]

                // this check can be skipped
                while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus3_target)
                {
                    start_index--;
                }

                // if the input four (or three, if the previous check was skipped)
                // inputs before inputs[index] meets the target,
                // input is a half circle

                // whether the previous check was skipped or not, the final input must be equal to start_target
                if (calc_directionals(inputs[start_index]) == start_target && index - start_index < max_input_length)
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
                if (calc_directionals(inputs[start_index]) == start_target && index - start_index < max_input_length)
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
                if (calc_directionals(inputs[start_index]) == start_target && index - start_index < max_input_length)
                {
                    return is_forward_input;
                }
            }
        }

        // end of function reached
        // no valid half circle input starting at index found in inputs
        start_index = -1;
        return null;
    }

    private bool? find_dp(List<Vector2> inputs, int index)
    {
        // if the first input is east or west, could still be a valid dp
        // skip backwards over inputs until a new input is found
        
        // int start_index = index;

        Directionals end_direction = calc_directionals(inputs[index]);
        
        /*
        if (end_direction == Directionals.EAST || end_direction == Directionals.WEST)
        {
            // skip backwards to the next unique direction
            // if index goes below its minimum requirement (2),
            // break out of the loop
            while (start_index >= 2 && calc_directionals(inputs[start_index]) == end_direction)
            {
                start_index--;
            }

            // update end_direction to the new index and skip all concurrent identical directions
            end_direction = calc_directionals(inputs[start_index]);
            while (start_index >= 2 && calc_directionals(inputs[start_index - 1]) == end_direction)
            {
                start_index--;
            }
        }
        */
        
        // dp requries a minimum of 3 inputs
        // if less than two values are before index, cannot be a dp
        if (index < 2)
        {
            return null;
        }

        // inputs[index] (last input) must be SOUTHWEST or SOUTHEAST
        // the first input must be WEST or EAST, respectively
        // otherwise, cannot be a dp

        bool is_forward_input;

        if (end_direction == Directionals.SOUTHEAST)
        {
            is_forward_input = true;
        }
        else if (end_direction == Directionals.SOUTHWEST)
        {
            is_forward_input = false;
        }
        else
        {
            // cannot be a dp
            return null;
        }

        var (minus1_target, minus2_target, start_target, _) = get_targets(is_forward_input ? InputTypes.DPFOR : InputTypes.DPBACK);

        Directionals minus1_direction = calc_directionals(inputs[index - 1]);
        int start_index = index - 2;

        if (minus1_direction == minus1_target)
        {
            while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus1_target)
            {
                start_index--;
            }

            // optional inbetween
            while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus2_target)
            {
                start_index--;
            }

            if (calc_directionals(inputs[start_index]) == start_target && index - start_index < max_input_length)
            {
                return is_forward_input;
            }
        }

        // end of function reached
        // no valid dp input starting at index found in inputs
        return null;
    }

    private bool? find_dp(List<Vector2> inputs, int index, out int start_index)
    {
        // if the first input is east or west, could still be a valid dp
        // skip backwards over inputs until a new input is found
        int new_index = index;
        Directionals end_direction = calc_directionals(inputs[new_index]);

        if (end_direction == Directionals.EAST || end_direction == Directionals.WEST)
        {
            // if index goes below its minimum requirement (2),
            // break out of the loop
            while (new_index >= 2 && calc_directionals(inputs[new_index]) == end_direction)
            {
                new_index--;
            }

            // update end_direction to the new index
            end_direction = calc_directionals(inputs[new_index]);
        }

        // dp requries a minimum of 3 inputs
        // if less than two values are before index, cannot be a dp
        if (new_index < 2)
        {
            start_index = -1;
            return null;
        }

        // inputs[index] (last input) must be SOUTHWEST or SOUTHEAST
        // the first input must be WEST or EAST, respectively
        // otherwise, cannot be a dp

        bool is_forward_input;

        if (end_direction == Directionals.SOUTHEAST)
        {
            is_forward_input = true;
        }
        else if (end_direction == Directionals.SOUTHWEST)
        {
            is_forward_input = false;
        }
        else
        {
            // cannot be a dp
            start_index = -1;
            return null;
        }

        var (minus1_target, minus2_target, start_target, _) = get_targets(is_forward_input ? InputTypes.DPFOR : InputTypes.DPBACK);

        Directionals minus1_direction = calc_directionals(inputs[new_index - 1]);
        start_index = new_index - 2;

        if (minus1_direction == minus1_target)
        {
            while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus1_target)
            {
                start_index--;
            }

            // optional inbetween
            while (start_index >= 1 && calc_directionals(inputs[start_index]) == minus2_target)
            {
                start_index--;
            }

            if (calc_directionals(inputs[start_index]) == start_target && index - start_index < max_input_length)
            {
                return is_forward_input;
            }
        }

        // end of function reached
        // no valid dp input starting at index found in inputs
        start_index = -1;
        return null;
    }

    // add comments to quarter circle functions
    private bool? find_quarter_circle(List<Vector2> inputs, int index)
    {
        // quarter circle requries a minimum of 3 inputs
        // if less than two values are before index, cannot be a quarter circle
        if (index < 2)
        {
            return null;
        }

        // inputs[index] (last input) must be WEST or EAST
        // the first input must be SOUTH
        // otherwise, cannot be a quarter circle

        bool is_forward_input;
        Directionals end_direction = calc_directionals(inputs[index]);

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
            // cannot be a quarter circle
            return null;
        }

        var (middle_target, start_target, _, _) = get_targets(is_forward_input ? InputTypes.QCIRCLEFOR : InputTypes.QCIRCLEBACK);

        Directionals minus1_direction = calc_directionals(inputs[index - 1]);
        int start_index = index - 2;

        if (minus1_direction == middle_target)
        {
            while (start_index >= 1 && calc_directionals(inputs[start_index]) == middle_target)
            {
                start_index--;
            }

            if (calc_directionals(inputs[start_index]) == start_target && index - start_index < max_input_length)
            {
                return is_forward_input;
            }
        }

        // end of function reached
        // no valid quarter circle input starting at index found in inputs
        return null;
    }

    private bool? find_quarter_circle(List<Vector2> inputs, int index, out int start_index)
    {
        // quarter circle requries a minimum of 3 inputs
        // if less than two values are before index, cannot be a quarter circle
        if (index < 2)
        {
            start_index = -1;
            return null;
        }

        // inputs[index] (last input) must be WEST or EAST
        // the first input must be SOUTH
        // otherwise, cannot be a quarter circle

        bool is_forward_input;
        Directionals end_direction = calc_directionals(inputs[index]);

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
            // cannot be a quarter circle
            start_index = -1;
            return null;
        }

        var (middle_target, start_target, _, _) = get_targets(is_forward_input ? InputTypes.QCIRCLEFOR : InputTypes.QCIRCLEBACK);

        Directionals minus1_direction = calc_directionals(inputs[index - 1]);
        start_index = index - 2;

        if (minus1_direction == middle_target)
        {
            while (start_index >= 1 && calc_directionals(inputs[start_index]) == middle_target)
            {
                start_index--;
            }

            if (calc_directionals(inputs[start_index]) == start_target && index - start_index < max_input_length)
            {
                return is_forward_input;
            }
        }

        // end of function reached
        // no valid quarter circle input starting at index found in inputs
        start_index = -1;
        return null;
    }

    private InputTypes directional_input_test(List<Vector2> inputs, int index)
    {
        bool? hcircle_present = find_half_circle(inputs, index);
        if (hcircle_present != null)
        {
            return (hcircle_present.Value ? InputTypes.HCIRCLEFOR : InputTypes.HCIRCLEBACK);
        }

        bool? dp_present = find_dp(inputs, index);
        if (dp_present != null)
        {
            return (dp_present.Value ? InputTypes.DPFOR : InputTypes.DPBACK);
        }

        bool? qcircle_present = find_quarter_circle(inputs, index);
        if (qcircle_present != null)
        {
            return (qcircle_present.Value ? InputTypes.QCIRCLEFOR : InputTypes.QCIRCLEBACK);
        }

        return InputTypes.NONE;
    }

    // adjust for loop condition back to i >= 2 if necessary/when finished testing
    private InputTypes check_inputs(List<Vector2> inputs)
    {
        int min_index = Math.Max(inputs.Count - buffer_frames, 0);

        for (int i = inputs.Count - 1; i >= min_index; i--)
        {
            InputTypes input = directional_input_test(inputs, i);
            if (input != InputTypes.NONE)
            {
                return input;
            }
        }

        return InputTypes.NONE;
    }

    /* function for testing the results of check_inputs */
    private void test_check_inputs()
    {
        List<Vector2> test_inputs = new List<Vector2>()
        {
            VectorFunctions.make_vector(0),
            VectorFunctions.make_vector(0),
            VectorFunctions.make_vector(0),
            VectorFunctions.make_vector(0),
            VectorFunctions.make_vector(0),
            VectorFunctions.make_vector(315),
            VectorFunctions.make_vector(315),
            VectorFunctions.make_vector(315),
            VectorFunctions.make_vector(270),
            VectorFunctions.make_vector(270),
            VectorFunctions.make_vector(270),
            VectorFunctions.make_vector(270),
            VectorFunctions.make_vector(315),
            VectorFunctions.make_vector(315),
            VectorFunctions.make_vector(0),
            VectorFunctions.make_vector(0),
            VectorFunctions.make_vector(0)
        };

        print(check_inputs(test_inputs));
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
        //test_check_inputs();

        user_inputs.Add(move_direction);
        // VectorFunctions.debug_vector2_list(user_inputs);

        /*
        Directionals test = calc_directionals(move_direction);
        if (test != Directionals.NONE)
        {
            test_list.Add(test);
        }
        */

        inputted_directional = check_inputs(user_inputs);
        print(inputted_directional);

        /*
        if (inputted_directional != InputTypes.NONE)
        {
            print(inputted_directional);

            string test_string = "";
            foreach (var item in test_list)
            {
                test_string += item.ToString() + " ";
            }

            print(test_string);

            test_list.Clear();
        }
        */
    }
}