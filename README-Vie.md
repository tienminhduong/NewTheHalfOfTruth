# NewTheHalfOfTruth

## Tóm tắt

NewTheHalfOfTruth là game điều tra và phiêu lưu 2D được xây dựng bằng Unity. Người chơi khám phá các scene liên kết với nhau, trò chuyện với NPC, thu thập bằng chứng, ghi chú vào sổ tay, mở khóa cửa và tiến triển thông qua các sự kiện cũng như cutscene được kịch bản hóa.

Các cuộc trò chuyện với NPC được gửi đến một dịch vụ LLM/RAG bên ngoài thông qua `LLMService`. Game cũng lưu tiến trình của người chơi cục bộ, bao gồm các NPC đã tương tác, ghi chú bằng chứng, cửa đã mở khóa và dữ liệu save tùy chỉnh.

## Công nghệ chính

| Phạm vi | Công nghệ |
| --- | --- |
| Game engine | Unity `6000.3.19f1` |
| Ngôn ngữ | C# |
| Rendering | Universal Render Pipeline (URP) |
| Gameplay 2D | Unity 2D Animation, Sprites, Tilemap, Sprite Shape, Aseprite và PSD importers |
| Input | Unity Input System với `InputSystem_Actions` |
| Nạp asset | Unity Addressables và `AssetReference` |
| Xử lý bất đồng bộ | Cysharp UniTask |
| Camera | Cinemachine |
| Cutscene | Unity Timeline và Signal assets |
| UI | Unity UI và TextMesh Pro |
| Công cụ Inspector | Tri Inspector |
| Hội thoại NPC | HTTP request đến API RAG/LLM bên ngoài |

Endpoint hội thoại bên ngoài hiện được `LLMService` sử dụng:

```text
https://rag-6fs2.onrender.com/api/query/ask
```

Endpoint này đang được hard-code trong Unity client. Backend không nằm trong repository này.

## Cấu trúc repository

```text
NewTheHalfOfTruth/
├── Assets/
│   ├── Scenes/                 Các scene Unity
│   ├── Scripts/
│   │   ├── Animations/         Hệ thống animation nhân vật
│   │   ├── Game/               Player, interactable, cửa, bằng chứng và NPC
│   │   ├── Interfaces/         Các contract dùng chung như IHuman
│   │   ├── ScriptableObjects/  Database dữ liệu runtime
│   │   ├── Services/           Service scene, interaction, chat, LLM và save
│   │   ├── UIs/                Menu, dialogue, chat và panel sổ tay
│   │   └── Utils/              Bootstrap, input, service locator và helper
│   ├── GameAssets/
│   │   ├── Animations/         Asset animation
│   │   ├── Fonts/              Asset font
│   │   ├── Prefabs/            GameObject có thể tái sử dụng
│   │   ├── Shaders/            Shader của project
│   │   ├── Signal/             Asset signal cho Timeline
│   │   ├── SOs/                Asset ScriptableObject
│   │   ├── Sprites/            Đồ họa 2D
│   │   └── TilePalette/        Palette cho Tilemap
│   ├── AddressableAssetsData/  Cấu hình và dữ liệu được sinh cho Addressables
│   ├── InputSystem_Actions.*   Input Action asset và C# wrapper được sinh tự động
│   ├── Resources/              Asset được nạp bằng hệ thống Resources của Unity
│   ├── Settings/               Asset cấu hình rendering và project
│   ├── TextMesh Pro/           Asset và setting của TextMesh Pro
│   └── Timeline/               Asset Timeline
├── Packages/
│   ├── manifest.json           Dependency trực tiếp của project
│   └── packages-lock.json      Phiên bản dependency đã resolve
├── ProjectSettings/            Cấu hình editor và build của Unity
├── Assembly-CSharp*.csproj     File project C# được sinh tự động
├── NewTheHalfOfTruth.slnx     File solution
├── Build/                      Output build cục bộ
├── Library/, Temp/, Logs/      Dữ liệu làm việc được Unity sinh tự động
└── README.md                  Tài liệu tiếng Anh
```

Không nên chỉnh sửa thủ công các thư mục do Unity sinh như `Library`, `Temp` và phần lớn file trong `Logs`. Chúng sẽ được Unity tạo lại và không thuộc kiến trúc ứng dụng.

## Định nghĩa và khái niệm cốt lõi

### Đăng ký service

