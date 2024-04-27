---
title: "ユニフォームバッファと3Dカメラ"
---

これまでの作業はすべて2Dのように見えて、実はずっと3Dで作業していたのです！　これが、`Vertex`構造体の`position`が2つのfloatではなく、3つのfloatの配列になっている理由の1つです。私たちは物を真正面から見ているため、シーンの3D性を実際に見ることができません。`Camera`を作って、視点を変えましょう。

## 遠近カメラ

このチュートリアルはwgpuの使い方を学ぶことに重点を置いており、線形代数についてはあまり触れていません。中で起こっていることに興味があれば、オンラインにたくさんの資料があります。ここでは、[cgmath](https://docs.rs/cgmath)を使って、すべての計算を処理することにします。以下を`Cargo.toml`に追加してください。

```toml:Cargo.toml
[dependencies]
# other deps...
cgmath = "0.18"
```

数学ライブラリを加えたので使ってみましょう。`State`構造体の上に`Camera`構造体を作成します。

```rust
struct Camera {
    eye: cgmath::Point3<f32>,
    target: cgmath::Point3<f32>,
    up: cgmath::Vector3<f32>,
    aspect: f32,
    fovy: f32,
    znear: f32,
    zfar: f32,
}

impl Camera {
    fn build_view_projection_matrix(&self) -> cgmath::Matrix4<f32> {
        // 1.
        let view = cgmath::Matrix4::look_at_rh(self.eye, self.target, self.up);
        // 2.
        let proj = cgmath::perspective(cgmath::Deg(self.fovy), self.aspect, self.znear, self.zfar);

        // 3.
        return OPENGL_TO_WGPU_MATRIX * proj * view;
    }
}
```

`build_view_projection_matrix`でマジックが起こる。

1. `view`行列はカメラの位置と回転にワールドを移動させる。これは基本的にカメラのトランスフォーム行列の逆行列です。
2. `proj`行列は、奥行きの効果を与えるためにシーンをワープさせます。これがなければ、近くのオブジェクトは遠くのオブジェクトと同じ大きさになってしまう。
3. wgpuの座標系はDirectXとMetalの座標系に基づいています。つまり、[正規化デバイス座標](https://github.com/gfx-rs/gfx/tree/master/src/backend/dx12#normalized-coordinates)では、x軸とy軸は-1.0から+1.0の範囲にあり、z軸は0.0から+1.0の範囲にあります。`cgmath`クレートは（ほとんどのゲーム数学クレートと同様に）OpenGLの座標系用に構築されています。この行列はOpenGLの座標系からwgpuの座標系にシーンを拡大縮小し、平行移動します。次のように定義します。

```rust
#[rustfmt::skip]
pub const OPENGL_TO_WGPU_MATRIX: cgmath::Matrix4<f32> = cgmath::Matrix4::new(
    1.0, 0.0, 0.0, 0.0,
    0.0, 1.0, 0.0, 0.0,
    0.0, 0.0, 0.5, 0.5,
    0.0, 0.0, 0.0, 1.0,
);
```

+ 注意：`OPENGL_TO_WGPU_MATRIX`は明示的に**必要**ではありませんが、(0, 0, 0) を中心としたモデルはクリッピングエリアの中途半端な位置になります。これはカメラ行列を使用していない場合にのみ問題となります。

では`State`へ`camera`フィールドを追加してみましょう。

```rust
struct State {
    // ...
    camera: Camera,
    // ...
}

async fn new(window: Window) -> Self {
    // let diffuse_bind_group ...

    let camera = Camera {
        // position the camera 1 unit up and 2 units back
        // +z is out of the screen
        eye: (0.0, 1.0, 2.0).into(),
        // have it look at the origin
        target: (0.0, 0.0, 0.0).into(),
        // which way is "up"
        up: cgmath::Vector3::unit_y(),
        aspect: config.width as f32 / config.height as f32,
        fovy: 45.0,
        znear: 0.1,
        zfar: 100.0,
    };

    Self {
        // ...
        camera,
        // ...
    }
}
```

さて、カメラを手に入れ、ビュー投影行列を作成できるようになったので、それを置く場所が必要です。また、それをシェーダーに取り込む方法も必要です。

## ユニフォームバッファ
ここまでは、頂点やインデックスのデータを保存したり、テクスチャをロードするために`Buffer`を使ってきました。今回もバッファを使用して、ユニフォームバッファと呼ばれるものを作成します。ユニフォームとは、シェーダのセットを呼び出すたびに利用可能なデータの塊です。技術的には、すでにテクスチャとサンプラーにユニフォームを使用しています。今回も、ビューの投影行列を保存するためにユニフォームを使用します。まず、ユニフォームを保持する構造体を作成します。

```rust
// We need this for Rust to store our data correctly for the shaders
#[repr(C)]
// This is so we can store this in a buffer
#[derive(Debug, Copy, Clone, bytemuck::Pod, bytemuck::Zeroable)]
struct CameraUniform {
    // We can't use cgmath with bytemuck directly, so we'll have
    // to convert the Matrix4 into a 4x4 f32 array
    view_proj: [[f32; 4]; 4],
}

impl CameraUniform {
    fn new() -> Self {
        use cgmath::SquareMatrix;
        Self {
            view_proj: cgmath::Matrix4::identity().into(),
        }
    }

    fn update_view_proj(&mut self, camera: &Camera) {
        self.view_proj = camera.build_view_projection_matrix().into();
    }
}
```

データの構造化ができたので、`camera_buffer`を作成しましょう。

```rust
// in new() after creating `camera`

let mut camera_uniform = CameraUniform::new();
camera_uniform.update_view_proj(&camera);

let camera_buffer = device.create_buffer_init(
    &wgpu::util::BufferInitDescriptor {
        label: Some("Camera Buffer"),
        contents: bytemuck::cast_slice(&[camera_uniform]),
        usage: wgpu::BufferUsages::UNIFORM | wgpu::BufferUsages::COPY_DST,
    }
);
```

## ユニフォームバッファとバインドグループ
いいですね！　さて、ユニフォームバッファを手に入れたところで、それをどうするか？ 答えは、バインドグループを作ることです。まず、バインドグループレイアウトを作成します。
```rust
let camera_bind_group_layout = device.create_bind_group_layout(&wgpu::BindGroupLayoutDescriptor {
    entries: &[
        wgpu::BindGroupLayoutEntry {
            binding: 0,
            visibility: wgpu::ShaderStages::VERTEX,
            ty: wgpu::BindingType::Buffer {
                ty: wgpu::BufferBindingType::Uniform,
                has_dynamic_offset: false,
                min_binding_size: None,
            },
            count: None,
        }
    ],
    label: Some("camera_bind_group_layout"),
});
```

いくつかの注意点があります。

1. 頂点シェーダで本当に必要なのはカメラ情報だけなので、`visibility` を `ShaderStages::VERTEX` に設定します。
2. `has_dynamic_offset`は、バッファ内のデータの位置が変わる可能性があることを意味します。これは、サイズが異なる複数のデータセットを1つのバッファに格納する場合に当てはまります。これをtrueに設定すると、後でオフセットを指定する必要があります。
3. `min_binding_size`はバッファの最小サイズを指定します。これを指定する必要はないので、`None`のままにしておきます。もっと詳しく知りたい場合は、[ドキュメント](https://docs.rs/wgpu/latest/wgpu/enum.BindingType.html#variant.Buffer.field.min_binding_size)を参照してください。

では、実際にバインドグループを作成しましょう。

```rust
let camera_bind_group = device.create_bind_group(&wgpu::BindGroupDescriptor {
    layout: &camera_bind_group_layout,
    entries: &[
        wgpu::BindGroupEntry {
            binding: 0,
            resource: camera_buffer.as_entire_binding(),
        }
    ],
    label: Some("camera_bind_group"),
});
```

テクスチャと同様に、`camera_bind_group_layout`をレンダーパイプラインに登録する必要があります。

```rust
let render_pipeline_layout = device.create_pipeline_layout(
    &wgpu::PipelineLayoutDescriptor {
        label: Some("Render Pipeline Layout"),
        bind_group_layouts: &[
            &texture_bind_group_layout,
            &camera_bind_group_layout,
        ],
        push_constant_ranges: &[],
    }
);
```

次に、`camera_buffer`と`camera_bind_group`を`State`に追加します。

```rust
struct State {
    // ...
    camera: Camera,
    camera_uniform: CameraUniform,
    camera_buffer: wgpu::Buffer,
    camera_bind_group: wgpu::BindGroup,
}

async fn new(window: Window) -> Self {
    // ...
    Self {
        // ...
        camera,
        camera_uniform,
        camera_buffer,
        camera_bind_group,
    }
}
```

シェーダーに入る前に最後にやるべきことは、`render()`でバインドグループを使うことです。

```rust
render_pass.set_pipeline(&self.render_pipeline);
render_pass.set_bind_group(0, &self.diffuse_bind_group, &[]);
// NEW!
render_pass.set_bind_group(1, &self.camera_bind_group, &[]);
render_pass.set_vertex_buffer(0, self.vertex_buffer.slice(..));
render_pass.set_index_buffer(self.index_buffer.slice(..), wgpu::IndexFormat::Uint16);

render_pass.draw_indexed(0..self.num_indices, 0, 0..1);
```

## 頂点シェーダーでユニフォームを使用する
頂点シェーダーを以下のように修正します。

```rust
// Vertex shader
struct CameraUniform {
    view_proj: mat4x4<f32>,
};
@group(1) @binding(0) // 1.
var<uniform> camera: CameraUniform;

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
    out.clip_position = camera.view_proj * vec4<f32>(model.position, 1.0); // 2.
    return out;
}
```

1. 新しいバインドグループを作成したので、シェーダで使用するグループを指定する必要があります。番号は `render_pipeline_layout` によって決まります。`texture_bind_group_layout` が最初にリストされているので `group(0)`、`camera_bind_group` が2番目なので `group(1)`となります。
2. 行列に関しては、乗算の順番が重要です。ベクトルは右に、行列は左に、重要な順に並べます。

## カメラのコントローラー
今このコードを実行すると、次のようになるはずです。

![](/images/rightside-up.png)

形が引き伸ばされることは少なくなりましたが、まだかなり静的です。カメラの位置を動かしてみることもできますが、ゲームのほとんどのカメラは動き回ります。このチュートリアルはwgpuの使い方についてのもので、ユーザー入力を処理する方法についてのものではないので、以下にただ`CameraController`のコードを掲載します。

```rust
struct CameraController {
    speed: f32,
    is_forward_pressed: bool,
    is_backward_pressed: bool,
    is_left_pressed: bool,
    is_right_pressed: bool,
}

impl CameraController {
    fn new(speed: f32) -> Self {
        Self {
            speed,
            is_forward_pressed: false,
            is_backward_pressed: false,
            is_left_pressed: false,
            is_right_pressed: false,
        }
    }

    fn process_events(&mut self, event: &WindowEvent) -> bool {
        match event {
            WindowEvent::KeyboardInput {
                input: KeyboardInput {
                    state,
                    virtual_keycode: Some(keycode),
                    ..
                },
                ..
            } => {
                let is_pressed = *state == ElementState::Pressed;
                match keycode {
                    VirtualKeyCode::W | VirtualKeyCode::Up => {
                        self.is_forward_pressed = is_pressed;
                        true
                    }
                    VirtualKeyCode::A | VirtualKeyCode::Left => {
                        self.is_left_pressed = is_pressed;
                        true
                    }
                    VirtualKeyCode::S | VirtualKeyCode::Down => {
                        self.is_backward_pressed = is_pressed;
                        true
                    }
                    VirtualKeyCode::D | VirtualKeyCode::Right => {
                        self.is_right_pressed = is_pressed;
                        true
                    }
                    _ => false,
                }
            }
            _ => false,
        }
    }

    fn update_camera(&self, camera: &mut Camera) {
        use cgmath::InnerSpace;
        let forward = camera.target - camera.eye;
        let forward_norm = forward.normalize();
        let forward_mag = forward.magnitude();

        // カメラがシーンの中心に近づきすぎた時のグリッチを防ぐ
        if self.is_forward_pressed && forward_mag > self.speed {
            camera.eye += forward_norm * self.speed;
        }
        if self.is_backward_pressed {
            camera.eye -= forward_norm * self.speed;
        }

        let right = forward_norm.cross(camera.up);

        // 前後が入力された時、半径を再計算する。
        let forward = camera.target - camera.eye;
        let forward_mag = forward.magnitude();

        if self.is_right_pressed {
            // ターゲットとカメラの眼との距離が変わらないように
            // スケールを変更します。したがって、眼は依然として
            // ターゲットと眼によって作られる円状にあります。
            camera.eye = camera.target - (forward + right * self.speed).normalize() * forward_mag;
        }
        if self.is_left_pressed {
            camera.eye = camera.target - (forward - right * self.speed).normalize() * forward_mag;
        }
    }
}
```

このコードは完璧ではありません。カメラを回転させると、ゆっくりと後ろに下がっていきます。しかし、私たちの目的には合っています。自由に改良してください！

このコードを既存のコードに追加して、何かできるようにする必要があります。コントローラを`State`に追加し、`new()`で作成します。

```rust
struct State {
    // ...
    camera: Camera,
    // NEW!
    camera_controller: CameraController,
    // ...
}
// ...
impl State {
    async fn new(window: Window) -> Self {
        // ...
        let camera_controller = CameraController::new(0.2);
        // ...

        Self {
            // ...
            camera_controller,
            // ...
        }
    }
}
```

いよいよ`input()`にコードを追加します（まだ追加していないことを前提に）！

```rust
fn input(&mut self, event: &WindowEvent) -> bool {
    self.camera_controller.process_events(event)
}
```

この時点まで、カメラコントローラーは実際には何もしていない。ユニフォーム・バッファの値を更新する必要があります。そのための主な方法はいくつかあります。

1. 別のバッファを作成し、その内容を`camera_buffer`にコピーします。この新しいバッファはステージングバッファと呼ばれます。メインバッファ（この場合は`camera_buffer`）の内容はGPUだけがアクセスできるようになるので、通常はこの方法で行います。GPUはスピードの最適化を行うことができますが、CPU経由でバッファにアクセスできる場合は、そのようなことはできません。
2. バッファ自身に対して、`map_read_async` と `map_write_async` というマッピングメソッドを呼び出すことができます。これらはバッファの内容に直接アクセスすることを可能にしますが、これらのメソッドの非同期的な側面を処理する必要があります。また、バッファが`BufferUsages::MAP_READ`や`BufferUsages::MAP_WRITE`を使用する必要があります。ここではそれについて話しませんが、もっと知りたければ[Wgpu without a window](https://sotrh.github.io/learn-wgpu/showcase/windowless)のチュートリアルをチェックしてください。
3. `queue`で`write_buffer`を使うことができる。

ここでは3番のオプションを使うことにします。

```rust
fn update(&mut self) {
    self.camera_controller.update_camera(&mut self.camera);
    self.camera_uniform.update_view_proj(&self.camera);
    self.queue.write_buffer(&self.camera_buffer, 0, bytemuck::cast_slice(&[self.camera_uniform]));
}
```

やるべきことはそれだけです。 ここでコードを実行すると、木のテクスチャを含む五角形が表示されます。wasd/矢印キーを使用して回転したり、ズームインしたりできます。

## 課題
モデルをカメラから独立して回転させます。ヒント：このためには別の行列が必要です。