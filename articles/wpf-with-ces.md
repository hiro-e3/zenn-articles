---
title: "F#のコンピュテーション式を使ってコード上でUIをXAMLっぽく書く"
emoji: "🛠"
type: "tech" # tech: 技術記事 / idea: アイデア
topics: ["FSharp", "WPF", "DotNet"]
published: true
---

[.NET 5.0](https://devblogs.microsoft.com/dotnet/announcing-net-5-0/) のリリースと共に[F#5.0](https://devblogs.microsoft.com/dotnet/announcing-f-5/)が始まりました。

現状、まだプレビュー機能ですが[コンピュテーション式](https://docs.microsoft.com/ja-jp/dotnet/fsharp/language-reference/computation-expressions)のカスタム演算(Custom Operation)のオーバーロードが可能になりました。F#5.0の新機能を試すのと、コンピュテーション式の学習も兼ねてWPFのViewにあたる部分をXAMLではなくコード上で書くようなものをやってみたいと思います。

``` fsharp : Main module
module Main =
    open Builder // 各ビルダークラスのインスタンスを定義
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
```

以上は定義部分を除いたソースコードです。`stackpanel { children ... }`の部分で今回のプレビュー機能であるカスタム演算のオーバーロードを使っています。また`width`は`int`、`float`両方の型を扱うことができます。

![実行結果](https://github.com/msanou/zenn-articles/blob/master/image/fs50withwpf.gif?raw=true)
実行結果はこのような感じになります。CommandやBindingに使用してるオブジェクトは[Elmish.WPF](https://github.com/elmish/Elmish.WPF)のライブラリを利用して作成していますが、今回はこちらについては触れません。

まずはコンピュテーション式とそのカスタム演算について簡単に例示します。

# コンピュテーション式(Computation Expressions : CEs)
コンピュテーション式は`Bind`や`Return`など特有のメソッドを持つクラスのインスタンスを使用して、モナドやDSLなどの表現ができるF#の機能の一つです。慣例的に`...Builder`という名前が付けられることが多い印象(Builderパターンが由来？)ですが、命名規則から外れていてもコンピュテーション式の構文は使うことは可能です。
その慣例に従って、コンピュテーション式に用いるクラスをBuilderクラスと呼称しています。

``` fsharp
type SampleBuilder() =   
    member __.Bind(m,f) =
        printfn "Call Bind method %A" m 
        f m      

    member __.Return(x) = 
        printfn "Call Return method %A" x
        x

let sample = SampleBuilder()

let a = sample {
    let! a = 100
    let! b = 99
    return a + b
}

printfn "%A" a
```

```
Call Bind method 100
Call Bind method 99
Call Return method 199
199
```

`let`を利用してBuilderクラスのインスタンスを束縛した識別子の後を波括弧で括ると、その内部で`let!`や`return`のキーワードを利用することが可能です。コンピュテーション式の中では記述した内容に応じて、Builderクラス内の対応するメソッドを呼び出します。`let!`は`Bind`を呼び出し、`return`は`Return`を呼び出します。

``` fsharp
let a = sample.Bind(100, fun a ->
        sample.Bind(99, fun b ->
        sample.Return(a + b)
        )
    )
```

前述のコンピュテーション式の内部は上記のように展開されるようです。

各キーワードに対してどの名称のメソッドが呼び出されるのかについての詳細は[MS Docs](https://docs.microsoft.com/ja-jp/dotnet/fsharp/language-reference/computation-expressions#creating-a-new-type-of-computation-expression)で確認していただければと思います。

## カスタム演算(Custom Operation)の実装
``` fsharp
type Expression =
    | Normal
    | Laugh
    | Angry
    | Smile

type Script =
    { Speaker : string; Text : string ; Expression : Expression}

type ScriptBuilder() =
    member __.Yield(_) =
        { Speaker="" ; Text="" ; Expression=Normal}

    [<CustomOperation("speaker")>]
    member __.SetSpeaker(script, speaker) =
        {script with Speaker=speaker}

    [<CustomOperation("text")>]
    member __.SetText(script, text) =
        {script with Text=text}

    [<CustomOperation("expression")>]
    member __.SetExpression(script, expression) =
        {script with Expression=expression}

let script = ScriptBuilder()
let page1 = script {
    speaker "Nick"
    text "Hello, Aki."
    expression Smile
}

printfn "%A" page1
```

```
{ Speaker = "Nick"
  Text = "Hello, Aki."
  Expression = Smile }
```

上記はADVゲームのスクリプトを書くようなものを考えています。

`CustomOperationAttribute("name")`を適用したBuilderクラス内のメソッドを、コンピュテーション式内に限り`name`で呼び出すことができるようになります。LINQのクエリ式のF#における実装はこの機能を利用しています。MS Docsでは`For`も実装する必要があると書いてありますが、`Yield`のみで問題ありません。`script {...} `が実際に何をしているのか、順を追って書いていきたいと思います。

1. `Yield`メソッドが呼び出され、初期状態の`Script`のレコードが作成される
2. `SetSpeaker`メソッドの第一引数には`Yield`メソッドで作成されたレコードが渡され、新たに`Speaker`に`"Nick"`がセットされたレコードを返す
3. `SetText`メソッドの第一引数には`SetSpeaker`で作成されたレコードが渡され、`Text`に`"Hello, Aki."`がセットされたレコードを返す
4. `SetExpression`メソッドに第一引数は`SetText`で作成されたレコードが渡され、`Expression`に`Smile`がセットされた値をメソッドが返し、`page1`に束縛される

このように、カスタムキーワードを設定することである特定のオブジェクトを簡単に生成する構文を自作する事が出来ます。

# Builderクラスの実装
基本的には各プロパティに値を代入する行為をカスタムキーワードでラップしているだけなので、かいつまんで書きたいと思います。

## System.Windows.Window
``` fsharp 
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
    // Custom Operationのオーバーロード
    [<CustomOperation("width")>]
    member  __.SetWidth(element : 'T when 'T :> FrameworkElement, width : float) =
        element.Width <- width
        element

    [<CustomOperation("height")>]
    member  __.SetHeight(element : 'T when 'T :> FrameworkElement, height : int) =
        element.Height <- height |> float
        element
    // Custom Operationのオーバーロード
    [<CustomOperation("height")>]
    member  __.SetHeight(element : 'T when 'T :> FrameworkElement, height : float) =
        element.Height <- height
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
```

Builderクラスの継承関係は各コントロールの継承関係と対応させています。全てを網羅していませんが、基本的にプロパティに値をセットするのをメソッドでラップして、`unit`ではなく各コントロールを返すようにしている感じのものがほとんどです。

Bindingの設定に用いる`DependencyProperty`をフィールド名称から取得して設定しているのはあまりスマートじゃない感があるので他にいい方法があればいいのですが。基本的に`TextBox.TextProperty`というような長さなのでXAMLの`Text="{Binding ...}"`を同じ書き味でできれば一番ベストなんですけども。DP用のModuleを定義してその中に列挙するぐらい？

## Grid
``` fsharp
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
```

XAMLでGridの設定を書いている時にあまり嬉しくなかったのが、Row、Columnの個数の定義とグリッドに含めるコントロール、コントロールをグリッドのどの行・列に配置するか、といった設定がバラバラだったことでした。

例えば、2行2列のGridを用意してそこにコントロールを配置する場合

``` xml : XAML
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition />
        <RowDefinition />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition />
        <ColumnDefinition />
    </Grid.ColumnDefinitions>
    <TextBlock Grid.Row="0" Grid.Column="0" />
    <TextBox Grid.Row="0" Grid.Column="1" />
    <Button Grid.Row="1" Grid.Column="0" />
    <TextBlock Grid.Row="1" Grid.Column="1" />
</Grid>
```

このようになります。個人的にはHTMLのTableタグっぽく書けた方が嬉しいと思い、はじめに記した形にしましたが、コンピュテーション式の実装を変更することで、如何様にもオレオレ記法を用意することが可能です。

``` fsharp
grid { 
    tr
    td (txtblock { text "Row 0 Column 0" })
    td (txtbox { text "Row 0 Column 1" })
    tr
    td (button { content "Row 1 Column 0" })
    td (txtblock { content "Row 1 Column 1" })
}
```

幾分かシンプルに書けるようになったのではないでしょうか？
コンピュテーション式をネストする場合は括弧で括る必要があります。

# 終わりに
今回使用したソースコードの全文はこちらとなります。
https://github.com/msanou/zenn-articles/blob/master/src/WPFwithCE/Program.fs

既に標準ライブラリにあるコンピュテーション式としてはシーケンスの処理を行う`seq{...}`や、C#のTaskとは別の非同期処理を提供する`async{...}`、 SQLライクにコレクションの操作を行うLINQのクエリ式のような構文を使える`query {...}`などがあります。

表題としてはXAMLっぽくとしましたが、どちらかというとElmでのViewの定義っぽく書きたい気持ちがありました(Elmishですし)。コンピュテーション式を上手く使いこなせればかなり有用だとは思いますが、モナドについても全然知らないのでHaskellも少し触ってみた方がいいのかなと少し考えています(やるとは言っていない)。