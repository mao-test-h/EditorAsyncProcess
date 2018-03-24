using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace EditorUtility
{
    /// <summary>
    /// 非同期処理監視
    /// </summary>
    interface IProcessOperation
    {
        /// <summary>
        /// 毎フレーム更新
        /// </summary>
        /// <returns>進行中の処理が完了したらfalse</returns>
        bool Update();
    }

    /// <summary>
    /// Editor拡張用 汎用非同期処理
    /// </summary>
    public partial class EditorAsyncProcess
    {
        // --------------------------------
        #region // Constants

        /// <summary>
        /// プロセスの同時進行数
        /// </summary>
        const int ProcessLimit = 5;

        #endregion // Constants

        // --------------------------------
        #region // Private Members

        /// <summary>
        /// プロセス管理用のキュー
        /// </summary>
        static Queue<IProcessOperation> _processOperationsQueue = null;

        /// <summary>
        /// 実行中のプロセス
        /// </summary>
        static List<IProcessOperation> _runningProcessOperations = null;

        #endregion // Private Members



        // ----------------------------------------------------------------
        #region // Public Methods

        /// <summary>
        /// プロセスの待機
        /// </summary>
        /// <param name="progressProcess">進行中の処理(完了したらfalseを返す)</param>
        /// <param name="finishedCallback">完了時コールバック</param>
        /// <returns>プロセス待機用オペレーション</returns>
        public static WaitProcessOperation WaitProcess(Func<bool> progressProcess, Action finishedCallback)
        {
            var operation = new WaitProcessOperation(progressProcess, finishedCallback);
            AddOperation(operation);
            return operation;
        }

        /// <summary>
        /// GET通信
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="finishedCallback">コールバック</param>
        /// <param name="headers">ヘッダー</param>
        /// <returns>通信処理用オペレーション</returns>
        public static WebRequestOperation Get(
            string url,
            Action<WebRequestOperation.Response> finishedCallback,
            Dictionary<string, string> headers = null)
        {
            var operation = new WebRequestOperation(url, finishedCallback, headers);
            AddOperation(operation);
            return operation;
        }

        /// <summary>
        /// POST通信
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="body">ボディデータ</param>
        /// <param name="finishedCallback">コールバック</param>
        /// <param name="headers">ヘッダー情報</param>
        /// <returns>通信処理用オペレーション</returns>
        public static WebRequestOperation Post(
            string url,
            WWWForm body,
            Action<WebRequestOperation.Response> finishedCallback,
            Dictionary<string, string> headers = null)
        {
            var operation = new WebRequestOperation(url, finishedCallback, headers, body);
            AddOperation(operation);
            return operation;
        }

        #endregion // Public Methods

        // ----------------------------------------------------------------
        #region // Private Methods

        /// <summary>
        /// オペレーションの追加
        /// </summary>
        /// <param name="addOperation">追加するオペレーション</param>
        static void AddOperation(IProcessOperation addOperation)
        {
            if (_processOperationsQueue == null)
            {
                _processOperationsQueue = new Queue<IProcessOperation>();
                _runningProcessOperations = new List<IProcessOperation>();
                EditorApplication.update = () =>
                {
                    if (_runningProcessOperations.Count < EditorAsyncProcess.ProcessLimit
                        && _processOperationsQueue.Count > 0)
                    {
                        _runningProcessOperations.Add(_processOperationsQueue.Dequeue());
                    }
                    for (int i = 0; i < _runningProcessOperations.Count; ++i)
                    {
                        var operation = _runningProcessOperations[i];
                        if (!operation.Update())
                        {
                            _runningProcessOperations.RemoveAt(i);
                        }
                    }
                };
            }
            _processOperationsQueue.Enqueue(addOperation);
        }

        #endregion // Private Methods
    }
}