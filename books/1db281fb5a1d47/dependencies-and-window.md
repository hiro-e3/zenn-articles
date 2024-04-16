---
title: "依存関係とウィンドウ"
---

## 退屈なのはわかってる
これを読んでいる方の中には、Rustでウィンドウを開くことにとても慣れていて、お気に入りのウィンドウライブラリをお持ちの方もいらっしゃると思いますが、このガイドは誰でも使えるように作られているので、カバーする必要がある内容になっています。幸いなことに、自分が何をやっているのか分かっている人はこれを読む必要はありません。ひとつだけ知っておくべきことは、どんなウィンドウソリューションであっても [raw-window-handle](https://github.com/rust-windowing/raw-window-handle) crateをサポートする必要があるということです。

## 何のクレートを使う？
初心者のために、非常にシンプルなものにするつもりです。順次追加していきますが、以下に関連する`Cargo.toml`の例をリストアップしておきます。
```toml:Cargo.toml
[dependencies]
winit = "0.28"
env_logger = "0.10"
log = "0.4"
wgpu = "0.18"
```

## Rust の新しいResolverを使用する
バージョン0.10では、wgpuは[cargoの最新機能のresolver](https://doc.rust-lang.org/cargo/reference/resolver.html#feature-resolver-version-2)を必要とします。これは2021年版（Rustバージョン1.56.0以降で開始した新しいプロジェクト）でデフォルトとなっています。しかし、まだ2018年版を使用している場合、単一クレートでの作業時は`Cargo.toml`の`[package]`セクションに、ワークスペースではルートの`Cargo.toml`の`[workspace]`セクションに`resolver = "2 "`を含める必要があります。

## [env_logger](https://docs.rs/env_logger/latest/env_logger/)
`env_logger::init();` でロギングを有効にすることは非常に重要です。wgpuが何らかのエラーにぶつかったとき、[log crate](https://docs.rs/log/latest/log/)を通して本当のエラーを記録する一方で、一般的なメッセージでパニックを起こします。これは `env_logger::init() `を含めない場合、wgpu は静かに失敗し、非常に混乱することを意味します!（下記のコードで行います）

## 新規プロジェクトの作成
`cargo new project_name`を実行します。project_nameはプロジェクトの名前です。
（以下の例では\`tutorial1_window\`を使用しています）

## コード
まだ大したことはしていないので、コードを全文掲載します。これを`lib.rs`かそれに準ずるものに貼り付けるだけです。
```rust:lib.rs
use winit::{
    event::*,
    event_loop::{ControlFlow, EventLoop},
    window::WindowBuilder,
};

pub fn run() {
    env_logger::init();
    let event_loop = EventLoop::new();
    let window = WindowBuilder::new().build(&event_loop).unwrap();

    event_loop.run(move |event, _, control_flow| match event {
        Event::WindowEvent {
            ref event,
            window_id,
        } if window_id == window.id() => match event {
            WindowEvent::CloseRequested
            | WindowEvent::KeyboardInput {
                input:
                    KeyboardInput {
                        state: ElementState::Pressed,
                        virtual_keycode: Some(VirtualKeyCode::Escape),
                        ..
                    },
                ..
            } => *control_flow = ControlFlow::Exit,
            _ => {}
        },
        _ => {}
    });
}
```

このコードはウィンドウを作成し、ユーザーが閉じるかエスケープを押すまで開いておくだけです。次に、コードを実行するための`main.rs`が必要です。これはとてもシンプルで、単に`run()`をインポートして、それを実行するだけです！

```rust:main.rs
use tutorial1_window::run;

fn main() {
    run();
}
```

（\`tutorial1_window\`は先にCargoで作成したプロジェクト名です）
デスクトップをサポートするだけならこれだけで十分です。次のチュートリアルではwgpuの使用を開始します！

## Webのサポートを追加する

WebGPU に関するこのチュートリアルを読んでも、Web 上での使用についてまったく話さなかったら、このチュートリアルは完了したとは言えません。 幸いなことに、設定さえしてしまえば、ブラウザーで wgpu アプリケーションを実行することはそれほど難しくありません。

`Cargo.toml`に必要な変更を加えるところから始めましょう：

```toml:Cargo.toml
[lib]
crate-type = ["cdylib", "rlib"]
```

これらの行は、クレートがネイティブ Rust スタティックライブラリ (rlib) と C/C++ 互換ライブラリ (cdylib) をビルドできるようにすることを Cargo に伝えます。 デスクトップ環境で wgpu を実行したい場合は、rlib が必要です。 ブラウザーが実行する Web Assemblyを作成するには cdylib が必要です。

:::message
### Web Assembly
Web Assembly（WASM） は、ほとんどの最新ブラウザでサポートされているバイナリ形式で、Rust などの低レベル言語を Web ページ上で実行できるようにします。 これにより、アプリケーションの大部分を Rust で記述し、数行の Javascript を使用して Web ブラウザで実行できるようになります。
:::

ここで必要なのは、WASM での実行に固有の依存関係をいくつか追加することだけです。

```toml:Cargo.toml
[dependencies]
cfg-if = "1"
# the other regular dependencies...

[target.'cfg(target_arch = "wasm32")'.dependencies]
console_error_panic_hook = "0.1.6"
console_log = "1.0"
wgpu = { version = "0.18", features = ["webgl"]}
wasm-bindgen = "0.2"
wasm-bindgen-futures = "0.4.30"
web-sys = { version = "0.3", features = [
    "Document",
    "Window",
    "Element",
]}
```

[`cfg-if`](https://docs.rs/cfg-if)クレートはプラットフォーム依存のコードをより扱いやすくするマクロを追加します。
`[target.'cfg(target_arch = "wasm32")'.dependencies]`の行はCargoへ`wasm32`アーキテクチャをターゲットにするときだけ、これらの依存関係を含めることを伝えます。次のいくつかの依存関係により、JavaScriptとのインターフェイスがはるかに簡単になります。

+ [console_error_panic_hook](https://docs.rs/console_error_panic_hook) `panic!`マクロがエラーをJavaScriptコンソールへ送るよう設定します。このクレートがない場合、パニックに遭遇した時に原因がわからなくなります。
+ [console_log](https://docs.rs/console_log) 全てのログをJavaScriptコンソールへ送る[log](https://docs.rs/log) APIの実装です。特定のログレベルのログのみを送信するように設定することもできます。これはデバッグにも最適です。
+ 現在のほとんどのブラウザで動作させたい場合は、wgpuのWebGL機能を有効にする必要があります。WebGPU APIを直接使用するためのサポートは準備中ですが、それはFirefox NightlyやChrome Canaryのようなブラウザの実験的なバージョンでのみ可能です。
これらのブラウザでこのコードをテストするのは大歓迎ですが（wgpuの開発者もそうしてくれるとありがたいです）、簡単にするために、WebGPU APIがより安定した状態になるまで、私はWebGL機能を使うことにこだわるつもりです。
もっと詳しく知りたい場合は、[wgpuのリポジトリ](https://github.com/gfx-rs/wgpu/wiki/Running-on-the-Web-with-WebGPU-and-WebGL)にあるWeb用にコンパイルするためのガイドをチェックしてください。
+ [wasm-bindgen](https://docs.rs/wasm-bindgen)はリストの中で最も重要な依存関係です。クレートの使用方法をブラウザに伝えるボイラープレートコードを生成する役割を果たします。また、JavaScript で使用できるメソッドを Rust で公開したり、その逆も可能になります。
wasm-bindgen の詳細には触れませんので、入門書 (または単なる復習) が必要な場合は、[The \`wasm-bindgen\` Guide](https://rustwasm.github.io/wasm-bindgen/)をチェックしてください。

## 追加のコード
はじめに、`wasm-bindgen`を`lib.rs`へインポートする必要があります。

```rust:lib.rs
#[cfg(target_arch="wasm32")]
use wasm_bindgen::prelude::*;

```

次に、WASMが読み込まれた時に`run()`関数が実行されるようwasm-bindgenへ伝える必要があります。

```rust:lib.rs
#[cfg_attr(target_arch="wasm32", wasm_bindgen(start))]
pub fn run() {
    // 今のところ上記と同じ...
}
```

その次に、WASMかどうかに基づいて、使用するロガーを切り替える必要があります。 `run()` 関数の先頭に次の行を追加し、`env_logger::init()` 行を置き換えます。

```rust:lib.rs
cfg_if::cfg_if! {
    if #[cfg(target_arch = "wasm32")] {
        std::panic::set_hook(Box::new(console_error_panic_hook::hook));
        console_log::init_with_level(log::Level::Warn).expect("Couldn't initialize logger");
    } else {
        env_logger::init();
    }
}
```

Webへのビルド時は`console_log`と`console_error_panic_hook`をセットアップし、通常のビルド時は`env_logger`を初期化します。これは現時点では`env_logger`はWeb Assemblyをサポートしていない為重要です。

次に、イベントループとウィンドウを作成したら、アプリケーションをホストするHTMLドキュメントにcanvasを追加する必要があります。

```rust:lib.rs
#[cfg(target_arch = "wasm32")]
{
    // Winit prevents sizing with CSS, so we have to set
    // the size manually when on web.
    use winit::dpi::PhysicalSize;
    window.set_inner_size(PhysicalSize::new(450, 400));
    
    use winit::platform::web::WindowExtWebSys;
    web_sys::window()
        .and_then(|win| win.document())
        .and_then(|doc| {
            let dst = doc.get_element_by_id("wasm-example")?;
            let canvas = web_sys::Element::from(window.canvas());
            dst.append_child(&canvas).ok()?;
            Some(())
        })
        .expect("Couldn't append canvas to document body.");
}
```

:::message
`"wasm-example"` idはこのチュートリアル専用のものです。あなたのHTMLで使っているidに置き換えてください。あるいは、wgpuのリポジトリにあるように、canvasを直接`<body>`に追加することもできます。この部分は最終的にはあなた次第です。
:::

現時点で必要な Web 固有のコードはこれですべてです。 次に行う必要があるのは、Web Assembly自体をビルドすることです。

## wasm-pack

これで、wasm-bindgen だけで wgpu アプリケーションを構築できるようになりましたが、その際にいくつかの問題が発生しました。 まず、wasm-bindgen をコンピューターにインストールし、依存関係として含める必要があります。 依存関係としてインストールするバージョンは、インストールしたバージョンと正確に一致する**必要**があります。 そうしないと、ビルドが失敗します。

この欠点を回避し、これを読んでいる皆さんの作業を楽にするために、[wasm-pack](https://rustwasm.github.io/docs/wasm-pack/) をミックスに追加することにしました。wasm-packは、正しいバージョンのwasm-bindgenのインストールを処理し、ブラウザー、NodeJS、webpack などのバンドラーなど、さまざまな種類の Web ターゲットのビルドもサポートします。

wasm-packを使用するにはまず[インストールする](https://rustwasm.github.io/wasm-pack/installer/)必要があります。

インストールが完了したら、wasm-packを使用してクレートを構築できます。 プロジェクトにクレートが1つしかない場合は、`wasm-pack build`を使用するだけで済みます。 ワークスペースを使用している場合は、どのクレートを構築するかを指定する必要があります。 クレートが`game`というディレクトリの時、次のように使用します。

```sh
wasm-pack build game
```

wasm-packのビルドが終わると、crateと同じディレクトリに`pkg`ディレクトリができます。これにはWASMのコードを実行するのに必要なJavaScriptのコードがすべて入っています。次に、JavaScriptでWASMモジュールをインポートします：

```js
const init = await import('./pkg/game.js');
init().then(() => console.log("WASM Loaded"));
```

このサイトでは [Vuepress](https://vuepress.vuejs.org/)を使用しているため、WASMをVueコンポーネントにロードします。WASMをどのように扱うかは、何をしたいかによって異なります。 私がどのように物事を行っているかを確認したい場合は、[これ](https://github.com/sotrh/learn-wgpu/blob/master/docs/.vuepress/components/WasmExample.vue)を見てください。

:::message
WASMモジュールをプレーンHTML Webサイトで使用する場合は、Webをターゲットにするようにwasm-packに指示する必要があります。

```sh
wasm-pack build --target web
```

その後、WASMコードをES6モジュールで実行する必要があります：
```html
<!DOCTYPE html>
<html lang="ja">

<head>
    <meta charset="UTF-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Learn WGPU</title>
    <style>
        canvas {
            background-color: black;
        }
    </style>
</head>

<body id="wasm-example">
  <script type="module">
      import init from "./pkg/pong.js";
      init().then(() => {
          console.log("WASM Loaded");
      });
  </script>
</body>

</html>
```