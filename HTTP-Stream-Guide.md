# HTTP Streaming in this project

This project does not wait for a full LLM response to return as one big payload. Instead, it uses a Server-Sent Events (SSE) style HTTP stream and processes chunks as they arrive.

The main implementation is in `Assets/Scripts/Services/LLMService.cs`.

## 1. Request flow

`LLMService.CallLLMAsync(...)` builds a request object:

- `npcName`
- `npcSystem`
- `question`

Then it serializes the data with `JsonUtility.ToJson(...)` and sends it to the backend endpoint:

```csharp
private const string BASE_URL = "https://rag-jkoh.onrender.com/api/query/ask-stream";
```

The actual request payload must use PascalCase fields, because the backend validates `NpcName`, `NpcSystem`, and `Question`:

```csharp
public struct LLMRequest
{
    public string NpcName;
    public string NpcSystem;
    public string Question;
}
```

The actual request is sent via `UnityWebRequest`:

```csharp
using var request = new UnityWebRequest(url, "POST");
byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
request.uploadHandler = new UploadHandlerRaw(bodyRaw);
request.downloadHandler = new SseDownloadHandler();
request.SetRequestHeader("Content-Type", "application/json");
request.SetRequestHeader("Accept", "text/event-stream");
```

Important details:

- Method: `POST`
- Content-Type: `application/json`
- Accept: `text/event-stream`
- The response is intentionally a streaming response, not a normal JSON document.

## 2. Why SSE is used

The backend returns a stream of SSE events. Each event includes an `event:` label and a `data:` payload. The client reads it incrementally and updates the UI as tokens arrive.

The live API format observed from the server is:

```text
event: meta
data: {"weakPointHit":false}

event: token
data: {"text":"Hey"}

event: token
data: {"text":" there"}

event: done
data: {}
```

This is especially useful for chat UIs, because the player sees text appear live rather than waiting for the full response to finish.

## 3. Receiving chunks from the network

The custom `SseDownloadHandler` inherits from `DownloadHandlerScript` and captures raw bytes as they arrive:

```csharp
protected override bool ReceiveData(byte[] data, int dataLength)
{
    var chunk = new byte[dataLength];
    Buffer.BlockCopy(data, 0, chunk, 0, dataLength);
    lock (_lock)
        _chunks.Enqueue(chunk);
    return true;
}
```

This is the key streaming mechanism:

- the engine pushes incoming network bytes into a queue
- the game loop drains that queue as data becomes available
- this avoids blocking until the whole response is complete

The queue is read by `TryDequeue(...)`:

```csharp
public bool TryDequeue(out byte[] chunk)
{
    lock (_lock)
    {
        if (_chunks.Count == 0)
        {
            chunk = null;
            return false;
        }

        chunk = _chunks.Dequeue();
        return true;
    }
}
```

## 4. Parsing the stream

The parser is `SseParser`.

It keeps three important pieces of state:

- `_lineBytes`: the current raw line being accumulated
- `_eventData`: the full text collected from `data:` lines for the current event
- `_eventName`: the current SSE event type, such as `token`, `meta`, or `done`

The `Append(...)` method reads each byte in the incoming chunk:

```csharp
foreach (var value in chunk)
{
    if (value == (byte)'\n')
    {
        ProcessLine(...);
        _lineBytes.Clear();
    }
    else if (value != (byte)'\r')
    {
        _lineBytes.Add(value);
    }
}
```

This means:

- `\r` is ignored
- `\n` ends the current line
- each finished line is passed to `ProcessLine(...)`

## 5. Processing each SSE line

`ProcessLine(...)` checks whether a line starts with `event:` or `data:`:

```csharp
if (line.StartsWith("event:", StringComparison.Ordinal))
{
    _eventName = line.Substring("event:".Length).Trim();
    return;
}

if (line.StartsWith("data:", StringComparison.Ordinal))
{
    if (_eventData.Length > 0)
        _eventData.Append('\n');
    _eventData.Append(line.Substring("data:".Length).Trim());
}
```

When the parser reaches a blank line, it treats that as the end of the current event and calls `ProcessEvent(...)`:

```csharp
if (string.IsNullOrEmpty(line))
{
    ProcessEvent(responseBuilder, onTokenReceived, onWeakPointHit);
    return;
}
```

This matches the SSE pattern:

```text
data: { ... JSON ... }

data: { ... next JSON ... }
```

## 6. Handling each event payload

`ProcessEvent(...)` is where the token stream is consumed:

```csharp
var data = _eventData.ToString();
_eventData.Clear();
if (data == "[DONE]")
    return;

var streamEvent = JsonUtility.FromJson<LlmStreamEvent>(data);
```

