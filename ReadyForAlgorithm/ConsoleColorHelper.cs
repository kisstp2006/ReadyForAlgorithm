namespace Program;

internal static class ConsoleColorHelper
{
    public static void SetColorForCell(char cell)
    {
        switch (cell)
        {
            case '&':
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case 'S':
                Console.ForegroundColor = ConsoleColor.Cyan;
                break;
            case 'G':
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case 'Y':
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case 'B':
                Console.ForegroundColor = ConsoleColor.Blue;
                break;
            case '#':
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
            case '.':
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            default:
                Console.ResetColor();
                break;
        }
    }

    public static void WriteColoredCell(char cell)
    {
        SetColorForCell(cell);
        Console.Write(cell);
        Console.ResetColor();
    }
}
