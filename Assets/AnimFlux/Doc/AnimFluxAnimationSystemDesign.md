## AnimFlux 动画系统需求与设计

### 1. 背景与目标
- `AnimFlux.Runtime.Playables` 已具备 `AFGraph`、`AFRoot`、`AFLayerManager`、`AFLayer` 等基础设施，可通过 PlayableGraph 播放并切换动画。
- 目标是在现有框架上扩展一套可用于生产的复杂动画系统，统一使用 Playables，满足分层动画、Locomotion、IK、动画事件等需求，同时保持 Animator 仅承担 Avatar/Root Motion/IK 回调等底层职能。

### 2. 需求清单
- **R1 分层动画**：支持 Base / UpperBody / Additive / IK 至少四层，支持动态增删、权重控制、AvatarMask 配置。
- **R2 Locomotion 模块**：提供 `SetMoveSpeed`、`SetMoveDirection`、`SetIsGrounded` 等接口，由外部逻辑输入参数，在层内平滑混合 Idle/Walk/Run 等动画；可开关 Root Motion。
- **R3 IK 支持**：通过统一接口（如 `CharacterIKController`）管理 LookAt、手脚 IK，可在 Playable 中执行或借助 Animator `OnAnimatorIK`，需与层系统协同。
- **R4 动画事件**：优先使用 ScriptPlayable 驱动事件或集中式 AnimationEvent Dispatcher，避免分散在 Clip；对外暴露 `IAnimationEventHandler`。
- **R5 Graph 生命周期**：可创建/销毁/暂停/恢复，支持手动 Evaluate 以便调试。
- **R6 可扩展性**：模块化设计，便于未来接入 Ability、Constraint、Procedural Pose 等；保持 `AnimFlux.Runtime` 命名空间规范。
- **R7 调试与监控**：提供运行时状态（层权重、当前 Clip、Fade 状态）和基础 Editor Inspector/Window（后续迭代）。
- **R8 性能**：跨层混合与事件回调需避免 GC；复用 `AFPlayableUtils` per-frame hook，确保常驻开销可控。

### 3. 现有基础
- **PlayableGraph Host**：`AFGraphHost` 负责 Graph 创建、更新模式与 Play/Pause。
- **Root Mixer 管理**：`AFRoot` 封装 `AnimationLayerMixerPlayable`、AnimationOutput 绑定与层级操作（权重、AvatarMask、Additive、速度）。
- **层与 Clip 过渡**：`AFLayerManager` + `AFLayer` 使用每层自带的 `ScriptPlayable` 行为驱动 CrossFade，保持 PlayableGraph 自更新。

### 4. 系统设计

#### 4.1 模块划分
- `AnimFlux.Runtime.Core`
  - `AnimController`：组合 Graph、Locomotion、IK、Event 模块，对外暴露统一接口。
  - `AnimationLayerType` 枚举与层配置。
- `AnimFlux.Runtime.Playables`
  - 保留 `AFGraph` 家族；新增 `LayerDefinition`、`LayerConfigurator` 数据驱动层创建。
  - ScriptPlayable 模板（`AnimationEventBehaviour`、`IKBehaviour` 等）。
- `AnimFlux.Runtime.Locomotion`
  - `LocomotionLayer`：封装 BaseLayer Mixer、参数平滑、Blend 数据。
  - `LocomotionConfig`（ScriptableObject）：存储速度段 Clip、曲线、Root Motion 策略。
- `AnimFlux.Runtime.IK`
  - `CharacterIKController`：统一 SetLookAtTarget / SetHandIKTarget / SetFootIKTarget。
  - `IKPlayableBehaviour`：在 Graph 中执行 IK 时读取控制器数据并输出姿势修正。
- `AnimFlux.Runtime.Events`
  - `AnimationEventStream` 数据源。
  - `AnimationEventPlayable`：沿 Clip 时间线触发回调，调用 `IAnimationEventHandler`。

