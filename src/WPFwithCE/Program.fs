namespace System.Windows.Extensions 
open System
open System.Windows.Data
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Dynamic
open System.Collections.Generic 
open System.ComponentModel

type FrameworkElementBuilder() =
    [<CustomOperation("binding")>]
    member __.SetBinding(element : 'T when 'T :> FrameworkElement, propertyName : string, bindingName : string, ?updateSource : UpdateSourceTrigger) =
        let binding = new Binding(bindingName)
        let dp = typeof<'T>.GetField($"{propertyName}Property").GetValue(element) :?> DependencyProperty
        match updateSource with
        | Some source ->
            binding.UpdateSourceTrigger <- source
        | None ->
            binding.UpdateSourceTrigger <- UpdateSourceTrigger.Default
        element.SetBinding(dp, binding) |> ignore
        element

    [<CustomOperation("width")>]
    member  __.SetWidth(element : 'T when 'T :> FrameworkElement, width : int) =
        element.Width <- width |> float
        element
    [<CustomOperation("width")>]
    member  __.SetWidth(element : 'T when 'T :> FrameworkElement, width : float) =
        element.Width <- width
        element

    [<CustomOperation("height")>]
    member  __.SetHeight(element : 'T when 'T :> FrameworkElement, height) =
        element.Height <- height |> float
        element

    [<CustomOperation("context")>]
    member __.Context (element : 'T when 'T :> FrameworkElement, context : obj) =
        element.DataContext <- context
        element

    [<CustomOperation("vertical")>]
    member __.SetVerticalAlign(element : 'T when 'T :> FrameworkElement, alignment : VerticalAlignment) =
        element.VerticalAlignment <- alignment
        element

    [<CustomOperation("horizontal")>]
    member __.SetHorizontalAlign(element : 'T when 'T :> FrameworkElement, alignment : HorizontalAlignment) =
        element.HorizontalAlignment <- alignment
        element

type ControlBuilder() =
    inherit FrameworkElementBuilder()

