namespace ReadyForAlgorithm.Core;

public static class PathPlanner
{
    public static (GridPosition Start, List<GridPosition> Goals) FindStartAndGoals(char[,] grid)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);

        GridPosition start = new(0, 0);
        List<GridPosition> goals = new();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char cell = grid[y, x];
                if (cell == 'S')
                {
                    start = new GridPosition(x, y);
                }
                else if (cell == 'G' || cell == 'Y' || cell == 'B')
                {
                    goals.Add(new GridPosition(x, y));
                }
            }
        }

        return (start, goals);
    }

    public static List<GridPosition> BuildMissionPath(char[,] grid)
    {
        (GridPosition start, List<GridPosition> goals) = FindStartAndGoals(grid);
        List<GridPosition> completePath = new() { start };
        GridPosition rover = start;
        List<GridPosition> remainingGoals = new(goals);

        while (remainingGoals.Count > 0)
        {
            GridPosition nearestGoal = remainingGoals
                .OrderBy(goal => Math.Abs(goal.X - rover.X) + Math.Abs(goal.Y - rover.Y))
                .First();

            List<GridPosition> pathToGoal = FindPath(grid, rover, new[] { nearestGoal });
            if (pathToGoal.Count == 0)
            {
                break;
            }

            completePath.AddRange(pathToGoal.Skip(1));
            rover = nearestGoal;
            grid[rover.Y, rover.X] = '.';
            remainingGoals.Remove(nearestGoal);
        }

        List<GridPosition> returnPath = FindPath(grid, rover, new[] { start });
        if (returnPath.Count > 0)
        {
            completePath.AddRange(returnPath.Skip(1));
        }

        return completePath;
    }

    public static List<GridPosition> FindPath(char[,] grid, GridPosition start, IEnumerable<GridPosition> goals)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        HashSet<GridPosition> goalSet = new(goals);

        GridPosition end = new(-1, -1);
        bool[,] visited = new bool[height, width];
        GridPosition[,] parent = new GridPosition[height, width];

        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                parent[row, column] = new GridPosition(-1, -1);
            }
        }

        Queue<GridPosition> queue = new();
        queue.Enqueue(start);
        visited[start.Y, start.X] = true;

        int[,] directions =
        {
            { 1, 0 },
            { -1, 0 },
            { 0, 1 },
            { 0, -1 },
            { 1, 1 },
            { 1, -1 },
            { -1, 1 },
            { -1, -1 }
        };

        while (queue.Count > 0)
        {
            GridPosition current = queue.Dequeue();
            if (goalSet.Contains(current))
            {
                end = current;
                break;
            }

            for (int i = 0; i < 8; i++)
            {
                int nextX = current.X + directions[i, 0];
                int nextY = current.Y + directions[i, 1];

                if (nextX >= 0 && nextX < width && nextY >= 0 && nextY < height)
                {
                    if (!visited[nextY, nextX] && grid[nextY, nextX] != '#')
                    {
                        visited[nextY, nextX] = true;
                        GridPosition next = new(nextX, nextY);
                        queue.Enqueue(next);
                        parent[nextY, nextX] = current;
                    }
                }
            }
        }

        if (end.X == -1 && end.Y == -1)
        {
            return new List<GridPosition>();
        }

        List<GridPosition> path = new();
        GridPosition step = end;
        while (step.X != -1 && step.Y != -1)
        {
            path.Add(step);
            step = parent[step.Y, step.X];
        }

        path.Reverse();
        return path;
    }
}