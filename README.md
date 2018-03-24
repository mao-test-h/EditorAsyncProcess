# EditorAsyncProcess
Editor拡張用 汎用非同期処理

# 使用例

## プロセスの待機

```
EditorAsyncProcess.WaitProcess(
    () =>
    {
        // ポーズ中は待機
        return EditorApplication.isPaused;
    },
    () =>
    {
        // ポーズ解除時に呼ばれる
        Debug.Log("Complete Test WaitProcess.");
    }
);
```

## GET通信

```
// Editor上でGET通信を行う(同時呼び出し可能)
EditorAsyncProcess.Get(
    (url),
    (res) =>
    {
        Debug.LogFormat(
            "Complete Test Get... [data : {0}], [error : {1}]",
            res.ResponseData, res.Error);
    }
);
```

## POST通信

```
// Editor上でPOST通信を行う(同時呼び出し可能)
WWWForm form = new WWWForm();
form.AddField("param", "hoge");
EditorAsyncProcess.Post(
    (url),
    form,
    (res) =>
    {
        Debug.LogFormat(
            "Complete Test Post... [data : {0}], [error : {1}]",
            res.ResponseData, res.Error);
    }
);
```
