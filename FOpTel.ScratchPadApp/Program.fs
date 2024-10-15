open System.Diagnostics
open OpenTelemetry
open OpenTelemetry.Trace

type TestExporter() =

    inherit BaseExporter<Activity>()

    override this.Export(batch) =
        use scope = SuppressInstrumentationScope.Begin()
        
        for activity in batch do
            
            printfn $"## ID: {activity.Id}"
            printfn $"## d: {activity.Events}"
            
            ()
        
        ExportResult.Success
  

  
    
let activitySource = new ActivitySource("FOpTel.Test")

let activitySource2 = new ActivitySource("FOpTel.Test.2")

let activity2 (name: string) (fn: unit -> Result<unit, unit>) =
    use activity = activitySource2.StartActivity()
    
    let tags = ActivityTagsCollection()
    tags.Add("type", "inner activity")
    
    activity.AddTag("type", "inner activity") |> ignore
    
    match fn () with
    | Ok _ -> activity.SetStatus ActivityStatusCode.Ok |> ignore
    | Error _ -> activity.SetStatus ActivityStatusCode.Error |> ignore
    

let activity (name: string) (fn: unit -> Result<unit, unit>) =
    use activity = activitySource.StartActivity()
    let tags = ActivityTagsCollection()
    tags.Add("a", 1)
    tags.Add("b", "Hello, World!")
    let e = ActivityEvent("Started", tags = tags)
    
    activity.AddEvent(e) |> ignore
    
    activity
        .SetTag("foo", 1)
        .SetTag("bar", "Hello, World!")
        .SetTag("baz", [| 1; 2; 3 |])
    |> ignore

    match fn () with
    | Ok _ -> activity.SetStatus ActivityStatusCode.Ok |> ignore
    | Error _ -> activity.SetStatus ActivityStatusCode.Error |> ignore


let traceProvider =
    Sdk
        .CreateTracerProviderBuilder()
        .AddSource("FOpTel.Test")
        .AddSource("FOpTel.Test.2")
        .AddConsoleExporter()
        .AddProcessor(new BatchActivityExportProcessor(new TestExporter()))
        .Build()

activity "test1" (fun _ ->
    activity2 "test3" (fun _ -> Ok ())
    Ok ())
//activity "test2" (fun _ -> Error ())


traceProvider.ForceFlush() |> ignore

Async.Sleep 5000 |> Async.RunSynchronously


traceProvider.Dispose()    
    
    
    
    
    

()


// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"
