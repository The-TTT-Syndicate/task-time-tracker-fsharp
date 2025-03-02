// Main window definition
namespace TaskTimeTracker

open Avalonia.FuncUI.Hosts
open TaskTimeTracker

type MainWindow() =
    inherit HostWindow()
    do
        base.Title <- "Task Time Tracker"
        base.MinWidth <- 600.0
        base.MinHeight <- 400.0
        base.Width <- 600.0
        base.Height <- 400.0
        base.MaxWidth <- 600.0
        base.MaxHeight <- 400.0
        let view = Main.view()
        base.Content <- view