Các service triển khai `IService`, cung cấp vòng đời `Awake()` và `OnDestroy()`. `ServiceLocator` lưu các service theo type, cho phép các object trong scene lấy service ngay lập tức hoặc đăng ký callback để chờ service sẵn sàng.

Có hai loại service base chính:

- `MonoBehaviourService<T>` đăng ký một service `MonoBehaviour` gắn với scene.
- `LogicOnlyService<T>` đăng ký service không kế thừa `MonoBehaviour`, được tạo bởi `DependencyInitializer`.

Các callback của service phải được gỡ khi object bị disable hoặc destroy. Điều này ngăn callback trỏ đến những object không còn tồn tại.

### Cơ chế Singleton

`Singleton<T>` cung cấp instance tĩnh cho một số class `MonoBehaviour`. Nó có thể tìm instance hiện có hoặc tự động tạo instance mới. Tuy vậy, scene vẫn nên chứa instance được cấu hình đúng vì việc tự động tạo có thể che giấu lỗi thiếu reference trong scene.

### Interactable

`Interactable` là class cơ sở cho các object mà người chơi có thể chọn và sử dụng. Nó tự đăng ký với `InteractableService` và nhận quyền truy cập đến `SavedDataService`.

Các loại interactable hiện tại gồm:

- `Evidence`
- `NPC`
- `Item`

### Input focus

`InputHandler` kết nối wrapper `InputSystem_Actions` được sinh tự động với các hệ thống gameplay. Nó phát event di chuyển và tương tác, đồng thời chuyển đổi giữa action map của player và UI.

- `InputHandler.FocusPlayerInput()` bật các action của player.
- `InputHandler.FocusUIInput()` bật các action của UI.

Các panel UI và quá trình chuyển scene phải duy trì đúng cơ chế focus này để input gameplay không tiếp tục hoạt động khi người chơi đang sử dụng menu hoặc panel hội thoại.

### Addressables

Scene, animation clip và cutscene asset được tham chiếu bằng Addressables. Code runtime nên dùng `AssetReference` hoặc runtime key của Addressable thay vì giả định scene luôn có sẵn trong build index thông thường.

### Dữ liệu ScriptableObject

`EvidenceDatabase` và `NPCDatabase` lưu nội dung được các object gameplay sử dụng. Các asset này được gán qua Unity Inspector và là một phần của model dữ liệu, dù dữ liệu không được lưu trực tiếp trong C# source.

### Dữ liệu tải scene

`CD.LoadSceneData` chứa thông tin chuyển scene:

- Scene Addressable cần tải.
- Hướng mà player cần quay mặt sau khi tải scene.
- Identifier của teleport/start point nơi player xuất hiện.

### Trạng thái hướng sự kiện

Hệ thống save và chat phát event khi trạng thái thay đổi. Các hệ thống UI subscribe vào những event này thay vì trực tiếp sở hữu dữ liệu save. Cách này tách biệt phần lưu trữ và phần hiển thị.

## Luồng hoạt động của project

### 1. Khởi động

1. `Bootstrapper.LoadBootstrapper()` chạy trước khi scene đầu tiên được tải.
2. Nó tải scene index `0` bằng `SceneManager.LoadSceneAsync`.
3. `ProjectSettings/EditorBuildSettings.asset` hiện đặt `Assets/Scenes/Bootstrapper.unity` ở index đó.
4. `DependencyInitializer` tạo các logic-only service:
   - `SavedDataService`
   - `ChatHistoryService`
   - `LLMService`
5. Các service gắn với scene tự đăng ký với `ServiceLocator` khi GameObject của chúng được khởi tạo.

### 2. Input và tương tác của player

```text
InputSystem_Actions
        |
        v
InputHandler
   |         |
   v         v
Player    InteractableService
```

`InputHandler` gửi thông tin di chuyển đến `Player`, để player di chuyển và cập nhật `HumanAnimSystem`. Nó cũng gửi input tương tác đến `InteractableService`, nơi tìm interactable đang hoạt động gần nhất trong phạm vi được cấu hình.

Khi một tương tác được chọn, player và target quay mặt về phía nhau trước khi gọi `Interactable.OnInteract()`.

### 3. Hội thoại với NPC

```text
Player tương tác
        |
        v
ChatNpc
        |
        v
ChatHistoryService / ChatUISystem
        |
        v
LLMService
        |
        v
API RAG/LLM bên ngoài
        |
        v
Phản hồi trên dialogue hoặc chat UI
```