#### 4.2 PlayableGraph 结构
1. `AFGraphHost` 创建 Graph（GameTime）。
2. `AFRoot` 初始化 `AnimationLayerMixerPlayable` 与 `AnimationPlayableOutput`。
3. `AFLayerManager` 依据 `LayerDefinition` 创建 `AnimationMixerPlayable`（每层 2 Input Slots）。
4. BaseLayer 插入 Locomotion Mixer，UpperBody / Additive / IK 层插入对应 Playable。
5. ScriptPlayable（事件、IK）可串接到层的 Mixer 之前，或作为独立层输入。
6. per-frame 逻辑统一通过 `AFPlayableUtils` Mixer 绑定，避免重复连接。

#### 4.3 分层设计
- 引入 `LayerDefinition`（名称、AvatarMask、默认权重、BlendMode）。
- `LayerConfigurator` 读取配置并调用 `AFGraph.AddLayer`，返回层索引常量。
- `AnimController` 内部维护 `Dictionary<AnimationLayerType, int>`，提供高层语义接口（`PlayUpperBodyClip`、`SetAdditivePose`、`SetIKWeight`）。

#### 4.4 Locomotion 模块
- `LocomotionLayer` 绑定 BaseLayer 索引，内部可使用 `AnimationMixerPlayable` 子树形成 Idle/Walk/Run 等速度混合。
- 参数输入：`DesiredSpeed`、`MoveDirection`、`IsGrounded`、`RootMotionMode`。
- 使用 `Mathf.SmoothDamp`、`Vector3.Slerp` 等平滑 Blend，驱动 Mixer 权重。
- Root Motion：通过 `Animator.applyRootMotion` 配合 `AnimController` 控制；如禁用则输出位移供角色控制器使用。

#### 4.5 IK 模块
- `CharacterIKController` 存储 IK 目标、权重、优先级。
- 支持两种模式：
  1. **Animator IK 回调**：`AnimController` 在 `OnAnimatorIK` 中读取控制器数据，统一调用 `Animator.SetIKPosition/Weight`。
  2. **Playable IK 层**：创建 `IKPlayableBehaviour`，挂在 `AnimationLayerMixerPlayable` 独立层，在 `PrepareFrame` 应用姿势修正。
- IK 权重需平滑过渡，并与 AvatarMask 协调仅影响目标身体部位。

#### 4.6 动画事件
- `AnimationEventData`：记录 ClipName / NormalizedTime / EventId。
- `AnimationEventPlayable`：ScriptPlayable 在 `PrepareFrame` 检测时间窗口并调用 `IAnimationEventHandler.OnAnimationEvent(string eventId, float normalizedTime)`。
- `AnimController` 或 gameplay 逻辑实现 Handler 处理攻击判定、音效等。

#### 4.7 数据流与 API
- 上层 gameplay 仅依赖 `AnimController`：
  - `Initialize(Animator animator, AnimControllerConfig config)`
  - `SetMovement(float speed, Vector3 direction, bool grounded)`
  - `PlayAbility(AbilityAnimationData data)`
  - `SetAimTarget(Transform target)`
  - `RegisterEventHandler(IAnimationEventHandler handler)`
- `AnimController` 内部协调 `AFGraph`、`LocomotionLayer`、`IKController` 等模块，避免外部直接操作 Playable。

### 5. 实施计划
1. 实现 `LayerDefinition` + `LayerConfigurator`，初始化标准层。
2. 开发 `LocomotionLayer`，实现参数接口与 Mixer 构建、Root Motion 支持。
3. 设计 `CharacterIKController` 与 `IKPlayableBehaviour`，先支持 Animator IK，再拓展 Playable IK。
4. 建立动画事件系统（数据格式 + ScriptPlayable + Handler）。
5. 在 UpperBody/Additive 层封装 Ability/Overlay 播放接口。
6. 提供运行时调试接口与基础 Editor Inspector（后续迭代）。
7. 扩充 Samples，演示分层、Locomotion、IK、事件协同。

### 6. 风险与注意事项
- 确保 Playable 资源（ClipPlayable、Mixer、ScriptPlayable）正确销毁，遵循 `AFLayer.Dispose` 模式。
- 层级混合时注意 AvatarMask 与 Additive 设置，避免姿势覆盖问题。
- IK 与 Locomotion 更新顺序需固定，确保最终姿势后再执行 IK。
- 动画事件在跨层/跨 Fade 时需同步 Clip 进度，避免重复触发。

