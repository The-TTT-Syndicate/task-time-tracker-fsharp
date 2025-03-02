// Core UI logic and components
namespace TaskTimeTracker

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open System
open System.Timers

module Main =
    let view () =
        Component(fun ctx ->
            // States
            let runningTaskIndex = ctx.useState None
            let tasks = ctx.useState (Persistence.loadTasksFromFile())
            let timerState = ctx.useState<Timer option>(None)
            let autoSaveTimer = new Timer(60000.0)
            autoSaveTimer.AutoReset <- true

            // Timer handler for task time tracking
            let timerHandler = ElapsedEventHandler(fun _ _ ->
                match runningTaskIndex.Current with
                | Some index ->
                    tasks.Set(
                        tasks.Current
                        |> List.mapi (fun i (name, elapsedTime) ->
                            if i = index then (name, elapsedTime + TimeSpan.FromSeconds(1.0))
                            else (name, elapsedTime)
                        )
                    )
                | None ->
                    match timerState.Current with
                    | Some t -> t.Enabled <- false
                    | None -> ()
            )

            // Timer handler for auto-saving tasks
            let autoSaveHandler = ElapsedEventHandler(fun _ _ ->
                Persistence.saveTasksToFile tasks.Current
            )

            autoSaveTimer.Elapsed.AddHandler(autoSaveHandler)
            autoSaveTimer.Start()

            // Create Timer once and store it in state
            ctx.useEffect(
                (fun () ->
                    let timer = new Timer(1000.0)
                    timer.AutoReset <- true
                    timer.Elapsed.AddHandler(timerHandler)
                    timerState.Set(Some timer)
                    { new IDisposable with
                        member _.Dispose() =
                            timer.Stop()
                            timer.Elapsed.RemoveHandler(timerHandler)
                            timer.Dispose()
                            autoSaveTimer.Stop()
                            autoSaveTimer.Elapsed.RemoveHandler(autoSaveHandler)
                            Persistence.saveTasksToFile tasks.Current
                    }
                ),
                [ EffectTrigger.AfterInit ]
            )

            let removeTask index =
                tasks.Set(tasks.Current |> List.indexed |> List.filter (fun (i, _) -> i <> index) |> List.map snd)
                match runningTaskIndex.Current with
                | Some i when i = index ->
                    runningTaskIndex.Set(None)
                    match timerState.Current with
                    | Some t -> t.Enabled <- false
                    | None -> ()
                | _ -> ()

            let addTask () =
                tasks.Set(tasks.Current @ [("New task", TimeSpan.Zero)])

            let resetTimers () =
                tasks.Set(tasks.Current |> List.map (fun (n, _) -> (n, TimeSpan.Zero)))

            let togglePlayPause index =
                match runningTaskIndex.Current with
                | Some i when i = index ->
                    runningTaskIndex.Set(None)
                    timerState.Current |> Option.iter (fun t -> t.Enabled <- false)
                | _ ->
                    runningTaskIndex.Set(Some index)
                    timerState.Current |> Option.iter (fun t -> t.Stop(); t.Start())

            // Calculate total elapsed time
            let totalElapsedTime =
                tasks.Current
                |> List.fold (fun acc (_, elapsedTime) -> acc + elapsedTime) TimeSpan.Zero

            DockPanel.create [
                DockPanel.children [
                    ScrollViewer.create [
                        ScrollViewer.verticalScrollBarVisibility ScrollBarVisibility.Auto
                        ScrollViewer.content (
                            StackPanel.create [
                                StackPanel.margin (Thickness(10.0))
                                StackPanel.children (
                                    (tasks.Current |> List.indexed |> List.map (fun (index, (task, elapsedTime)) ->
                                        Grid.create [
                                            Grid.columnDefinitions "50, *, 50, 100"
                                            Grid.horizontalAlignment HorizontalAlignment.Stretch
                                            Grid.children [
                                                Button.create [
                                                    Button.content "🚮"
                                                    Button.margin (Thickness(0.0, 0.0, 10.0, 0.0))
                                                    Button.onClick (fun _ -> removeTask index)
                                                    Grid.column 0
                                                ]
                                                TextBox.create [
                                                    TextBox.text task
                                                    TextBox.fontSize 18.0
                                                    TextBox.margin (Thickness(0.0, 5.0))
                                                    TextBox.horizontalAlignment HorizontalAlignment.Stretch
                                                    TextBox.onTextChanged (fun newText ->
                                                        tasks.Set(
                                                            tasks.Current |> List.mapi (fun i (n, t) ->
                                                                if i = index then (newText, t) else (n, t)
                                                            )
                                                        )
                                                    )
                                                    TextBox.onGotFocus (fun args ->
                                                        let textBox = args.Source :?> TextBox
                                                        if textBox.Text = "New task" then textBox.Clear()
                                                    )
                                                    Grid.column 1
                                                ]
                                                Button.create [
                                                    Button.content (if runningTaskIndex.Current = Some index then "⏸️" else "⏯️")
                                                    Button.margin (Thickness(5.0, 0.0, 0.0, 0.0))
                                                    Button.horizontalAlignment HorizontalAlignment.Right
                                                    Button.onClick (fun _ -> togglePlayPause index)
                                                    Grid.column 2
                                                ]
                                                TextBlock.create [
                                                    TextBlock.text $"%02d{elapsedTime.Hours}:%02d{elapsedTime.Minutes}:%02d{elapsedTime.Seconds}"
                                                    TextBlock.margin (Thickness(10.0, 0.0, 10.0, 0.0))
                                                    TextBlock.fontSize 18.0
                                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                                    TextBlock.horizontalAlignment HorizontalAlignment.Right
                                                    Grid.column 3
                                                ]
                                            ]
                                        ]
                                    )) @ [
                                        Grid.create [
                                            Grid.columnDefinitions "auto, *"
                                            Grid.children [
                                                StackPanel.create [
                                                    StackPanel.orientation Orientation.Horizontal
                                                    StackPanel.children [
                                                        Button.create [
                                                            Button.content "🆕"
                                                            Button.margin (Thickness(0.0, 10.0, 10.0, 0.0))
                                                            Button.horizontalAlignment HorizontalAlignment.Left
                                                            Button.onClick (fun _ -> addTask ())
                                                        ]
                                                        Button.create [
                                                            Button.content "🔄"
                                                            Button.margin (Thickness(0.0, 10.0, 10.0, 0.0))
                                                            Button.horizontalAlignment HorizontalAlignment.Left
                                                            Button.onClick (fun _ -> resetTimers ())
                                                        ]
                                                        TextBlock.create [
                                                            TextBlock.text $"Total time: %02d{totalElapsedTime.Hours}:%02d{totalElapsedTime.Minutes}:%02d{totalElapsedTime.Seconds}"
                                                            TextBlock.fontSize 18.0
                                                            TextBlock.verticalAlignment VerticalAlignment.Center
                                                            TextBlock.horizontalAlignment HorizontalAlignment.Left
                                                        ]
                                                    ]
                                                    Grid.column 0
                                                    Grid.columnSpan 2
                                                ]
                                            ]
                                        ]
                                    ]
                                )
                            ]
                        )
                    ]
                ]
            ]
        )