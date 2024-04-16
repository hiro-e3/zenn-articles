---
title: "サーフェス"
---

## まずはいくつかの事務作業から: State

便宜上、すべてのフィールドを構造体にまとめ、その上にいくつかのメソッドを作成することにします。

```rust:lib.rs
use winit::window::Window;

struct State {
    surface: wgpu::Surface,
    device: wgpu::Device,
    queue: wgpu::Queue,
    config: wgpu::SurfaceConfiguration,
    size: winit::dpi::PhysicalSize<u32>,
    // surfaceにはwindowのリソースに対する安全でない参照が含まれるため
    // windowはsurfaceの後に削除されるようsurfaceの後に宣言されなければならない
    window: Window,
}

impl State {
    // 一部のwgpuの型の作成には非同期コードが必要
    async fn new(window: Window) -> Self {
        todo!()
    }

    pub fn window(&self) -> &Window {
        &self.window
    }

    fn resize(&mut self, new_size: winit::dpi::PhysicalSize<u32>) {
        todo!()
    }

    fn input(&mut self, event: &WindowEvent) -> bool {
        todo!()
    }

    fn update(&mut self) {
        todo!()
    }

    fn render(&mut self) -> Result<(), wgpu::SurfaceError> {
        todo!()
    }
}
```

ここでは`State`のフィールドについては説明しませんが、メソッドの背後にあるコードを説明する際により意味がわかるでしょう。

## State::new()

コードはとても単純ですが、これを少し分解してみましょう。

```rust:lib.rs
impl State {
    // ...
    async fn new(window: Window) -> Self {
        let size = window.inner_size();

        // インスタンスは GPU へのハンドルです
        // Backends::all => Vulkan + Metal + DX12 + Browser WebGPU
        let instance = wgpu::Instance::new(wgpu::InstanceDescriptor {
            backends: wgpu::Backends::all(),
            ..Default::default()
        });
        
        // # Safety
        //
        // surfaceはこれを作成したwindowと同様の長さ生存する必要がある
        // Stateはwindowを所有している為安全。
        let surface = unsafe { instance.create_surface(&window) }.unwrap();

        let adapter = instance.request_adapter(
            &wgpu::RequestAdapterOptions {
                power_preference: wgpu::PowerPreference::default(),
                compatible_surface: Some(&surface),
                force_fallback_adapter: false,
            },
        ).await.unwrap();
    }
```

### InstanceとAdapter
`Instance`はwgpu使用するとき初めに作成するものです。この主目的は`Adapter`と`Surface`を作成することです。

`Adapter`は実際のグラフィックカードへのハンドルです。これを使用して、グラフィックカードの名前や、アダプタが使用するバックエンドなどの情報を取得することができます。この`Adapter`は、後で `Device` と `Queue` を作成するために使用します。`RequestAdapterOptions` のフィールドについて説明します。

+ `power_preference`(電源の優先度)には2つのバリエーションがあります。`LowPower`と`HighPerformance`です。これは、`LowPower `を使用する場合、統合GPU（訳注:IGP、オンボードのグラフィックカードなど) のようなバッテリー寿命を優先するAdapterを選択することを意味します。`HighPerformance`は、専用グラフィックカードのように、より多くの電力を必要とする高性能なGPUのためのAdapterを選択します。WGPUは`HighPerformance` オプション用のAdapterがない場合、`LowPower`を優先します。
+ `compatible_surface`(互換性のある`Surface`)は与えられたSurfaceに提示できるAdapterを探すように wgpu に指示します。
+ `force_fallback_adapter`はwgpuに全てのハードウェアで動作するアダプタを選択するように強制します。これは通常、レンダリングバックエンドがGPUのようなハードウェアではなく、"ソフトウェア"システムを使用することを意味します。

