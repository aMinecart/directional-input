using System;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalMovementV2 : MonoBehaviour
{
    // private static readonly int buffer_frames = 13;
    private static readonly int max_input_length = 25; // idea for default is 10

    [SerializeField] private InputReader input_reader;

    private Vector2 move_vector;
    private Directions move_direction;

    private List<TimedDirection> inputted_directions = new List<TimedDirection>();
    private Inputs current_input;

    private enum Directions
    {
        NORTH, SOUTH, WEST, EAST,
        NORTHWEST, NORTHEAST, SOUTHWEST, SOUTHEAST,
        NONE, NULL
    }

    private enum Inputs
    {
        QCIRCLEFOR,
        QCIRCLEBACK,
        HCIRCLEFOR,
        HCIRCLEBACK,
        DPFOR,
        DPBACK,
        NONE
    }

    private Directions calc_direction(Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            return Directions.NONE;
        }

        float angle = VectorFunctions.get_vector_angle(direction);
        switch (angle)
        {
            case > 337.5f or <= 22.5f:
                return Directions.EAST;

            case > 22.5f and <= 67.5f:
                return Directions.NORTHEAST;

            case > 67.5f and <= 112.5f:
                return Directions.NORTH;

            case > 112.5f and <= 157.5f:
                return Directions.NORTHWEST;

            case > 157.5f and <= 202.5f:
                return Directions.WEST;

            case > 202.5f and <= 247.5f:
                return Directions.SOUTHWEST;
                
            case > 247.5f and <= 292.5f:
                return Directions.SOUTH;
                
            case > 292.5f and <= 337.5f:
                return Directions.SOUTHEAST;

            default:
                return Directions.NONE;
        }
    }

    private (Directions, Directions, Directions, Directions, Directions) get_targets(Inputs type)
    {
        return type switch
        {
            Inputs.HCIRCLEFOR => (Directions.WEST, Directions.SOUTHWEST, Directions.SOUTH, Directions.SOUTHEAST, Directions.EAST),
            Inputs.HCIRCLEBACK => (Directions.EAST, Directions.SOUTHEAST, Directions.SOUTH, Directions.SOUTHWEST, Directions.WEST),
            Inputs.DPFOR => (Directions.EAST, Directions.SOUTHEAST, Directions.SOUTH, Directions.SOUTHEAST, Directions.EAST),
            Inputs.DPBACK => (Directions.WEST, Directions.SOUTHWEST, Directions.SOUTH, Directions.SOUTHWEST, Directions.WEST),
            Inputs.QCIRCLEFOR => (Directions.SOUTH, Directions.SOUTHEAST, Directions.EAST, Directions.NULL, Directions.NULL),
            Inputs.QCIRCLEBACK => (Directions.SOUTH, Directions.SOUTHWEST, Directions.WEST, Directions.NULL, Directions.NULL),
            _ => (Directions.NULL, Directions.NULL, Directions.NULL, Directions.NULL, Directions.NULL)
        };
    }

    // add comments to find_* functions
    private bool? find_quarter_circle(List<TimedDirection> inputs, int index)
    {
        // quarter circle requries a minimum of 3 inputs
        // if less than two values are before index, cannot be a quarter circle
        if (index < 2)
        {
            return null;
        }

        // inputs[index] (last direction) must be WEST or EAST
        // the first direction must be SOUTH
        // otherwise, cannot be a quarter circle

        bool is_forward_input;
        Directions end_direction = inputs[index].direction;

        if (end_direction == Directions.EAST)
        {
            is_forward_input = true;
        }
        else if (end_direction == Directions.WEST)
        {
            is_forward_input = false;
        }
        else
        {
            // cannot be a quarter circle
            return null;
        }

        var (start_target, middle_target, _, _, _) = get_targets(is_forward_input ? Inputs.QCIRCLEFOR : Inputs.QCIRCLEBACK);
        int input_length = 2;

        Directions start_direction = inputs[index - 2].direction;
        TimedDirection middle_timed_direction = inputs[index - 1];

        input_length += middle_timed_direction.frames_active;

        if (start_direction == start_target &&
            middle_timed_direction.direction == middle_target &&
            input_length <= max_input_length)
        {
            return is_forward_input;
        }

        // end of function reached
        // no valid quarter circle input starting at index found in inputs
        return null;
    }

    private bool? find_dp(List<TimedDirection> inputs, int index)
    {
        // dp requries a minimum of 3 inputs
        // if less than two values are before index, cannot be a quarter circle
        if (index < 2)
        {
            return null;
        }

        // inputs[index] (last direction) must be (SOUTH)WEST or (SOUTH)EAST
        // the first direction must be WEST or EAST, respectively
        // otherwise, cannot be a dp

        bool is_forward_input;
        TimedDirection end_timed_direction = inputs[index];

        if (end_timed_direction.direction == Directions.SOUTHEAST || end_timed_direction.direction == Directions.EAST)
        {
            is_forward_input = true;
        }
        else if (end_timed_direction.direction == Directions.SOUTHWEST || end_timed_direction.direction == Directions.WEST)
        {
            is_forward_input = false;
        }
        else
        {
            // cannot be a dp
            return null;
        }

        var (start_target, in_between_target, middle_target, end_target1, end_target2) = get_targets(is_forward_input ? Inputs.DPFOR : Inputs.DPBACK);
        int input_length = 2;

        if (end_timed_direction.direction == end_target2 && index >= 3)
        {
            index--;
            end_timed_direction = inputs[index];
            input_length += end_timed_direction.frames_active;
        }

        if (end_timed_direction.direction != end_target1)
        {
            return null;
        }

        TimedDirection middle_timed_direction = inputs[index - 1];
        input_length += middle_timed_direction.frames_active;

        TimedDirection start_direction = inputs[index - 2];
        if (start_direction.direction == in_between_target && index >= 3)
        {
            input_length += start_direction.frames_active;
            start_direction = inputs[index - 3];
        }

        if (start_direction.direction == start_target &&
            middle_timed_direction.direction == middle_target &&
            input_length <= max_input_length)
        {
            return is_forward_input;
        }

        // end of function reached
        // no valid dp input starting at index found in inputs
        return null;
    }

    /*private Inputs directional_input_test(List<Vector2> inputs, int index)
    {
        bool? hcircle_present = find_half_circle(inputs, index);
        if (hcircle_present.HasValue)
        {
            return (hcircle_present.Value ? Inputs.HCIRCLEFOR : Inputs.HCIRCLEBACK);
        }

        bool? dp_present = find_dp(inputs, index);
        if (dp_present.HasValue)
        {
            return (dp_present.Value ? Inputs.DPFOR : Inputs.DPBACK);
        }

        bool? qcircle_present = find_quarter_circle(inputs, index);
        if (qcircle_present.HasValue)
        {
            return (qcircle_present.Value ? Inputs.QCIRCLEFOR : Inputs.QCIRCLEBACK);
        }

        return Inputs.NONE;
    }*/

    // refactor to use frames_active property of list elements
    /*private Inputs check_inputs(List<Vector2> inputs)
    {
        int min_index = Math.Max(inputs.Count - buffer_frames, 0);

        for (int i = inputs.Count - 1; i >= min_index; i--)
        {
            Inputs input = directional_input_test(inputs, i);
            if (input != Inputs.NONE)
            {
                return input;
            }
        }

        return Inputs.NONE;
    }*/

    // Start is called before the first frame update
    void Start()
    {
        input_reader.MoveEvent += handleMove;
    }

    private void handleMove(Vector2 dir)
    {
        move_vector = dir;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        move_direction = calc_direction(move_vector);

        if (inputted_directions.Count > 0 && move_direction == inputted_directions[^1].direction)
        {
            inputted_directions[^1].AddActiveFrame();
        }
        else
        {
            inputted_directions.Add(new TimedDirection(move_direction));
        }

        /*
        print(
            find_quarter_circle(
                new List<TimedDirection>() {
                    new TimedDirection(Directions.SOUTH, 1),
                    new TimedDirection(Directions.SOUTHEAST, 23),
                    new TimedDirection(Directions.EAST, 1)
                },
                2
            )
        );
        */
        
        /*
        print(
            find_dp(
                new List<TimedDirection>() {
                    new TimedDirection(Directions.EAST, 1),
                    new TimedDirection(Directions.SOUTHEAST, 7),
                    new TimedDirection(Directions.SOUTH, 9),
                    new TimedDirection(Directions.SOUTHEAST, 7),
                    new TimedDirection(Directions.EAST, 1),
                },
                4
            )
        );
        */
    }

    private class TimedDirection
    {
        public Directions direction { get; }
        public int frames_active { get; private set; }

        public TimedDirection(Directions direction, int length = 1)
        {
            this.direction = direction;
            frames_active = length;
        }

        public void AddActiveFrame()
        {
            frames_active++;
        }

        public override string ToString()
        {
            return $"Direction {direction} lasting for {frames_active} frames";
        }
    }
}