`ChatNpc` lấy tên, system prompt và đoạn hội thoại ban đầu từ `NPCDatabase`. `LLMService` gửi tên NPC, system của NPC và câu hỏi của người chơi dưới dạng JSON. Response chứa câu trả lời và cờ `weakPointHit`. Khi weak point được kích hoạt, `LLMService.OnWeakPointHit` được phát để các logic game-specific xử lý.

Backend RAG/GraphRAG bên ngoài chịu trách nhiệm truy hồi tri thức, sinh câu trả lời và phát hiện weak point. Các thành phần backend này không được triển khai trong repository Unity.

### 4. Bằng chứng, cửa và dữ liệu save

Các interactable thông báo cho `SavedDataService` khi người chơi thu thập bằng chứng, ghi chú vào sổ tay, nói chuyện với NPC hoặc thay đổi trạng thái cửa. `SavedDataService` serialize tiến trình của người chơi vào Unity `PlayerPrefs` và phát event để UI cập nhật.

`ConditionDoor` mở rộng hành vi cửa bằng các điều kiện tiến trình. Vì vậy, identifier của bằng chứng và cửa được sử dụng như save key và cần được giữ ổn định.

### 5. Chuyển scene

```text
MainMenu / SceneTransis / Door
              |
              v
        SceneService
              |
              v
Fade to black -> Tải scene Addressables -> Fade in
              |
              v
Khôi phục start point và hướng quay của player
```

`SceneService` điều khiển lớp chuyển cảnh, tạm thời chuyển input focus sang UI, tải scene Addressable rồi khôi phục input của player. Service này cũng có thể ánh xạ địa chỉ trigger của scene tới một cutscene asset trước khi tải scene.

`Player` sử dụng dữ liệu scene để đặt vị trí tại start point được cấu hình và áp dụng hướng quay tương ứng.

### 6. UI focus

`BaseUIPanelSystem` tắt input của player khi panel mở và khôi phục input khi panel đóng. `ChatUISystem`, `DialogueUISystem`, `NotebookUISystem` và các component menu được xây dựng dựa trên behavior này.

## Class chính theo module

### Utils

| Class | Trách nhiệm |
| --- | --- |
| `Bootstrapper` | Tải bootstrap scene trước khi game khởi động bình thường và có thể yêu cầu tải một scene Addressable ban đầu. |
| `DependencyInitializer` | Tạo và khởi tạo các logic-only service. |
| `ServiceLocator` | Đăng ký service và resolve dependency ngay lập tức hoặc trì hoãn. |
| `Singleton<T>` | Cung cấp vòng đời singleton cho một số `MonoBehaviour`. |
| `InputHandler` | Kết nối Input System với event gameplay/UI và chuyển đổi input map. |
| `ObjectIdentifier` | Tìm scene object thông qua identifier ổn định. |
| `PersistentObject` | Hỗ trợ các object tồn tại qua nhiều scene. |
| `CommonData` | Định nghĩa các cấu trúc dữ liệu dùng chung cho scene, animation và cutscene. |

### Services

| Class | Trách nhiệm |
| --- | --- |
| `SceneService` | Chuyển cảnh bằng hiệu ứng fade, tải scene Addressable và truyền dữ liệu scene. |
| `InteractableService` | Theo dõi interactable, chọn target gần nhất và xử lý input tương tác. |
| `LLMService` | Gửi câu hỏi NPC đến API RAG/LLM bên ngoài và xử lý response. |
| `ChatHistoryService` | Lưu conversation và chat history của NPC hiện tại. |
| `SavedDataService` | Đọc và lưu dữ liệu sổ tay, NPC, cửa và tiến trình tùy chỉnh. |
| `CutsceneService` | Service shell dành cho behavior cutscene; mapping cutscene trong chuyển scene hiện được triển khai tại `SceneService`. |
| `MonoBehaviourService<T>` | Base class cho service gắn với scene. |
| `LogicOnlyService<T>` | Base class cho service không cần `GameObject` trong scene. |

### Game

