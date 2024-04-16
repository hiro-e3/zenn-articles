---
title: "テクスチャとバインドグループ"
---

# Texture and bind groups
ここまでは、非常にシンプルな図形を描いてきました。三角形だけでゲームを作ることはできますが、非常に細かいオブジェクトを描こうとすると、ゲームを実行できるデバイスが大幅に制限されます。しかし、**テクスチャ**を使えば、この問題を回避することができます。

テクスチャとは、三角形のメッシュに画像を重ねて、より細かく見えるようにしたものです。テクスチャには、法線マップ、バンプマップ、スペキュラマップ、拡散マップなど、複数の種類があります。今回は拡散マップ、もっと簡単に言うとカラーテクスチャについて説明します。

## 画像をファイルから読み込む
メッシュに画像をマッピングする場合、まず画像が必要です。この幸せの木を使ってみましょう。
![](https://sotrh.github.io/learn-wgpu/assets/img/happy-tree.bdff8a19.png)
木を読み込むために、[image crate](https://crates.io/crates/image)を使用します。依存関係へ追加してみましょう。

```Cargo.toml:toml
[dependencies.image]
version = "0.24"
default-features = false
features = ["png", "jpeg"]
```

`image`に含まれているjpegデコーダーは、スレッドを使用してデコードを高速化するために[rayon](https://docs.rs/rayon)を使用しています。WASMは現在スレッドをサポートしていないので、Web上でjpegをロードしようとしたときにコードがクラッシュしないように、これを無効にする必要があります。

:::
WASMでjpegをデコードするのはあまりパフォーマンスが良くありません。WASMで一般的に画像の読み込みを高速化したい場合は、`wasm-bindgen`でビルドするときに、`image`の代わりにブラウザの組み込みデコーダを使うこともできます。これには、画像を取得するためにRustで`<img>`タグを作成し、ピクセルデータを取得するために`<canvas>`を作成する必要がありますが、これは読者のための練習として残しておきます。
:::

`State`の`new()`メソッドでは、`surface`を設定した直後に以下を追加してください。

```rust
surface.configure(&device, &config);
// NEW!

let diffuse_bytes = include_bytes!("happy-tree.png");
let diffuse_image = image::load_from_memory(diffuse_bytes).unwrap();
let diffuse_rgba = diffuse_image.to_rgba8();

use image::GenericImageView;
let dimensions = diffuse_image.dimensions();
```

ここでは、画像ファイルからバイナリを取得、ロードして、rgbaバイトの`Vec`に変換しています。また、実際の`Texture`を作成するときのために、画像の寸法も保存しておきます。

では、`Texture`を作成してみましょう。

```rust
let texture_size = wgpu::Extent3d {
    width: dimensions.0,
    height: dimensions.1,
    depth_or_array_layers: 1,
};
let diffuse_texture = device.create_texture(
    &wgpu::TextureDescriptor {
        // すべてのテクスチャは3Dとして保存されるので、深度を1に設定することで2Dテクスチャを表現する。
        size: texture_size,
        mip_level_count: 1, // 後述します
        sample_count: 1,
        dimension: wgpu::TextureDimension::D2,
        // ほとんどの画像はsRGBで保存されているので、ここではそれを反映させる必要がある。
        format: wgpu::TextureFormat::Rgba8UnormSrgb,
        // TEXTURE_BINDING はwgpuにシェーダーでこのテクスチャーを使いたいことを伝える。
        // COPY_DST はこのテクスチャにデータをコピーすることを意味する。
        usage: wgpu::TextureUsages::TEXTURE_BINDING | wgpu::TextureUsages::COPY_DST,
        label: Some("diffuse_texture"),
        // SurfaceConfigと同様. このテクスチャの TextureView を作成するために
        // どのテクスチャ形式を使用できるかを指定する。
        // 基本となるテクスチャ形式 (この場合 Rgba8UnormSrgb)は常にサポートされる。
        // 異なるテクスチャ形式の使用は、WebGL2ではサポートされていないことに注意。
        view_formats: &[],
    }
);
```

## テクスチャにデータを取り込む
`Texture`構造体には、データを直接操作するためのメソッドはありません。しかし、先ほど作成した`queue`の`write_texture`というメソッドを使って、テクスチャを読み込むことができます。その方法について見てみましょう。
```rust
queue.write_texture(
    // wgpuへどこにピクセルデータをコピーすればよいか伝える
    wgpu::ImageCopyTexture {
        texture: &diffuse_texture,
        mip_level: 0,
        origin: wgpu::Origin3d::ZERO,
        aspect: wgpu::TextureAspect::All,
    },
    // 実際のピクセルデータ
    &diffuse_rgba,
    // テクスチャのレイアウト
   wgpu::ImageDataLayout {
        offset: 0,
        bytes_per_row: Some(4 * dimensions.0),
        rows_per_image: Some(dimensions.1),
    },
    texture_size,
);
```

::: message
テクスチャへのデータ書き込みは、従来はピクセルデータをバッファにコピーしてからテクスチャにコピーする方法でした。`write_texture`を使用すると、バッファを1つ少なくできるので、少し効率的です。しかし、従来の方法が必要となる場合に備えてここに残しておきます。

```rust
let buffer = device.create_buffer_init(
    &wgpu::util::BufferInitDescriptor {
        label: Some("Temp Buffer"),
        contents: &diffuse_rgba,
        usage: wgpu::BufferUsages::COPY_SRC,
    }
);

let mut encoder = device.create_command_encoder(&wgpu::CommandEncoderDescriptor {
    label: Some("texture_buffer_copy_encoder"),
});

encoder.copy_buffer_to_texture(
    wgpu::ImageCopyBuffer {
        buffer: &buffer,
        offset: 0,
        bytes_per_row: 4 * dimensions.0,
        rows_per_image: dimensions.1,
    },
    wgpu::ImageCopyTexture {
        texture: &diffuse_texture,
        mip_level: 0,
        array_layer: 0,
        origin: wgpu::Origin3d::ZERO,
    },
    size,
);

queue.submit(std::iter::once(encoder.finish()));
```

`bytes_per_row`フィールドは少し考慮する必要があります。この値は256の倍数である必要があります。詳しくは[gifチュートリアル](https://sotrh.github.io/learn-wgpu/showcase/gifs/#how-do-we-make-the-frames)をご覧ください。
:::

## TextureViewsとSampler
テクスチャにデータが入ったので、それを利用する方法が必要です。そこで登場するのが`TextureView`と`Sampler`です。`TextureView`はテクスチャを表示するためのものです。`Sampler`は`Texture`をどのようにサンプリングするかを制御します。サンプリングはGIMPやPhotoshop(訳注:Clip Studio Paint)のスポイトツールに似た働きをします。プログラムは、テクスチャ上の座標（テクスチャ座標と呼ばれる）を提供します。そして、サンプラーはテクスチャといくつかの内部パラメータに基づいて、対応する色を返します。

それでは、`diffuse_texture_view` と `diffuse_sampler` を定義してみましょう。

```rust
// テクスチャビューはそれほど定義する必要はありません。
// wgpuに定義させましょう
let diffuse_texture_view = diffuse_texture.create_view(&wgpu::TextureViewDescriptor::default());
let diffuse_sampler = device.create_sampler(&wgpu::SamplerDescriptor {
    address_mode_u: wgpu::AddressMode::ClampToEdge,
    address_mode_v: wgpu::AddressMode::ClampToEdge,
    address_mode_w: wgpu::AddressMode::ClampToEdge,
    mag_filter: wgpu::FilterMode::Linear,
    min_filter: wgpu::FilterMode::Nearest,
    mipmap_filter: wgpu::FilterMode::Nearest,
    ..Default::default()
});
```

`address_mode_*` パラメータは、サンプラーがテクスチャーの外側の座標を取得した場合の処理を決定します。いくつかのオプションから選択できます。

+ `ClampToEdge`：テクスチャの外側にあるテクスチャ座標は、テクスチャのエッジに最も近いピクセルの色を返します。
+ `Repeat`：テクスチャ座標がテクスチャの寸法を超えると、テクスチャは繰り返されます。
+ `MirrorRepeat`：`Repeat`に似ていますが、境界を越えるときにイメージが反転します。

![](/images/address_mode.png)

`mag_filter`フィールドと`min_filter`フィールドは、サンプルのフットプリントが1テクセルより小さいときと大きいときの処理を記述します。この2つのフィールドは通常、シーン内のマッピングがカメラから遠いか近い場合に機能します。

2つのオプションがあり、

+ `Linear`: 各次元で2つのテクセルを選択し、それらの値の間の線形補間を返す。
+ `Nearest`: テクスチャ座標に最も近いテクセル値を返します。これにより、遠くから見ると鮮明ですが、近くではピクセル化された画像が作成されます。ただし、ピクセルアートゲームやMinecraftのようなボクセルゲームのように、テクスチャがピクセル化するように設計されている場合は、これが望ましい場合があります。

ミップマップは複雑なトピックなので、将来的には独自のセクションが必要になるでしょう(訳注:todoの項目をまとめたページがあったみたいだが存在しない)。今のところ、`mipmap_filter` は `(mag/min)_filter` と同じように機能し、ミップマップ間のブレンド方法をサンプラーに指示すると言えるでしょう。

他のフィールドはデフォルトのものを使っています。もし、それらが何であるか知りたい場合は、[wgpu docs](https://docs.rs/wgpu/latest/wgpu/struct.SamplerDescriptor.html)をチェックしてください。

このように様々なリソースがあるのは良いことですが、どこにも接続できないのであればあまり意味がありません。ここで`BindGroup`と`PipelineLayout`の出番です。

## The BindGroup
`BindGroup`はリソースのセットと、それらがシェーダーによってどのようにアクセスされるかを記述します。`BindGroup`は`BindGroupLayout`を使って作成します。まず、そのうちの1つを作りましょう。
```rust
let texture_bind_group_layout =
            device.create_bind_group_layout(&wgpu::BindGroupLayoutDescriptor {
                entries: &[
                    wgpu::BindGroupLayoutEntry {
                        binding: 0,
                        visibility: wgpu::ShaderStages::FRAGMENT,
                        ty: wgpu::BindingType::Texture {
                            multisampled: false,
                            view_dimension: wgpu::TextureViewDimension::D2,
                            sample_type: wgpu::TextureSampleType::Float { filterable: true },
                        },
                        count: None,
                    },
                    wgpu::BindGroupLayoutEntry {
                        binding: 1,
                        visibility: wgpu::ShaderStages::FRAGMENT,
                        // 上記の対応するTextureエントリのフィルター可能なフィールドと一致する必要がある。
                        ty: wgpu::BindingType::Sampler(wgpu::SamplerBindingType::Filtering),
                        count: None,
                    },
                ],
                label: Some("texture_bind_group_layout"),
            });
```

`texture_bind_group_layout`には2つのエントリがあります。1つはバインディング0にサンプリングされたテクスチャ用、もう1つはバインディング1にサンプラー用のものです。これらのバインディングは両方とも、`FRAGMENT `で指定されたフラグメントシェーダにのみ表示されます。このフィールドに指定できる値は、`NONE`、`VERTEX`、`FRAGMENT`、または `COMPUTE` のビットの組み合わせです。ほとんどの場合、`FRAGMENT` はテクスチャとサンプラーにのみ使用しますが、他に何が利用できるかを知っておくとよいでしょう。

`texture_bind_group_layout` で、`BindGroup` を作成することができます。
```rust
let diffuse_bind_group = device.create_bind_group(
    &wgpu::BindGroupDescriptor {
        layout: &texture_bind_group_layout,
        entries: &[
            wgpu::BindGroupEntry {
                binding: 0,
                resource: wgpu::BindingResource::TextureView(&diffuse_texture_view),
            },
            wgpu::BindGroupEntry {
                binding: 1,
                resource: wgpu::BindingResource::Sampler(&diffuse_sampler),
            }
        ],
        label: Some("diffuse_bind_group"),
    }
);
```
これを見ると少しデジャヴュを感じるかもしれませんね。`BindGroup`は`BindGroupLayout`をより具体的に宣言したものだからです。`BindGroup`が分かれているのは、同じ`BindGroupLayout`を共有していれば、その場で`BindGroup`を入れ替えられるからです。作成したテクスチャとサンプラーは、それぞれ`BindGroup`に追加する必要があります。今回は、それぞれのテクスチャに対して新しいバインドグループを作成します。

`diffuse_bind_group`ができたので、`State`構造体に追加しましょう。
```rust
struct State {
    surface: wgpu::Surface,
    device: wgpu::Device,
    queue: wgpu::Queue,
    config: wgpu::SurfaceConfiguration,
    size: winit::dpi::PhysicalSize<u32>,
    render_pipeline: wgpu::RenderPipeline,
    vertex_buffer: wgpu::Buffer,
    index_buffer: wgpu::Buffer,
    num_indices: u32,
    diffuse_bind_group: wgpu::BindGroup, // NEW!
}
```

これらのフィールドを`new`メソッドで返すようにします。

```rust
impl State {
    async fn new() -> Self {
        // ...
        Self {
            surface,
            device,
            queue,
            config,
            size,
            render_pipeline,
            vertex_buffer,
            index_buffer,
            num_indices,
            // NEW!
            diffuse_bind_group,
        }
    }
}
```

`BindGroup`を取得したので、`render()`関数内で使用できます。

```rust
// render()
// ...
render_pass.set_pipeline(&self.render_pipeline);
render_pass.set_bind_group(0, &self.diffuse_bind_group, &[]); // NEW!
render_pass.set_vertex_buffer(0, self.vertex_buffer.slice(..));
render_pass.set_index_buffer(self.index_buffer.slice(..), wgpu::IndexFormat::Uint16);

render_pass.draw_indexed(0..self.num_indices, 0, 0..1);
```

## Pipeline Layout
[パイプラインの章](/books/1db281fb5a1d47/viewer/pipeline%252Emd)で作成した`PipelineLayout`を覚えていますか？ やっと使えるようになりました。`PipelineLayout`には、パイプラインが使用できる`BindGroupLayout`のリストが含まれています。`render_pipeline_layout` を修正して `texture_bind_group_layout` を使用するようにします。

```rust
async fn new(...) {
    // ...
    let render_pipeline_layout = device.create_pipeline_layout(
        &wgpu::PipelineLayoutDescriptor {
            label: Some("Render Pipeline Layout"),
            bind_group_layouts: &[&texture_bind_group_layout], // NEW!
            push_constant_ranges: &[],
        }
    );
    // ...
}
```

## VERTICESの変更点
`Vertex`の定義について、いくつか変更する必要があります。今までは、メッシュの色を設定するために`color`フィールドを使用していました。テクスチャを使用するため、`color`を `tex_coords` に置き換える必要があります。この座標は`Sampler`に渡され、適切な色を取得します。

`tex_coords`は2次元なので、3つのfloatではなく、2つのfloatを取るようにフィールドを変更します。

まず、`Vertex`構造体を変更します。
```rust
#[repr(C)]
#[derive(Copy, Clone, Debug, bytemuck::Pod, bytemuck::Zeroable)]
struct Vertex {
    position: [f32; 3],
    tex_coords: [f32; 2], // NEW!
}
```

そして、その変更を`VertexBufferLayout`に反映させます。

```rust
impl Vertex {
    fn desc() -> wgpu::VertexBufferLayout<'static> {
        use std::mem;
        wgpu::VertexBufferLayout {
            array_stride: mem::size_of::<Vertex>() as wgpu::BufferAddress,
            step_mode: wgpu::VertexStepMode::Vertex,
            attributes: &[
                wgpu::VertexAttribute {
                    offset: 0,
                    shader_location: 0,
                    format: wgpu::VertexFormat::Float32x3,
                },
                wgpu::VertexAttribute {
                    offset: mem::size_of::<[f32; 3]>() as wgpu::BufferAddress,
                    shader_location: 1,
                    format: wgpu::VertexFormat::Float32x2, // NEW!
                },
            ]
        }
    }
}
```

最後に`VERTICES`そのものを変更する必要があります。既存の定義を次のように置き換えます。

```rust
// Changed
const VERTICES: &[Vertex] = &[
    Vertex { position: [-0.0868241, 0.49240386, 0.0], tex_coords: [0.4131759, 0.99240386], }, // A
    Vertex { position: [-0.49513406, 0.06958647, 0.0], tex_coords: [0.0048659444, 0.56958647], }, // B
    Vertex { position: [-0.21918549, -0.44939706, 0.0], tex_coords: [0.28081453, 0.05060294], }, // C
    Vertex { position: [0.35966998, -0.3473291, 0.0], tex_coords: [0.85967, 0.1526709], }, // D
    Vertex { position: [0.44147372, 0.2347359, 0.0], tex_coords: [0.9414737, 0.7347359], }, // E
];
```

## シェーダの時間
新しい`Vertex`の構造ができたので、シェーダを更新する時が来ました。まず、頂点シェーダに `tex_coord` を渡す必要があります。そして、それをフラグメントシェーダに渡して、`Sampler`から最終的なカラーを取得します。まず、バーテックスシェーダから始めましょう。
```wgsl
// Vertex shader

struct VertexInput {
    @location(0) position: vec3<f32>,
    @location(1) tex_coords: vec2<f32>,
}

struct VertexOutput {
    @builtin(position) clip_position: vec4<f32>,
    @location(0) tex_coords: vec2<f32>,
}

@vertex
fn vs_main(
    model: VertexInput,
) -> VertexOutput {
    var out: VertexOutput;
    out.tex_coords = model.tex_coords;
    out.clip_position = vec4<f32>(model.position, 1.0);
    return out;
}
```
頂点シェーダが `tex_coord` を出力するようになったので、フラグメントシェーダを変更して、それを取り込む必要があります。この座標があれば、サンプラーを使用してテクスチャから色を取得することができます。
```wgsl
// Fragment shader

@group(0) @binding(0)
var t_diffuse: texture_2d<f32>;
@group(0) @binding(1)
var s_diffuse: sampler;

@fragment
fn fs_main(in: VertexOutput) -> @location(0) vec4<f32> {
    return textureSample(t_diffuse, s_diffuse, in.tex_coords);
}
```
変数`t_diffuse`と`s_diffuse`は、いわゆるユニフォームと呼ばれるものです。ユニフォームについては、[カメラのセクション](https://sotrh.github.io/learn-wgpu/beginner/tutorial6-uniforms/)で詳しく説明します。今のところ、`group()`は`set_bind_group()`の第1パラメータに対応し、`binding()`は`BindGroupLayout`と`BindGroup`を作成したときに指定したバインドに対応することだけを知っていればよいです。

## 実行結果
今、このプログラムを実行すると、次のような結果が得られるはずです。

![](/images/upside-down.png)

おかしいな、木が逆さまだ! これは、wgpuのワールド座標はY軸が上を向いているのに対し、テクスチャ座標はY軸が下を向いているためです。つまり、テクスチャ座標の(0, 0)は画像の左上、(1, 1)は右下に相当します。

![](/images/happy-tree-uv-coords.png)
各テクスチャ座標の`y`座標を`1-y`に置き換えることで、三角形を真横にすることができます。
```rust
const VERTICES: &[Vertex] = &[
    // Changed
    Vertex { position: [-0.0868241, 0.49240386, 0.0], tex_coords: [0.4131759, 0.00759614], }, // A
    Vertex { position: [-0.49513406, 0.06958647, 0.0], tex_coords: [0.0048659444, 0.43041354], }, // B
    Vertex { position: [-0.21918549, -0.44939706, 0.0], tex_coords: [0.28081453, 0.949397], }, // C
    Vertex { position: [0.35966998, -0.3473291, 0.0], tex_coords: [0.85967, 0.84732914], }, // D
    Vertex { position: [0.44147372, 0.2347359, 0.0], tex_coords: [0.9414737, 0.2652641], }, // E
];
```
これで、六角形の上に木の画像を正立させることができました。
![](/images/rightside-up.png)

## 片付け
便宜上、テクスチャのコードを独自のモジュールにまとめてみましょう。まず、エラー処理を簡単にするために、`Cargo.toml` ファイルに [anyhow](https://docs.rs/anyhow/latest/anyhow/) crate を追加する必要があります。

```toml:Cargo.toml
[dependencies]
image = "0.23"
cgmath = "0.18"
winit = "0.26"
env_logger = "0.9"
log = "0.4"
pollster = "0.2"
wgpu = "0.12"
bytemuck = { version = "1.4", features = [ "derive" ] }
anyhow = "1.0" // NEW!
```
そして、`src/texture.rs`という新しいファイルに、以下を追加してください。

```rust
use image::GenericImageView;
use anyhow::*;

pub struct Texture {
    pub texture: wgpu::Texture,
    pub view: wgpu::TextureView,
    pub sampler: wgpu::Sampler,
}

impl Texture {
    pub fn from_bytes(
        device: &wgpu::Device,
        queue: &wgpu::Queue,
        bytes: &[u8], 
        label: &str
    ) -> Result<Self> {
        let img = image::load_from_memory(bytes)?;
        Self::from_image(device, queue, &img, Some(label))
    }

    pub fn from_image(
        device: &wgpu::Device,
        queue: &wgpu::Queue,
        img: &image::DynamicImage,
        label: Option<&str>
    ) -> Result<Self> {
        let rgba = img.to_rgba8();
        let dimensions = img.dimensions();

        let size = wgpu::Extent3d {
            width: dimensions.0,
            height: dimensions.1,
            depth_or_array_layers: 1,
        };
        let texture = device.create_texture(
            &wgpu::TextureDescriptor {
                label,
                size,
                mip_level_count: 1,
                sample_count: 1,
                dimension: wgpu::TextureDimension::D2,
                format: wgpu::TextureFormat::Rgba8UnormSrgb,
                usage: wgpu::TextureUsages::TEXTURE_BINDING | wgpu::TextureUsages::COPY_DST,
                view_formats: &[],
            }
        );

        queue.write_texture(
            wgpu::ImageCopyTexture {
                aspect: wgpu::TextureAspect::All,
                texture: &texture,
                mip_level: 0,
                origin: wgpu::Origin3d::ZERO,
            },
            &rgba,
            wgpu::ImageDataLayout {
                offset: 0,
                bytes_per_row: Some(4 * dimensions.0),
                rows_per_image: Some(dimensions.1),
            },
            size,
        );

        let view = texture.create_view(&wgpu::TextureViewDescriptor::default());
        let sampler = device.create_sampler(
            &wgpu::SamplerDescriptor {
                address_mode_u: wgpu::AddressMode::ClampToEdge,
                address_mode_v: wgpu::AddressMode::ClampToEdge,
                address_mode_w: wgpu::AddressMode::ClampToEdge,
                mag_filter: wgpu::FilterMode::Linear,
                min_filter: wgpu::FilterMode::Nearest,
                mipmap_filter: wgpu::FilterMode::Nearest,
                ..Default::default()
            }
        );
        
        Ok(Self { texture, view, sampler })
    }
}
```

:::message
`as_rgba8()`の代わりに`to_rgba8()`を使っていることに注意してください。PNGはアルファチャンネルを持っているので、`as_rgba8()`を使っても問題なく動作します。しかし、JPEGにはアルファチャンネルがないので、これから使うJPEGテクスチャ画像で`as_rgba8()`を呼び出そうとすると、コードがパニックになります。その代わりに`to_rgba8()`を使えば、元画像にアルファチャンネルがなくてもアルファチャンネルを持つ新しい画像バッファを生成することができます。
:::

モジュールとして`texture.rs`をインポートする必要があるので、`lib.rs`の先頭のどこかに以下を追加してください。

```rust:lib.rs
mod texture;
```

`new()`内のテクスチャ作成コードが大幅に簡略化されました。

```rust
surface.configure(&device, &config);
let diffuse_bytes = include_bytes!("happy-tree.png"); // CHANGED!
let diffuse_texture = texture::Texture::from_bytes(&device, &queue, diffuse_bytes, "happy-tree.png").unwrap(); // CHANGED!

// `let texture_bind_group_layout = ...`まで削除できます。
```

バインドグループがどのようにレイアウトされているかを`Texture`が知る必要がないように、`BindGroup`を別に保存する必要があることに変わりはありません。`diffuse_bind_group` の作成は、`diffuse_texture` の `view` と `sampler` フィールドを使用するように少し変更します。

```rust
let diffuse_bind_group = device.create_bind_group(
    &wgpu::BindGroupDescriptor {
        layout: &texture_bind_group_layout,
        entries: &[
            wgpu::BindGroupEntry {
                binding: 0,
                resource: wgpu::BindingResource::TextureView(&diffuse_texture.view), // CHANGED!
            },
            wgpu::BindGroupEntry {
                binding: 1,
                resource: wgpu::BindingResource::Sampler(&diffuse_texture.sampler), // CHANGED!
            }
        ],
        label: Some("diffuse_bind_group"),
    }
);
```

最後に、`State`フィールドを更新して、新しい`Texture`構造体を使用するようにしましょう。
```rust
struct State {
    // ...
    diffuse_bind_group: wgpu::BindGroup,
    diffuse_texture: texture::Texture, // NEW
}
```
```rust
impl State {
    async fn new() -> Self {
        // ...
        Self {
            // ...
            num_indices,
            diffuse_bind_group,
            diffuse_texture, // NEW
        }
    }
}
```

ふぅ！

これらの変更により、コードは以前と同じように動作するはずですが、テクスチャを作成する方法がより簡単になりました。

## 課題
別のテクスチャを作成し、スペースキーを押すと入れ替わるようにしてください。

[コードを確認](https://github.com/sotrh/learn-wgpu/tree/master/code/beginner/tutorial5-textures/)