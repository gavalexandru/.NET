using System;
using System.Collections.Generic;
using System.Linq;
// ===== Static Lambda for Filtering =====
Func<Task, bool> OverdueFilter = static t => !t.IsCompleted && t.DueDate < DateTime.Now;

// ===== Top-level statements =====

// Create initial project
var project = new Project("AI Chatbot", new List<Task>
{
    new Task("Design conversation flow", false, DateTime.Now.AddDays(-2)),
    new Task("Implement model integration", true, DateTime.Now.AddDays(-1)),
    new Task("Write documentation", false, DateTime.Now.AddDays(3))
});

// Clone project with 'with' expression to add a new task
var newTask = new Task("Add user feedback system", false, DateTime.Now.AddDays(5));
var updatedProject = project with { Tasks = new List<Task>(project.Tasks) { newTask } };

// Display project info
Console.WriteLine("Pattern Matching:\n");
PatternMatching.DisplayInfo(updatedProject);

// ===== User Interaction =====
while (true)
{
    Console.WriteLine("Enter a new task title (or type 'EXIT' to finish):\n");
    string title = Console.ReadLine() ?? "Untitled Task";
    if (title.Trim().ToUpper() == "EXIT")
        break;

    Console.WriteLine("Is the task completed? (y/n):");
    bool isCompleted = Console.ReadLine()?.Trim().ToLower() == "y";

    var userTask = new Task(title, isCompleted, DateTime.Now.AddDays(2));
    updatedProject.Tasks.Add(userTask);

    Console.WriteLine($"Task '{title}' added successfully!");
}

// ===== Display all tasks =====
Console.WriteLine("\nAll Tasks:");
foreach (var t in updatedProject.Tasks)
{
    Console.WriteLine($"- {t.Title} | Completed: {t.IsCompleted} | Due: {t.DueDate:d}");
}

// ===== Filter overdue tasks using static lambda =====
var overdueTasks = updatedProject.Tasks.Where(OverdueFilter).ToList();

Console.WriteLine("\nOverdue and not completed:");
foreach (var t in overdueTasks)
{
    Console.WriteLine($"- {t.Title} (Due: {t.DueDate:d})");
}

// ===== Manager Info =====
var manager = new Manager
{
    Name = "Alice Johnson",
    Team = "AI Team",
    Email = "alice@company.com"
};
Console.WriteLine($"\nManager: {manager.Name}, Team: {manager.Team}, Email: {manager.Email}");

// ===== Records =====
public record Task(string Title, bool IsCompleted, DateTime DueDate);

public record Project(string Name, List<Task> Tasks);

// ===== Manager class  =====
public class Manager
{
    public string Name { get; init; }
    public string Team { get; init; }
    public string Email { get; init; }
    
}

// ===== Pattern Matching =====
class PatternMatching
{
    public static void DisplayInfo(object obj)
    {
        switch (obj)
        {
            case Task t:
                Console.WriteLine($"Task: {t.Title} | Completed: {t.IsCompleted}");
                break;
            case Project p:
                Console.WriteLine($"Project: {p.Name} | Tasks: {p.Tasks.Count}");
                break;
            default:
                Console.WriteLine("Unknown type");
                break;
        }
    }
}