| Class | Trách nhiệm |
| --- | --- |
| `Player` | Xử lý di chuyển, animation theo hướng, đăng ký tương tác và vị trí spawn khi chuyển scene. |
| `Interactable` | Contract cơ sở và cơ chế đăng ký cho các interactable. |
| `Evidence` | Object bằng chứng có thể tương tác, được dùng bởi hệ thống điều tra và sổ tay. |
| `Door` | Behavior chuyển scene và trạng thái cửa. |
| `ConditionDoor` | Behavior cửa được điều khiển bởi điều kiện tiến trình đã lưu. |
| `SceneTransis` | Trigger chuyển scene được cấu hình trong Inspector. |
| `TeleportPoint` | Destination có identifier dùng khi đi vào scene. |

### NPC

| Class | Trách nhiệm |
| --- | --- |
| `ChatNpc` | NPC hội thoại cơ sở; tải dữ liệu NPC và bắt đầu tương tác chat. |
| `Johny` | Behavior riêng của NPC cho hình ảnh spawn và reference đến ending/cutscene. |
| `Brad` | Behavior riêng kế thừa từ `ChatNpc`. |
| `WalkingNpc` | Behavior di chuyển của NPC. |
| `AppearCondNpc` | Behavior xuất hiện của NPC được điều khiển bởi điều kiện. |

### UI

| Class | Trách nhiệm |
| --- | --- |
| `BaseUIPanelSystem` | Behavior mở/đóng chung và chuyển đổi input focus giữa player/UI. |
| `ChatUISystem` | Giao diện chat history và nhập câu hỏi của người chơi. |
| `DialogueUISystem` | Hiển thị dialogue và xử lý response. |
| `NotebookUISystem` | Giao diện sổ tay và ghi chú bằng chứng đã lưu. |
| `MainMenu` | Xử lý thao tác menu chính và vào scene. |
| `ChatBubble` | Thành phần hiển thị một đoạn chat/dialogue. |
| `BookPage` | Hiển thị trang sổ tay và tương tác với ghi chú đã lưu. |
| `TypingEffectTMP` | Hiệu ứng hiển thị chữ theo thời gian cho TextMesh Pro. |

### Animations

| Class | Trách nhiệm |
| --- | --- |
| `BaseAnimSystem` | Tải và quản lý animation clip dạng Addressable. |
| `HumanAnimSystem` | Chọn animation idle/walk dựa trên hướng và trạng thái của human. |

### ScriptableObjects

| Class | Trách nhiệm |
| --- | --- |
| `EvidenceDatabase` | Lưu định nghĩa bằng chứng và kết nối dữ liệu bằng chứng với hệ thống save. |
| `NPCDatabase` | Lưu system prompt và đoạn hội thoại ban đầu của NPC. |

### Interface và dữ liệu dùng chung

| Type | Trách nhiệm |
| --- | --- |
| `IHuman` | Contract dùng chung cho object có thể quay mặt theo hướng. |
| `IService` | Contract vòng đời được `ServiceLocator` sử dụng. |
| `CD.LoadSceneData` | Dữ liệu scene đích, hướng quay của player và start point. |
| `CD.AnimClipReference` | Reference strongly typed tới animation dạng Addressable. |
| `CD.CutsceneData` | Mapping giữa địa chỉ scene trigger và cutscene asset. |

## Cấu hình quan trọng

### Phiên bản Unity

Phiên bản editor yêu cầu được ghi trong `ProjectSettings/ProjectVersion.txt`:

```text
6000.3.19f1
```

Nên sử dụng đúng phiên bản Unity này khi có thể. Mở project bằng phiên bản khác có thể khiến Unity reserialize scene, asset hoặc project setting.

### Build Settings

`ProjectSettings/EditorBuildSettings.asset` hiện chỉ chứa:

```text
Assets/Scenes/Bootstrapper.unity
```

Điều này quan trọng vì `Bootstrapper` tải build index `0` trước khi phần còn lại của game khởi động. Các gameplay scene dự kiến được tải thông qua Addressables nên không cần được liệt kê như build scene thông thường trong flow này.

### Packages

- `Packages/manifest.json` khai báo các dependency trực tiếp.
- `Packages/packages-lock.json` ghi lại phiên bản dependency đã resolve.
- Các package quan trọng gồm Addressables, Input System, UniTask, Cinemachine, Timeline, URP, các package Unity 2D, UI, hỗ trợ TextMesh Pro và Tri Inspector.

Khi thay đổi dependency, hãy cập nhật package thông qua Unity Package Manager để cả hai file được đồng bộ. Tránh chỉnh sửa thủ công thông tin package đã resolve nếu không có lý do cụ thể.