The token payload from the live API is shaped like:

```json
{"text":"Hey"}
```

The metadata payload is shaped like:

```json
{"weakPointHit":false}
```

Then the code does this:

```csharp
if (string.Equals(eventName, "meta", StringComparison.OrdinalIgnoreCase))
{
    var streamEvent = JsonUtility.FromJson<LlmStreamEvent>(data);
    if (streamEvent.weakPointHit && !_weakPointHitRaised)
    {
        _weakPointHitRaised = true;
        onWeakPointHit?.Invoke();
    }
    return;
}

var token = ExtractToken(data);
if (string.IsNullOrEmpty(token))
    return;

responseBuilder.Append(token);
onTokenReceived?.Invoke(token);
```

This means:

- partial tokens arrive one by one
- each token is appended to the final response string
- the caller can react immediately to each received token
- a special `weakPointHit` flag can trigger a higher-level event once

## 7. Main streaming loop

The request is processed in `PostRequestAsync(...)`:

```csharp
var operation = request.SendWebRequest();
var responseBuilder = new StringBuilder();
var parser = new SseParser();

while (!operation.isDone)
{
    ProcessSseChunks(downloadHandler, parser, responseBuilder, onTokenReceived, OnWeakPointHit);
    await UniTask.Yield();
}

ProcessSseChunks(downloadHandler, parser, responseBuilder, onTokenReceived, OnWeakPointHit);
parser.Complete(responseBuilder, onTokenReceived, OnWeakPointHit);
```

This loop does the following:

1. sends the HTTP request
2. continuously drains queued chunks while network work is still active
3. yields control back to Unity so the frame loop stays responsive
4. processes any remaining data after completion
5. finalizes the SSE parser

## 8. Final result to the caller

After the stream ends, `PostRequestAsync(...)` checks the underlying HTTP result:

```csharp
if (request.result == UnityWebRequest.Result.Success)
    return responseBuilder.ToString();
else
    throw new InvalidOperationException(request.error ?? ERROR_MESSAGE);
```

The final text is then passed back to `CallLLMAsync(...)`, which stores it in chat history and returns an `LLMResponse`:

```csharp
_fileLogService?.LogConversation(npcName, question, response, elapsedMilliseconds);
_chatHistoryService?.PushChatItem(npcName, response);

return new LLMResponse
{
    response = response,
    status = LLMResponse.Status.Success
};
```

## 9. In practice: how the UI receives live text

When the game asks the LLM a question, it calls something like:

```csharp
await LLMService.Instance.CallLLMAsync(
    npcName,
    npcSystem,
    question,
    token => { /* update UI with partial token */ }
);
```

The callback `onTokenReceived` is invoked for each token as soon as it arrives, so the UI can append characters progressively while the server is still streaming.

This is why the chat text appears to “type” in real time instead of waiting for a full reply.

## 10. Real stream result observed from Postman

The actual network result from the backend matches this pattern. In Postman, the response is not a single JSON block; it is a sequence of streamed events.

Observed examples from the running request:

```text
data: {"token":"*text*":"..."}

data: {"token":"*text*":"..."}

data: {"weakPointHit":false}
```

The important point is that each event is a separate `data:` message and the client processes them one by one:

- token events add text to the chat response
- the `weakPointHit` flag is read once and can trigger a game event
- the stream continues until the server closes the connection or sends a terminal marker

In other words, the response is effectively:

```text
stream event 1 -> {"token":"t"}
stream event 2 -> {"token":"e"}
stream event 3 -> {"token":"x"}
stream event 4 -> {"token":"t"}
stream event 5 -> {"weakPointHit":false}
```

This is exactly what the Unity code is designed to parse:

```csharp
var streamEvent = JsonUtility.FromJson<LlmStreamEvent>(data);
if (streamEvent.weakPointHit && !_weakPointHitRaised)
    onWeakPointHit?.Invoke();

if (string.IsNullOrEmpty(streamEvent.token))
    return;

responseBuilder.Append(streamEvent.token);
onTokenReceived?.Invoke(streamEvent.token);
```

So the UI receives partial text immediately while the HTTP connection is still open.

## 11. Summary

The streaming pipeline is:

1. build JSON payload with PascalCase fields (`NpcName`, `NpcSystem`, `Question`)
2. send POST request with `Accept: text/event-stream`
3. receive raw bytes in `SseDownloadHandler`
4. queue bytes in a thread-safe queue
5. parse SSE `event:` and `data:` lines
6. handle `meta`, `token`, and `done` events correctly
7. deserialize JSON event objects
8. append each token immediately
9. stop when the `done` event is received

This is a lightweight SSE implementation tailored for Unity and the project’s chat flow.
