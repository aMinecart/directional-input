using System.Collections.Generic;
using UnityEngine;

// note: for some reason, dp inputs linger one or two extra frames
// when forward (for DPFOR) or backward (for DPBACK) is held after the input
public class DirectionalMovementV2 : MonoBehaviour
{
    private static readonly int buffer_frames = 13;
    private static readonly int max_input_length = 10; // idea for default is 10

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
        // quarter circle requires a minimum of 3 inputs
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
        // no valid quarter circle input ending at index found in inputs
        return null;
    }

    private bool? find_dp(List<TimedDirection> inputs, int index)
    {
        // dp requires a minimum of 3 inputs
        // if less than two values are before index, cannot be a dp
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
        // no valid dp input ending at index found in inputs
        return null;
    }

    private bool? find_half_circle(List<TimedDirection> inputs, int index)
    {
        // half circle requires a minimum of 4 inputs
        // if less than three values are before index, cannot be a half circle
        if (index < 3)
        {
            return null;
        }

        // inputs[index] (last direction) must be WEST or EAST
        // the first direction must be the opposite
        // otherwise, cannot be a half circle

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
            // cannot be a half circle
            return null;
        }

        var (start_target, middle_target_3, middle_target_2, middle_target_1, _) = get_targets(is_forward_input ? Inputs.HCIRCLEFOR : Inputs.HCIRCLEBACK);
        int input_length = 2;

        TimedDirection minus1_timed_direction = inputs[index - 1];
        TimedDirection minus2_timed_direction = inputs[index - 2];
        TimedDirection minus3_timed_direction = inputs[index - 3];
        Directions minus4_direction = (index >= 4 ? inputs[index - 4].direction : Directions.NULL);

        input_length += minus1_timed_direction.frames_active;
        input_length += minus2_timed_direction.frames_active;

        if (minus1_timed_direction.direction == middle_target_1 &&
            minus2_timed_direction.direction == middle_target_2 &&
            minus3_timed_direction.direction == middle_target_3 &&
            minus4_direction == start_target)
        {
            input_length += minus3_timed_direction.frames_active;
            if (input_length <= max_input_length)
            {
                return is_forward_input;
            }
        }
        else if (minus1_timed_direction.direction == middle_target_1 &&
                minus2_timed_direction.direction == middle_target_2 &&
                minus3_timed_direction.direction == start_target &&
                input_length <= max_input_length)
        {
            return is_forward_input;
        }
        else if (minus1_timed_direction.direction == middle_target_1 &&
                minus2_timed_direction.direction == middle_target_3 &&
                minus3_timed_direction.direction == start_target &&
                input_length <= max_input_length)
        {
            return is_forward_input;
        }
        else if (minus1_timed_direction.direction == middle_target_2 &&
                minus2_timed_direction.direction == middle_target_3 &&
                minus3_timed_direction.direction == start_target &&
                input_length <= max_input_length)
        {
            return is_forward_input;
        }

        // end of function reached
        // no valid half circle input ending at index found in inputs
        return null;
    }

    private Inputs find_directional_input(List<TimedDirection> inputs, int index)
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
    }

    private Inputs check_inputs(List<TimedDirection> inputs)
    {
        int time_since_input = 0;
        for (int i = inputs.Count - 1; i >= 0; i--)
        {
            time_since_input += inputs[i].frames_active;
            if (time_since_input > buffer_frames)
            {
                break;
            }

            Inputs input = find_directional_input(inputs, i);
            if (input != Inputs.NONE)
            {
                return input;
            }
        }

        return Inputs.NONE;
    }

    private void test_find_di()
    {
        print(
            find_directional_input(
                new List<TimedDirection>() {
                    new TimedDirection(Directions.SOUTH, 1),
                    new TimedDirection(Directions.SOUTHEAST, 23),
                    new TimedDirection(Directions.EAST, 1)
                },
                2
            )
        );

        print(
            find_directional_input(
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

        print(
            find_directional_input(
                new List<TimedDirection>() {
                    new TimedDirection(Directions.WEST, 1),
                    new TimedDirection(Directions.SOUTHWEST, 9),
                    new TimedDirection(Directions.SOUTH, 5),
                    new TimedDirection(Directions.SOUTHEAST, 9),
                    new TimedDirection(Directions.EAST, 1)
                },
                4
            )
        );
    }

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

        current_input = check_inputs(inputted_directions);
        print(current_input);
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