### Input Actions

`Assets/InputSystem_Actions.inputactions` là asset nguồn cho wrapper được sinh tại `Assets/InputSystem_Actions.cs`. Thay đổi tên action, action map hoặc binding có thể ảnh hưởng đến `InputHandler` và mọi class subscribe vào static event của nó.

### Addressables

Scene, animation clip và cutscene asset dạng Addressable phụ thuộc vào serialized GUID và runtime key. Khi thêm hoặc di chuyển asset có thể tải runtime:

1. Xác nhận asset vẫn được đánh dấu Addressable.
2. Gán lại reference trong Inspector nếu Unity báo reference bị hỏng.
3. Build lại nội dung Addressables khi cần.
4. Kiểm tra flow chuyển scene bắt đầu từ bootstrap scene.

Nên kiểm tra layout chính xác của Addressables group trong Unity Editor thay vì suy ra chỉ từ code.

### Reference trong scene và asset đã serialize

Nhiều dependency được gán trong scene, prefab hoặc ScriptableObject thay vì được khởi tạo trong code. Các reference quan trọng trong Inspector gồm:

- Database và setting save của `DependencyInitializer`.
- Transition image và cutscene mapping của `SceneService`.
- Interaction indicator canvas của `InteractableService`.
- Animation reference và sprite renderer của `Player`.
- NPC database và animation reference.
- Reference của door, teleport point và scene transition.
- Các object panel và button của UI.

C# compile thành công không đảm bảo các serialized reference vẫn hợp lệ.

## Điểm quan trọng khi thay đổi code

- Bảo toàn các file Unity `.meta`. Chúng chứa GUID được scene, prefab, Addressables và ScriptableObject sử dụng.
- Giữ `Bootstrapper.unity` ở build index `0`, trừ khi đồng thời thay đổi chiến lược bootstrap.
- Đăng ký và hủy service callback đối xứng trong `OnEnable`/`OnDisable` hoặc `Awake`/`OnDestroy`.
- Hủy subscribe khỏi static input event và save event khi object bị disable hoặc destroy.
- Bảo toàn contract input focus giữa player và UI. Panel UI mới nên sử dụng behavior của `BaseUIPanelSystem` hoặc tự khôi phục input player rõ ràng.
- Giữ tên GameObject của NPC đồng bộ với key tra cứu trong `NPCDatabase`.
- Coi evidence key, tên cửa, identifier của teleport point và tên field save là các data contract ổn định.
- Giữ Addressable reference hợp lệ khi di chuyển hoặc đổi tên scene, animation clip hay cutscene asset.
- Release Addressables handle khi tự tải asset bằng code.
- Xử lý lỗi LLM và network để chat UI hoặc input system không bị khóa ở trạng thái dở dang.
- Nếu endpoint LLM hoặc response schema thay đổi, cần cập nhật đồng thời `LLMService`, `LlmApiResponse` và code UI sử dụng response.
- Không bỏ qua các giả định vòng đời của `ServiceLocator` bằng cách cache scene service sau khi scene đã bị hủy.
- Cân nhắc backward compatibility trước khi thay đổi dữ liệu save trong `PlayerPrefs`, vì người dùng có thể đã có dữ liệu từ schema cũ.
- Không chỉnh sửa thủ công `Library`, `Temp` hoặc các thư mục output được Unity sinh tự động.
- Sau khi thay đổi serialized field, hãy mở các scene và prefab liên quan trong Unity để xác nhận reference không bị mất.

## Phát triển và kiểm tra

1. Cài đặt Unity `6000.3.19f1`.
2. Mở thư mục gốc của repository dưới dạng Unity project.
3. Chờ Unity resolve package từ `Packages/manifest.json`.
4. Kiểm tra cấu hình Addressables và xác nhận các group/asset cần thiết có sẵn.
5. Mở `Assets/Scenes/Bootstrapper.unity`.
6. Chạy project từ bootstrap scene.
7. Kiểm tra việc di chuyển player, chọn mục tiêu tương tác, luồng chat với NPC, lưu bằng chứng và ít nhất một lần chuyển scene bằng Addressables.

Repository hiện chưa mô tả automated gameplay test suite. Việc kiểm tra runtime trong Unity Editor vẫn quan trọng vì nhiều dependency được lưu trong dữ liệu scene và asset đã serialize.
