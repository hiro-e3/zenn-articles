open System 
open System.Windows 
open System.Windows.Markup
open System.Reflection

[<STAThread>] 
[<EntryPoint>]
do Assembly.GetExecutingAssembly().GetManifestResourceNames() 
    |> Array.find(fun x -> x.Contains("MainWindow.xaml"))
    |> Assembly.GetExecutingAssembly().GetManifestResourceStream
    |> XamlReader.Load :?> Window
    |> Application().Run |> ignore