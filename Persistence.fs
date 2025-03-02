// Saving and loading tasks to/from JSON
namespace TaskTimeTracker

open System
open System.IO
open System.Text.Json

module Persistence =
    let filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "task-time-tracker", "tasks.json")

    let saveTasksToFile tasks =
        let directory = Path.GetDirectoryName(filePath)
        if not (Directory.Exists(directory)) then
            Directory.CreateDirectory(directory) |> ignore
        let json = JsonSerializer.Serialize(tasks)
        File.WriteAllText(filePath, json)

    let loadTasksFromFile () =
        if File.Exists(filePath) then
            let json = File.ReadAllText(filePath)
            JsonSerializer.Deserialize<(string * TimeSpan) list>(json)
        else
            [("New task", TimeSpan.Zero)]