type ContentControlBuilder() =
    inherit ControlBuilder()

    [<CustomOperation("content")>]
    member __.Content<'T when 'T :> ContentControl> (cc : 'T, contents : obj) =
        cc.Content <- contents
        cc

type WindowBuilder() =
    inherit ContentControlBuilder()
    member __.Yield(_) =
        Window()

    [<CustomOperation("title")>]
    member __.Title(window : Window, ?title) =
        match title with
        | Some title -> window.Title <- title
        | None -> window.Title <- ""
        window

type PanelBuilder() =
    inherit FrameworkElementBuilder()
    [<CustomOperation("children")>]
    member __.Children<'T when 'T :> Panel>(panel : 'T, [<ParamArray>]contents : UIElement array) =
        for content in contents do 
            panel.Children.Add(content) |> ignore
        panel
    
type StackPanelBuilder() =
    inherit PanelBuilder()
    member __.Yield(_) =
        StackPanel()

type GridBuilder() =
    inherit PanelBuilder()
    let mutable currentRow = 0
    let mutable currentColumn = 0
    let mutable createColumn = true
    
    member __.Yield(_) =
        currentRow <- 0
        currentColumn <- 0
        createColumn <- true
        Grid()
    
    [<CustomOperation("tr")>]
    member __.AddRow(grid : Grid) =
        grid.RowDefinitions.Add(RowDefinition())
        currentRow <- currentRow + 1
        if createColumn && currentColumn > 0 then
            createColumn <- false
        currentColumn <- 0
        grid
    
    [<CustomOperation("td")>]
    member __.AddColumn(grid : Grid, element : UIElement, ?rowspan, ?colspan) =
        if createColumn then 
            grid.ColumnDefinitions.Add(ColumnDefinition())
        currentColumn <- currentColumn + 1
        Grid.SetRow(element, currentRow - 1)
        Grid.SetColumn(element, currentColumn - 1)
        printfn "%d %d" currentRow currentColumn
        match rowspan with
        | Some r ->
            for _= currentRow to r do 
                grid.RowDefinitions.Add(RowDefinition())
            Grid.SetRowSpan(element, r)
        | None -> ()
        match colspan with
        | Some c ->
            for _= currentColumn to c do 
                grid.ColumnDefinitions.Add(ColumnDefinition())
            Grid.SetColumnSpan(element, c)
        | None -> ()
        grid.Children.Add(element) |> ignore
        grid    

type ButtonBaseBuilder() =
    inherit ContentControlBuilder()

type ButtonBuilder() =
    inherit ButtonBaseBuilder()

    member __.Yield(_) = 
        Button()

    [<CustomOperation("command")>]
    member __.Command(button : Button, propertyName : string) =
        button.SetBinding(Button.CommandProperty, propertyName) |> ignore
        button   

type TextBlockBuilder() =
    inherit FrameworkElementBuilder()
    member __.Yield(_) =
        TextBlock()

    [<CustomOperation("text")>]
    member __.SetText(textblock : TextBlock, text) =
        textblock.Text <- text
        textblock

type TextBoxBuilder () =
    inherit FrameworkElementBuilder()
    member __.Yield(_) =
        TextBox()

    [<CustomOperation("text")>]
    member __.SetText(textbox : TextBox , text) =
        textbox.Text <- text
        textbox

type ViewModelBuilder() =
    member __.Yield(_) =
        ViewModel()

    [<CustomOperation("command")>]
    member __.CreateCommand(vm : ViewModel, cmdName : string, cmd : ICommand) =
        vm.propertyDict.Add(cmdName,cmd)
        vm

    [<CustomOperation("property")>]
    member __.SetProperty(vm : ViewModel, propertyName, property) =
        vm.propertyDict.Add(propertyName, property)
        vm

and ViewModel() as vm = 
    inherit DynamicObject()
    let propertyChanged = Event<PropertyChangedEventHandler,PropertyChangedEventArgs>()
    member val propertyDict : Dictionary<string,obj> = new Dictionary<string, obj>()

    override __.TryGetMember(binder, result : byref<obj>) =
        if vm.propertyDict.ContainsKey(binder.Name) then 
            result <- vm.propertyDict.[binder.Name]
            true
        else
            false
            
    override __.TrySetMember(binder, value) =
        if value <> null then
            if vm.propertyDict.ContainsKey binder.Name then
                vm.propertyDict.[binder.Name] <- value
                propertyChanged.Trigger(__, PropertyChangedEventArgs(binder.Name))
            else
                vm.propertyDict.Add(binder.Name, value)
            true
        else 
            false

    [<CLIEvent>]
    member __.PropertyChanged = propertyChanged.Publish
    interface INotifyPropertyChanged with
        member __.add_PropertyChanged(handler) = __.PropertyChanged.AddHandler(handler)
        member __.remove_PropertyChanged(handler) = __.PropertyChanged.RemoveHandler(handler)

module Builder =
    let window = new WindowBuilder()
    let stackpanel = new StackPanelBuilder()
    let button = new ButtonBuilder()
    let vm = new ViewModelBuilder()
    let txtblock = TextBlockBuilder()
    let txtbox = TextBoxBuilder()
    let grid = GridBuilder()

module Main =
    open Builder
    open Elmish
    open Elmish.WPF
    open type UpdateSourceTrigger

    type Model = 
        {Text : string}

    type Msg =
        | InputText of string
        | OnClick

    let update msg m =
        match msg with
        | InputText str -> { m with Text = str}
        | OnClick _ -> 
            MessageBox.Show($"%A{m}","Current Model") |> ignore
            m

    let init () = {Text = ""}

    let bindings () : Binding<Model, Msg> list = [
        "InputText" |> Binding.twoWay(
            (fun m -> m.Text),InputText
        )
        "OnClick" |> Binding.cmdIf(
            OnClick, fun m -> String.IsNullOrEmpty(m.Text) |> not
        )
    ]
    let viewmodel = ViewModel.designInstance (init ()) (bindings ())
    
    let mainWindow = 
        window {
            title "CEs is very useful !" ; width 480.5 ; height 360
            content (
                stackpanel {
                    vertical VerticalAlignment.Center
                    children
                        (grid { 
                              tr
                              td (button { content "Row 0 Column 0" })
                              td (button { content "Row 0 Column 1" })
                              tr
                              td (button { content "Row 1 Column 0" })
                              td (button { content "Row 1 Column 1" })
                              tr
                              td (button { content "Row 2 Column 0" })
                              td (button { content "Row 2 Column 1" ; command "OnClick"})
                            })
                        (txtblock { binding "Text" "InputText"})
                        (txtbox { binding "Text" "InputText" PropertyChanged }) 
                        (button { content "Click!" ; command "OnClick"})
                }
            )
            context viewmodel
        }

    [<EntryPoint>]
    [<STAThread>]
    do Program.mkSimpleWpf init update bindings
    |> Program.withConsoleTrace
    |> Program.runWindowWithConfig { ElmConfig.Default with LogConsole = true; Measure = true } mainWindow
    |> ignore