:::message
`request_adapter`に渡したオプションは、すべてのデバイスで動作することを保証するものではありませんが、ほとんどのデバイスで動作するはずです。wgpuが必要なパーミッションを持つアダプタを見つけられない場合、`request_adapter`は`None`を返します。特定のバックエンドのためにすべてのアダプタを取得したい場合、`enumerate_adapters`を使うことができます。これはイテレータを提供し、アダプタのうちの1つがあなたのニーズに合うかどうかをチェックするためにループさせることができます。
```rust
let adapter = instance
    .enumerate_adapters(wgpu::Backends::all())
    .filter(|adapter| {
        // Check if this adapter supports our surface
        adapter.is_surface_supported(&surface)
    })
    .next()
    .unwrap()
```

注意点の1つは、`enumerate_adapters`はWASMでは使用できないため、`request_adapter`を使用する必要があることです。

もう1つの注意点は、`Adapter`が特定のバックエンドにロックされていることです。 Windowsを使用していてグラフィックスカードが2枚ある場合は、少なくとも4つのアダプタ (Vulkan2つと DirectX2つ) を使用できます。


検索を絞り込むために使用できる他のフィールドについては、[ドキュメントをチェックしてください](https://docs.rs/wgpu/0.18.0/wgpu/struct.Adapter.html)。
:::

### Surface
`Surface`とは、ウィンドウに描画する部分のことです。画面に直接描画するために必要です。私たちのウィンドウは`surface`を作るために [raw-window-handle](https://crates.io/crates/raw-window-handle)の `HasRawWindowHandle` trait を実装する必要があります。幸いなことに、winitの`Window`がその条件に合っています。また、`adapter`を要求するために必要です。

### DeviceとQueue
`Adapter`を使ってdeviceとqueueを作ってみましょう。
```rust 
    let (device, queue) = adapter.request_device(
            &wgpu::DeviceDescriptor {
                features: wgpu::Features::empty(),
                // WebGL doesn't support all of wgpu's features, so if
                // we're building for the web, we'll have to disable some.
                limits: if cfg!(target_arch = "wasm32") {
                    wgpu::Limits::downlevel_webgl2_defaults()
                } else {
                    wgpu::Limits::default()
                },
                label: None,
            },
            None, // Trace path
        ).await.unwrap();
```

`DeviceDescriptor`の`features`フィールドでは、どのような機能を追加するかを指定することができます。このシンプルな例では、余計な機能は使わないことにしました。

:::message
使用しているグラフィックカードにより、使用できる機能が制限されます。特定の機能を使用したい場合は、サポートするデバイスを制限したり、回避策を提供したりする必要がある場合があります。

デバイスでサポートされている機能のリストは、`adapter.features()`または`device.features()`を使用して取得できます。

機能の完全なリストは[ここ](https://docs.rs/wgpu/latest/wgpu/struct.Features.html)でご覧いただけます。
:::

`limits`フィールドは、作成できる特定のタイプのリソースの上限を記述します。このチュートリアルでは、ほとんどのデバイスをサポートできるように、デフォルトを使用します。制限の一覧は、[こちら](https://docs.rs/wgpu/0.18.0/wgpu/struct.Limits.html)で見ることができます。

```rust
        let surface_caps = surface.get_capabilities(&adapter);
        // このチュートリアルのシェーダーコードは、sRGB表面テクスチャを前提としています。 
        // 別のものを使用すると、すべての色が暗くなります。 
        // 非sRGBサーフェスをサポートしたい場合は、フレームに描画するときにそれを考慮する必要があります。
        let surface_format = surface_caps.formats.iter()
            .copied()
            .filter(|f| f.is_srgb())
            .next()
            .unwrap_or(surface_caps.formats[0]);
        let config = wgpu::SurfaceConfiguration {
            usage: wgpu::TextureUsages::RENDER_ATTACHMENT,
            format: surface_format,
            width: size.width,
            height: size.height,
            present_mode: surface_caps.present_modes[0],
            alpha_mode: surface_caps.alpha_modes[0],
            view_formats: vec![],
        };
        surface.configure(&device, &config);
```

ここでは、surfaceの設定を定義しています。これは、surfaceがその基礎となる`SurfaceTexture`を作成する方法を定義します。`SurfaceTexture`については、`render`関数のところで説明します。今のところ、`config`のフィールドについて説明します。

`usage` フィールドは、`SurfaceTextures`の使用方法について説明します。`RENDER_ATTACHMENT `は、テクスチャを使用して画面に書き込むことを指定します（`TextureUsages`については後で詳しく説明します）。

`format`は、`SurfaceTexture`がGPUにどのように保存されるかを定義します。ディスプレイによって好むフォーマットが異なります。`surface.get_preferred_format(&adapter)` を使って、使用しているディスプレイに応じて最適なフォーマットを割り出しています。

`width`と `height`は `SurfaceTexture` の幅と高さをピクセルで指定します。これは通常、ウィンドウの幅と高さであるべきです。

:::message alert
アプリがクラッシュする可能性があるので、`SurfaceTexture` の幅と高さが 0 でないことを確認してください。
:::

`present_mode`は`wgpu::PresentMode`enumを使用し、surfaceとディスプレイをどのように同期させるかを決定します。私たちが選んだオプションである`FIFO`は、ディスプレイのフレームレートで表示レートを制限します。これは本質的にVSync(訳注:垂直同期)です。これは、モバイルで最も最適なモードでもあります。他にもオプションがあり、[ドキュメント](https://docs.rs/wgpu/0.12.0/wgpu/enum.PresentMode.html)ですべて見ることができます。

これでsurfaceを適切に設定できたので、メソッドの最後にこれらの新しいフィールドを追加することができます。

:::message
ユーザーに`PresentMode`を選択させたい場合、[`SurfaceCapabilities::present_modes`](https://docs.rs/wgpu/latest/wgpu/struct.SurfaceCapabilities.html#structfield.present_modes)を使用してsurfaceがサポートする全ての`PresentMode`のリストを取得できます。

```rust
let modes = &surface_caps.present_modes;
```

いずれにせよ、`PresentMode::Fifo`は常にサポートされ、`PresentMode::AutoVsync`と`PresentMode::AutoNoVsync`はフォールバックサポートを備えているため、すべてのプラットフォームで動作します。
:::

`alpha_mode`は正直なところ、私には馴染みのないものです。 透明なウィンドウに関係があると思いますが、お気軽にプルリクエストを開いてください。 ここでは、`surface_caps`で指定されたリストの最初の `AlphaMode`を使用するだけです。

`view_formats`は、`TextureView`を作成するときに使用できる`TextureFormat`のリストです (これらについては、このチュートリアルの後半で簡単に説明し、テクスチャのチュートリアルでさらに詳しく説明します)。 執筆時点では、これは、サーフェスが sRGB カラー スペースの場合、リニアカラースペースを使用するテクスチャビューを作成できることを意味します。

サーフェスを適切に構成したので、メソッドの最後にこれらの新しいフィールドを追加できます。

```rust:lib.rs
    async fn new(window: Window) -> Self {
        // ...

        Self {
            window,
            surface,
            device,
            queue,
            config,
            size,
        }
    }
```

`State::new()`メソッドはasyncなので、`run()`関数もasyncにして待機可能にする必要があります。

`window`は`State`へムーブしたので、これを反映するために`event_loop`を更新する必要があります。

```rust
#[cfg_attr(target_arch = "wasm32", wasm_bindgen(start))]
pub async fn run() {
    // Window setup...

    let mut state = State::new(window).await;

    event_loop.run(move |event, _, control_flow| {
        match event {
            Event::WindowEvent {
                ref event,
                window_id,
            } if window_id == state.window().id() => match event {
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
        }
    });
}
```

`run()`もasyncになったことから、`main()`は何かしらの方法でFutureを待機する必要があります。[tokio](https://docs.rs/tokio)や[async-std](https://docs.rs/async-std)のようなクレートを使うことができますが、より軽量の[pollster](https://docs.rs/pollster)を使います。`Cargo.toml`へ以下を追加してください。

```toml:Cargo.toml
[dependencies]
# other deps...
pollster = "0.3"
```

そして、pollsterが提供する`block_on`関数を使用してFutureを待機します。

```rust:main.rs
fn main() {
    pollster::block_on(run());
}
```

:::message alert
WASMをサポートするつもりなら、非同期関数内で`block_on`を使わないでください。Futureはブラウザのエクゼキュータを使って実行する必要があります。独自のものを持ち込もうとすると、すぐに実行されないFutureに遭遇したときにコードがクラッシュしてしまいます。
:::

今WASMをビルドしようとすると、`wasm-bindgen`が`start`メソッドをasync関数として使用することをサポートしていないため失敗します。JavaScriptで手動で`run`を呼び出すように切り替えることもできますが、コードを変更する必要がないので、簡単にするために、[wasm-bindgen-futures](https://docs.rs/wasm-bindgen-futures)クレートをWASMの依存関係に追加します（訳注：Chapter2にて既に4.30が追加されている）。依存関係は次のようになります。



```toml:Cargo.toml
[dependencies]
cfg-if = "1"
winit = "0.28"
env_logger = "0.10"
log = "0.4"
wgpu = "0.18"
pollster = "0.3"

[target.'cfg(target_arch = "wasm32")'.dependencies]
console_error_panic_hook = "0.1.6"
console_log = "1.0"
wgpu = { version = "0.18", features = ["webgl"]}
wasm-bindgen = "0.2"
wasm-bindgen-futures = "0.4"
web-sys = { version = "0.3", features = [
    "Document",
    "Window",
    "Element",
]}
```

## resize()
アプリケーションでリサイズをサポートする場合、ウィンドウのサイズが変わるたびに`surface`を再設定する必要があります。それが、物理的な`size`と`surface`を構成するために使用する`config`を保存した理由です。これらすべてがあれば、`resize`メソッドは非常にシンプルになります。

```rust
// impl State
pub fn resize(&mut self, new_size: winit::dpi::PhysicalSize<u32>) {
    if new_size.width > 0 && new_size.height > 0 {
        self.size = new_size;
        self.config.width = new_size.width;
        self.config.height = new_size.height;
        self.surface.configure(&self.device, &self.config);
    }
}
```

ここは`surface`の初期化時と特に変わるところはないので、割愛します。
このメソッドをイベントループ内の`main()`で、以下のイベントに対して呼び出しています。

```rust:lib.rs
match event {
    // ...

    } if window_id == window.id() => if !state.input(event) {
        match event {
            // ...

            WindowEvent::Resized(physical_size) => {
                state.resize(*physical_size);
            }
            WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
                // new_inner_size は &&mut なので2度デリファレンスする必要がある。
                state.resize(**new_inner_size);
            }
            // ...
}
```

## input()
`input()` は、イベントが完全に処理されたかどうかを示す `bool` 値を返します。このメソッドが `true` を返した場合、メインループはイベントをそれ以上処理しません。

今は捕捉したいイベントがないので、`false` を返すことにします。

```rust
// impl State
fn input(&mut self, event: &WindowEvent) -> bool {
    false
}
```

イベントループの中でもう少し作業をする必要があります。`State`が`run()`よりも優先されるようにしたいのです。これを実行すると（そして以前の変更も）、ループは次のようになるはずです。

```rust
// main()
event_loop.run(move |event, _, control_flow| {
    match event {
        Event::WindowEvent {
            ref event,
            window_id,
        } if window_id == window.id() => if !state.input(event) { // UPDATED!
            match event {
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
                WindowEvent::Resized(physical_size) => {
                    state.resize(*physical_size);
                }
                WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
                    state.resize(**new_inner_size);
                }
                _ => {}
            }
        }
        _ => {}
    }
});
```

## update()
まだ更新するものがないので、このメソッドは空のままにしておきます。

```rust
fn update(&mut self) {
    // remove `todo!()`
}
```

ここには後でオブジェクトを移動させるためのコードを追加します。

## render()
ここで、マジックが起こります。まず、レンダリング先のフレームを取得する必要があります。

```rust
// impl State

fn render(&mut self) -> Result<(), wgpu::SurfaceError> {
    let output = self.surface.get_current_texture()?;
```

[`get_current_texture` ](https://docs.rs/wgpu/latest/wgpu/struct.Surface.html#method.get_current_texture)関数は、レンダリング先となる新しい `SurfaceTexture` を提供するために、`surface`を待ちます。これは後々のために`output`に保存しておきます。

```rust
 let view = output.texture.create_view(&wgpu::TextureViewDescriptor::default());
```

この行は、デフォルトの設定で`TextureView`を作成します。描画コードがテクスチャとどのように相互作用するかを制御したいので、これを行う必要があります。

また、GPUに送信する実際のコマンドを作成するために、`CommandEncoder`を作成する必要があります。最近のグラフィックスフレームワークでは、GPUに送信する前にコマンドをコマンド バッファに格納することを想定しているものがほとんどです。`encoder`はコマンドバッファを構築し、それを GPU に送信することができます。

```rust
let mut encoder = self.device.create_command_encoder(&wgpu::CommandEncoderDescriptor {
    label: Some("Render Encoder"),
});
```
‘
これで実際に画面をクリアすることができるようになりました（長かったですね）。`encoder`を使って`RenderPass`を作成する必要があります。`RenderPass`には、実際の描画に必要なすべてのメソッドが含まれています。`RenderPass`を作成するコードは少し入れ子になっているので、その断片について話す前にここにすべてコピーしておきます。

```rust
    {
        let _render_pass = encoder.begin_render_pass(&wgpu::RenderPassDescriptor {
            label: Some("Render Pass"),
            color_attachments: &[Some(wgpu::RenderPassColorAttachment {
                view: &view,
                resolve_target: None,
                ops: wgpu::Operations {
                    load: wgpu::LoadOp::Clear(wgpu::Color {
                        r: 0.1,
                        g: 0.2,
                        b: 0.3,
                        a: 1.0,
                    }),
                    store: wgpu::StoreOp::Store,
                },
            })],
            depth_stencil_attachment: None,
            occlusion_query_set: None,
            timestamp_writes: None,
        });
    }

    // submit will accept anything that implements IntoIter
    self.queue.submit(std::iter::once(encoder.finish()));
    output.present();

    Ok(())
}

```

まず最初に、`encoder.begin_render_pass(...)`の周りの余分なブロック（`{}`）について説明します。`begin_render_pass()`は`encoder`をmutable（別名`&mut self`）に借用します。この可変借用を解放するまで、`encoder.finish()`を呼び出すことはできません。このブロックは、コードがそのスコープを出たときに、その中の変数を削除するように rust に伝えます。こうして `encoder` の可変借用を解放し、それを `finish()` できるようにします。もし `{}` が嫌いなら、 `drop(render_pass) `を使っても同じ効果が得られます。

この`{}`と`let _render_pass =`の行を削除しても同じ結果が得られますが、次のチュートリアルで`_render_pass`にアクセスする必要があるので、このままにしておきましょう。

コードの最後の行は、wgpuにコマンドバッファを終了させ、GPUのレンダーキューに送信するよう指示します。

このメソッドを呼び出すために、イベントループを再び更新する必要があります。また、その前に`update()`も呼び出すことにします。

```rust
// main()
event_loop.run(move |event, _, control_flow| {
    match event {
        // ...
        Event::RedrawRequested(window_id) if window_id == state.window().id() => {
            state.update();
            match state.render() {
                Ok(_) => {}
                // もしsurfaceがロストしたら再設定する
                Err(wgpu::SurfaceError::Lost) => state.resize(state.size),
                // The system is out of memory, we should probably quit
                Err(wgpu::SurfaceError::OutOfMemory) => *control_flow = ControlFlow::Exit,
                // All other errors (Outdated, Timeout) should be resolved by the next frame
                Err(e) => eprintln!("{:?}", e),
            }
        }
        Event::MainEventsCleared => {
            // RedrawRequested will only trigger once unless we manually
            // request it.
            state.window().request_redraw();
        }
        // ...
    }
});
```

これだけあれば、このようなものができあがるはずです。

!["window"](/images/winit_window.png)

## 待てよ、RenderPassDescriptorはどうなっているんだ？
見ただけで何が起こっているのか分かる方もいらっしゃるかもしれませんが、ここで確認しておかなければ損です。もう一度、コードを見てみましょう。

```rust
&wgpu::RenderPassDescriptor {
    label: Some("Render Pass"),
    color_attachments: &[
        // ...
    ],
    depth_stencil_attachment: None,
}
```

`RenderPassDescriptor`は、`label`、`color_attachments`、`depth_stencil_attachment`の3つのフィールドのみを持っています。`color_attachments`は、どこに色を描画するかを記述します。画面へのレンダリングを確実に行うために、先ほど作成した`TextureView`を使用します。

`depth_stencil_attachment`は後で使いますが、今は`None`に設定しておきます。

```rust
Some(wgpu::RenderPassColorAttachment {
    view: &view,
    resolve_target: None,
    ops: wgpu::Operations {
        load: wgpu::LoadOp::Clear(wgpu::Color {
            r: 0.1,
            g: 0.2,
            b: 0.3,
            a: 1.0,
        }),
        store: wgpu::StoreOp::Store,
    },
})
```

`RenderPassColorAttachment`は`view`フィールドを持ち、どのテクスチャに色を保存するかをwgpuに通知します。この場合、`surface.get_current_texture()`を使用して作成された`view`を指定します。これは、このアタッチメントに描画した色がスクリーンに描画されることを意味します。

`resolve_target` は、解決された出力を受け取るテクスチャです。これは、マルチサンプリングが有効でない限り、`view`と同じになります。これは指定する必要がないので、`None`のままにしておきます。

`ops`フィールドは、`wpgu::Operations`を受け取ります。これはwgpuにスクリーン上の色（`view`によって指定される）をどうするか指示します。`load`フィールドは、前のフレームから保存された色を処理する方法をwgpuに伝えます。現在、私たちは青っぽい色で画面をクリアしています。`store`フィールドは、レンダリング結果を`TextureView`の後ろの`Texture`（この場合は`SurfaceTexture`）に保存したいかどうかをwgpuに伝えます。レンダリング結果を保存したいので、`StoreOp::Store`を使用します。


:::message
画面が完全にオブジェクトで覆われてしまう場合、画面をクリアしないことも珍しくありません。 ただし、シーンが画面全体をカバーしていない場合は、このような結果になる可能性があります。

![](/images/no-clear.png)
:::

## バリデーションエラー？
wgpuがあなたのマシン上でVulkanを使用している場合、Vulkan SDKの古いバージョンを実行していると、検証エラーに遭遇する可能性があります。古いバージョンでは誤検出する可能性があるため、少なくともバージョン1.2.182を使用する必要があります。エラーが続く場合は、wgpuのバグに遭遇している可能性があります。https://github.com/gfx-rs/wgpu でissueを投稿することができます

## 課題
`input()`メソッドを修正してマウスイベントをキャプチャし、それを使ってクリアカラーを更新する。*ヒント：おそらく`WindowEvent::CursorMoved`を使用する必要があるはずです*。