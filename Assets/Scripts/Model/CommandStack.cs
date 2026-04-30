using System.Collections.Generic;

/// <summary>
/// Manages a stack of commands for undo/redo functionality.
/// Replaces the static State class with an instance-based, testable approach.
/// </summary>
public class CommandStack
{
    private List<ICommand> commands;
    private int currentIndex;

    public CommandStack()
    {
        commands = new List<ICommand>();
        currentIndex = -1;
    }

    /// <summary>
    /// Number of commands that can be undone.
    /// </summary>
    public int UndoCount => currentIndex + 1;

    /// <summary>
    /// Number of commands that can be redone.
    /// </summary>
    public int RedoCount => commands.Count - 1 - currentIndex;

    /// <summary>
    /// Executes a command and adds it to the stack.
    /// Clears any redo history.
    /// </summary>
    public void Execute(ICommand command)
    {
        command.Execute();

        // Remove any redo history
        if (currentIndex < commands.Count - 1)
        {
            commands.RemoveRange(currentIndex + 1, commands.Count - currentIndex - 1);
        }

        commands.Add(command);
        currentIndex++;
    }

    /// <summary>
    /// Undoes the most recent command if available.
    /// Returns true if a command was undone.
    /// </summary>
    public bool Undo()
    {
        if (currentIndex < 0)
            return false;

        commands[currentIndex].Undo();
        currentIndex--;
        return true;
    }

    /// <summary>
    /// Redoes the most recently undone command if available.
    /// Returns true if a command was redone.
    /// </summary>
    public bool Redo()
    {
        if (currentIndex >= commands.Count - 1)
            return false;

        currentIndex++;
        commands[currentIndex].Execute();
        return true;
    }

    /// <summary>
    /// Clears all commands from the stack.
    /// </summary>
    public void Clear()
    {
        commands.Clear();
        currentIndex = -1;
    }

    /// <summary>
    /// Resets the stack to the initial state, undoing all commands.
    /// </summary>
    public void ResetToBeginning()
    {
        while (Undo())
        {
            // Undo all commands
        }
    }
}
