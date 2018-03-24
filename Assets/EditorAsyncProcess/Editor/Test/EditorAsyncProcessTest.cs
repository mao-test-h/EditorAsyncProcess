using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace EditorUtility.Test
{
    public class AsyncProcessTest
    {
        [MenuItem("Test Editor Utility/AsyncProcess/Test WaitProcess")]
        static void TestAddProcess()
        {
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
        }


        const string ApiServerURL = @"http://localhost:3000/api_test/"; // ローカル

        [MenuItem("Test Editor Utility/AsyncProcess/Test Get")]
        static void TestGet()
        {
            // Editor上でGET通信を行う(同時呼び出し可能)
            EditorAsyncProcess.Get(
                ApiServerURL + "test2?test1=call_1&test2=hoge",
                (res) =>
                {
                    Debug.LogFormat(
                        "Complete Test Get... [data : {0}], [error : {1}]",
                        res.ResponseData, res.Error);
                }
            );
        }

        [MenuItem("Test Editor Utility/AsyncProcess/Test Post")]
        static void TestPost()
        {
            // Editor上でPOST通信を行う(同時呼び出し可能)
            WWWForm form = new WWWForm();
            form.AddField("param", "hoge");
            EditorAsyncProcess.Post(
                ApiServerURL + "test3",
                form,
                (res) =>
                {
                    Debug.LogFormat(
                        "Complete Test Post... [data : {0}], [error : {1}]",
                        res.ResponseData, res.Error);
                }
            );
        }
    }

}